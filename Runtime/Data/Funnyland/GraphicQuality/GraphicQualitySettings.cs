using System.Collections;
using System.Collections.Generic;
using SoFunny.Rendering.Funnyland;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SoFunny.Rendering.Funnyland {
    public static class GraphicQualitySettings {
        public static void SetQualityPrefab(GraphicQualitySettingData data) {
            var asset = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            ShadowSetting(asset, data.shadow);
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
    }
}
