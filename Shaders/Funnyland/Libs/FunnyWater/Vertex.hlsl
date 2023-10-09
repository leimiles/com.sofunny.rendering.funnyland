struct Attributes
{
	float4 positionOS 	: POSITION;
	float4 uv 			: TEXCOORD0;
	float4 normalOS 	: NORMAL;
	float4 tangentOS 	: TANGENT;
	float4 color 		: COLOR0;
	float2 staticLightmapUV   : TEXCOORD1;
	float2 dynamicLightmapUV  : TEXCOORD2;
	UNITY_VERTEX_INPUT_INSTANCE_ID  
};

struct Varyings
{	
	float4 uv 			: TEXCOORD0;

	half4 fogFactorAndVertexLight : TEXCOORD1; // x: fogFactor, yzw: vertex light
	
	#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) //No shadow cascades
	float4 shadowCoord 	: TEXCOORD2;
	#endif
	
	//wPos.x in w-component
	float4 normalWS 	: NORMAL;
	#ifdef _NORMALMAP
	//wPos.y in w-component
	float4 tangent 		: TANGENT; 
	//wPos.z in w-component
	float4 bitangent 	: TEXCOORD4;
	#else
	float3 positionWS 	: TEXCOORD4;
	#endif

	float4 screenPos 	: TEXCOORD5;
	
	DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 8); 
	#ifdef DYNAMICLIGHTMAP_ON
	float2  dynamicLightmapUV : TEXCOORD9; // Dynamic lightmap UVs
	#endif

	float4 positionCS 	: SV_POSITION;
	float4 color 		: COLOR0;
	
	UNITY_VERTEX_INPUT_INSTANCE_ID 
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings LitPassVertex(Attributes input)
{
	Varyings output = (Varyings)0;

	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

#if defined(CURVEDWORLD_IS_INSTALLED) && !defined(CURVEDWORLD_DISABLED_ON) 
#if defined(CURVEDWORLD_NORMAL_TRANSFORMATION_ON)
	CURVEDWORLD_TRANSFORM_VERTEX_AND_NORMAL(input.positionOS, input.normalOS.xyz, input.tangentOS)
#else
    CURVEDWORLD_TRANSFORM_VERTEX(input.positionOS)
#endif
#endif

	output.uv.xy = input.uv.xy;
	output.uv.z = _TimeParameters.x;
	output.uv.w = 0;

	float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
	float3 offset = 0;
	
	#ifdef MODIFIERS_ENABLED
	offset += GetDisplacementOffset(positionWS);
	#endif

	VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS.xyz, input.tangentOS);

	if(_WorldSpaceUV > 0)
	{
		normalInput.tangentWS = half3(1.0, 0.0, 0.0);
		normalInput.bitangentWS = half3(0.0, 0.0, 1.0);
	}
	
	float4 vertexColor = GetVertexColor(input.color.rgba, float4(_IntersectionSource > 0 ? 1 : 0, _VertexColorDepth, _VertexColorWaveFlattening, _VertexColorFoam));
	#if defined(_WAVES)
	float2 uv = GetSourceUV(input.uv.xy, positionWS.xz, _WorldSpaceUV);

	//Vertex animation
	WaveInfo waves = GetWaveInfo(uv, TIME_VERTEX * _WaveSpeed, _WaveHeight, lerp(1, 0, vertexColor.b), _WaveFadeDistance.x, _WaveFadeDistance.y);
	//Offset in direction of normals (only when using mesh uv)
	if(_WorldSpaceUV == 0) waves.position *= normalInput.normalWS.xyz;
	
	offset += waves.position.xyz;
	#endif
	
	//Apply vertex displacements
	positionWS += offset;

	output.positionCS = TransformWorldToHClip(positionWS);
	half fogFactor = InitializeInputDataFog(float4(positionWS, 1.0), output.positionCS.z);

	output.screenPos = ComputeScreenPos(output.positionCS);
	
	output.normalWS = float4(normalInput.normalWS, positionWS.x);
#ifdef _NORMALMAP
	output.tangent = float4(normalInput.tangentWS, positionWS.y);
	output.bitangent = float4(normalInput.bitangentWS, positionWS.z);
#else
	output.positionWS = positionWS.xyz;
#endif

	//Lambert shading
	half3 vertexLight = 0;
#ifdef _ADDITIONAL_LIGHTS_VERTEX
	vertexLight = VertexLighting(positionWS, normalInput.normalWS);
#endif

	output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
	output.color = vertexColor;
	
	OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
	#ifdef DYNAMICLIGHTMAP_ON
	output.dynamicLightmapUV = input.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
	#endif
	OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
	VertexPositionInputs vertexInput = (VertexPositionInputs)0;
	vertexInput.positionWS = positionWS;
	vertexInput.positionCS = output.positionCS;
	output.shadowCoord = GetShadowCoord(vertexInput);
#endif

	return output;
}