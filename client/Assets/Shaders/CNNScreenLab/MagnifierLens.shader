Shader "CNN/CNNScreenLab/Magnifier Lens"
{
    Properties
    {
        _Flash ("Pickup flash", Range(0,1)) = 0
        _Sweep ("Pickup sweep progress", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent+12" "RenderType" = "Transparent" }
        Pass
        {
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _Flash;
                half _Sweep;
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
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = TransformObjectToWorldNormal(float3(0, 0, 1));
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 p = input.uv * 2.0 - 1.0;
                float radius = length(p);
                float edgeWidth = max(fwidth(radius), 0.003);
                half disk = 1.0 - smoothstep(0.982 - edgeWidth, 0.982, radius);
                clip(disk - 0.001);

                half facing = saturate(abs(dot(normalize(input.normalWS), GetWorldSpaceNormalizeViewDir(input.positionWS))));
                half grazing = 1.0h - facing;
                half fresnel = grazing * grazing * grazing * grazing;
                float reflection = abs(dot(p, normalize(float2(0.65, 1.0))) - 0.36);
                half glint = (1.0 - smoothstep(0.015, 0.14, reflection)) * (1.0 - smoothstep(0.72, 0.93, radius));
                half3 glassColor = lerp(half3(0.38, 0.65, 0.78), half3(0.78, 0.94, 1.0), glint);
                half glassAlpha = 0.045h + fresnel * 0.10h + glint * 0.16h;

                float rimDistance = abs(radius - 0.935);
                half rimLine = 1.0 - smoothstep(0.011, 0.011 + edgeWidth, rimDistance);
                half halo = 1.0 - smoothstep(0.014, 0.058, rimDistance);
                float angle = atan2(p.y, p.x + 0.00001);
                half sweep = pow(saturate(0.5 + 0.5 * cos(angle - (_Sweep * TWO_PI - 1.1))), 10.0);
                half flash = _Flash * (0.45h + 0.55h * sweep);
                half3 cyan = lerp(half3(0.12, 0.77, 1.07), half3(0.78, 1.38, 1.65), flash);
                half rim = saturate(rimLine + halo * (0.20h + flash * 0.57h));
                half alpha = saturate(glassAlpha * (1.0h - rimLine) + rimLine * 0.90h + halo * flash * 0.30h);
                return half4(lerp(glassColor, cyan, rim), alpha * disk);
            }
            ENDHLSL
        }
    }
}
