using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using _Scripts.GameManagement;

namespace _Scripts.UI {
    /// <summary>
    /// Plays main menu music and button hover/confirm sounds.
    /// </summary>
    [DisallowMultipleComponent]
    public class MainMenuAudioController : MonoBehaviour {

        #region Variables

        [Header("Music")]
        [SerializeField] private AudioClip mainMenuMusic;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.65f;
        [SerializeField] private bool playMusicOnEnable = true;
        [SerializeField] private bool restartMusicOnEnable;
        [SerializeField] private AudioVolumeMultipliers volumeMultipliers;

        [Header("Button SFX")]
        [SerializeField] private AudioClip hoverSound;
        [SerializeField] private AudioClip confirmSound;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.9f;
        [SerializeField, Range(0.5f, 1.5f)] private float hoverPitch = 1.08f;
        [SerializeField, Range(0.5f, 1.5f)] private float confirmPitch = 0.96f;
        [SerializeField, Min(0f)] private float hoverCooldown = 0.05f;

        [Header("Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        private float _lastHoverTime = -999f;

        #endregion
        #region Unity Methods

        private void Awake() {
            ResolveSources();
            volumeMultipliers?.LoadSavedValues();
            RegisterChildButtons();
        }

        private void OnEnable() {
            ResolveSources();
            volumeMultipliers?.LoadSavedValues();
            RegisterChildButtons();

            if (playMusicOnEnable) {
                PlayMusic();
            }
        }

        private void OnTransformChildrenChanged() {
            if (!isActiveAndEnabled) return;

            RegisterChildButtons();
        }

        private void OnValidate() {
            ResolveSources();
            ApplyMusicVolume();
        }

        #endregion
        #region Public Methods

        /// <summary>
        /// Starts the looping main menu music.
        /// </summary>
        public void PlayMusic() {
            ResolveSources();
            if (musicSource == null || mainMenuMusic == null) return;

            if (!restartMusicOnEnable && musicSource.isPlaying && musicSource.clip == mainMenuMusic) return;

            musicSource.clip = mainMenuMusic;
            musicSource.loop = true;
            musicSource.volume = GetMusicVolume();
            musicSource.spatialBlend = 0f;
            musicSource.Play();
        }

        /// <summary>
        /// Stops the main menu music.
        /// </summary>
        public void StopMusic() {
            if (musicSource != null) {
                musicSource.Stop();
            }
        }

        /// <summary>
        /// Plays the configured hover sound.
        /// </summary>
        public void PlayHoverSound() {
            if (Time.unscaledTime - _lastHoverTime < hoverCooldown) return;

            _lastHoverTime = Time.unscaledTime;
            PlaySfx(hoverSound, hoverPitch);
        }

        /// <summary>
        /// Plays the configured confirm sound.
        /// </summary>
        public void PlayConfirmSound() {
            PlaySfx(confirmSound, confirmPitch);
        }

        /// <summary>
        /// Assigns the shared volume multiplier asset used by options sliders.
        /// </summary>
        public void SetVolumeMultipliers(AudioVolumeMultipliers multipliers) {
            volumeMultipliers = multipliers;
            RefreshVolumes();
        }

        /// <summary>
        /// Applies the latest saved slider values to active menu audio.
        /// </summary>
        public void RefreshVolumes() {
            volumeMultipliers?.LoadSavedValues();
            ApplyMusicVolume();
        }

        #endregion
        #region Playback

        private void PlaySfx(AudioClip clip, float pitch) {
            ResolveSources();
            if (sfxSource == null || clip == null) return;

            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(clip, GetUiSfxVolume());
        }

        private void ApplyMusicVolume() {
            if (musicSource != null) {
                musicSource.volume = GetMusicVolume();
            }
        }

        private float GetMusicVolume() {
            return musicVolume * GetMainVolumeMultiplier() * GetMusicVolumeMultiplier();
        }

        private float GetUiSfxVolume() {
            return sfxVolume * GetMainVolumeMultiplier() * GetUiVolumeMultiplier() * GetSfxVolumeMultiplier();
        }

        private float GetMainVolumeMultiplier() {
            return volumeMultipliers != null ? volumeMultipliers.MainVolumeMultiplier : 1f;
        }

        private float GetUiVolumeMultiplier() {
            return volumeMultipliers != null ? volumeMultipliers.UiVolumeMultiplier : 1f;
        }

        private float GetSfxVolumeMultiplier() {
            return volumeMultipliers != null ? volumeMultipliers.SfxVolumeMultiplier : 1f;
        }

        private float GetMusicVolumeMultiplier() {
            return volumeMultipliers != null ? volumeMultipliers.MusicVolumeMultiplier : 1f;
        }

        #endregion
        #region Setup

        private void ResolveSources() {
            if (musicSource == null) {
                musicSource = gameObject.AddComponent<AudioSource>();
            }

            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;

            if (sfxSource == null) {
                sfxSource = gameObject.AddComponent<AudioSource>();
            }

            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f;
        }

        private void RegisterChildButtons() {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var button in buttons) {
                if (button == null) continue;

                var trigger = button.GetComponent<MainMenuAudioButtonTrigger>();
                if (trigger == null) {
                    trigger = button.gameObject.AddComponent<MainMenuAudioButtonTrigger>();
                }

                trigger.SetController(this);
            }
        }

        #endregion
    }

    /// <summary>
    /// Child button event bridge used by MainMenuAudioController.
    /// </summary>
    [DisallowMultipleComponent]
    public class MainMenuAudioButtonTrigger : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, ISubmitHandler {

        private MainMenuAudioController _controller;

        public void SetController(MainMenuAudioController controller) {
            _controller = controller;
        }

        public void OnPointerEnter(PointerEventData eventData) {
            _controller?.PlayHoverSound();
        }

        public void OnPointerDown(PointerEventData eventData) {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            _controller?.PlayConfirmSound();
        }

        public void OnSubmit(BaseEventData eventData) {
            _controller?.PlayConfirmSound();
        }
    }
}
