using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace SoFunny.Rendering.Funnyland {
#if UNITY_EDITOR
    // 为后续多debug准备 暂时DebugPass只有一个Debug所以并没有使用keyward
    class DebugKeyward {
        public const string Hlod = "_Debug_Hlod";
    }
    internal struct FunnyDebugPasses {
        public DrawObjectsPass DebugOpaqueForwardPass;
        public DrawObjectsPass DebugTransparentForwardPass;
        private DebugModeType m_CurrentDebugMode;
        private string m_DebugKeyword;
        private static readonly string m_DrawOpaqueForwardDebugPass = "Draw Opaque Forward Debug Pass";
        private static readonly string m_DrawTransparentForwardDebugPass = "Draw Transparent Forward Debug Pass";
        private ShaderTagId[] m_DebugTagIds;

        public bool isCreated {
            get => m_CurrentDebugMode != DebugModeType.Off;
        }
        
        public FunnyDebugPasses(DebugModeType debugModeType, StencilState defaultStencilState, StencilStateData stencilData) {
            m_CurrentDebugMode = debugModeType;
            m_DebugTagIds = new ShaderTagId[1];
            m_DebugTagIds[0] = new ShaderTagId("DebugPass");
            DebugOpaqueForwardPass = null;
            DebugTransparentForwardPass = null;
            m_DebugKeyword = null;
            
            DebugKeyword();
            if (isCreated) {
                DebugOpaqueForwardPass = new DrawObjectsPass(m_DrawOpaqueForwardDebugPass, m_DebugTagIds, true,RenderPassEvent.BeforeRenderingOpaques, RenderQueueRange.opaque, -1, defaultStencilState, stencilData.stencilReference);
                DebugTransparentForwardPass = new DrawObjectsPass(m_DrawTransparentForwardDebugPass, m_DebugTagIds, false,RenderPassEvent.BeforeRenderingTransparents, RenderQueueRange.transparent, -1, defaultStencilState, stencilData.stencilReference);
            }
        }
        

        public void DebugKeyword() {
            switch (m_CurrentDebugMode) {
                case DebugModeType.HlodDebug:
                    m_DebugKeyword = "Hlod";
                    break;
            }
        }
    }
#endif
}