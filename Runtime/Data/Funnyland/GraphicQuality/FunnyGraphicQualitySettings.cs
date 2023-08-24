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
            ShadowSetting(asset, data.shadow);
            ShadingQuality();
            PostSetting(asset, data.post);
            DecalSetting();
            TextureQualitySetting((int)data.globalTextureMipmapLevel);
            AnisotropicTextureSetting(data.anisotropicTexture);
        }

        private static void ShadowSetting(UniversalRenderPipelineAsset assets, bool isShadow) {
            assets.supportsMainLightShadows = isShadow;
        }
        private static void ShadowSetting(bool isShadow) {
            var assets = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            assets.supportsMainLightShadows = isShadow;
        }
        
        private static void ShadingQuality() {

        }

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
        public static void SetHighQualitySetting() {
            var asset = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            ShadowSetting(asset, true);
            ShadingQuality();
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
            ShadowSetting(asset, false);
            ShadingQuality();
            PostSetting(asset, false);
            DecalSetting();
            TextureQualitySetting((int)GlobalTextureMipmapLevel.Half);
            AnisotropicTextureSetting(false);
        }
        
    }
}
