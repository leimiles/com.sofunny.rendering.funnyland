using SoFunny.Rendering.Funnyland;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SoFunny.Rendering.Funnyland
{
    public static class FunnyRendererGlobalSetting
    {
        static ScriptableRenderer GetRenderer() {
            UniversalRenderPipelineAsset asset = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            return asset.GetRenderer(0);
        }

        public static void SetFeatureActive(int index,bool active) {
            ScriptableRenderer renderer = GetRenderer();
            renderer.rendererFeatures[index].SetActive(active);
        }
    }
}
