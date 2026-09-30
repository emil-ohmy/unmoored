using UnityEngine;

namespace MyGame
{
    // The music: a looping, non-positional ambient track that fades in when the scene starts, and a Boss's own music
    // while its fight is on. The ambient gives way when a Boss fight begins; the Boss's track for its phase takes
    // over, and a new phase crossfades in step (its tracks share one loop, so the next starts where the last was).
    // Everything sinks while the Apprentice is dead (the death toll has the stage). A Boss beaten: its music stops,
    // a silence, then the ambient comes back.
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

        public static bool Hushed; // a quiet moment (a Crossing): the ambient sinks low
        [Range(0f, 1f)] public float whileHushed = 0.3f;

        public int FightPhase { get; private set; } // the Boss track playing now (0: none)
        public float AmbientTarget { get; private set; }

        AudioSource source;
        readonly AudioSource[] fight = new AudioSource[2];
        float ambientLevel;                               // the fades' own levels; the sources play them at the
        readonly float[] fightLevel = new float[2];       // player's music volume (Settings.Music)
        Apprentice apprentice;
        IBoss boss;
        float quietUntil;

        void Awake()
        {
            Hushed = false;
            source = GetComponent<AudioSource>();
            Setup(source);
            for (int i = 0; i < fight.Length; i++)
                Setup(fight[i] = gameObject.AddComponent<AudioSource>());
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
            int slot = boss != null ? FightPhase - 1 : -1;
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
            source.volume = ambientLevel * Settings.Music;
        }

        // The fight begins: its music for the phase it's in, from the top.
        void Begin(IBoss b)
        {
            FightPhase = Mathf.Clamp(b.Phase, 1, fight.Length);
            var clip = Clip(b, FightPhase);
            var s = fight[FightPhase - 1];
            if (clip == null)
                return;
            if (s.clip != clip || !s.isPlaying)
            {
                s.clip = clip;
                s.timeSamples = 0;
                s.Play();
            }
        }

        // A new phase: its track starts where the last one is in the loop, and they crossfade.
        void NextPhase(IBoss b)
        {
            int from = FightPhase - 1;
            FightPhase = Mathf.Clamp(b.Phase, 1, fight.Length);
            var clip = Clip(b, FightPhase);
            var s = fight[FightPhase - 1];
            if (clip == null)
                return;
            s.clip = clip;
            int samples = fight[from].isPlaying && fight[from].clip != null ? fight[from].timeSamples : 0;
            s.Play();
            s.timeSamples = Mathf.Clamp(samples, 0, clip.samples - 1);
        }

        static AudioClip Clip(IBoss b, int phase) =>
            b.Music != null && b.Music.Length >= phase ? b.Music[phase - 1] : null;
    }
}
