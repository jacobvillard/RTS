using UnityEngine;

namespace _Scripts.Units {
    
    /// <summary>
    /// Manages the damage area of a unit.
    /// </summary>
    public class UnitDamageArea : MonoBehaviour {
        
        /// <summary>
        /// The unit this damage area belongs to.
        /// </summary>
        private Unit _unit;

        // Start is called before the first frame update
        private void Start() {
            _unit = GetComponentInParent<Unit>();
        }
        
        
        /// <summary>
        /// Whenever a collider enters our trigger, check if it's an opposing unit and store it.
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other) {
            // Attempt to get a Unit component on the collider
            var otherUnit = other.GetComponent<Unit>();
            
            // If the other collider doesn't have a Unit component, it's not a unit; ignore it
            if (otherUnit == null || otherUnit.team == _unit.team) return;
            
            
            // Opposing team; add it to our target list
            if (!_unit.targetUnits.Contains(otherUnit)) {
                _unit.targetUnits.Add(otherUnit);
            }
        }
        
        private void OnTriggerStay2D(Collider2D other)
        {
            // // Attempt to get a Unit component on the collider
            var otherUnit = other.GetComponent<Unit>();
            
            // If the other collider doesn't have a Unit component, it's not a unit; ignore it
            if (otherUnit == null || otherUnit.team == _unit.team) return;
            
            
            // Opposing team; add it to our target list
            if (!_unit.targetUnits.Contains(otherUnit)) {
                _unit.targetUnits.Add(otherUnit);
            }
        }
    

        /// <summary>
        /// Whenever a collider leaves our trigger, remove it from our list if it's there.
        /// </summary>
        private void OnTriggerExit2D(Collider2D other) {
            var otherUnit = other.GetComponent<Unit>();
            if (otherUnit != null && otherUnit.team != _unit.team) {
                _unit.targetUnits.Remove(otherUnit);
            }
        }
    }
}
