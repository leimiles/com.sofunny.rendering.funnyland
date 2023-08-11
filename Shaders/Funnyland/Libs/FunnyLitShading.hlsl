#ifndef FUNNY_LIT_SHADING_INCLUDE
#define FUNNY_LIT_SHADING_INCLUDE

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

half TrowbridgeReitzNormalDistribution(half NdotH, half roughness)
{
    half roughnessSqr = roughness * roughness;
    half Distribution = NdotH * NdotH * (roughnessSqr - 1.0) + 1.0;
    return roughnessSqr / (3.1415926535 * Distribution * Distribution);
}

half3 FunnyLightingSpecular(half3 lightColor, half3 lightDir, half3 normal, half3 viewDir, half4 specular, half roughness)
{
    float3 halfVec = SafeNormalize(float3(lightDir) + float3(viewDir));
    half NdotH = half(saturate(dot(normal, halfVec)));
    half specularTerm = TrowbridgeReitzNormalDistribution(NdotH, roughness);
    return specularTerm * specular.rgb * lightColor.rgb;
}

half3 CalculateFunnyBlinnPhong(Light light, InputData inputData, SurfaceData surfaceData)
{
    half3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
    half3 lightDiffuseColor = LightingLambert(attenuatedLightColor, light.direction, inputData.normalWS);

    half3 lightSpecularColor = half3(0, 0, 0);
    half perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.smoothness);
    half roughness = max(PerceptualRoughnessToRoughness(perceptualRoughness), HALF_MIN_SQRT);
    lightSpecularColor += FunnyLightingSpecular(attenuatedLightColor, light.direction, inputData.normalWS, inputData.viewDirectionWS, half4(surfaceData.specular, 1), roughness);

    #if _ALPHAPREMULTIPLY_ON
        return lightDiffuseColor * surfaceData.albedo * surfaceData.alpha + lightSpecularColor;
    #else
    return lightDiffuseColor * surfaceData.albedo + lightSpecularColor;
    #endif
}

half3 CalculateEnvironmentSpecular(InputData inputData, SurfaceData surfaceData)
{
    half3 reflectVector = reflect(-inputData.viewDirectionWS, inputData.normalWS);
    half perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.smoothness);
    half3 irradiance = GlossyEnvironmentReflection(reflectVector, inputData.positionWS, perceptualRoughness, 1.0h, inputData.normalizedScreenSpaceUV);

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

half3 CalculateGI(InputData inputData, SurfaceData surfaceData)
{
    half3 indirectSpecular = CalculateEnvironmentSpecular(inputData, surfaceData);
    half3 indirectDiffuse = inputData.bakedGI;
    return indirectDiffuse + indirectSpecular;
}

half4 FunnyFragmentBlinnPhong(InputData inputData, SurfaceData surfaceData)
{
    #if defined(DEBUG_DISPLAY)
    half4 debugColor;

    if (CanDebugOverrideOutputColor(inputData, surfaceData, debugColor))
    {
        return debugColor;
    }
    #endif

    uint meshRenderingLayers = GetMeshRenderingLayer();
    half4 shadowMask = CalculateShadowMask(inputData);
    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
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

    return CalculateFinalColor(lightingData, surfaceData.alpha);
}
#endif
