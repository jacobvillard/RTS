using System;
using System.Collections.Generic;
using _Scripts.Units;
using UnityEngine;
using UnityEngine.EventSystems;
using Unit = _Scripts.Units.Unit;

namespace _Scripts.GameManagement {
    /// <summary>
    /// Tracks active units, selection, battle start, and win/loss state.
    /// </summary>
    public class BattleController : Singleton<BattleController> {

        #region Variables

        private readonly List<Unit> _teamPlayerUnits = new(); // Player-owned units registered in the battle.
        private readonly List<Unit> _teamAIUnits = new();     // AI-owned units registered in the battle.
        private bool _battleResolved;                         // True once a win/loss/draw has been reported.
        private bool _battleStarted;                          // True once registered units have been released.

        public Unit SelectedUnit { get; private set; }
        public string winningTeam; 

        #endregion
        #region Unity Methods

        private void Update() {
            TryStartBattle();
            CheckWinLoseConditions();
            HandleSelectedUnitInput();
        }

        #endregion
        #region Selection

        /// <summary>
        /// Selects a unit and clears the previous unit highlight.
        /// </summary>
        /// <param name="unit">The unit to select.</param>
        public void SelectUnit(Unit unit) {
            if (SelectedUnit != null) {
                SelectedUnit.GetComponentInChildren<SelectedUnit>()?.DeselectUnit();
            }

            SelectedUnit = unit;
        }

        /// <summary>
        /// Clears the currently selected unit.
        /// </summary>
        public void ClearSelectedUnit() {
            if (SelectedUnit != null) {
                SelectedUnit.GetComponentInChildren<SelectedUnit>()?.DeselectUnit();
            }

            SelectedUnit = null;
        }

        /// <summary>
        /// Clears registered player units, usually after pre-game placement is reset.
        /// </summary>
        public void ClearAllPlayerUnits() {
            _teamPlayerUnits.Clear();
            ClearSelectedUnit();
        }

        #endregion
        #region Registration

        /// <summary>
        /// Adds a unit to the matching team list.
        /// </summary>
        /// <param name="unit">The unit being registered.</param>
        /// <param name="team">The unit's team.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an unknown team is passed.</exception>
        public void AddUnit(Unit unit, Team team) {
            if (unit == null) return;

            switch (team) {
                case Team.AI:
                    AddUniqueUnit(_teamAIUnits, unit);
                    break;
                case Team.Player:
                    AddUniqueUnit(_teamPlayerUnits, unit);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(team), team, null);
            }

            Debug.Log($"Registered {team} unit '{unit.name}'. Player units: {_teamPlayerUnits.Count}, AI units: {_teamAIUnits.Count}");
        }

        /// <summary>
        /// Removes a unit from whichever team owns it.
        /// </summary>
        /// <param name="unit">The unit to remove.</param>
        public void RemoveUnit(Unit unit) {
            _teamAIUnits.Remove(unit);
            _teamPlayerUnits.Remove(unit);
        }

        /// <summary>
        /// Adds a unit to a list once.
        /// </summary>
        /// <param name="units">The target team list.</param>
        /// <param name="unit">The unit to add.</param>
        private static void AddUniqueUnit(List<Unit> units, Unit unit) {
            if (!units.Contains(unit)) {
                units.Add(unit);
            }
        }

        #endregion
        #region Target Queries

        /// <summary>
        /// Finds the closest opposing unit to a source unit.
        /// </summary>
        /// <param name="source">The unit searching for a target.</param>
        /// <param name="team">The source unit's team.</param>
        /// <returns>The closest living opposing unit, or null.</returns>
        public Unit FindClosestTarget(Unit source, Team team) {
            var targets = team == Team.Player ? _teamAIUnits : _teamPlayerUnits;
            return FindClosest(source, targets);
        }

        /// <summary>
        /// Gets all registered opposing units for a team.
        /// </summary>
        /// <param name="myTeam">The querying unit's team.</param>
        /// <returns>The opposing team list.</returns>
        public List<Unit> GetOpposingUnits(Team myTeam) {
            return myTeam == Team.Player ? _teamAIUnits : _teamPlayerUnits;
        }

        /// <summary>
        /// Gets all registered friendly units for a team.
        /// </summary>
        /// <param name="team">The team being queried.</param>
        /// <returns>The friendly team list.</returns>
        public List<Unit> GetFriendlyUnits(Team team) {
            return team == Team.Player ? _teamPlayerUnits : _teamAIUnits;
        }

        /// <summary>
        /// Finds the closest living unit from a candidate list.
        /// </summary>
        /// <param name="source">The source unit.</param>
        /// <param name="targets">Candidate targets.</param>
        /// <returns>The closest living target, or null.</returns>
        private static Unit FindClosest(Unit source, List<Unit> targets) {
            Unit closest = null;
            var closestDist = Mathf.Infinity;

            foreach (var target in targets) {
                if (target == null || !target.IsAlive) continue;

                var dist = Vector2.Distance(source.transform.position, target.transform.position);
                if (!(dist < closestDist)) continue;

                closestDist = dist;
                closest = target;
            }

            return closest;
        }

