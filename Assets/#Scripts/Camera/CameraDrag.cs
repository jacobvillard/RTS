using System;
using UnityEngine;

namespace _Scripts.Camera {
    /// <summary>
    /// This script allows the camera to be dragged around the scene.
    /// </summary>
    public class CameraDrag : MonoBehaviour {
        [SerializeField] private float dragSpeed = 2;
        [SerializeField] private Vector2 minLimits; // Minimum X and Y position
        [SerializeField] private Vector2 maxLimits; // Maximum X and Y position
        [SerializeField] private float zoomSpeed = 0.1f; // Speed of zooming
        [SerializeField] private float minZoom = 5f; // Minimum camera size for zooming
        [SerializeField] private float maxZoom = 20f; // Maximum camera size for zooming
        
        private Vector3 _dragOrigin;
        private UnityEngine.Camera _camera;
        private float _actualDragSpeed = 0;
        
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
        
        /// <summary>
        /// Handles mouse input for camera dragging (for Unity Editor or standalone builds).
        /// </summary>
        private void HandleMouseInput() {
            if (Input.GetMouseButtonDown(0)) {
                _dragOrigin = Input.mousePosition; // Save the starting position
            }

            if (Input.GetMouseButton(0)) {
                var currentPosition = Input.mousePosition;
                var difference = _dragOrigin - currentPosition; // Calculate difference

                _actualDragSpeed = dragSpeed * (_camera.orthographicSize / 5); // Adjust drag speed based on zoom level
                
                if (difference != Vector3.zero) { // Only move if there is a difference
                    var move = new Vector3(difference.x * _actualDragSpeed * Time.deltaTime, difference.y * _actualDragSpeed * Time.deltaTime, 0);
                    transform.Translate(move, Space.World);
                }

                _dragOrigin = currentPosition; // Update the previous position
            }
        }

        /// <summary>
        /// Handles touch input for camera dragging (on mobile devices).
        /// </summary>
        private void HandleTouchInput() {
            if (Input.touchCount == 1) { // Only handle single touch input
                var touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Began) {
                    _dragOrigin = touch.position; // Save the starting position
                } else if (touch.phase == TouchPhase.Moved) {
                    var currentPosition = touch.position;
                    var difference = (Vector3)_dragOrigin - (Vector3)currentPosition; // Calculate difference
                    
                    _actualDragSpeed = dragSpeed * (_camera.orthographicSize / 5); // Adjust drag speed based on zoom level

                    if (difference != Vector3.zero) { // Only move if there is a difference
                        var move = new Vector3(difference.x * _actualDragSpeed * Time.deltaTime, difference.y * _actualDragSpeed * Time.deltaTime, 0);
                        transform.Translate(move, Space.World);
                    }

                    _dragOrigin = currentPosition; // Update the previous position
                }
            }
        }
        
        /// <summary>
        /// Clamps the camera's position within defined X and Y bounds.
        /// </summary>
        private void ClampCameraPosition() {
            var clampedX = Mathf.Clamp(transform.position.x, minLimits.x, maxLimits.x);
            var clampedY = Mathf.Clamp(transform.position.y, minLimits.y, maxLimits.y);
            transform.position = new Vector3(clampedX, clampedY, transform.position.z);
        }

        /// <summary>
        /// Handles mouse scroll wheel zooming for standalone platforms.
        /// </summary>
        private void HandleScrollZoom() {
            if (_camera.orthographic) {
                var scroll = Input.GetAxis("Mouse ScrollWheel");
                _camera.orthographicSize -= scroll * zoomSpeed * 100; // Adjust zoom speed
                _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize, minZoom, maxZoom); // Clamp zoom
            }
        }

        /// <summary>
        /// Handles pinch zooming for mobile devices.
        /// </summary>
        private void HandlePinchZoom() {
            if (Input.touchCount == 2) {
                var touch0 = Input.GetTouch(0);
                var touch1 = Input.GetTouch(1);

                // Calculate the difference in positions between the current and previous frames
                var prevTouchDelta = (touch0.position - touch0.deltaPosition) - (touch1.position - touch1.deltaPosition);
                var currentTouchDelta = touch0.position - touch1.position;

                // Calculate the difference in distances
                var deltaMagnitudeDiff = prevTouchDelta.magnitude - currentTouchDelta.magnitude;

                // Adjust the orthographic size
                _camera.orthographicSize += deltaMagnitudeDiff * zoomSpeed;
                _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize, minZoom, maxZoom); // Clamp zoom
            }
        }


    }
}
