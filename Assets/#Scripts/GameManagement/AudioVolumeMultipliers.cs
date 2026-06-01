using UnityEngine;

namespace _Scripts.GameManagement {
    /// <summary>
    /// Stores user-facing audio volume multipliers.
    /// </summary>
    [CreateAssetMenu(fileName = "Audio Volume Multipliers", menuName = "Audio/Volume Multipliers")]
    public class AudioVolumeMultipliers : ScriptableObject {

        #region Variables

        [Header("Multipliers")]
        [SerializeField, Range(0f, 1f)] private float mainVolumeMultiplier = 0.5f;  // Master volume multiplier.
        [SerializeField, Range(0f, 1f)] private float uiVolumeMultiplier = 0.5f;    // UI volume multiplier.
        [SerializeField, Range(0f, 1f)] private float sfxVolumeMultiplier = 0.5f;   // SFX volume multiplier.
        [SerializeField, Range(0f, 1f)] private float musicVolumeMultiplier = 0.5f; // Music volume multiplier.

        public float MainVolumeMultiplier => mainVolumeMultiplier;
        public float UiVolumeMultiplier => uiVolumeMultiplier;
        public float SfxVolumeMultiplier => sfxVolumeMultiplier;
        public float MusicVolumeMultiplier => musicVolumeMultiplier;

        #endregion
        #region Public Methods

        /// <summary>
        /// Loads saved audio multiplier values from disk.
        /// </summary>
        public void LoadSavedValues() {
            mainVolumeMultiplier = PersistentGameSettings.MainVolumeMultiplier;
            uiVolumeMultiplier = PersistentGameSettings.UiVolumeMultiplier;
            sfxVolumeMultiplier = PersistentGameSettings.SfxVolumeMultiplier;
            musicVolumeMultiplier = PersistentGameSettings.MusicVolumeMultiplier;
        }

        /// <summary>
        /// Saves current audio multiplier values to disk.
        /// </summary>
        public void SaveValues() {
            PersistentGameSettings.SetAudioMultipliers(
                mainVolumeMultiplier,
                uiVolumeMultiplier,
                sfxVolumeMultiplier,
                musicVolumeMultiplier);
        }

        /// <summary>
        /// Sets the master volume multiplier.
        /// </summary>
        /// <param name="value">The new multiplier.</param>
        public void SetMainVolumeMultiplier(float value) {
            mainVolumeMultiplier = Mathf.Clamp01(value);
            SaveValues();
        }

        /// <summary>
        /// Sets the UI volume multiplier.
        /// </summary>
        /// <param name="value">The new multiplier.</param>
        public void SetUiVolumeMultiplier(float value) {
            uiVolumeMultiplier = Mathf.Clamp01(value);
            SaveValues();
        }

        /// <summary>
        /// Sets the SFX volume multiplier.
        /// </summary>
        /// <param name="value">The new multiplier.</param>
        public void SetSfxVolumeMultiplier(float value) {
            sfxVolumeMultiplier = Mathf.Clamp01(value);
            SaveValues();
        }

        /// <summary>
        /// Sets the music volume multiplier.
        /// </summary>
        /// <param name="value">The new multiplier.</param>
        public void SetMusicVolumeMultiplier(float value) {
            musicVolumeMultiplier = Mathf.Clamp01(value);
            SaveValues();
        }

        #endregion
    }
}
