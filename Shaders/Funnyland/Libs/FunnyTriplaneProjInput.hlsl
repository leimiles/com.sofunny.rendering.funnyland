#ifndef FUNNY_TRIPLANEPROJ_INPUT_INCLUDED
#define FUNNY_TRIPLANEPROJ_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Funnyland/Libs/FunnySurfaceData.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _TillingAndOffset;
    half4 _BaseColor;
    half4 _EmissionColor;
    half _BlendRange;
    half _Cutoff;
    half _Surface;
    half _RoughnessHigh;
    half _RoughnessLow;
    half _BumpScale;
    half _IndirectSpecularOcclusion;
CBUFFER_END

#ifdef UNITY_DOTS_INSTANCING_ENABLED
    UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
        UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
        UNITY_DOTS_INSTANCED_PROP(float4, _EmissionColor)
        UNITY_DOTS_INSTANCED_PROP(float , _BlendRange)
        UNITY_DOTS_INSTANCED_PROP(float , _Cutoff)
        UNITY_DOTS_INSTANCED_PROP(float , _Surface)
        UNITY_DOTS_INSTANCED_PROP(float , _RoughnessHigh)
        UNITY_DOTS_INSTANCED_PROP(float , _RoughnessLow)
        UNITY_DOTS_INSTANCED_PROP(float , _BumpScale)
        UNITY_DOTS_INSTANCED_PROP(float , _IndirectSpecularOcclusion)
    UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

    #define _BaseColor                      UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4 , _BaseColor)
    #define _EmissionColor                  UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4 , _EmissionColor)
    #define _BlendRange                     UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _BlendRange)
    #define _Cutoff                         UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _Cutoff)
    #define _Surface                        UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _Surface)
    #define _RoughnessHigh                  UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _RoughnessHigh)
    #define _RoughnessLow                   UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _RoughnessLow)
    #define _BumpScale                      UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _BumpScale)
    #define _IndirectSpecularOcclusion      UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _IndirectSpecularOcclusion)
#endif

TEXTURE2D(_MixMap);   SAMPLER(sampler_MixMap);

half4 TextureBlend(Texture2D tex, sampler sampler_tex, float2 xy, float2 zy, float2 xz, half3 normalWS)
{
    half4 color_X = SAMPLE_TEXTURE2D(tex, sampler_tex, zy);
    half4 color_Y = SAMPLE_TEXTURE2D(tex, sampler_tex, xz);
    half4 color_Z = SAMPLE_TEXTURE2D(tex, sampler_tex, xy);

    half3 blend = pow(abs(normalWS), max(_BlendRange, 1.0));
    blend = blend / (blend.x + blend.y + blend.z);
    half4 finalColor = color_X * blend.x + color_Y * blend.y + color_Z * blend.z;

    return finalColor;
}

half3 NormalBlend(float2 xy, float2 zy, float2 xz, half3 normalWS, half scale)
{
#ifdef _NORMALMAP
    #if BUMP_SCALE_NOT_SUPPORTED
        half3 normal_X = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, zy));
        half3 normal_Y = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, xz));
        half3 normal_Z = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, xy));
    #else
        half3 normal_X = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, zy), scale);
        half3 normal_Y = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, xz), scale);
        half3 normal_Z = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, xy), scale);
    #endif

    normal_X = half3(normal_X.xy + normalWS.zy, abs(normal_X.z) * normalWS.x);
    normal_Y = half3(normal_Y.xy + normalWS.xz, abs(normal_Y.z) * normalWS.y);
    normal_Z = half3(normal_Z.xy + normalWS.xy, abs(normal_Z.z) * normalWS.z);

    half3 normalBlend = max(pow(abs(normalWS), _BlendRange), 0);
    normalBlend /= (normalBlend.x + normalBlend.y + normalBlend.z).xxx;

    half3 finalNormal = half3(normalize(normal_X.zyx * normalBlend.x + normal_Y.xzy * normalBlend.y + normal_Z.xyz * normalBlend.z));
    return finalNormal;
#else
    return half3(0.0h, 0.0h, 1.0h);
#endif
}

#endif
