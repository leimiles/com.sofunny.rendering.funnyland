#ifndef FUNNY_LIT_INPUT_INCLUDED
#define FUNNY_LIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Funnyland/Libs/FunnySurfaceData.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;
    half4 _EmissionColor;
    half _Cutoff;
    half _Surface;
    half _MetallicOffset;
    half _RoughnessOffset;
    half _BumpScale;
CBUFFER_END

#ifdef UNITY_DOTS_INSTANCING_ENABLED
    UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
        UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
        UNITY_DOTS_INSTANCED_PROP(float4, _EmissionColor)
        UNITY_DOTS_INSTANCED_PROP(float , _Cutoff)
        UNITY_DOTS_INSTANCED_PROP(float , _Surface)
        UNITY_DOTS_INSTANCED_PROP(float , _MetallicOffset)
        UNITY_DOTS_INSTANCED_PROP(float , _RoughnessOffset)
        UNITY_DOTS_INSTANCED_PROP(float , _BumpScale)
    UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

    #define _BaseColor          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4 , _BaseColor)
    #define _EmissionColor      UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4 , _EmissionColor)
    #define _Cutoff             UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _Cutoff)
    #define _Surface            UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _Surface)
    #define _MetallicOffset     UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _MetallicOffset)
    #define _RoughnessOffset    UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _RoughnessOffset)
    #define _BumpScale          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _BumpScale)
#endif

TEXTURE2D(_MixMap);   SAMPLER(sampler_MixMap);

inline void InitializeSimpleLitSurfaceData(float2 uv, out FunnySurfaceData outSurfaceData)
{
    outSurfaceData = (FunnySurfaceData)0;

    half4 albedoAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    outSurfaceData.alpha = albedoAlpha.a * _BaseColor.a;
    outSurfaceData.alpha = AlphaDiscard(outSurfaceData.alpha, _Cutoff);

    half4 mixMap = SAMPLE_TEXTURE2D(_MixMap, sampler_MixMap, uv);
    mixMap.r = saturate(mixMap.r + _MetallicOffset);
    mixMap.b = saturate(mixMap.b + _RoughnessOffset);
    
    outSurfaceData.metallic = mixMap.r;

    outSurfaceData.albedo = saturate((1 - outSurfaceData.metallic) * 0.96) * albedoAlpha.rgb * _BaseColor.rgb;
    outSurfaceData.albedo = AlphaModulate(outSurfaceData.albedo, outSurfaceData.alpha);
    
    outSurfaceData.specular = lerp(0.04, (albedoAlpha.rgb * _BaseColor.rgb), outSurfaceData.metallic);
    outSurfaceData.smoothness = (1 - mixMap.b);
    
    outSurfaceData.normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
    outSurfaceData.occlusion = mixMap.g;
    outSurfaceData.indirectSpecularOcclusion = 1.0;
    outSurfaceData.emission = SampleEmission(uv, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));
}

#endif
