using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts.GameManagement {
    /// <summary>
    /// Routes the first level scene to the highest played numeric level once per app launch.
    /// </summary>
    public class ApplicationLaunchLevelRouter : MonoBehaviour {

        #region Variables

        [Header("Launch Routing")]
        [SerializeField] private bool routeOnlyFromLevelOne = true; // Prevents redirects unless the active scene is named 1.

        private static bool _hasRoutedThisLaunch; // Ensures routing happens only once per application launch.

        #endregion
        #region Unity Methods

        private void Start() {
            RouteOnApplicationLaunch();
        }

        #endregion
        #region Routing

        /// <summary>
        /// Sends the player to the highest played level when the app first opens on level one.
        /// </summary>
        private void RouteOnApplicationLaunch() {
            if (_hasRoutedThisLaunch) return;

            _hasRoutedThisLaunch = true;

            var activeSceneName = SceneManager.GetActiveScene().name;
            if (routeOnlyFromLevelOne && activeSceneName != "1") return;

            var targetLevel = PersistentGameSettings.HighestLevelPlayed;
            if (targetLevel <= 1) return;

            var targetSceneName = targetLevel.ToString();
            if (targetSceneName == activeSceneName) return;

            SceneManager.LoadScene(targetSceneName);
        }

        #endregion
    }
}
