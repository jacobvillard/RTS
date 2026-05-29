using UnityEngine;
using _Scripts.Units;
using System.Collections.Generic;

namespace _Scripts.GameManagement {
    /// <summary>
    /// Central place for UI and battle sound cues.
    /// </summary>
    public class AudioManager : Singleton<AudioManager> {

        #region Variables

        [Header("2D UI Sounds")]
        [SerializeField] private AudioClip defaultButtonSound;    // Generic UI button click.
        [SerializeField] private AudioClip roundStartSound;       // Played when the battle starts.
        [SerializeField] private AudioClip roundEndSound;         // Played when the battle ends.
        [SerializeField] private AudioClip clearUnitsSound;       // Played when placed units are cleared.
        [SerializeField] private AudioClip placementFailedSound;  // Played when placement cannot happen.
        [SerializeField] private AudioClip unitSelectedSound;     // Played when a unit is selected.

        [Header("3D Placement Sounds")]
        [SerializeField] private AudioClip placeUnitSound;        // Played at a newly placed unit position.
        [SerializeField] private AudioClip badUnitPositionSound;  // Played at a bad movement destination.
        [SerializeField] private AudioClip moveOrderSound;        // Played at a valid movement destination.

        [Header("3D Combat Sounds")]
        [SerializeField] private AudioClip[] meleeHitSounds;      // Random melee impact sounds.
        [SerializeField] private AudioClip[] cavalryHitSounds;    // Random cavalry impact sounds.
        [SerializeField] private AudioClip[] musketShotSounds;    // Random musket shot sounds.
        [SerializeField] private AudioClip[] unitDeathSounds;     // Random unit death sounds.
        [SerializeField] private AudioClip[] cavalryDeathSounds;  // Random cavalry death sounds.
        [SerializeField] private AudioClip[] aiAlertSounds;       // Random AI response/alert sounds.

        [Header("Volume")]
        [SerializeField, Range(0f, 1f)] private float mainVolume = 1f;   // Master volume applied to all audio.
        [SerializeField, Range(0f, 1f)] private float uiVolume = 1f;     // Volume for 2D UI cues.
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.8f;  // Volume for non-music sound effects.
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.7f; // Volume for music playback.
        [SerializeField, Range(0f, 1f)] private float battleVolume = 0.3f; // Extra multiplier for battle/combat cues.

        [Header("3D Audio")]
        [SerializeField, Range(0f, 1f)] private float spatialBlend = 1f; // 1 is fully 3D.
        [SerializeField] private float minDistance = 0.75f;             // Distance before attenuation begins.
        [SerializeField] private float maxDistance = 12f;               // Distance where the sound is quietest.

        [Header("Music")]
        [SerializeField] private AudioClip musicClip;            // Optional looping music clip.
        [SerializeField] private AudioClip[] roundMusicClips;    // Random looping music used during active rounds.
        [SerializeField] private bool playMusicOnAwake;         // Starts music automatically.

        [Header("Sources")]
        [SerializeField] private AudioSource uiAudioSource; // Optional source used for 2D UI sounds.
        [SerializeField] private AudioSource musicAudioSource; // Optional source used for music.

        private readonly List<OwnedAudio> _ownedAudio = new(); // Active sounds associated with scene components.

        #endregion
        #region Types

        private class OwnedAudio {
            public Component owner;       // Component that emitted this sound.
            public GameObject soundObject; // Runtime one-shot audio object.
            public AudioSource source;    // Source playing the owned sound.
        }

        #endregion
        #region Unity Methods

