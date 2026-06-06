using _Scripts.GameManagement;
using UnityEngine;

namespace _Scripts.Units {
    /// <summary>
    /// Selection behaviour for officers that opens and closes the officer command HUD.
    /// </summary>
    public class OfficerSelectedUnit : SelectedUnit {

        #region Selection

        /// <summary>
        /// Selects the officer and opens the command HUD.
        /// </summary>
        public override void SelectUnit() {
            if (_isSelected) return;

            base.SelectUnit();
            var officerCommands = GetComponentInParent<OfficerCommandController>();
            if (officerCommands != null) {
                GameManager.Instance?.ShowCommandUi(officerCommands);
            }
        }

        /// <summary>
        /// Deselects the officer and closes the command HUD.
        /// </summary>
        public override void DeselectUnit() {
            var officerCommands = GetComponentInParent<OfficerCommandController>();
            base.DeselectUnit();

            if (officerCommands != null) {
                GameManager.Instance?.HideCommandUi(officerCommands);
            }
        }

        #endregion
    }
}
