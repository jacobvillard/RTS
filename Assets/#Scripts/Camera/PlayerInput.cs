using _Scripts.GameManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

namespace _Scripts.Camera {
    /// <summary>
    /// Converts player clicks into NavMesh movement commands for selected units.
    /// </summary>
    public class PlayerInput : MonoBehaviour {

        #region Variables

        private const float MaxRayDistance = 1000f; // Maximum click raycast distance.
        private const float NavMeshSampleRadius = 1f; // Radius used to find nearby NavMesh positions.

        #endregion
        #region Unity Methods

        private void Update() {
            if (!Input.GetMouseButtonDown(0)) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            TryMoveSelectedUnit();
        }

        #endregion
        #region Movement

        /// <summary>
        /// Attempts to move the currently selected unit to the clicked NavMesh point.
        /// </summary>
        private void TryMoveSelectedUnit() {
            var mainCamera = UnityEngine.Camera.main;
            if (mainCamera == null) return;

            var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hitInfo, MaxRayDistance)) return;
            if (!NavMesh.SamplePosition(hitInfo.point, out var navHit, NavMeshSampleRadius, NavMesh.AllAreas)) return;

            var selectedUnit = BattleController.Instance.SelectedUnit;
            if (selectedUnit != null && selectedUnit.IsAlive) {
                selectedUnit.SetDestination(navHit.position);
            }
        }

        #endregion
    }
}