        protected override void Awake() {
            base.Awake();

            if (Instance != this) {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            EnsureUiSource();
            EnsureMusicSource();

            if (playMusicOnAwake) {
                PlayMusic();
            }
        }

        #endregion
        #region Public UI Cues

        /// <summary>
        /// Plays the generic button click sound.
        /// </summary>
        public void PlayDefaultButtonSound() {
            Play2D(defaultButtonSound);
        }

        /// <summary>
        /// Plays the battle start sound.
        /// </summary>
        public void PlayRoundStart() {
            Play2D(roundStartSound);
        }

        /// <summary>
        /// Plays the battle end sound.
        /// </summary>
        public void PlayRoundEnd() {
            Play2D(roundEndSound);
        }

        /// <summary>
        /// Plays the clear placed units sound.
        /// </summary>
        public void PlayClearUnits() {
            Play2D(clearUnitsSound);
        }

        /// <summary>
        /// Plays the placement failed sound.
        /// </summary>
        public void PlayPlacementFailed() {
            Play2D(placementFailedSound);
        }

        /// <summary>
        /// Plays the unit selected sound.
        /// </summary>
        public void PlayUnitSelected() {
            Play2D(unitSelectedSound);
        }

        /// <summary>
        /// Starts looping music when a music clip is assigned.
        /// </summary>
        public void PlayMusic() {
            PlayMusicClip(musicClip);
        }

        /// <summary>
        /// Stops music playback.
        /// </summary>
        public void StopMusic() {
            if (musicAudioSource != null) {
                musicAudioSource.Stop();
            }
        }

        /// <summary>
        /// Picks a random round music track and loops it.
        /// </summary>
        public void PlayRandomRoundMusic() {
            var roundMusicClip = GetRandomClip(roundMusicClips);
            if (roundMusicClip == null) return;

            PlayMusicClip(roundMusicClip);
        }

        #endregion
        #region Public World Cues

        /// <summary>
        /// Plays a unit placement sound in world space.
        /// </summary>
        /// <param name="position">The placement position.</param>
        public void PlayPlaceUnit(Vector3 position) {
            Play3D(placeUnitSound, position);
        }

        /// <summary>
        /// Plays a bad destination sound in world space.
        /// </summary>
        /// <param name="position">The rejected position.</param>
        public void PlayBadUnitPosition(Vector3 position) {
            Play3D(badUnitPositionSound, position);
        }

        /// <summary>
        /// Plays a valid move order sound in world space.
        /// </summary>
        /// <param name="position">The destination position.</param>
        public void PlayMoveOrder(Vector3 position) {
            Play3D(moveOrderSound, position);
        }

        /// <summary>
        /// Plays a random melee hit sound in world space.
        /// </summary>
        /// <param name="position">The hit position.</param>
        public void PlayMeleeHit(Vector3 position) {
            PlayMeleeHit(position, UnitType.Infantry);
        }

        /// <summary>
        /// Plays a random melee hit sound in world space for a unit class.
        /// </summary>
        /// <param name="position">The hit position.</param>
        /// <param name="unitType">The unit class making the hit.</param>
        public void PlayMeleeHit(Vector3 position, UnitType unitType) {
            PlayMeleeHit(position, unitType, null);
        }

        /// <summary>
        /// Plays a random melee hit sound in world space for a unit class.
        /// </summary>
        /// <param name="position">The hit position.</param>
        /// <param name="unitType">The unit class making the hit.</param>
        /// <param name="owner">The unit or component that emitted the sound.</param>
        public void PlayMeleeHit(Vector3 position, UnitType unitType, Component owner) {
            var clip = unitType == UnitType.Cavalry
                ? GetRandomClip(cavalryHitSounds, meleeHitSounds)
                : GetRandomClip(meleeHitSounds);

            Play3D(clip, position, GetBattleVolume(), owner);
        }

        /// <summary>
        /// Plays a random musket shot sound in world space.
        /// </summary>
        /// <param name="position">The shot position.</param>
        public void PlayMusketShot(Vector3 position) {
            PlayMusketShot(position, null);
        }

        /// <summary>
        /// Plays a random musket shot sound in world space.
        /// </summary>
        /// <param name="position">The shot position.</param>
        /// <param name="owner">The unit or component that emitted the sound.</param>
        public void PlayMusketShot(Vector3 position, Component owner) {
            Play3D(GetRandomClip(musketShotSounds), position, GetBattleVolume(), owner);
        }

        /// <summary>
        /// Plays a random unit death sound in world space.
        /// </summary>
        /// <param name="position">The death position.</param>
        public void PlayUnitDeath(Vector3 position) {
            PlayUnitDeath(position, UnitType.Infantry);
        }

        /// <summary>
        /// Plays a random unit death sound in world space for a unit class.
        /// </summary>
        /// <param name="position">The death position.</param>
        /// <param name="unitType">The dying unit class.</param>
        public void PlayUnitDeath(Vector3 position, UnitType unitType) {
            var clip = unitType == UnitType.Cavalry
                ? GetRandomClip(cavalryDeathSounds, unitDeathSounds)
                : GetRandomClip(unitDeathSounds);

            Play3D(clip, position, GetBattleVolume());
        }

        /// <summary>
        /// Plays a random AI alert sound in world space.
        /// </summary>
        /// <param name="position">The alerted unit position.</param>
        public void PlayAiAlert(Vector3 position) {
            PlayAiAlert(position, null);
        }

        /// <summary>
        /// Plays a random AI alert sound in world space.
        /// </summary>
        /// <param name="position">The alerted unit position.</param>
        /// <param name="owner">The unit or component that emitted the sound.</param>
        public void PlayAiAlert(Vector3 position, Component owner) {
            Play3D(GetRandomClip(aiAlertSounds), position, GetBattleVolume(), owner);
        }

        /// <summary>
        /// Stops all still-playing one-shot sounds emitted by an owner.
        /// </summary>
        /// <param name="owner">The component whose sounds should stop.</param>
        public void StopSoundsForOwner(Component owner) {
            if (owner == null) return;

            for (var i = _ownedAudio.Count - 1; i >= 0; i--) {
                var ownedAudio = _ownedAudio[i];
                if (ownedAudio == null || ownedAudio.soundObject == null) {
                    _ownedAudio.RemoveAt(i);
                    continue;
                }

                if (ownedAudio.owner != owner) continue;

                StopAndDestroyOwnedAudio(ownedAudio);
                _ownedAudio.RemoveAt(i);
            }
        }

        /// <summary>
        /// Stops all tracked combat sounds near a world position.
        /// </summary>
        /// <param name="position">The center position.</param>
        /// <param name="radius">The stop radius.</param>
        public void StopSoundsNear(Vector3 position, float radius) {
            for (var i = _ownedAudio.Count - 1; i >= 0; i--) {
                var ownedAudio = _ownedAudio[i];
                if (ownedAudio == null || ownedAudio.soundObject == null) {
                    _ownedAudio.RemoveAt(i);
                    continue;
                }

                if (Vector3.Distance(position, ownedAudio.soundObject.transform.position) > radius) continue;

                StopAndDestroyOwnedAudio(ownedAudio);
                _ownedAudio.RemoveAt(i);
            }
        }

        #endregion
        #region Playback

        /// <summary>
        /// Plays a non-spatial sound through the UI audio source.
        /// </summary>
        /// <param name="clip">The clip to play.</param>
        private void Play2D(AudioClip clip) {
            if (clip == null || uiAudioSource == null) return;

            uiAudioSource.PlayOneShot(clip, GetUiVolume());
        }

        /// <summary>
        /// Starts looping a specific music clip.
        /// </summary>
        /// <param name="clip">The music clip to loop.</param>
        private void PlayMusicClip(AudioClip clip) {
            if (musicAudioSource == null || clip == null) return;

            musicAudioSource.Stop();
            musicAudioSource.clip = clip;
            musicAudioSource.loop = true;
            musicAudioSource.volume = GetMusicVolume();
            musicAudioSource.Play();
        }

        /// <summary>
        /// Plays a spatial sound at a world position.
        /// </summary>
        /// <param name="clip">The clip to play.</param>
        /// <param name="position">The world position.</param>
        private void Play3D(AudioClip clip, Vector3 position) {
            Play3D(clip, position, GetSfxVolume());
        }

        /// <summary>
        /// Plays a spatial sound at a world position with a volume override.
        /// </summary>
        /// <param name="clip">The clip to play.</param>
        /// <param name="position">The world position.</param>
        /// <param name="volume">The playback volume.</param>
        private void Play3D(AudioClip clip, Vector3 position, float volume) {
            Play3D(clip, position, volume, null);
        }

        /// <summary>
        /// Plays a spatial sound at a world position with a volume override.
        /// </summary>
        /// <param name="clip">The clip to play.</param>
        /// <param name="position">The world position.</param>
        /// <param name="volume">The playback volume.</param>
        /// <param name="owner">Optional owner used to stop this sound early.</param>
        private void Play3D(AudioClip clip, Vector3 position, float volume, Component owner) {
            if (clip == null) return;

            CleanupOwnedAudio();

            var soundObject = new GameObject("One Shot Audio - " + clip.name);
            soundObject.transform.position = position;

            var source = soundObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = volume;
            source.spatialBlend = spatialBlend;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.Play();

            if (owner != null) {
                _ownedAudio.Add(new OwnedAudio {
                    owner = owner,
                    soundObject = soundObject,
                    source = source
                });
            }

            Destroy(soundObject, clip.length + 0.1f);
        }

        /// <summary>
        /// Picks a random clip from an optional clip list.
        /// </summary>
        /// <param name="clips">Candidate clips.</param>
        /// <returns>A random clip or null.</returns>
        private static AudioClip GetRandomClip(AudioClip[] clips) {
            if (clips == null || clips.Length == 0) return null;

            return clips[UnityEngine.Random.Range(0, clips.Length)];
        }

        /// <summary>
        /// Picks from preferred clips, falling back to default clips if needed.
        /// </summary>
        /// <param name="preferredClips">Clips to prefer.</param>
        /// <param name="fallbackClips">Fallback clips.</param>
        /// <returns>A random clip or null.</returns>
        private static AudioClip GetRandomClip(AudioClip[] preferredClips, AudioClip[] fallbackClips) {
            var preferredClip = GetRandomClip(preferredClips);
            return preferredClip != null ? preferredClip : GetRandomClip(fallbackClips);
        }

        /// <summary>
        /// Creates a UI audio source when one was not assigned.
        /// </summary>
        private void EnsureUiSource() {
            if (uiAudioSource == null) {
                uiAudioSource = gameObject.AddComponent<AudioSource>();
            }

            uiAudioSource.playOnAwake = false;
            uiAudioSource.spatialBlend = 0f;
        }

        /// <summary>
        /// Creates a music audio source when one was not assigned.
        /// </summary>
        private void EnsureMusicSource() {
            if (musicAudioSource == null) {
                musicAudioSource = gameObject.AddComponent<AudioSource>();
            }

            musicAudioSource.playOnAwake = false;
            musicAudioSource.spatialBlend = 0f;
            musicAudioSource.volume = GetMusicVolume();
        }

        /// <summary>
        /// Stops an owned source immediately and destroys its object.
        /// </summary>
        /// <param name="ownedAudio">The owned audio to stop.</param>
        private void StopAndDestroyOwnedAudio(OwnedAudio ownedAudio) {
            if (ownedAudio == null) return;

            if (ownedAudio.source != null) {
                ownedAudio.source.Stop();
            }

            if (ownedAudio.soundObject != null) {
                Destroy(ownedAudio.soundObject);
            }
        }

        /// <summary>
        /// Removes finished or destroyed owned audio entries.
        /// </summary>
        private void CleanupOwnedAudio() {
            for (var i = _ownedAudio.Count - 1; i >= 0; i--) {
                if (_ownedAudio[i] == null || _ownedAudio[i].soundObject == null) {
                    _ownedAudio.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Gets final UI volume after master volume.
        /// </summary>
        /// <returns>The final UI volume.</returns>
        private float GetUiVolume() {
            return mainVolume * uiVolume;
        }

        /// <summary>
        /// Gets final SFX volume after master volume.
        /// </summary>
        /// <returns>The final SFX volume.</returns>
        private float GetSfxVolume() {
            return mainVolume * sfxVolume;
        }

        /// <summary>
        /// Gets final battle volume after master and SFX volume.
        /// </summary>
        /// <returns>The final battle volume.</returns>
        private float GetBattleVolume() {
            return mainVolume * sfxVolume * battleVolume;
        }

        /// <summary>
        /// Gets final music volume after master volume.
        /// </summary>
        /// <returns>The final music volume.</returns>
        private float GetMusicVolume() {
            return mainVolume * musicVolume;
        }

        #endregion
    }
}
