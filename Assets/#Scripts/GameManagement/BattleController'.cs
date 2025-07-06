using System;
using System.Collections.Generic;
using _Scripts.Units;
using UnityEngine;
using UnityEngine.UI;
using Unit = _Scripts.Units.Unit;

namespace _Scripts.GameManagement {
    public class BattleController : Singleton<BattleController> {
        private List<Unit> _teamPlayerUnits = new (), _teamAIUnits = new ();
        public Unit SelectedUnit { get; private set; }
        

        
        /// <summary>
        /// Selects a unit.
        /// </summary>
        /// <param name="unit"></param>
        public void SelectUnit(Unit unit) {
            if(SelectedUnit != null) {
                SelectedUnit.GetComponentInChildren<SelectedUnit>().DeselectUnit();
            }
            SelectedUnit = unit;
        }

        /// <summary>
        /// Clears the selected unit.
        /// </summary>
        public void ClearSelectedUnit() {
            if(SelectedUnit != null) {
                SelectedUnit.GetComponentInChildren<SelectedUnit>().DeselectUnit();
            }
            SelectedUnit = null;
        }
        
        /// <summary>
        /// Adds a unit to the battle controller.
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="team"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void AddUnit(Unit unit, Team team) {
            switch (team) {
                case Team.AI:
                    _teamAIUnits.Add(unit);
                    break;
                case Team.Player:
                    _teamPlayerUnits.Add(unit);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(team), team, null);
            }
        }
        
        /// <summary>
        /// Removes a unit from the battle controller.
        /// </summary>
        /// <param name="unit"></param>
        public void RemoveUnit(Unit unit) {
            if (_teamAIUnits.Contains(unit)) {
                _teamAIUnits.Remove(unit);
            }
            else if (_teamPlayerUnits.Contains(unit)) {
                _teamPlayerUnits.Remove(unit);
            }
        }


        


        /// <summary>
        /// Finds the closest target to the source unit.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="team"></param>
        /// <returns></returns>
        public Unit FindClosestTarget(Unit source, Team team) {
            
            var targets = team == Team.Player ? _teamAIUnits : _teamPlayerUnits;
            var closest = FindClosest(source, targets);
            if (closest != null) {
                return closest;
            }

            return null;
        }
        
        /// <summary>
        /// Gets the units of the opposing team for a unit.
        /// </summary>
        /// <param name="myTeam"></param>
        /// <returns></returns>
        public List<Unit> GetOpposingUnits(Team myTeam)
        {
            // If "myTeam" is Player, return the AI units; if "myTeam" is AI, return the Player units.
            return (myTeam == Team.Player) ? _teamAIUnits : _teamPlayerUnits;
        }


        /// <summary>
        /// Finds the closest unit to the source unit.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="targets"></param>
        /// <returns></returns>
        private Unit FindClosest(Unit source, List<Unit> targets)
        {
            Unit closest = null;
            var closestDist = Mathf.Infinity;

            foreach (Unit t in targets)
            {
                if (!t.IsAlive) continue;
                var dist = Vector2.Distance(source.transform.position, t.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = t;
                }
            }
            return closest;
        }

        private void Update()
        {
            // Check win/lose conditions
            var teamAAlive = _teamPlayerUnits.Exists(u => u.IsAlive);
            var teamBAlive = _teamAIUnits.Exists(u => u.IsAlive);

            if (!teamAAlive && teamBAlive)
            {
                Debug.Log("Team B Wins!");
            }
            else if (!teamBAlive && teamAAlive)
            {
                Debug.Log("Team A Wins!");
            }
            else if (!teamAAlive && !teamBAlive)
            {
                Debug.Log("Draw or Both Defeated!");
            }
#if UNITY_ANDROID
            // ----- ANDROID TOUCH INPUT -----
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                Vector3 tapPosition = Camera.main.ScreenToWorldPoint(Input.GetTouch(0).position);
                tapPosition.z = 0f; 
                Debug.Log("Android tap at world position: " + tapPosition);
            }
#else
            // Left-click check
            if (Input.GetMouseButtonDown(0))
            {
                var inputVector = UnityEngine.Camera.main.ScreenToWorldPoint(Input.mousePosition);
    
                if (SelectedUnit != null && SelectedUnit.IsAlive) {
                    SelectedUnit.SetDestination(inputVector);
                }
            }
#endif
            
        }
    }
}
