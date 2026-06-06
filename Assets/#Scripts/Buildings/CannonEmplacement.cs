using _Scripts.GameManagement;
using _Scripts.Units;
using UnityEngine;

namespace _Scripts.Buildings {
    /// <summary>
    /// A slow movable cannon that can be manned by friendly infantry and fires scatter damage.
    /// </summary>
    public class CannonEmplacement : MonoBehaviour {

        #region Variables

        [Header("Ownership")]
        [SerializeField] private CapturableBuilding owningBuilding; // Optional building that controls who can use the cannon.

        [Header("Manning")]
        [SerializeField] private float manningRadius = 1.5f; // Friendly infantry inside this radius can operate the cannon.

        [Header("Combat")]
        [SerializeField] private float attackRange = 7f;       // Cannon target range.
        [SerializeField] private float scatterRadius = 1.25f;  // Area damaged around the target.
        [SerializeField] private float damage = 35f;           // Scatter damage dealt to each enemy hit.
        [SerializeField] private float cooldown = 4f;          // Seconds between cannon shots.

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 0.35f; // Very slow movement speed for repositioning.

        private readonly Collider2D[] _nearbyColliders = new Collider2D[96]; // Reused unit query buffer.
        private Vector2 _moveDestination;                                    // Destination used when repositioning.
        private bool _hasMoveDestination;                                    // True while cannon is moving.
        private float _attackTimer;                                          // Cannon fire timer.

        #endregion
        #region Unity Methods

        private void Awake() {
            if (owningBuilding == null) {
                owningBuilding = GetComponent<CapturableBuilding>();
            }
        }

        private void Update() {
            MoveIfOrdered();
            TryFire();
        }

        #endregion
        #region Commands

        /// <summary>
        /// Orders this cannon to move very slowly toward a point.
        /// </summary>
        /// <param name="destination">The world position to move toward.</param>
        public void MoveTo(Vector2 destination) {
            _moveDestination = destination;
            _hasMoveDestination = true;
        }

        #endregion
        #region Movement

        /// <summary>
        /// Moves toward the current destination when ordered.
        /// </summary>
        private void MoveIfOrdered() {
            if (!_hasMoveDestination) return;

            transform.position = Vector3.MoveTowards(transform.position, _moveDestination, moveSpeed * Time.deltaTime);
            if (Vector2.Distance(transform.position, _moveDestination) <= 0.05f) {
                _hasMoveDestination = false;
            }
        }

        #endregion
        #region Combat

        /// <summary>
        /// Fires at the nearest enemy when the cannon is owned and manned.
        /// </summary>
        private void TryFire() {
            _attackTimer += Time.deltaTime;
            if (_attackTimer < cooldown) return;
            if (!TryGetOwnerTeam(out var ownerTeam)) return;
            if (!HasManningInfantry(ownerTeam)) return;

            var target = FindNearestEnemy(ownerTeam);
            if (target == null) return;

            _attackTimer = 0f;
            DamageScatter(target, ownerTeam);
        }

        /// <summary>
        /// Gets the team currently able to use this cannon.
        /// </summary>
        /// <param name="ownerTeam">Owning team.</param>
        /// <returns>True when a team can own this cannon.</returns>
        private bool TryGetOwnerTeam(out Team ownerTeam) {
            if (owningBuilding != null) {
                return owningBuilding.TryGetOwnerTeam(out ownerTeam);
            }

            ownerTeam = Team.AI;
            return false;
        }

        /// <summary>
        /// Checks for friendly infantry-style units close enough to crew the cannon.
        /// </summary>
        /// <param name="ownerTeam">Team that owns the cannon.</param>
        /// <returns>True when crew is available.</returns>
        private bool HasManningInfantry(Team ownerTeam) {
            var count = Physics2D.OverlapCircleNonAlloc(transform.position, manningRadius, _nearbyColliders);
            for (var i = 0; i < count; i++) {
                var unit = _nearbyColliders[i] != null
                    ? _nearbyColliders[i].GetComponentInParent<Unit>()
                    : null;

                if (unit == null || !unit.IsAlive || unit.team != ownerTeam) continue;
                if (IsInfantryStyleUnit(unit)) return true;
            }

            return false;
        }

        /// <summary>
        /// Finds the nearest enemy unit inside cannon range.
        /// </summary>
        /// <param name="ownerTeam">Team that owns the cannon.</param>
        /// <returns>The nearest enemy unit, or null.</returns>
        private Unit FindNearestEnemy(Team ownerTeam) {
            if (BattleController.Instance == null) return null;

            Unit closest = null;
            var closestDistance = Mathf.Infinity;
            var enemies = BattleController.Instance.GetOpposingUnits(ownerTeam);
            foreach (var enemy in enemies) {
                if (enemy == null || !enemy.IsAlive) continue;

                var distance = Vector2.Distance(transform.position, enemy.transform.position);
                if (distance > attackRange || distance >= closestDistance) continue;

                closestDistance = distance;
                closest = enemy;
            }

            return closest;
        }

        /// <summary>
        /// Checks whether a unit can crew a cannon.
        /// </summary>
        /// <param name="unit">Unit to inspect.</param>
        /// <returns>True when the unit is infantry-style.</returns>
        private static bool IsInfantryStyleUnit(Unit unit) {
            if (unit == null) return false;

            return unit.ClassType == UnitType.Infantry ||
                   unit.ClassType == UnitType.Pikemen ||
                   unit.ClassType == UnitType.Officer;
        }

        /// <summary>
        /// Damages all enemies near the target impact point.
        /// </summary>
        /// <param name="target">Primary target.</param>
        /// <param name="ownerTeam">Team that fired.</param>
        private void DamageScatter(Unit target, Team ownerTeam) {
            var count = Physics2D.OverlapCircleNonAlloc(target.transform.position, scatterRadius, _nearbyColliders);
            for (var i = 0; i < count; i++) {
                var unit = _nearbyColliders[i] != null
                    ? _nearbyColliders[i].GetComponentInParent<Unit>()
                    : null;

                if (unit == null || !unit.IsAlive || unit.team == ownerTeam) continue;

                unit.ApplyDirectDamage(damage);
            }
        }

        #endregion
    }
}
