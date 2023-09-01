using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering.Universal;
using UnityEditor.Rendering;
using UnityEngine.Rendering.Universal;

namespace SoFunny.Rendering.Funnyland {
    [CustomEditor(typeof(FunnylandMobileRendererData), true)]
    public class FunnylandMobileRendererDataEditor : ScriptableRendererDataEditor {
        private static class Styles {
            public static readonly GUIContent LightModes = EditorGUIUtility.TrTextContent("LightModes: ", "允许渲染的 shader tag light mode.");
            public static readonly GUIContent FrameLimit = EditorGUIUtility.TrTextContent("帧率锁定: ", "当前的帧率 ultra = 60, standard = 30.");
            public static readonly GUIContent VolumeProfile = EditorGUIUtility.TrTextContent("镜头效果: ", "只能调色，别的不开");
            public static readonly GUIContent PostProssType = EditorGUIUtility.TrTextContent("后处理开关: ", "只允许主相机或者最后一个相机渲染Post");
            public static readonly GUIContent OccluderStencilLayerMask = EditorGUIUtility.TrTextContent("遮挡LayerMask: ", "在该layer层后的物体和开启遮挡描边");
            public static readonly GUIContent Histogram = EditorGUIUtility.TrTextContent("色彩直方图: ", "开启色彩直方图debug");
        }
        SerializedProperty m_LightModes;
        SerializedProperty m_FrameLimit;
        SerializedProperty m_SharedProfile;
        SerializedProperty m_PostProcessType;
        SerializedProperty m_PostProcessData;
        SerializedProperty m_OccluderStencilLayerMask;
        SerializedProperty m_Histogram;
        
        bool isDebug = true;

        //List<VolumeComponentEditor> m_Editors = new List<VolumeComponentEditor>();
        private void OnEnable() {
            m_LightModes = serializedObject.FindProperty("m_ShaderTagLightModes");
            m_FrameLimit = serializedObject.FindProperty("m_FrameLimit");
            m_SharedProfile = serializedObject.FindProperty("m_SharedProfile");
            m_PostProcessType = serializedObject.FindProperty("postProssType");
            m_PostProcessData = serializedObject.FindProperty("postProcessData");
            m_OccluderStencilLayerMask = serializedObject.FindProperty("m_OccluderStencilLayerMask");
            m_Histogram = serializedObject.FindProperty("m_Histogram");
        }
        public override void OnInspectorGUI() {
            serializedObject.Update();
            EditorGUILayout.Space();
            

            EditorGUILayout.PropertyField(m_LightModes, Styles.LightModes);
            EditorGUILayout.PropertyField(m_FrameLimit, Styles.FrameLimit);
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_PostProcessType, Styles.PostProssType);
            if (EditorGUI.EndChangeCheck()) {
                if(m_PostProcessData.objectReferenceValue == null)
                    // postProcessData 默认Data
                    m_PostProcessData.objectReferenceValue = PostProcessData.GetDefaultPostProcessData();
            }
            EditorGUILayout.PropertyField(m_OccluderStencilLayerMask, Styles.OccluderStencilLayerMask);

            isDebug = EditorGUILayout.Foldout(isDebug, "Debug");
            if (isDebug) {
                EditorGUILayout.PropertyField(m_Histogram, Styles.Histogram);
            }
            //EditorGUILayout.PropertyField(m_SharedProfile, Styles.VolumeProfile);     // 自定义 profile 不开放
            serializedObject.ApplyModifiedProperties();
        }
    }
}
