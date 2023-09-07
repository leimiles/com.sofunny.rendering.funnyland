using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SoFunny.Rendering.Funnyland
{
    public class FunnyUIBackgroundBlurPass : ScriptableRenderPass
    {
        private static readonly int BlurRadius = Shader.PropertyToID("_BlurRadius");

        private ProfilingSampler m_profilingSampler;

        private RTHandle m_UIBlurRTHandle;
        private RTHandle m_sourceRTHandle;
        private RenderTextureDescriptor m_baseRTDescriptor;
        private Material m_UIBgBlurMaterial;
        private float m_BlurRadius;
        private bool m_enable;

        private const int m_MaxPyramidSize = 8;
        private int m_MaxIterations = 2;
        private RTHandle[] m_UIBlurMipUpRT;
        private RTHandle[] m_UIBlurMipDownRT;
        private int[] m_UIBlurMipUpRTName;
        private int[] m_UIBlurMipDownRTName;

        internal FunnyUIBackgroundBlurPass(RenderPassEvent evt, bool enable)
        {
            renderPassEvent = evt;
            m_enable = enable;

            m_profilingSampler = new ProfilingSampler(nameof(FunnyUIBackgroundBlurPass));

            m_UIBlurMipUpRT = new RTHandle[m_MaxPyramidSize];
            m_UIBlurMipDownRT = new RTHandle[m_MaxPyramidSize];
            m_UIBlurMipUpRTName = new int[m_MaxPyramidSize];
            m_UIBlurMipDownRTName = new int[m_MaxPyramidSize];

            for (int i = 0; i < m_MaxPyramidSize; i++)
            {
                m_UIBlurMipUpRTName[i] = Shader.PropertyToID("_UIBlurMipUp" + i);
                m_UIBlurMipDownRTName[i] = Shader.PropertyToID("_UIBlurMipDown" + i);

                m_UIBlurMipUpRT[i] = RTHandles.Alloc(m_UIBlurMipUpRTName[i], name: "_UIBlurMipUp" + i);
                m_UIBlurMipDownRT[i] = RTHandles.Alloc(m_UIBlurMipDownRTName[i], name: "_UIBlurMipDown" + i);
            }
            
            m_MaxIterations = 2;
            m_BlurRadius = 0.2f;
        }

        internal void Setup(in RenderTextureDescriptor baseDescriptor, in RTHandle sourceRTHandle, Material uiBgBlurMaterial)
        {
            m_sourceRTHandle = sourceRTHandle;
            m_UIBgBlurMaterial = uiBgBlurMaterial;

            m_baseRTDescriptor = baseDescriptor;
            m_baseRTDescriptor.depthBufferBits = (int)DepthBits.None;
            m_baseRTDescriptor.useMipMap = false;
            m_baseRTDescriptor.autoGenerateMips = false;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!m_enable)
            {
                return;
            }
            CommandBuffer cmd = renderingData.commandBuffer;
            
            if (m_sourceRTHandle == renderingData.cameraData.renderer.GetCameraColorFrontBuffer(cmd)) {
                m_sourceRTHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;
            }
            
            using (new ProfilingScope(cmd, m_profilingSampler))
            {
                cmd.SetGlobalFloat(BlurRadius, m_BlurRadius);
                DoBlur(cmd);
            }
        }

        private void DoBlur(CommandBuffer cmd)
        {
            int tw = m_baseRTDescriptor.width >> 3;
            int th = m_baseRTDescriptor.height >> 3;

            // Determine the iteration count
            int maxSize = Mathf.Max(tw, th);
            int iterations = Mathf.FloorToInt(Mathf.Log(maxSize, 2f) - 1);
            int mipCount = Mathf.Clamp(iterations, 1, m_MaxIterations);

            // Set Texture Size
            RenderTextureDescriptor uiBlurRTDesc = m_baseRTDescriptor;
            uiBlurRTDesc.width = tw;
            uiBlurRTDesc.height = th;
            for (int i = 0; i < mipCount; i++)
            {
                RenderingUtils.ReAllocateIfNeeded(ref m_UIBlurMipUpRT[i], uiBlurRTDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: m_UIBlurMipUpRT[i].name);
                RenderingUtils.ReAllocateIfNeeded(ref m_UIBlurMipDownRT[i], uiBlurRTDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: m_UIBlurMipDownRT[i].name);
                uiBlurRTDesc.width = Mathf.Max(1, uiBlurRTDesc.width >> 1);
                uiBlurRTDesc.height = Mathf.Max(1, uiBlurRTDesc.height >> 1);
            }

            Blitter.BlitCameraTexture(cmd, m_sourceRTHandle, m_UIBlurMipDownRT[0], RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, m_UIBgBlurMaterial, 0);
            
            DoDualKawaseBlur(cmd, mipCount);
        }

        private void DoDualKawaseBlur(CommandBuffer cmd, int mipCount)
        {
            // Downsample -  pyramid
            RTHandle lastDown = m_UIBlurMipDownRT[0];
            for (int i = 1; i < mipCount; i++)
            {
                Blitter.BlitCameraTexture(cmd, lastDown, m_UIBlurMipDownRT[i], RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, m_UIBgBlurMaterial, 1);

                lastDown = m_UIBlurMipDownRT[i];
            }

            // Upsample
            RTHandle lastUp = m_UIBlurMipDownRT[mipCount - 1];
            for (int i = mipCount - 2; i >= 0; i--)
            {
                RTHandle mipUp = m_UIBlurMipUpRT[i];
                Blitter.BlitCameraTexture(cmd, lastUp, mipUp, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, m_UIBgBlurMaterial, 2);

                lastUp = mipUp;
            }
            
            Blitter.BlitCameraTexture(cmd, lastUp, m_sourceRTHandle, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, m_UIBgBlurMaterial, 0);
        }
    }
}