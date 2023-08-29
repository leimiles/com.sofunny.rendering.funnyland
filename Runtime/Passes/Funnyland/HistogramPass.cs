using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;
namespace SoFunny.Rendering.Funnyland
{
    public class HistogramPass : ScriptableRenderPass
    {
        ComputeBuffer m_Data;
        ComputeShader m_ComputeShader;
        Material m_Material;
        
        ProfilingSampler m_ProfilingSampler;

        const int m_NumBins = 256;
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
        public HistogramPass(ComputeShader cs, RenderPassEvent renderPassEvent, Material mat) {
            this.m_ComputeShader = cs;
            this.renderPassEvent = renderPassEvent;
            this.m_Material = mat;
            if (m_Data == null)
                m_Data = new ComputeBuffer(m_NumBins, sizeof(uint));
            m_ProfilingSampler = new ProfilingSampler("Histogram");

        }
        
        public void Setup(RTHandle colorHandle, HistogramChannel histogramChannel)
        {
            m_Source = colorHandle;
            channel = histogramChannel;
        }
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            if (channel == HistogramChannel.None) {
                return;
            }
            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, m_ProfilingSampler)) {
                //Clear
                int kernel = m_ComputeShader.FindKernel("HistogramClear");
                cmd.SetComputeBufferParam(m_ComputeShader, kernel, "_HistogramBuffer", m_Data);
                cmd.DispatchCompute(m_ComputeShader, kernel, Mathf.CeilToInt(m_NumBins / (float)m_ThreadGroupSizeX), 1, 1);
                
                //Gather all pixels
                kernel = m_ComputeShader.FindKernel("HistogramGather");
                
                // x:Width, y:Height, z:IsLinear?, w:Render Channel
                var parameters = new Vector4(
                    renderingData.cameraData.pixelWidth / 2,
                    renderingData.cameraData.pixelHeight / 2,
                    1,
                    (int)channel
                );
                // var cameraTarget = renderingData.cameraData.targetTexture;
                if (m_Source == renderingData.cameraData.renderer.GetCameraColorFrontBuffer(cmd))
                {
                    m_Source = renderingData.cameraData.renderer.cameraColorTargetHandle;
                }
                cmd.SetComputeVectorParam(m_ComputeShader, "_Params", parameters);
                cmd.SetComputeTextureParam(m_ComputeShader, kernel, "_Source", m_Source);
                cmd.SetComputeBufferParam(m_ComputeShader, kernel, "_HistogramBuffer", m_Data);
                cmd.DispatchCompute(m_ComputeShader, kernel,
                    Mathf.CeilToInt(parameters.x / m_ThreadGroupSizeX),
                    Mathf.CeilToInt(parameters.y / m_ThreadGroupSizeY),
                    1
                );

                m_Material.SetBuffer("_HistogramBuffer", m_Data);
                //Blitter.BlitTexture(cmd, cameraTarget, Vector2.one, m_Material, 0);
                cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_Material);
            }
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
        
        public void Dispose()
        {
            m_Data?.Release();
        }

    }
    public enum HistogramChannel
    {
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
        /// 不开启 Histogram
        /// </summary>
        None
    }
}
