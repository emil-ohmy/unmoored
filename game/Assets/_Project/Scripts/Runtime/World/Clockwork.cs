using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    // A piece of the Hall of the Last Hour's stopped clockwork (a drive gear, the pendulum): the Stroke's wave reaching
    // its strike-plate (or sent from on it, whichever way) sets it running, a while (runTime), then it winds down and stops again where it is. While it runs, what it
    // turns turns (spinners), what swings swings (swingers: a pendulum from where it was held), what it drives moves
    // (ClockworkMover: a door rolls aside, a lift climbs; and back as it stops), and time moves round it: the dust and
    // drops held in the air nearby (StillAir) drift and fall a little, then hang still again. Struck again while it runs,
    // it runs on from then. Heard: a clunk as it catches, its ticking while it runs, a slowing as it stops. Once the
    // Hall's time is set going (its central engine), every machine of it runs for good (RunForever).
    public sealed class Clockwork : MonoBehaviour, IStoodOnWaveable
    {
        [System.Serializable]
        public sealed class Spinner
        {
            public Transform part;
            public Vector3 axis = Vector3.up;  // world
            public float speed = 40f;          // degrees a second, running
        }

        [System.Serializable]
        public sealed class Swinger
        {
            public Transform part;
            public Vector3 axis = Vector3.forward; // world
            public float amplitude = 12f;          // degrees either side
            public float period = 2.2f;
            public float heldAt;                   // degrees from hanging straight, where it was stopped
            [HideInInspector] public Quaternion rest;
            [HideInInspector] public float angle, held; // (where it is now; where it was held, from its rest)
        }

        public Transform strikePoint;          // where the wave must reach (on the floor by it)
        public float strikeRadius = 1.2f;
        public float runTime = 8f;
        public float spinUp = 0.6f, spinDown = 1.4f;
        public Spinner[] spinners = new Spinner[0];
        public Swinger[] swingers = new Swinger[0];
        public float thawRadius = 8f;          // the still air this near it moves while it runs
        public AudioClip[] startSound, tickSound, stopSound;

        public bool Running => Forever || runUntil > Time.time;
        public bool Forever { get; private set; }  // (time running in the Hall: it never stops again)
        public event System.Action Started;
        public float Run { get; private set; }     // 0 stopped .. 1 running (eased up and down)
        public int Starts { get; private set; }    // (tests)

        float runUntil = -10f, swingTime;
        AudioSource ticking;
        StillAir[] air;
        bool wasRunning;

        void OnEnable() => Waveables.Register(this);
        void OnDisable() => Waveables.Unregister(this);

        public Vector3 WavePoint => strikePoint != null ? strikePoint.position : transform.position;
        public float WaveRadius => strikeRadius;

        void Start()
        {
            foreach (var s in swingers)
                if (s.part != null)
                {
                    s.rest = Quaternion.AngleAxis(-s.heldAt, s.axis) * s.part.rotation; // (hanging straight)
                    s.held = s.angle = s.heldAt;
                }
            var near = new List<StillAir>();
            foreach (var a in FindObjectsByType<StillAir>(FindObjectsSortMode.None))
                if (Vector3.Distance(a.transform.position, WavePoint) < thawRadius + Mathf.Max(a.size.x, a.size.z) * 0.5f)
                    near.Add(a);
            air = near.ToArray();
            ticking = gameObject.AddComponent<AudioSource>();
            ticking.loop = true;
            ticking.playOnAwake = false;
            ticking.spatialBlend = 1f;
            ticking.rolloffMode = AudioRolloffMode.Linear;
            ticking.minDistance = 4f;
            ticking.maxDistance = 26f;
            ticking.dopplerLevel = 0f;
        }

        // The Stroke's wave reaching it: it runs (on again, if it was running).
        public void TakeWave(StrokeWave wave, Vector3 push) => Set();

        public void Set()
        {
            if (!Running)
            {
                Starts++;
                swingTime = 0f; // (it swings on from where it was held)
                Sfx.Instance?.Attack(startSound, WavePoint + Vector3.up, 1f, "clockwork_start");
                Started?.Invoke();
                if (tickSound != null && tickSound.Length > 0 && ticking != null)
                {
                    ticking.clip = tickSound[0];
                    ticking.volume = Settings.Effects * 0.8f;
                    ticking.Play();
                }
            }
            runUntil = Time.time + runTime;
        }

        // For good: time running again in the Hall.
        public void RunForever(bool atOnce = false)
        {
            if (!Forever)
                Set();
            Forever = true;
            if (atOnce)
                Run = 1f; // (a save loaded: already going)
        }

        void Update()
        {
            float dt = Time.deltaTime;
            bool running = Running;
            if (wasRunning && !running)
            {
                Sfx.Instance?.Attack(stopSound, WavePoint + Vector3.up, 0.9f, "clockwork_stop");
                if (ticking != null)
                    ticking.Stop();
            }
            wasRunning = running;
            Run = Mathf.MoveTowards(Run, running ? 1f : 0f, dt / (running ? spinUp : spinDown));
            if (Run <= 0f)
                return;
            float run = UiKit.Smooth(0f, 1f, Run);
            foreach (var s in spinners)
                if (s.part != null)
                    s.part.Rotate(s.axis, s.speed * run * dt, Space.World);
            // A pendulum swings from where it was held, as if it had never stopped; slowing, it stops where it is.
            swingTime += dt * run;
            foreach (var s in swingers)
            {
                if (s.part == null)
                    continue;
                float target = s.amplitude * Mathf.Sin(2f * Mathf.PI * swingTime / s.period + Mathf.Asin(Mathf.Clamp(s.held / Mathf.Max(0.01f, s.amplitude), -1f, 1f)));
                s.angle = Mathf.Lerp(s.held, target, run);
                s.part.rotation = Quaternion.AngleAxis(s.angle, s.axis) * s.rest;
                if (!running)
                    s.held = s.angle; // (wherever it comes to rest, it's held there)
            }
            if (air != null)
                foreach (var a in air)
                    if (a != null)
                        a.Thaw(WavePoint, thawRadius, run, dt);
        }
    }
}
