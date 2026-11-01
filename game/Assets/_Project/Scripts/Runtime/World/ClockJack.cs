using UnityEngine;

namespace MyGame
{
    // A clock jack on the bell gallery (a trap): a squat automaton by the wall that strikes the quarters, its hammer
    // coming down on the bronze strip in the floor before it. Running, it keeps the Hall's time (HallClock): once a bar,
    // on its own beat, the hammer falls and rings the strip like a bell; then it's wound up again, slowly (a ratchet),
    // held high, leaning back, shivering a moment (the tell), and brought down. What's on the strip as it falls is
    // struck down and thrown aside. Stopped, it's held where it was: hammer high, as the clock stopped.
    public sealed class ClockJack : MonoBehaviour
    {
        public Clockwork clockwork;
        public Transform hammer;                 // its arms and hammer (turning at its shoulders)
        public Vector3 axis = Vector3.right;     // world: the hammer turns about it (down, the jack's way)
        public float leanBack = 8f, struck = -128f; // degrees from raised straight up
        public int beat;                         // which beat of the bar it strikes on
        public Vector3 strip;                    // the strip's middle (world, on the floor)
        public Vector2 stripHalf = new Vector2(0.6f, 0.8f); // its half-size (e, n)
        public float knock = 22f;
        public float startAt = 3.2f;             // where in its round the clock stopped it (held high)
        public AudioClip[] windSound, strikeSound;

        public const float Round = HallClock.Beat * HallClock.BeatsInBar; // (a bar: it strikes once in it)
        const float Rest = 0.45f, Lift = 2.1f, Fall = 3.82f;              // its round: down, wound up, held (the tell), falling

        public float Cycle { get; private set; }   // 0 the blow .. Round
        public float Angle { get; private set; }
        public int Strikes { get; private set; }   // (tests)
        public bool Raised => Cycle >= Lift && Cycle < Fall; // (tests) held high: the tell
        public TrapBlows Blows { get; } = new TrapBlows();

        Quaternion rest;
        bool wound;

        void Start()
        {
            if (hammer != null)
                rest = hammer.rotation;
            Cycle = startAt;
            Pose();
        }

        void Update()
        {
            float run = clockwork != null ? UiKit.Smooth(0f, 1f, clockwork.Run) : 1f;
            if (run <= 0f)
                return;
            float step = Time.deltaTime * run;
            if (clockwork != null && clockwork.Forever)
            {
                // Going for good, it falls into step with the Hall's beat (drawn to it over a few seconds, not jumping).
                float behind = Mathf.Repeat((float)(HallClock.Now - beat * HallClock.Beat) - Cycle + Round * 0.5f, Round) - Round * 0.5f;
                step = Mathf.Max(0f, step + behind * Mathf.Min(1f, Time.deltaTime * 1.5f));
            }
            bool blow = Cycle + step >= Round; // (round past the blow)
            Cycle = Mathf.Repeat(Cycle + step, Round);
            if (!wound && Cycle >= Rest && Cycle < Lift)
            {
                wound = true;
                Sfx.Instance?.Attack(windSound, transform.position + Vector3.up * 1.5f, 0.8f, "trap_jack_wind");
            }
            Pose();
            // Falling, low (the hammer's down past level), or the blow itself: what's on the strip is struck.
            if (blow || (Cycle >= Fall && Angle < -95f))
                Blows.Sweep((feet, r) => Mathf.Abs(feet.x - strip.x) <= stripHalf.x + r && Mathf.Abs(feet.z - strip.z) <= stripHalf.y + r
                                         && feet.y < strip.y + 1.2f && feet.y > strip.y - 0.6f,
                            feet => new Vector3(Mathf.Sign(feet.x - strip.x + 0.001f) * knock, 0f, -knock * 0.35f));
            if (blow)
                Strike();
        }

        void Strike()
        {
            Strikes++;
            wound = false;
            Sfx.Instance?.Attack(strikeSound, strip + Vector3.up * 0.2f, 1f, "trap_jack_strike");
            Effects.Instance?.BlockSpark(strip + Vector3.up * 0.15f, Vector3.up);
            var apprentice = FindAnyObjectByType<Apprentice>();
            if (apprentice != null && (apprentice.transform.position - strip).sqrMagnitude < 9f * 9f)
                CameraRig.Instance?.Shake(0.12f);
        }

        // The hammer where its round has it: down on the strip, wound up, held high leaning back (shivering at the
        // last), falling faster and faster.
        void Pose()
        {
            float c = Cycle;
            if (c < Rest)
                Angle = struck;
            else if (c < Lift)
                Angle = Mathf.Lerp(struck, 0f, UiKit.Smooth(0f, 1f, (c - Rest) / (Lift - Rest)));
            else if (c < Fall)
            {
                float u = (c - Lift) / (Fall - Lift);
                float shiver = u > 0.6f ? Mathf.Sin(c * 70f) * 1.6f * (u - 0.6f) / 0.4f : 0f;
                Angle = leanBack * UiKit.Smooth(0f, 1f, Mathf.Min(1f, u * 2f)) + shiver;
            }
            else
            {
                float u = (c - Fall) / (Round - Fall);
                Angle = Mathf.Lerp(leanBack, struck, u * u);
            }
            if (hammer != null)
                hammer.rotation = Quaternion.AngleAxis(Angle, axis) * rest;
        }
    }
}
