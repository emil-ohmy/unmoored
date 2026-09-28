using UnityEngine;

namespace MyGame
{
    // One-shot sound effects from art/audio (rendered by tools/build-audio.sh). Picks a random variant (never the
    // same one twice in a row) with a little pitch and volume variation, from a small pool of 3D sources.
    public sealed class Sfx : MonoBehaviour
    {
        public static Sfx Instance { get; private set; }

        public AudioClip[] oarSwing, oarSwingHeavy, dash, blockClang, parry, soulAbsorb, deathToll, lanternLit, oarHit, hurt, obolClink, charmFit, uiTick, heal, splash, arrowThunk, gateOpen, gateRattle;
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
        public void DeathToll(Vector3 at) => Play(deathToll, at, 1f, "death_toll");
        public void OarHit(Vector3 at, bool heavy) => Play(oarHit, at, heavy ? 1f : 0.8f, "oar_hit", heavy ? 0.82f : 1f);
        public void Hurt(Vector3 at) => Play(hurt, at, 0.9f, "apprentice_hurt");
        public void Heal(Vector3 at) => Play(heal, at, 0.8f, "heal");
        public void Splash(Vector3 at) => Play(splash, at, 1f, "splash");
        public void ArrowThunk(Vector3 at) => Play(arrowThunk, at, 0.8f, "arrow_thunk");
        public void GateOpen(Vector3 at) => Play(gateOpen, at, 1f, "gate_open");
        public void GateRattle(Vector3 at) => Play(gateRattle, at, 0.8f, "gate_rattle");
        public void ObolClink(Vector3 at) => Play(obolClink, at, 0.8f, "obol_clink");
        public void CharmFit(Vector3 at) => Play(charmFit, at, 0.8f, "charm_fit");
        public void UiTick()
        {
            if (listener == null)
                listener = FindAnyObjectByType<AudioListener>();
            Play(uiTick, listener != null ? listener.transform.position : Vector3.zero, 0.35f, "ui_tick");
        }

        AudioListener listener;
        public void LanternLit(Vector3 at) => Play(lanternLit, at, 0.8f, "lantern_lit");

        // A Soured's attack sounds (each attack carries its own clips). Returns the voice, so a tell can be cut short.
        public AudioSource Attack(AudioClip[] set, Vector3 at, float volume, string label, float pitch = 1f) =>
            Play(set, at, volume, label, pitch);

        AudioSource Play(AudioClip[] set, Vector3 at, float volume, string label, float pitch = 1f)
        {
            LastPlayed = label;
            if (set == null || set.Length == 0)
                return null;
            int index = Random.Range(0, set.Length);
            if (set.Length > 1 && lastIndex.TryGetValue(set, out int last) && index == last)
                index = (index + 1) % set.Length;
            lastIndex[set] = index;

            if (set[index] == null)
                return null; // a missing clip (e.g. a scene built before its sound was imported): stay silent
            var voice = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            voice.transform.position = at;
            voice.clip = set[index];
            voice.pitch = pitch * (1f + Random.Range(-pitchJitter, pitchJitter));
            voice.volume = volume * (1f + Random.Range(-volumeJitter, volumeJitter));
            voice.Play();
            Played?.Invoke(voice.clip, voice);
            return voice;
        }
    }
}
