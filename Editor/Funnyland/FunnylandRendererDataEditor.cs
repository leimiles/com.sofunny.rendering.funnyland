using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering.Universal;

namespace SoFunny.Rendering.Funnyland {
    [CustomEditor(typeof(FunnylandMobileRendererData), true)]
    public class FunnylandMobileRendererDataEditor : ScriptableRendererDataEditor {
        private static class Styles {
            public static readonly GUIContent LightModes = EditorGUIUtility.TrTextContent("LightModes: ", "允许渲染的 shader tag light mode.");
            public static readonly GUIContent FrameLimit = EditorGUIUtility.TrTextContent("帧率锁定: ", "当前的帧率 ultra = 60, standard = 30.");
        }
        SerializedProperty m_LightModes;
        SerializedProperty m_FrameLimit;
        private void OnEnable() {
            m_LightModes = serializedObject.FindProperty("m_ShaderTagLightModes");
            m_FrameLimit = serializedObject.FindProperty("m_FrameLimit");
        }
        public override void OnInspectorGUI() {
            serializedObject.Update();
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_LightModes, Styles.LightModes);
            EditorGUILayout.PropertyField(m_FrameLimit, Styles.FrameLimit);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
