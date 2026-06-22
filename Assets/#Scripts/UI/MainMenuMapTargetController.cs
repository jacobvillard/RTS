using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _Scripts.UI {
    /// <summary>
    /// Moves one physical menu map object to configured poses when menu buttons are pressed.
    /// </summary>
    [DisallowMultipleComponent]
    public class MainMenuMapTargetController : MonoBehaviour {

        #region Types

        public enum MenuLocation {
            Play,
            Options,
            Sandbox,
            Editor
        }

        [System.Serializable]
        private class MapTarget {
            public MenuLocation location;
            public Button button;
            public Vector3 position;
            public Vector3 rotation;
        }

        #endregion
        #region Variables

        [Header("Map")]
        [SerializeField] private Transform physicalMap;
        [SerializeField] private bool useLocalSpace = true;
        [SerializeField] private Vector3 defaultPosition;
        [SerializeField] private Vector3 defaultRotation;
        [SerializeField] private bool captureDefaultPoseOnAwake = true;

        [Header("Targets")]
        [SerializeField] private MapTarget playTarget = new() { location = MenuLocation.Play };
        [SerializeField] private MapTarget optionsTarget = new() { location = MenuLocation.Options };
        [SerializeField] private MapTarget sandboxTarget = new() { location = MenuLocation.Sandbox };
        [SerializeField] private MapTarget editorTarget = new() { location = MenuLocation.Editor };

        [Header("Options Reveal")]
        [SerializeField] private GameObject optionsRoot;
        [SerializeField] private CanvasGroup optionsCanvasGroup;
        [SerializeField, Min(0f)] private float optionsFadeDelay;
        [SerializeField, Min(0.01f)] private float optionsFadeTime = 0.25f;

        [Header("Level Select Reveal")]
        [SerializeField] private GameObject levelSelectRoot;
        [SerializeField] private CanvasGroup levelSelectCanvasGroup;
        [SerializeField, Min(0f)] private float levelSelectFadeDelay;
        [SerializeField, Min(0.01f)] private float levelSelectFadeTime = 0.25f;

        [Header("Transition")]
        [SerializeField, Min(0f)] private float transitionDelay = 0.12f;
        [SerializeField, Min(0.01f)] private float transitionTime = 0.75f;
        [SerializeField] private bool useUnscaledTime = true;

        private Coroutine _transitionRoutine;
        private bool _defaultPoseCaptured;

        #endregion
        #region Unity Methods

        private void Awake() {
            ResolveReferences();
            CaptureDefaultPoseIfNeeded();
            RegisterButtonPressTriggers();
        }

        private void OnEnable() {
            ResolveReferences();
            CaptureDefaultPoseIfNeeded();
            RegisterButtonPressTriggers();
        }

        private void OnValidate() {
            ResolveReferences();
            playTarget.location = MenuLocation.Play;
            optionsTarget.location = MenuLocation.Options;
            sandboxTarget.location = MenuLocation.Sandbox;
            editorTarget.location = MenuLocation.Editor;
        }

        #endregion
        #region Public Methods

        public void MoveToPlay() {
            MoveToTarget(playTarget);
        }

        public void MoveToOptions() {
            MoveToTarget(optionsTarget);
        }

        public void MoveToSandbox() {
            MoveToTarget(sandboxTarget);
        }

        public void MoveToEditor() {
            MoveToTarget(editorTarget);
        }

        public void ResetToDefault() {
            MoveToPose(defaultPosition, defaultRotation);
        }

        public void SnapToDefault() {
            if (physicalMap == null) return;

            if (_transitionRoutine != null) {
                StopCoroutine(_transitionRoutine);
                _transitionRoutine = null;
            }

            SetMapPose(defaultPosition, Quaternion.Euler(defaultRotation));
        }

        public IEnumerator ResetToDefaultAndWait(bool snap) {
            yield return ResetToDefaultAndWait(snap, 1f);
        }

        public IEnumerator ResetToDefaultAndWait(bool snap, float transitionTimeMultiplier) {
            if (snap) {
                SnapToDefault();
                yield break;
            }

            MoveToPose(defaultPosition, defaultRotation, MenuLocation.Play, transitionTimeMultiplier, false);
            while (_transitionRoutine != null) {
                yield return null;
            }
        }

        #endregion
        #region Button Setup

        private void RegisterButtonPressTriggers() {
            RegisterButtonPressTrigger(playTarget);
            RegisterButtonPressTrigger(optionsTarget);
            RegisterButtonPressTrigger(sandboxTarget);
            RegisterButtonPressTrigger(editorTarget);
        }

        private void RegisterButtonPressTrigger(MapTarget target) {
            if (target?.button == null) return;

            var trigger = target.button.GetComponent<MainMenuMapButtonTrigger>();
            if (trigger == null) {
                trigger = target.button.gameObject.AddComponent<MainMenuMapButtonTrigger>();
            }

            trigger.SetTarget(this, target.location);
        }

        #endregion
        #region Transition

        private void MoveToTarget(MapTarget target) {
            if (physicalMap == null || target == null) return;

            MoveToPose(target.position, target.rotation, target.location, 1f, true);
        }

        private void MoveToPose(Vector3 position, Vector3 rotation, MenuLocation location = MenuLocation.Play) {
            MoveToPose(position, rotation, location, 1f, false);
        }

        private void MoveToPose(
            Vector3 position,
            Vector3 rotation,
            MenuLocation location,
            float transitionTimeMultiplier,
            bool revealScreen) {
            if (physicalMap == null) return;

            if (_transitionRoutine != null) {
                StopCoroutine(_transitionRoutine);
            }

            if (revealScreen) {
                PrepareScreenFadeIn(location);
            }

            _transitionRoutine = StartCoroutine(TransitionMap(position, rotation, location, transitionTimeMultiplier, revealScreen));
        }

        private IEnumerator TransitionMap(
            Vector3 targetPosition,
            Vector3 targetRotation,
            MenuLocation location,
            float transitionTimeMultiplier,
            bool revealScreen) {
            if (transitionDelay > 0f) {
                var delayElapsed = 0f;
                while (delayElapsed < transitionDelay) {
                    delayElapsed += GetDeltaTime();
                    yield return null;
                }
            }

            var startPosition = GetCurrentPosition();
            var startRotation = GetCurrentRotation();
            var endRotation = Quaternion.Euler(targetRotation);
            var duration = Mathf.Max(0.01f, transitionTime * Mathf.Max(0.01f, transitionTimeMultiplier));
            var elapsed = 0f;

            while (elapsed < duration) {
                elapsed += GetDeltaTime();
                var progress = EaseInOut(Mathf.Clamp01(elapsed / duration));

                SetMapPose(
                    Vector3.Lerp(startPosition, targetPosition, progress),
                    Quaternion.Slerp(startRotation, endRotation, progress));

                yield return null;
            }

            SetMapPose(targetPosition, endRotation);
            if (revealScreen) {
                yield return FadeScreenIn(location);
            }

            _transitionRoutine = null;
        }

        private void ResolveReferences() {
            if (optionsCanvasGroup == null && optionsRoot != null) {
                optionsCanvasGroup = optionsRoot.GetComponent<CanvasGroup>();
            }

            if (levelSelectCanvasGroup == null && levelSelectRoot != null) {
                levelSelectCanvasGroup = levelSelectRoot.GetComponent<CanvasGroup>();
            }
        }

        private void PrepareScreenFadeIn(MenuLocation location) {
            var screen = GetScreenReveal(location);
            if (screen.Root != null) {
                screen.Root.SetActive(true);
            }

            ResolveReferences();
            screen = GetScreenReveal(location);

            if (screen.CanvasGroup == null) return;

            screen.CanvasGroup.alpha = 0f;
            screen.CanvasGroup.interactable = false;
            screen.CanvasGroup.blocksRaycasts = false;
        }

        private IEnumerator FadeScreenIn(MenuLocation location) {
            ResolveReferences();
            var screen = GetScreenReveal(location);
            if (screen.CanvasGroup == null) yield break;

            if (screen.FadeDelay > 0f) {
                var delayElapsed = 0f;
                while (delayElapsed < screen.FadeDelay) {
                    delayElapsed += GetDeltaTime();
                    yield return null;
                }
            }

            var elapsed = 0f;
            while (elapsed < screen.FadeTime) {
                elapsed += GetDeltaTime();
                var progress = EaseInOut(Mathf.Clamp01(elapsed / screen.FadeTime));
                screen.CanvasGroup.alpha = Mathf.Lerp(0f, 1f, progress);
                yield return null;
            }

            screen.CanvasGroup.alpha = 1f;
            screen.CanvasGroup.interactable = true;
            screen.CanvasGroup.blocksRaycasts = true;
        }

        private ScreenReveal GetScreenReveal(MenuLocation location) {
            return location switch {
                MenuLocation.Play => new ScreenReveal(levelSelectRoot, levelSelectCanvasGroup, levelSelectFadeDelay, levelSelectFadeTime),
                MenuLocation.Options => new ScreenReveal(optionsRoot, optionsCanvasGroup, optionsFadeDelay, optionsFadeTime),
                _ => default
            };
        }

        private void CaptureDefaultPoseIfNeeded() {
            if (!captureDefaultPoseOnAwake || _defaultPoseCaptured || physicalMap == null) return;

            defaultPosition = GetCurrentPosition();
            defaultRotation = GetCurrentRotation().eulerAngles;
            _defaultPoseCaptured = true;
        }

        private Vector3 GetCurrentPosition() {
            return useLocalSpace ? physicalMap.localPosition : physicalMap.position;
        }

        private Quaternion GetCurrentRotation() {
            return useLocalSpace ? physicalMap.localRotation : physicalMap.rotation;
        }

        private void SetMapPose(Vector3 position, Quaternion rotation) {
            if (useLocalSpace) {
                physicalMap.localPosition = position;
                physicalMap.localRotation = rotation;
                return;
            }

            physicalMap.position = position;
            physicalMap.rotation = rotation;
        }

        private float GetDeltaTime() {
            return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        }

        private static float EaseInOut(float value) {
            return value * value * (3f - 2f * value);
        }

        private readonly struct ScreenReveal {
            public readonly GameObject Root;
            public readonly CanvasGroup CanvasGroup;
            public readonly float FadeDelay;
            public readonly float FadeTime;

            public ScreenReveal(GameObject root, CanvasGroup canvasGroup, float fadeDelay, float fadeTime) {
                Root = root;
                CanvasGroup = canvasGroup;
                FadeDelay = fadeDelay;
                FadeTime = fadeTime;
            }
        }

        #endregion
    }

    /// <summary>
    /// Child button event bridge used by MainMenuMapTargetController.
    /// </summary>
    [DisallowMultipleComponent]
    public class MainMenuMapButtonTrigger : MonoBehaviour, IPointerDownHandler, ISubmitHandler {

        private MainMenuMapTargetController _controller;
        private MainMenuMapTargetController.MenuLocation _location;

        public void SetTarget(MainMenuMapTargetController controller, MainMenuMapTargetController.MenuLocation location) {
            _controller = controller;
            _location = location;
        }

        public void OnPointerDown(PointerEventData eventData) {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            MoveMap();
        }

        public void OnSubmit(BaseEventData eventData) {
            MoveMap();
        }

        private void MoveMap() {
            if (_controller == null) return;

            switch (_location) {
                case MainMenuMapTargetController.MenuLocation.Play:
                    _controller.MoveToPlay();
                    break;
                case MainMenuMapTargetController.MenuLocation.Options:
                    _controller.MoveToOptions();
                    break;
                case MainMenuMapTargetController.MenuLocation.Sandbox:
                    _controller.MoveToSandbox();
                    break;
                case MainMenuMapTargetController.MenuLocation.Editor:
                    _controller.MoveToEditor();
                    break;
            }
        }
    }
}
