using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FRP.Rendering {
    /// <summary>
    /// 受击特效
    /// </summary>
    public class EffectsPass : ScriptableRenderPass {
        Material m_Material;
        MaterialPropertyBlock materialPropertyBlock;
        ProfilingSampler m_ProfilingSampler;

        public EffectsPass(RenderPassEvent renderPassEvent, Material material) {
            m_Material = material;
            this.renderPassEvent = renderPassEvent;
            materialPropertyBlock = new MaterialPropertyBlock();
            EffectsManager.Init();
            m_ProfilingSampler = new ProfilingSampler("EffectsPass");
            
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            var cmd = renderingData.commandBuffer;

            using (new ProfilingScope(cmd, m_ProfilingSampler)) {
                DrawRenderersByAttacked(ref cmd, 0);
                DrawRenderersBySelectOutline(ref cmd, 2);
                DrawRenderersByOccluder(ref cmd, 1);
            }
        }

        void DrawRenderersByAttacked(ref CommandBuffer cmd, int passIndex = 0) {
            if (EffectsManager.EffectsTriggers == null) {
                return ;
            }
            foreach (var effectsTrigger in EffectsManager.EffectsTriggers) {
                foreach (var renderer in effectsTrigger.GetRenderers()) {
                    if (renderer == null) {
                        continue;
                    }
                    if (effectsTrigger.attackedColorIntensity != 0) {
                        renderer.GetPropertyBlock(materialPropertyBlock);
                        materialPropertyBlock.SetColor(Shader.PropertyToID("_Color"), effectsTrigger.attackedColor);
                        materialPropertyBlock.SetFloat(Shader.PropertyToID("_AttackedColorIntensity"), effectsTrigger.attackedColorIntensity);
                        renderer.SetPropertyBlock(materialPropertyBlock);
                        Material[] sharedMaterials = renderer.sharedMaterials;
                        for (int i = 0; i < sharedMaterials.Length; i++) {
                            if (sharedMaterials == null)
                                continue;

                            cmd.DrawRenderer(renderer, m_Material, i, passIndex);
                        }
                    }
                }
            }
        }

        void DrawRenderersByOccluder(ref CommandBuffer cmd, int passIndex = 1) {
            if (EffectsManager.EffectsTriggers == null) {
                return ;
            }
            foreach (var effectsTrigger in EffectsManager.EffectsTriggers) {
                var (isActive, intensity, color) = effectsTrigger.GetOccludeeParam();

                if (isActive) {
                    foreach (var renderer in effectsTrigger.GetRenderers()) {
                        if (renderer == null) {
                            continue;
                        }

                        renderer.GetPropertyBlock(materialPropertyBlock);
                        materialPropertyBlock.SetFloat(Shader.PropertyToID("_OccludeeColorIntensity"), intensity);
                        materialPropertyBlock.SetColor(Shader.PropertyToID("_OccludeeColor"), color);
                        renderer.SetPropertyBlock(materialPropertyBlock);
                        Material[] sharedMaterials = renderer.sharedMaterials;
                        for (int i = 0; i < sharedMaterials.Length; i++) {
                            if (sharedMaterials == null)
                                continue;

                            cmd.DrawRenderer(renderer, m_Material, i, passIndex);
                        }
                    }
                }
            }
        }

        void DrawRenderersBySelectOutline(ref CommandBuffer cmd, int passIndex = 2) {
            if (EffectsManager.EffectsTriggers == null) {
                return ;
            }
            foreach (var effectsTrigger in EffectsManager.EffectsTriggers) {
                var (isActive, width, color) = effectsTrigger.GetOutlineParam();

                if (isActive) {
                    foreach (var renderer in effectsTrigger.GetRenderers()) {
                        if (renderer == null) {
                            continue;
                        }

                        renderer.GetPropertyBlock(materialPropertyBlock);
                        materialPropertyBlock.SetFloat(Shader.PropertyToID("_OutlineWidth"), width);
                        materialPropertyBlock.SetColor(Shader.PropertyToID("_OutlineColor"), color);
                        renderer.SetPropertyBlock(materialPropertyBlock);
                        Material[] sharedMaterials = renderer.sharedMaterials;
                        for (int i = 0; i < sharedMaterials.Length; i++) {
                            if (sharedMaterials == null)
                                continue;

                            cmd.DrawRenderer(renderer, m_Material, i, passIndex);
                        }
                    }
                }
            }
        }
    }
}