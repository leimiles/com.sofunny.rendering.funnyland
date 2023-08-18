using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal.Internal;
using UnityEngine.Rendering.Universal;

namespace SoFunny.Rendering.Funnyland {
    public class FunnylandMobileRenderer : ScriptableRenderer {
        private static class Profiling {
            private const string k_Name = nameof(FunnylandMobileRenderer);
            public static readonly ProfilingSampler createCameraRenderTarget = new ProfilingSampler($"{k_Name}.{nameof(CreateCameraRenderTarget)}");
        }
        static class ProfilerSamplerString {
            public static readonly string drawOpaqueForwardPass = "Draw Opaque Forward Pass";
            public static readonly string drawTransparentForwardPass = "Draw Transparent Forward Pass";
        }
#if UNITY_SWITCH || UNITY_ANDROID
        const GraphicsFormat k_DepthStencilFormat = GraphicsFormat.D24_UNorm_S8_UInt;
        const int k_DepthBufferBits = 24;
#else
        const GraphicsFormat k_DepthStencilFormat = GraphicsFormat.D32_SFloat_S8_UInt;
        const int k_DepthBufferBits = 32;
#endif
<<<<<<< Runtime/Funnyland/FunnylandRenderer.cs

=======
        
>>>>>>> Runtime/Funnyland/FunnylandRenderer.cs
        public override int SupportedCameraStackingTypes()
        {
            switch (m_RenderingMode)
            {
                case RenderingMode.Forward:
                case RenderingMode.ForwardPlus:
                    return 1 << (int)CameraRenderType.Base | 1 << (int)CameraRenderType.Overlay;
                case RenderingMode.Deferred:
                    return 1 << (int)CameraRenderType.Base;
                default:
                    return 0;
            }
        }
        
        const int k_FinalBlitPassQueueOffset = 1;
        internal RenderTargetBufferSystem m_ColorBufferSystem;
        internal RTHandle m_ActiveCameraColorAttachment;
        internal RTHandle m_ActiveCameraDepthAttachment;
        internal RTHandle m_CameraDepthAttachment;
        internal RTHandle m_DepthTexture;
        StencilState m_DefaultStencilState;
        MainLightShadowCasterPass m_MainLightShadowCasterPass;
        AdditionalLightsShadowCasterPass m_AdditionalLightsShadowCasterPass;
        DrawObjectsPass m_RenderOpaqueForwardPass;
        DrawObjectsPass m_RenderTransparentForwardPass;
        DrawSkyboxPass m_DrawSkyboxPass;
        CopyDepthPass m_CopyDepthPass;
        FinalBlitPass m_FinalBlitPass;
        bool m_DepthPrimingRecommended;
        CopyDepthMode m_CopyDepthMode;
        RenderingMode m_RenderingMode;
        bool m_Clustering;
        ForwardLights m_ForwardLights;
        LightCookieManager m_LightCookieManager;

        Material m_BlitMaterial = null;
        Material m_CopyDepthMaterial = null;
        
        
        FunnyPostProcessPasses m_PostProcessPasses;
        internal FunnyColorGradingLutPass colorGradingLutPass { get => m_PostProcessPasses.colorGradingLutPass; }
        internal FunnyPostProcessPass postProcessPass { get => m_PostProcessPasses.postProcessPass; }
        internal FunnyPostProcessPass finalPostProcessPass { get => m_PostProcessPasses.finalPostProcessPass; }
        internal RTHandle colorGradingLut { get => m_PostProcessPasses.colorGradingLut; }

