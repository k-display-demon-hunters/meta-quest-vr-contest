// Adapted from URP Lit in com.unity.render-pipelines.universal.
// Copyright (c) 2020 Unity Technologies ApS.
// Licensed under the Unity Companion License: https://unity.com/legal/licenses/Unity_Companion_License

#include "FactoryCombinedSurfaceInput.hlsl"

#ifndef FACTORY_COMBINED_UNIVERSAL_LIT_META_PASS_INCLUDED
#define FACTORY_COMBINED_UNIVERSAL_LIT_META_PASS_INCLUDED

#ifndef UNIVERSAL_META_PASS_INCLUDED
#define UNIVERSAL_META_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

struct Attributes
{
    half4 surfaceColor : TEXCOORD3;
    half2 surfaceFactors : TEXCOORD4;
    half3 surfaceEmission : TEXCOORD5;
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float2 uv0          : TEXCOORD0;
    float2 uv1          : TEXCOORD1;
    float2 uv2          : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    half4 surfaceColor : TEXCOORD3;
    half2 surfaceFactors : TEXCOORD4;
    half3 surfaceEmission : TEXCOORD5;
    float4 positionCS   : SV_POSITION;
    float2 uv           : TEXCOORD0;
#ifdef EDITOR_VISUALIZATION
    float2 VizUV        : TEXCOORD1;
    float4 LightCoord   : TEXCOORD2;
#endif
};

Varyings UniversalVertexMeta(Attributes input)
{
    Varyings output = (Varyings)0;
    output.positionCS = UnityMetaVertexPosition(input.positionOS.xyz, input.uv1, input.uv2);
    output.uv = TRANSFORM_TEX(input.uv0, _BaseMap);
#ifdef EDITOR_VISUALIZATION
    UnityEditorVizData(input.positionOS.xyz, input.uv0, input.uv1, input.uv2, output.VizUV, output.LightCoord);
#endif
    output.surfaceColor = input.surfaceColor;
    output.surfaceFactors = input.surfaceFactors;
    output.surfaceEmission = input.surfaceEmission;
    return output;
}

half4 UniversalFragmentMeta(Varyings fragIn, MetaInput metaInput)
{
#ifdef EDITOR_VISUALIZATION
    metaInput.VizUV = fragIn.VizUV;
    metaInput.LightCoord = fragIn.LightCoord;
#endif

    return UnityMetaFragment(metaInput);
}
#endif

half4 UniversalFragmentMetaLit(Varyings input) : SV_Target
{
    SurfaceData surfaceData;
    InitializeFactorySurface(input.uv, input.surfaceColor, input.surfaceFactors, input.surfaceEmission, surfaceData);

    BRDFData brdfData;
    InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);

    MetaInput metaInput;
    metaInput.Albedo = brdfData.diffuse + brdfData.specular * brdfData.roughness * 0.5;
    metaInput.Emission = surfaceData.emission;
    return UniversalFragmentMeta(input, metaInput);
}
#endif
