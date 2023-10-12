#ifndef FUNNY_Jelly_SURFACE_DATA_INCLUDED
#define FUNNY_Jelly_SURFACE_DATA_INCLUDED

struct FunnyJellySurfaceData
{
    half3 albedo;
    half3 specular;
    half  metallic;
    half  smoothness;
    half3 normalTS;
    half3 emission;
    half  occlusion;
    half  indirectSpecularOcclusion;
    half  alpha;
    half  clearCoatMask;
    half  clearCoatSmoothness;

    half thickness;
    half subsurfaceIntensity;
    half4 color;
    half transmission;
    half refractIntensity;
};

#endif
