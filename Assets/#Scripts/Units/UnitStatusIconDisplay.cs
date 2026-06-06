using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.Units {
    /// <summary>
    /// Toggles small per-unit status icons for morale, healing, and captured-building buffs.
    /// </summary>
    public class UnitStatusIconDisplay : MonoBehaviour {

        #region Variables

        [Header("Icons")]
        [SerializeField] private GameObject moraleBoostIcon;   // Icon shown while the unit has a bannerman morale boost.
        [SerializeField] private GameObject healingIcon;       // Icon shown while the unit is being healed.
        [SerializeField] private GameObject strategicBuffIcon; // Icon shown while captured buildings buff the unit.

        [Header("Display")]
        [SerializeField] private bool playerUnitsOnly = true; // Hides icons on AI units when enabled.

        [Header("Pulse Colours")]
        [SerializeField] private Color moraleBoostColour = new(0.992f, 0.522f, 0f); // Colour pulsed with white for morale icons.
        [SerializeField] private Color healingColour = Color.green;          // Colour pulsed with white for healing icons.
        [SerializeField] private Color strategicBuffColour = Color.yellow;   // Colour pulsed with white for building-buff icons.
        [SerializeField] private float pulseSpeed = 3f;                      // How quickly icons pulse between colour and white.

        private Unit _unit; // Unit whose statuses are displayed.

        #endregion
        #region Unity Methods

        private void Awake() {
            ResolveUnit();
            HideAllIcons();
        }

        private void Update() {
            ResolveUnit();
            RefreshIcons();
        }

        #endregion
        #region Display

        /// <summary>
        /// Updates every icon to match the unit's current status flags.
        /// </summary>
        private void RefreshIcons() {
            if (_unit == null || !_unit.IsAlive || (playerUnitsOnly && _unit.team != Team.Player)) {
                HideAllIcons();
                return;
            }

            SetIconActive(moraleBoostIcon, _unit.HasMoraleBoost);
            SetIconActive(healingIcon, _unit.IsBeingHealed);
            SetIconActive(strategicBuffIcon, _unit.HasStrategicBuff);

            PulseIcon(moraleBoostIcon, moraleBoostColour);
            PulseIcon(healingIcon, healingColour);
            PulseIcon(strategicBuffIcon, strategicBuffColour);
        }

        /// <summary>
        /// Hides every configured icon.
        /// </summary>
        private void HideAllIcons() {
            SetIconActive(moraleBoostIcon, false);
            SetIconActive(healingIcon, false);
            SetIconActive(strategicBuffIcon, false);
        }

        /// <summary>
        /// Toggles an icon when assigned.
        /// </summary>
        /// <param name="icon">Icon object to toggle.</param>
        /// <param name="isActive">Whether it should be visible.</param>
        private static void SetIconActive(GameObject icon, bool isActive) {
            if (icon != null && icon.activeSelf != isActive) {
                icon.SetActive(isActive);
            }
        }

        /// <summary>
        /// Pulses an active icon between its configured colour and white.
        /// </summary>
        /// <param name="icon">Icon object to colour.</param>
        /// <param name="colour">Configured pulse colour.</param>
        private void PulseIcon(GameObject icon, Color colour) {
            if (icon == null || !icon.activeSelf) return;

            var pulseAmount = Mathf.PingPong(Time.time * pulseSpeed, 1f);
            var pulseColour = Color.Lerp(colour, Color.white, pulseAmount);
            ApplyIconColour(icon, pulseColour);
        }

        /// <summary>
        /// Applies colour to world-space sprites and UI images under an icon root.
        /// </summary>
        /// <param name="icon">Icon object to colour.</param>
        /// <param name="colour">Colour to apply.</param>
        private static void ApplyIconColour(GameObject icon, Color colour) {
            var spriteRenderers = icon.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var spriteRenderer in spriteRenderers) {
                spriteRenderer.color = colour;
            }

            var images = icon.GetComponentsInChildren<Image>(true);
            foreach (var image in images) {
                image.color = colour;
            }
        }

        /// <summary>
        /// Finds the runtime Unit component after UnitInit has created it.
        /// </summary>
        private void ResolveUnit() {
            if (_unit == null) {
                _unit = GetComponentInParent<Unit>();
            }
        }

        #endregion
    }
}
