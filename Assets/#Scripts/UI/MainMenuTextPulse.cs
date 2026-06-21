using TMPro;
using UnityEngine;

namespace _Scripts.UI {
    /// <summary>
    /// Adds breathing scale, rotation, color, and TMP glow animation to a menu title.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class MainMenuTextPulse : MonoBehaviour {

        #region Variables

        [Header("Target")]
        [SerializeField] private TextMeshProUGUI titleText; // TMP label to animate.

        [Header("Pulse")]
        [SerializeField] private bool useUnscaledTime = true;
        [SerializeField, Min(0f)] private float speed = 1f;
        [SerializeField, Range(0f, 0.5f)] private float growAmount = 0.08f;
        [SerializeField, Range(0f, 25f)] private float rotationAmount = 2.5f;
        [SerializeField, Range(0f, 0.25f)] private float driftAmount = 0.03f;

        [Header("Color")]
        [SerializeField, Range(0f, 1f)] private float saturationBoost = 0.25f;
        [SerializeField, Range(0f, 1f)] private float brightnessBoost = 0.15f;
        [SerializeField] private Color glowColor = new(1f, 0.84f, 0.35f, 1f);

        [Header("Glow")]
        [SerializeField] private bool animateGlow = true;
        [SerializeField, Range(0f, 1f)] private float minGlowPower = 0.08f;
        [SerializeField, Range(0f, 1f)] private float maxGlowPower = 0.38f;
        [SerializeField, Range(0f, 1f)] private float glowOuter = 0.35f;

        private RectTransform _rectTransform;
        private Vector3 _baseScale;
        private Quaternion _baseRotation;
        private Color _baseColor;
        private Color _boostedColor;
        private Material _originalMaterial;
        private Material _runtimeMaterial;
        private float _timeOffset;

        #endregion
        #region Unity Methods

        private void Awake() {
            ResolveReferences();
            CacheStartingValues();
            CreateRuntimeMaterial();
        }

        private void OnEnable() {
            ResolveReferences();
            CacheStartingValues();
            CreateRuntimeMaterial();
        }

        private void Update() {
            if (titleText == null || _rectTransform == null) return;

            var time = (useUnscaledTime ? Time.unscaledTime : Time.time) * speed + _timeOffset;
            var breath = SmoothPulse(time);
            var slowDrift = Mathf.Sin(time * 0.43f + 1.7f);
            var quickLift = Mathf.Sin(time * 1.87f + 0.4f) * driftAmount;

            var scale = 1f + breath * growAmount + quickLift;
            _rectTransform.localScale = _baseScale * scale;
            _rectTransform.localRotation = _baseRotation * Quaternion.Euler(0f, 0f, slowDrift * rotationAmount);

            var colorPulse = Mathf.Clamp01(0.5f + breath * 0.5f);
            titleText.color = Color.Lerp(_baseColor, _boostedColor, colorPulse);

            UpdateGlow(colorPulse);
        }

        private void OnDisable() {
            ResetAnimatedValues();
        }

        private void OnDestroy() {
            if (_runtimeMaterial == null) return;

            if (titleText != null && _originalMaterial != null) {
                titleText.fontMaterial = _originalMaterial;
            }

            if (Application.isPlaying) {
                Destroy(_runtimeMaterial);
            } else {
                DestroyImmediate(_runtimeMaterial);
            }
        }

        private void OnValidate() {
            minGlowPower = Mathf.Min(minGlowPower, maxGlowPower);
            maxGlowPower = Mathf.Max(maxGlowPower, minGlowPower);
            ResolveReferences();
        }

        #endregion
        #region Animation

        /// <summary>
        /// Combines two softened waves so the title breathes instead of ticking.
        /// </summary>
        private static float SmoothPulse(float time) {
            var main = Mathf.Sin(time * Mathf.PI * 2f);
            var secondary = Mathf.Sin(time * Mathf.PI * 1.13f + 0.8f) * 0.35f;
            var wave = Mathf.Clamp01((main + secondary + 1f) * 0.5f);
            return Mathf.SmoothStep(-1f, 1f, wave);
        }

        private void UpdateGlow(float pulse) {
            if (!animateGlow || _runtimeMaterial == null) return;

            if (_runtimeMaterial.HasProperty(ShaderUtilities.ID_GlowColor)) {
                _runtimeMaterial.SetColor(ShaderUtilities.ID_GlowColor, glowColor);
            }

            if (_runtimeMaterial.HasProperty(ShaderUtilities.ID_GlowPower)) {
                _runtimeMaterial.SetFloat(ShaderUtilities.ID_GlowPower, Mathf.Lerp(minGlowPower, maxGlowPower, pulse));
            }

            if (_runtimeMaterial.HasProperty(ShaderUtilities.ID_GlowOuter)) {
                _runtimeMaterial.SetFloat(ShaderUtilities.ID_GlowOuter, glowOuter);
            }
        }

        #endregion
        #region Setup

        private void ResolveReferences() {
            if (titleText == null) {
                titleText = GetComponent<TextMeshProUGUI>();
            }

            if (_rectTransform == null) {
                _rectTransform = transform as RectTransform;
            }
        }

        private void CacheStartingValues() {
            if (titleText == null || _rectTransform == null) return;

            _baseScale = _rectTransform.localScale;
            _baseRotation = _rectTransform.localRotation;
            _baseColor = titleText.color;
            _boostedColor = GetBoostedColor(_baseColor);
            _timeOffset = Random.Range(0f, 100f);
        }

        private void CreateRuntimeMaterial() {
            if (titleText == null || titleText.fontSharedMaterial == null || _runtimeMaterial != null) return;

            _originalMaterial = titleText.fontSharedMaterial;
            _runtimeMaterial = Instantiate(titleText.fontSharedMaterial);
            _runtimeMaterial.name = $"{titleText.fontSharedMaterial.name} Main Menu Pulse";
            _runtimeMaterial.EnableKeyword("GLOW_ON");
            titleText.fontMaterial = _runtimeMaterial;
            UpdateGlow(0f);
        }

        private Color GetBoostedColor(Color color) {
            Color.RGBToHSV(color, out var hue, out var saturation, out var value);
            saturation = Mathf.Clamp01(saturation + saturationBoost);
            value = Mathf.Clamp01(value + brightnessBoost);

            var boosted = Color.HSVToRGB(hue, saturation, value);
            boosted.a = color.a;
            return boosted;
        }

        private void ResetAnimatedValues() {
            if (_rectTransform != null) {
                _rectTransform.localScale = _baseScale;
                _rectTransform.localRotation = _baseRotation;
            }

            if (titleText != null) {
                titleText.color = _baseColor;
            }
        }

        #endregion
    }
}
