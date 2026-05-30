using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts.GameManagement {
    /// <summary>
    /// Provides UI-friendly scene loading helpers.
    /// </summary>
    public class LevelLoader : MonoBehaviour {

        #region Variables

        [Header("Level")]
        [SerializeField] private string defaultLevelName; // Scene name loaded by LoadDefaultLevel.

        #endregion
        #region Public Methods

        /// <summary>
        /// Loads the configured default level.
        /// </summary>
        public void LoadDefaultLevel() {
            LoadLevel(defaultLevelName);
        }

        /// <summary>
        /// Loads a level by scene name.
        /// </summary>
        /// <param name="levelName">The scene name to load.</param>
        public void LoadLevel(string levelName) {
            if (string.IsNullOrWhiteSpace(levelName)) {
                Debug.LogWarning("Cannot load level because no scene name was provided.");
                return;
            }

            SceneManager.LoadScene(levelName);
        }

        /// <summary>
        /// Loads a level by build index.
        /// </summary>
        /// <param name="buildIndex">The build index to load.</param>
        public void LoadLevel(int buildIndex) {
            SceneManager.LoadScene(buildIndex);
        }

        /// <summary>
        /// Reloads the currently active scene.
        /// </summary>
        public void ReloadCurrentLevel() {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>
        /// Loads the next numerically named level.
        /// </summary>
        public void LoadNextLevel() {
            var currentSceneName = SceneManager.GetActiveScene().name;
            if (!int.TryParse(currentSceneName, out var currentLevelNumber)) {
                Debug.LogWarning("Cannot load next level because the current scene name is not numeric: " + currentSceneName);
                return;
            }

            LoadLevel((currentLevelNumber + 1).ToString());
        }

        /// <summary>
        /// Quits the application.
        /// </summary>
        public void QuitGame() {
            Application.Quit();
        }

        #endregion
    }
}
