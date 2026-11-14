Shader "Cinematic/SceneTransition"
{
 Properties { _Color("Color",Color)=(0.1,0.8,0.75,1) _Cull("Cull",Float)=2 }
 SubShader {
 Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+100" "RenderType"="Transparent" }
 Pass {
 Cull [_Cull] ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A { float4 positionOS:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
 struct V { float4 positionCS:SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };
 CBUFFER_START(UnityPerMaterial)
 float4 _Color;
 CBUFFER_END
 V vert(A i) { V o; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.positionCS=TransformObjectToHClip(i.positionOS.xyz); return o; }
 half4 frag(V i):SV_Target { UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); return _Color; }
 ENDHLSL
 }
 }
}
