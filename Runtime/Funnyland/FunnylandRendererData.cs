#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using ShaderKeywordFilter = UnityEditor.ShaderKeywordFilter;
using System.IO;
#endif
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Assertions;

namespace SoFunny.Rendering.Funnyland {
    [Serializable, ReloadGroup, ExcludeFromPreset]
    public class FunnylandMobileRendererData : ScriptableRendererData, ISerializationCallbackReceiver {
#if UNITY_EDITOR
        public static readonly string packagePath = "Packages/com.unity.render-pipelines.universal";
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812")]
        internal class CreateFunnylandRendererAsset : EndNameEditAction {
            public override void Action(int instanceId, string pathName, string resourceFile) {
                var instance = CreateRendererAsset(pathName, false) as FunnylandMobileRendererData;
                Selection.activeObject = instance;
            }

            internal static ScriptableRendererData CreateRendererAsset(string path, bool relativePath = true, string suffix = "Renderer") {
                /*
                ScriptableRendererData data = CreateInstance<FunnylandMobileRendererData>();
                string dataPath;
                if (relativePath)
                    dataPath =
                        $"{System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), System.IO.Path.GetFileNameWithoutExtension(path))}_{suffix}{System.IO.Path.GetExtension(path)}";
                else
                    dataPath = path;
                AssetDatabase.CreateAsset(data, dataPath);
                return data;
                */
                ScriptableRendererData data = CreateRendererData();
                string dataPath;
                if (relativePath)
                    dataPath =
                        $"{Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path))}_{suffix}{Path.GetExtension(path)}";
                else
                    dataPath = path;
                AssetDatabase.CreateAsset(data, dataPath);
                ResourceReloader.ReloadAllNullIn(data, packagePath);
                LoadFunnylandResources(data as FunnylandMobileRendererData);
                return data;
            }

            static void LoadFunnylandResources(FunnylandMobileRendererData data) {
                #region  load volume profile
                var path = System.IO.Path.Combine(packagePath, "Runtime/Materials/Funnyland/VolumeProfiles/Volume Profile Color Curves.asset");
                data.m_SharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
                #endregion
            }

            static ScriptableRendererData CreateRendererData() {
                var rendererData = CreateInstance<FunnylandMobileRendererData>();
                rendererData.postProcessData = PostProcessData.GetDefaultPostProcessData();
                return rendererData;
            }

        }
        [MenuItem("Assets/Create/Rendering/Funnyland Renderer", priority = CoreUtils.Sections.section3 + CoreUtils.Priorities.assetsCreateRenderingMenuPriority + 3)]
        static void CreateFunnylandRendererData() {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateFunnylandRendererAsset>(), "Funnyland Renderer Data.asset", null, null);
        }
#endif
        [Serializable, ReloadGroup]
        public sealed class ShaderResources {
            [Reload("Shaders/Utils/CopyDepth.shader")]
            public Shader copyDepthPS;

            [Reload("Shaders/Utils/CoreBlit.shader"), SerializeField]
            internal Shader coreBlitPS;
            
            [Reload("Shaders/Utils/CoreBlitColorAndDepth.shader"), SerializeField]
            internal Shader coreBlitColorAndDepthPS;
            
            [Reload("Shaders/Utils/Sampling.shader")]
            public Shader samplingPS;
            
            [Reload("Shaders/Funnyland/Utils/FunnyEffects.shader"), SerializeField]
            internal Shader funnyEffectsPS;
            
            [Reload("Shaders/Funnyland/Utils/Histogram.shader"), SerializeField]
            internal Shader histogramPS;
            
            [Reload("Shaders/Funnyland/Utils/Histogram.compute"), SerializeField]
            internal ComputeShader histogramComputerShader;
            
            [Reload("Shaders/Funnyland/Utils/UIBackgroundBlur.shader"), SerializeField]
            internal Shader uiBackgroundBlurPS;

            public bool CheckHasNull() {
                return !copyDepthPS 
                       || !copyDepthPS 
                       || !funnyEffectsPS 
                       || !histogramPS 
                       || !histogramComputerShader 
                       || !uiBackgroundBlurPS;
            }
        }
        public ShaderResources shaderResources = null;


        [Serializable, ReloadGroup]
        public sealed class TextureResources {
            [Reload("Textures/Funnyland/BayerDither.tga")]
            public Texture2D ditherTexture;
        }
        public TextureResources textureResources = null;

        
        [Serializable, ReloadGroup]
        public sealed class MeshResources {
            [Reload("Shaders/Funnyland/DecalBox/DecalBox.mesh"), SerializeField]
            static internal Mesh decalBox;
        }
#if UNITY_EDITOR
        [SerializeField] public DebugModeType debugModeType = DebugModeType.Off;
#endif
        [SerializeField] public PostProssType postProssType = PostProssType.BaseCamera;
        public PostProcessData postProcessData;

        public MeshResources meshResources = null;
        
        [SerializeField] LayerMask m_OccluderStencilLayerMask = 0;
        private RenderObjects.RenderObjectsSettings m_OccluderStencilData = new RenderObjects.RenderObjectsSettings();

        [SerializeField] HistogramChannel m_Histogram = HistogramChannel.None;
        [SerializeField] private bool m_enableUIBlur = false;
        [SerializeField] private UIBlurSettings m_uiBlurSettings = new UIBlurSettings(){ maxIterations = 2, blurRadius = 1};
        
