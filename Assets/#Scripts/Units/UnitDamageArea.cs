using UnityEngine;

namespace _Scripts.Units {
    /// <summary>
    /// Tracks opposing units inside a unit's damage trigger.
    /// </summary>
    public class UnitDamageArea : MonoBehaviour {

        #region Variables

        private Unit _unit; // Unit that owns this damage area.

        #endregion
        #region Unity Methods

        private void Start() {
            _unit = GetComponentInParent<Unit>();
        }
        
        private void OnTriggerEnter2D(Collider2D other) {
            TryAddTarget(other);
        }
        
        private void OnTriggerStay2D(Collider2D other) {
            TryAddTarget(other);
        }

        private void OnTriggerExit2D(Collider2D other) {
            var otherUnit = other.GetComponent<Unit>();
            if (otherUnit != null && _unit != null && otherUnit.team != _unit.team) {
                _unit.targetUnits.Remove(otherUnit);
            }
        }

        #endregion
        #region Targeting

        /// <summary>
        /// Adds an opposing unit to the owner target list when possible.
        /// </summary>
        /// <param name="other">The collider entering or staying in the trigger.</param>
        private void TryAddTarget(Collider2D other) {
            if (_unit == null) return;

            var otherUnit = other.GetComponent<Unit>();
            if (otherUnit == null || otherUnit.team == _unit.team) return;

            if (!_unit.targetUnits.Contains(otherUnit)) {
                _unit.targetUnits.Add(otherUnit);
            }
        }

        #endregion
    }
}
