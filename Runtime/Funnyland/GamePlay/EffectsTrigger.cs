using System;
using System.Collections.Generic;
using UnityEngine;

namespace FRP.Rendering {
    [DisallowMultipleComponent]
    public class EffectsTrigger : MonoBehaviour {
        public enum OutlineType : byte {
            Teammate = 0,
            Danger = 1,
            Flag = 2,
        }

        public enum OccludeeType : byte {
            Teammate = 0,
            Danger = 1,
            Flag = 2,
        }

        // 受击
        [Range(0.0f, 1.0f)] public float attackedColorIntensity = 0.0f;
        public Color attackedColor = new(1f, 0.12f, 0.15f);
        [SerializeField] private OutlineParam[] outlines;
        [SerializeField] private OccludeeParam[] occludees;

        private Renderer[] renderers;

        public bool IsActive { get; private set; }

        private readonly List<OutlineState> outlineStates = new((byte)OutlineType.Flag);
        private readonly List<OccludeeState> occludeeStates = new((byte)OccludeeType.Flag);

        
        // Test
        // 仅仅用来调试效果调用, 在PCRetime时可以用Update, 但是当打包之后需要预热管线或者将Update函数改为Render后的函数调用
        // 原因是我们在new EffectsPass时进行EffectsManager.Init()初始化状态
        // private void Update() {
        //     OutlineState outlineState;
        //     outlineState.type = OutlineType.Teammate;
        //     outlineState.isActive = true;
        //     outlineState.priority = 0;
        //     SetOutlineState(outlineState);
        //
        //     OccludeeState occludeeState;
        //     occludeeState.priority = 0;
        //     occludeeState.type = OccludeeType.Teammate;
        //     occludeeState.isActive = true;
        //     SetOccludeeState(occludeeState);
        // }

        public void SetOutlineState(OutlineState input) {
            var exist = false;
            var dirty = false;

            for (var i = 0; i < outlineStates.Count; i++) {
                var outlineState = outlineStates[i];

                if (outlineState.type == input.type) {
                    if (!outlineState.Equals(input)) {
                        outlineStates[i] = input;
                        dirty = true;
                    }

                    exist = true;
                    break;
                }
            }

            if (!exist) {
                outlineStates.Add(input);
                dirty = true;
            }

            if (dirty) {
                outlineStates.Sort((x, y) => {
                    if (x.isActive && !y.isActive) {
                        return -1;
                    }

                    if (!x.isActive && y.isActive) {
                        return 1;
                    }

                    return -x.priority.CompareTo(y.priority);
                });
            }

            if (outlineStates[0].isActive || occludeeStates.Count > 0 && occludeeStates[0].isActive) {
                Begin();
            } else {
                Stop();
            }
        }

        public (bool, float, Color) GetOutlineParam() {
            if (outlineStates.Count > 0) {
                var state = outlineStates[0];

                if (state.isActive) {
                    foreach (var outline in outlines) {
                        if (outline.type == state.type) {
                            return (true, outline.with, outline.color);
                        }
                    }
                }
            }

            return (false, 0f, Color.black);
        }

        public void SetOccludeeState(OccludeeState input) {
            var exist = false;
            var dirty = false;

            for (var i = 0; i < occludeeStates.Count; i++) {
                var occludeeState = occludeeStates[i];

                if (occludeeState.type == input.type) {
                    if (!occludeeState.Equals(input)) {
                        occludeeStates[i] = input;
                        dirty = true;
                    }

                    exist = true;
                    break;
                }
            }

            if (!exist) {
                occludeeStates.Add(input);
                dirty = true;
            }

            if (dirty) {
                occludeeStates.Sort((x, y) => {
                    if (x.isActive && !y.isActive) {
                        return -1;
                    }

                    if (!x.isActive && y.isActive) {
                        return 1;
                    }

                    return -x.priority.CompareTo(y.priority);
                });
            }

            if (occludeeStates[0].isActive || outlineStates.Count > 0 && outlineStates[0].isActive) {
                Begin();
            } else {
                Stop();
            }
        }

        public (bool, float, Color) GetOccludeeParam() {
            if (occludeeStates.Count > 0) {
                var state = occludeeStates[0];

                if (state.isActive) {
                    foreach (var occludee in occludees) {
                        if (occludee.type == state.type) {
                            return (true, occludee.intensity, occludee.color);
                        }
                    }
                }
            }

            return (false, 0f, Color.black);
        }

        public void Begin() {
            if (IsActive) {
                return;
            }

            IsActive = true;
            renderers = gameObject.GetComponentsInChildren<Renderer>();

            if (EffectsManager.state) {
                EffectsManager.AddTrigger(this);
            }
        }

        public void Stop() {
            if (!IsActive) {
                return;
            }

            IsActive = false;

            if (EffectsManager.state && EffectsManager.Exists(this)) {
                EffectsManager.RemoveTrigger(this);
            }
        }

        public Renderer[] GetRenderers() {
            return renderers;
        }

        private void OnDestroy() {
            Stop();
        }

        public struct OutlineState : IEquatable<OutlineState> {
            public OutlineType type;
            public bool isActive;
            public byte priority;

            public bool Equals(OutlineState other) {
                return type == other.type && isActive == other.isActive && priority == other.priority;
            }
        }

        public struct OccludeeState : IEquatable<OccludeeState> {
            public OccludeeType type;
            public bool isActive;
            public byte priority;

            public bool Equals(OccludeeState other) {
                return type == other.type && isActive == other.isActive && priority == other.priority;
            }
        }

        [System.Serializable]
        public struct OutlineParam {
            public OutlineType type;
            [Range(0f, 0.1f)] public float with;
            [ColorUsageAttribute(true, true)] public Color color;
        }

        [System.Serializable]
        public struct OccludeeParam {
            public OccludeeType type;
            [Range(0f, 1f)] public float intensity;
            [ColorUsageAttribute(true, true)] public Color color;
        }
    }
}