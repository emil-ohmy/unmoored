using UnityEngine;

namespace MyGame
{
    // Global hit-stop: nearly freezes time for a few real-time milliseconds on impact.
    public sealed class HitStop : MonoBehaviour
    {
        const float FrozenScale = 0.02f;
        static HitStop instance;
        float until;

        public static void Trigger(float seconds)
        {
            if (instance == null)
            {
                instance = new GameObject("HitStop").AddComponent<HitStop>();
                DontDestroyOnLoad(instance.gameObject);
            }
            instance.until = Mathf.Max(instance.until, Time.unscaledTime + seconds);
        }

        void Update() => Time.timeScale = Time.unscaledTime < until ? FrozenScale : 1f;

        void OnDestroy()
        {
            Time.timeScale = 1f;
            if (instance == this)
                instance = null;
        }
    }
}
