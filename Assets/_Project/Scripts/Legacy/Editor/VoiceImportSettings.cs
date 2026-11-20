using UnityEditor;

internal sealed class VoiceImportSettings : AssetPostprocessor
{
    private void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith("Assets/_Project/Art/Audio/Resources/Voices/",
            System.StringComparison.Ordinal)) return;
        var importer = (AudioImporter)assetImporter;
        importer.forceToMono = true;
        var settings = importer.defaultSampleSettings;
        settings.loadType = UnityEngine.AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = UnityEngine.AudioCompressionFormat.Vorbis;
        settings.quality = .6f;
        settings.preloadAudioData = true;
        importer.defaultSampleSettings = settings;
    }
}
