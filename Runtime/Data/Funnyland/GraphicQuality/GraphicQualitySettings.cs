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
    public static class GraphicQualitySettings {
        public static void SetQualityPrefab(ref GraphicQualitySettingData data) {
            if (data == null) {
                return;
            }
            var asset = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            //ShadowSetting(asset, data.shadow);
            ShadingQuality();
            PostSetting(asset, data.post);
            DecalSetting();
            TextureQualitySetting((int)data.globalTextureMipmapLevel);
            AnisotropicTextureSetting(data.anisotropicTexture);
        }

        public static void ShadowSetting(UniversalRenderPipelineAsset assets, bool isShadow) {
            assets.supportsMainLightShadows = isShadow;
        }
        public static void ShadowSetting(bool isShadow) {
            var assets = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            assets.supportsMainLightShadows = isShadow;
        }
        
        public static void ShadingQuality() {

        }

        public static void PostSetting(UniversalRenderPipelineAsset assets, bool isPost) {
            assets.supportPost = isPost;
        }
        public static void PostSetting(bool isPost) {
            var assets = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            assets.supportPost = isPost;
        }
        
        public static void DecalSetting() {

        }

        public static void TextureQualitySetting(int level) {
            // 0:Full 1:half 2:Quarter 3:Eighth
            QualitySettings.globalTextureMipmapLimit = level;
        }

        public static void AnisotropicTextureSetting(bool isAnisotropicTexture) {
            if (isAnisotropicTexture) {
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
            } else {
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            }
        }

        /// <summary>
        /// 设置高配置数据
        /// </summary>
        public static void SetHighQualitySetting(ref GraphicQualitySettingData data) {
            data.post = true;
            data.shadow = true;
            data.globalTextureMipmapLevel = GlobalTextureMipmapLevel.Full;
            data.anisotropicTexture = true;
            SetQualityPrefab(ref data);
        }
        
        /// <summary>
        /// 设置低配置数据
        /// </summary>
        public static void SetLowQualitySetting(ref GraphicQualitySettingData data) {
            data.post = false;
            data.shadow = false;
            data.globalTextureMipmapLevel = GlobalTextureMipmapLevel.Half;
            data.anisotropicTexture = false;
            SetQualityPrefab(ref data);
        }

        /// <summary>
        /// 根据机型初始化默认配置以及设置高低配置
        /// </summary>
        public static void SetDefaultQualitySetting(GraphicQuality quality, ref GraphicQualitySettingData data) {
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
        
    }
}
