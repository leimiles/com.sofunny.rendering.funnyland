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
            if (renderer is FunnylandMobileRenderer) {
                if (index < renderer.rendererFeatures.Count) {
                    renderer.rendererFeatures[index].SetActive(active);   
                }
                else {
                    Debug.Log("索引超出数组界限");
                }
            }
            else {
                Debug.Log("非FRP renderer 不支持此操作");
            }
        }
    }
}
