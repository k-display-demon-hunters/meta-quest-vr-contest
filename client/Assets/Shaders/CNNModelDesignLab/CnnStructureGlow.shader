Shader "CNN/CNNModelDesignLab/Structure Glow"
{
    Properties
    {
        _Opacity ("Opacity", Range(0, 1)) = 1
        _Elapsed ("Rise time", Float) = 0
        _Reveal ("Revealed connections", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" "DisableBatching" = "True" }

        Pass
        {
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend One OneMinusSrcAlpha
            ColorMask RGB
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Opacity;
                float _Elapsed;
                float _Reveal;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                half4 color : COLOR;
                float3 corner : TEXCOORD0;
                float4 originAndDelay : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                half2 corner : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 position = input.positionOS;
                half visibility = 1.0h;

                if (input.corner.z > 0.0)
                {
                    float age = _Elapsed - input.originAndDelay.w;
                    float t = saturate(age);
                    float ease = 1.0 - (1.0 - t) * (1.0 - t) * (1.0 - t);
                    position = lerp(input.originAndDelay.xyz, position, ease);
                    position.y += 0.17 * sin(PI * t);
                    visibility = saturate(age * 10.0);
                }
                else
                {
                    visibility = saturate(_Reveal - input.originAndDelay.w);
                }

                // Per-eye view space keeps each sprite facing the viewer in stereo.
                float3 view = TransformWorldToView(TransformObjectToWorld(position));
                view.xy += input.corner.xy * input.corner.z;
                output.positionCS = mul(UNITY_MATRIX_P, float4(view, 1.0));
                output.corner = input.corner.xy;
                output.color = half4(input.color.rgb, input.color.a * visibility);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half falloff = saturate(1.0h - length(input.corner));
                half alpha = input.color.a * smoothstep(0.0h, 0.65h, falloff) * _Opacity;
                return half4(input.color.rgb * alpha, alpha);
            }
            ENDHLSL
        }
    }
}
