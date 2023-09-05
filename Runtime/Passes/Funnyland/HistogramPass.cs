using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace SoFunny.Rendering.Funnyland {
    public class HistogramPass : ScriptableRenderPass {
        ComputeBuffer m_Data;
        ComputeBuffer m_RData;
        ComputeBuffer m_GData;
        ComputeBuffer m_BData;

        ComputeShader m_ComputeShader;
        Material m_Material;

        ProfilingSampler m_ProfilingSampler;

        int m_NumBins = 256;
        bool isRGB;
        const int m_ThreadGroupSizeX = 16;
        const int m_ThreadGroupSizeY = 16;

        RTHandle m_Source;

        /// <summary>
        /// The width of the rendered histogram.
        /// </summary>
        public int width = 512;

        /// <summary>
        /// The height of the rendered histogram.
        /// </summary>
        public int height = 256;

        HistogramChannel channel;

        public HistogramPass(ComputeShader cs, RenderPassEvent renderPassEvent, Material mat, HistogramChannel histogramChannel) {
            this.m_ComputeShader = cs;
            this.renderPassEvent = renderPassEvent;
            this.m_Material = mat;
            channel = histogramChannel;
            isRGB = channel == HistogramChannel.RGB;
            if (isRGB) {
                m_NumBins = m_NumBins * 3;
                m_Data?.Release();
                m_Data = new ComputeBuffer(m_NumBins, sizeof(uint));
            } else {
                m_NumBins = 256;
                m_Data?.Release();
                m_Data = new ComputeBuffer(m_NumBins, sizeof(uint));
            }

            m_ProfilingSampler = new ProfilingSampler("Histogram");
        }

        public void Setup(RTHandle colorHandle) {
            m_Source = colorHandle;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            if (channel == HistogramChannel.None) {
                return;
            }

            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, m_ProfilingSampler)) {
                //Clear
                var parameters = new Vector4(renderingData.cameraData.pixelWidth / 2, renderingData.cameraData.pixelHeight / 2, 1, (int)channel);
                cmd.SetComputeVectorParam(m_ComputeShader, "_Params", parameters);

                int kernel = m_ComputeShader.FindKernel("HistogramClear");
                cmd.SetComputeBufferParam(m_ComputeShader, kernel, "_HistogramBuffer", m_Data);
                cmd.DispatchCompute(m_ComputeShader, kernel, Mathf.CeilToInt(m_NumBins / (float)m_ThreadGroupSizeX), 1, 1);

                //Gather all pixels
                kernel = m_ComputeShader.FindKernel("HistogramGather");

                // x:Width, y:Height, z:IsLinear?, w:Render Channel
                // var cameraTarget = renderingData.cameraData.targetTexture;
                if (m_Source == renderingData.cameraData.renderer.GetCameraColorFrontBuffer(cmd)) {
                    m_Source = renderingData.cameraData.renderer.cameraColorTargetHandle;
                }

                cmd.SetComputeTextureParam(m_ComputeShader, kernel, "_Source", m_Source);
                cmd.SetComputeBufferParam(m_ComputeShader, kernel, "_HistogramBuffer", m_Data);
                cmd.DispatchCompute(m_ComputeShader, kernel, Mathf.CeilToInt(parameters.x / m_ThreadGroupSizeX), Mathf.CeilToInt(parameters.y / m_ThreadGroupSizeY), 1);
                m_Material.SetBuffer("_HistogramBuffer", m_Data);
                isEnableKeyword(m_Material, isRGB);

                // 在Direct3D11 Api 时如果不设置渲染目标的话会渲染给到GUI上 暂时不知道原因 怀疑SScriptableRenderer中有限制
                cmd.SetRenderTarget(m_Source);
                Blitter.BlitTexture(cmd, Vector4.one, m_Material, 0);
                //cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_Material);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        void isEnableKeyword(Material mat, bool isEnable) {
            if (isEnable) {
                mat.EnableKeyword("_HISTOGRAM_RGB");
            } else {
                mat.DisableKeyword("_HISTOGRAM_RGB");
            }
        }

        public void Dispose() {
            m_Data?.Release();
        }
    }

    public enum HistogramChannel {
        /// <summary>
        /// The red channel.
        /// </summary>
        Red,

        /// <summary>
        /// The green channel.
        /// </summary>
        Green,

        /// <summary>
        /// The blue channel.
        /// </summary>
        Blue,

        /// <summary>
        /// The master (luminance) channel.
        /// </summary>
        Master,

        /// <summary>
        /// RGB channel.
        /// </summary>
        RGB,

        /// <summary>
        /// 不开启 Histogram
        /// </summary>
        None
    }
}