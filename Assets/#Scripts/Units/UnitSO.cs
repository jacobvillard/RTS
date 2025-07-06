using UnityEngine;

namespace _Scripts.Units {
    
    /// <summary>
    /// A ScriptableObject that represents a unit in the game.
    /// </summary>
    [CreateAssetMenu(fileName = "New Unit", menuName = "Unit")]
    public class UnitSO : ScriptableObject {
        public UnitType unitType;
        public int health = 100;
        public float moveSpeed = 1f;
        public float attackRange = 1f;
        public int attackDamage = 20;
        public float attackCooldown = 1f;
        public Sprite icon;
    }
}
