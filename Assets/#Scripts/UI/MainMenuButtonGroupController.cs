using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _Scripts.UI {
    /// <summary>
    /// Fades a group of menu buttons when any child button is pressed.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public class MainMenuButtonGroupController : MonoBehaviour {

        #region Variables

        [Header("Fade")]
        [SerializeField, Min(0f)] private float fadeDelay = 0.12f;
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.35f;
        [SerializeField] private bool disableInteractionDuringFade = true;
        [SerializeField] private bool useUnscaledTime = true;
        [SerializeField] private bool resetFadeOnEnable = true;

        [Header("Intro")]
        [SerializeField] private bool playChildIntrosAfterSceneLoad = true;
        [SerializeField, Min(0f)] private float introStartDelay = 0.05f;
        [SerializeField, Min(0f)] private float introDelayStep = 0.08f;

        private CanvasGroup _canvasGroup;
        private Coroutine _fadeRoutine;
        private Coroutine _introRoutine;
        private bool _isFadingOut;
        private bool _skipNextEnableIntro;
        private float _lastIntroRequestTime = -999f;
        public bool ControlsChildIntros => playChildIntrosAfterSceneLoad;

        #endregion
        #region Unity Methods

        private void Awake() {
            ResolveReferences();
            RegisterChildButtons();
        }

        private void OnEnable() {
            ResolveReferences();

            if (resetFadeOnEnable) {
                SetGroupVisible();
            }

            if (playChildIntrosAfterSceneLoad && !_skipNextEnableIntro) {
                PlayChildButtonIntrosAfterLayout();
            }

            _skipNextEnableIntro = false;
        }

        private void OnTransformChildrenChanged() {
            if (!isActiveAndEnabled) return;

            RegisterChildButtons();
        }

        private void OnValidate() {
            ResolveReferences();
        }

        #endregion
        #region Public Methods

        /// <summary>
        /// Starts fading the whole button group out.
        /// </summary>
        public void FadeOutButtons() {
            ResolveReferences();
            if (_canvasGroup == null || _isFadingOut) return;

            if (_fadeRoutine != null) {
                StopCoroutine(_fadeRoutine);
            }

            _fadeRoutine = StartCoroutine(FadeGroupOut());
        }

        /// <summary>
        /// Restores the button group to fully visible and interactive.
        /// </summary>
        public void ResetButtons() {
            ResetButtons(true);
        }

        /// <summary>
        /// Restores the button group, optionally replaying the child intro animation.
        /// </summary>
        public void ResetButtons(bool replayIntros) {
            if (_fadeRoutine != null) {
                StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
            }

            if (_introRoutine != null) {
                StopCoroutine(_introRoutine);
                _introRoutine = null;
            }

            SetGroupVisible();

            if (replayIntros && playChildIntrosAfterSceneLoad) {
                PlayChildButtonIntrosAfterLayout();
            }
        }

        /// <summary>
        /// Prevents the next OnEnable from replaying child button intros.
        /// </summary>
        public void SkipNextEnableIntro() {
            _skipNextEnableIntro = true;
        }

        /// <summary>
        /// Replays child button intros from a stable layout-ready moment.
        /// </summary>
        public void PlayChildButtonIntrosAfterLayout() {
            var time = useUnscaledTime ? Time.unscaledTime : Time.time;
            if (time - _lastIntroRequestTime < 0.05f) return;

            _lastIntroRequestTime = time;

            if (_introRoutine != null) {
                StopCoroutine(_introRoutine);
            }

            _introRoutine = StartCoroutine(PlayChildButtonIntros());
        }

        #endregion
        #region Intro

        private IEnumerator PlayChildButtonIntros() {
            yield return null;
            Canvas.ForceUpdateCanvases();

            if (introStartDelay > 0f) {
                var elapsed = 0f;
                while (elapsed < introStartDelay) {
                    elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                    yield return null;
                }
            }

            var animators = GetComponentsInChildren<MainMenuButtonAnimator>(true);
            for (var i = 0; i < animators.Length; i++) {
                if (animators[i] == null) continue;
                animators[i].PlayIntro(i * introDelayStep);
            }

            _introRoutine = null;
        }

        #endregion
        #region Fade

        private IEnumerator FadeGroupOut() {
            _isFadingOut = true;

            if (fadeDelay > 0f) {
                var delayElapsed = 0f;
                while (delayElapsed < fadeDelay) {
                    delayElapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                    yield return null;
                }
            }

            var startAlpha = _canvasGroup.alpha;
            var elapsed = 0f;

            while (elapsed < fadeDuration) {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / fadeDuration);
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, EaseInOut(progress));
                yield return null;
            }

            _canvasGroup.alpha = 0f;

            if (disableInteractionDuringFade) {
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }

            _fadeRoutine = null;
        }

        private void SetGroupVisible() {
            if (_canvasGroup == null) return;

            _isFadingOut = false;
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }

        #endregion
        #region Setup

        private void ResolveReferences() {
            if (_canvasGroup == null) {
                _canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        private void RegisterChildButtons() {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var button in buttons) {
                if (button == null) continue;

                var trigger = button.GetComponent<MainMenuButtonFadeTrigger>();
                if (trigger == null) {
                    trigger = button.gameObject.AddComponent<MainMenuButtonFadeTrigger>();
                }

                trigger.SetController(this);
            }
        }

        private static float EaseInOut(float value) {
            return value * value * (3f - 2f * value);
        }

        #endregion
    }

    /// <summary>
    /// Child button event bridge used by MainMenuButtonGroupController.
    /// </summary>
    [DisallowMultipleComponent]
    public class MainMenuButtonFadeTrigger : MonoBehaviour, IPointerDownHandler, ISubmitHandler {

        private MainMenuButtonGroupController _controller;
        private Coroutine _fadeRoutine;

        public void SetController(MainMenuButtonGroupController controller) {
            _controller = controller;
        }

        public void OnPointerDown(PointerEventData eventData) {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            FadeNow();
        }

        public void OnSubmit(BaseEventData eventData) {
            FadeNow();
        }

        private void FadeNow() {
            if (_fadeRoutine != null) {
                StopCoroutine(_fadeRoutine);
            }

            _fadeRoutine = StartCoroutine(FadeThisFrame());
        }

        private IEnumerator FadeThisFrame() {
            _controller?.FadeOutButtons();
            yield return null;
            _fadeRoutine = null;
        }
    }
}
