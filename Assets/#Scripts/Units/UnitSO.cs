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

        [Header("AI Response")]
        public float aiAssistCallRadius = 3f;             // Distance used by AI units to call nearby allies for help.
        public float aiMusketPathRangeMultiplier = 1.25f; // How much longer than musket range an AI path can be before retreating.
        public float aiMusketRetreatPadding = 2f;         // Extra distance AI tries to add when retreating from muskets.

        #endregion
    }
}
