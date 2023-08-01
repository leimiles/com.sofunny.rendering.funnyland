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
        }
        SerializedProperty m_LightModes;
        SerializedProperty m_UseFunnySky;
        private void OnEnable() {
            m_LightModes = serializedObject.FindProperty("m_ShaderTagLightModes");
        }
        public override void OnInspectorGUI() {
            serializedObject.Update();
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_LightModes, Styles.LightModes);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
