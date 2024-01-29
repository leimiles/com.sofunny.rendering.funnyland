#ifndef FUNNY_JELLY_SHADING_INCLUDE
#define FUNNY_JELLY_SHADING_INCLUDE

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

half TrowbridgeReitzNormalDistribution(half NdotH, half roughness)
{
    half roughnessSqr = roughness * roughness;
    half Distribution = NdotH * NdotH * (roughnessSqr - 1.0) + 1.0;
    half specularTerm = SafeDiv( roughnessSqr, (PI * Distribution * Distribution));
    specularTerm = specularTerm - HALF_MIN;
    specularTerm = clamp(specularTerm, 0.0, 1000.0);
    return specularTerm;
}

half3 FunnyLightingSpecular(half3 lightDir, half3 normal, half3 viewDir, half4 specular, half roughness)
{
    float3 halfVec = SafeNormalize(float3(lightDir) + float3(viewDir));
    half NdotH = saturate(dot(normal, halfVec));
    half specularTerm = TrowbridgeReitzNormalDistribution(NdotH, roughness);
    return specularTerm * specular.rgb;
}

float SchlickWeight(float u)
{
    float m = clamp(1.0 - u, 0.0, 1.0);
    float m2 = m * m;
    return m2 * m2 * m;
}

float SchlickWeight2(float u)
{
    float m = clamp(1.0 - u, 0.0, 1.0);
    float m2 = m * m;
    return m2 ;
}

half CalculateSubsurface(Light light, InputData inputData, FunnyJellySurfaceData surfaceData)
{
    #ifdef _FRP_HIGH_SHADER_QUALITY
        //modify Hanrahan Krueger diffuse model
        half perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.smoothness);
        half roughness = max(PerceptualRoughnessToRoughness(perceptualRoughness), HALF_MIN_SQRT);
        half3 Hdir = normalize(light.direction + inputData.viewDirectionWS);
        half LdotH = saturate(dot(light.direction, Hdir));
        half NdotL = saturate(dot(inputData.normalWS, light.direction));
        half NdotV = saturate(dot(inputData.normalWS, inputData.viewDirectionWS));
        float FL = SchlickWeight2(NdotL);
        float FV = SchlickWeight(NdotV);
        float Rr = 2.0 * roughness * LdotH * LdotH;
        float Fss90 = 0.5 * Rr;
        float Fss = lerp(1.0, Fss90, FL) * lerp(1.0, Fss90, FV);
        float ss = 1.25 * (Fss * (1.0 / (NdotL + NdotV) - 0.5) + 0.5);

        return saturate(ss * FL * (1 - surfaceData.thickness));
    #else
        half NdotL = saturate(dot(inputData.normalWS, light.direction));
        float FL = SchlickWeight2(NdotL);
        half3 h = normalize(-light.direction + inputData.normalWS * 0.5);
        half VdotH = dot(inputData.viewDirectionWS, h);
        return  saturate(FL * PositivePow(VdotH, 5) * (1 - surfaceData.thickness));
    #endif
}

half3 CalculateDirectLighting(Light light, InputData inputData, FunnyJellySurfaceData surfaceData)
{
    half3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
    half NdotL = saturate(dot(inputData.normalWS, light.direction) * 0.5 + 1.0);
    half3 radiance = attenuatedLightColor * NdotL;

    half subsurfaceTerm = CalculateSubsurface(light, inputData, surfaceData);
    subsurfaceTerm *= surfaceData.subsurfaceIntensity;
    half3 subsurfaceColor = subsurfaceTerm * surfaceData.color.rgb * lerp(0, 5, surfaceData.subsurfaceIntensity);
    subsurfaceColor = lerp(subsurfaceColor, 0, surfaceData.transmission);
    
    half perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.smoothness);
    half roughness = max(PerceptualRoughnessToRoughness(perceptualRoughness), HALF_MIN_SQRT);
    half3 lightSpecularColor = FunnyLightingSpecular(light.direction, inputData.normalWS, inputData.viewDirectionWS, half4(surfaceData.specular, 1), roughness);
    half3 diffuseColor = lerp(0, surfaceData.albedo, saturate(1 - subsurfaceTerm - surfaceData.transmission));

    #if _ALPHAPREMULTIPLY_ON
        return (radiance * (diffuseColor * surfaceData.alpha + lightSpecularColor) + subsurfaceColor);
    #else
        return (radiance * (diffuseColor + lightSpecularColor) + subsurfaceColor);
    #endif
}

half3 CalculateEnvironmentSpecular(InputData inputData, FunnyJellySurfaceData surfaceData)
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

half3 CalculateGI(InputData inputData, FunnyJellySurfaceData surfaceData)
{
    half3 indirectSpecular = CalculateEnvironmentSpecular(inputData, surfaceData);
    half3 indirectDiffuse = inputData.bakedGI;
    return indirectDiffuse + indirectSpecular;
}

