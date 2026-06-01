using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts.GameManagement {
    /// <summary>
    /// Applies scene-load cleanup for persistent managers and scene UI.
    /// </summary>
    public class SceneLoadInitializer : MonoBehaviour {

        #region Variables

        [Header("Scene UI To Close")]
        [SerializeField] private List<GameObject> uiObjectsToClose = new(); // UI roots hidden whenever a scene loads.

        [Header("Scene UI To Enable")]
        [SerializeField] private List<GameObject> uiObjectsToEnable = new(); // UI roots shown whenever a scene loads.

        [Header("Refresh")]
        [SerializeField] private bool refreshMoney = true;      // Refreshes level money from the level settings database.
        [SerializeField] private bool refreshUnitPlacer = true; // Rebinds UnitPlacer UI and resets placement money.
        [SerializeField] private bool refreshGameManager = true; // Rebinds GameManager scene state and placement areas.
        [SerializeField] private bool refreshLevelSelector = true; // Refreshes level selector unlock state from the save file.
        [SerializeField] private bool markNumericScenePlayed = true; // Saves numeric level scenes as played.

        private bool _hasInitializedActiveScene; // Prevents double-running on the scene that created this object.

        #endregion
        #region Unity Methods

        private void OnEnable() {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void Start() {
            if (_hasInitializedActiveScene) return;

            InitializeLoadedScene();
        }

        private void OnDisable() {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        #endregion
        #region Scene Load

        /// <summary>
        /// Runs cleanup after Unity loads a scene.
        /// </summary>
        /// <param name="scene">The loaded scene.</param>
        /// <param name="loadMode">The load mode Unity used.</param>
        private void HandleSceneLoaded(Scene scene, LoadSceneMode loadMode) {
            InitializeLoadedScene();
        }

        /// <summary>
        /// Applies UI closing and persistent manager refreshes for the current scene.
        /// </summary>
        private void InitializeLoadedScene() {
            _hasInitializedActiveScene = true;
            MarkScenePlayed();
            CloseConfiguredUi();
            EnableConfiguredUi();
            RefreshPersistentState();
        }

        #endregion
        #region Progress

        /// <summary>
        /// Saves numeric level scenes into player progression.
        /// </summary>
        private void MarkScenePlayed() {
            if (markNumericScenePlayed) {
                PersistentGameSettings.MarkActiveScenePlayed();
            }
        }

        #endregion
        #region UI

        /// <summary>
        /// Disables configured UI roots such as options, game over, level select, and stats panels.
        /// </summary>
        private void CloseConfiguredUi() {
            foreach (var uiObject in uiObjectsToClose) {
                if (uiObject != null) {
                    uiObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Enables configured UI roots such as the shop panel.
        /// </summary>
        private void EnableConfiguredUi() {
            foreach (var uiObject in uiObjectsToEnable) {
                if (uiObject != null) {
                    uiObject.SetActive(true);
                }
            }
        }

        #endregion
        #region Refresh

        /// <summary>
        /// Refreshes persistent managers that keep state between scenes.
        /// </summary>
        private void RefreshPersistentState() {
            if (refreshGameManager && GameManager.Instance != null) {
                GameManager.Instance.RefreshForSceneLoad();
            }

            if (refreshMoney && GameManager.Instance != null && GameManager.Instance.LevelStats != null) {
                GameManager.Instance.LevelStats.RefreshForSceneLoad();
            }

            if (refreshUnitPlacer && UnitPlacer.Instance != null) {
                UnitPlacer.Instance.RefreshForSceneLoad();
            }

            if (refreshLevelSelector) {
                var levelSelector = FindObjectOfType<LevelSelector>();
                if (levelSelector != null) {
                    levelSelector.RefreshFromSave();
                }
            }
        }

        #endregion
    }
}
