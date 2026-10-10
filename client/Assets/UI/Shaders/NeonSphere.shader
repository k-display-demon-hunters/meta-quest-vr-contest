Shader "Custom/NeonSphere" {
Properties { _BaseColor("Body",Color)=(0.035,0.35,0.85,1) _RimColor("Rim",Color)=(0.15,1.8,3.5,1) }
SubShader {
Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
Pass {
HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
struct Varyings { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; float3 positionWS:TEXCOORD1; };
CBUFFER_START(UnityPerMaterial)
float4 _BaseColor;
float4 _RimColor;
CBUFFER_END
Varyings vert(Attributes a) { Varyings o; o.positionWS=TransformObjectToWorld(a.positionOS.xyz); o.positionCS=TransformWorldToHClip(o.positionWS); o.normalWS=TransformObjectToWorldNormal(a.normalOS); return o; }
half4 frag(Varyings i):SV_Target {
float3 n=normalize(i.normalWS);float3 v=normalize(GetWorldSpaceViewDir(i.positionWS));
float edge=1-saturate(dot(n,v));
float lighting=0.65+0.35*saturate(dot(n,normalize(float3(-0.5,0.7,-1))));
float3 c=_BaseColor.rgb*lighting;
c=lerp(c, _RimColor.rgb, smoothstep(0.35,0.92,edge));
float highlight=pow(saturate(dot(n,normalize(float3(-0.4,0.65,-1)))),12);
c+=float3(.12,.14,.16)*highlight*(1-smoothstep(0.35,0.92,edge));
return half4(c,1);
}
ENDHLSL
}
}
}