using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI {
    /// <summary>
    /// Cycles a SpriteRenderer or UI Image through sprites in order.
    /// </summary>
    [DisallowMultipleComponent]
    public class SpriteCycleAnimator : MonoBehaviour {

        #region Variables

        [Header("Target")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Image image;

        [Header("Sprites")]
        [SerializeField] private Sprite[] sprites = new Sprite[3];

        [Header("Timing")]
        [SerializeField, Min(0.01f)] private float frameDuration = 0.2f;
        [SerializeField] private bool playOnEnable = true;
        [SerializeField] private bool useUnscaledTime = true;

        private int _currentIndex;
        private float _timer;
        private bool _isPlaying;

        #endregion
        #region Unity Methods

        private void Awake() {
            ResolveTarget();
        }

        private void OnEnable() {
            ResolveTarget();
            _isPlaying = playOnEnable;
            _timer = 0f;
            ApplySprite();
        }

        private void Update() {
            if (!_isPlaying || sprites == null || sprites.Length == 0) return;

            _timer += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            if (_timer < frameDuration) return;

            _timer -= frameDuration;
            _currentIndex = (_currentIndex + 1) % sprites.Length;
            ApplySprite();
        }

        private void OnValidate() {
            ResolveTarget();
        }

        #endregion
        #region Public Methods

        public void Play() {
            _isPlaying = true;
        }

        public void Stop() {
            _isPlaying = false;
        }

        public void Restart() {
            _currentIndex = 0;
            _timer = 0f;
            _isPlaying = true;
            ApplySprite();
        }

        #endregion
        #region Helpers

        private void ResolveTarget() {
            if (spriteRenderer == null) {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (image == null) {
                image = GetComponent<Image>();
            }
        }

        private void ApplySprite() {
            if (sprites == null || sprites.Length == 0) return;

            var sprite = sprites[Mathf.Clamp(_currentIndex, 0, sprites.Length - 1)];
            if (sprite == null) return;

            if (spriteRenderer != null) {
                spriteRenderer.sprite = sprite;
            }

            if (image != null) {
                image.sprite = sprite;
            }
        }

        #endregion
    }
}
