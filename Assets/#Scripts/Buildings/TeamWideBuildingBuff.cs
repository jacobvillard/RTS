using System.Collections.Generic;
using _Scripts.GameManagement;
using _Scripts.Units;
using UnityEngine;

namespace _Scripts.Buildings {
    /// <summary>
    /// Applies team-wide unit buffs while its building is captured.
    /// </summary>
    [RequireComponent(typeof(CapturableBuilding))]
    public class TeamWideBuildingBuff : MonoBehaviour {

        #region Variables

        [Header("Buffs")]
        [SerializeField] private float speedMultiplier = 1.03f;     // Team-wide movement multiplier when owned.
        [SerializeField] private float attackRateMultiplier = 1.03f; // Team-wide attack/reload-rate multiplier when owned.
        [SerializeField] private float refreshInterval = 0.5f;      // Seconds between global buff refreshes.

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

            GetTeamMultipliers(Team.Player, out var playerSpeed, out var playerAttackRate);
            GetTeamMultipliers(Team.AI, out var aiSpeed, out var aiAttackRate);

            ApplyTeamBuff(Team.Player, playerSpeed, playerAttackRate);
            ApplyTeamBuff(Team.AI, aiSpeed, aiAttackRate);
        }

        /// <summary>
        /// Gets additive multipliers for a team from all owned sources.
        /// </summary>
        /// <param name="team">Team to aggregate for.</param>
        /// <param name="speed">Final speed multiplier.</param>
        /// <param name="attackRate">Final attack-rate multiplier.</param>
        private static void GetTeamMultipliers(Team team, out float speed, out float attackRate) {
            speed = 1f;
            attackRate = 1f;

            foreach (var source in Sources) {
                if (source == null || source._building == null) continue;
                if (!source._building.TryGetOwnerTeam(out var ownerTeam) || ownerTeam != team) continue;

                speed += Mathf.Max(0f, source.speedMultiplier - 1f);
                attackRate += Mathf.Max(0f, source.attackRateMultiplier - 1f);
            }
        }

        /// <summary>
        /// Applies a final strategic buff to every living friendly unit.
        /// </summary>
        /// <param name="team">Team receiving the buff.</param>
        /// <param name="speed">Speed multiplier.</param>
        /// <param name="attackRate">Attack-rate multiplier.</param>
        private static void ApplyTeamBuff(Team team, float speed, float attackRate) {
            var units = BattleController.Instance.GetFriendlyUnits(team);
            foreach (var unit in units) {
                if (unit == null || !unit.IsAlive) continue;

                unit.SetStrategicBuffs(speed, attackRate);
            }
        }

        #endregion
    }
}
