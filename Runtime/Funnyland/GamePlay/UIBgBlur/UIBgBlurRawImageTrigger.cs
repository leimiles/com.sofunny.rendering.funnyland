using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SoFunny.Rendering.Funnyland
{
    public class UIBgBlurRawImageTrigger : UIBgBlurTriggerBase
    {
        public RawImage rawImage;

        private void Start()
        {
            if (rawImage == null)
            {
                rawImage = GetComponent<RawImage>();
            }
            
            StopAllCoroutines();
            StartCoroutine(RequestBlurRT());
        }
        
        private void OnDestroy()
        {
            if (rawImage != null)
            {
                rawImage.texture = null;
            }
        }

        protected override void OnBlurRT(RenderTexture uiBlurRT)
        {
            if (rawImage != null)
            {
                rawImage.texture = uiBlurRT;
            }
        }
    }
}