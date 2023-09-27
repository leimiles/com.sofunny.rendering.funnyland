#define COLLAPSIBLE_GROUP 1
#define PLANAR_REFLECTION_DISTORTION_MULTIPLIER 0.25

struct SceneData
{
	float4 positionSS;
	float2 screenPos; 
	float3 positionWS;
	float3 color;

	#ifdef SCENE_SHADOWMASK
	float shadowMask;
	#endif
	
	float viewDepth;
	float verticalDepth;
	
	#if RESAMPLE_REFRACTION_DEPTH && _REFRACTION
	float viewDepthRefracted;
	float verticalDepthRefracted;
	#endif
	
	float skyMask;

	//More easy debugging
	half refractionMask;
};

void PopulateSceneData(inout SceneData scene, Varyings input, WaterSurface water, float4 shadowCoords)
{	
	scene.positionSS = input.screenPos;
	scene.screenPos = scene.positionSS.xy / scene.positionSS.w;
	
	scene.viewDepth = 1;
	scene.verticalDepth = 1;

	scene.refractionMask = 1.0;
	#if !_DISABLE_DEPTH_TEX
	SceneDepth depth = SampleDepth(scene.positionSS);
	scene.positionWS = ReconstructWorldPosition(scene.positionSS, water.viewDelta, depth);
	
	//Invert normal when viewing backfaces
	float normalSign = ceil(dot(water.viewDir, water.waveNormal));
	normalSign = normalSign == 0 ? -1 : 1;
	
	scene.viewDepth = SurfaceDepth(depth, input.positionCS);
	scene.verticalDepth = DepthDistance(water.positionWS, scene.positionWS, water.waveNormal * normalSign);
	
	#if _REFRACTION
		SceneDepth depthRefracted = SampleDepth(scene.positionSS + water.refractionOffset);
		float3 opaqueWorldPosRefracted = ReconstructWorldPosition(scene.positionSS + water.refractionOffset, water.viewDelta, depthRefracted);
	
		scene.refractionMask = saturate(SurfaceDepth(depthRefracted, input.positionCS));
		water.refractionOffset *= scene.refractionMask;
	
		#if RESAMPLE_REFRACTION_DEPTH
		depthRefracted = SampleDepth(scene.positionSS + water.refractionOffset);
		opaqueWorldPosRefracted = ReconstructWorldPosition(scene.positionSS + water.refractionOffset, water.viewDelta, depthRefracted);
		scene.positionWS = lerp(scene.positionWS, opaqueWorldPosRefracted, scene.refractionMask);
		scene.viewDepthRefracted = SurfaceDepth(depthRefracted, input.positionCS);
		scene.verticalDepthRefracted = DepthDistance(water.positionWS, opaqueWorldPosRefracted, water.waveNormal * normalSign);
		#endif
	#endif

	#ifdef SCENE_SHADOWMASK
		Light sceneLight = GetMainLight(shadowCoords, scene.positionWS, 1.0);
		scene.shadowMask = sceneLight.shadowAttenuation;
	#endif

	#if _ADVANCED_SHADING
		half VdotN = 1.0 - saturate(dot(water.viewDir, water.waveNormal));
		float grazingTerm = saturate(pow(VdotN, 64));
	
		//Resort to z-depth at surface edges. Otherwise makes intersection/edge fade visible through the water surface
		scene.verticalDepth = lerp(scene.verticalDepth, scene.viewDepth, grazingTerm);

		#if RESAMPLE_REFRACTION_DEPTH && _REFRACTION
		scene.verticalDepthRefracted = lerp(scene.verticalDepthRefracted, scene.viewDepthRefracted, grazingTerm);
		#endif
	#endif
	
	#endif

	#if _REFRACTION
	float dispersion = _RefractionChromaticAberration * lerp(1.0, 2.0,  unity_OrthoParams.w);

	scene.color = SampleOpaqueTexture(scene.positionSS, water.refractionOffset.xy, dispersion);
	#endif

	//Skybox mask is used for backface (underwater) reflections, to blend between refraction and reflection probes
	scene.skyMask = 0;
	#ifdef DEPTH_MASK
		#if !_DISABLE_DEPTH_TEX
		float depthSource = depth.linear01;
		#if RESAMPLE_REFRACTION_DEPTH && _REFRACTION
		//Use depth resampled with refracted screen UV
		depthSource = depthRefracted.linear01;
		#endif
		scene.skyMask = depthSource > 0.99 ? 1 : 0;
		#endif
	#endif
}

