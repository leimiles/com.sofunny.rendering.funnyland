using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace SoFunny.Rendering.Funnyland {
    [Serializable]
    public enum GlobalTextureMipmapLevel {
        Full = 0,
        Half = 1,
        Quarter = 2,
        Eighth = 3
    }
    
    public enum ShaderQuality {
        High,
        Low,
        Custom
    }
    
    // 画质分级暂时不需data
    // [CreateAssetMenu(fileName = "GraphicQualitySettingData", menuName = "ScriptableObject/GraphicQualitySettingData", order = 0)]
    public class FunnyGraphicQualitySettingData : ScriptableObject {
        public bool isSimpleRendering;
        public bool shadow;
        public bool post;
        public bool anisotropicTexture;
        public GlobalTextureMipmapLevel globalTextureMipmapLevel;
        public ShaderQuality shaderQuality;
    }
}