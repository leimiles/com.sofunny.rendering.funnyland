using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SoFunny.Rendering.Funnyland {
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class FunnyDecal : MonoBehaviour {
        MeshFilter meshFilter;
        MeshRenderer meshRenderer;
        void OnEnable() {
            meshFilter = GetComponent<MeshFilter>();
            meshFilter.sharedMesh = FunnylandMobileRendererData.MeshResources.decalBox;
            meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        }
    }
}
