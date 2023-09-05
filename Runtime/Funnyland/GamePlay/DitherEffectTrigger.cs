using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SoFunny.Rendering.Funnyland {
    public class DitherEffectTrigger : MonoBehaviour {
        [Range(0, 10)] [SerializeField] private float min = 0.5f;
        [Range(0, 15)] [SerializeField] private float max = 5f;
        [SerializeField] private Vector3 center = new(0f, 1.5f, 0f);

        private readonly List<Renderer> ditherRenders = new();

        private State state;

        public bool IsActive {
            get => state == State.Enable;
            set {
                switch (state) {
                    case State.None:
                        if (value) {
                            Begin();
                            state = State.Enable;
                        } else {
                            Stop();
                            state = State.Disable;
                        }

                        break;

                    case State.Enable:

                        if (!value) {
                            Stop();
                            state = State.Disable;
                        }

                        break;

                    case State.Disable:
                        if (value) {
                            Begin();
                            state = State.Enable;
                        }

                        break;
                }
            }
        }

        private void OnEnable() {
            if (state == State.Enable) {
                Begin();
            }
        }

        private void OnDisable() {
            if (state == State.Enable) {
                Stop();
            }
        }

        private void Begin() {
            this.GetComponentsInChildren(true, ditherRenders);

            foreach (var ditherRenderer in ditherRenders) {
                if (ditherRenderer == null) {
                    continue;
                }

                foreach (var material in ditherRenderer.materials) {
                    if (material == null) {
                        continue;
                    }

                    material.EnableKeyword(new LocalKeyword(material.shader, "_DITHER_FADING_ON"));
                    material.SetFloat("_MinDitherDistance", min);
                    material.SetFloat("_MaxDitherDistance", max);
                }
            }
        }

        private void Stop() {
            foreach (var ditherRenderer in ditherRenders) {
                if (ditherRenderer == null) {
                    continue;
                }

                foreach (var material in ditherRenderer.materials) {
                    if (material == null) {
                        continue;
                    }

                    if (material.shader.name == "Hidden/InternalErrorShader") {
                        continue;
                    }

                    material.DisableKeyword(new LocalKeyword(material.shader, "_DITHER_FADING_ON"));
                }
            }
        }

        private void LateUpdate() {
            if (state == State.Enable) {
                var position = this.transform.position + center;

                foreach (var ditherRenderer in ditherRenders) {
                    if (ditherRenderer == null) {
                        continue;
                    }

                    foreach (var material in ditherRenderer.materials) {
                        if (material == null) {
                            continue;
                        }

                        material.SetVector("_ObjectPosition", position);
                    }
                }
            }
        }

        public enum State {
            None,
            Disable,
            Enable,
        }
    }
}