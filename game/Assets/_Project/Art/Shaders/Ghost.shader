// Pale, transparent, unlit silhouette used for the dash's ghost images (they show the invincibility frames).
// Per-ghost fade comes from _Color's alpha (set with a MaterialPropertyBlock).
Shader "MyGame/Ghost"
{
    Properties
    {
        _Color("Color", Color) = (0.78, 0.9, 1.0, 0.5)
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        Pass
        {
            Name "Ghost"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha One // additive-ish glow so ghosts read against dark ground
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 positionWS : TEXCOORD1; half fogFactor : TEXCOORD2; };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Brighter at the silhouette edge, so a ghost reads as a spectral outline rather than a blob.
                half facing = saturate(dot(normalize(input.normalWS), GetWorldSpaceNormalizeViewDir(input.positionWS)));
                half alpha = _Color.a * lerp(1.0, 0.45, facing);
                return half4(MixFog(_Color.rgb, input.fogFactor), alpha);
            }
            ENDHLSL
        }
    }
}
