using UnityEngine;

namespace MyGame
{
    // The music: a looping, non-positional ambient track that fades in when the scene starts, and a Boss's own music
    // while its fight is on. The ambient gives way when a Boss fight begins; the Boss's track for its phase takes
    // over, and a new phase crossfades in step (its tracks share one loop, so the next starts where the last was).
    // Everything sinks while the Apprentice is dead (the death toll has the stage). A Boss beaten: its music stops,
    // a silence, then the ambient comes back. An Area that wakes (the Hall of the Last Hour) has its woken music too.
    [RequireComponent(typeof(AudioSource))]
    public sealed class MusicPlayer : MonoBehaviour
    {
        [Range(0f, 1f)] public float volume = 0.4f;
        [Range(0f, 1f)] public float fightVolume = 0.5f;
        public float fadeIn = 4f;
        [Range(0f, 1f)] public float whileDead = 0.15f;
        public float duckTime = 0.6f;
        public float phaseCrossfade = 2.5f;
        public float silenceAfterVictory = 5f;

        public AudioClip woken;               // the Area's music once it's woken (the Hall's, its engine set going): the
        public float wakeCrossfade = 4f;      // same loop as the ambient's, crossfaded in step
        public bool Woken { get; private set; }
        public float WakeMix => wake;         // (tests) 0 the ambient .. 1 the woken music

        public static bool Hushed; // a quiet moment (a Crossing): the ambient sinks low
        public static MusicPlayer Instance { get; private set; }

        // Seconds into the Area's music loop (the ambient's, or its woken one's: they keep step), or -1 when neither is
        // playing. The Hall of the Last Hour keeps its beat by it (HallClock).
        public double Clock
        {
            get
            {
                var s = wokenSource != null && wokenSource.isPlaying && wake > 0.5f ? wokenSource : source;
                if (s == null || !s.isPlaying || s.clip == null)
                    return -1.0;
                // (no audio at all, e.g. a test run muted: it says it's playing but never moves on; then it's no clock)
                if (s.timeSamples != clockSamples)
                {
                    clockSamples = s.timeSamples;
                    clockMoved = Time.unscaledTime;
                }
                return Time.unscaledTime - clockMoved < 0.3f ? (double)s.timeSamples / s.clip.frequency : -1.0;
            }
        }
        int clockSamples = -1;
        float clockMoved = -10f;
        [Range(0f, 1f)] public float whileHushed = 0.3f;

        public int FightPhase { get; private set; } // the Boss track playing now (0: none)
        public float AmbientTarget { get; private set; }

        AudioSource source, wokenSource;
        float wake;                                       // 0 the ambient as it was .. 1 woken
        readonly AudioSource[] fight = new AudioSource[2];
        int fightSlot;                                    // the player the phase now is on (they take turns)
        float ambientLevel;                               // the fades' own levels; the sources play them at the
        readonly float[] fightLevel = new float[2];       // player's music volume (Settings.Music)
        Apprentice apprentice;
        IBoss boss;
        float quietUntil;

        void Awake()
        {
            Instance = this;
            Hushed = false;
            source = GetComponent<AudioSource>();
            Setup(source);
            for (int i = 0; i < fight.Length; i++)
                Setup(fight[i] = gameObject.AddComponent<AudioSource>());
            Setup(wokenSource = gameObject.AddComponent<AudioSource>());
        }

        // The Area wakes (or, in a lab, sleeps again): its woken music takes over from where the ambient is in the loop.
        public void Wake(bool on = true)
        {
            if (woken == null || on == Woken)
                return;
            Woken = on;
            if (on && !wokenSource.isPlaying)
            {
                wokenSource.clip = woken;
                wokenSource.Play();
                wokenSource.timeSamples = Mathf.Clamp(source.isPlaying ? source.timeSamples : 0, 0, woken.samples - 1);
            }
        }

        static void Setup(AudioSource s)
        {
            s.loop = true;
            s.spatialBlend = 0f;
            s.volume = 0f;
            s.playOnAwake = false;
            s.ignoreListenerPause = true; // (the music plays on under the pause menu)
        }

        void Start()
        {
            if (source.clip != null)
                source.Play();
            apprentice = FindAnyObjectByType<Apprentice>();
        }

