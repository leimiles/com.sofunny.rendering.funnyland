using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering.Universal;
using UnityEditor.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SoFunny.Rendering.Funnyland {
    [CustomEditor(typeof(FunnylandMobileRendererData), true)]
    public class FunnylandMobileRendererDataEditor : ScriptableRendererDataEditor {
        private static class Styles {
            public static readonly GUIContent LightModes = EditorGUIUtility.TrTextContent("LightModes: ", "允许渲染的 shader tag light mode.");
            public static readonly GUIContent GraphicQuality = EditorGUIUtility.TrTextContent("GraphicQuality: ", "画质分级");
            public static readonly GUIContent FrameLimit = EditorGUIUtility.TrTextContent("帧率锁定: ", "当前的帧率 ultra = 60, standard = 30.");
            public static readonly GUIContent VolumeProfile = EditorGUIUtility.TrTextContent("镜头效果: ", "只能调色，别的不开");
            public static readonly GUIContent PostProssType = EditorGUIUtility.TrTextContent("后处理开关: ", "只允许主相机或者最后一个相机渲染Post");
            public static readonly GUIContent OccluderStencilLayerMask = EditorGUIUtility.TrTextContent("遮挡LayerMask: ", "在该layer层后的物体和开启遮挡描边");
            public static readonly GUIContent Histogram = EditorGUIUtility.TrTextContent("色彩直方图: ", "开启色彩直方图debug");
            public static readonly GUIContent Debug = EditorGUIUtility.TrTextContent("DebugMode: ", "Debug模式进行某些特殊效果的Debug，仅在Editor情况下");
            public static readonly GUIContent EnableUIBgBlur = EditorGUIUtility.TrTextContent("开启UI背景模糊: ", "开启UI背景模糊效果");
            public static readonly GUIContent UIBlurMaxIterations = EditorGUIUtility.TrTextContent("模糊最大迭代次数: ", "迭代次数越大模糊程度越高");
            public static readonly GUIContent UIBlurRadius = EditorGUIUtility.TrTextContent("模糊半径: ", "模糊半径大小");
        }
        SerializedProperty m_LightModes;
        SerializedProperty m_GraphicQuality;
        SerializedProperty m_FrameLimit;
        SerializedProperty m_SharedProfile;
        SerializedProperty m_PostProcessType;
        SerializedProperty m_PostProcessData;
        SerializedProperty m_OccluderStencilLayerMask;
        SerializedProperty m_Histogram;
        SerializedProperty m_EnableUIBlur;
        SerializedProperty m_uiBlurMaxIterations;
        SerializedProperty m_uiBlurRadius;
        SerializedProperty m_DebugMode;

        bool isDebug = true;
        private FunnylandMobileRendererData _funnylandMobileRendererData;

        //List<VolumeComponentEditor> m_Editors = new List<VolumeComponentEditor>();
        private void OnEnable() {
            _funnylandMobileRendererData = target as FunnylandMobileRendererData;
            m_LightModes = serializedObject.FindProperty("m_ShaderTagLightModes");
            m_GraphicQuality = serializedObject.FindProperty("m_GraphicQuality");
            m_FrameLimit = serializedObject.FindProperty("m_FrameLimit");
            m_SharedProfile = serializedObject.FindProperty("m_SharedProfile");
            m_PostProcessType = serializedObject.FindProperty("postProssType");
            m_PostProcessData = serializedObject.FindProperty("postProcessData");
            m_DebugMode = serializedObject.FindProperty("debugModeType");
            m_OccluderStencilLayerMask = serializedObject.FindProperty("m_OccluderStencilLayerMask");
            m_Histogram = serializedObject.FindProperty("m_Histogram");
            m_EnableUIBlur = serializedObject.FindProperty("m_enableUIBlur");
            SerializedProperty uiBlurSettings = serializedObject.FindProperty("m_uiBlurSettings");
            m_uiBlurMaxIterations = uiBlurSettings.FindPropertyRelative("maxIterations");
            m_uiBlurRadius = uiBlurSettings.FindPropertyRelative("blurRadius");
        }
        public override void OnInspectorGUI() {
            serializedObject.Update();
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_LightModes, Styles.LightModes);
            EditorGUILayout.PropertyField(m_GraphicQuality, Styles.GraphicQuality);
            
            EditorGUILayout.PropertyField(m_FrameLimit, Styles.FrameLimit);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_PostProcessType, Styles.PostProssType);
            if (EditorGUI.EndChangeCheck()) {
                if(m_PostProcessData.objectReferenceValue == null)
                    // postProcessData 默认Data
                    m_PostProcessData.objectReferenceValue = PostProcessData.GetDefaultPostProcessData();
            }
            EditorGUILayout.PropertyField(m_OccluderStencilLayerMask, Styles.OccluderStencilLayerMask);
            EditorGUILayout.PropertyField(m_EnableUIBlur, Styles.EnableUIBgBlur);

            if (m_EnableUIBlur.boolValue) {
                EditorGUI.indentLevel += 2;
                EditorGUILayout.PropertyField(m_uiBlurMaxIterations, Styles.UIBlurMaxIterations);
                EditorGUILayout.PropertyField(m_uiBlurRadius, Styles.UIBlurRadius);
                EditorGUI.indentLevel -= 2;
            }

            isDebug = EditorGUILayout.Foldout(isDebug, "Debug");
            if (isDebug) {
                EditorGUILayout.PropertyField(m_Histogram, Styles.Histogram);
                EditorGUILayout.PropertyField(m_DebugMode, Styles.Debug);
            }
            //EditorGUILayout.PropertyField(m_SharedProfile, Styles.VolumeProfile);     // 自定义 profile 不开放
            serializedObject.ApplyModifiedProperties();
            
            CheckNullData();
        }

        private void CheckNullData() {
            if (_funnylandMobileRendererData == null || _funnylandMobileRendererData.shaderResources == null) {
                return;
            }

            if (_funnylandMobileRendererData.shaderResources.CheckHasNull()) {
                ResourceReloader.TryReloadAllNullIn(_funnylandMobileRendererData, UniversalRenderPipelineAsset.packagePath);
            }
        }
    }
}
