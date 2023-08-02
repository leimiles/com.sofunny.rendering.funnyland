using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal.Internal;
using UnityEngine.Rendering.Universal;

namespace SoFunny.Rendering.Funnyland {
    public class FunnylandMobileRenderer : ScriptableRenderer {
#if UNITY_SWITCH || UNITY_ANDROID
        const GraphicsFormat k_DepthStencilFormat = GraphicsFormat.D24_UNorm_S8_UInt;
        const int k_DepthBufferBits = 24;
#else
        const GraphicsFormat k_DepthStencilFormat = GraphicsFormat.D32_SFloat_S8_UInt;
        const int k_DepthBufferBits = 32;
#endif
        StencilState m_DefaultStencilState;
        static class ProfilerSamplerString {
            public static readonly string drawOpaqueForwardPass = "Draw Opaque Forward Pass";
            public static readonly string drawTransparentForwardPass = "Draw Transparent Forward Pass";
        }
        DrawObjectsPass m_RenderOpaqueForwardPass;
        DrawObjectsPass m_RenderTransparentForwardPass;
        DrawSkyboxPass m_DrawSkyboxPass;
        bool m_DepthPrimingRecommended;
        CopyDepthMode m_CopyDepthMode;
        RenderingMode m_RenderingMode;
        bool m_Clustering;
        ForwardLights m_ForwardLights;
        LightCookieManager m_LightCookieManager;
        public FunnylandMobileRenderer(FunnylandMobileRendererData data) : base(data) {
            Application.targetFrameRate = 60;
            ProjectSettingMobile();
            StencilStateData stencilData = data.defaultStencilState;
            SetDefaultStencilState(stencilData);

            if (UniversalRenderPipeline.asset?.supportsLightCookies ?? false) {
                var settings = LightCookieManager.Settings.Create();
                var asset = UniversalRenderPipeline.asset;
                if (asset) {
                    settings.atlas.format = asset.additionalLightsCookieFormat;
                    settings.atlas.resolution = asset.additionalLightsCookieResolution;
                }
                m_LightCookieManager = new LightCookieManager(ref settings);
            }

            this.stripShadowsOffVariants = true;
            this.stripAdditionalLightOffVariants = true;
#if ENABLE_VR && ENABLE_VR_MODULE
#if PLATFORM_WINRT || PLATFORM_ANDROID
            // AdditionalLightOff variant is available on HL&Quest platform due to performance consideration.
            this.stripAdditionalLightOffVariants = !PlatformAutoDetect.isXRMobile;
#endif
#endif

            ForwardLights.InitParams forwardInitParams;
            forwardInitParams.forwardPlus = true;
            forwardInitParams.lightCookieManager = m_LightCookieManager;
            m_ForwardLights = new ForwardLights(forwardInitParams);
            //m_Clustering = true;
            //this.m_RenderingMode = RenderingMode.ForwardPlus;
            //this.m_CopyDepthMode = CopyDepthMode.AfterOpaques;
            //this.m_DepthPrimingRecommended = false;

            m_RenderOpaqueForwardPass = new DrawObjectsPass(ProfilerSamplerString.drawOpaqueForwardPass, data.shaderTagIds, true, RenderPassEvent.BeforeRenderingOpaques, RenderQueueRange.opaque, data.opaqueLayerMask, m_DefaultStencilState, stencilData.stencilReference);
            m_DrawSkyboxPass = new DrawSkyboxPass(RenderPassEvent.BeforeRenderingSkybox);
            m_RenderTransparentForwardPass = new DrawObjectsPass(ProfilerSamplerString.drawTransparentForwardPass, data.shaderTagIds, false, RenderPassEvent.BeforeRenderingTransparents, RenderQueueRange.transparent, data.transparentLayerMask, m_DefaultStencilState, stencilData.stencilReference);
        }

        void SetDefaultStencilState(StencilStateData stencilData) {
            m_DefaultStencilState = StencilState.defaultValue;
            m_DefaultStencilState.enabled = stencilData.overrideStencilState;
            m_DefaultStencilState.SetCompareFunction(stencilData.stencilCompareFunction);
            m_DefaultStencilState.SetPassOperation(stencilData.passOperation);
            m_DefaultStencilState.SetFailOperation(stencilData.failOperation);
            m_DefaultStencilState.SetZFailOperation(stencilData.zFailOperation);

        }

