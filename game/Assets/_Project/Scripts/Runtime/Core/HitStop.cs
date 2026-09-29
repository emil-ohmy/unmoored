using UnityEngine;

namespace MyGame
{
    // Also slow motion (the Apprentice's fall): time slowed, easing back to normal by the end.
    // Global hit-stop: nearly freezes time for a few real-time milliseconds on impact. Its clock is real time, except
    // when the game is stepped at a fixed rate (tests, recordings: Time.captureDeltaTime), when it counts those steps,
    // so a hit-stop lasts the same number of frames however fast the frames render.
    public sealed class HitStop : MonoBehaviour
    {
        const float FrozenScale = 0.02f;
        static HitStop instance;
        float until, slowFrom, slowUntil, slowScale = 1f;
        static float clock;

        public static float Clock => clock;

        public static void Trigger(float seconds)
        {
            Ensure();
            instance.until = Mathf.Max(instance.until, clock + seconds);
        }

        // Time runs at `scale` at first, easing back to normal over `seconds` (of its clock).
        public static void Slow(float scale, float seconds)
        {
            Ensure();
            instance.slowScale = scale;
            instance.slowFrom = clock;
            instance.slowUntil = clock + seconds;
        }

        static void Ensure()
        {
            if (instance != null)
                return;
            instance = new GameObject("HitStop").AddComponent<HitStop>();
            DontDestroyOnLoad(instance.gameObject);
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (_, __) =>
            {
                if (instance != null)
                    instance.until = instance.slowUntil = 0f; // a new scene starts at full speed
            };
        }

        void Update()
        {
            clock += Time.captureDeltaTime > 0f ? Time.captureDeltaTime : Time.unscaledDeltaTime;
            float slow = clock < slowUntil ? Mathf.Lerp(slowScale, 1f, Mathf.Pow((clock - slowFrom) / (slowUntil - slowFrom), 3f)) : 1f;
            Time.timeScale = clock < until ? FrozenScale : slow;
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
            if (instance == this)
                instance = null;
        }
    }
}
