using UnityEngine;

namespace _Scripts.Units {
    /// <summary>
    /// Stores base stats and UI data for a unit type.
    /// </summary>
    [CreateAssetMenu(fileName = "New Unit", menuName = "Unit")]
    public class UnitSO : ScriptableObject {

        #region Variables

        [Header("Identity")]
        public UnitType unitType; // Unit role used by combat advantage rules.
        public Sprite icon;       // UI/icon sprite for this unit type.

        [Header("Stats")]
        public int health = 100;            // Starting health.
        public float moveSpeed = 1f;        // Base movement speed.
        public float attackRange = 1f;      // Distance at which the unit can attack.
        public int attackDamage = 20;       // Base damage per attack.
        public float attackCooldown = 1f;   // Seconds between attacks.

        #endregion
    }
}
