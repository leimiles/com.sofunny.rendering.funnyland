using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SoFunny.Rendering.Funnyland {
    /// <summary>
    /// 受击特效
    /// </summary>
    public class EffectsPass : ScriptableRenderPass {
        Material m_Material;
        MaterialPropertyBlock materialPropertyBlock;
        ProfilingSampler m_ProfilingSampler;
        List<Material> m_sharedMaterials;
        public EffectsPass(RenderPassEvent renderPassEvent, Material material) {
            m_Material = material;
            this.renderPassEvent = renderPassEvent;
            materialPropertyBlock = new MaterialPropertyBlock();
            EffectsManager.Init();
            m_ProfilingSampler = new ProfilingSampler("EffectsPass");
            m_sharedMaterials = new List<Material>();
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            var cmd = renderingData.commandBuffer;
            CameraType cameraType = renderingData.cameraData.camera.cameraType;
            if (cameraType != CameraType.Game && cameraType != CameraType.SceneView) {
                return;
            }
            using (new ProfilingScope(cmd, m_ProfilingSampler)) {
                DrawRenderersByAttacked(ref cmd, 0);
                DrawRenderersByOccluder(ref cmd, 1);
                DrawRenderersBySelectOutline(ref cmd, ref renderingData, 2);
            }
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
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
                        
                        m_sharedMaterials.Clear();
                        renderer.GetSharedMaterials(m_sharedMaterials);
                        for (int i = 0; i < m_sharedMaterials.Count; i++) {
                            if (m_sharedMaterials == null)
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
                        
                        m_sharedMaterials.Clear();
                        renderer.GetSharedMaterials(m_sharedMaterials);
                        for (int i = 0; i < m_sharedMaterials.Count; i++) {
                            if (m_sharedMaterials == null)
                                continue;

                            cmd.DrawRenderer(renderer, m_Material, i, passIndex);
                        }
                    }
                }
            }
        }

        void DrawRenderersBySelectOutline(ref CommandBuffer cmd, ref RenderingData renderingData, int passIndex = 2, int passStencilIndex = 3) {
            if (EffectsManager.EffectsTriggers == null) {
                return ;
            }
            
            foreach (var effectsTrigger in EffectsManager.EffectsTriggers) {
                var (isActive, width, color) = effectsTrigger.GetOutlineParam();

                if (isActive) {
                    // 写入模板测试
                    foreach (var renderer in effectsTrigger.GetRenderers()) {
                        if (renderer == null) {
                            continue;
                        }

                        m_sharedMaterials.Clear();
                        renderer.GetSharedMaterials(m_sharedMaterials);
                        for (int i = 0; i < m_sharedMaterials.Count; i++) {
                            if (m_sharedMaterials == null)
                                continue;

                            cmd.DrawRenderer(renderer, m_Material, i, passStencilIndex);
                        }
                    }
                }
            }
            
            foreach (var effectsTrigger in EffectsManager.EffectsTriggers) {
                var (isActive, width, color) = effectsTrigger.GetOutlineParam();

                if (isActive) {
                    // 外扩描边
                    foreach (var renderer in effectsTrigger.GetRenderers()) {
                        if (renderer == null) {
                            continue;
                        }

                        renderer.GetPropertyBlock(materialPropertyBlock);
                        // materialPropertyBlock.SetVector(Shader.PropertyToID("_MeshCenter"),   renderer.transform.worldToLocalMatrix.MultiplyPoint(renderer.bounds.center));
                        materialPropertyBlock.SetFloat(Shader.PropertyToID("_OutlineWidth"), width);
                        materialPropertyBlock.SetColor(Shader.PropertyToID("_OutlineColor"), color);
                        renderer.SetPropertyBlock(materialPropertyBlock);
                        
                        for (int i = 0; i < m_sharedMaterials.Count; i++) {
                            if (m_sharedMaterials == null)
                                continue;

                            cmd.DrawRenderer(renderer, m_Material, i, passIndex);
                        }
                    }
                }
            }
        }
    }
}