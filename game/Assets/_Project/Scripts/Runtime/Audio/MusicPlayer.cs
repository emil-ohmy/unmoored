using UnityEngine;

namespace MyGame
{
    // Background music: a looping, non-positional track that fades in when the scene starts.
    [RequireComponent(typeof(AudioSource))]
    public sealed class MusicPlayer : MonoBehaviour
    {
        [Range(0f, 1f)] public float volume = 0.4f;
        public float fadeIn = 4f;

        AudioSource source;

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
        }

        void Update() => source.volume = Mathf.MoveTowards(source.volume, volume, volume / Mathf.Max(fadeIn, 0.01f) * Time.unscaledDeltaTime);
    }
}
