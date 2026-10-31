using UnityEngine;

namespace MyGame
{
    // The Hall of the Last Hour's time: stopped (as you find it), or running (its central engine set going). Stopped,
    // whatever isn't in time is drained to a cold grey (the Toon and Water shaders, from the globals set here: only the
    // things' own colour, so light and glow keep theirs, and the Apprentice's materials are never stilled), the dust
    // hangs, stirring only round the Apprentice (StillAir), and the music is the stopped one. Set going, the colour comes
    // back in a wave out from the engine, the dust falls where it passes, and the music wakes. Leaving the scene, the
    // globals go back to nothing, so no other Area is touched.
    public sealed class HallTime : MonoBehaviour
    {
        public static HallTime Instance { get; private set; }

        public Transform engine;                              // where the wave starts (the central engine; else here)
        public float waveSpeed = 12f;                         // metres a second
        public float waveWidth = 6f;                          // its soft edge
        public float reach = 140f;                            // past all of the Hall: the wave's done
        public float stopFade = 1.5f;                         // stopped again (the lab): back to grey over this long
        public Color tint = new Color(0.86f, 0.9f, 1f, 1f);   // the grey's cast: cold, silvery

        public bool Running { get; private set; }
        public float WaveRadius { get; private set; }
        public float Stillness { get; private set; } = 1f;    // everywhere, 0..1 (the wave aside)
        public Vector3 WaveCentre => engine != null ? engine.position : transform.position;
        public event System.Action<bool> Changed;             // running or not

        static readonly int StillId = Shader.PropertyToID("_HallStill"), WaveId = Shader.PropertyToID("_HallWave"),
                            WaveOnId = Shader.PropertyToID("_HallWaveOn"), WaveWidthId = Shader.PropertyToID("_HallWaveWidth"),
                            TintId = Shader.PropertyToID("_HallStillTint");

        void Awake()
        {
            Instance = this;
            Apply();
        }

        void OnEnable() => Instance = this;

        void OnDisable()
        {
            if (Instance == this)
                Instance = null;
            Shader.SetGlobalFloat(StillId, 0f);
            Shader.SetGlobalFloat(WaveOnId, 0f);
        }

        // The central engine set going: time again, out from it.
        public void Run()
        {
            if (Running)
                return;
            Running = true;
            WaveRadius = 0f;
            Stillness = 0f;
            MusicPlayer.Instance?.Wake(true);
            Changed?.Invoke(true);
        }

        // Held still again (the lab's: the game never stops it once it's going).
        public void Stop()
        {
            if (!Running)
                return;
            Running = false;
            Stillness = 0f;
            MusicPlayer.Instance?.Wake(false);
            Changed?.Invoke(false);
        }

        // Whether time is stopped at `point` (stopped everywhere; running, until the wave has passed it).
        public bool StoppedAt(Vector3 point)
        {
            if (!Running)
                return true;
            Vector3 d = point - WaveCentre;
            d.y = 0f;
            return d.magnitude > WaveRadius - waveWidth * 0.5f;
        }

        void Update()
        {
            if (Running)
                WaveRadius = Mathf.Min(reach, WaveRadius + waveSpeed * Time.deltaTime);
            else
                Stillness = Mathf.MoveTowards(Stillness, 1f, Time.deltaTime / Mathf.Max(0.01f, stopFade));
            Apply();
        }

        void Apply()
        {
            Vector3 c = WaveCentre;
            Shader.SetGlobalFloat(StillId, Running ? 0f : Stillness);
            Shader.SetGlobalFloat(WaveOnId, Running && WaveRadius < reach ? 1f : 0f);
            Shader.SetGlobalVector(WaveId, new Vector4(c.x, c.y, c.z, WaveRadius));
            Shader.SetGlobalFloat(WaveWidthId, waveWidth);
            Shader.SetGlobalColor(TintId, tint);
        }

        // A colour as time stopped shows it: its brightness only, in the cold grey.
        public Color Stilled(Color colour)
        {
            float l = colour.r * 0.3f + colour.g * 0.59f + colour.b * 0.11f;
            return new Color(l * tint.r, l * tint.g, l * tint.b, colour.a);
        }
    }
}