        PostProssType m_postProssType;
        PostVolumeData m_volumeData;
        public FunnylandMobileRenderer(FunnylandMobileRendererData data) : base(data) {
            Application.targetFrameRate = data.frameLimit;
            ProjectSettingMobile();
            StencilStateData stencilData = data.defaultStencilState;
            SetDefaultStencilState(stencilData);

            m_BlitMaterial = CoreUtils.CreateEngineMaterial(data.shaderResources.coreBlitPS);
            m_CopyDepthMaterial = CoreUtils.CreateEngineMaterial(data.shaderResources.copyDepthPS);

            UniversalRenderPipeline.asset.renderScale = GetAdaptedScale();

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
            m_MainLightShadowCasterPass = new MainLightShadowCasterPass(RenderPassEvent.BeforeRenderingShadows);
            m_AdditionalLightsShadowCasterPass = new AdditionalLightsShadowCasterPass(RenderPassEvent.BeforeRenderingShadows);
            m_RenderOpaqueForwardPass = new DrawObjectsPass(ProfilerSamplerString.drawOpaqueForwardPass, data.shaderTagIds, true, RenderPassEvent.BeforeRenderingOpaques, RenderQueueRange.opaque, data.opaqueLayerMask, m_DefaultStencilState, stencilData.stencilReference);
            m_DrawSkyboxPass = new DrawSkyboxPass(RenderPassEvent.BeforeRenderingSkybox);
            m_CopyDepthPass = new CopyDepthPass(
                RenderPassEvent.AfterRenderingSkybox,
                m_CopyDepthMaterial,
                shouldClear: true,
                copyResolvedDepth: false);
            m_RenderTransparentForwardPass = new DrawObjectsPass(ProfilerSamplerString.drawTransparentForwardPass, data.shaderTagIds, false, RenderPassEvent.BeforeRenderingTransparents, RenderQueueRange.transparent, data.transparentLayerMask, m_DefaultStencilState, stencilData.stencilReference);
            m_FinalBlitPass = new FinalBlitPass(RenderPassEvent.AfterRendering + k_FinalBlitPassQueueOffset, m_BlitMaterial, m_BlitMaterial);
            m_ColorBufferSystem = new RenderTargetBufferSystem("_CameraColorRTAttachment");

            {
                var postProcessParams = PostProcessParams.Create();
                postProcessParams.blitMaterial = m_BlitMaterial;
                postProcessParams.requestHDRFormat = GraphicsFormat.B10G11R11_UFloatPack32;
                var asset = UniversalRenderPipeline.asset;
                if (asset)
                    postProcessParams.requestHDRFormat = UniversalRenderPipeline.MakeRenderTextureGraphicsFormat(asset.supportsHDR, asset.hdrColorBufferPrecision, false);

                m_PostProcessPasses = new FunnyPostProcessPasses(data.postProcessData, ref postProcessParams);
            }
            m_volumeData = new PostVolumeData(data.GetVolumePrpfile(), data.GetVolumeStack());
            m_postProssType = data.postProssType;
        }

        void SetDefaultStencilState(StencilStateData stencilData) {
            m_DefaultStencilState = StencilState.defaultValue;
            m_DefaultStencilState.enabled = stencilData.overrideStencilState;
            m_DefaultStencilState.SetCompareFunction(stencilData.stencilCompareFunction);
            m_DefaultStencilState.SetPassOperation(stencilData.passOperation);
            m_DefaultStencilState.SetFailOperation(stencilData.failOperation);
            m_DefaultStencilState.SetZFailOperation(stencilData.zFailOperation);

        }

