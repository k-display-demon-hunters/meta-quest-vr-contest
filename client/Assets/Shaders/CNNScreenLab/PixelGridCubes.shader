Shader "CNN/CNNScreenLab/Pixel Grid Cubes"
{
    Properties
    {
        _PixelColor ("Pixel color and selection weight", Vector) = (1,1,1,0)
        _Coverage ("Reveal coverage", Range(0,1)) = 1
        [HDR] _HighlightColor ("Selected edge", Color) = (0.28,0.9,1,1)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
        Pass
        {
            Tags { "LightMode" = "SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma instancing_options forcemaxcount:250
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _Coverage;
                half4 _HighlightColor;
            CBUFFER_END

            UNITY_INSTANCING_BUFFER_START(PixelProperties)
                UNITY_DEFINE_INSTANCED_PROP(float4, _PixelColor)
            UNITY_INSTANCING_BUFFER_END(PixelProperties)

            struct Attributes
            {
                float4 positionOS : POSITION;
                half3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 tint : TEXCOORD0;
                float2 localFace : TEXCOORD1;
                half front : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                // Keep the displayed RGB on the image-facing surface and shade only the sides.
                half shade = input.normalOS.z < -0.5h ? 1.0h : (input.normalOS.y > 0.5h ? 0.84h : 0.61h);
                half4 pixel = UNITY_ACCESS_INSTANCED_PROP(PixelProperties, _PixelColor);
                output.tint = half4(pixel.rgb * shade, pixel.a);
                output.localFace = input.positionOS.xy;
                output.front = input.normalOS.z < -0.5h ? 1.0h : 0.0h;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                if (_Coverage < 1.0h)
                {
                    // The screen-door reveal lets the fading source show through the cubes.
                    float coverageNoise = frac(52.9829189 * frac(dot(floor(input.positionCS.xy), float2(0.06711056, 0.00583715))));
                    clip(_Coverage - max(coverageNoise, 0.0001));
                }

                half3 color = input.tint.rgb * (1.0h + 0.35h * input.tint.a);
                float edge = max(abs(input.localFace.x), abs(input.localFace.y));
                float feather = max(fwidth(edge), 0.002);
                half edgeWeight = smoothstep(0.455 - feather, 0.455, edge);
                color = lerp(color, _HighlightColor.rgb, edgeWeight * input.tint.a * input.front);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }
    }
}
