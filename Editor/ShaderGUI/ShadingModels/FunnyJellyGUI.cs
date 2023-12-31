using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Universal.ShaderGUI
{
    /// <summary>
    /// Editor script for the Lit material inspector.
    /// </summary>
    public static class FunnyJellyGUI
    {
        /// <summary>
        /// Workflow modes for the shader.
        /// </summary>
        public enum WorkflowMode
        {
            /// <summary>
            /// Use this for specular workflow.
            /// </summary>
            Specular = 0,

            /// <summary>
            /// Use this for metallic workflow.
            /// </summary>
            Metallic
        }

        /// <summary>
        /// Options to select the texture channel where the smoothness value is stored.
        /// </summary>
        public enum SmoothnessMapChannel
        {
            /// <summary>
            /// Use this when smoothness is stored in the alpha channel of the Specular/Metallic Map.
            /// </summary>
            SpecularMetallicAlpha,

            /// <summary>
            /// Use this when smoothness is stored in the alpha channel of the Albedo Map.
            /// </summary>
            AlbedoAlpha,
        }

        /// <summary>
        /// Container for the text and tooltips used to display the shader.
        /// </summary>
        public static class Styles
        {
            /// <summary>
            /// The text and tooltip for the workflow Mode GUI.
            /// </summary>
            public static GUIContent workflowModeText = EditorGUIUtility.TrTextContent("Workflow Mode",
                "Select a workflow that fits your textures. Choose between Metallic or Specular.");
            
            public static GUIContent zWriteText = EditorGUIUtility.TrTextContent("Force ZWrite","是否强制开启深度写入");

            /// <summary>
            /// The text and tooltip for the specular Map GUI.
            /// </summary>
            public static GUIContent specularMapText =
                EditorGUIUtility.TrTextContent("Specular Map", "Designates a Specular Map and specular color determining the apperance of reflections on this Material's surface.");

            /// <summary>
            /// The text and tooltip for the metallic Map GUI.
            /// </summary>
            public static GUIContent metallicMapText =
                EditorGUIUtility.TrTextContent("MixMap(R:Metallic G:AO B:Roughness)", "MixMap R:Metallic G:AO B:Roughness.");

            /// <summary>
            /// The text and tooltip for the smoothness GUI.
            /// </summary>
            public static GUIContent roughnessOffsetText = EditorGUIUtility.TrTextContent("Roughness Offset",
                "Controls the Roughness Offset.");
            
            public static GUIContent metallicOffsetText = EditorGUIUtility.TrTextContent("Metallic Offset",
                "Controls the Metallic Offset.");

            /// <summary>
            /// The text and tooltip for the smoothness source GUI.
            /// </summary>
            public static GUIContent smoothnessMapChannelText =
                EditorGUIUtility.TrTextContent("Source",
                    "Specifies where to sample a smoothness map from. By default, uses the alpha channel for your map.");

            /// <summary>
            /// The text and tooltip for the specular Highlights GUI.
            /// </summary>
            public static GUIContent highlightsText = EditorGUIUtility.TrTextContent("Specular Highlights",
                "When enabled, the Material reflects the shine from direct lighting.");

            /// <summary>
            /// The text and tooltip for the environment Reflections GUI.
            /// </summary>
            public static GUIContent reflectionsText =
                EditorGUIUtility.TrTextContent("Environment Reflections",
                    "When enabled, the Material samples reflections from the nearest Reflection Probes or Lighting Probe.");

            /// <summary>
            /// The text and tooltip for the height map GUI.
            /// </summary>
            public static GUIContent heightMapText = EditorGUIUtility.TrTextContent("Height Map",
                "Defines a Height Map that will drive a parallax effect in the shader making the surface seem displaced.");

            /// <summary>
            /// The text and tooltip for the occlusion map GUI.
            /// </summary>
            public static GUIContent occlusionText = EditorGUIUtility.TrTextContent("Occlusion Map",
                "Sets an occlusion map to simulate shadowing from ambient lighting.");

            /// <summary>
            /// The names for smoothness alpha options available for metallic workflow.
            /// </summary>
            public static readonly string[] metallicSmoothnessChannelNames = { "Metallic Alpha", "Albedo Alpha" };

            /// <summary>
            /// The names for smoothness alpha options available for specular workflow.
            /// </summary>
            public static readonly string[] specularSmoothnessChannelNames = { "Specular Alpha", "Albedo Alpha" };

            /// <summary>
            /// The text and tooltip for the enabling/disabling clear coat GUI.
            /// </summary>
            public static GUIContent clearCoatText = EditorGUIUtility.TrTextContent("Clear Coat",
                "A multi-layer material feature which simulates a thin layer of coating on top of the surface material." +
                "\nPerformance cost is considerable as the specular component is evaluated twice, once per layer.");

            /// <summary>
            /// The text and tooltip for the clear coat Mask GUI.
            /// </summary>
            public static GUIContent clearCoatMaskText = EditorGUIUtility.TrTextContent("Mask",
                "Specifies the amount of the coat blending." +
                "\nActs as a multiplier of the clear coat map mask value or as a direct mask value if no map is specified." +
                "\nThe map specifies clear coat mask in the red channel and clear coat smoothness in the green channel.");

            /// <summary>
            /// The text and tooltip for the clear coat smoothness GUI.
            /// </summary>
            public static GUIContent clearCoatSmoothnessText = EditorGUIUtility.TrTextContent("Smoothness",
                "Specifies the smoothness of the coating." +
                "\nActs as a multiplier of the clear coat map smoothness value or as a direct smoothness value if no map is specified.");
            
            public static GUIContent thicknessMapText = EditorGUIUtility.TrTextContent("ThicknessMap",
                "厚度图.");
            public static GUIContent subsurfaceColorText = EditorGUIUtility.TrTextContent("Subsurface Color",
                "次表面散射颜色控制.");
            public static GUIContent subsurfaceIntensityText = EditorGUIUtility.TrTextContent("Subsurface Intensity",
                "次表面散射强度控制.");
            public static GUIContent transmissionText = EditorGUIUtility.TrTextContent("Transmission",
                "通透度控制.");
            public static GUIContent thicknessOffsetText = EditorGUIUtility.TrTextContent("Thickness Offset",
                "厚度偏移控制.");
            
            public static GUIContent useRefractText = EditorGUIUtility.TrTextContent("Use Refract",
                "启用折射.");
            
            public static GUIContent refractIntensityText = EditorGUIUtility.TrTextContent("Refract Intensity",
                "折射强度控制");
            
        }

        /// <summary>
        /// Container for the properties used in the <c>LitGUI</c> editor script.
        /// </summary>
        public struct LitProperties
        {
            // Surface Option Props

            /// <summary>
            /// The MaterialProperty for workflow mode.
            /// </summary>
            public MaterialProperty workflowMode;
            
            /// <summary>
            /// 深度写入参数控制
            /// </summary>
            public MaterialProperty zWrite;

            // Surface Input Props

            /// <summary>
            /// The MaterialProperty for metallic value.
            /// </summary>
            public MaterialProperty metallic;

            /// <summary>
            /// The MaterialProperty for specular color.
            /// </summary>
            public MaterialProperty specColor;

            /// <summary>
            /// The MaterialProperty for metallic Smoothness map.
            /// </summary>
            public MaterialProperty metallicGlossMap;

            /// <summary>
            /// The MaterialProperty for specular smoothness map.
            /// </summary>
            public MaterialProperty specGlossMap;

            /// <summary>
            /// The MaterialProperty for smoothness value.
            /// </summary>
            public MaterialProperty smoothness;

            /// <summary>
            /// The MaterialProperty for smoothness alpha channel.
            /// </summary>
            public MaterialProperty smoothnessMapChannel;

            /// <summary>
            /// The MaterialProperty for normal map.
            /// </summary>
            public MaterialProperty bumpMapProp;

            /// <summary>
            /// The MaterialProperty for normal map scale.
            /// </summary>
            public MaterialProperty bumpScaleProp;

            /// <summary>
            /// The MaterialProperty for height map.
            /// </summary>
            public MaterialProperty parallaxMapProp;

            /// <summary>
            /// The MaterialProperty for height map scale.
            /// </summary>
            public MaterialProperty parallaxScaleProp;

            /// <summary>
            /// The MaterialProperty for occlusion strength.
            /// </summary>
            public MaterialProperty occlusionStrength;

            /// <summary>
            /// The MaterialProperty for occlusion map.
            /// </summary>
            public MaterialProperty occlusionMap;


            // Advanced Props

            /// <summary>
            /// The MaterialProperty for specular highlights.
            /// </summary>
            public MaterialProperty highlights;

            /// <summary>
            /// The MaterialProperty for environment reflections.
            /// </summary>
            public MaterialProperty reflections;

            /// <summary>
            /// The MaterialProperty for enabling/disabling clear coat.
            /// </summary>
            public MaterialProperty clearCoat; // Enable/Disable dummy property

            /// <summary>
            /// The MaterialProperty for clear coat map.
            /// </summary>
            public MaterialProperty clearCoatMap;

            /// <summary>
            /// The MaterialProperty for clear coat mask.
            /// </summary>
            public MaterialProperty clearCoatMask;

            /// <summary>
            /// The MaterialProperty for clear coat smoothness.
            /// </summary>
            public MaterialProperty clearCoatSmoothness;
            
            
            // _ThickScale("Thick Scale", Range(0, 5)) = 1
            // _SubsurfaceColor("Subsurface Color", Color) = (1, 1, 1, 1)
            // _SubsurfaceControlMap("R:Thickness G:AlphaScale", 2D) = "white"{} //用于控制散射的强度
            // _SubsurfaceDistortion("Subsurface Distortion", Range(0, 1)) = 0.5
            // _SubsurfacePower("Subsurface Power", Range(0.001, 50)) = 1
            // _SubsurfaceScale("Subsurface Scale", Range(0, 3)) = 1

            public MaterialProperty subsurfaceColor;
            public MaterialProperty subsurfaceIntensity;
            public MaterialProperty thicknessMap;
            public MaterialProperty transmission;
            public MaterialProperty thicknessOffset;
            public MaterialProperty useRefract;
            public MaterialProperty refractIntensity;

            /// <summary>
            /// Constructor for the <c>LitProperties</c> container struct.
            /// </summary>
            /// <param name="properties"></param>
            public LitProperties(MaterialProperty[] properties)
            {
                // Surface Option Props
                workflowMode = BaseShaderGUI.FindProperty("_WorkflowMode", properties, false);
                zWrite = BaseShaderGUI.FindProperty("_ZWrite", properties, false);
                // Surface Input Props
                metallic = BaseShaderGUI.FindProperty("_MetallicOffset", properties);
                specColor = BaseShaderGUI.FindProperty("_SpecColor", properties, false);
                metallicGlossMap = BaseShaderGUI.FindProperty("_MixMap", properties);
                specGlossMap = BaseShaderGUI.FindProperty("_SpecGlossMap", properties, false);
                smoothness = BaseShaderGUI.FindProperty("_RoughnessOffset", properties, false);
                smoothnessMapChannel = BaseShaderGUI.FindProperty("_SmoothnessTextureChannel", properties, false);
                bumpMapProp = BaseShaderGUI.FindProperty("_BumpMap", properties, false);
                bumpScaleProp = BaseShaderGUI.FindProperty("_BumpScale", properties, false);
                parallaxMapProp = BaseShaderGUI.FindProperty("_ParallaxMap", properties, false);
                parallaxScaleProp = BaseShaderGUI.FindProperty("_Parallax", properties, false);
                occlusionStrength = BaseShaderGUI.FindProperty("_OcclusionStrength", properties, false);
                occlusionMap = BaseShaderGUI.FindProperty("_OcclusionMap", properties, false);
                
                //Subsurface Input Porps
                subsurfaceColor = BaseShaderGUI.FindProperty("_SubsurfaceColor", properties);
                thicknessMap = BaseShaderGUI.FindProperty("_ThicknessMap", properties);
                transmission = BaseShaderGUI.FindProperty("_Transmission", properties);
                thicknessOffset = BaseShaderGUI.FindProperty("_ThicknessOffset", properties);
                useRefract = BaseShaderGUI.FindProperty("_UseRefract", properties);
                subsurfaceIntensity = BaseShaderGUI.FindProperty("_SubsurfaceIntensity", properties);
                refractIntensity = BaseShaderGUI.FindProperty("_RefractIntensity", properties);
                
                // Advanced Props
                highlights = BaseShaderGUI.FindProperty("_SpecularHighlights", properties, false);
                reflections = BaseShaderGUI.FindProperty("_EnvironmentReflections", properties, false);

                clearCoat = BaseShaderGUI.FindProperty("_ClearCoat", properties, false);
                clearCoatMap = BaseShaderGUI.FindProperty("_ClearCoatMap", properties, false);
                clearCoatMask = BaseShaderGUI.FindProperty("_ClearCoatMask", properties, false);
                clearCoatSmoothness = BaseShaderGUI.FindProperty("_ClearCoatSmoothness", properties, false);
            }
        }

        /// <summary>
        /// Draws the surface inputs GUI.
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="materialEditor"></param>
        /// <param name="material"></param>
        public static void Inputs(LitProperties properties, MaterialEditor materialEditor, Material material)
        {
            DoMetallicSpecularArea(properties, materialEditor, material);
            BaseShaderGUI.DrawNormalArea(materialEditor, properties.bumpMapProp, properties.bumpScaleProp);
            DoSubsurfaceArea(properties, materialEditor, material);
        }

        public static void DrawAdvancedOptions(LitProperties properties, MaterialEditor materialEditor, Material material)
        {
            materialEditor.ShaderProperty(properties.useRefract, Styles.useRefractText);

            if (material.HasProperty("_UseRefract"))
            {
                CoreUtils.SetKeyword(material, "_USE_REFRACT",material.GetFloat("_UseRefract") == 1.0f);
            }
        }

        private static bool ClearCoatAvailable(Material material)
        {
            return material.HasProperty("_ClearCoat")
                   && material.HasProperty("_ClearCoatMap")
                   && material.HasProperty("_ClearCoatMask")
                   && material.HasProperty("_ClearCoatSmoothness");
        }

        private static bool HeightmapAvailable(Material material)
        {
            return material.HasProperty("_Parallax")
                   && material.HasProperty("_ParallaxMap");
        }

        private static void DoHeightmapArea(LitProperties properties, MaterialEditor materialEditor)
        {
            materialEditor.TexturePropertySingleLine(Styles.heightMapText, properties.parallaxMapProp,
                properties.parallaxMapProp.textureValue != null ? properties.parallaxScaleProp : null);
        }

        private static bool ClearCoatEnabled(Material material)
        {
            return material.HasProperty("_ClearCoat") && material.GetFloat("_ClearCoat") > 0.0;
        }

        /// <summary>
        /// Draws the clear coat GUI.
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="materialEditor"></param>
        /// <param name="material"></param>
        public static void DoClearCoat(LitProperties properties, MaterialEditor materialEditor, Material material)
        {
            materialEditor.ShaderProperty(properties.clearCoat, Styles.clearCoatText);
            var coatEnabled = material.GetFloat("_ClearCoat") > 0.0;

            EditorGUI.BeginDisabledGroup(!coatEnabled);
            {
                EditorGUI.indentLevel += 2;
                materialEditor.TexturePropertySingleLine(Styles.clearCoatMaskText, properties.clearCoatMap, properties.clearCoatMask);

                // Texture and HDR color controls
                materialEditor.ShaderProperty(properties.clearCoatSmoothness, Styles.clearCoatSmoothnessText);

                EditorGUI.indentLevel -= 2;
            }
            EditorGUI.EndDisabledGroup();
        }

        /// <summary>
        /// Draws the metallic/specular area GUI.
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="materialEditor"></param>
        /// <param name="material"></param>
        public static void DoMetallicSpecularArea(LitProperties properties, MaterialEditor materialEditor, Material material)
        {
            materialEditor.TexturePropertySingleLine(Styles.metallicMapText, properties.metallicGlossMap);

            DoMetallicOffset(materialEditor, material, properties.metallic);
            DoRoughness(materialEditor, material, properties.smoothness);
        }

        public static void DoSubsurfaceArea(LitProperties properties, MaterialEditor materialEditor, Material material)
        {
            materialEditor.TexturePropertySingleLine(Styles.thicknessMapText, properties.thicknessMap);
            
            EditorGUI.indentLevel += 2;
            materialEditor.ShaderProperty(properties.thicknessOffset, Styles.thicknessOffsetText);
            materialEditor.ShaderProperty(properties.subsurfaceColor, Styles.subsurfaceColorText);
            materialEditor.ShaderProperty(properties.subsurfaceIntensity, Styles.subsurfaceIntensityText);
            materialEditor.ShaderProperty(properties.refractIntensity, Styles.refractIntensityText);
            materialEditor.ShaderProperty(properties.transmission, Styles.transmissionText);
            EditorGUI.indentLevel -= 2;
        }

        internal static bool IsOpaque(Material material)
        {
            bool opaque = true;
            if (material.HasProperty(Property.SurfaceType))
                opaque = ((BaseShaderGUI.SurfaceType)material.GetFloat(Property.SurfaceType) == BaseShaderGUI.SurfaceType.Opaque);
            return opaque;
        }
        
        /// <summary>
        /// Draws the smoothness GUI.
        /// </summary>
        /// <param name="materialEditor"></param>
        /// <param name="material"></param>
        /// <param name="metallicOffset"></param>
        /// <param name="smoothnessChannelNames"></param>
        public static void DoMetallicOffset(MaterialEditor materialEditor, Material material, MaterialProperty metallicOffset)
        {
            EditorGUI.indentLevel += 2;
            materialEditor.ShaderProperty(metallicOffset, Styles.metallicOffsetText);
            EditorGUI.indentLevel -= 2;
        }

        /// <summary>
        /// Draws the smoothness GUI.
        /// </summary>
        /// <param name="materialEditor"></param>
        /// <param name="material"></param>
        /// <param name="smoothness"></param>
        /// <param name="smoothnessMapChannel"></param>
        /// <param name="smoothnessChannelNames"></param>
        public static void DoRoughness(MaterialEditor materialEditor, Material material, MaterialProperty smoothness)
        {
            EditorGUI.indentLevel += 2;

            materialEditor.ShaderProperty(smoothness, Styles.roughnessOffsetText);

            EditorGUI.indentLevel -= 2;
        }

        /// <summary>
        /// Retrieves the alpha channel used for smoothness.
        /// </summary>
        /// <param name="material"></param>
        /// <returns>The Alpha channel used for Smoothness.</returns>
        public static SmoothnessMapChannel GetSmoothnessMapChannel(Material material)
        {
            int ch = (int)material.GetFloat("_SmoothnessTextureChannel");
            if (ch == (int)SmoothnessMapChannel.AlbedoAlpha)
                return SmoothnessMapChannel.AlbedoAlpha;

            return SmoothnessMapChannel.SpecularMetallicAlpha;
        }

        // (shared by all lit shaders, including shadergraph Lit Target and Lit.shader)
        internal static void SetupSpecularWorkflowKeyword(Material material, out bool isSpecularWorkflow)
        {
            isSpecularWorkflow = false; // default is metallic workflow
            if (material.HasProperty(Property.SpecularWorkflowMode))
                isSpecularWorkflow = ((WorkflowMode)material.GetFloat(Property.SpecularWorkflowMode)) == WorkflowMode.Specular;
            CoreUtils.SetKeyword(material, "_SPECULAR_SETUP", isSpecularWorkflow);
        }

        /// <summary>
        /// Sets up the keywords for the Lit shader and material.
        /// </summary>
        /// <param name="material"></param>
        public static void SetMaterialKeywords(Material material)
        {
            if (material.HasProperty("_SpecularHighlights"))
                CoreUtils.SetKeyword(material, "_SPECULARHIGHLIGHTS_OFF",
                    material.GetFloat("_SpecularHighlights") == 0.0f);
            if (material.HasProperty("_EnvironmentReflections"))
                CoreUtils.SetKeyword(material, "_ENVIRONMENTREFLECTIONS_OFF",
                    material.GetFloat("_EnvironmentReflections") == 0.0f);
            if (material.HasProperty("_OcclusionMap"))
                CoreUtils.SetKeyword(material, "_OCCLUSIONMAP", material.GetTexture("_OcclusionMap"));

            if (material.HasProperty("_ParallaxMap"))
                CoreUtils.SetKeyword(material, "_PARALLAXMAP", material.GetTexture("_ParallaxMap"));

            if (material.HasProperty("_SmoothnessTextureChannel"))
            {
                var opaque = IsOpaque(material);
                CoreUtils.SetKeyword(material, "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A",
                    GetSmoothnessMapChannel(material) == SmoothnessMapChannel.AlbedoAlpha && opaque);
            }

            // Clear coat keywords are independent to remove possiblity of invalid combinations.
            if (ClearCoatEnabled(material))
            {
                var hasMap = material.HasProperty("_ClearCoatMap") && material.GetTexture("_ClearCoatMap") != null;
                if (hasMap)
                {
                    CoreUtils.SetKeyword(material, "_CLEARCOAT", false);
                    CoreUtils.SetKeyword(material, "_CLEARCOATMAP", true);
                }
                else
                {
                    CoreUtils.SetKeyword(material, "_CLEARCOAT", true);
                    CoreUtils.SetKeyword(material, "_CLEARCOATMAP", false);
                }
            }
            else
            {
                CoreUtils.SetKeyword(material, "_CLEARCOAT", false);
                CoreUtils.SetKeyword(material, "_CLEARCOATMAP", false);
            }
        }
    }
}