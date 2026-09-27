// Stylised water for URP: gentle vertex waves, shallow-to-deep colour and a stepped foam band where the water
// meets geometry (from the camera depth texture), toon main light with shadows, lantern light pools, fog.
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
        _LampReflection("Lamp Reflection", Range(0, 3)) = 0.6
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

                float3 normalWS = normalize(input.normalWS);
                Light light = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half lit = smoothstep(0.35, 0.65, light.shadowAttenuation) * smoothstep(0.0, 0.1, dot(normalWS, light.direction));
                half3 albedo = lerp(water, _FoamColor.rgb, foam);
                half3 color = albedo * lerp(_ShadowTint.rgb, 1.0, lit) * light.color * 0.85 + albedo * SampleSH(normalWS) * 0.35;

            #if defined(_ADDITIONAL_LIGHTS)
                // Lantern reflections: bright stepped pools on the water around each light.
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalizedScreenSpaceUV = screenUV;
                uint lightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(lightCount)
                    Light lamp = GetAdditionalLight(lightIndex, input.positionWS, half4(1, 1, 1, 1));
                    half atten = lamp.distanceAttenuation;
                    half pool = smoothstep(0.04, 0.09, atten) * 0.6 + saturate(atten) * 0.4;
                    color += lamp.color * pool * _LampReflection * (0.35 + 0.65 * albedo);
                LIGHT_LOOP_END
            #endif

                return half4(MixFog(color, input.fogFactor), 1.0);
            }
            ENDHLSL
        }
    }
}
