using System.Collections;
using UnityEngine;

namespace _Scripts.Camera {
    /// <summary>
    /// Plays an optional start-of-scene camera move before enabling player camera control.
    /// </summary>
    public class CameraStartAnimation : MonoBehaviour {

        #region Variables

        [Header("Animation")]
        [SerializeField] private bool playAnimationOnSceneStart = true;
        [SerializeField] private float duration = 2f;
        [SerializeField] private Vector3 startPosition;
        [SerializeField] private Vector3 endPosition;
        [SerializeField] private AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private CameraDrag _cameraDrag;
        private Coroutine _animationRoutine;

        #endregion
        #region Unity Methods

        private void Awake() {
            _cameraDrag = GetComponent<CameraDrag>();

            if (playAnimationOnSceneStart) {
                SetCameraControlEnabled(false);
            }
        }

        private void Start() {
            if (!playAnimationOnSceneStart) {
                transform.position = endPosition;
                SetCameraControlEnabled(true);
                return;
            }

            transform.position = startPosition;
            _animationRoutine = StartCoroutine(AnimateToEndPosition());
        }

        private void OnDisable() {
            if (_animationRoutine != null) {
                StopCoroutine(_animationRoutine);
                _animationRoutine = null;
            }

            SetCameraControlEnabled(true);
        }

        #endregion
        #region Public Methods

        public void SetStartPositionToCurrentTransform() {
            startPosition = transform.position;
        }

        public void SetEndPositionToCurrentTransform() {
            endPosition = transform.position;
        }

        #endregion
        #region Animation

        private IEnumerator AnimateToEndPosition() {
            var elapsed = 0f;
            var animationDuration = Mathf.Max(0.01f, duration);

            while (elapsed < animationDuration) {
                elapsed += Time.unscaledDeltaTime;
                var normalizedTime = Mathf.Clamp01(elapsed / animationDuration);
                var easedTime = easing != null ? easing.Evaluate(normalizedTime) : normalizedTime;
                transform.position = Vector3.LerpUnclamped(startPosition, endPosition, easedTime);

                yield return null;
            }

            transform.position = endPosition;
            _animationRoutine = null;
            SetCameraControlEnabled(true);
        }

        private void SetCameraControlEnabled(bool enabled) {
            if (_cameraDrag == null) return;

            _cameraDrag.enabled = enabled;
        }

        #endregion
    }
}
