using _Scripts.Units;
using UnityEngine;

namespace _Scripts.UI {
    /// <summary>
    /// UI bridge for officer command buttons.
    /// </summary>
    public class OfficerCommandHud : MonoBehaviour {

        #region Variables

        [Header("UI")]
        [SerializeField] private GameObject commandRoot; // Root object toggled while an officer is selected.

        private OfficerCommandController _currentOfficer; // Officer currently driving the command buttons.

        #endregion
        #region Unity Methods

        private void Awake() {
            if (commandRoot == null) {
                commandRoot = gameObject;
            }

            Hide();
        }

        #endregion
        #region Display

        /// <summary>
        /// Shows command buttons for a selected officer.
        /// </summary>
        /// <param name="officer">Selected officer command controller.</param>
        public void Show(OfficerCommandController officer) {
            _currentOfficer = officer;

            if (commandRoot != null) {
                commandRoot.SetActive(_currentOfficer != null);
            }
        }

        /// <summary>
        /// Hides the command buttons and clears the selected officer.
        /// </summary>
        public void Hide() {
            _currentOfficer = null;

            if (commandRoot != null) {
                commandRoot.SetActive(false);
            }
        }

        /// <summary>
        /// Hides the command buttons only if they belong to the supplied officer.
        /// </summary>
        /// <param name="officer">Officer being deselected.</param>
        public void HideIfCurrent(OfficerCommandController officer) {
            if (_currentOfficer != officer) return;

            Hide();
        }

        #endregion
        #region Buttons

        /// <summary>
        /// Button hook for the officer hold command.
        /// </summary>
        public void PressHold() {
            _currentOfficer?.ShowCommandRangeIndicator();
            _currentOfficer?.Hold();
        }

        /// <summary>
        /// Button hook for the officer attack command.
        /// </summary>
        public void PressAttack() {
            _currentOfficer?.ShowCommandRangeIndicator();
            _currentOfficer?.Attack();
        }

        /// <summary>
        /// Button hook for the officer follow-me command.
        /// </summary>
        public void PressFollowMe() {
            _currentOfficer?.ShowCommandRangeIndicator();
            _currentOfficer?.FollowMe();
        }

        #endregion
    }
}
