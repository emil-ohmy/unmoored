using UnityEngine;

namespace MyGame
{
    // Background music: a looping, non-positional track that fades in when the scene starts. It sinks away while the
    // Apprentice is dead (the death toll has the stage) and swells back in after waking.
    [RequireComponent(typeof(AudioSource))]
    public sealed class MusicPlayer : MonoBehaviour
    {
        [Range(0f, 1f)] public float volume = 0.4f;
        public float fadeIn = 4f;
        [Range(0f, 1f)] public float whileDead = 0.15f;
        public float duckTime = 0.6f;

        AudioSource source;
        Apprentice apprentice;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 0f;
            source.playOnAwake = false;
        }

        void Start()
        {
            if (source.clip != null)
                source.Play();
            apprentice = FindAnyObjectByType<Apprentice>();
        }

        void Update()
        {
            float target = apprentice != null && !apprentice.IsAlive ? volume * whileDead : volume;
            float time = source.volume > target ? duckTime : fadeIn;
            source.volume = Mathf.MoveTowards(source.volume, target, volume / Mathf.Max(time, 0.01f) * Time.unscaledDeltaTime);
        }
    }
}
