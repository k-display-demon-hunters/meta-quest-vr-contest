Shader "CNN/Workbench Hologram Backdrop"
{
    Properties
    {
        _Tint ("Glass tint and opacity", Color) = (0.023, 0.049, 0.081, 0.84)
        _Accent ("Teal glow", Color) = (0.16, 0.91, 0.79, 1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-50" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half4 _Accent;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.uv;
                float2 edge = min(uv, 1.0 - uv);
                float edgeDistance = min(edge.x * 3.1, edge.y);
                float rim = 1.0 - smoothstep(0.003, 0.007, edgeDistance);
                float halo = 1.0 - smoothstep(0.005, 0.047, edgeDistance);
                float scan = 0.5 + 0.5 * sin((uv.y * 39.0 - _Time.y * 0.22) * 6.283185);
                float sweep = 1.0 - smoothstep(0.008, 0.028, abs(uv.y - frac(_Time.y * 0.10)));
                float2 grainCell = floor(uv * float2(530.0, 200.0));
                float grain = frac(dot(grainCell, float2(0.06711056, 0.00583715)) * 52.9829189);
                half3 face = _Tint.rgb + (grain - 0.5) * 0.012;
                face += _Accent.rgb * (0.018 * scan + 0.033 * sweep + 0.16 * halo);
                face = lerp(face, _Accent.rgb, rim * 0.84);
                return half4(face, lerp(_Tint.a, 0.98, rim));
            }
            ENDHLSL
        }
    }
}
