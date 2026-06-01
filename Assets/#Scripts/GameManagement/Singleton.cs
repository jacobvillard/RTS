using UnityEngine;

namespace _Scripts.GameManagement {
    /// <summary>
    /// Lightweight MonoBehaviour singleton base for managers that survive scene loads.
    /// </summary>
    /// <typeparam name="T">The component type that owns the singleton instance.</typeparam>
    public abstract class Singleton<T> : MonoBehaviour where T : Component {

        #region Variables

        public static T Instance; // Globally accessible manager instance.
        protected bool IsDuplicateInstance { get; private set; } // True when this object is a duplicate being destroyed.

        #endregion
        #region Unity Methods

        protected virtual void Awake() {
            if (Instance != null && Instance != this) {
                IsDuplicateInstance = true;
                Debug.LogWarning($"More than one instance of {typeof(T).Name} found. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            Instance = this as T;
            DontDestroyOnLoad(gameObject);
        }

        protected virtual void OnDestroy() {
            if (Instance == this) {
                Instance = null;
            }
        }

        #endregion
    }
}