        public override void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData) {
            m_ForwardLights.Setup(context, ref renderingData);
        }

        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData) {
            m_ForwardLights.PreSetup(ref renderingData);
            ref CameraData cameraData = ref renderingData.cameraData;
            Camera camera = cameraData.camera;
            var cmd = renderingData.commandBuffer;


            bool isSceneViewOrPreviewCamera = cameraData.isSceneViewCamera || cameraData.cameraType == CameraType.Preview;
#if UNITY_EDITOR
            bool isGizmosEnabled = UnityEditor.Handles.ShouldRenderGizmos();
#else
            bool isGizmosEnabled = false;
#endif
            RenderTextureDescriptor cameraTargetDescriptor = cameraData.cameraTargetDescriptor;
            var colorDescriptor = cameraTargetDescriptor;
            colorDescriptor.useMipMap = false;
            colorDescriptor.autoGenerateMips = false;
            colorDescriptor.depthBufferBits = (int)DepthBits.None;
            m_ColorBufferSystem.SetCameraSettings(colorDescriptor, FilterMode.Bilinear);
<<<<<<< Runtime/Funnyland/FunnylandRenderer.cs

=======
            
>>>>>>> Runtime/Funnyland/FunnylandRenderer.cs
            // OverlayCamera 不开启阴影
            bool mainLightShadows = m_MainLightShadowCasterPass.Setup(ref renderingData) && cameraData.renderType != CameraRenderType.Overlay;
            
            // 暂无需支持附加光阴影
            // bool additionalLightShadows = m_AdditionalLightsShadowCasterPass.Setup(ref renderingData);

            bool requiresDepthTexture = cameraData.requiresDepthTexture;
            bool createDepthTexture = requiresDepthTexture;
            createDepthTexture |= !cameraData.resolveFinalTarget;

            bool requiresDepthCopyPass = (renderingData.cameraData.requiresDepthTexture) && createDepthTexture && cameraData.renderType ==CameraRenderType.Base;

            if (cameraData.renderType == CameraRenderType.Base) {
                bool sceneViewFilterEnabled = camera.sceneViewFilterMode == Camera.SceneViewFilterMode.ShowFiltered;
                bool intermediateRenderTexture = createDepthTexture || !sceneViewFilterEnabled;

                if (intermediateRenderTexture) {
                    CreateCameraRenderTarget(context, ref cameraTargetDescriptor, cmd);
                }
                // 初始化新的 color 和 depth buffer
                m_ActiveCameraColorAttachment = m_ColorBufferSystem.PeekBackBuffer();
                m_ActiveCameraDepthAttachment = m_CameraDepthAttachment;
            } else {
                cameraData.baseCamera.TryGetComponent<UniversalAdditionalCameraData>(out var baseCameraData);
                var baseRenderer = (FunnylandMobileRenderer)baseCameraData.scriptableRenderer;
                if (m_ColorBufferSystem != baseRenderer.m_ColorBufferSystem) {
                    m_ColorBufferSystem.Dispose();
                    m_ColorBufferSystem = baseRenderer.m_ColorBufferSystem;
                }
                m_ActiveCameraColorAttachment = m_ColorBufferSystem.PeekBackBuffer();
                m_ActiveCameraDepthAttachment = baseRenderer.m_ActiveCameraDepthAttachment;
            }

            // 更改渲染目标至新的 color 和 depth buffer
            ConfigureCameraTarget(m_ActiveCameraColorAttachment, m_ActiveCameraDepthAttachment);

            #region shadows pass
            if (mainLightShadows)
                EnqueuePass(m_MainLightShadowCasterPass);

            /* 暂无需支持附加光阴影
            if (additionalLightShadows)
                EnqueuePass(m_AdditionalLightsShadowCasterPass);
            */
            #endregion

            cameraData.postProcessEnabled = false;
            bool lastCameraInTheStack = cameraData.resolveFinalTarget;
            if (m_postProssType == PostProssType.Off) {
                cameraData.postProcessEnabled = false;
            }
            if (m_postProssType == PostProssType.lastCamera && lastCameraInTheStack) {
                cameraData.postProcessEnabled = true;
            }
            if (m_postProssType == PostProssType.BaseCamera && cameraData.renderType == CameraRenderType.Base) {
                cameraData.postProcessEnabled = true;
            }
            
            #region LUT
            bool generateColorGradingLUT = cameraData.postProcessEnabled && m_PostProcessPasses.isCreated;
            if (generateColorGradingLUT)
            {
                colorGradingLutPass.ConfigureDescriptor(in renderingData.postProcessingData, out var desc, out var filterMode);
                RenderingUtils.ReAllocateIfNeeded(ref m_PostProcessPasses.m_ColorGradingLut, desc, filterMode, TextureWrapMode.Clamp, anisoLevel: 0, name: "_InternalGradingLut");
                colorGradingLutPass.Setup(colorGradingLut, m_volumeData);
                EnqueuePass(colorGradingLutPass);
            }
            #endregion

            // 分配 m_DepthTexture 内存
            var depthDescriptor = cameraTargetDescriptor;
            depthDescriptor.graphicsFormat = GraphicsFormat.None;
            depthDescriptor.depthStencilFormat = k_DepthStencilFormat;
            depthDescriptor.depthBufferBits = k_DepthBufferBits;
            depthDescriptor.msaaSamples = 1;// Depth-Only pass don't use MSAA
            RenderingUtils.ReAllocateIfNeeded(ref m_DepthTexture, depthDescriptor, FilterMode.Point, wrapMode: TextureWrapMode.Clamp, name: "_CameraDepthTexture");
            cmd.SetGlobalTexture(m_DepthTexture.name, m_DepthTexture.nameID);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            #region opaque pass
            RenderBufferStoreAction opaquePassColorStoreAction = RenderBufferStoreAction.Store;
            // 因为需要 copy depth，所以保存 store action，否则 don't care 才是性能之道
            RenderBufferStoreAction opaquePassDepthStoreAction = RenderBufferStoreAction.Store;
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

            #region  copyDepth pass
            if (requiresDepthCopyPass) {
                m_CopyDepthPass.Setup(m_ActiveCameraDepthAttachment, m_DepthTexture);
                EnqueuePass(m_CopyDepthPass);
            }
            #endregion

            #region transparent pass
            RenderBufferStoreAction transparentPassColorStoreAction = cameraTargetDescriptor.msaaSamples > 1 && lastCameraInTheStack ? RenderBufferStoreAction.Resolve : RenderBufferStoreAction.Store;
            RenderBufferStoreAction transparentPassDepthStoreAction = lastCameraInTheStack ? RenderBufferStoreAction.DontCare : RenderBufferStoreAction.Store;
            m_RenderTransparentForwardPass.ConfigureColorStoreAction(transparentPassColorStoreAction);
            m_RenderTransparentForwardPass.ConfigureDepthStoreAction(transparentPassDepthStoreAction);
            EnqueuePass(m_RenderTransparentForwardPass);
            #endregion

            #region post processing
            bool applyPostProcessing = cameraData.postProcessEnabled && m_PostProcessPasses.isCreated;
            bool applyFinalPostProcessing = false;
            bool resolvePostProcessingToCameraTarget = !applyFinalPostProcessing;
            
            bool needsColorEncoding = true;
            if (applyPostProcessing)
            {
                var desc = PostProcessPass.GetCompatibleDescriptor(cameraTargetDescriptor, cameraTargetDescriptor.width, cameraTargetDescriptor.height, cameraTargetDescriptor.graphicsFormat, DepthBits.None);
                RenderingUtils.ReAllocateIfNeeded(ref m_PostProcessPasses.m_AfterPostProcessColor, desc, FilterMode.Point, TextureWrapMode.Clamp, name: "_AfterPostProcessTexture");
            }
            
            if (lastCameraInTheStack)
            {
                // Post-processing will resolve to final target. No need for final blit pass.
                if (applyPostProcessing)
                {
                    // if resolving to screen we need to be able to perform sRGBConversion in post-processing if necessary
                    bool doSRGBEncoding = resolvePostProcessingToCameraTarget && needsColorEncoding;
                    postProcessPass.Setup(cameraTargetDescriptor, m_ActiveCameraColorAttachment, resolvePostProcessingToCameraTarget, m_volumeData, m_ActiveCameraDepthAttachment, colorGradingLut, null, applyFinalPostProcessing, doSRGBEncoding);
                    EnqueuePass(postProcessPass);
                }

                var sourceForFinalPass = m_ActiveCameraColorAttachment;

                // Do FXAA or any other final post-processing effect that might need to run after AA.
                if (applyFinalPostProcessing)
                {
                    finalPostProcessPass.SetupFinalPass(sourceForFinalPass, true, needsColorEncoding);
                    EnqueuePass(finalPostProcessPass);
                }

                bool cameraTargetResolved =
                    // final PP always blit to camera target
                    applyFinalPostProcessing ||
                    // no final PP but we have PP stack. In that case it blit unless there are render pass after PP
                    applyPostProcessing;

                // We need final blit to resolve to screen
                if (!cameraTargetResolved)
                {
                    m_FinalBlitPass.Setup(cameraTargetDescriptor, sourceForFinalPass);
                    EnqueuePass(m_FinalBlitPass);
                }
            }
            // stay in RT so we resume rendering on stack after post-processing
            else if (applyPostProcessing)
            {
                postProcessPass.Setup(cameraTargetDescriptor, m_ActiveCameraColorAttachment, false, m_volumeData, m_ActiveCameraDepthAttachment, colorGradingLut, null, false, false);
                EnqueuePass(postProcessPass);
            }
            #endregion

        }

        void CreateCameraRenderTarget(ScriptableRenderContext context, ref RenderTextureDescriptor descriptor, CommandBuffer cmd) {
            using (new ProfilingScope(null, Profiling.createCameraRenderTarget)) {
                if (m_ColorBufferSystem.PeekBackBuffer() == null || m_ColorBufferSystem.PeekBackBuffer().nameID != BuiltinRenderTextureType.CameraTarget) {
                    m_ActiveCameraColorAttachment = m_ColorBufferSystem.GetBackBuffer(cmd);
                    ConfigureCameraColorTarget(m_ActiveCameraColorAttachment);
                    cmd.SetGlobalTexture("_CameraColorTexture", m_ActiveCameraColorAttachment.nameID);
                    //Set _AfterPostProcessTexture, users might still rely on this although it is now always the cameratarget due to swapbuffer
                    //cmd.SetGlobalTexture("_AfterPostProcessTexture", m_ActiveCameraColorAttachment.nameID);
                }
                if (m_CameraDepthAttachment == null || m_CameraDepthAttachment.nameID != BuiltinRenderTextureType.CameraTarget) {
                    var depthDescriptor = descriptor;
                    depthDescriptor.useMipMap = false;
                    depthDescriptor.autoGenerateMips = false;
                    depthDescriptor.bindMS = false;
                    depthDescriptor.graphicsFormat = GraphicsFormat.None;
                    depthDescriptor.depthStencilFormat = k_DepthStencilFormat;
                    RenderingUtils.ReAllocateIfNeeded(ref m_CameraDepthAttachment, depthDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "_CameraDepthRTAttachment");
                    cmd.SetGlobalTexture(m_CameraDepthAttachment.name, m_CameraDepthAttachment.nameID);
                }
            }
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        public override void FinishRendering(CommandBuffer cmd) {
            m_ColorBufferSystem.Clear();
            m_ActiveCameraColorAttachment = null;
            m_ActiveCameraDepthAttachment = null;
        }

        internal override void ReleaseRenderTargets() {
            // 一次性释放多个 rthandle 资源
            m_ColorBufferSystem.Dispose();
            m_CameraDepthAttachment?.Release();
            m_MainLightShadowCasterPass?.Dispose();
            m_AdditionalLightsShadowCasterPass?.Dispose();
            m_DepthTexture?.Release();
            hasReleasedRTs = true;
        }

        protected override void Dispose(bool disposing) {
            m_ForwardLights.Cleanup();
            m_FinalBlitPass?.Dispose();
            ReleaseRenderTargets();
            base.Dispose(disposing);
            CoreUtils.Destroy(m_BlitMaterial);
            CoreUtils.Destroy(m_CopyDepthMaterial);
        }

        /// <inheritdoc />
        public override void SetupCullingParameters(ref ScriptableCullingParameters cullingParameters,
            ref CameraData cameraData) {
            // TODO: PerObjectCulling also affect reflection probes. Enabling it for now.
            // if (asset.additionalLightsRenderingMode == LightRenderingMode.Disabled ||
            //     asset.maxAdditionalLightsCount == 0)

            // if (renderingModeActual == RenderingMode.ForwardPlus)
            cullingParameters.cullingOptions |= CullingOptions.DisablePerObjectCulling;


            // We disable shadow casters if both shadow casting modes are turned off
            // or the shadow distance has been turned down to zero
            bool isShadowCastingDisabled = !UniversalRenderPipeline.asset.supportsMainLightShadows && !UniversalRenderPipeline.asset.supportsAdditionalLightShadows;
            bool isShadowDistanceZero = Mathf.Approximately(cameraData.maxShadowDistance, 0.0f);
            if (isShadowCastingDisabled || isShadowDistanceZero) {
                cullingParameters.cullingOptions &= ~CullingOptions.ShadowCasters;
            }

            // We set the number of maximum visible lights allowed and we add one for the mainlight...
            //
            // Note: However ScriptableRenderContext.Cull() does not differentiate between light types.
            //       If there is no active main light in the scene, ScriptableRenderContext.Cull() might return  ( cullingParameters.maximumVisibleLights )  visible additional lights.
            //       i.e ScriptableRenderContext.Cull() might return  ( UniversalRenderPipeline.maxVisibleAdditionalLights + 1 )  visible additional lights !
            cullingParameters.maximumVisibleLights = UniversalRenderPipeline.maxVisibleAdditionalLights + 1;
            cullingParameters.shadowDistance = cameraData.maxShadowDistance;

            cullingParameters.conservativeEnclosingSphere = UniversalRenderPipeline.asset.conservativeEnclosingSphere;

            cullingParameters.numIterationsEnclosingSphere = UniversalRenderPipeline.asset.numIterationsEnclosingSphere;
        }
        
        internal override void SwapColorBuffer(CommandBuffer cmd)
        {
            m_ColorBufferSystem.Swap();

            //Check if we are using the depth that is attached to color buffer
            if (m_ActiveCameraDepthAttachment.nameID != BuiltinRenderTextureType.CameraTarget)
                ConfigureCameraTarget(m_ColorBufferSystem.GetBackBuffer(cmd), m_ColorBufferSystem.GetBufferA());
            else
                ConfigureCameraColorTarget(m_ColorBufferSystem.GetBackBuffer(cmd));

            m_ActiveCameraColorAttachment = m_ColorBufferSystem.GetBackBuffer(cmd);
            cmd.SetGlobalTexture("_CameraColorTexture", m_ActiveCameraColorAttachment.nameID);
            //Set _AfterPostProcessTexture, users might still rely on this although it is now always the cameratarget due to swapbuffer
            cmd.SetGlobalTexture("_AfterPostProcessTexture", m_ActiveCameraColorAttachment.nameID);
        }

        internal override RTHandle GetCameraColorFrontBuffer(CommandBuffer cmd)
        {
            return m_ColorBufferSystem.GetFrontBuffer(cmd);
        }

        internal override RTHandle GetCameraColorBackBuffer(CommandBuffer cmd)
        {
            return m_ColorBufferSystem.GetBackBuffer(cmd);
        }

        float GetAdaptedScale() {
            float sideLength = (float)Mathf.Min(Screen.width, Screen.height);
            float scale = 1.0f;
            if (sideLength > 720.0f) {
                scale = 720.0f / sideLength;
            }
            return scale;
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
    }
}
