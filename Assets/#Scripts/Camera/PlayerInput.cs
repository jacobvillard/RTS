using System;
using _Scripts.GameManagement;
using UnityEngine;
using UnityEngine.AI;

namespace _Scripts.Camera {
    public class PlayerInput : MonoBehaviour
    {
        



        void Update()
        {
            // Left-click check
            if (Input.GetMouseButtonDown(0))
            {
                // Create a ray from the Camera through the mouse position
                var ray = UnityEngine.Camera.main!.ScreenPointToRay(Input.mousePosition);
                RaycastHit hitInfo;

                // Perform a raycast against the ground/walkable layer
                if (Physics.Raycast(ray, out hitInfo, 1000f))
                {
                    // Check if the clicked point is on the NavMesh
                    NavMeshHit navHit;
                    if (NavMesh.SamplePosition(hitInfo.point, out navHit, 1.0f, NavMesh.AllAreas))
                    {
                        // If we found a valid position on the NavMesh,
                        // and we have a selected unit, then set its destination.
                        var selectedUnit = BattleController.Instance.SelectedUnit;
                        if (selectedUnit != null && selectedUnit.IsAlive)
                        {
                            // Move the unit
                            selectedUnit.SetDestination(navHit.position);
                        }
                    }
                }
            }
        }
    }
}