        [SerializeField] string[] m_ShaderTagLightModes;
        string[] m_DefaultShaderTagLightModes = new []{"SRPDefaultUnlit", "FunnyLandMobileForward"};

        [SerializeField] private GraphicQuality m_GraphicQuality = GraphicQuality.High;
        
        public ShaderTagId[] shaderTagIds {
            get {
                ShaderTagId[] shaderTagIds = new ShaderTagId[shaderTags.Length];
                for (int i = 0; i < shaderTags.Length; ++i) {
                    shaderTagIds[i] = new ShaderTagId(shaderTags[i]);
                }

                return shaderTagIds;
            }
        }

        public string[] shaderTags {
            get {
                List<string> shaderTagLightModes = new List<string>();

                foreach (var defaultShaderTagLightMode in m_DefaultShaderTagLightModes) {
                    shaderTagLightModes.Add(defaultShaderTagLightMode);
                }
                if(m_ShaderTagLightModes != null && m_ShaderTagLightModes.Length > 0){
                    foreach (var shaderTagLightMode in m_ShaderTagLightModes) {
                        shaderTagLightModes.Add(shaderTagLightMode);
                    }
                }

                return shaderTagLightModes.ToArray();
            }
        }

        [SerializeField] VolumeProfile m_SharedProfile;
        // Default VolumeData
        [SerializeField] VolumeStack m_SharedStack { get => VolumeManager.instance.CreateStack(); }
        public VolumeProfile GetVolumePrpfile() {
            return m_SharedProfile;
        }

        public VolumeStack GetVolumeStack() {
            return m_SharedStack;
        }

        public GraphicQuality graphicQuality {
            get => m_GraphicQuality;
            set {
                SetDirty();
                m_GraphicQuality = value;
            }
        }

        public enum FrameLimit {
            Standard = 30,
            Ultra = 60
        }
        [SerializeField] FrameLimit m_FrameLimit = FrameLimit.Ultra;
        public int frameLimit {
            get => (int)m_FrameLimit;
            set {
                SetDirty();
                m_FrameLimit = (FrameLimit)value;
            }
        }

        [SerializeField] LayerMask m_OpaqueLayerMask = -1;
        public LayerMask opaqueLayerMask {
            get => m_OpaqueLayerMask;
            set {
                SetDirty();
                m_OpaqueLayerMask = value;
            }
        }

        [SerializeField] LayerMask m_TransparentLayerMask = -1;
        public LayerMask transparentLayerMask {
            get => m_TransparentLayerMask;
            set {
                SetDirty();
                m_TransparentLayerMask = value;
            }
        }

        [SerializeField] StencilStateData m_DefaultStencilState = new StencilStateData() { passOperation = StencilOp.Replace };
        public StencilStateData defaultStencilState {
            get => m_DefaultStencilState;
            set {
                SetDirty();
                m_DefaultStencilState = value;
            }
        }
        
        public RenderObjects.RenderObjectsSettings occluderStencilData {
            get {
                m_OccluderStencilData.filterSettings.LayerMask = m_OccluderStencilLayerMask;
                m_OccluderStencilData.stencilSettings.overrideStencilState = true;
                m_OccluderStencilData.stencilSettings.stencilReference = 3;
                m_OccluderStencilData.stencilSettings.passOperation = StencilOp.Replace;
                return m_OccluderStencilData;
            }
        }

        public HistogramChannel histogramChannel {
            get => m_Histogram;
            set {
                SetDirty();
                m_Histogram = value;
            }
        }
        
        public bool enableUIBlur
        {
            get => m_enableUIBlur;
            set
            {
                SetDirty();
                m_enableUIBlur = value;
            }
        }

        public UIBlurSettings uiBlurSettings
        {
            get => m_uiBlurSettings;
            set
            {
                SetDirty();
                m_uiBlurSettings = value;
            }
        }
        
        protected override ScriptableRenderer Create() {
            return new FunnylandMobileRenderer(this);
        }

#if UNITY_EDITOR
        internal override Shader GetDefaultShader()
        {
            return Shader.Find("SoFunny/Funnyland/FunnyLit");
        }
#endif

        protected override void OnEnable() {
            base.OnEnable();
            if (shaderResources == null) {
                return;
            }
            if (textureResources == null) {
                return;
            }
            ReloadAllNullProperties();
        }
        private void ReloadAllNullProperties() {
#if UNITY_EDITOR
            ResourceReloader.TryReloadAllNullIn(this, UniversalRenderPipelineAsset.packagePath);
            /*
            if (postProcessData != null) {}
                ResourceReloader.TryReloadAllNullIn(postProcessData, UniversalRenderPipelineAsset.packagePath);
            */
#endif
        }
        public void OnAfterDeserialize() {
        }

        public void OnBeforeSerialize() {

        }
    }
    public enum PostProssType {
        /// <summary>
        /// 不开启PostPross
        /// </summary>
        Off,

        /// <summary>
        /// BaseCamera 开启 PostPross
        /// </summary>
        BaseCamera,

        /// <summary>
        /// 相机堆栈的最后一个相机开启PostPross
        /// </summary>
        lastCamera,
    }
    
#if UNITY_EDITOR
    public enum DebugModeType {
        /// <summary>
        /// 不开启Debug模式
        /// </summary>
        Off,

        /// <summary>
        /// DebugHlod
        /// </summary>
        HlodDebug,
    }
#endif
}
