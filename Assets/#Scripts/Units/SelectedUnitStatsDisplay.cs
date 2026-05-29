using _Scripts.GameManagement;
using TMPro;
using UnityEngine;

namespace _Scripts.Units {
    /// <summary>
    /// Displays class, health, and measured real-time speed for the currently selected unit.
    /// </summary>
    public class SelectedUnitStatsDisplay : MonoBehaviour {

        #region Variables

        [Header("Parent")]
        [SerializeField] private GameObject statsParent; // Parent UI object toggled when a unit is selected.

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI classTypeText; // Text showing the selected unit class.
        [SerializeField] private TextMeshProUGUI healthText;    // Text showing current health.
        [SerializeField] private TextMeshProUGUI speedText;     // Text showing actual real-time movement speed.

        [Header("Speed Display")]
        [SerializeField] private float speedSampleInterval = 0.05f; // Time window used to measure movement speed.
        [SerializeField] private float speedSmoothing = 4f;         // How quickly the displayed speed follows measured speed.

        private Unit _lastSelectedUnit; // Last unit used to refresh the display.
        private Vector3 _lastUnitPosition; // Last sampled unit position used for speed calculation.
        private float _sampleDistance;     // Distance accumulated during the current speed sample.
        private float _sampleTimer;        // Time accumulated during the current speed sample.
        private float _measuredSpeed;      // Latest interval-measured movement speed.
        private float _displayedSpeed;     // Smoothed speed shown in the UI.

        #endregion
        #region Unity Methods

        private void Start() {
            RefreshDisplay();
        }

        private void Update() {
            RefreshDisplay();
        }

        #endregion
        #region Display

        /// <summary>
        /// Updates visibility and text from the battle controller selection.
        /// </summary>
        private void RefreshDisplay() {
            var selectedUnit = BattleController.Instance != null ? BattleController.Instance.SelectedUnit : null;
            var hasSelectedUnit = selectedUnit != null && selectedUnit.IsAlive;

            if (statsParent != null && statsParent.activeSelf != hasSelectedUnit) {
                statsParent.SetActive(hasSelectedUnit);
            }

            if (!hasSelectedUnit) {
                _lastSelectedUnit = null;
                ResetSpeed();
                return;
            }

            if (_lastSelectedUnit != selectedUnit) {
                _lastSelectedUnit = selectedUnit;
                _lastUnitPosition = selectedUnit.transform.position;
                ResetSpeed();
                UpdateStatsText(selectedUnit);
                return;
            }

            UpdateActualSpeed(selectedUnit);
            UpdateStatsText(selectedUnit);
        }

        /// <summary>
        /// Measures how quickly the selected unit moved since the previous frame.
        /// </summary>
        /// <param name="selectedUnit">The unit currently selected.</param>
        private void UpdateActualSpeed(Unit selectedUnit) {
            if (Time.deltaTime <= 0f) {
                return;
            }

            var currentPosition = selectedUnit.transform.position;
            _sampleDistance += Vector3.Distance(_lastUnitPosition, currentPosition);
            _sampleTimer += Time.deltaTime;
            _lastUnitPosition = currentPosition;

            if (_sampleTimer >= speedSampleInterval) {
                _measuredSpeed = _sampleDistance / _sampleTimer;
                _sampleDistance = 0f;
                _sampleTimer = 0f;
            }

            _displayedSpeed = Mathf.Lerp(
                _displayedSpeed,
                _measuredSpeed,
                1f - Mathf.Exp(-speedSmoothing * Time.deltaTime));
        }

        /// <summary>
        /// Writes the selected unit stats into the configured TMP labels.
        /// </summary>
        /// <param name="selectedUnit">The unit currently selected.</param>
        private void UpdateStatsText(Unit selectedUnit) {
            SetText(classTypeText, "Class: " + selectedUnit.ClassType);
            SetText(healthText, "Health: " + selectedUnit.CurrentHealth.ToString("0"));
            SetText(speedText, "Speed: " + _displayedSpeed.ToString("0.00"));
        }

        /// <summary>
        /// Clears speed sampling state when no unit is selected or the selection changes.
        /// </summary>
        private void ResetSpeed() {
            _sampleDistance = 0f;
            _sampleTimer = 0f;
            _measuredSpeed = 0f;
            _displayedSpeed = 0f;
        }

        /// <summary>
        /// Safely writes text when a label has been assigned.
        /// </summary>
        /// <param name="text">The TMP label to update.</param>
        /// <param name="value">The value to show.</param>
        private static void SetText(TextMeshProUGUI text, string value) {
            if (text != null) {
                text.text = value;
            }
        }

        #endregion
    }
}
