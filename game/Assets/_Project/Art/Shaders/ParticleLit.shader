// Alpha-blended particle, lit: texture alpha x vertex colour, lit by the moon (no shadows), the ambient and every
// lantern in reach (stepped pools, as the Toon shader's), with fog. For things that aren't light themselves: falling
// leaves, low mist. (MyGame/Particle is additive: sparks, embers, motes that glow.)
Shader "MyGame/ParticleLit"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Ambient("Ambient", Range(0, 2)) = 1
        _MoonStrength("Moon", Range(0, 2)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        Pass
        {
            Name "ParticleLit"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half _Ambient;
                half _MoonStrength;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half fogFactor : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.color = input.color;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half mask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                half3 albedo = input.color.rgb;
                Light moon = GetMainLight();
                half3 light = SampleSH(half3(0, 1, 0)) * _Ambient + moon.color * _MoonStrength * 0.6;
            #if defined(_ADDITIONAL_LIGHTS)
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                uint lightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(lightCount)
                    Light lamp = GetAdditionalLight(lightIndex, input.positionWS, half4(1, 1, 1, 1));
                    half atten = lamp.distanceAttenuation;
                    light += lamp.color * (smoothstep(0.03, 0.1, atten) * 0.55 + saturate(atten) * 0.45);
                LIGHT_LOOP_END
            #endif
                half3 colour = MixFog(albedo * light, input.fogFactor);
                return half4(colour, mask * input.color.a);
            }
            ENDHLSL
        }
    }
}
