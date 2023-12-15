using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace SoFunny.Rendering.Funnyland {
    internal struct FunnyDebugPasses : IDisposable {
        public DrawObjectsPass m_DebugOpaqueForwardPass;
        public DrawObjectsPass m_DebugTransparentForwardPass;
        private DebugModeType m_CurrentDebugMode;

        private static readonly string drawOpaqueForwardDebugPass = "Draw Opaque Forward Debug Pass";
        private static readonly string drawTransparentForwardDebugPass = "Draw Transparent Forward Debug Pass";
        private ShaderTagId[] debugTagIds;

        public bool isCreated {
            get => m_CurrentDebugMode != DebugModeType.Off;
        }
        
        public FunnyDebugPasses(DebugModeType debugModeType, StencilState defaultStencilState, StencilStateData stencilData) {
            m_CurrentDebugMode = debugModeType;
            debugTagIds = new ShaderTagId[1];
            debugTagIds[0] = new ShaderTagId("DebugPass");
            m_DebugOpaqueForwardPass = null;
            m_DebugTransparentForwardPass = null;
            
            if (isCreated) {
                m_DebugOpaqueForwardPass = new DrawObjectsPass(drawOpaqueForwardDebugPass, debugTagIds, true,RenderPassEvent.BeforeRenderingOpaques, RenderQueueRange.opaque, -1, defaultStencilState, stencilData.stencilReference);
                m_DebugTransparentForwardPass = new DrawObjectsPass(drawTransparentForwardDebugPass, debugTagIds, false,RenderPassEvent.BeforeRenderingTransparents, RenderQueueRange.transparent, -1, defaultStencilState, stencilData.stencilReference);
            }
        }
        

        public void Dispose() {

        }
    }
}