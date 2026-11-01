// Toon shader for URP (the style locked in the art test, see docs/brief.md).
// Stepped main-light shading with tinted shadows, stepped pools from point lights (Forward+), a soft rim,
// emission, fog, a hard-edged glint for metal (_Specular: from the main light and every lantern, so metal catches the
// lamps it passes), and an inverted-hull outline. The outline pushes vertices along a SMOOTHED normal that
// ModelImportRules bakes into the mesh tangents, so faceted meshes still get a closed outline and skinned
// meshes keep it while animating. Meshes without baked tangents (e.g. Unity primitives) get no outline.
// _Dissolve (0..1, set per renderer with a MaterialPropertyBlock) eats the surface away in a noise pattern with a
// glowing edge, in every pass, so shadow, outline and depth vanish with it (a defeated Soured).
// _Cutaway (1 on architecture: set on import) opens a soft-edged hole in a surface that stands between the camera and
// the Apprentice (_CutawayCentre, set every frame by Cutaway.cs: xyz their chest, w the hole's radius), so walls,
// houses and floors never hide them. Only surfaces a metre or more nearer the camera than them, and above their feet.
Shader "MyGame/Toon"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _ShadowTint("Shadow Tint", Color) = (0.74, 0.71, 0.88, 1)
        _ShadeThreshold("Shade Threshold", Range(-1, 1)) = 0.05
        _ShadeSoftness("Shade Softness", Range(0.001, 0.5)) = 0.03
        _AmbientStrength("Ambient Strength", Range(0, 1)) = 0.5
        _RimColor("Rim Color", Color) = (0.8, 0.85, 1, 1)
        _RimStrength("Rim Strength", Range(0, 1)) = 0.12
        _Specular("Specular (metal)", Range(0, 2)) = 0
        _SpecularSize("Specular Size", Range(0.005, 0.5)) = 0.08
        [HDR] _EmissionColor("Emission", Color) = (0, 0, 0, 1)
        _OutlineColor("Outline Color", Color) = (0.07, 0.06, 0.09, 1)
        _OutlineWidth("Outline Width (px)", Range(0, 6)) = 1.6
        _Dissolve("Dissolve", Range(0, 1)) = 0
        [HDR] _DissolveEdgeColor("Dissolve Edge", Color) = (1.4, 3.0, 0.8, 1)
        _DissolveScale("Dissolve Noise Scale", Float) = 7
        _Cutaway("Cutaway (opens between camera and Apprentice)", Float) = 0
        _InTime("In time (never stilled: the Apprentice's)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 _ShadowTint;
            half _ShadeThreshold;
            half _ShadeSoftness;
            half _AmbientStrength;
            half4 _RimColor;
            half _RimStrength;
            half _Specular;
            half _SpecularSize;
            half4 _EmissionColor;
            half4 _OutlineColor;
            float _OutlineWidth;
            half _Dissolve;
            half4 _DissolveEdgeColor;
            float _DissolveScale;
            half _Cutaway;
            half _InTime;
        CBUFFER_END

        // The Hall of the Last Hour with its time stopped (HallTime): what isn't in time is drained to grey, all of its
        // shading (its colour, the light on it, the fog), only its glow keeping its colour (the Soured's eyes green, a
        // lamp's glass). Set going again, the colour comes back in a wave out from its engine. Outside the Hall these
        // are all 0: no effect.
        half _HallStill;      // 0..1, everywhere
        half _HallHeld;       // 0..1: what's in time (the Apprentice) stopped too, a moment (the Hour's doing)
        float4 _HallWave;     // xyz the wave's centre, w its radius (colour back within it)
        half _HallWaveOn;
        half _HallWaveWidth;
        half4 _HallStillTint;
        float4 _HallPockets[4]; // xyz a pocket of time's centre, w its reach (TimePocket)
        half _HallPocketCount;

        half HallStillness(float3 positionWS)
        {
            half s = _HallStill;
            if (_HallWaveOn > 0.5)
            {
                float d = distance(positionWS.xz, _HallWave.xz);
                s = max(s, smoothstep(_HallWave.w - _HallWaveWidth, _HallWave.w, d));
            }
            // Pockets of time round machines set going (TimePocket): colour back within each, a storey up and down.
            for (int i = 0; i < 4; i++)
            {
                if (i >= (int)_HallPocketCount)
                    break;
                float4 p = _HallPockets[i];
                float r = distance(positionWS.xz, p.xz);
                half outside = max(smoothstep(p.w - 0.9, p.w, r), smoothstep(3.5, 4.0, abs(positionWS.y - p.y)));
                s = min(s, outside);
            }
            return s;
        }

        half _HallStillContrast; // (the grey a little harder: an old photograph)

        half3 Stilled(half3 colour, half still)
        {
            half l = dot(colour, half3(0.3, 0.59, 0.11));
            l = max(0.0, 0.18 + (l - 0.18) * _HallStillContrast);
            return lerp(colour, l * _HallStillTint.rgb, still);
        }

        float4 _CutawayCentre;

        float DissolveHash(float3 p)
        {
            p = frac(p * 0.3183099 + 0.1);
            p *= 17.0;
            return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
        }

        // Smooth 3D value noise in 0..1.
        float DissolveNoise(float3 x)
        {
            float3 i = floor(x);
            float3 f = frac(x);
            f = f * f * (3.0 - 2.0 * f);
            return lerp(lerp(lerp(DissolveHash(i + float3(0, 0, 0)), DissolveHash(i + float3(1, 0, 0)), f.x),
                             lerp(DissolveHash(i + float3(0, 1, 0)), DissolveHash(i + float3(1, 1, 0)), f.x), f.y),
                        lerp(lerp(DissolveHash(i + float3(0, 0, 1)), DissolveHash(i + float3(1, 0, 1)), f.x),
                             lerp(DissolveHash(i + float3(0, 1, 1)), DissolveHash(i + float3(1, 1, 1)), f.x), f.y), f.z);
        }

        // Discards dissolved fragments; returns 0..1 how close this fragment is to the burning edge.
        half DissolveClip(float3 positionWS)
        {
            if (_Dissolve <= 0.0)
                return 0.0;
            float n = DissolveNoise(positionWS * _DissolveScale) * 0.75 + DissolveNoise(positionWS * _DissolveScale * 3.1) * 0.25;
            clip(n - _Dissolve);
            return 1.0 - saturate((n - _Dissolve) / 0.08);
        }

        // Discards this fragment if it's architecture standing between the camera and the Apprentice. `grow` widens the
        // hole (the outline pass: its hull stands proud of the surface, so it's cut a little wider, leaving no scraps).
        void CutawayClip(float3 positionWS, float grow = 0.0)
        {
            if (_Cutaway < 0.5 || _CutawayCentre.w <= 0.0)
                return;
            float3 toCentre = _CutawayCentre.xyz - _WorldSpaceCameraPos;
            float reach = length(toCentre);
            float3 dir = toCentre / reach;
            float3 toPixel = positionWS - _WorldSpaceCameraPos;
            float along = dot(toPixel, dir);
            if (along > reach - 1.0 || positionWS.y < _CutawayCentre.y - 0.4)
                return; // behind them, beside them, or at their feet (the floor they stand on)
            float away = length(toPixel - dir * along);
            float n = DissolveNoise(positionWS * 1.6);
            clip(away - _CutawayCentre.w * (0.94 + 0.12 * n) - grow); // (a softly uneven edge, not a perfect circle)
        }
        ENDHLSL

        Pass
        {
            Name "ToonForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                half fogFactor : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half edge = DissolveClip(input.positionWS);
                CutawayClip(input.positionWS);
                float3 normalWS = normalize(input.normalWS);
                Light light = GetMainLight(TransformWorldToShadowCoord(input.positionWS));

                half ndotl = dot(normalWS, light.direction);
                half lit = smoothstep(_ShadeThreshold - _ShadeSoftness, _ShadeThreshold + _ShadeSoftness, ndotl);
                lit *= smoothstep(0.35, 0.65, light.shadowAttenuation); // shadows get the same hard edge

                half still = max(HallStillness(input.positionWS) * (1.0 - _InTime), _HallHeld * _InTime);
                half3 albedo = _BaseColor.rgb;
                half3 direct = albedo * lerp(_ShadowTint.rgb, 1.0, lit) * light.color;
                half3 ambient = albedo * SampleSH(normalWS) * _AmbientStrength;
                half3 viewDir = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half rim = pow(1.0 - saturate(dot(normalWS, viewDir)), 4.0) * _RimStrength * lit;

                half3 color = direct + ambient + rim * _RimColor.rgb; // (its glow added at the end: kept in colour)
                // Metal: a hard-edged glint, tinted by the metal (gold glints gold), and a darker body between.
                half3 glintTint = lerp(albedo, 1.0, 0.45);
                half glintEdge = 1.0 - _SpecularSize;
                if (_Specular > 0.0)
                {
                    half nh = dot(normalWS, normalize(light.direction + viewDir));
                    color += glintTint * light.color * _Specular * lit * smoothstep(glintEdge - 0.01, glintEdge + 0.01, nh);
                    // (and a band from the sky above, so it reads as metal even away from any light)
                    color += albedo * _Specular * 0.3 * smoothstep(0.6, 0.66, normalWS.y * 0.5 + 0.5);
                }

            #if defined(_ADDITIONAL_LIGHTS)
                // Lantern light: a soft-edged pool rather than a smooth falloff, to match the stepped look.
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                uint lightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(lightCount)
                    Light lamp = GetAdditionalLight(lightIndex, input.positionWS, half4(1, 1, 1, 1));
                    half facing = smoothstep(0.0, 0.1, dot(normalWS, lamp.direction));
                    half atten = lamp.distanceAttenuation * lamp.shadowAttenuation;
                    half pool = smoothstep(0.03, 0.1, atten) * 0.55 + saturate(atten) * 0.45;
                    color += albedo * lamp.color * facing * pool;
                    if (_Specular > 0.0)
                    {
                        half lh = dot(normalWS, normalize(lamp.direction + viewDir));
                        color += glintTint * lamp.color * _Specular * saturate(atten * 2.0) * facing * smoothstep(glintEdge - 0.01, glintEdge + 0.01, lh);
                    }
                LIGHT_LOOP_END
            #endif

                // Its glow (and a dissolve's edge) kept apart: time stopped drains the rest, fog and all, to grey.
                half3 glow = lerp(_EmissionColor.rgb, _DissolveEdgeColor.rgb, edge);
                color *= 1.0 - edge;
                half3 bare = MixFog(color, input.fogFactor);
                half3 glowing = MixFog(color + glow, input.fogFactor);
                return half4(Stilled(bare, still) + (glowing - bare), 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 tangentOS : TANGENT; // smoothed normal, baked on import
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD1;
                half fogFactor : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float4 positionCS = TransformObjectToHClip(input.positionOS.xyz);
                float3 smoothNormal = input.tangentOS.xyz;
                if (dot(smoothNormal, smoothNormal) > 1e-6)
                {
                    float3 normalCS = TransformWorldToHClipDir(TransformObjectToWorldNormal(smoothNormal));
                    float2 dir = normalCS.xy;
                    float len = length(dir);
                    if (len > 1e-5)
                        positionCS.xy += (dir / len) * (2.0 / _ScreenParams.xy) * _OutlineWidth * positionCS.w;
                }
                output.positionCS = positionCS;
                output.fogFactor = ComputeFogFactor(positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                DissolveClip(input.positionWS);
                CutawayClip(input.positionWS, 0.12);
                return half4(MixFog(_OutlineColor.rgb, input.fogFactor), 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirection = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirection = _LightDirection;
            #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirection));
            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                output.positionCS = positionCS;
                output.positionWS = positionWS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                DissolveClip(input.positionWS);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                DissolveClip(input.positionWS);
                CutawayClip(input.positionWS);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                DissolveClip(input.positionWS);
                CutawayClip(input.positionWS);
                return half4(normalize(input.normalWS), 0.0);
            }
            ENDHLSL
        }
    }
}
