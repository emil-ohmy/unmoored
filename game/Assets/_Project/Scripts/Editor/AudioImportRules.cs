using UnityEditor;
using UnityEngine;

// Import rules for sounds rendered by tools/build-audio.sh into Assets/_Project/Audio/:
// short effects decompress on load (instant playback); music in Audio/Music/ streams from disk as Vorbis.
public class AudioImportRules : AssetPostprocessor
{
    void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith("Assets/_Project/Audio/"))
            return;
        var importer = (AudioImporter)assetImporter;
        bool music = assetPath.StartsWith("Assets/_Project/Audio/Music/");
        var settings = importer.defaultSampleSettings;
        settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = music ? 0.55f : 0.8f;
        importer.defaultSampleSettings = settings;
        importer.loadInBackground = music;
    }
}
