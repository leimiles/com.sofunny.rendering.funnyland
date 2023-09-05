using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoFunny.Rendering.Funnyland {
    public class EffectsManager {
        static List<EffectsTrigger> effectsTriggers;

        public static List<EffectsTrigger> EffectsTriggers {
            get { return effectsTriggers; }
        }

        public static bool state = false;

        public static void Init() {
            if (effectsTriggers == null) {
                effectsTriggers = new List<EffectsTrigger>();
            }

            state = true;
        }

        public static void AddTrigger(EffectsTrigger effectsTrigger) {
            if (!effectsTriggers.Contains(effectsTrigger)) {
                effectsTriggers.Add(effectsTrigger);
            }
        }

        public static void RemoveTrigger(EffectsTrigger effectsTrigger) {
            if (effectsTriggers.Contains(effectsTrigger)) {
                effectsTriggers.Remove(effectsTrigger);
            }
        }

        public static int Count() {
            return effectsTriggers.Count;
        }

        public static bool Exists(EffectsTrigger effectsTrigger) {
            return effectsTriggers.Contains(effectsTrigger);
        }
    }
}