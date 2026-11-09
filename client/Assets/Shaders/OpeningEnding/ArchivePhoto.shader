Shader "CNN/ArchivePhoto"
{
    Properties
    {
        _MainTex ("Archive / reconstruction", 2D) = "black" {}
        _Mono ("Monochrome", Range(0,1)) = 0
        _Brightness ("Brightness", Range(0,2)) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST; float _Mono; float _Brightness;
            CBUFFER_END
            Varyings vert(Attributes input)
            {
                Varyings output; UNITY_SETUP_INSTANCE_ID(input); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz); output.uv=input.uv; return output;
            }
            half4 frag(Varyings input):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half3 col=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv*_MainTex_ST.xy+_MainTex_ST.zw).rgb;
                half lum=dot(col,half3(.2126,.7152,.0722));
                col=lerp(col,lum*half3(1,.96,.87),_Mono);
                float grain=frac(sin(dot(input.uv+floor(_Time.y*18),float2(12.9898,78.233)))*43758.5453)-.5;
                float vignette=1-.22*dot(input.uv-.5,input.uv-.5);
                return half4(max(0,col*_Brightness*vignette+grain*.018),1);
            }
            ENDHLSL
        }
    }
}
