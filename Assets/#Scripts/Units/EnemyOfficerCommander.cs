using UnityEngine;

namespace _Scripts.Units {
    /// <summary>
    /// Lets AI officers periodically issue simple commands to nearby AI units.
    /// </summary>
    [RequireComponent(typeof(OfficerCommandController))]
    public class EnemyOfficerCommander : MonoBehaviour {

        #region Variables

        [Header("Timing")]
        [SerializeField] private float commandInterval = 1.5f; // Seconds between AI officer commands.

        [Header("Behaviour")]
        [SerializeField] private bool orderAttackWhenEnemyVisible = true; // Attacks when the officer can see an enemy.
        [SerializeField] private bool gatherNearbyUnitsWhenIdle = true;   // Uses Follow Me when no visible enemy is found.

        private OfficerCommandController _officerCommands; // Command component shared with player officer UI.
        private Unit _officerUnit;                         // Runtime unit attached after UnitInit runs.
        private float _nextCommandTime;                    // Next allowed command time.

        #endregion
        #region Unity Methods

        private void Awake() {
            _officerCommands = GetComponent<OfficerCommandController>();
        }

        private void Update() {
            if (Time.time < _nextCommandTime) return;

            _nextCommandTime = Time.time + commandInterval;
            ResolveOfficerUnit();
            if (_officerUnit == null || !_officerUnit.IsAlive || _officerUnit.team != Team.AI) return;

            IssueCommand();
        }

        #endregion
        #region Commands

        /// <summary>
        /// Chooses a lightweight command for nearby AI units.
        /// </summary>
        private void IssueCommand() {
            if (orderAttackWhenEnemyVisible && _officerUnit.FindClosestVisibleEnemy() != null) {
                _officerCommands.Attack();
                return;
            }

            if (gatherNearbyUnitsWhenIdle) {
                _officerCommands.FollowMe();
            }
        }

        /// <summary>
        /// Finds the runtime Unit component after UnitInit has created it.
        /// </summary>
        private void ResolveOfficerUnit() {
            if (_officerUnit == null) {
                _officerUnit = GetComponent<Unit>();
            }
        }

        #endregion
    }
}
