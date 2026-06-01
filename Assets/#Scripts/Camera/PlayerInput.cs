using _Scripts.GameManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _Scripts.Camera {
    /// <summary>
    /// Converts player clicks into NavMesh movement commands for selected units.
    /// </summary>
    public class PlayerInput : MonoBehaviour {

        #region Variables

        private const float CommandTapMovementThreshold = 20f; // Maximum touch movement still treated as a command tap.

        private Vector2 _commandPointerStartScreenPosition; // Pointer position where a movement command tap began.
        private bool _isCommandPointerPressActive;          // True while waiting to confirm a movement command tap.

        #endregion
        #region Unity Methods

        private void Update() {
            if (GameManager.Instance != null && GameManager.Instance.IsPreGame()) return;
            if (!TryGetCommandScreenPosition(out var screenPosition)) return;

            TryMoveSelectedUnit(screenPosition);
        }

        #endregion
        #region Movement

        /// <summary>
        /// Attempts to move the currently selected unit to the clicked NavMesh point.
        /// </summary>
        /// <param name="screenPosition">The screen position used for the command.</param>
        private void TryMoveSelectedUnit(Vector2 screenPosition) {
            var selectedUnit = BattleController.Instance != null ? BattleController.Instance.SelectedUnit : null;
            if (selectedUnit == null || !selectedUnit.IsAlive) return;

            var mainCamera = UnityEngine.Camera.main;
            if (mainCamera == null) return;

            selectedUnit.SetDestination(GetWorldPosition(mainCamera, screenPosition));
        }

        /// <summary>
        /// Gets the mouse or first-touch command position while ignoring UI.
        /// </summary>
        /// <param name="screenPosition">The valid command screen position.</param>
        /// <returns>True when a command pointer began this frame.</returns>
        private bool TryGetCommandScreenPosition(out Vector2 screenPosition) {
#if UNITY_EDITOR || UNITY_STANDALONE
            screenPosition = Input.mousePosition;
            if (!Input.GetMouseButtonDown(0)) return false;
            if (IsPointerOverUi()) return false;

            return true;
#elif UNITY_ANDROID || UNITY_IOS
            return TryGetTouchCommandTap(out screenPosition);
#else
            screenPosition = Input.mousePosition;
            if (!Input.GetMouseButtonDown(0)) return false;
            if (IsPointerOverUi()) return false;

            return true;
#endif
        }

        /// <summary>
        /// Gets a completed touch tap for movement commands while rejecting UI touches and camera drags.
        /// </summary>
        /// <param name="screenPosition">The completed tap position.</param>
        /// <returns>True when a movement command tap ended this frame.</returns>
        private bool TryGetTouchCommandTap(out Vector2 screenPosition) {
            screenPosition = default;

            if (Input.touchCount != 1) {
                _isCommandPointerPressActive = false;
                return false;
            }

            var touch = Input.GetTouch(0);
            switch (touch.phase) {
                case TouchPhase.Began:
                    _isCommandPointerPressActive = !IsPointerOverUi(touch.fingerId);
                    _commandPointerStartScreenPosition = touch.position;
                    return false;
                case TouchPhase.Ended:
                    if (!_isCommandPointerPressActive) return false;

                    _isCommandPointerPressActive = false;
                    screenPosition = touch.position;
                    return Vector2.Distance(_commandPointerStartScreenPosition, screenPosition) <= CommandTapMovementThreshold;
                case TouchPhase.Canceled:
                    _isCommandPointerPressActive = false;
                    return false;
                default:
                    return false;
            }
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
