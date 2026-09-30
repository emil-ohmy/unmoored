// An invisible lid over the inside of a hull, just above the waves: it writes depth only, after everything solid is
// drawn (so the boat, the Apprentice and a passenger all still show) and before the water, which then can't be seen
// inside the boat. Drawn in the transparent range, one queue step ahead of the water (Water.shader: Transparent-100).
Shader "MyGame/WaterMask"
{
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent-101" }

        Pass
        {
            Name "WaterMask"
            ColorMask 0
            ZWrite On
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return o;
            }

            half4 frag(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
