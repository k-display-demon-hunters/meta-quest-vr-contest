Shader "CNN/CNNScreenLab/Magnifier Pedestal Glow"
{
    Properties
    {
        [HDR] _EmissionColor ("Glow color and strength", Color) = (0.1, 0.65, 0.92, 0.19)
        [Toggle] _FadeHeight ("Fade toward top", Float) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" }
        Pass
        {
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend One One
            ColorMask RGB
            Cull Back
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _EmissionColor;
                half _FadeHeight;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half height : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS);
                // Unity's cylinder runs from -1 to +1 along its local Y axis.
                output.height = saturate(input.positionOS.y * 0.5 + 0.5);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half remaining = 1.0h - input.height;
                half fade = lerp(1.0h, remaining * remaining, _FadeHeight);
                return half4(_EmissionColor.rgb * (_EmissionColor.a * fade), 0.0h);
            }
            ENDHLSL
        }
    }
}
