using System.Collections;
using System.Collections.Generic;
using BitWave_Labs.AnimatedTextReveal;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI {
    /// <summary>
    /// Reveals level-select button labels one row at a time using AnimatedTextReveal.
    /// </summary>
    [DisallowMultipleComponent]
    public class LevelSelectButtonTextRevealController : MonoBehaviour {

        #region Variables

        [Header("Targets")]
        [SerializeField] private Transform linesParent;
        [SerializeField] private bool includeInactiveChildren = true;

        [Header("Reveal")]
        [SerializeField] private bool playOnEnable = true;
        [SerializeField, Min(0f)] private float initialDelay = 0.05f;
        [SerializeField, Min(0f)] private float delayBetweenLines = 0.08f;
        [SerializeField, Min(0.01f)] private float fadeSpeed = 20f;
        [SerializeField, Min(1)] private int characterSpread = 10;

        [Header("Rows")]
        [SerializeField] private bool hideButtonRowsBeforeReveal = true;
        [SerializeField] private bool disableButtonsUntilRevealed = true;
        [SerializeField] private bool useUnscaledTime = true;

        private readonly List<RevealLine> _lines = new();
        private Coroutine _revealRoutine;

        #endregion
        #region Unity Methods

        private void Awake() {
            ResolveReferences();
        }

        private void OnEnable() {
            ResolveReferences();

            if (playOnEnable) {
                PlayReveal();
            }
        }

        private void OnDisable() {
            if (_revealRoutine != null) {
                StopCoroutine(_revealRoutine);
                _revealRoutine = null;
            }
        }

        private void OnValidate() {
            ResolveReferences();
        }

        #endregion
        #region Public Methods

        public void PlayReveal() {
            ResolveReferences();

            if (_revealRoutine != null) {
                StopCoroutine(_revealRoutine);
            }

            _revealRoutine = StartCoroutine(RevealLines());
        }

        public void ShowImmediately() {
            ResolveReferences();

            foreach (var line in _lines) {
                line.SetRowVisible(true);
                line.SetButtonInteractable(true);
                line.Reveal?.Configure(line.Label, fadeSpeed, characterSpread);
                line.Reveal?.SetAllCharactersAlpha(255);
            }
        }

        #endregion
        #region Reveal

        private IEnumerator RevealLines() {
            RefreshLines();

            foreach (var line in _lines) {
                line.Reveal.Configure(line.Label, fadeSpeed, characterSpread);
                line.Reveal.SetAllCharactersAlpha(0);
                line.CaptureTargetInteractable();
                line.SetRowVisible(!hideButtonRowsBeforeReveal);
                line.SetButtonInteractable(!disableButtonsUntilRevealed);
            }

            if (initialDelay > 0f) {
                yield return Wait(initialDelay);
            }

            foreach (var line in _lines) {
                line.SetRowVisible(true);
                yield return line.Reveal.FadeText(true);
                line.RestoreTargetInteractable();

                if (delayBetweenLines > 0f) {
                    yield return Wait(delayBetweenLines);
                }
            }

            _revealRoutine = null;
        }

        #endregion
        #region Setup

        private void ResolveReferences() {
            if (linesParent == null) {
                linesParent = transform;
            }
        }

        private void RefreshLines() {
            _lines.Clear();

            if (linesParent == null) return;

            var buttons = linesParent.GetComponentsInChildren<Button>(includeInactiveChildren);
            foreach (var button in buttons) {
                if (button == null) continue;

                var label = button.GetComponentInChildren<TextMeshProUGUI>(includeInactiveChildren);
                if (label == null) continue;

                var reveal = label.GetComponent<AnimatedTextReveal>();
                if (reveal == null) {
                    reveal = label.gameObject.AddComponent<AnimatedTextReveal>();
                }

                var rowGroup = button.GetComponent<CanvasGroup>();
                if (rowGroup == null) {
                    rowGroup = button.gameObject.AddComponent<CanvasGroup>();
                }

                _lines.Add(new RevealLine(button, label, reveal, rowGroup));
            }
        }

        #endregion
        #region Helpers

        private IEnumerator Wait(float duration) {
            var elapsed = 0f;
            while (elapsed < duration) {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }
        }

        private sealed class RevealLine {
            public readonly Button Button;
            public readonly TextMeshProUGUI Label;
            public readonly AnimatedTextReveal Reveal;
            private readonly CanvasGroup _rowGroup;
            private bool _targetInteractable;

            public RevealLine(
                Button button,
                TextMeshProUGUI label,
                AnimatedTextReveal reveal,
                CanvasGroup rowGroup) {
                Button = button;
                Label = label;
                Reveal = reveal;
                _rowGroup = rowGroup;
            }

            public void CaptureTargetInteractable() {
                _targetInteractable = Button == null || Button.interactable;
            }

            public void RestoreTargetInteractable() {
                SetButtonInteractable(_targetInteractable);
            }

            public void SetRowVisible(bool visible) {
                if (_rowGroup == null) return;

                _rowGroup.alpha = visible ? 1f : 0f;
            }

            public void SetButtonInteractable(bool interactable) {
                if (Button != null) {
                    Button.interactable = interactable;
                }

                if (_rowGroup == null) return;

                _rowGroup.interactable = interactable;
                _rowGroup.blocksRaycasts = interactable;
            }
        }

        #endregion
    }
}
