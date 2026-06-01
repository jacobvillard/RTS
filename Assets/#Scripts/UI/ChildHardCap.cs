using UnityEngine;

namespace _Scripts.UI {
    /// <summary>
    /// Keeps a UI/container transform from showing more than a configured number of child objects.
    /// </summary>
    public class ChildHardCap : MonoBehaviour {

        #region Variables

        [Header("Child Cap")]
        [SerializeField] private int maxActiveChildren = 20; // Maximum child objects allowed to stay active.

        [Header("Behaviour")]
        [SerializeField] private bool includeInactiveChildren = true; // Counts inactive children when deciding the cap.

        #endregion
        #region Unity Methods

        private void Awake() {
            ApplyCap();
        }

        private void OnEnable() {
            ApplyCap();
        }

        private void OnTransformChildrenChanged() {
            ApplyCap();
        }

        private void OnValidate() {
            maxActiveChildren = Mathf.Max(0, maxActiveChildren);
            ApplyCap();
        }

        #endregion
        #region Public Methods

        /// <summary>
        /// Applies the configured child cap immediately.
        /// </summary>
        public void ApplyCap() {
            var countedChildren = 0;

            for (var i = 0; i < transform.childCount; i++) {
                var child = transform.GetChild(i);
                if (child == null) continue;

                if (!includeInactiveChildren && !child.gameObject.activeSelf) continue;

                var shouldBeActive = countedChildren < maxActiveChildren;
                if (child.gameObject.activeSelf != shouldBeActive) {
                    child.gameObject.SetActive(shouldBeActive);
                }

                countedChildren++;
            }
        }

        #endregion
    }
}
