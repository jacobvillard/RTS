using UnityEngine;
using UnityEngine.EventSystems;

namespace _Scripts.Camera {
    /// <summary>
    /// Allows the camera to pan and zoom around the battle scene.
    /// </summary>
    public class CameraDrag : MonoBehaviour {

        #region Variables

        [Header("Drag")]
        [SerializeField] private float dragSpeed = 2; // Base camera drag speed.
        [SerializeField] private Vector2 minLimits;   // Minimum world position.
        [SerializeField] private Vector2 maxLimits;   // Maximum world position.

        [Header("Zoom")]
        [SerializeField] private float zoomSpeed = 0.1f; // Zoom speed multiplier.
        [SerializeField] private float minZoom = 5f;     // Minimum orthographic size.
        [SerializeField] private float maxZoom = 20f;    // Maximum orthographic size.

        private Vector3 _dragOrigin;             // Previous world position under the pointer during drag.
        private UnityEngine.Camera _camera;      // Main camera being controlled.
        private bool _isDragging;                // True while a non-UI pointer is dragging the camera.
        private float _baseDragSpeed;            // Original drag speed for reference.

        #endregion
        #region Unity Methods

        private void Start() {
            _camera = UnityEngine.Camera.main;
            _baseDragSpeed = dragSpeed;
        }

        private void Update() {
#if UNITY_EDITOR || UNITY_STANDALONE
            HandleMouseInput();
            HandleScrollZoom();
#elif UNITY_ANDROID || UNITY_IOS
            HandleTouchInput();
            HandlePinchZoom();
#endif
            ClampCameraPosition();
        }

        #endregion

        #region Public Methods

        public void EnterOptions() {
            dragSpeed = 0;
        }
        
        public void ExitOptions() {
            dragSpeed = _baseDragSpeed;
        }
        

        #endregion
        #region Drag
        
        /// <summary>
        /// Handles mouse drag input for editor and desktop builds.
        /// </summary>
        private void HandleMouseInput() {
            if (Input.GetMouseButtonDown(0)) {
                BeginDrag(Input.mousePosition);
            }

            if (Input.GetMouseButtonUp(0)) {
                _isDragging = false;
                return;
            }

            if (!Input.GetMouseButton(0) || !_isDragging) return;

            DragTo(Input.mousePosition);
        }

        /// <summary>
        /// Handles single-touch drag input for mobile builds.
        /// </summary>
        private void HandleTouchInput() {
            if (Input.touchCount != 1) {
                _isDragging = false;
                return;
            }

            var touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began) {
                BeginDrag(touch.position, touch.fingerId);
            }
            else if (touch.phase == TouchPhase.Moved) {
                DragTo(touch.position);
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) {
                _isDragging = false;
            }
        }

        /// <summary>
        /// Starts camera dragging when the pointer begins away from UI.
        /// </summary>
        /// <param name="screenPosition">The pointer position on screen.</param>
        /// <param name="pointerId">The touch pointer id, or -1 for mouse.</param>
        private void BeginDrag(Vector2 screenPosition, int pointerId = -1) {
            if (_camera == null || IsPointerOverUi(pointerId)) {
                _isDragging = false;
                return;
            }

            _dragOrigin = GetPointerWorldPosition(screenPosition);
            _isDragging = true;
        }

        /// <summary>
        /// Moves the camera based on pointer movement.
        /// </summary>
        /// <param name="currentPosition">The current pointer screen position.</param>
        private void DragTo(Vector2 currentPosition) {
            if (_camera == null || !_isDragging || dragSpeed <= 0f) return;

            var currentWorldPosition = GetPointerWorldPosition(currentPosition);
            var difference = _dragOrigin - currentWorldPosition;

            if (difference != Vector3.zero) {
                var dragSensitivity = dragSpeed / Mathf.Max(_baseDragSpeed, 0.01f);
                transform.Translate(difference * dragSensitivity, Space.World);
            }

            _dragOrigin = GetPointerWorldPosition(currentPosition);
        }

        /// <summary>
        /// Converts a pointer screen position to the battle plane in world space.
        /// </summary>
        /// <param name="screenPosition">The pointer position on screen.</param>
        /// <returns>The world position under the pointer.</returns>
        private Vector3 GetPointerWorldPosition(Vector2 screenPosition) {
            var pointerPosition = new Vector3(screenPosition.x, screenPosition.y, -_camera.transform.position.z);
            var worldPosition = _camera.ScreenToWorldPoint(pointerPosition);
            worldPosition.z = 0f;
            return worldPosition;
        }

        /// <summary>
        /// Checks whether the pointer began over a UI element.
        /// </summary>
        /// <param name="pointerId">The touch pointer id, or -1 for mouse.</param>
        /// <returns>True when the pointer is over Unity UI.</returns>
        private static bool IsPointerOverUi(int pointerId) {
            if (EventSystem.current == null) return false;

            return pointerId >= 0
                ? EventSystem.current.IsPointerOverGameObject(pointerId)
                : EventSystem.current.IsPointerOverGameObject();
        }

        /// <summary>
        /// Keeps the camera inside the configured world bounds.
        /// </summary>
        private void ClampCameraPosition() {
            var clampedX = Mathf.Clamp(transform.position.x, minLimits.x, maxLimits.x);
            var clampedY = Mathf.Clamp(transform.position.y, minLimits.y, maxLimits.y);
            transform.position = new Vector3(clampedX, clampedY, transform.position.z);
        }

        #endregion
        #region Zoom

        /// <summary>
        /// Handles mouse scroll wheel zooming.
        /// </summary>
        private void HandleScrollZoom() {
            if (_camera == null || !_camera.orthographic) return;

            var scroll = Input.GetAxis("Mouse ScrollWheel");
            _camera.orthographicSize -= scroll * zoomSpeed * 100;
            ClampZoom();
        }

        /// <summary>
        /// Handles mobile pinch zooming.
        /// </summary>
        private void HandlePinchZoom() {
            if (_camera == null || Input.touchCount != 2) return;

            var touch0 = Input.GetTouch(0);
            var touch1 = Input.GetTouch(1);
            var prevTouchDelta = (touch0.position - touch0.deltaPosition) - (touch1.position - touch1.deltaPosition);
            var currentTouchDelta = touch0.position - touch1.position;
            var deltaMagnitudeDiff = prevTouchDelta.magnitude - currentTouchDelta.magnitude;

            _camera.orthographicSize += deltaMagnitudeDiff * zoomSpeed;
            ClampZoom();
        }

        /// <summary>
        /// Keeps the camera zoom inside the configured limits.
        /// </summary>
        private void ClampZoom() {
            _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize, minZoom, maxZoom);
        }

        #endregion
    }
}
