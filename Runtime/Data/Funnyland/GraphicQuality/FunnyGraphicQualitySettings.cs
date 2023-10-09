using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using SoFunny.Rendering.Funnyland;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Debug = System.Diagnostics.Debug;

namespace SoFunny.Rendering.Funnyland {
    public enum GraphicQuality {
        High,
        Low,
        Custom
    }

    public static class FunnyGraphicQualitySettings {
        private static void SetQualityPrefab(ref FunnyGraphicQualitySettingData data) {
            if (data == null) {
                return;
            }
            var asset = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            RenderingSetting(asset, data.isSimpleRendering);
            ShadowSetting(asset, data.shadow);
            ShadingQuality(data.shaderQuality);
            PostSetting(asset, data.post);
            DecalSetting();
            TextureQualitySetting((int)data.globalTextureMipmapLevel);
            AnisotropicTextureSetting(data.anisotropicTexture);
        }
        private static void RenderingSetting(UniversalRenderPipelineAsset assets, bool isSimple) {
            assets.enalbeSimpleRendering = isSimple;
        }
        private static void RenderingSetting(bool isSimple) {
            var assets = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            assets.enalbeSimpleRendering = isSimple;
        }
        
        private static void ShadowSetting(UniversalRenderPipelineAsset assets, bool isShadow) {
            assets.supportsMainLightShadows = isShadow;
        }
        private static void ShadowSetting(bool isShadow) {
            var assets = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            assets.supportsMainLightShadows = isShadow;
        }
        
        // 目前是在FunnyRender.ChangeAssetSettings中强制设置 无法更改 更改方式只有控制isSimpleRender 在SimpleRender的情况下会关闭
        private static void CopyColorSetting(UniversalRenderPipelineAsset assets, bool isEnable) {
            assets.supportsCameraOpaqueTexture = isEnable;
        }
        private static void CopyColorSetting(bool isEnable) {
            var assets = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            assets.supportsCameraOpaqueTexture = isEnable;
        }
        
        // 目前是在FunnyRender.ChangeAssetSettings中强制设置 无法更改 更改方式只有控制isSimpleRender 在SimpleRender的情况下会关闭
        private static void CopyDepthSetting(UniversalRenderPipelineAsset assets, bool isEnable) {
            assets.supportsCameraDepthTexture = isEnable;
        }
        private static void CopyDepthSetting(bool isEnable) {
            var assets = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            assets.supportsCameraDepthTexture = isEnable;
        }

        #region shader表现设置
        private static void ShadingQuality(ShaderQuality shaderQuality) {
            SetReflectQuality(shaderQuality);
        }

        private static void SetReflectQuality(ShaderQuality shaderQuality) {
            if (shaderQuality == ShaderQuality.High) {
                Shader.EnableKeyword("_FRP_REFRACT");
            } else {
                Shader.DisableKeyword("_FRP_REFRACT");
            }
        }

        #endregion

        private static void PostSetting(UniversalRenderPipelineAsset assets, bool isPost) {
            assets.supportPost = isPost;
        }
        private static void PostSetting(bool isPost) {
            var assets = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            assets.supportPost = isPost;
        }
        
        private static void DecalSetting() {

        }

        private static void TextureQualitySetting(int level) {
            // 0:Full 1:half 2:Quarter 3:Eighth
            QualitySettings.globalTextureMipmapLimit = level;
        }

        private static void AnisotropicTextureSetting(bool isAnisotropicTexture) {
            if (isAnisotropicTexture) {
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
            } else {
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            }
        }

        /// <summary>
        /// 设置高配置数据
        /// </summary>
        private static void SetHighQualitySetting(ref FunnyGraphicQualitySettingData data) {
            data.post = true;
            data.shadow = true;
            data.globalTextureMipmapLevel = GlobalTextureMipmapLevel.Full;
            data.anisotropicTexture = true;
            SetQualityPrefab(ref data);
        }
        
        /// <summary>
        /// 设置低配置数据
        /// </summary>
        private static void SetLowQualitySetting(ref FunnyGraphicQualitySettingData data) {
            data.post = false;
            data.shadow = false;
            data.globalTextureMipmapLevel = GlobalTextureMipmapLevel.Half;
            data.anisotropicTexture = false;
            SetQualityPrefab(ref data);
        }

        /// <summary>
        /// 根据机型初始化默认配置以及设置高低配置
        /// </summary>
        private static void SetDefaultQualitySetting(GraphicQuality quality, ref FunnyGraphicQualitySettingData data) {
            switch (quality) {
                case GraphicQuality.High:
                    SetHighQualitySetting(ref data); 
                    break;
                case GraphicQuality.Low:
                    SetLowQualitySetting(ref data);
                    break;
                default:
                    SetQualityPrefab(ref data);
                    break;
            } 
        }
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        /// 不使用data 直接进行赋值 只需要一个高和低的默认配置即可
        /// <summary>
        /// 根据机型初始化默认配置以及设置高低配置
        /// </summary>
        public static void SetDefaultQualitySetting(GraphicQuality quality) {
            switch (quality) {
                case GraphicQuality.High:
                    SetHighQualitySetting(); 
                    break;
                case GraphicQuality.Low:
                    SetLowQualitySetting();
                    break;
                default:
                    break;
            } 
        }
        
        /// <summary>
        /// 设置高配置数据
        /// </summary>
        private static void SetHighQualitySetting() {
            var asset = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            RenderingSetting(asset, false);
            ShadowSetting(asset, true);
            CopyColorSetting(true);
            ShadingQuality(ShaderQuality.High);
            PostSetting(asset, true);
            DecalSetting();
            TextureQualitySetting((int)GlobalTextureMipmapLevel.Full);
            AnisotropicTextureSetting(true);
        }
        
        /// <summary>
        /// 设置低配置数据
        /// </summary>
        private static void SetLowQualitySetting() {
            var asset = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            RenderingSetting(asset, true);
            ShadowSetting(asset, true);
            CopyColorSetting(false);
            ShadingQuality(ShaderQuality.Low);
            PostSetting(asset, false);
            DecalSetting();
            TextureQualitySetting((int)GlobalTextureMipmapLevel.Half);
            AnisotropicTextureSetting(false);
        }
        
    }
}
