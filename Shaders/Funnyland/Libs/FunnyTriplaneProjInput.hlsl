#ifndef FUNNY_TRIPLANEPROJ_INPUT_INCLUDED
#define FUNNY_TRIPLANEPROJ_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Funnyland/Libs/FunnySurfaceData.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Funnyland/Libs/GlobalParams.hlsl"

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

// inline void InitializeTriplaneProjSurfaceData(Varyings input, out FunnySurfaceData outSurfaceData)
// {
//     outSurfaceData = (FunnySurfaceData)0;
//
//     //half4 albedoAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
//     // basemap
//     // float2 uv_Z = input.positionWS.xy * _TexLeftTilling + _TexLeftOffset.xy;
//     // float2 uv_X = input.positionWS.zy * _TexForwardTilling + _TexForwardOffset.xy;
//     // float2 uv_Y = input.positionWS.xz * _TexUpTilling + _TexUpOffset.xy;
//     float2 uv_Z = input.positionWS.xy;
//     float2 uv_X = input.positionWS.zy;
//     float2 uv_Y = input.positionWS.xz;
//     half4 albedoAlpha = TextureBlend(_BaseMap, sampler_BaseMap, uv_Z, uv_X, uv_Y, input.normalWS);
//     
//     outSurfaceData.alpha = albedoAlpha.a * _BaseColor.a;
//     outSurfaceData.alpha = AlphaDiscard(outSurfaceData.alpha, _Cutoff);
//
//     //half4 mixMap = SAMPLE_TEXTURE2D(_MixMap, sampler_MixMap, uv);
//     half4 mixMap = TextureBlend(_MixMap, sampler_MixMap, uv_Z, uv_X, uv_Y, input.normalWS);
//     mixMap.b = saturate(lerp(_RoughnessLow, _RoughnessHigh, mixMap.b));
//     
//     outSurfaceData.metallic = mixMap.r;
//
//     outSurfaceData.albedo = saturate((1 - outSurfaceData.metallic) * 0.96) * albedoAlpha.rgb * _BaseColor.rgb;
//     outSurfaceData.albedo = AlphaModulate(outSurfaceData.albedo, outSurfaceData.alpha);
//     
//     outSurfaceData.specular = lerp(0.04, (albedoAlpha.rgb * _BaseColor.rgb), outSurfaceData.metallic);
//     outSurfaceData.smoothness = (1 - mixMap.b);
//
//     // normal
//     #ifdef _NORMALMAP
//         // half3 normalTS = NormalBlend(uv_Z, uv_X, uv_Y, input.normalWS, _BumpScale);
//         //
//         // half3 bitangent = input.tangentWS.w * cross(input.normalWS, input.tangentWS.xyz);
//         // half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent, input.normalWS);
//         // normalTS = TransformWorldToTangent(normalTS, tangentToWorld);
//         // outSurfaceData.normalTS = normalTS;
//     #else
//         outSurfaceData.normalTS = half3(0, 0, 1);
//     #endif
//     
//     outSurfaceData.occlusion = mixMap.g;
//     outSurfaceData.indirectSpecularOcclusion = _IndirectSpecularOcclusion;
//     //outSurfaceData.emission = SampleEmission(uv, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));
// }

#endif