        void Update()
        {
            bool dead = apprentice != null && !apprentice.IsAlive;
            float duck = dead ? whileDead : Hushed ? whileHushed : 1f;
            var current = BossFight.Current;
            if (current != boss)
            {
                if (boss != null && !boss.IsAlive)
                    quietUntil = UiKit.Now + silenceAfterVictory; // beaten: let the silence ring
                boss = current;
                if (boss != null)
                    Begin(boss);
            }
            if (boss != null && boss.Phase != FightPhase && FightPhase > 0)
                NextPhase(boss);
            int slot = boss != null ? fightSlot : -1;
            bool victory = UiKit.Now < quietUntil;
            for (int i = 0; i < fight.Length; i++)
            {
                float target = i == slot ? fightVolume * duck : 0f;
                float time;
                if (target > fightLevel[i])
                    time = fightLevel[1 - i] > 0.01f ? phaseCrossfade : 1.5f; // swelling in: a new phase, or the fight
                else if (i == slot)
                    time = duckTime;                                           // ducking under the death toll
                else
                    time = boss != null ? phaseCrossfade : (victory ? 0.4f : 1.2f); // the old phase, or the fight's end
                fightLevel[i] = Mathf.MoveTowards(fightLevel[i], target, fightVolume / Mathf.Max(time, 0.01f) * UiKit.DeltaTime);
                fight[i].volume = fightLevel[i] * Settings.Music;
                if (fight[i].isPlaying && fightLevel[i] <= 0f && i != slot)
                    fight[i].Stop();
            }
            if (boss == null)
                FightPhase = 0;

            bool fightMusic = boss != null && boss.Music != null && boss.Music.Length > 0; // (a miniboss has none: the Area's plays on)
            AmbientTarget = fightMusic || UiKit.Now < quietUntil ? 0f : volume * duck;
            float ambientTime = ambientLevel > AmbientTarget ? (boss != null ? 1.5f : duckTime) : fadeIn;
            ambientLevel = Mathf.MoveTowards(ambientLevel, AmbientTarget, volume / Mathf.Max(ambientTime, 0.01f) * UiKit.DeltaTime);
            wake = Mathf.MoveTowards(wake, Woken ? 1f : 0f, UiKit.DeltaTime / Mathf.Max(wakeCrossfade, 0.01f));
            source.volume = ambientLevel * Mathf.Sqrt(1f - wake) * Settings.Music; // (equal power: no dip between them)
            wokenSource.volume = ambientLevel * Mathf.Sqrt(wake) * Settings.Music;
            if (wake <= 0f && wokenSource.isPlaying)
                wokenSource.Stop();
        }

        // The fight begins: its music for the phase it's in, from the top.
        void Begin(IBoss b)
        {
            FightPhase = Mathf.Clamp(b.Phase, 1, Mathf.Max(1, b.Music != null ? b.Music.Length : 1));
            var clip = Clip(b, FightPhase);
            var s = fight[fightSlot];
            if (clip == null)
                return;
            if (s.clip != clip || !s.isPlaying)
            {
                s.clip = clip;
                s.timeSamples = 0;
                s.Play();
            }
        }

        // A new phase: its track starts where the last one is in the loop, and they crossfade (the two players by turns,
        // however many phases a Boss has).
        void NextPhase(IBoss b)
        {
            int from = fightSlot;
            int phase = Mathf.Clamp(b.Phase, 1, Mathf.Max(1, b.Music != null ? b.Music.Length : 1));
            var clip = Clip(b, phase);
            if (phase == FightPhase || clip == null)
            {
                FightPhase = b.Phase;
                return;
            }
            FightPhase = phase;
            fightSlot = 1 - fightSlot;
            var s = fight[fightSlot];
            s.clip = clip;
            int samples = fight[from].isPlaying && fight[from].clip != null ? fight[from].timeSamples : 0;
            s.Play();
            s.timeSamples = Mathf.Clamp(samples, 0, clip.samples - 1);
        }

        static AudioClip Clip(IBoss b, int phase) =>
            b.Music != null && b.Music.Length >= phase ? b.Music[phase - 1] : null;
    }
}
