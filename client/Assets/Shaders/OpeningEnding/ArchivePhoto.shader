Shader "CNN/ArchivePhoto"
{
    Properties
    {
        _MainTex ("Archive / reconstruction", 2D) = "black" {}
        _Mono ("Monochrome", Range(0,1)) = 0
        _Brightness ("Brightness", Range(0,2)) = 1
        _PeopleMotionTime ("Post office motion time", Float) = 0
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
            float4 _MainTex_ST; float _Mono; float _Brightness; float _PeopleMotionTime;
            CBUFFER_END
            Varyings vert(Attributes input)
            {
                Varyings output; UNITY_SETUP_INSTANCE_ID(input); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz); output.uv=input.uv; return output;
            }
            float2 ShoulderMotion(float2 uv, float2 center, float2 size, float phase)
            {
                float2 delta = (uv - center) / size;
                float weight = 1.0 - smoothstep(.1, 1.0, dot(delta, delta));
                float angle = _PeopleMotionTime * 1.75 + phase;
                return weight * float2(.0024 * sin(angle), .0018 * sin(angle * .83));
            }

            half4 frag(Varyings input):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 sourceUV = input.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                if (_PeopleMotionTime > 0)
                {
                    // Local movement on people in the archival photograph. Keep the room still.
                    float2 movement = ShoulderMotion(sourceUV, float2(.430, .595), float2(.041, .123), .8)
                                    + ShoulderMotion(sourceUV, float2(.540, .609), float2(.035, .108), 2.2)
                                    + ShoulderMotion(sourceUV, float2(.695, .624), float2(.046, .115), 1.5)
                                    + ShoulderMotion(sourceUV, float2(.837, .614), float2(.034, .092), 3.1);
                    sourceUV += movement * smoothstep(0.0, .65, _PeopleMotionTime);
                }
                half3 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sourceUV).rgb;
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
