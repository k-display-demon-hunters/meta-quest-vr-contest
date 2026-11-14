// Adapted from URP Lit in com.unity.render-pipelines.universal.
// Copyright (c) 2020 Unity Technologies ApS.
// Licensed under the Unity Companion License: https://unity.com/legal/licenses/Unity_Companion_License

#ifndef FACTORY_COMBINED_SURFACE_INPUT_INCLUDED
#define FACTORY_COMBINED_SURFACE_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"

half4 SampleFactoryMetallicSpecGloss(float2 uv, half albedoAlpha, half smoothness, half metallic)
{
    half4 specGloss;

#ifdef _METALLICSPECGLOSSMAP
    specGloss = half4(SAMPLE_METALLICSPECULAR(uv));
    #ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
        specGloss.a = albedoAlpha * smoothness;
    #else
        specGloss.a *= smoothness;
    #endif
#else // _METALLICSPECGLOSSMAP
    #if _SPECULAR_SETUP
        specGloss.rgb = _SpecColor.rgb;
    #else
        specGloss.rgb = metallic.rrr;
    #endif

    #ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
        specGloss.a = albedoAlpha * smoothness;
    #else
        specGloss.a = smoothness;
    #endif
#endif

    return specGloss;
}

inline void InitializeFactorySurface(float2 uv, half4 colorAndSmoothness, half2 metallicAndNormalScale, half3 emissionColor, out SurfaceData outSurfaceData)
{
    half4 albedoAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    outSurfaceData.alpha = Alpha(albedoAlpha.a, _BaseColor, _Cutoff);

    half4 specGloss = SampleFactoryMetallicSpecGloss(uv, albedoAlpha.a, colorAndSmoothness.a, metallicAndNormalScale.x);
    outSurfaceData.albedo = albedoAlpha.rgb * colorAndSmoothness.rgb;
    outSurfaceData.albedo = AlphaModulate(outSurfaceData.albedo, outSurfaceData.alpha);

#if _SPECULAR_SETUP
    outSurfaceData.metallic = half(1.0);
    outSurfaceData.specular = specGloss.rgb;
#else
    outSurfaceData.metallic = specGloss.r;
    outSurfaceData.specular = half3(0.0, 0.0, 0.0);
#endif

    outSurfaceData.smoothness = specGloss.a;
    outSurfaceData.normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), metallicAndNormalScale.y);
    outSurfaceData.occlusion = SampleOcclusion(uv);
    outSurfaceData.emission = SampleEmission(uv, emissionColor, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));

#if defined(_CLEARCOAT) || defined(_CLEARCOATMAP)
    half2 clearCoat = SampleClearCoat(uv);
    outSurfaceData.clearCoatMask       = clearCoat.r;
    outSurfaceData.clearCoatSmoothness = clearCoat.g;
#else
    outSurfaceData.clearCoatMask       = half(0.0);
    outSurfaceData.clearCoatSmoothness = half(0.0);
#endif

#if defined(_DETAIL)
    half detailMask = SAMPLE_TEXTURE2D(_DetailMask, sampler_DetailMask, uv).a;
    float2 detailUv = uv * _DetailAlbedoMap_ST.xy + _DetailAlbedoMap_ST.zw;
    outSurfaceData.albedo = ApplyDetailAlbedo(detailUv, outSurfaceData.albedo, detailMask);
    outSurfaceData.normalTS = ApplyDetailNormal(detailUv, outSurfaceData.normalTS, detailMask);
#endif
}

#endif
