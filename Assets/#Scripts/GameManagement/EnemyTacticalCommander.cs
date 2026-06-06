using _Scripts.Buildings;
using _Scripts.Units;
using UnityEngine;

namespace _Scripts.GameManagement {
    /// <summary>
    /// Gives AI units simple low-frequency tactical orders without heavy per-frame processing.
    /// </summary>
    public class EnemyTacticalCommander : MonoBehaviour {

        #region Variables

        [Header("Timing")]
        [SerializeField] private float decisionInterval = 1.25f; // Seconds between tactical decisions.

        [Header("Priorities")]
        [SerializeField] private bool captureBuildings = true; // Sends AI units to uncaptured/player buildings.
        [SerializeField] private bool takeCannons = true;      // Sends infantry-style units toward usable cannons.
        [SerializeField] private bool holdChokePoints = true;  // Sends spare units to configured choke points.

        [Header("Choke Points")]
        [SerializeField] private Transform[] chokePoints; // Optional defensive points for AI to hold.

        private float _nextDecisionTime; // Next allowed decision time.
        private int _nextChokeIndex;     // Choke point rotation index.

        #endregion
        #region Unity Methods

        private void Update() {
            if (Time.time < _nextDecisionTime) return;
            if (GameManager.Instance != null && GameManager.Instance.IsPreGame()) return;

            _nextDecisionTime = Time.time + decisionInterval;
            MakeDecision();
        }

        #endregion
        #region Decisions

        /// <summary>
        /// Runs one cheap tactical decision in priority order.
        /// </summary>
        private void MakeDecision() {
            if (BattleController.Instance == null) return;

            if (captureBuildings && TryOrderBuildingCapture()) return;
            if (takeCannons && TryOrderCannonCapture()) return;
            if (holdChokePoints) {
                TryOrderChokePointHold();
            }
        }

        /// <summary>
        /// Orders the nearest AI unit to capture a useful building.
        /// </summary>
        /// <returns>True when an order was issued.</returns>
        private bool TryOrderBuildingCapture() {
            var buildings = FindObjectsOfType<CapturableBuilding>();
            CapturableBuilding targetBuilding = null;
            Unit assignedUnit = null;
            var bestDistance = Mathf.Infinity;

            foreach (var building in buildings) {
                if (building == null) continue;
                if (building.TryGetOwnerTeam(out var ownerTeam) && ownerTeam == Team.AI) continue;

                var unit = FindNearestAiUnit(building.transform.position);
                if (unit == null) continue;

                var distance = Vector2.Distance(unit.transform.position, building.transform.position);
                if (distance >= bestDistance) continue;

                bestDistance = distance;
                targetBuilding = building;
                assignedUnit = unit;
            }

            if (targetBuilding == null || assignedUnit == null) return false;

            assignedUnit.CommandAdvance(targetBuilding.transform.position);
            return true;
        }

        /// <summary>
        /// Orders nearby AI infantry-style units toward cannons so they can man them.
        /// </summary>
        /// <returns>True when an order was issued.</returns>
        private bool TryOrderCannonCapture() {
            var cannons = FindObjectsOfType<CannonEmplacement>();
            CannonEmplacement targetCannon = null;
            Unit assignedUnit = null;
            var bestDistance = Mathf.Infinity;

            foreach (var cannon in cannons) {
                if (cannon == null) continue;

                var unit = FindNearestAiInfantry(cannon.transform.position);
                if (unit == null) continue;

                var distance = Vector2.Distance(unit.transform.position, cannon.transform.position);
                if (distance >= bestDistance) continue;

                bestDistance = distance;
                targetCannon = cannon;
                assignedUnit = unit;
            }

            if (targetCannon == null || assignedUnit == null) return false;

            assignedUnit.CommandAdvance(targetCannon.transform.position);
            return true;
        }

        /// <summary>
        /// Orders one spare AI unit to hold a configured choke point.
        /// </summary>
        /// <returns>True when an order was issued.</returns>
        private bool TryOrderChokePointHold() {
            if (chokePoints == null || chokePoints.Length == 0) return false;

            var chokePoint = GetNextValidChokePoint();
            if (chokePoint == null) return false;

            var unit = FindNearestAiUnit(chokePoint.position);
            if (unit == null) return false;

            unit.CommandAdvance(chokePoint.position);
            return true;
        }

        #endregion
        #region Queries

        /// <summary>
        /// Finds the nearest living AI unit to a point.
        /// </summary>
        /// <param name="position">Position to search around.</param>
        /// <returns>The nearest AI unit, or null.</returns>
        private static Unit FindNearestAiUnit(Vector3 position) {
            if (BattleController.Instance == null) return null;

            Unit closest = null;
            var closestDistance = Mathf.Infinity;
            var units = BattleController.Instance.GetFriendlyUnits(Team.AI);

            foreach (var unit in units) {
                if (unit == null || !unit.IsAlive) continue;

                var distance = Vector2.Distance(position, unit.transform.position);
                if (distance >= closestDistance) continue;

                closestDistance = distance;
                closest = unit;
            }

            return closest;
        }

        /// <summary>
        /// Finds the nearest AI unit that can crew a cannon.
        /// </summary>
        /// <param name="position">Position to search around.</param>
        /// <returns>The nearest infantry-style AI unit, or null.</returns>
        private static Unit FindNearestAiInfantry(Vector3 position) {
            if (BattleController.Instance == null) return null;

            Unit closest = null;
            var closestDistance = Mathf.Infinity;
            var units = BattleController.Instance.GetFriendlyUnits(Team.AI);

            foreach (var unit in units) {
                if (unit == null || !unit.IsAlive) continue;
                if (!IsInfantryStyleUnit(unit)) continue;

                var distance = Vector2.Distance(position, unit.transform.position);
                if (distance >= closestDistance) continue;

                closestDistance = distance;
                closest = unit;
            }

            return closest;
        }

        /// <summary>
        /// Checks whether a unit can be used for infantry-style tactical jobs.
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
        /// Gets the next assigned choke point, skipping missing references.
        /// </summary>
        /// <returns>A valid choke point, or null.</returns>
        private Transform GetNextValidChokePoint() {
            for (var i = 0; i < chokePoints.Length; i++) {
                var index = (_nextChokeIndex + i) % chokePoints.Length;
                var chokePoint = chokePoints[index];
                if (chokePoint == null) continue;

                _nextChokeIndex = (index + 1) % chokePoints.Length;
                return chokePoint;
            }

            return null;
        }

        #endregion
    }
}
