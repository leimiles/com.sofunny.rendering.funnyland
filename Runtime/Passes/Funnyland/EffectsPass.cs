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
        // Material m_Material;
        // MaterialPropertyBlock materialPropertyBlock;
        ProfilingSampler m_ProfilingSampler;
        List<Material> m_sharedMaterials;
        public EffectsPass(RenderPassEvent renderPassEvent, Material material) {
            // m_Material = material;
            this.renderPassEvent = renderPassEvent;
            // materialPropertyBlock = new MaterialPropertyBlock();
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
            if (EffectsManager.AttackedParams == null) {
                return ;
            }
            foreach (var attackedParam in EffectsManager.AttackedParams) {
                var (isActive, intensity, color) = attackedParam.GetParams();
                if (isActive) {
                    foreach (var renderer in attackedParam.GetRenderers()) {
                        if (renderer == null) {
                            continue;
                        }
                        if (intensity != 0) {
                            // renderer.GetPropertyBlock(materialPropertyBlock);
                            // materialPropertyBlock.SetColor(Shader.PropertyToID("_Color"), color);
                            // materialPropertyBlock.SetFloat(Shader.PropertyToID("_AttackedColorIntensity"), intensity);
                            // renderer.SetPropertyBlock(materialPropertyBlock);
                            Material material = attackedParam.GetMaterial();
                            material.SetFloat("_AttackedColorIntensity", intensity);
                            material.SetColor("_Color", color);
                            
                            m_sharedMaterials.Clear();
                            renderer.GetSharedMaterials(m_sharedMaterials);
                            for (int i = 0; i < m_sharedMaterials.Count; i++) {
                                if (m_sharedMaterials == null)
                                    continue;
                                cmd.DrawRenderer(renderer, material, i, passIndex);
                            }
                        }
                    }
                }
            }
        }

        void DrawRenderersByOccluder(ref CommandBuffer cmd, int passIndex = 1) {
            if (EffectsManager.OccludeeParams == null) {
                return ;
            }
            foreach (var occludeeParam in EffectsManager.OccludeeParams) {
                var (isActive, intensity, color) = occludeeParam.GetParams();

                if (isActive) {
                    foreach (var renderer in occludeeParam.GetRenderers()) {
                        if (renderer == null) {
                            continue;
                        }

                        if (intensity != 0) {
                            // renderer.GetPropertyBlock(materialPropertyBlock);
                            // materialPropertyBlock.SetFloat(Shader.PropertyToID("_OccludeeColorIntensity"), intensity);
                            // materialPropertyBlock.SetColor(Shader.PropertyToID("_OccludeeColor"), color);
                            // renderer.SetPropertyBlock(materialPropertyBlock);
                            Material material = occludeeParam.GetMaterial();
                            material.SetFloat("_OccludeeColorIntensity", intensity);
                            material.SetColor("_OccludeeColor", color);

                            m_sharedMaterials.Clear();
                            renderer.GetSharedMaterials(m_sharedMaterials);
                            for (int i = 0; i < m_sharedMaterials.Count; i++) {
                                if (m_sharedMaterials == null)
                                    continue;

                                cmd.DrawRenderer(renderer, material, i, passIndex);
                            }
                        }
                    }
                }
            }
        }

        void DrawRenderersBySelectOutline(ref CommandBuffer cmd, ref RenderingData renderingData, int passIndex = 2, int passStencilIndex = 3) {
            if (EffectsManager.OutlineParams == null) {
                return ;
            }
            
            // foreach (var outlineParam in EffectsManager.OutlineParams) {
            //     var (isActive, width, color) = outlineParam.GetParams();
            //
            //     if (isActive) {
            //         // 写入模板测试
            //         foreach (var renderer in outlineParam.GetRenderers()) {
            //             if (renderer == null) {
            //                 continue;
            //             }
            //
            //             m_sharedMaterials.Clear();
            //             renderer.GetSharedMaterials(m_sharedMaterials);
            //             for (int i = 0; i < m_sharedMaterials.Count; i++) {
            //                 if (m_sharedMaterials == null)
            //                     continue;
            //
            //                 cmd.DrawRenderer(renderer, m_Material, i, passStencilIndex);
            //             }
            //         }
            //     }
            // }
            
            foreach (var outlineParam in EffectsManager.OutlineParams) {
                var (isActive, width, color) = outlineParam.GetParams();

                if (isActive) {
                    // 外扩描边
                    foreach (var renderer in outlineParam.GetRenderers()) {
                        if (renderer == null) {
                            continue;
                        }

                        if (width > 0) {
                            // materialPropertyBlock会同时打断其他的 Pass 的 SRPBatch
                            // renderer.GetPropertyBlock(materialPropertyBlock);
                            // materialPropertyBlock.SetFloat(Shader.PropertyToID("_OutlineWidth"), width);
                            // materialPropertyBlock.SetColor(Shader.PropertyToID("_OutlineColor"), color);
                            // renderer.SetPropertyBlock(materialPropertyBlock);
                            Material material = outlineParam.GetMaterial();
                            material.SetFloat("_OutlineWidth", width);
                            material.SetColor("_OutlineColor", color);

                            m_sharedMaterials.Clear();
                            renderer.GetSharedMaterials(m_sharedMaterials);

                            for (int i = 0; i < m_sharedMaterials.Count; i++) {
                                if (m_sharedMaterials == null)
                                    continue;
                                cmd.DrawRenderer(renderer, material, i, passIndex);
                            }
                        }
                    }
                }
            }
        }
    }
}