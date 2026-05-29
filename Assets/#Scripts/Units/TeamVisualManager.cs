using System;
using System.IO;
using UnityEngine;

namespace _Scripts.Units {
    /// <summary>
    /// Loads, saves, and provides custom visual sprites for each team.
    /// </summary>
    public class TeamVisualManager : MonoBehaviour {

        #region Variables

        private const string PlayerImageFileName = "player-team-image.png";
        private const string AiImageFileName = "ai-team-image.png";

        public static TeamVisualManager Instance; // Runtime access point for team images.

        [Header("Fallback Sprites")]
        [SerializeField] private Sprite defaultPlayerSprite; // Sprite used when the player has not uploaded an image.
        [SerializeField] private Sprite defaultAiSprite;     // Sprite used when the AI has not uploaded an image.

        [Header("Import Settings")]
        [SerializeField] private int maxTextureSize = 256;   // Maximum imported image width or height.
        [SerializeField] private float pixelsPerUnit = 100f; // Pixels-per-unit used when creating runtime sprites.

        private Sprite _playerSprite; // Runtime sprite for Team.Player.
        private Sprite _aiSprite;     // Runtime sprite for Team.AI.

        public event Action<Team, Sprite> TeamSpriteChanged;

        #endregion
        #region Unity Methods

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            LoadSavedTeamImages();
        }

        #endregion
        #region Public Methods

        /// <summary>
        /// Gets the current sprite for a team, falling back to configured defaults.
        /// </summary>
        /// <param name="team">The team being displayed.</param>
        /// <returns>The team sprite, or null if none exists.</returns>
        public Sprite GetTeamSprite(Team team) {
            return team switch {
                Team.Player => _playerSprite != null ? _playerSprite : defaultPlayerSprite,
                Team.AI => _aiSprite != null ? _aiSprite : defaultAiSprite,
                _ => null
            };
        }

        /// <summary>
        /// Imports an image from disk, saves a copy, and applies it to a team.
        /// </summary>
        /// <param name="team">The team receiving the image.</param>
        /// <param name="sourcePath">The png or jpg file path to import.</param>
        /// <returns>True when the image was imported.</returns>
        public bool TrySetTeamImageFromPath(Team team, string sourcePath) {
            if (!TryLoadSpriteFromPath(sourcePath, out var sprite, out var encodedPng)) {
                return false;
            }

            File.WriteAllBytes(GetSavedImagePath(team), encodedPng);
            SetTeamSprite(team, sprite);
            return true;
        }

        /// <summary>
        /// Clears a custom team image and restores the fallback sprite.
        /// </summary>
        /// <param name="team">The team to reset.</param>
        public void ClearTeamImage(Team team) {
            var savedPath = GetSavedImagePath(team);
            if (File.Exists(savedPath)) {
                File.Delete(savedPath);
            }

            SetTeamSprite(team, null);
        }

        #endregion
        #region Loading

        /// <summary>
        /// Loads previously saved team images from persistent storage.
        /// </summary>
        private void LoadSavedTeamImages() {
            LoadSavedTeamImage(Team.Player);
            LoadSavedTeamImage(Team.AI);
        }

        /// <summary>
        /// Loads one saved team image when present.
        /// </summary>
        /// <param name="team">The team to load.</param>
        private void LoadSavedTeamImage(Team team) {
            var savedPath = GetSavedImagePath(team);
            if (!File.Exists(savedPath)) return;

            if (TryLoadSpriteFromPath(savedPath, out var sprite, out _)) {
                SetTeamSprite(team, sprite);
            }
        }

        /// <summary>
        /// Loads and normalizes a sprite from a source image path.
        /// </summary>
        /// <param name="path">The path to load.</param>
        /// <param name="sprite">The loaded sprite.</param>
        /// <param name="encodedPng">A png copy of the normalized texture.</param>
        /// <returns>True when the file could be loaded as an image.</returns>
        private bool TryLoadSpriteFromPath(string path, out Sprite sprite, out byte[] encodedPng) {
            sprite = null;
            encodedPng = null;

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) {
                Debug.LogWarning("Team image path was empty or missing: " + path);
                return false;
            }

            var sourceTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!sourceTexture.LoadImage(File.ReadAllBytes(path))) {
                Debug.LogWarning("Selected file could not be loaded as an image: " + path);
                return false;
            }

            var normalizedTexture = ResizeIfNeeded(sourceTexture);
            normalizedTexture.name = Path.GetFileNameWithoutExtension(path);
            normalizedTexture.filterMode = FilterMode.Bilinear;
            normalizedTexture.wrapMode = TextureWrapMode.Clamp;

            encodedPng = normalizedTexture.EncodeToPNG();
            sprite = Sprite.Create(
                normalizedTexture,
                new Rect(0f, 0f, normalizedTexture.width, normalizedTexture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit);

            return true;
        }

        #endregion
        #region Sprite State

        /// <summary>
        /// Sets a team's runtime sprite and notifies listeners.
        /// </summary>
        /// <param name="team">The team to update.</param>
        /// <param name="sprite">The new runtime sprite.</param>
        private void SetTeamSprite(Team team, Sprite sprite) {
            switch (team) {
                case Team.Player:
                    _playerSprite = sprite;
                    break;
                case Team.AI:
                    _aiSprite = sprite;
                    break;
            }

            TeamSpriteChanged?.Invoke(team, GetTeamSprite(team));
        }

        /// <summary>
        /// Gets the persistent save path for a team's custom image.
        /// </summary>
        /// <param name="team">The team being saved.</param>
        /// <returns>The full image save path.</returns>
        private static string GetSavedImagePath(Team team) {
            var fileName = team == Team.Player ? PlayerImageFileName : AiImageFileName;
            return Path.Combine(Application.persistentDataPath, fileName);
        }

        #endregion
        #region Texture Helpers

        /// <summary>
        /// Resizes very large textures to keep runtime images small and consistent.
        /// </summary>
        /// <param name="source">The source texture.</param>
        /// <returns>The original texture or a resized copy.</returns>
        private Texture2D ResizeIfNeeded(Texture2D source) {
            var largestSide = Mathf.Max(source.width, source.height);
            if (largestSide <= maxTextureSize) return source;

            var scale = maxTextureSize / (float)largestSide;
            var width = Mathf.Max(1, Mathf.RoundToInt(source.width * scale));
            var height = Mathf.Max(1, Mathf.RoundToInt(source.height * scale));
            var resized = new Texture2D(width, height, TextureFormat.RGBA32, false);

            for (var y = 0; y < height; y++) {
                for (var x = 0; x < width; x++) {
                    var u = width > 1 ? x / (float)(width - 1) : 0f;
                    var v = height > 1 ? y / (float)(height - 1) : 0f;
                    resized.SetPixel(x, y, source.GetPixelBilinear(u, v));
                }
            }

            resized.Apply();
            return resized;
        }

        #endregion
    }
}
