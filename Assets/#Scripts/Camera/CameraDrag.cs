using UnityEngine;

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

        private Vector3 _dragOrigin;             // Previous pointer position during drag.
        private UnityEngine.Camera _camera;      // Main camera being controlled.
        private float _actualDragSpeed;          // Drag speed adjusted by zoom level.

        #endregion
        #region Unity Methods

        private void Start() {
            _camera = UnityEngine.Camera.main;
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
        #region Drag
        
        /// <summary>
        /// Handles mouse drag input for editor and desktop builds.
        /// </summary>
        private void HandleMouseInput() {
            if (Input.GetMouseButtonDown(0)) {
                _dragOrigin = Input.mousePosition;
            }

            if (!Input.GetMouseButton(0)) return;

            DragTo(Input.mousePosition);
        }

        /// <summary>
        /// Handles single-touch drag input for mobile builds.
        /// </summary>
        private void HandleTouchInput() {
            if (Input.touchCount != 1) return;

            var touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began) {
                _dragOrigin = touch.position;
            }
            else if (touch.phase == TouchPhase.Moved) {
                DragTo(touch.position);
            }
        }

        /// <summary>
        /// Moves the camera based on pointer movement.
        /// </summary>
        /// <param name="currentPosition">The current pointer position.</param>
        private void DragTo(Vector3 currentPosition) {
            if (_camera == null) return;

            var difference = _dragOrigin - currentPosition;
            _actualDragSpeed = dragSpeed * (_camera.orthographicSize / 5);

            if (difference != Vector3.zero) {
                var move = new Vector3(
                    difference.x * _actualDragSpeed * Time.unscaledDeltaTime,
                    difference.y * _actualDragSpeed * Time.unscaledDeltaTime,
                    0);
                transform.Translate(move, Space.World);
            }

            _dragOrigin = currentPosition;
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
