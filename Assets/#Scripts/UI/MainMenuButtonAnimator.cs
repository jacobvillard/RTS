using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _Scripts.UI {
    /// <summary>
    /// Gives menu buttons a staggered slide-in, idle motion, hover growth, and selected feedback.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class MainMenuButtonAnimator : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        ISelectHandler,
        IDeselectHandler,
        ISubmitHandler {

        #region Variables

        [Header("Targets")]
        [SerializeField] private RectTransform target; // Button transform to animate.
        [SerializeField] private Graphic tintGraphic;  // Optional image/text tint target.
        [SerializeField] private CanvasGroup canvasGroup; // Optional fade target.

        [Header("Intro")]
        [SerializeField] private bool playIntroOnEnable = true;
        [SerializeField] private bool delayBySiblingIndex = true;
        [SerializeField, Min(0f)] private float baseDelay = 0.05f;
        [SerializeField, Min(0f)] private float delayStep = 0.08f;
        [SerializeField, Min(0.01f)] private float introDuration = 0.45f;
        [SerializeField] private Vector2 slideFromOffset = new(-220f, 0f);
        [SerializeField, Range(0f, 1f)] private float introStartScale = 0.9f;
        [SerializeField] private bool fadeDuringIntro;

        [Header("Hover")]
        [SerializeField, Range(1f, 1.5f)] private float hoverScale = 1.08f;
        [SerializeField, Range(0.1f, 40f)] private float hoverSnappiness = 16f;
        [SerializeField] private Vector2 hoverNudge = new(16f, 0f);

        [Header("Selected")]
        [SerializeField, Range(1f, 1.6f)] private float selectedScale = 1.14f;
        [SerializeField] private Vector2 selectedNudge = new(24f, 0f);
        [SerializeField, Range(0f, 12f)] private float selectedWobbleDegrees = 2.5f;
        [SerializeField] private Color selectedTint = new(1f, 0.86f, 0.38f, 1f);

        [Header("Idle")]
        [SerializeField] private bool idleMotion = true;
        [SerializeField, Range(0f, 12f)] private float idleFloatAmount = 3f;
        [SerializeField, Range(0f, 8f)] private float idleTiltDegrees = 0.8f;
        [SerializeField, Range(0f, 5f)] private float idleSpeed = 0.8f;

        [Header("Press")]
        [SerializeField, Range(0.75f, 1f)] private float pressedScale = 0.96f;
        [SerializeField, Min(0.01f)] private float clickPulseDuration = 0.18f;

        [Header("Timing")]
        [SerializeField] private bool useUnscaledTime = true;

        private Vector2 _baseAnchoredPosition;
        private Vector3 _baseScale;
        private Quaternion _baseRotation;
        private Color _baseTint;
        private float _introDelay;
        private float _introTimer;
        private float _timeOffset;
        private float _clickPulseTimer;
        private Coroutine _introRoutine;
        private bool _introStarted;
        private bool _introComplete;
        private bool _hovered;
        private bool _selected;
        private bool _pressed;

        #endregion
        #region Unity Methods

        private void Awake() {
            ResolveReferences();
            CacheStartingValues();
        }

        private void OnEnable() {
            ResolveReferences();
            CacheStartingValues();
            var parentGroup = GetComponentInParent<MainMenuButtonGroupController>();
            if (parentGroup != null && parentGroup.ControlsChildIntros) return;

            PrepareIntro();
        }

        private void LateUpdate() {
            if (target == null) return;

            var deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            var time = useUnscaledTime ? Time.unscaledTime : Time.time;

            UpdateIntro(deltaTime);
            UpdateButtonPose(deltaTime, time);
        }

        private void OnDisable() {
            if (_introRoutine != null) {
                StopCoroutine(_introRoutine);
                _introRoutine = null;
            }

            ResetVisuals();
        }

        private void OnValidate() {
            ResolveReferences();
        }

        #endregion
        #region Events

        public void OnPointerEnter(PointerEventData eventData) {
            _hovered = true;
        }

        public void OnPointerExit(PointerEventData eventData) {
            _hovered = false;
            _pressed = false;
        }

        public void OnPointerDown(PointerEventData eventData) {
            _pressed = true;
        }

        public void OnPointerUp(PointerEventData eventData) {
            if (_pressed) {
                TriggerClickPulse();
            }

            _pressed = false;
        }

        public void OnSelect(BaseEventData eventData) {
            _selected = true;
        }

        public void OnDeselect(BaseEventData eventData) {
            _selected = false;
            _pressed = false;
        }

        public void OnSubmit(BaseEventData eventData) {
            TriggerClickPulse();
        }

        #endregion
        #region Public Methods

        /// <summary>
        /// Plays the slide-in animation with a caller-controlled delay.
        /// </summary>
        public void PlayIntro(float delay) {
            ResolveReferences();
            CacheStartingValues();
            PrepareIntro(Mathf.Max(0f, delay), true);
        }

        #endregion
        #region Animation

        private void PrepareIntro() {
            _introDelay = delayBySiblingIndex ? baseDelay + transform.GetSiblingIndex() * delayStep : baseDelay;
            PrepareIntro(_introDelay, playIntroOnEnable);
        }

        private void PrepareIntro(float introDelay, bool shouldPlayIntro) {
            _introDelay = introDelay;
            _introTimer = 0f;
            _introComplete = !shouldPlayIntro;
            _introStarted = !shouldPlayIntro;
            _clickPulseTimer = 0f;
            _hovered = false;
            _selected = false;
            _pressed = false;

            if (!shouldPlayIntro || target == null) return;

            target.localScale = _baseScale * introStartScale;

            if (fadeDuringIntro && canvasGroup != null) {
                canvasGroup.alpha = 0f;
            }

            if (_introRoutine != null) {
                StopCoroutine(_introRoutine);
            }

            _introRoutine = StartCoroutine(StartIntroAfterLayout());
        }

        private IEnumerator StartIntroAfterLayout() {
            yield return null;

            Canvas.ForceUpdateCanvases();
            CacheLayoutPosition();

            _introTimer = 0f;
            _introStarted = true;
            target.anchoredPosition = _baseAnchoredPosition + slideFromOffset;
            target.localScale = _baseScale * introStartScale;

            if (fadeDuringIntro && canvasGroup != null) {
                canvasGroup.alpha = 0f;
            }

            _introRoutine = null;
        }

        private void UpdateIntro(float deltaTime) {
            if (!_introStarted || _introComplete || target == null) return;

            _introTimer += deltaTime;
            var progress = Mathf.Clamp01((_introTimer - _introDelay) / introDuration);
            var easedProgress = EaseOutBack(progress);

            target.anchoredPosition = Vector2.LerpUnclamped(_baseAnchoredPosition + slideFromOffset, _baseAnchoredPosition, easedProgress);
            target.localScale = _baseScale * Mathf.LerpUnclamped(introStartScale, 1f, Mathf.Clamp(easedProgress, 0f, 1.08f));

            if (fadeDuringIntro && canvasGroup != null) {
                canvasGroup.alpha = progress;
            }

            _introComplete = progress >= 1f;
        }

        private void UpdateButtonPose(float deltaTime, float time) {
            if (!_introStarted || !_introComplete) return;

            if (_clickPulseTimer > 0f) {
                _clickPulseTimer = Mathf.Max(0f, _clickPulseTimer - deltaTime);
            }

            var idleWave = idleMotion ? Mathf.Sin((time + _timeOffset) * idleSpeed * Mathf.PI * 2f) : 0f;
            var activeScale = _selected ? selectedScale : (_hovered ? hoverScale : 1f);
            var pressScale = _pressed ? pressedScale : 1f;
            var clickPulse = GetClickPulse();
            var scale = activeScale * pressScale + clickPulse;

            var activeNudge = _selected ? selectedNudge : (_hovered ? hoverNudge : Vector2.zero);
            var idleNudge = new Vector2(0f, idleWave * idleFloatAmount);
            var desiredPosition = _baseAnchoredPosition + activeNudge + idleNudge;
            var desiredRotation = _baseRotation * Quaternion.Euler(0f, 0f, GetDesiredTilt(idleWave, time));

            target.anchoredPosition = Vector2.Lerp(target.anchoredPosition, desiredPosition, GetLerpAmount(hoverSnappiness, deltaTime));
            target.localScale = Vector3.Lerp(target.localScale, _baseScale * scale, GetLerpAmount(hoverSnappiness, deltaTime));
            target.localRotation = Quaternion.Slerp(target.localRotation, desiredRotation, GetLerpAmount(hoverSnappiness, deltaTime));

            UpdateTint(deltaTime);
        }

        private float GetDesiredTilt(float idleWave, float time) {
            var idleTilt = idleWave * idleTiltDegrees;
            if (!_selected) return idleTilt;

            var selectedWobble = Mathf.Sin((time + _timeOffset) * 8f) * selectedWobbleDegrees;
            return idleTilt + selectedWobble;
        }

        private float GetClickPulse() {
            if (_clickPulseTimer <= 0f) return 0f;

            var progress = 1f - _clickPulseTimer / clickPulseDuration;
            return Mathf.Sin(progress * Mathf.PI) * 0.08f;
        }

        private void UpdateTint(float deltaTime) {
            if (tintGraphic == null) return;

            var targetColor = _selected ? selectedTint : _baseTint;
            tintGraphic.color = Color.Lerp(tintGraphic.color, targetColor, GetLerpAmount(hoverSnappiness, deltaTime));
        }

        private void TriggerClickPulse() {
            _clickPulseTimer = clickPulseDuration;
        }

        #endregion
        #region Helpers

        private void ResolveReferences() {
            if (target == null) {
                target = transform as RectTransform;
            }

            if (tintGraphic == null) {
                tintGraphic = GetComponent<Graphic>();
            }

            if (canvasGroup == null) {
                canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        private void CacheStartingValues() {
            if (target == null) return;

            _baseAnchoredPosition = target.anchoredPosition;
            _baseScale = target.localScale;
            _baseRotation = target.localRotation;
            _baseTint = tintGraphic != null ? tintGraphic.color : Color.white;
            _timeOffset = Random.Range(0f, 100f);
        }

        private void CacheLayoutPosition() {
            if (target == null) return;

            _baseAnchoredPosition = target.anchoredPosition;
        }

        private void ResetVisuals() {
            if (target != null) {
                target.anchoredPosition = _baseAnchoredPosition;
                target.localScale = _baseScale;
                target.localRotation = _baseRotation;
            }

            if (tintGraphic != null) {
                tintGraphic.color = _baseTint;
            }

            if (canvasGroup != null) {
                canvasGroup.alpha = 1f;
            }
        }

        private static float EaseOutBack(float value) {
            const float overshoot = 1.70158f;
            var shifted = value - 1f;
            return 1f + shifted * shifted * ((overshoot + 1f) * shifted + overshoot);
        }

        private static float GetLerpAmount(float snappiness, float deltaTime) {
            return 1f - Mathf.Exp(-snappiness * deltaTime);
        }

        #endregion
    }
}