LightingData CreateLightingData(InputData inputData, FunnyJellySurfaceData surfaceData)
{
    LightingData lightingData;

    lightingData.giColor = inputData.bakedGI;
    lightingData.emissionColor = surfaceData.emission;
    lightingData.vertexLightingColor = 0;
    lightingData.mainLightColor = 0;
    lightingData.additionalLightsColor = 0;

    return lightingData;
}

void FillDebugSurfaceData(inout SurfaceData debugSurfaceData, FunnyJellySurfaceData funnySurfaceData)
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

half GetShadowArea(Light mainLight, half3 normalWS)
{
    half maxMainLightColorChannel = max(max(mainLight.color.r, mainLight.color.g), mainLight.color.b); 
    half NdotL = saturate(dot(normalWS, mainLight.direction));
    half shadowArea = (mainLight.shadowAttenuation * mainLight.distanceAttenuation) * NdotL;
    shadowArea = smoothstep(0.0, 0.1, shadowArea);
    //wangxiaolong:修复没有平行光时，渲染变黑的问题
    shadowArea = LerpWhiteTo(shadowArea, step(0.001, maxMainLightColorChannel));
    return shadowArea;
}

#ifdef _FRP_HIGH_SHADER_QUALITY
half4 SamplerBgTexture(half2 screenUV, half transmission)
{
    float2 texelSize = _CameraOpaqueTexture_TexelSize.xy;
    half2 uv = screenUV;

    //Kawase Blur
    half4 sum = 0;
    half pixelOffset = lerp(2, 1, transmission);
    sum += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv + float2(pixelOffset + 0.5, pixelOffset + 0.5) * texelSize); 
    sum += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv + float2(-pixelOffset - 0.5, pixelOffset + 0.5) * texelSize); 
    sum += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv + float2(-pixelOffset - 0.5, -pixelOffset - 0.5) * texelSize); 
    sum += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv + float2(pixelOffset + 0.5, -pixelOffset - 0.5) * texelSize); 
    return sum * 0.25;
}

void CalculateTransmission(inout half4 color, InputData inputData, FunnyJellySurfaceData surfaceData)
{
    // 内部物体颜色计算暂时去掉
    // half insideRefractValue = lerp(0.02, 0, surfaceData.transmission);
    // half4 insideObjMap = SAMPLE_TEXTURE2D(_ScreenColorRT, sampler_ScreenColorRT, inputData.normalizedScreenSpaceUV + inputData.normalWS.yz * insideRefractValue);
    // half3 insideColor = lerp(color.rgb + insideObjMap.rgb * surfaceData.color.rgb, insideObjMap.rgb, saturate(surfaceData.transmission - 0.3));//[0, 0.7]
    // color.rgb = lerp(color.rgb, insideColor, surfaceData.transmission);

    #if defined(_USE_REFRACT)
    half bgRefractValue = lerp(0.0, 0.2, surfaceData.refractIntensity);
    half4 screenBgMap = SamplerBgTexture(inputData.normalizedScreenSpaceUV + inputData.normalWS.yz * bgRefractValue, surfaceData.transmission);
    half3 bgColor = lerp(0, screenBgMap.rgb * saturate(1 - surfaceData.thickness), surfaceData.transmission);
    color.rgb += bgColor;
    #endif 
}
#endif

half4 FunnyFragmentSubsurface(InputData inputData, FunnyJellySurfaceData surfaceData)
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
    mainLight.shadowAttenuation = lerp(1, mainLight.shadowAttenuation, _MainLightShadowColor.a);

    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI, aoFactor);

    inputData.bakedGI *= surfaceData.albedo;

    LightingData lightingData = CreateLightingData(inputData, surfaceData);

    lightingData.giColor = CalculateGI(inputData, surfaceData) * aoFactor.indirectAmbientOcclusion;

    #ifdef _LIGHT_LAYERS
    if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
    #endif
    {
        lightingData.mainLightColor += CalculateDirectLighting(mainLight, inputData, surfaceData);
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
            lightingData.additionalLightsColor += CalculateDirectLighting(light, inputData, surfaceData);
        }
    }
    #endif

    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
    #ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
    #endif
        {
            lightingData.additionalLightsColor += CalculateDirectLighting(light, inputData, surfaceData);
        }
    LIGHT_LOOP_END
    #endif

    #if defined(_ADDITIONAL_LIGHTS_VERTEX)
    lightingData.vertexLightingColor += inputData.vertexLighting * surfaceData.albedo;
    #endif

    half4 finalColor = CalculateFinalColor(lightingData, surfaceData.alpha);
    half shadowArea = GetShadowArea(mainLight, inputData.normalWS);
    finalColor.rgb = lerp(finalColor.rgb * _MainLightShadowColor.rgb, finalColor.rgb, shadowArea);

    #ifdef _FRP_HIGH_SHADER_QUALITY
        CalculateTransmission(finalColor, inputData, surfaceData);
    #endif
    
    return finalColor;
}
#endif