        static void ProjectSettingMobile() {
#if UNITY_EDITOR
            UnityEditor.PlayerSettings.companyName = "SoFunny";
            UnityEditor.PlayerSettings.colorSpace = ColorSpace.Linear;
            UnityEditor.PlayerSettings.useAnimatedAutorotation = true;
            UnityEditor.PlayerSettings.allowedAutorotateToPortrait = false;
            UnityEditor.PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            UnityEditor.PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            UnityEditor.PlayerSettings.allowedAutorotateToLandscapeRight = true;
            UnityEditor.PlayerSettings.stripUnusedMeshComponents = true;
            UnityEditor.PlayerSettings.accelerometerFrequency = 0;
            UnityEditor.PlayerSettings.gcIncremental = true;
            UnityEditor.PlayerSettings.useHDRDisplay = false;
            UnityEditor.PlayerSettings.hdrBitDepth = HDRDisplayBitDepth.BitDepth10;
            UnityEditor.PlayerSettings.enableOpenGLProfilerGPURecorders = false;
            UnityEditor.PlayerSettings.preserveFramebufferAlpha = false;
            UnityEditor.PlayerSettings.enableFrameTimingStats = false;
            UnityEditor.PlayerSettings.gpuSkinning = false;
            UnityEditor.PlayerSettings.graphicsJobs = false;
            UnityEditor.PlayerSettings.SetVirtualTexturingSupportEnabled(false);
            UnityEditor.PlayerSettings.spriteBatchVertexThreshold = 1500;
            UnityEditor.PlayerSettings.SetShaderPrecisionModel(UnityEditor.ShaderPrecisionModel.PlatformDefault);
            UnityEditor.PlayerSettings.SetDefaultShaderChunkSizeInMB(16);
#if UNITY_ANDROID
            UnityEditor.PlayerSettings.SetNormalMapEncoding(UnityEditor.BuildTargetGroup.Android, UnityEditor.NormalMapEncoding.XYZ);
            UnityEditor.PlayerSettings.SetMobileMTRendering(UnityEditor.BuildTargetGroup.Android, true);
            UnityEditor.PlayerSettings.vulkanNumSwapchainBuffers = 3;
            UnityEditor.PlayerSettings.vulkanEnableSetSRGBWrite = false;
            UnityEditor.PlayerSettings.vulkanEnableLateAcquireNextImage = false;
            UnityEditor.PlayerSettings.openGLRequireES31 = true;
            UnityEditor.PlayerSettings.openGLRequireES31AEP = true;
            UnityEditor.PlayerSettings.openGLRequireES32 = true;

#endif
#endif

        }

        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData) {
            m_ForwardLights.PreSetup(ref renderingData);
            ref CameraData cameraData = ref renderingData.cameraData;
            Camera camera = cameraData.camera;
            RenderTextureDescriptor cameraTargetDescriptor = cameraData.cameraTargetDescriptor;
            bool lastCameraInTheStack = cameraData.resolveFinalTarget;
            #region opaque pass
            RenderBufferStoreAction opaquePassColorStoreAction = RenderBufferStoreAction.Store;
            RenderBufferStoreAction opaquePassDepthStoreAction = RenderBufferStoreAction.DontCare;
            DrawObjectsPass renderOpaqueForwardPass = null;
            renderOpaqueForwardPass = m_RenderOpaqueForwardPass;
            renderOpaqueForwardPass.ConfigureColorStoreAction(opaquePassColorStoreAction);
            renderOpaqueForwardPass.ConfigureDepthStoreAction(opaquePassDepthStoreAction);
            ClearFlag opaqueForwardPassClearFlag = (cameraData.renderType != CameraRenderType.Base) ? ClearFlag.None : ClearFlag.Color;
            renderOpaqueForwardPass.ConfigureClear(opaqueForwardPassClearFlag, Color.black);
            EnqueuePass(renderOpaqueForwardPass);
            #endregion
            #region  skybox pass
            if (camera.clearFlags == CameraClearFlags.Skybox && cameraData.renderType != CameraRenderType.Overlay) {
                if (RenderSettings.skybox != null || (camera.TryGetComponent(out Skybox cameraSkybox) && cameraSkybox.material != null)) {
                    EnqueuePass(m_DrawSkyboxPass);
                }
            }
            #endregion
            #region transparent pass
            RenderBufferStoreAction transparentPassColorStoreAction = cameraTargetDescriptor.msaaSamples > 1 && lastCameraInTheStack ? RenderBufferStoreAction.Resolve : RenderBufferStoreAction.Store;
            RenderBufferStoreAction transparentPassDepthStoreAction = lastCameraInTheStack ? RenderBufferStoreAction.DontCare : RenderBufferStoreAction.Store;
            m_RenderTransparentForwardPass.ConfigureColorStoreAction(transparentPassColorStoreAction);
            m_RenderTransparentForwardPass.ConfigureDepthStoreAction(transparentPassDepthStoreAction);
            EnqueuePass(m_RenderTransparentForwardPass);
            #endregion

        }

        public override void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData) {
            m_ForwardLights.Setup(context, ref renderingData);
        }

        protected override void Dispose(bool disposing) {
            m_ForwardLights.Cleanup();
            base.Dispose(disposing);
        }
    }
}
