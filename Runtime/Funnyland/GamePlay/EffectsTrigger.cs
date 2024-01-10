using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace SoFunny.Rendering.Funnyland {
    [DisallowMultipleComponent]
    public class EffectsTrigger : MonoBehaviour {
        [SerializeField] private AttackedParam attack;
        [SerializeField] private OutlineParam outline;
        [SerializeField] private OccludeeParam occludee;

        private Renderer[] m_Renderers;
        private Material m_EffectMaterial;

        private Renderer[] renderers {
            get {
                if (m_Renderers == null || m_Renderers.Length == 0) {
                    m_Renderers = gameObject.GetComponentsInChildren<Renderer>();
                }
                return m_Renderers;
            }    
        }

        private Material effectMaterial {
            get {
                if (m_EffectMaterial == null) {
                    m_EffectMaterial = new Material(Shader.Find("Hidden/SoFunny/Funnyland/FunnyEffects"));
                }

                return m_EffectMaterial;
            }   
        }
        
        // 测试
        // private void Start() {
        //     SetOutlineState(true);
        // }

        public void SetAttackedState(bool isAttackedActive) {
            if (isAttackedActive) {
                attack.renderers = renderers;
                attack.material = effectMaterial;
                attack.isActive = isAttackedActive;
                EffectsManager.AddAttackedTrigger(attack);
            } else {
                attack.isActive = isAttackedActive;
                EffectsManager.RemoveAttackedTrigger(attack);
            }
        }
        
        public void SetOutlineState(bool isOutlineActive) {
            if (isOutlineActive) {
                outline.renderers = renderers;
                outline.material = effectMaterial;
                outline.isActive = isOutlineActive;
                EffectsManager.AddOutlineTrigger(outline);
            } else {
                outline.isActive = isOutlineActive;
                EffectsManager.RemoveOutlineTrigger(outline);
            }
        }
        
        public void SetOccludeeState(bool isOccludeeActive) {
            if (isOccludeeActive) {
                occludee.renderers = renderers;
                occludee.material = effectMaterial;
                occludee.isActive = isOccludeeActive;
                EffectsManager.AddOccludeeTrigger(occludee);
            } else {
                occludee.isActive = isOccludeeActive;
                EffectsManager.RemoveOccludeeTrigger(occludee);
            }
        }

        private void OnDestroy() {
            Destroy(m_EffectMaterial);
            EffectsManager.RemoveAttackedTrigger(attack);
            EffectsManager.RemoveOutlineTrigger(outline);
            EffectsManager.RemoveOccludeeTrigger(occludee);
        }
    }
    
    [System.Serializable]
    public class EffectParam {
        [HideInInspector]public bool isActive = false;
        [HideInInspector]public Material material;
        [HideInInspector]public Renderer[] renderers;
        
        public Renderer[] GetRenderers() {
            return renderers;
        }
        
        public Material GetMaterial() {
            if (material == null) {
                material = new Material(Shader.Find("Hidden/SoFunny/Funnyland/FunnyEffects"));
            }
            return material;
        }
    }
    
    [System.Serializable]
    public class AttackedParam : EffectParam{
        [Range(0f, 1f)] public float intensity;
        [ColorUsageAttribute(true, true)] public Color color;

        public (bool, float, Color) GetParams() {
            if (this.isActive) {
                return (true, this.intensity, this.color);
            }

            return (false, 0f, Color.black);
        }
    }
    
    [System.Serializable]
    public class OutlineParam : EffectParam{
        [Range(0f, 5.0f)] public float with;
        [ColorUsageAttribute(true, true)] public Color color;
        
        public (bool, float, Color) GetParams() {
            if (this.isActive) {
                return (true, this.with, this.color);
            }

            return (false, 0f, Color.black);
        }
    }

    [System.Serializable]
    public class OccludeeParam : EffectParam{
        [Range(0f, 1f)] public float intensity;
        [ColorUsageAttribute(true, true)] public Color color;
        
        public (bool, float, Color) GetParams() {
            if (this.isActive) {
                return (true, this.intensity, this.color);
            }

            return (false, 0f, Color.black);
        }
    }
}