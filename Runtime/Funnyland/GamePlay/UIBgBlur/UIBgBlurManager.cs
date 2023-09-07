using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SoFunny.Rendering.Funnyland
{
    public class UIBgBlurManager
    {
        public UIBgBlurManager()
        {
            _uiBgBlurTriggerList = new List<UIBgBlurTriggerBase>();
        }

        private static UIBgBlurManager _instance;

        public static UIBgBlurManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new UIBgBlurManager();
                }

                return _instance;
            }
        }

        private readonly List<UIBgBlurTriggerBase> _uiBgBlurTriggerList = default;


        public bool SendRequest(UIBgBlurTriggerBase uiBgBlurTriggerBase)
        {
            if (_uiBgBlurTriggerList == null)
            {
                return false;
            }

            if (_uiBgBlurTriggerList.Contains(uiBgBlurTriggerBase))
            {
                return true;
            }

            _uiBgBlurTriggerList.Add(uiBgBlurTriggerBase);
            return true;
        }

        public int GetCount()
        {
            if (_uiBgBlurTriggerList == null)
            {
                return 0;
            }

            return _uiBgBlurTriggerList.Count;
        }

        public void Clear()
        {
            _uiBgBlurTriggerList?.Clear();
        }
    }
}