using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts.GameManagement {
    /// <summary>
    /// Stores per-level runtime settings that should not live on scene objects.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelSettingsDatabase", menuName = "RTS/Level Settings Database")]
    public class LevelSettingsDatabase : ScriptableObject {

        #region Types

        /// <summary>
        /// Runtime values applied when a matching level scene is loaded.
        /// </summary>
        [Serializable]
        public class LevelSettings {

            [Header("Scene")]
            public string sceneName; // Scene name this row applies to.
            
            [Header("LevelName")]
            public string levelName; // Optional display name for the level.

            [Header("Budget")]
            public int startMoney = 220; // Starting money for unit placement.

            [Header("Camera")]
            public float cameraOrthographicSize = 20f; // Starting orthographic size for the main camera.

            [Header("Available Units")] 
            public bool officer;
            public bool Scout;
            public bool Pikemen;
            public bool Skirmishers;
            public bool Grenadiers;
            public bool Bannermen;


        }

        #endregion
        #region Variables

        private const string ResourcesPath = "LevelSettingsDatabase";

        [Header("Fallback")]
        [SerializeField] private int fallbackStartMoney = 220;              // Used when the current scene has no row.
        [SerializeField] private float fallbackCameraOrthographicSize = 20f; // Used when the current scene has no row.

        [Header("Levels")]
        [SerializeField] private List<LevelSettings> levels = new(); // Per-scene level settings.

        private static LevelSettingsDatabase _cachedDatabase; // Resources-loaded database cache.

        #endregion
        #region Public Methods

        /// <summary>
        /// Loads the project-wide level settings database from Resources.
        /// </summary>
        /// <returns>The loaded database, or null if the asset has not been created.</returns>
        public static LevelSettingsDatabase Load() {
            if (_cachedDatabase != null) return _cachedDatabase;

            _cachedDatabase = Resources.Load<LevelSettingsDatabase>(ResourcesPath);
            if (_cachedDatabase == null) {
                Debug.LogWarning(
                    "No LevelSettingsDatabase found at Assets/Resources/LevelSettingsDatabase.asset. " +
                    "Using scene fallback values until the asset is created.");
            }

            return _cachedDatabase;
        }

        /// <summary>
        /// Gets the settings for the active Unity scene.
        /// </summary>
        /// <returns>The settings row for the active scene, or fallback settings.</returns>
        public LevelSettings GetCurrentLevelSettings() {
            return GetSettings(SceneManager.GetActiveScene().name);
        }

        /// <summary>
        /// Gets the settings for a scene by name.
        /// </summary>
        /// <param name="sceneName">The scene name to search for.</param>
        /// <returns>The matching settings row, or fallback settings.</returns>
        public LevelSettings GetSettings(string sceneName) {
            foreach (var level in levels) {
                if (level == null || string.IsNullOrWhiteSpace(level.sceneName)) continue;
                if (level.sceneName.Equals(sceneName, StringComparison.OrdinalIgnoreCase)) return level;
            }

            return new LevelSettings {
                sceneName = sceneName,
                startMoney = fallbackStartMoney,
                cameraOrthographicSize = fallbackCameraOrthographicSize
            };
        }

        #endregion
    }
}
