// Stylised flame: an emissive, unlit low-poly flame that sways, stretches and flickers, more towards its tip, and
// shades from a hot core colour at the base to a dark red tip. Used for the torch flames (ModelImportRules assigns it
// to materials named Torch*_Glow). Each torch gets its own phase from its world position.
Shader "MyGame/Flame"
{
    Properties
    {
        [HDR] _EmissionColor("Core Colour", Color) = (5, 1.6, 0.3, 1)
        [HDR] _TipColor("Tip Colour", Color) = (2.2, 0.35, 0.08, 1)
        _FlameBase("Flame Base Height (object space)", Float) = 1.72
        _FlameHeight("Flame Height", Float) = 0.5
        _Sway("Sway (m)", Float) = 0.07
        _Stretch("Stretch", Float) = 0.22
        _Speed("Speed", Float) = 6
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "Flame"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _EmissionColor;
                half4 _TipColor;
                float _FlameBase;
                float _FlameHeight;
                float _Sway;
                float _Stretch;
                float _Speed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half height : TEXCOORD0;
                half flicker : TEXCOORD1;
                half fogFactor : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 origin = TransformObjectToWorld(float3(0, 0, 0));
                float seed = origin.x * 1.7 + origin.z * 3.1;
                float t = _Time.y * _Speed + seed;
                float3 p = input.positionOS.xyz;
                float h = saturate((p.y - _FlameBase) / _FlameHeight); // 0 at the basket, 1 at the tip
                // Stretch up and down from the base, sway more the higher the vertex (two out-of-step waves each way).
                float stretch = 1.0 + _Stretch * (sin(t * 1.7) * 0.6 + sin(t * 2.9 + 1.3) * 0.4);
                p.y = _FlameBase + (p.y - _FlameBase) * stretch;
                float swayX = sin(t + p.y * 7.0) * 0.6 + sin(t * 2.3 + p.y * 13.0) * 0.4;
                float swayZ = sin(t * 1.3 + p.y * 9.0 + 2.0) * 0.6 + sin(t * 2.7 + p.y * 11.0) * 0.4;
                p.xz += float2(swayX, swayZ) * _Sway * h * h;
                output.positionCS = TransformObjectToHClip(p);
                output.height = h;
                output.flicker = 0.85 + 0.15 * sin(t * 4.1) * sin(t * 2.3 + 0.7);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 colour = lerp(_EmissionColor.rgb, _TipColor.rgb, smoothstep(0.1, 0.9, input.height)) * input.flicker;
                return half4(MixFog(colour, input.fogFactor), 1.0);
            }
            ENDHLSL
        }
    }
}
