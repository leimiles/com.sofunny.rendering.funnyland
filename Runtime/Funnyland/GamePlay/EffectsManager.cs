using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoFunny.Rendering.Funnyland {
    public class EffectsManager {
        // static List<EffectsTrigger> effectsTriggers;
        static List<AttackedParam> attackedParams;
        static List<OutlineParam> outlineParams;
        static List<OccludeeParam> occludeeParams;

        // public static List<EffectsTrigger> EffectsTriggers {
        //     get { return effectsTriggers; }
        // }
        
        public static List<AttackedParam> AttackedParams {
            get { return attackedParams; }
        }
        
        public static List<OutlineParam> OutlineParams {
            get { return outlineParams; }
        }
        
        public static List<OccludeeParam> OccludeeParams {
            get { return occludeeParams; }
        }
        
        public static bool state = false;

        public static void Init() {
            if (attackedParams == null) {
                attackedParams = new List<AttackedParam>();
            }
            if (outlineParams == null) {
                outlineParams = new List<OutlineParam>();
            }
            if (occludeeParams == null) {
                occludeeParams = new List<OccludeeParam>();
            }
            state = true;
        }
        
        public static void AddAttackedTrigger(AttackedParam attackedParam) {
            if (!attackedParams.Contains(attackedParam)) {
                attackedParams.Add(attackedParam);
            }
        }

        public static void RemoveAttackedTrigger(AttackedParam attackedParam) {
            if (attackedParams.Contains(attackedParam)) {
                attackedParams.Remove(attackedParam);
            }
        }
        
        public static void AddOutlineTrigger(OutlineParam outlineParam) {
            if (!outlineParams.Contains(outlineParam)) {
                outlineParams.Add(outlineParam);
            }
        }

        public static void RemoveOutlineTrigger(OutlineParam outlineParam) {
            if (outlineParams.Contains(outlineParam)) {
                outlineParams.Remove(outlineParam);
            }
        }
        
        public static void AddOccludeeTrigger(OccludeeParam occludeeParam) {
            if (!occludeeParams.Contains(occludeeParam)) {
                occludeeParams.Add(occludeeParam);
            }
        }

        public static void RemoveOccludeeTrigger(OccludeeParam occludeeParam) {
            if (occludeeParams.Contains(occludeeParam)) {
                occludeeParams.Remove(occludeeParam);
            }
        }
        
        // public static int Count() {
        //     return effectsTriggers.Count;
        // }
        //
        // public static bool Exists(EffectsTrigger effectsTrigger) {
        //     return effectsTriggers.Contains(effectsTrigger);
        // }
    }
}