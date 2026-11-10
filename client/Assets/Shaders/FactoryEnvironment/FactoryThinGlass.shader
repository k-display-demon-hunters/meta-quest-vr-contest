Shader "Factory/Thin Refractive Glass"
{
    Properties
    {
        _Tint("Transmission tint", Color) = (0.92,0.97,1,1)
        _IOR("Index of refraction", Range(1,2)) = 1.5
        _Thickness("Optical thickness (m)", Range(0,0.05)) = 0.008
        _Roughness("Surface roughness", Range(0,1)) = 0.12
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "Glass"
            Tags { "LightMode"="UniversalForward" }
            ZWrite Off
            Cull Back
            Blend One Zero
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile _ _REFLECTION_PROBE_BOX_PROJECTION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                float _IOR;
                float _Thickness;
                half _Roughness;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half3 normal = normalize(input.normalWS);
                half3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float facing = saturate(dot(normal, view));
                float f0 = (_IOR - 1) / (_IOR + 1);
                f0 *= f0;
                float fresnel = f0 + (1 - f0) * pow(1 - facing, 5);
                // Snell refraction through a thin pane; screen-space transmission is an approximation.
                float3 transmitted = refract(-view, normal, rcp(_IOR));
                float3 exitPosition = input.positionWS + transmitted * (_Thickness / max(.15, abs(dot(transmitted, normal))));
                float4 exitClip = TransformWorldToHClip(exitPosition);
                float4 exitScreen = ComputeScreenPos(exitClip);
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float2 refractedUV = exitScreen.xy / exitScreen.w;
                half3 background = SampleSceneColor(saturate(refractedUV));
                half3 reflection = GlossyEnvironmentReflection(reflect(-view, normal), input.positionWS, _Roughness, 1, screenUV);
                half3 transmissionTint = pow(max(_Tint.rgb, half3(.01,.01,.01)), rcp(max(facing,.2)));
                return half4(lerp(background * transmissionTint, reflection, fresnel), 1);
            }
            ENDHLSL
        }
    }
}
