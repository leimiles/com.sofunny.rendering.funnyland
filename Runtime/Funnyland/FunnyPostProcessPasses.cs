using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace SoFunny.Rendering.Funnyland
{
    /// <summary>
    /// Run-time creation parameters for post process passes.
    /// </summary>
    internal struct PostProcessParams
    {
        /// <summary>
        /// The blit <c>Material</c> to use.
        /// </summary>
        /// <seealso cref="Material"/>
        public Material blitMaterial;
        /// <summary>
        /// Requested <c>GraphicsFormat</c> for HDR postprocess rendering.
        /// </summary>
        /// <seealso cref="GraphicsFormat"/>
        public GraphicsFormat requestHDRFormat;

        /// <summary>
        /// A static factory function for default initialization of PostProcessParams.
        /// </summary>
        /// <returns></returns>
        public static PostProcessParams Create()
        {
            PostProcessParams ppParams;
            ppParams.blitMaterial = null;
            ppParams.requestHDRFormat = GraphicsFormat.None;
            return ppParams;
        }
    }

    /// <summary>
    /// Type acts as wrapper for post process passes. Can we be recreated and destroyed at any point during runtime with post process data.
    /// </summary>
    internal struct FunnyPostProcessPasses : IDisposable
    {
        FunnyColorGradingLutPass m_ColorGradingLutPass;
        FunnyPostProcessPass m_PostProcessPass;
        FunnyPostProcessPass m_FinalPostProcessPass;

        internal RTHandle m_AfterPostProcessColor;
        internal RTHandle m_ColorGradingLut;

        PostProcessData m_RendererPostProcessData;
        PostProcessData m_CurrentPostProcessData;
        Material m_BlitMaterial;

        public FunnyColorGradingLutPass colorGradingLutPass { get => m_ColorGradingLutPass; }
        public FunnyPostProcessPass postProcessPass { get => m_PostProcessPass; }
        public FunnyPostProcessPass finalPostProcessPass { get => m_FinalPostProcessPass; }
        public RTHandle afterPostProcessColor { get => m_AfterPostProcessColor; }
        public RTHandle colorGradingLut { get => m_ColorGradingLut; }

        public bool isCreated { get => m_CurrentPostProcessData != null; }

        /// <summary>
        /// Creates post process passes with supplied data anda params.
        /// </summary>
        /// <param name="rendererPostProcessData">Post process resources.</param>
        /// <param name="postProcessParams">Post process run-time creation parameters.</param>
        public FunnyPostProcessPasses(PostProcessData rendererPostProcessData, ref PostProcessParams postProcessParams)
        {
            m_ColorGradingLutPass = null;
            m_PostProcessPass = null;
            m_FinalPostProcessPass = null;
            m_CurrentPostProcessData = null;

            m_AfterPostProcessColor = null;
            m_ColorGradingLut = null;

            m_RendererPostProcessData = rendererPostProcessData;
            m_BlitMaterial = postProcessParams.blitMaterial;
            Recreate(rendererPostProcessData, ref postProcessParams);
        }

        /// <summary>
        /// Recreates post process passes with supplied data. If already contains valid post process passes, they will be replaced by new ones.
        /// </summary>
        /// <param name="data">Resources used for creating passes. In case of the null, no passes will be created.</param>
        /// <param name="ppParams">Run-time parameters used for creating passes.</param>
        public void Recreate(PostProcessData data, ref PostProcessParams ppParams)
        {
            if (m_RendererPostProcessData)
                data = m_RendererPostProcessData;

            if (data == m_CurrentPostProcessData)
                return;

            if (m_CurrentPostProcessData != null)
            {
                m_ColorGradingLutPass?.Cleanup();
                m_PostProcessPass?.Cleanup();
                m_FinalPostProcessPass?.Cleanup();

                // We need to null post process passes to avoid using them
                m_ColorGradingLutPass = null;
                m_PostProcessPass = null;
                m_FinalPostProcessPass = null;
                m_CurrentPostProcessData = null;
            }

            if (data != null)
            {
                m_ColorGradingLutPass = new FunnyColorGradingLutPass(RenderPassEvent.BeforeRenderingPrePasses, data);
                m_PostProcessPass = new FunnyPostProcessPass(RenderPassEvent.BeforeRenderingPostProcessing, data, ref ppParams);
                m_FinalPostProcessPass = new FunnyPostProcessPass(RenderPassEvent.AfterRenderingPostProcessing, data, ref ppParams);
                m_CurrentPostProcessData = data;
            }
        }

        public void Dispose()
        {
            // always dispose unmanaged resources
            m_ColorGradingLutPass?.Cleanup();
            m_PostProcessPass?.Cleanup();
            m_FinalPostProcessPass?.Cleanup();
            m_AfterPostProcessColor?.Release();
            m_ColorGradingLut?.Release();
        }
        internal void ReleaseRenderTargets()
        {
            m_AfterPostProcessColor?.Release();
            m_PostProcessPass?.Dispose();
            m_FinalPostProcessPass?.Dispose();
            m_ColorGradingLut?.Release();
        }
        
        /// <summary>
        /// 判断是是否自定义 VolumeComponent 如果没有则使用默认即不开启对应效果
        /// </summary>
        public static void GetVolumeComponent<T>(PostVolumeData volumeData, out T volumeComponent) 
            where T : VolumeComponent
        {
            if (volumeData.sharedProfile.TryGet(out T temp)) {
                volumeComponent = temp;
            } else {
                volumeComponent = volumeData.sharedStack.GetComponent<T>();
            }
        }
    }
}
