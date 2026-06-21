using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI {
    /// <summary>
    /// Fades the options screen out, resets the menu map, and restores the start menu.
    /// </summary>
    [DisallowMultipleComponent]
    public class MainMenuBackTransitionController : MonoBehaviour {

        #region Variables

        [Header("Buttons")]
        [SerializeField] private Button backButton;

        [Header("Screens")]
        [SerializeField] private GameObject optionsRoot;
        [SerializeField] private CanvasGroup optionsCanvasGroup;
        [SerializeField] private GameObject mainMenuRoot;
        [SerializeField] private MainMenuButtonGroupController mainMenuButtonGroup;

        [Header("World Reset")]
        [SerializeField] private MainMenuMapTargetController mapTargetController;
        [SerializeField] private bool snapMapToDefault;
        [SerializeField, Min(0.01f)] private float mapResetTimeMultiplier = 0.25f;

        [Header("Fade")]
        [SerializeField, Min(0f)] private float fadeDelay = 0.05f;
        [SerializeField, Min(0.01f)] private float fadeTime = 0.45f;
        [SerializeField] private bool useUnscaledTime = true;

        private Coroutine _transitionRoutine;
        private float _optionsStartingAlpha = 1f;

        #endregion
        #region Unity Methods

        private void Awake() {
            ResolveReferences();
            AddButtonListener();
        }

        private void OnEnable() {
            _transitionRoutine = null;
            ResolveReferences();
            AddButtonListener();
            SetBackInteractable(true);
        }

        private void OnDisable() {
            _transitionRoutine = null;
            SetBackInteractable(true);
        }

        private void OnDestroy() {
            RemoveButtonListener();
        }

        private void OnValidate() {
            ResolveReferences();
        }

        #endregion
        #region Public Methods

        /// <summary>
        /// Starts the return-to-start transition.
        /// </summary>
        public void PlayBackTransition() {
            if (_transitionRoutine != null) return;

            _transitionRoutine = StartCoroutine(BackTransition());
        }

        #endregion
        #region Transition

        private IEnumerator BackTransition() {
            SetBackInteractable(false);
            SetOptionsInteractable(false);

            if (fadeDelay > 0f) {
                yield return Wait(fadeDelay);
            }

            var fadeDone = false;
            var restoreDone = false;

            StartCoroutine(RunAndMarkDone(FadeOptionsOut(), () => fadeDone = true));
            StartCoroutine(RunAndMarkDone(RestoreStartMenu(), () => restoreDone = true));

            while (!fadeDone || !restoreDone) {
                yield return null;
            }

            ResetOptionsVisuals();
            SetBackInteractable(true);
            _transitionRoutine = null;
            HideOptionsRoot();
        }

        private IEnumerator FadeOptionsOut() {
            if (optionsCanvasGroup == null) yield break;

            _optionsStartingAlpha = optionsCanvasGroup.alpha;
            var elapsed = 0f;

            while (elapsed < fadeTime) {
                elapsed += GetDeltaTime();
                var progress = EaseInOut(Mathf.Clamp01(elapsed / fadeTime));
                optionsCanvasGroup.alpha = Mathf.Lerp(_optionsStartingAlpha, 0f, progress);
                yield return null;
            }

            optionsCanvasGroup.alpha = 0f;
        }

        private IEnumerator RestoreStartMenu() {
            if (mapTargetController != null) {
                yield return mapTargetController.ResetToDefaultAndWait(snapMapToDefault, mapResetTimeMultiplier);
            }

            mainMenuButtonGroup?.SkipNextEnableIntro();

            if (mainMenuRoot != null) {
                mainMenuRoot.SetActive(true);
            }

            mainMenuButtonGroup?.ResetButtons(false);
        }

        private void HideOptionsRoot() {
            if (optionsRoot != null) {
                optionsRoot.SetActive(false);
            }
        }

        private void ResetOptionsVisuals() {
            if (optionsCanvasGroup == null) return;

            optionsCanvasGroup.alpha = _optionsStartingAlpha;
            SetOptionsInteractable(true);
        }

        #endregion
        #region Setup

        private void ResolveReferences() {
            if (optionsCanvasGroup == null && optionsRoot != null) {
                optionsCanvasGroup = optionsRoot.GetComponent<CanvasGroup>();
            }
        }

        private void AddButtonListener() {
            if (backButton == null) return;

            backButton.onClick.RemoveListener(PlayBackTransition);
            backButton.onClick.AddListener(PlayBackTransition);
        }

        private void RemoveButtonListener() {
            if (backButton != null) {
                backButton.onClick.RemoveListener(PlayBackTransition);
            }
        }

        private void SetBackInteractable(bool interactable) {
            if (backButton != null) {
                backButton.interactable = interactable;
            }
        }

        private void SetOptionsInteractable(bool interactable) {
            if (optionsCanvasGroup == null) return;

            optionsCanvasGroup.interactable = interactable;
            optionsCanvasGroup.blocksRaycasts = interactable;
        }

        #endregion
        #region Helpers

        private IEnumerator Wait(float duration) {
            var elapsed = 0f;
            while (elapsed < duration) {
                elapsed += GetDeltaTime();
                yield return null;
            }
        }

        private static IEnumerator RunAndMarkDone(IEnumerator routine, System.Action markDone) {
            yield return routine;
            markDone?.Invoke();
        }

        private float GetDeltaTime() {
            return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        }

        private static float EaseInOut(float value) {
            return value * value * (3f - 2f * value);
        }

        #endregion
    }
}
