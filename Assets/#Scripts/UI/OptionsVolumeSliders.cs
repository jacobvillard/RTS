using _Scripts.GameManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace _Scripts.UI {
    /// <summary>
    /// Connects options menu volume sliders to saved audio settings.
    /// </summary>
    [DisallowMultipleComponent]
    public class OptionsVolumeSliders : MonoBehaviour {

        #region Variables

        [Header("Settings")]
        [SerializeField] private AudioVolumeMultipliers volumeMultipliers;
        [SerializeField] private MainMenuAudioController mainMenuAudioController;
        [SerializeField] private bool refreshOnEnable = true;

        [Header("Sliders")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Slider uiVolumeSlider;

        [Header("Events")]
        [SerializeField] private UnityEvent volumesChanged;

        private UnityAction<float> _masterChanged;
        private UnityAction<float> _musicChanged;
        private UnityAction<float> _sfxChanged;
        private UnityAction<float> _uiChanged;

        #endregion
        #region Unity Methods

        private void Awake() {
            ResolveReferences();
            CacheListeners();
            AddSliderListeners();
        }

        private void OnEnable() {
            ResolveReferences();
            LinkAudioControllers();

            if (refreshOnEnable) {
                RefreshSliders();
            }
        }

        private void OnDestroy() {
            RemoveSliderListeners();
        }

        private void OnValidate() {
            ResolveReferences();
        }

        #endregion
        #region Public Methods

        /// <summary>
        /// Reloads saved values and updates sliders without firing slider events.
        /// </summary>
        public void RefreshSliders() {
            if (volumeMultipliers == null) return;

            LinkAudioControllers();
            volumeMultipliers.LoadSavedValues();
            SetSliderWithoutNotify(masterVolumeSlider, volumeMultipliers.MainVolumeMultiplier);
            SetSliderWithoutNotify(musicVolumeSlider, volumeMultipliers.MusicVolumeMultiplier);
            SetSliderWithoutNotify(sfxVolumeSlider, volumeMultipliers.SfxVolumeMultiplier);
            SetSliderWithoutNotify(uiVolumeSlider, volumeMultipliers.UiVolumeMultiplier);
            NotifyVolumesChanged();
        }

        #endregion
        #region Slider Events

        private void CacheListeners() {
            _masterChanged = SetMasterVolume;
            _musicChanged = SetMusicVolume;
            _sfxChanged = SetSfxVolume;
            _uiChanged = SetUiVolume;
        }

        private void AddSliderListeners() {
            if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.AddListener(_masterChanged);
            if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.AddListener(_musicChanged);
            if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.AddListener(_sfxChanged);
            if (uiVolumeSlider != null) uiVolumeSlider.onValueChanged.AddListener(_uiChanged);
        }

        private void RemoveSliderListeners() {
            if (masterVolumeSlider != null && _masterChanged != null) masterVolumeSlider.onValueChanged.RemoveListener(_masterChanged);
            if (musicVolumeSlider != null && _musicChanged != null) musicVolumeSlider.onValueChanged.RemoveListener(_musicChanged);
            if (sfxVolumeSlider != null && _sfxChanged != null) sfxVolumeSlider.onValueChanged.RemoveListener(_sfxChanged);
            if (uiVolumeSlider != null && _uiChanged != null) uiVolumeSlider.onValueChanged.RemoveListener(_uiChanged);
        }

        private void SetMasterVolume(float value) {
            if (volumeMultipliers == null) return;

            volumeMultipliers.SetMainVolumeMultiplier(value);
            NotifyVolumesChanged();
        }

        private void SetMusicVolume(float value) {
            if (volumeMultipliers == null) return;

            volumeMultipliers.SetMusicVolumeMultiplier(value);
            NotifyVolumesChanged();
        }

        private void SetSfxVolume(float value) {
            if (volumeMultipliers == null) return;

            volumeMultipliers.SetSfxVolumeMultiplier(value);
            NotifyVolumesChanged();
        }

        private void SetUiVolume(float value) {
            if (volumeMultipliers == null) return;

            volumeMultipliers.SetUiVolumeMultiplier(value);
            NotifyVolumesChanged();
        }

        #endregion
        #region Helpers

        private void ResolveReferences() {
            if (mainMenuAudioController == null) {
                mainMenuAudioController = FindObjectOfType<MainMenuAudioController>();
            }
        }

        private void LinkAudioControllers() {
            if (volumeMultipliers == null) return;

            mainMenuAudioController?.SetVolumeMultipliers(volumeMultipliers);
        }

        private void NotifyVolumesChanged() {
            LinkAudioControllers();
            mainMenuAudioController?.RefreshVolumes();
            AudioManager.Instance?.UpdateSlidersFromVolumeMultipliers();
            volumesChanged?.Invoke();
        }

        private static void SetSliderWithoutNotify(Slider slider, float value) {
            if (slider != null) {
                slider.SetValueWithoutNotify(Mathf.Clamp01(value));
            }
        }

        #endregion
    }
}
