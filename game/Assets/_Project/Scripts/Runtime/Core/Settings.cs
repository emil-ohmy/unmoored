using UnityEngine;

namespace MyGame
{
    // The player's settings, kept on this machine (PlayerPrefs): master, music and effects volume, each 0..1 in tenths.
    public static class Settings
    {
        const string MasterKey = "settings.volume.master", MusicKey = "settings.volume.music", EffectsKey = "settings.volume.effects";

        public static float Master { get => PlayerPrefs.GetFloat(MasterKey, 1f); set => Set(MasterKey, value); }
        public static float Music { get => PlayerPrefs.GetFloat(MusicKey, 0.8f); set => Set(MusicKey, value); }
        public static float Effects { get => PlayerPrefs.GetFloat(EffectsKey, 1f); set => Set(EffectsKey, value); }

        static void Set(string key, float value)
        {
            PlayerPrefs.SetFloat(key, Mathf.Round(Mathf.Clamp01(value) * 10f) / 10f);
            PlayerPrefs.Save();
            Apply();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Apply() => AudioListener.volume = Master; // (music and effects scale their own sources)
    }

    // The game paused (the pause menu): time stops, the world's sounds hold, the Apprentice ignores the controls.
    public static class Pause
    {
        public static bool IsPaused { get; private set; }

        public static void Set(bool paused)
        {
            IsPaused = paused;
            AudioListener.pause = paused;
            Time.timeScale = paused ? 0f : 1f; // (HitStop keeps it there while paused)
        }
    }
}
