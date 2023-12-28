using System;
using UnityEditor;
using UnityEditor.Rendering.Universal;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SoFunny.Rendering.Funnyland
{
    internal class FunnyLitShader : BaseShaderGUI
    {
        static readonly string[] workflowModeNames = Enum.GetNames(typeof(FunnyLitGUI.WorkflowMode));

        private FunnyLitGUI.LitProperties litProperties;
        private bool isOpaque;
        
        // collect properties from the material properties
        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);
            litProperties = new FunnyLitGUI.LitProperties(properties);
        }

        // material changed check
        public override void ValidateMaterial(Material material)
        {
            // Setup blending - consistent across all Universal RP shaders
            SetupMaterialBlendModeInternal(material, out int renderQueue);

            // apply automatic render queue
            if ((renderQueue != material.renderQueue))
                material.renderQueue = renderQueue;

            // Cast Shadows
            bool castShadows = true;
            if (material.HasProperty(Property.CastShadows))
            {
                castShadows = (material.GetFloat(Property.CastShadows) != 0.0f);
            }
            else
            {
                // Lit.shader or Unlit.shader -- set based on transparency
                castShadows = LitGUI.IsOpaque(material);
            }
            material.SetShaderPassEnabled("ShadowCaster", castShadows);
            
            // Receive Shadows
            if (material.HasProperty(Property.ReceiveShadows))
                CoreUtils.SetKeyword(material, ShaderKeywordStrings._RECEIVE_SHADOWS_OFF, material.GetFloat(Property.ReceiveShadows) == 0.0f);


            // Setup double sided GI based on Cull state
            if (material.HasProperty(Property.CullMode))
                material.doubleSidedGI = (RenderFace)material.GetFloat(Property.CullMode) != RenderFace.Front;

            // Temporary fix for lightmapping. TODO: to be replaced with attribute tag.
            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", material.GetTexture("_BaseMap"));
                material.SetTextureScale("_MainTex", material.GetTextureScale("_BaseMap"));
                material.SetTextureOffset("_MainTex", material.GetTextureOffset("_BaseMap"));
            }
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", material.GetColor("_BaseColor"));
            

            // Normal Map
            if (material.HasProperty("_BumpMap"))
                CoreUtils.SetKeyword(material, ShaderKeywordStrings._NORMALMAP, material.GetTexture("_BumpMap"));
        }

        // material main surface options
        public override void DrawSurfaceOptions(Material material)
        {
            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;

            DoPopup(Styles.surfaceType, surfaceTypeProp, Styles.surfaceTypeNames);
            DoPopup(Styles.cullingText, cullingProp, Styles.renderFaceNames);

            //Transparent BlendMode 强制设置为 Alpha
            if (surfaceTypeProp != null)
            {
                if (surfaceTypeProp.floatValue == 1)
                {
                    if (blendModeProp != null)
                    {
                        blendModeProp.floatValue = 0;
                    }

                    if (preserveSpecProp != null)
                    {
                        preserveSpecProp.floatValue = 0;
                    }
                
                    if (receiveShadowsProp != null)
                    {
                        receiveShadowsProp.floatValue = 0;
                    }
                    
                    if (litProperties.zWrite != null)
                    {
                        if (isOpaque)
                        {
                            litProperties.zWrite.floatValue = 0;
                            isOpaque = false;
                        }
                        
                        DrawFloatToggleProperty(FunnyLitGUI.Styles.zWriteText, litProperties.zWrite);
                        // ZWriteControl
                        // 0:Auto 1:ForceEnable 2ForceDisable
                        zwriteProp.floatValue = litProperties.zWrite.floatValue == 1 ?  1 : 2;
                    }
                }
                else
                {
                    isOpaque = true;
                    if (receiveShadowsProp != null)
                    {
                        zwriteProp.floatValue = 0;
                        receiveShadowsProp.floatValue = 1;
                    }
                }
            }

            DrawFloatToggleProperty(FunnyLitGUI.Styles.ditherText, litProperties.dither);
            
            if (material.HasProperty("_Dither")) {
                CoreUtils.SetKeyword(material, "_DITHER_FADING_ON", 
                    material.GetFloat("_Dither") == 1.0f);
            }
            
            DrawFloatToggleProperty(Styles.alphaClipText, alphaClipProp);

            if ((alphaClipProp != null) && (alphaCutoffProp != null) && (alphaClipProp.floatValue == 1))
                materialEditor.ShaderProperty(alphaCutoffProp, Styles.alphaClipThresholdText, 1);
        }

        // material main surface inputs
        public override void DrawSurfaceInputs(Material material)
        {
            base.DrawSurfaceInputs(material);
            FunnyLitGUI.Inputs(litProperties, materialEditor, material);
            // DrawEmissionProperties(material, true);
            DrawTileOffset(materialEditor, baseMapProp);
        }

        // material main advanced options
        public override void DrawAdvancedOptions(Material material)
        {
            if (litProperties.reflections != null && litProperties.highlights != null)
            {
                materialEditor.ShaderProperty(litProperties.highlights, FunnyLitGUI.Styles.highlightsText);
                materialEditor.ShaderProperty(litProperties.reflections, FunnyLitGUI.Styles.reflectionsText);
            }

            base.DrawAdvancedOptions(material);
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            // _Emission property is lost after assigning Standard shader to the material
            // thus transfer it before assigning the new shader
            if (material.HasProperty("_Emission"))
            {
                material.SetColor("_EmissionColor", material.GetColor("_Emission"));
            }

            base.AssignNewShaderToMaterial(material, oldShader, newShader);

            if (oldShader == null || !oldShader.name.Contains("Legacy Shaders/"))
            {
                SetupMaterialBlendMode(material);
                return;
            }

            SurfaceType surfaceType = SurfaceType.Opaque;
            BlendMode blendMode = BlendMode.Alpha;
            if (oldShader.name.Contains("/Transparent/Cutout/"))
            {
                surfaceType = SurfaceType.Opaque;
                material.SetFloat("_AlphaClip", 1);
            }
            else if (oldShader.name.Contains("/Transparent/"))
            {
                // NOTE: legacy shaders did not provide physically based transparency
                // therefore Fade mode
                surfaceType = SurfaceType.Transparent;
                blendMode = BlendMode.Alpha;
            }
            material.SetFloat("_Blend", (float)blendMode);

            material.SetFloat("_Surface", (float)surfaceType);
            if (surfaceType == SurfaceType.Opaque)
            {
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            else
            {
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }

            if (oldShader.name.Equals("Standard (Specular setup)"))
            {
                material.SetFloat("_WorkflowMode", (float)FunnyLitGUI.WorkflowMode.Specular);
                Texture texture = material.GetTexture("_SpecGlossMap");
                if (texture != null)
                    material.SetTexture("_MetallicSpecGlossMap", texture);
            }
            else
            {
                material.SetFloat("_WorkflowMode", (float)FunnyLitGUI.WorkflowMode.Metallic);
                Texture texture = material.GetTexture("_MetallicGlossMap");
                if (texture != null)
                    material.SetTexture("_MetallicSpecGlossMap", texture);
            }
        }
    }
}