float GetWaterDensity(SceneData scene, float mask, float heightScalar, float viewDepthScalar, bool exponential)
{
	float density = 1.0;
	#if !_DISABLE_DEPTH_TEX
	float viewDepth = scene.viewDepth;
	float verticalDepth = scene.verticalDepth;
		#if defined(RESAMPLE_REFRACTION_DEPTH) && _REFRACTION
		viewDepth = scene.viewDepthRefracted;
		verticalDepth = scene.verticalDepthRefracted;
		#endif

	float depthAttenuation = 1.0 - exp(-viewDepth * viewDepthScalar * 0.1);
	float heightAttenuation = saturate(lerp(verticalDepth * heightScalar, 1.0 - exp(-verticalDepth * heightScalar), exponential));
	
	density = max(depthAttenuation, heightAttenuation);
	#endif
	
	//Use green vertex color channel to subtract density
	density = saturate(density - mask);
	return density;
}

float3 GetWaterColor(SceneData scene, float3 scatterColor, float density, float absorption)
{
	float depth = scene.verticalDepth;
	float accumulation = scene.viewDepth;

	#if defined(RESAMPLE_REFRACTION_DEPTH) && _REFRACTION
	depth = scene.verticalDepthRefracted;
	accumulation = scene.viewDepthRefracted;
	#endif
	
	const float3 underwaterColor = saturate(scene.color * exp(-density * (depth + accumulation)));
	const float scatterAmount = saturate(exp(-absorption * accumulation));
	return lerp(underwaterColor, scatterColor, scatterAmount);
}

