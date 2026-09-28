// Stylised water for URP: gentle vertex waves, shallow-to-deep colour and a stepped foam band where the water
// meets geometry (from the camera depth texture), toon main light with shadows, fog. At night it reads as water by
// its moving light: small animated ripples break up a cold sky sheen into crisp stepped glints, and every torch or
// lantern throws a shimmering streak across the ripples.
// Renders after opaques (it samples their depth). Needs a subdivided mesh for the waves (see WaterSurface).
Shader "MyGame/Water"
{
    Properties
    {
        _DeepColor("Deep Color", Color) = (0.14, 0.3, 0.35, 1)
        _ShallowColor("Shallow Color", Color) = (0.28, 0.5, 0.52, 1)
        _FoamColor("Foam Color", Color) = (0.86, 0.9, 0.88, 1)
        _ShadowTint("Shadow Tint", Color) = (0.74, 0.71, 0.88, 1)
        _DepthFade("Depth Fade (m)", Float) = 1.6
        _FoamDistance("Foam Distance (m)", Float) = 0.25
        _WaveHeight("Wave Height (m)", Float) = 0.05
        _WaveLength("Wave Length (m)", Float) = 3.5
        _WaveSpeed("Wave Speed", Float) = 0.8
        _LampReflection("Lamp Pool", Range(0, 3)) = 0.2
        _LampSpecular("Lamp Streaks", Range(0, 6)) = 2.2
        _LampShininess("Lamp Streak Sharpness", Float) = 30
        _RippleScale("Ripple Scale (per m)", Float) = 2.2
        _RippleSpeed("Ripple Speed", Float) = 0.35
        _RippleStrength("Ripple Strength", Float) = 0.12
        _SkyColor("Sky Reflection", Color) = (0.12, 0.17, 0.26, 1)
        _SheenDir("Sheen Direction", Vector) = (0, 0.5, 1, 0)
        _GlintColor("Glint Colour", Color) = (0.25, 0.32, 0.43, 1)
        _GlintThreshold("Glint Threshold", Range(0.8, 1)) = 0.992
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent-100" }

        Pass
        {
            Name "WaterForward"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _DeepColor;
                half4 _ShallowColor;
                half4 _FoamColor;
                half4 _ShadowTint;
                float _DepthFade;
                float _FoamDistance;
                float _WaveHeight;
                float _WaveLength;
                float _WaveSpeed;
                half _LampReflection;
                half _LampSpecular;
                float _LampShininess;
                float _RippleScale;
                float _RippleSpeed;
                float _RippleStrength;
                half4 _SkyColor;
                float4 _SheenDir;
                half4 _GlintColor;
                half _GlintThreshold;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                half fogFactor : TEXCOORD2;
            };

            // Two crossing sine waves; returns height and its x/z slope.
            float3 Wave(float2 p)
            {
                float k = 6.2831853 / _WaveLength;
                float t = _Time.y * _WaveSpeed;
                float a = k * (p.x * 0.8 + p.y * 0.6) + t;
                float b = k * 1.3 * (p.x * -0.5 + p.y * 0.87) + t * 1.4;
                float h = _WaveHeight * (sin(a) + 0.6 * sin(b));
                float dx = _WaveHeight * k * (0.8 * cos(a) + 0.6 * 1.3 * -0.5 * cos(b));
                float dz = _WaveHeight * k * (0.6 * cos(a) + 0.6 * 1.3 * 0.87 * cos(b));
                return float3(h, dx, dz);
            }

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), u.x), lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), u.x), u.y);
            }

            // Small ripples: two layers of noise drifting different ways.
            float Ripples(float2 p)
            {
                float t = _Time.y * _RippleSpeed;
                float2 q = p * _RippleScale * float2(0.45, 1.6); // stretched sideways: light breaks into thin horizontal streaks
                return ValueNoise(q + float2(t, t * 0.6)) * 0.6 + ValueNoise(q * 2.3 + float2(-t * 0.8, t * 1.1)) * 0.4;
            }

            float3 RippleNormal(float3 waveNormal, float2 p)
            {
                const float e = 0.04;
                float h = Ripples(p);
                float dx = (Ripples(p + float2(e, 0)) - h) / e;
                float dz = (Ripples(p + float2(0, e)) - h) / e;
                return normalize(waveNormal + float3(-dx, 0, -dz) * _RippleStrength);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 wave = Wave(positionWS.xz);
                positionWS.y += wave.x;
                output.positionWS = positionWS;
                output.normalWS = normalize(float3(-wave.y, 1.0, -wave.z));
                output.positionCS = TransformWorldToHClip(positionWS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float surfaceDepth = -TransformWorldToView(input.positionWS).z;
                float below = max(0.0, sceneDepth - surfaceDepth);

                half3 water = lerp(_ShallowColor.rgb, _DeepColor.rgb, saturate(below / _DepthFade));

                // Foam: a stepped band whose edge wobbles slowly.
                float wobble = 0.25 * sin(input.positionWS.x * 2.3 + _Time.y * 1.1) * sin(input.positionWS.z * 1.9 - _Time.y * 0.8);
                half foam = step(below, _FoamDistance * (1.0 + wobble));

                float3 normalWS = RippleNormal(normalize(input.normalWS), input.positionWS.xz);
                float3 viewWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float3 reflected = reflect(-viewWS, normalWS);
                Light light = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half lit = smoothstep(0.35, 0.65, light.shadowAttenuation) * smoothstep(0.0, 0.1, dot(normalWS, light.direction));
                half3 albedo = lerp(water, _FoamColor.rgb, foam);
                half3 color = albedo * lerp(_ShadowTint.rgb, 1.0, lit) * light.color * 0.85 + albedo * SampleSH(normalWS) * 0.35;

                // The night sky on the surface: a cold sheen the ripples break up, and crisp glints where they catch it.
                half facing = dot(reflected, normalize(_SheenDir.xyz));
                half fresnel = pow(1.0 - saturate(dot(normalWS, viewWS)), 3.0);
                half sheen = smoothstep(0.85, 0.99, facing);
                half glint = step(_GlintThreshold, facing) * (1.0 - foam);
                color += _SkyColor.rgb * (0.2 + fresnel + 0.25 * sheen) * (1.0 - foam) + _GlintColor.rgb * glint * lerp(0.35, 1.0, lit);

            #if defined(_ADDITIONAL_LIGHTS)
                // Lantern reflections: a glint on the water close to each light.
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalizedScreenSpaceUV = screenUV;
                uint lightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(lightCount)
                    Light lamp = GetAdditionalLight(lightIndex, input.positionWS, half4(1, 1, 1, 1));
                    half atten = saturate(lamp.distanceAttenuation);
                    half pool = atten * atten; // a soft pool close to the light
                    // A streak of its light across the ripples (stepped, like the rest of the shading).
                    half streak = pow(saturate(dot(reflected, lamp.direction)), _LampShininess);
                    streak = smoothstep(0.25, 0.45, streak) * saturate(atten * 4.0) * (1.0 - foam);
                    color += lamp.color * (pool * _LampReflection * (0.35 + 0.65 * albedo) + streak * _LampSpecular * saturate(atten * 2.0 + 0.1));
                LIGHT_LOOP_END
            #endif

                return half4(MixFog(color, input.fogFactor), 1.0);
            }
            ENDHLSL
        }
    }
}
