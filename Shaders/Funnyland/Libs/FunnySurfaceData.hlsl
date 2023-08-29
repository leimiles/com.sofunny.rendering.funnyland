#ifndef FUNNY_SURFACE_DATA_INCLUDED
#define FUNNY_SURFACE_DATA_INCLUDED

struct FunnySurfaceData
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
};

#endif
