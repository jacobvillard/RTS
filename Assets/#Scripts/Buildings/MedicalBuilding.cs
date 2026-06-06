using _Scripts.Units;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.Buildings {
    /// <summary>
    /// Heals nearby units for the team that owns this building.
    /// </summary>
    [RequireComponent(typeof(CapturableBuilding))]
    public class MedicalBuilding : MonoBehaviour {

        #region Variables

        [Header("Healing")]
        [SerializeField] private float healingRadius = 3f;       // Units inside this radius can be healed.
        [SerializeField] private float healingPerSecond = 5f;    // Health restored per second.
        [SerializeField, Range(0f, 1f)] private float healCap = 0.5f; // Maximum health percentage this building can restore to.
        [SerializeField] private float refreshInterval = 0.25f;  // Seconds between heal ticks.

        private readonly Collider2D[] _nearbyColliders = new Collider2D[96]; // Reused heal query buffer.
        private readonly HashSet<Unit> _healedUnitsThisTick = new();         // Prevents multi-collider units healing more than once per tick.
        private CapturableBuilding _building;                                // Ownership source.
        private float _nextRefreshTime;                                      // Next allowed heal tick.

        #endregion
        #region Unity Methods

        private void Awake() {
            _building = GetComponent<CapturableBuilding>();
        }

        private void Update() {
            if (Time.time < _nextRefreshTime) return;

            _nextRefreshTime = Time.time + refreshInterval;
            HealOwnedUnits();
        }

        #endregion
        #region Healing

        /// <summary>
        /// Heals nearby units that match the current owner.
        /// </summary>
        private void HealOwnedUnits() {
            if (_building == null || !_building.TryGetOwnerTeam(out var ownerTeam)) return;

            _healedUnitsThisTick.Clear();
            var healAmount = healingPerSecond * refreshInterval;
            var count = Physics2D.OverlapCircleNonAlloc(transform.position, healingRadius, _nearbyColliders);
            for (var i = 0; i < count; i++) {
                var unit = _nearbyColliders[i] != null
                    ? _nearbyColliders[i].GetComponentInParent<Unit>()
                    : null;

                if (unit == null || !unit.IsAlive || unit.team != ownerTeam) continue;
                if (!_healedUnitsThisTick.Add(unit)) continue;

                unit.Heal(healAmount, unit.MaxHealth * healCap);
            }
        }

        #endregion
    }
}
