using UnityEngine;

namespace MyGame
{
    // One-shot sound effects from art/audio (rendered by tools/build-audio.sh). Picks a random variant (never the
    // same one twice in a row) with a little pitch and volume variation, from a small pool of 3D sources.
    public sealed class Sfx : MonoBehaviour
    {
        public static Sfx Instance { get; private set; }

        public AudioClip[] oarSwing, oarSwingHeavy, dash, blockClang, parry, soulAbsorb;
        [Range(0f, 0.3f)] public float pitchJitter = 0.07f;
        [Range(0f, 0.3f)] public float volumeJitter = 0.1f;

        public string LastPlayed { get; private set; } = ""; // for tests: batch mode may have no audio device
        public static event System.Action<AudioClip, AudioSource> Played; // lets a recording log every sound

        const int Voices = 12;
        AudioSource[] voices;
        int nextVoice;
        readonly System.Collections.Generic.Dictionary<AudioClip[], int> lastIndex = new System.Collections.Generic.Dictionary<AudioClip[], int>();

        void Awake()
        {
            Instance = this;
            voices = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
            {
                var go = new GameObject("Sfx Voice");
                go.transform.SetParent(transform, false);
                voices[i] = go.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
                voices[i].spatialBlend = 0.7f; // mostly positional, never so much that a sound vanishes
                voices[i].rolloffMode = AudioRolloffMode.Linear;
                voices[i].minDistance = 3f;
                voices[i].maxDistance = 25f;
                voices[i].dopplerLevel = 0f;
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void OarSwing(Vector3 at, bool heavy) => Play(heavy ? oarSwingHeavy : oarSwing, at, heavy ? 0.9f : 0.75f, heavy ? "oar_swing_heavy" : "oar_swing");
        public void Dash(Vector3 at) => Play(dash, at, 0.7f, "dash");
        public void BlockClang(Vector3 at, float volume = 1f) => Play(blockClang, at, volume, "block_clang");
        public void Parry(Vector3 at) => Play(parry, at, 1f, "parry");
        public void SoulAbsorb(Vector3 at) => Play(soulAbsorb, at, 0.9f, "soul_absorb");

        void Play(AudioClip[] set, Vector3 at, float volume, string label)
        {
            LastPlayed = label;
            if (set == null || set.Length == 0)
                return;
            int index = Random.Range(0, set.Length);
            if (set.Length > 1 && lastIndex.TryGetValue(set, out int last) && index == last)
                index = (index + 1) % set.Length;
            lastIndex[set] = index;

            var voice = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            voice.transform.position = at;
            voice.clip = set[index];
            voice.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            voice.volume = volume * (1f + Random.Range(-volumeJitter, volumeJitter));
            voice.Play();
            Played?.Invoke(voice.clip, voice);
        }
    }
}
