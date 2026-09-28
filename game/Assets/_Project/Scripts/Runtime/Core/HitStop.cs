using UnityEngine;

namespace MyGame
{
    // Global hit-stop: nearly freezes time for a few real-time milliseconds on impact. Its clock is real time, except
    // when the game is stepped at a fixed rate (tests, recordings: Time.captureDeltaTime), when it counts those steps,
    // so a hit-stop lasts the same number of frames however fast the frames render.
    public sealed class HitStop : MonoBehaviour
    {
        const float FrozenScale = 0.02f;
        static HitStop instance;
        float until;
        static float clock;

        public static float Clock => clock;

        public static void Trigger(float seconds)
        {
            if (instance == null)
            {
                instance = new GameObject("HitStop").AddComponent<HitStop>();
                DontDestroyOnLoad(instance.gameObject);
            }
            instance.until = Mathf.Max(instance.until, clock + seconds);
        }

        void Update()
        {
            clock += Time.captureDeltaTime > 0f ? Time.captureDeltaTime : Time.unscaledDeltaTime;
            Time.timeScale = clock < until ? FrozenScale : 1f;
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
            if (instance == this)
                instance = null;
        }
    }
}
