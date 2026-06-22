using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts.GameManagement {
    /// <summary>
    /// Reads and writes player settings and progression to a persistent JSON file.
    /// </summary>
    public static class PersistentGameSettings {

        #region Types

        /// <summary>
        /// Save payload stored on disk.
        /// </summary>
        [Serializable]
        private class SaveData {
            public int highestLevelPlayed = 1;
            public float mainVolumeMultiplier = 0.5f;
            public float uiVolumeMultiplier = 0.5f;
            public float sfxVolumeMultiplier = 0.5f;
            public float musicVolumeMultiplier = 0.5f;
            public System.Collections.Generic.List<LevelBestTime> levelBestTimes = new();
        }

        /// <summary>
        /// Best completion time stored by one-based level number.
        /// </summary>
        [Serializable]
        private class LevelBestTime {
            public int levelNumber;
            public float seconds;
        }

        #endregion
        #region Variables

        private const string SaveFileName = "player-settings.json";

        private static SaveData _cachedSaveData; // Runtime save cache.

        #endregion
        #region Properties

        public static int HighestLevelPlayed => Load().highestLevelPlayed;
        public static float MainVolumeMultiplier => Load().mainVolumeMultiplier;
        public static float UiVolumeMultiplier => Load().uiVolumeMultiplier;
        public static float SfxVolumeMultiplier => Load().sfxVolumeMultiplier;
        public static float MusicVolumeMultiplier => Load().musicVolumeMultiplier;

        #endregion
        #region Progress

        /// <summary>
        /// Records a level as played when it is higher than the saved value.
        /// </summary>
        /// <param name="levelNumber">The numeric level scene played.</param>
        public static void MarkLevelPlayed(int levelNumber) {
            var saveData = Load();
            var safeLevelNumber = Mathf.Max(1, levelNumber);
            if (safeLevelNumber <= saveData.highestLevelPlayed) return;

            saveData.highestLevelPlayed = safeLevelNumber;
            Save();
        }

        /// <summary>
        /// Records the active scene as played when its name is numeric.
        /// </summary>
        public static void MarkActiveScenePlayed() {
            if (int.TryParse(SceneManager.GetActiveScene().name, out var levelNumber)) {
                MarkLevelPlayed(levelNumber);
            }
        }

        /// <summary>
        /// Gets a safe highest played level for level select UI.
        /// </summary>
        /// <param name="fallbackLevel">The fallback if no save exists.</param>
        /// <returns>The highest played level.</returns>
        public static int GetHighestLevelPlayed(int fallbackLevel) {
            return Mathf.Max(1, Load(fallbackLevel).highestLevelPlayed);
        }

        /// <summary>
        /// Gets the saved best time for a one-based level number.
        /// </summary>
        public static bool TryGetBestTime(int levelNumber, out float seconds) {
            var saveData = Load();
            var safeLevelNumber = Mathf.Max(1, levelNumber);
            foreach (var bestTime in saveData.levelBestTimes) {
                if (bestTime == null || bestTime.levelNumber != safeLevelNumber) continue;

                seconds = bestTime.seconds;
                return seconds > 0f;
            }

            seconds = 0f;
            return false;
        }

        /// <summary>
        /// Stores a completion time when it beats the previous saved time.
        /// </summary>
        public static void SetBestTime(int levelNumber, float seconds) {
            if (seconds <= 0f) return;

            var saveData = Load();
            var safeLevelNumber = Mathf.Max(1, levelNumber);
            foreach (var bestTime in saveData.levelBestTimes) {
                if (bestTime == null || bestTime.levelNumber != safeLevelNumber) continue;
                if (bestTime.seconds > 0f && bestTime.seconds <= seconds) return;

                bestTime.seconds = seconds;
                Save();
                return;
            }

            saveData.levelBestTimes.Add(new LevelBestTime {
                levelNumber = safeLevelNumber,
                seconds = seconds
            });
            Save();
        }

        #endregion
        #region Audio

        /// <summary>
        /// Stores audio multiplier values in the persistent save file.
        /// </summary>
        public static void SetAudioMultipliers(float main, float ui, float sfx, float music) {
            var saveData = Load();
            saveData.mainVolumeMultiplier = Mathf.Clamp01(main);
            saveData.uiVolumeMultiplier = Mathf.Clamp01(ui);
            saveData.sfxVolumeMultiplier = Mathf.Clamp01(sfx);
            saveData.musicVolumeMultiplier = Mathf.Clamp01(music);
            Save();
        }

        #endregion
        #region File

        /// <summary>
        /// Loads save data from disk or creates defaults.
        /// </summary>
        /// <param name="fallbackHighestLevel">The fallback highest level when creating defaults.</param>
        /// <returns>The loaded save data.</returns>
        private static SaveData Load(int fallbackHighestLevel = 1) {
            if (_cachedSaveData != null) return _cachedSaveData;

            var path = GetSavePath();
            if (!File.Exists(path)) {
                _cachedSaveData = new SaveData {
                    highestLevelPlayed = Mathf.Max(1, fallbackHighestLevel)
                };
                Save();
                return _cachedSaveData;
            }

            try {
                _cachedSaveData = JsonUtility.FromJson<SaveData>(File.ReadAllText(path)) ?? new SaveData();
            }
            catch (Exception exception) {
                Debug.LogWarning($"Could not read save file at '{path}'. Creating defaults. {exception.Message}");
                _cachedSaveData = new SaveData();
            }

            _cachedSaveData.highestLevelPlayed = Mathf.Max(1, _cachedSaveData.highestLevelPlayed);
            _cachedSaveData.levelBestTimes ??= new System.Collections.Generic.List<LevelBestTime>();
            return _cachedSaveData;
        }

        /// <summary>
        /// Writes the cached save data to disk.
        /// </summary>
        private static void Save() {
            if (_cachedSaveData == null) return;

            var path = GetSavePath();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(_cachedSaveData, true));
        }

        /// <summary>
        /// Gets the player settings save path.
        /// </summary>
        /// <returns>The full save file path.</returns>
        private static string GetSavePath() {
            return Path.Combine(Application.persistentDataPath, SaveFileName);
        }

        #endregion
    }
}
