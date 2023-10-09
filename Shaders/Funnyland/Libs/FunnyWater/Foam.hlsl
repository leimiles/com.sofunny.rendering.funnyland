#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_FoamTex);
SAMPLER(sampler_FoamTex);

float SampleFoamTexture(float2 uv, float tiling, float subTiling, float2 time, float speed, float subSpeed, float slopeMask)
{
	float4 uvs = PackedUV(uv * tiling, time, speed, subTiling, subSpeed);

	float f1 = SAMPLE_TEXTURE2D(_FoamTex, sampler_FoamTex, uvs.xy).r;	
	float f2 = SAMPLE_TEXTURE2D(_FoamTex, sampler_FoamTex, uvs.zw).r;

	#if UNITY_COLORSPACE_GAMMA
	f1 = SRGBToLinear(f1);
	f2 = SRGBToLinear(f2);
	#endif

	float foam = saturate(f1 + f2);
	return foam;
}