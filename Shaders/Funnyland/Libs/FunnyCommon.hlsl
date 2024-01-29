#ifndef FUNNY_COMMON_INCLUDED
#define FUNNY_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"

half GetShadowArea(Light mainLight, half3 normalWS)
{
    half maxMainLightColorChannel = max(max(mainLight.color.r, mainLight.color.g), mainLight.color.b); 
    half NdotL = saturate(dot(normalWS, mainLight.direction));
    half shadowArea = (mainLight.shadowAttenuation * mainLight.distanceAttenuation) * NdotL;
    shadowArea = smoothstep(0.0, 0.1, shadowArea);
    //wangxiaolong:修复没有平行光时，渲染变黑的问题
    shadowArea = lerp(1, shadowArea, step(0.001, maxMainLightColorChannel));
    return shadowArea;
}

#endif
