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

        void DrawRenderersBySelectOutline(ref CommandBuffer cmd, ref RenderingData renderingData, int passIndex = 2) {
            if (EffectsManager.EffectsTriggers == null) {
                return ;
            }

            int outlineID = Shader.PropertyToID("_SelectOutlineTex");
            bool isCreatTex = true;
            float widthOutline = 0;
            Color colorOutline = Color.black;
            foreach (var effectsTrigger in EffectsManager.EffectsTriggers) {
                var (isActive, width, color) = effectsTrigger.GetOutlineParam();

                if (isActive) {
                    foreach (var renderer in effectsTrigger.GetRenderers()) {
                        if (renderer == null) {
                            continue;
                        }

                        if (isCreatTex) {
                            cmd.GetTemporaryRT(outlineID, renderingData.cameraData.renderer.cameraColorTargetHandle.rt.descriptor);
                            cmd.SetRenderTarget(outlineID, renderingData.cameraData.renderer.cameraDepthTargetHandle);
                            cmd.ClearRenderTarget(false, true, Color.clear);
                            isCreatTex = false;
                        }

                        // renderer.GetPropertyBlock(materialPropertyBlock);
                        // materialPropertyBlock.SetFloat(Shader.PropertyToID("_OutlineWidth"), width);
                        // materialPropertyBlock.SetColor(Shader.PropertyToID("_OutlineColor"), color);
                        // renderer.SetPropertyBlock(materialPropertyBlock);
                        widthOutline = width;
                        colorOutline = color;
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

            if (!isCreatTex) {
                //cmd.SetGlobalTexture("_SelectOutlineTex", outlineID);
                // 暂时是使用EffectsManager.EffectsTriggers中最后一个开启OutLine得参数，需要根据玩法需求进行调整
                m_Material.SetFloat(Shader.PropertyToID("_OutlineWidth"), widthOutline);
                m_Material.SetColor(Shader.PropertyToID("_OutlineColor"), colorOutline);
                cmd.SetRenderTarget(renderingData.cameraData.renderer.cameraColorTargetHandle);
                Blitter.BlitTexture(cmd, Vector4.one, m_Material, 3);
                cmd.ReleaseTemporaryRT(outlineID);
            }
        }
    }
}