        #endregion
        #region Battle State

        /// <summary>
        /// Runs once when the game enters active play.
        /// </summary>
        private void TryStartBattle() {
            if (_battleStarted) return;
            if (GameManager.Instance == null || GameManager.Instance.GameState != GameState.Playing) return;

            _battleStarted = true;
            Debug.Log($"Battle started. Player units: {_teamPlayerUnits.Count}, AI units: {_teamAIUnits.Count}");
        }

        /// <summary>
        /// Checks for win, loss, or draw while the game is playing.
        /// </summary>
        private void CheckWinLoseConditions() {
            if (_battleResolved) return;
            if (GameManager.Instance == null || GameManager.Instance.GameState != GameState.Playing) return;

            RemoveMissingUnits();

            var teamAAlive = _teamPlayerUnits.Exists(unit => unit != null && unit.IsAlive);
            var teamBAlive = _teamAIUnits.Exists(unit => unit != null && unit.IsAlive);

            if (!teamAAlive && teamBAlive) {
                ResolveBattle($"Team B Wins! Registered player units: {_teamPlayerUnits.Count}, AI units: {_teamAIUnits.Count}");
                winningTeam = "AI";
            }
            else if (!teamBAlive && teamAAlive) {
                ResolveBattle($"Team A Wins! Registered player units: {_teamPlayerUnits.Count}, AI units: {_teamAIUnits.Count}");
                winningTeam = "Player";
            }
            else if (!teamAAlive && !teamBAlive) {
                ResolveBattle($"Draw or Both Defeated! Registered player units: {_teamPlayerUnits.Count}, AI units: {_teamAIUnits.Count}");
                winningTeam = "Draw";
            }
        }

        /// <summary>
        /// Marks the battle as complete and informs the game manager.
        /// </summary>
        /// <param name="message">The debug result message.</param>
        private void ResolveBattle(string message) {
            _battleResolved = true;
            Debug.Log(message);
            GameManager.Instance.EndGame();
        }

        /// <summary>
        /// Removes destroyed units from both registered team lists.
        /// </summary>
        private void RemoveMissingUnits() {
            _teamPlayerUnits.RemoveAll(unit => unit == null);
            _teamAIUnits.RemoveAll(unit => unit == null);
        }

        #endregion
        #region Input

        /// <summary>
        /// Handles tap/click commands for the currently selected unit.
        /// </summary>
        private void HandleSelectedUnitInput() {
            if (GameManager.Instance != null && GameManager.Instance.IsPreGame()) return;
            if (SelectedUnit == null || !SelectedUnit.IsAlive) return;
            if (!TryGetCommandScreenPosition(out var screenPosition)) return;

            var mainCamera = UnityEngine.Camera.main;
            if (mainCamera == null) return;

            SelectedUnit.SetDestination(GetWorldPosition(mainCamera, screenPosition));
        }

        /// <summary>
        /// Gets the mouse or first-touch command position while ignoring UI.
        /// </summary>
        /// <param name="screenPosition">The valid command screen position.</param>
        /// <returns>True when a command pointer began this frame.</returns>
        private static bool TryGetCommandScreenPosition(out Vector2 screenPosition) {
#if UNITY_EDITOR || UNITY_STANDALONE
            screenPosition = Input.mousePosition;
            if (!Input.GetMouseButtonDown(0)) return false;
            if (IsPointerOverUi()) return false;

            return true;
#elif UNITY_ANDROID || UNITY_IOS
            screenPosition = default;
            if (Input.touchCount <= 0) return false;

            var touch = Input.GetTouch(0);
            if (touch.phase != TouchPhase.Began) return false;
            if (IsPointerOverUi(touch.fingerId)) return false;

            screenPosition = touch.position;
            return true;
#else
            screenPosition = Input.mousePosition;
            if (!Input.GetMouseButtonDown(0)) return false;
            if (IsPointerOverUi()) return false;

            return true;
#endif
        }

        /// <summary>
        /// Converts a screen position into the 2D battle plane.
        /// </summary>
        /// <param name="mainCamera">The camera used for projection.</param>
        /// <param name="screenPosition">The pointer position on screen.</param>
        /// <returns>The target world position.</returns>
        private static Vector3 GetWorldPosition(UnityEngine.Camera mainCamera, Vector2 screenPosition) {
            var pointerPosition = new Vector3(screenPosition.x, screenPosition.y, -mainCamera.transform.position.z);
            var worldPosition = mainCamera.ScreenToWorldPoint(pointerPosition);
            worldPosition.z = 0f;
            return worldPosition;
        }

        /// <summary>
        /// Checks whether the pointer began over a UI element.
        /// </summary>
        /// <param name="pointerId">The touch pointer id, or -1 for mouse.</param>
        /// <returns>True when the pointer is over Unity UI.</returns>
        private static bool IsPointerOverUi(int pointerId = -1) {
            if (EventSystem.current == null) return false;

            return pointerId >= 0
                ? EventSystem.current.IsPointerOverGameObject(pointerId)
                : EventSystem.current.IsPointerOverGameObject();
        }

        #endregion
    }
}
