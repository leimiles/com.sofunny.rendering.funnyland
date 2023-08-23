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
    
    [CreateAssetMenu(fileName = "GraphicQualitySettingData", menuName = "ScriptableObject/GraphicQualitySettingData", order = 0)]
    public class GraphicQualitySettingData : ScriptableObject {

        public bool shadow;
        public bool post;
        public bool anisotropicTexture;
        public GlobalTextureMipmapLevel globalTextureMipmapLevel;
    }
}