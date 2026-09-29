// A swing's smear, drawn like a cel animation's smear frame rather than a blurred streak: an unlit crescent whose
// fade (vertex alpha: newest samples opaque, oldest clear; and uv.y: bright at the weapon's tip edge, clear at its
// inner edge) is stepped into a few hard-edged tones. Used by SwingTrail.
Shader "MyGame/Swoosh"
{
    Properties
    {
        _Color("Colour", Color) = (1, 1, 1, 1)
        _Steps("Tones", Float) = 3
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        Pass
        {
            Name "Swoosh"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Steps;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; half fogFactor : TEXCOORD1; };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Strongest right at the blade: falls off quickly in from the tip's edge (squared), and with age along
                // its length (the vertex alpha).
                half across = input.uv.y * input.uv.y;
                half a = input.color.a * across;
                half stepped = floor(saturate(a) * _Steps + 0.35) / _Steps; // hard-edged tones, not a gradient
                clip(stepped - 0.01);
                half3 colour = MixFog(_Color.rgb * input.color.rgb, input.fogFactor);
                return half4(colour, stepped * _Color.a);
            }
            ENDHLSL
        }
    }
}
