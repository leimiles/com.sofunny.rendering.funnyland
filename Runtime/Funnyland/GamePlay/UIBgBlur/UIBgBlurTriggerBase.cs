using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SoFunny.Rendering.Funnyland
{
    public class UIBgBlurTriggerBase : MonoBehaviour
    {
        private RenderTexture _blurRT;

        private static readonly int UIBlurRT = Shader.PropertyToID("_UIBlurRT");

        protected IEnumerator RequestBlurRT()
        {
            UIBgBlurManager.Instance.SendRequest(this);
            yield return null;

            if (_blurRT != null)
            {
                RenderTexture.ReleaseTemporary(_blurRT);
            }

            Texture uiBlurRT = Shader.GetGlobalTexture(UIBlurRT);
            RenderTexture destRT = RenderTexture.GetTemporary(uiBlurRT.width / 2, uiBlurRT.height / 2);
            Graphics.Blit(uiBlurRT, destRT);
            _blurRT = destRT;

            OnBlurRT(_blurRT);
        }

        protected virtual void OnBlurRT(RenderTexture uiBlurRT)
        {
        }

        private void OnDestroy()
        {
            if (_blurRT != null)
            {
                RenderTexture.ReleaseTemporary(_blurRT);
            }
        }
    }
}