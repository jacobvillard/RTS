using UnityEngine;

namespace _Scripts.GameManagement {
    /// <summary>
    /// Lightweight MonoBehaviour singleton base for scene-level managers.
    /// </summary>
    /// <typeparam name="T">The component type that owns the singleton instance.</typeparam>
    public abstract class Singleton<T> : MonoBehaviour where T : Component {

        #region Variables

        public static T Instance; // Globally accessible manager instance.

        #endregion
        #region Unity Methods

        protected virtual void Awake() {
            if (Instance != null && Instance != this) {
                Debug.LogWarning($"More than one instance of {typeof(T).Name} found.");
                return;
            }

            Instance = this as T;
        }

        #endregion
    }
}