#define FRONT_FACE_SEMANTIC_REAL FRONT_FACE_SEMANTIC
float4 ForwardPassFragment(Varyings input, FRONT_FACE_TYPE vertexFace : FRONT_FACE_SEMANTIC_REAL) : SV_Target
{
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
	
	InputData inputData = (InputData)0;
	SurfaceData surfaceData = (SurfaceData)0; 
	WaterSurface water = (WaterSurface)0;
	SceneData scene = (SceneData)0;

	water.alpha = 1.0;
	water.vFace = IS_FRONT_VFACE(vertexFace, true, false); //0 = back face
	int faceSign = water.vFace > 0 ? 1 : -1;
	
	/* ========
	// GEOMETRY DATA
	=========== */
	#if COLLAPSIBLE_GROUP

	float4 vertexColor = input.color;
	float3 normalWS = normalize(input.normalWS.xyz);
#ifdef _NORMALMAP
	float3 WorldTangent = input.tangent.xyz;
	float3 WorldBiTangent = input.bitangent.xyz;
	float3 positionWS = float3(input.normalWS.w, input.tangent.w, input.bitangent.w);
#else
	float3 positionWS = input.positionWS;
#endif
	water.positionWS = positionWS;
	water.viewDelta = GetCurrentViewPosition() - positionWS;
	water.viewDir = normalize(water.viewDelta);
	half VdotN = 1.0 - saturate(dot(water.viewDir * faceSign, normalWS));
	
	water.vertexNormal = normalWS;
	//Returns mesh or world-space UV
	float2 uv = GetSourceUV(input.uv.xy, positionWS.xz, _WorldSpaceUV);;
	#endif

	/* ========
	// WAVES
	=========== */
	#if COLLAPSIBLE_GROUP
	water.waveNormal = normalWS;
#if _WAVES
	WaveInfo waves = GetWaveInfo(uv, TIME * _WaveSpeed, _WaveHeight,  lerp(1, 0, vertexColor.b), _WaveFadeDistance.x, _WaveFadeDistance.y);
	waves.normal = lerp(waves.normal, normalWS, lerp(0, 1, vertexColor.b));
	water.waveNormal = waves.normal;
	water.offset.y += waves.position.y;
	water.offset.xz += waves.position.xz * 0.5;
#endif
	#endif

	#if _WAVES
	if(_WorldSpaceUV == 1) uv = GetSourceUV(input.uv.xy, positionWS.xz + water.offset.xz, _WorldSpaceUV);
	#endif
	
	/* ========
	// SHADOWS
	=========== */
	#if COLLAPSIBLE_GROUP
	water.shadowMask = 1.0;
	float4 shadowCoords = float4(0, 0, 0, 0);
	#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
	shadowCoords = input.shadowCoord;
	#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
	shadowCoords = TransformWorldToShadowCoord(water.positionWS);
	#endif
	half4 shadowMask = 1.0;
	shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
	Light mainLight = GetMainLight(shadowCoords, water.positionWS, shadowMask);
	
	#if _LIGHT_LAYERS
	uint meshRenderingLayers = GetMeshRenderingLayer();
	if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
	#endif
	{
		water.shadowMask = mainLight.shadowAttenuation;
	}
	
	half backfaceShadows = 1;
	#endif

	/* ========
	// NORMALS
	=========== */
	#if COLLAPSIBLE_GROUP
	water.tangentNormal = float3(0.5, 0.5, 1);
	water.tangentWorldNormal = water.waveNormal;
	
#if _NORMALMAP
	//Tangent-space
	water.tangentNormal = SampleNormals(uv, _NormalTiling, _NormalSubTiling, positionWS, TIME, _NormalSpeed, _NormalSubSpeed, water.slope, water.vFace);
	water.tangentToWorldMatrix = half3x3(WorldTangent, WorldBiTangent, water.waveNormal);
	//World-space
	water.tangentWorldNormal = normalize(TransformTangentToWorld(water.tangentNormal, water.tangentToWorldMatrix));	
#endif
	#endif
		
	#if _REFRACTION
	float3 refractionViewDir = water.viewDir;
	refractionViewDir = GetWorldToViewMatrix()[2].xyz;
	water.refractionOffset.xy = RefractionOffset(input.screenPos.xy / input.screenPos.w, refractionViewDir, water.tangentWorldNormal, _RefractionStrength * lerp(1, 0.1,  unity_OrthoParams.w));
	water.refractionOffset.zw = 0;
	#endif
	PopulateSceneData(scene, input, water, shadowCoords);
	
	/* =========
	// COLOR + FOG
	============ */
	#if COLLAPSIBLE_GROUP
	water.fog = GetWaterDensity(scene, vertexColor.g, _DepthHorizontal, _DepthVertical, _DepthExp);
	//Albedo
	float4 baseColor = lerp(_ShallowColor, _BaseColor, water.fog);
	baseColor.rgb += saturate(_WaveTint * water.offset.y);

	water.fog *= baseColor.a;
	water.alpha = baseColor.a;

	#if COLOR_ABSORPTION && _REFRACTION
	if (_ColorAbsorption > 0)
	{
		baseColor.rgb = GetWaterColor(scene, baseColor.rgb, water.fog, _ColorAbsorption * water.vFace);
	}
	#endif
	
	water.albedo.rgb = baseColor.rgb;	
	#endif

	/* ========
	// INTERSECTION FOAM
	=========== */
	#if COLLAPSIBLE_GROUP

	water.intersection = 0;
#if _SHARP_INERSECTION || _SMOOTH_INTERSECTION

	float interSecGradient = 0;
	
	#if !_DISABLE_DEPTH_TEX
	interSecGradient = 1-saturate(exp(scene.verticalDepth) / _IntersectionLength);	
	#endif
	
	if (_IntersectionSource == 1) interSecGradient = vertexColor.r;
	if (_IntersectionSource == 2) interSecGradient = saturate(interSecGradient + vertexColor.r);
	water.intersection = SampleIntersection(uv.xy, interSecGradient, TIME * _IntersectionSpeed) * _IntersectionColor.a;
	
	#if _WAVES
	if(positionWS.y < scene.positionWS.y) water.intersection = 0;
	#endif
	
	water.waveNormal = lerp(water.waveNormal, normalWS, water.intersection);
#endif

	#if _NORMALMAP
	water.tangentWorldNormal = lerp(water.tangentWorldNormal, water.vertexNormal, water.intersection);
	#endif
	#endif

	/* ========
	// SURFACE FOAM
	=========== */
	#if COLLAPSIBLE_GROUP
	water.foam = 0;
	
#if _FOAM
	float crest = saturate(water.offset.y) * _FoamWaveAmount;
	float foamSlopeMask = 0;
	float baseFoam = saturate(_FoamBaseAmount * 1-water.slope);
	float foamMask = crest + baseFoam + foamSlopeMask + vertexColor.a;
	float foamOffset = water.offset.y;
	half2 distortion = (_FoamDistortion * saturate(foamOffset * 0.5 + 0.5));
	float foamTex = SampleFoamTexture((uv + distortion.xy), _FoamTiling, _FoamSubTiling, TIME, _FoamSpeed, _FoamSubSpeed, foamSlopeMask);
	if(_FoamClipping > 0) foamTex = smoothstep(_FoamClipping, 1.0, foamTex);
	//Dissolve the foam
	foamMask = saturate(1.0 - foamMask);
	water.foam = smoothstep(foamMask, foamMask + 1.0, foamTex) * saturate(_FoamColor.a);
	
	#if _NORMALMAP
	water.tangentWorldNormal = lerp(water.tangentWorldNormal, water.waveNormal, water.foam);
	#endif
#endif
	#endif
	
	/* ========
	// EMISSION (Caustics + Specular)
	=========== */
	#if COLLAPSIBLE_GROUP
	#if _CAUSTICS
	float2 causticsProjection = scene.positionWS.xz;
	#if _DISABLE_DEPTH_TEX
	causticsProjection = water.positionWS.xz;
	#endif
	water.caustics = SampleCaustics(causticsProjection + lerp(water.waveNormal.xz, water.tangentWorldNormal.xz, _CausticsDistortion), TIME * _CausticsSpeed, _CausticsTiling);
	float causticsMask = saturate((1-water.fog) - water.intersection - water.foam - scene.skyMask) * water.vFace;

	#ifdef SCENE_SHADOWMASK
	causticsMask *= scene.shadowMask;
	#endif
	water.caustics *= causticsMask * _CausticsBrightness;
	#endif
	
#ifndef _SPECULARHIGHLIGHTS_OFF
	float3 lightReflectionNormal = water.tangentWorldNormal;
	half specularMask = saturate((1-water.foam * 2.0) * (1-water.intersection) * water.shadowMask);
	half3 sunSpec = SpecularReflection(mainLight, water.viewDir, water.waveNormal, lightReflectionNormal, _SunReflectionDistortion, lerp(8196, 64, _SunReflectionSize),
		/* Mask: */ _SunReflectionStrength * specularMask);
	water.specular += sunSpec;
#endif
	 
	//Reflection probe/planar
#ifndef _ENVIRONMENTREFLECTIONS_OFF
	float3 refWorldTangentNormal = lerp(water.waveNormal, normalize(water.waveNormal + water.tangentWorldNormal), _ReflectionDistortion);
	float3 reflectionVector = reflect(-water.viewDir, refWorldTangentNormal);
	float2 reflectionPixelOffset = lerp(water.vertexNormal.xz, water.tangentWorldNormal.xz, _ReflectionDistortion * scene.positionSS.w * PLANAR_REFLECTION_DISTORTION_MULTIPLIER).xy;
	water.reflections = SampleReflections(reflectionVector, _ReflectionBlur, 0, scene.positionSS.xyzw, positionWS, refWorldTangentNormal, water.viewDir, reflectionPixelOffset, inputData.normalizedScreenSpaceUV);
	float reflectionFresnel = ReflectionFresnel(refWorldTangentNormal, water.viewDir * faceSign, _ReflectionFresnel);
	water.reflectionMask = _ReflectionStrength * reflectionFresnel;
	water.reflectionLighting = 1-_ReflectionLighting;

	#if _UNLIT
	water.reflectionLighting = 1.0;
	#endif
#endif
	#endif

	/* ========
	// COMPOSITION
	=========== */
	#ifdef COLLAPSIBLE_GROUP
	#if _FOAM
	water.albedo.rgb = lerp(water.albedo.rgb, _FoamColor.rgb, saturate(water.foam * 2.0));
	#endif

	#if _SHARP_INERSECTION || _SMOOTH_INTERSECTION
	water.albedo.rgb = lerp(water.albedo.rgb, _IntersectionColor.rgb, water.intersection);
	#endif

	#if _FOAM || _SHARP_INERSECTION || _SMOOTH_INTERSECTION
	water.alpha = saturate(water.alpha + water.intersection + water.foam);
	#endif

	#ifndef _ENVIRONMENTREFLECTIONS_OFF
	water.reflectionMask = saturate(water.reflectionMask - water.foam - water.intersection) * _ReflectionStrength;

	#if !_UNLIT
	water.albedo.rgb = lerp(water.albedo, lerp(water.albedo.rgb, water.reflections, water.reflectionMask), _ReflectionLighting);
	#endif
	#endif

	#if !_UNLIT
	water.diffuseNormal = lerp(water.waveNormal, water.tangentWorldNormal, _NormalStrength);
	#endif
	
	float fresnel = saturate(pow(VdotN, _HorizonDistance)) * _HorizonColor.a;
	water.albedo.rgb = lerp(water.albedo.rgb, _HorizonColor.rgb, fresnel);
	
	//Final alpha
	water.edgeFade = saturate(scene.verticalDepth / (_EdgeFade * 0.01));
	water.alpha *= water.edgeFade;
	// return water.edgeFade;
	#endif
	
	/* ========
	// UNITY SURFACE & INPUT DATA
	=========== */
	#if COLLAPSIBLE_GROUP
	surfaceData.albedo = water.albedo.rgb;
	surfaceData.specular = water.specular.rgb;
	surfaceData.metallic = 0;
	surfaceData.smoothness = 0;
	surfaceData.normalTS = water.tangentNormal;
	surfaceData.emission = 0; //To be populated with translucency+caustics
	surfaceData.occlusion = 1.0;
	surfaceData.alpha = water.alpha;
	
	inputData.positionWS = positionWS;
	inputData.viewDirectionWS = water.viewDir;
	inputData.shadowCoord = shadowCoords;
	inputData.normalWS = water.tangentWorldNormal;
	inputData.fogCoord = InitializeInputDataFog(float4(positionWS, 1.0), input.fogFactorAndVertexLight.x);
	inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;

	inputData.bakedGI = 0;
	#if defined(DYNAMICLIGHTMAP_ON)
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV, input.vertexSH, water.waveNormal);
    #else
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, water.waveNormal);
    #endif
	inputData.shadowMask = shadowMask;
	#endif
	
	float4 finalColor = float4(ApplyLighting(surfaceData, scene.color, mainLight, inputData, water, _ShadowStrength, water.vFace), water.alpha);
	
	#if _REFRACTION
	finalColor.rgb = lerp(scene.color.rgb, finalColor.rgb, saturate(water.fog + water.intersection + water.foam));
	// water.alpha = water.edgeFade;
	#endif

	half fogMask = 1.0;
	finalColor.rgb = MixFog(finalColor.rgb, inputData.fogCoord);
	finalColor.a = water.alpha;

	return finalColor;
}