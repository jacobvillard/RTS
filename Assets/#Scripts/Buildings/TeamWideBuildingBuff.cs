using System.Collections.Generic;
using _Scripts.GameManagement;
using _Scripts.Units;
using UnityEngine;

namespace _Scripts.Buildings {
    /// <summary>
    /// Applies strategic unit buffs around a captured building.
    /// </summary>
    [RequireComponent(typeof(CapturableBuilding))]
    public class TeamWideBuildingBuff : MonoBehaviour {

        #region Variables

        [Header("Buffs")]
        [SerializeField] private float buffRadius = 12f;            // Friendly units inside this radius receive the captured-building buff.
        [SerializeField] private float speedMultiplier = 1.03f;     // Movement multiplier applied while inside radius.
        [SerializeField] private float attackRateMultiplier = 1.03f; // Attack/reload-rate multiplier applied while inside radius.
        [SerializeField] private float refreshInterval = 0.5f;      // Seconds between buff refreshes.

        private static readonly List<TeamWideBuildingBuff> Sources = new(); // Active buff sources in the loaded scene.
        private static float _nextGlobalRefreshTime;                       // Shared next refresh time.

        private CapturableBuilding _building; // Building that controls ownership.

        #endregion
        #region Unity Methods

        private void Awake() {
            _building = GetComponent<CapturableBuilding>();
        }

        private void OnEnable() {
            if (!Sources.Contains(this)) {
                Sources.Add(this);
            }
        }

        private void OnDisable() {
            Sources.Remove(this);
            ApplyAllBuffs();
        }

        private void Update() {
            if (Time.time < _nextGlobalRefreshTime) return;

            _nextGlobalRefreshTime = Time.time + refreshInterval;
            ApplyAllBuffs();
        }

        #endregion
        #region Buff Application

        /// <summary>
        /// Aggregates every active captured-building buff and applies it to both teams.
        /// </summary>
        private static void ApplyAllBuffs() {
            if (BattleController.Instance == null) return;

            ApplyTeamBuffs(Team.Player);
            ApplyTeamBuffs(Team.AI);
        }

        /// <summary>
        /// Applies final strategic buffs to every living unit on one team.
        /// </summary>
        /// <param name="team">Team receiving owned building buffs.</param>
        private static void ApplyTeamBuffs(Team team) {
            var units = BattleController.Instance.GetFriendlyUnits(team);
            foreach (var unit in units) {
                if (unit == null || !unit.IsAlive) continue;

                GetUnitMultipliers(unit, out var speed, out var attackRate, out var hasStrategicBuff);
                unit.SetStrategicBuffs(speed, attackRate, hasStrategicBuff);
            }
        }

        /// <summary>
        /// Gets additive multipliers for a unit from all nearby owned sources.
        /// </summary>
        /// <param name="unit">Unit to calculate buffs for.</param>
        /// <param name="speed">Final speed multiplier.</param>
        /// <param name="attackRate">Final attack-rate multiplier.</param>
        /// <param name="hasStrategicBuff">Whether at least one owned building is affecting the unit.</param>
        private static void GetUnitMultipliers(Unit unit, out float speed, out float attackRate, out bool hasStrategicBuff) {
            speed = 1f;
            attackRate = 1f;
            hasStrategicBuff = false;

            foreach (var source in Sources) {
                if (!source.CanBuffUnit(unit)) continue;

                hasStrategicBuff = true;
                speed += Mathf.Max(0f, source.speedMultiplier - 1f);
                attackRate += Mathf.Max(0f, source.attackRateMultiplier - 1f);
            }
        }

        /// <summary>
        /// Checks whether this captured building should buff a unit.
        /// </summary>
        /// <param name="unit">Potential receiving unit.</param>
        /// <returns>True when the source is owned by the unit's team and within radius.</returns>
        private bool CanBuffUnit(Unit unit) {
            if (unit == null || !unit.IsAlive || _building == null) return false;
            if (!_building.TryGetOwnerTeam(out var ownerTeam) || ownerTeam != unit.team) return false;

            var distanceSqr = ((Vector2)unit.transform.position - (Vector2)transform.position).sqrMagnitude;
            return distanceSqr <= buffRadius * buffRadius;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected() {
            Gizmos.color = new Color(1f, 0.92f, 0.1f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, buffRadius);
        }
#endif

        #endregion
    }
}
