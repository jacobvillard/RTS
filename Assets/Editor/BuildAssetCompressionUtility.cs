using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Applies mobile-friendly import settings to large audio and texture assets.
/// </summary>
public static class BuildAssetCompressionUtility {

    #region Constants

    private const int LongAudioSeconds = 10;
    private const float MusicQuality = 0.35f;
    private const float SfxQuality = 0.5f;
    private const uint MusicSampleRate = 22050;
    private const uint SfxSampleRate = 22050;
    private const int LargeTextureBytes = 1024 * 1024;
    private const int AndroidTextureMaxSize = 2048;

    #endregion
    #region Menu

    /// <summary>
    /// Applies import settings to audio and texture assets, then saves the updated metadata.
    /// </summary>
    [MenuItem("Tools/Build/Apply Mobile Compression Settings")]
    public static void ApplyMobileCompressionSettings() {
        var changedAssets = 0;

        changedAssets += CompressAudioAssets();
        changedAssets += CompressTextureAssets();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Applied mobile compression settings to {changedAssets} assets.");
    }

    #endregion
    #region Audio

    /// <summary>
    /// Applies streaming Vorbis settings to music and compressed mono settings to short sound effects.
    /// </summary>
    /// <returns>The number of audio importers updated.</returns>
    private static int CompressAudioAssets() {
        var changedAssets = 0;
        var audioGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Sounds" });

        foreach (var guid in audioGuids) {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetImporter.GetAtPath(path) is not AudioImporter importer) continue;

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            var isLongAudio = clip != null && clip.length >= LongAudioSeconds;

            Undo.RecordObject(importer, "Apply Mobile Audio Compression");
            ApplyAudioSettings(importer, isLongAudio);

            importer.SaveAndReimport();
            changedAssets++;
        }

        return changedAssets;
    }

    /// <summary>
    /// Applies audio settings based on whether the clip is music/ambience or a short sound effect.
    /// </summary>
    /// <param name="importer">The audio importer to update.</param>
    /// <param name="isLongAudio">Whether the clip should stream as music or ambience.</param>
    private static void ApplyAudioSettings(AudioImporter importer, bool isLongAudio) {
        var settings = importer.defaultSampleSettings;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = isLongAudio ? MusicQuality : SfxQuality;
        settings.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
        settings.sampleRateOverride = isLongAudio ? MusicSampleRate : SfxSampleRate;
        settings.loadType = isLongAudio ? AudioClipLoadType.Streaming : AudioClipLoadType.CompressedInMemory;
        settings.preloadAudioData = !isLongAudio;

        importer.defaultSampleSettings = settings;
        importer.forceToMono = !isLongAudio;
        importer.loadInBackground = isLongAudio;
    }

    #endregion
    #region Textures

    /// <summary>
    /// Applies Android texture compression and size limits to large texture assets.
    /// </summary>
    /// <returns>The number of texture importers updated.</returns>
    private static int CompressTextureAssets() {
        var changedAssets = 0;
        var textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });

        foreach (var guid in textureGuids) {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.StartsWith("Assets/Editor/")) continue;
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;

            var fileInfo = new FileInfo(path);
            if (!fileInfo.Exists || fileInfo.Length < LargeTextureBytes) continue;

            Undo.RecordObject(importer, "Apply Mobile Texture Compression");
            ApplyAndroidTextureSettings(importer, fileInfo.Length);

            importer.SaveAndReimport();
            changedAssets++;
        }

        return changedAssets;
    }

    /// <summary>
    /// Applies Android override settings that keep alpha-capable sprites compatible with mobile GPUs.
    /// </summary>
    /// <param name="importer">The texture importer to update.</param>
    /// <param name="fileSizeBytes">The source texture file size.</param>
    private static void ApplyAndroidTextureSettings(TextureImporter importer, long fileSizeBytes) {
        importer.isReadable = false;

        var androidSettings = importer.GetPlatformTextureSettings("Android");
        androidSettings.overridden = true;
        androidSettings.maxTextureSize = AndroidTextureMaxSize;
        androidSettings.format = importer.DoesSourceTextureHaveAlpha()
            ? TextureImporterFormat.ETC2_RGBA8
            : TextureImporterFormat.ETC2_RGB4;
        androidSettings.textureCompression = TextureImporterCompression.Compressed;
        androidSettings.compressionQuality = 50;
        androidSettings.crunchedCompression = false;

        importer.SetPlatformTextureSettings(androidSettings);
    }

    #endregion
}
