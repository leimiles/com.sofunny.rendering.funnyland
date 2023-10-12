Shader "SoFunny/Funnyland/FunnyWater"
{
    Properties
    {
        //Rendering
        [Header(General)]
        [Space(5)]
        [MaterialEnum(Mesh UV,0,World XZ projected ,1)]_WorldSpaceUV("UV Coordinates", Float) = 1
        _Speed("Animation Speed", Float) = 1
        _Direction("Animation direction", Vector) = (0,-1,0,0)

        [Header(Lighting)]
        [Space(5)]
        [ToggleOff(_UNLIT)] _LightingOn("Enable lighting", Float) = 1
        [ToggleOff(_RECEIVE_SHADOWS_OFF)] _ReceiveShadows("Recieve Shadows", Float) = 1
        _ShadowStrength("Shadow Strength", Range(0 , 1)) = 1

        //Color + Transparency
        [Header(WaterColor)]
        [Space(5)]
        [HDR]_BaseColor("Deep", Color) = (0, 0.44, 0.62, 1)
        [HDR]_ShallowColor("Shallow", Color) = (0.1, 0.9, 0.89, 0.02)

        _DepthVertical("View Depth", Range(0.01 , 16)) = 4
        _DepthHorizontal("Vertical Height Depth", Range(0.01 , 8)) = 1
        [Toggle] _DepthExp("Exponential Blend", Float) = 1

        [PowerSlider(3)] _ColorAbsorption("Color Absorption", Range(0 , 1)) = 0
        [Toggle] _VertexColorDepth("Vertex color (G) depth", Float) = 0
        _EdgeFade("Edge Fade", Float) = 0.1

        [HDR]_HorizonColor("Horizon", Color) = (0.84, 1, 1, 0.15)
        _HorizonDistance("Horizon Distance", Range(0.01 , 32)) = 8
        _WaveTint("Wave tint", Range( -0.1 , 0.1)) = 0

        //Normals
        [Header(WaterNormal)]
        [Space(5)]
        [Toggle(_NORMALMAP)] _NormalMapOn("Enable Normal maps", Float) = 1
        _NormalStrength("Strength", Range(0 , 1)) = 0.135
        [NoScaleOffset][Normal][SingleLineTexture]_BumpMap("Normals", 2D) = "bump" {}
        _NormalTiling("Tiling", Float) = 1
        _NormalSubTiling("Tiling (sub-layer)", Float) = 0.5
        _NormalSpeed("Speed multiplier", Float) = 0.2
        _NormalSubSpeed("Speed multiplier (sub-layer)", Float) = -0.5

        //Underwater
        [Header(Underwater)]
        [Space(5)]
        [Toggle(_CAUSTICS)] _CausticsOn("Enable Caustics", Float) = 1
        [NoScaleOffset][SingleLineTexture]_CausticsTex("Caustics RGB", 2D) = "black" {}
        _CausticsBrightness("Brightness", Float) = 2
        _CausticsDistortion("Distortion", Range(0, 1)) = 0.15
        _CausticsTiling("Tiling", Float) = 0.5
        _CausticsSpeed("Speed multiplier", Float) = 0.1

        _UnderwaterSurfaceSmoothness("Underwater Surface Smoothness", Range(0, 1)) = 0.8
        _UnderwaterRefractionOffset("Underwater Refraction Offset", Range(0, 1)) = 0.2

        [Toggle(_WATER_REFRACTION)] _RefractionOn("Enable Refraction", Float) = 1
        _RefractionStrength("Refraction Strength", Range(0, 1)) = 0.1
        _RefractionChromaticAberration("Refraction Chromatic Aberration)", Range(0, 1)) = 1

        //Intersection Foam
        [Header(Intersection Foam)]
        [Space(5)]
        [MaterialEnum(Depth Texture,0,Vertex Color (R),1,Depth Texture and Vertex Color,2)] _IntersectionSource("Intersection source", Float) = 0

        [NoScaleOffset][SingleLineTexture]_IntersectionNoise("Intersection noise", 2D) = "white" {}
        [hdr]_IntersectionColor("Color", Color) = (1,1,1,1)
        _IntersectionLength("Distance", Range(0.01 , 5)) = 2
        _IntersectionFalloff("Falloff", Range(0.01 , 1)) = 0.5
        _IntersectionTiling("Noise Tiling", float) = 0.2
        _IntersectionSpeed("Speed multiplier", float) = 0.1
        _IntersectionClipping("Cutoff", Range(0.01, 1)) = 0.5
        _IntersectionRippleDist("Ripple distance", float) = 32
        _IntersectionRippleStrength("Ripple Strength", Range(0 , 1)) = 0.5

        //Surface Foam
        [Header(Surface Foam)]
        [Space(5)]
        [Toggle(_FOAM)] _FoamOn("Enable Foam", Float) = 1
        [NoScaleOffset][SingleLineTexture]_FoamTex("Foam Mask", 2D) = "black" {}
        _FoamBaseAmount("Base amount", Range(0 , 1)) = 0
        _FoamClipping("Clipping", Range(0 , 0.999)) = 0
        [Toggle] _VertexColorFoam("Vertex color (A) foam", Float) = 0
        [HDR]_FoamColor("Color", Color) = (1,1,1,1)
        _FoamWaveAmount("Wave crest amount", Range(0 , 2)) = 0
        _FoamTiling("Tiling", float) = 0.1
        _FoamSubTiling("Tiling (sub-layer)", float) = 0.5
        _FoamSpeed("Speed multiplier", float) = 0.1
        _FoamSubSpeed("Speed multiplier (sub-layer)", float) = -0.25
        _FoamDistortion("Offset distortion", Range(0, 3)) = 0.1

        //Light Reflections
        [Header(Light Reflections)]
        [Space(5)]
        [ToggleOff(_SPECULARHIGHLIGHTS_OFF)] _SpecularReflectionsOn("Enable Specular", Float) = 1
        _SunReflectionStrength("Sun Strength", Float) = 10
        [PowerSlider(0.1)] _SunReflectionSize("Sun Size", Range(0 , 1)) = 0.5
        _SunReflectionDistortion("Sun Distortion", Range(0 ,2)) = 0.49

        //World Reflections
        [Header(World Reflections)]
        [Space(5)]
        [ToggleOff(_ENVIRONMENTREFLECTIONS_OFF)] _EnvironmentReflectionsOn("Enable Environment Reflections", Float) = 1
        _ReflectionStrength("Strength", Range(0, 1)) = 1
        _ReflectionLighting("Lighting influence", Range(0, 1)) = 0
        _ReflectionFresnel("Curvature mask", Range(0.01, 20)) = 5
        _ReflectionDistortion("Distortion", Range(0, 1)) = 0.05
        _ReflectionBlur("Blur", Range(0, 1)) = 0

        //Waves
        [Header(Waves)]
        [Space(5)]
        [Toggle(_WAVES)] _WavesOn("Enable Waves", Float) = 0
        _WaveSpeed("Speed", Float) = 2
        [Toggle] _VertexColorWaveFlattening("Vertex color (B) wave flattening", Float) = 0
        _WaveHeight("Height", Range(0 , 10)) = 0.25
        _WaveCount("Count", Range(1 , 5)) = 1
        _WaveDirection("Direction", vector) = (1,1,1,1)
        _WaveDistance("Distance", Range(0 , 1)) = 0.8
        _WaveSteepness("Steepness", Range(0 , 5)) = 0.1
        _WaveNormalStr("Normal Strength", Range(0 , 32)) = 0.5
        _WaveFadeDistance("Wave fade distance (Start/End)", Vector) = (150, 300, 0, 0)

        //Keyword states
        [Header(Rendering)]
        [Space(5)]
        [Toggle(_DISABLE_DEPTH_TEX)] _DisableDepthTexture("Disable depth texture", Float) = 0
        [MainTexture] [HideInInspector] _BaseMap("Albedo", 2D) = "white" {}
        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "FunnyLandMobileForward"
            }

            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite off
            Cull back
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #pragma multi_compile _ _FORWARD_PLUS
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            // Universal Pipeline keywords
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
            #pragma multi_compile_fragment _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTERED_RENDERING
            #pragma multi_compile_fragment _ _FRP_HIGH_SHADER_QUALITY

            //Unity defined keywords
            #pragma multi_compile_fog
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON

            // Defines
            #define _SURFACE_TYPE_TRANSPARENT 1
            #define _SHARP_INERSECTION 1
            #define UnityFog 1
            #define _ADVANCED_SHADING 1

            // Material Keywords
            // #define _NORMALMAP 1
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _WAVES
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local_fragment _DISABLE_DEPTH_TEX
            #pragma shader_feature_local_fragment _WATER_REFRACTION
            #pragma shader_feature_local_fragment _ADVANCED_SHADING
            #pragma shader_feature_local_fragment _UNLIT
            #pragma shader_feature_local_fragment _CAUSTICS
            #pragma shader_feature_local_fragment _FOAM
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            #pragma shader_feature_local_fragment _ _SHARP_INERSECTION _SMOOTH_INTERSECTION
            
            #if _ADVANCED_SHADING
            #define RESAMPLE_REFRACTION_DEPTH 1
            #define PHYSICAL_REFRACTION 1

            // 开启高品质渲染会关闭折射效果
            #if _WATER_REFRACTION && _FRP_HIGH_SHADER_QUALITY
                #define _REFRACTION 1
            #endif
            
            #if _REFRACTION //Requires opaque texture
                #define COLOR_ABSORPTION 1
            #endif

            //Mask caustics by shadows cast on scene geometry. Doubles the shadow sampling cost
            #if _CAUSTICS && defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                #define SCENE_SHADOWMASK 1
            #endif

            #if !_DISABLE_DEPTH_TEX && _CAUSTICS
                //Compose a mask for pixels against the skybox
                #define DEPTH_MASK 1
            #endif
            #endif

            #include "Packages/com.unity.render-pipelines.universal/Shaders/Funnyland/Libs/FunnyWater/URP.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Funnyland/Libs/FunnyWater/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Funnyland/Libs/FunnyWater/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Funnyland/Libs/FunnyWater/Waves.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Funnyland/Libs/FunnyWater/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Funnyland/Libs/FunnyWater/Features.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Funnyland/Libs/FunnyWater/Foam.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Funnyland/Libs/FunnyWater/Caustics.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Funnyland/Libs/FunnyWater/Vertex.hlsl"

            #pragma vertex LitPassVertex
            #pragma fragment ForwardPassFragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Funnyland/Libs/FunnyWater/ForwardPass.hlsl"
            ENDHLSL
        }
    }
}