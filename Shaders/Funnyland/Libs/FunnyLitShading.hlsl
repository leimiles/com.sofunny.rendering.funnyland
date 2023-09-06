#ifndef FUNNY_LIT_SHADING_INCLUDE
#define FUNNY_LIT_SHADING_INCLUDE

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

half TrowbridgeReitzNormalDistribution(half NdotH, half roughness)
{
    half roughnessSqr = roughness * roughness;
    half Distribution = NdotH * NdotH * (roughnessSqr - 1.0) + 1.0;
    return SafeDiv( roughnessSqr, (PI * Distribution * Distribution));
}

half3 FunnyLightingSpecular(half3 lightDir, half3 normal, half3 viewDir, half4 specular, half roughness)
{
    float3 halfVec = SafeNormalize(float3(lightDir) + float3(viewDir));
    half NdotH = half(saturate(dot(normal, halfVec)));
    half specularTerm = TrowbridgeReitzNormalDistribution(NdotH, roughness);
    return specularTerm * specular.rgb;
}

half3 CalculateFunnyBlinnPhong(Light light, InputData inputData, FunnySurfaceData surfaceData)
{
    half3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
    half3 radiance = LightingLambert(attenuatedLightColor, light.direction, inputData.normalWS);

    half perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.smoothness);
    half roughness = max(PerceptualRoughnessToRoughness(perceptualRoughness), HALF_MIN_SQRT);
    half3 lightSpecularColor = FunnyLightingSpecular(light.direction, inputData.normalWS, inputData.viewDirectionWS, half4(surfaceData.specular, 1), roughness);

    #if _ALPHAPREMULTIPLY_ON
        return saturate(radiance * (surfaceData.albedo * surfaceData.alpha + lightSpecularColor));
    #else
        return saturate(radiance * (surfaceData.albedo + lightSpecularColor));
    #endif
}

half3 CalculateEnvironmentSpecular(InputData inputData, FunnySurfaceData surfaceData)
{
    half3 reflectVector = reflect(-inputData.viewDirectionWS, inputData.normalWS);
    half perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.smoothness);
    half3 irradiance = GlossyEnvironmentReflection(reflectVector, inputData.positionWS, perceptualRoughness, surfaceData.indirectSpecularOcclusion, inputData.normalizedScreenSpaceUV);

    half NoV = saturate(dot(inputData.normalWS, inputData.viewDirectionWS));
    half fresnelTerm = Pow4(1.0 - NoV);
    half oneMinusReflectivity = OneMinusReflectivityMetallic(surfaceData.metallic);
    half reflectivity = half(1.0) - oneMinusReflectivity;
    half grazingTerm = saturate(surfaceData.smoothness + reflectivity);
    half roughness = max(PerceptualRoughnessToRoughness(perceptualRoughness), HALF_MIN_SQRT);

    float surfaceReduction = 1.0 / (roughness * roughness + 1.0);
    half3 irradianceMask = half3(surfaceReduction * lerp(surfaceData.specular, grazingTerm, fresnelTerm));

    return irradiance * irradianceMask;
}

half3 CalculateGI(InputData inputData, FunnySurfaceData surfaceData)
{
    half3 indirectSpecular = CalculateEnvironmentSpecular(inputData, surfaceData);
    half3 indirectDiffuse = inputData.bakedGI;
    return indirectDiffuse + indirectSpecular;
}

LightingData CreateLightingData(InputData inputData, FunnySurfaceData surfaceData)
{
    LightingData lightingData;

    lightingData.giColor = inputData.bakedGI;
    lightingData.emissionColor = surfaceData.emission;
    lightingData.vertexLightingColor = 0;
    lightingData.mainLightColor = 0;
    lightingData.additionalLightsColor = 0;

    return lightingData;
}

void FillDebugSurfaceData(inout SurfaceData debugSurfaceData, FunnySurfaceData funnySurfaceData)
{
    debugSurfaceData.albedo = funnySurfaceData.albedo;
    debugSurfaceData.specular = funnySurfaceData.specular;
    debugSurfaceData.metallic = funnySurfaceData.metallic;
    debugSurfaceData.smoothness = funnySurfaceData.smoothness;
    debugSurfaceData.normalTS = funnySurfaceData.normalTS;
    debugSurfaceData.emission = funnySurfaceData.emission;
    debugSurfaceData.occlusion = funnySurfaceData.occlusion;
    debugSurfaceData.alpha = funnySurfaceData.alpha;
    debugSurfaceData.clearCoatMask = funnySurfaceData.clearCoatMask;
    debugSurfaceData.clearCoatSmoothness = funnySurfaceData.clearCoatSmoothness;
}

half4 FunnyFragmentBlinnPhong(InputData inputData, FunnySurfaceData surfaceData)
{
    #if defined(DEBUG_DISPLAY)
    half4 debugColor;

    SurfaceData debugSurfaceData = (SurfaceData)0;
    FillDebugSurfaceData(debugSurfaceData, surfaceData);
    if (CanDebugOverrideOutputColor(inputData, debugSurfaceData, debugColor))
    {
        return debugColor;
    }
    #endif

    uint meshRenderingLayers = GetMeshRenderingLayer();
    half4 shadowMask = CalculateShadowMask(inputData);
    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData.normalizedScreenSpaceUV, surfaceData.occlusion);
    Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);

    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI, aoFactor);

    inputData.bakedGI *= surfaceData.albedo;

    LightingData lightingData = CreateLightingData(inputData, surfaceData);

    lightingData.giColor = CalculateGI(inputData, surfaceData) * aoFactor.indirectAmbientOcclusion;

    #ifdef _LIGHT_LAYERS
    if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
    #endif
    {
        lightingData.mainLightColor += CalculateFunnyBlinnPhong(mainLight, inputData, surfaceData);
    }
    
    #if defined(_ADDITIONAL_LIGHTS)
    uint pixelLightCount = GetAdditionalLightsCount();

    #if USE_FORWARD_PLUS
    for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK

        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
    #ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
    #endif
        {
            lightingData.additionalLightsColor += CalculateFunnyBlinnPhong(light, inputData, surfaceData);
        }
    }
    #endif

    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
    #ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
    #endif
        {
            lightingData.additionalLightsColor += CalculateFunnyBlinnPhong(light, inputData, surfaceData);
        }
    LIGHT_LOOP_END
    #endif

    #if defined(_ADDITIONAL_LIGHTS_VERTEX)
    lightingData.vertexLightingColor += inputData.vertexLighting * surfaceData.albedo;
    #endif

    half4 finalColor = CalculateFinalColor(lightingData, surfaceData.alpha);
    finalColor.rgb = lerp(finalColor.rgb * _MainLightShadowColor.rgb, finalColor.rgb, mainLight.shadowAttenuation * mainLight.distanceAttenuation);
    return finalColor;
}
#endif
