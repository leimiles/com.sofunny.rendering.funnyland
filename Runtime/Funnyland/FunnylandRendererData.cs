#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using ShaderKeywordFilter = UnityEditor.ShaderKeywordFilter;
#endif
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Assertions;

namespace SoFunny.Rendering.Funnyland {
    [Serializable, ReloadGroup, ExcludeFromPreset]
    public class FunnylandMobileRendererData : ScriptableRendererData, ISerializationCallbackReceiver {
#if UNITY_EDITOR
        public static readonly string packagePath = "Packages/SoFunny.Rendering.Funnyland";
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812")]
        internal class CreateFunnylandRendererAsset : EndNameEditAction {
            public override void Action(int instanceId, string pathName, string resourceFile) {
                var instance = CreateRendererAsset(pathName, RendererType.UniversalRenderer, false) as UniversalRendererData;
                Selection.activeObject = instance;
            }

            internal static ScriptableRendererData CreateRendererAsset(string path, RendererType type, bool relativePath = true, string suffix = "Renderer") {
                ScriptableRendererData data = CreateInstance<FunnylandMobileRendererData>();
                string dataPath;
                if (relativePath)
                    dataPath =
                        $"{System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), System.IO.Path.GetFileNameWithoutExtension(path))}_{suffix}{System.IO.Path.GetExtension(path)}";
                else
                    dataPath = path;
                AssetDatabase.CreateAsset(data, dataPath);
                return data;
            }

        }
        [MenuItem("Assets/Create/Rendering/Funnyland Renderer", priority = CoreUtils.Sections.section3 + CoreUtils.Priorities.assetsCreateRenderingMenuPriority + 3)]
        static void CreateFunnylandRendererData() {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateFunnylandRendererAsset>(), "Funnyland Renderer Data.asset", null, null);
        }
#endif

        [SerializeField] string[] m_ShaderTagLightModes;
        public ShaderTagId[] shaderTagIds {
            get {
                if (m_ShaderTagLightModes != null && m_ShaderTagLightModes.Length > 0) {
                    ShaderTagId[] shaderTagIds = new ShaderTagId[m_ShaderTagLightModes.Length];
                    for (int i = 0; i < shaderTagIds.Length; ++i) {
                        shaderTagIds[i] = new ShaderTagId(m_ShaderTagLightModes[i]);
                    }
                    return shaderTagIds;
                } else {
                    ShaderTagId[] shaderTagIds = { new ShaderTagId("FunnylandTest") };
                    return shaderTagIds;
                }
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
        protected override ScriptableRenderer Create() {
            return new FunnylandMobileRenderer(this);
        }
        public void OnAfterDeserialize() {
        }

        public void OnBeforeSerialize() {

        }

    }
}