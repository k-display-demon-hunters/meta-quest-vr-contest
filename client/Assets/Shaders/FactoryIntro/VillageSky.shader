Shader "FactoryIntro/VillageSky"
{
    Properties
    {
        _Dusk ("Storm darkness", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Dusk;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 direction = normalize(input.direction);
                float height = saturate(direction.y);
                half3 horizon = lerp(half3(0.88, 0.81, 0.62), half3(0.18, 0.12, 0.27), _Dusk);
                half3 zenith = lerp(half3(0.19, 0.57, 0.77), half3(0.035, 0.025, 0.10), _Dusk);
                half3 color = lerp(horizon, zenith, pow(height, 0.45));
                color = lerp(color, horizon * 0.65, saturate(-direction.y * 3.0));
                float sunDistance = dot(direction, normalize(float3(-0.45, 0.6, 0.85)));
                float sun = smoothstep(0.9984, 0.9991, sunDistance);
                float glow = pow(saturate(sunDistance), 40.0) * 0.18;
                color += half3(1.0, 0.71, 0.35) * (sun + glow) * (1.0 - _Dusk);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
