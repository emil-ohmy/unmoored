using UnityEngine;

namespace MyGame
{
    // A Wound: couldn't let go of more time. One of the household's hounds, a wind-up key in its back. Wound tight it
    // runs at you fast, bites in flurries and pounces from a distance; its spring runs down as it goes (and with every
    // bite), and everything about it slows with it (its pace: its running, its tells, its blows), till it sags onto its
    // haunches and winds itself again, a few seconds sat still, cranking the key round: the time to punish it. Struck
    // while winding it starts again. The Stroke's wave knocks its key loose: run down at once, and longer to wind.
    public sealed class Wound : Soured
    {
        [Header("The spring")]
        public float runTime = 7f;              // seconds from wound tight to run down, while it's after you
        public float biteCost = 0.05f, pounceCost = 0.12f;
        public float runDownAt = 0.12f;
        public float slowest = 0.3f, fastest = 1.15f; // its pace, run down .. wound tight
        public float runDownTime = 1.0f, windTime = 2.4f, looseWindTime = 4.0f;

        [Header("The pounce")]
        public SouredAttack pounce = new SouredAttack
        {
            name = "Pounce", windupClip = "Gather", strikeClip = "Pounce", recoverClip = "PounceRecover",
            telegraph = 0.45f, strike = 0.45f, recover = 0.7f, impactAt = 0.7f,
            lunge = 0.5f, maxLunge = 6f, trackUntil = 0.8f, damage = 14f, reach = 1.6f, arc = 45f, knockback = 7f,
        };
        public float pounceFrom = 3.2f, pounceTo = 6.5f, pounceAbove = 0.5f, pounceEvery = 2.5f;

        [Header("Its key")]
        public Transform key;
        public float keySpin = 900f, windSpin = 540f;

        [Header("Sounds")]
        public AudioClip whirr;                  // the spring running (a loop; its pitch follows its pace)
        public AudioClip[] runDownSound, windSound, woundSound, looseSound;

        public float Tension { get; private set; } = 1f; // 1 wound tight .. 0 run down
        public bool KeyLoose { get; private set; }
        public bool Winding => phase == Phase.Winding && CurrentState == State.Busy;
        public int Winds { get; private set; }            // (tests) times it's wound itself again
        public float Speed => Pace;                        // (tests)

        enum Phase { Running, RunningDown, Winding }
        Phase phase;
        float windEnds, lastPounce = -10f, keyAngle;
        AudioSource running;

        bool Hunting => CurrentState == State.Chase || CurrentState == State.Telegraph || CurrentState == State.Strike
                        || CurrentState == State.Recover;

        // Its time runs with its spring: tight, a little quick; slowing all the way; near run down, under half.
        protected override float Pace => Hunting ? Mathf.Lerp(slowest, fastest, Tension) : 1f;

        protected override string ChaseClip(float distance) => "Run";

        protected override void Start()
        {
            base.Start();
            running = gameObject.AddComponent<AudioSource>();
            running.clip = whirr;
            running.loop = true;
            running.playOnAwake = false;
            running.spatialBlend = 1f;
            running.rolloffMode = AudioRolloffMode.Linear;
            running.minDistance = 2f;
            running.maxDistance = 18f;
            running.dopplerLevel = 0f;
        }

        protected override SouredAttack ChooseAttack(Vector3 toTarget, float distance)
        {
            if (distance <= attackRange)
                return primary;
            if (Tension >= pounceAbove && distance >= pounceFrom && distance <= pounceTo && Time.time - lastPounce > pounceEvery)
            {
                lastPounce = Time.time;
                return pounce;
            }
            return null;
        }

        protected override void OnEntered(State state)
        {
            if (state == State.Strike && CurrentAttack != null)
                Spend(CurrentAttack == pounce ? pounceCost : biteCost);
        }

        void Spend(float amount) => Tension = Mathf.Max(0f, Tension - amount);

        protected override void Tick(float dt)
        {
            if (Hunting && phase == Phase.Running)
                Spend(Time.deltaTime / Mathf.Max(0.1f, runTime));
            if (CurrentState == State.Chase)
            {
                if (phase == Phase.Running && Tension <= runDownAt)
                {
                    phase = Phase.RunningDown;
                    BeBusy("RunDown", runDownTime);
                    Sfx.Instance?.Attack(runDownSound, transform.position + Vector3.up * 0.6f, 1f, "wound_rundown");
                }
                else if (phase == Phase.RunningDown)
                    Wind();
                else if (phase == Phase.Winding)
                {
                    if (Time.time >= windEnds - 0.05f)
                        WoundTight();
                    else
                        Wind(); // (struck while winding: it starts again)
                }
            }
            clips.SetTempo(Pace);
            Whirr();
        }

        void Wind()
        {
            phase = Phase.Winding;
            float time = KeyLoose ? looseWindTime : windTime;
            windEnds = Time.time + time;
            BeBusy("Wind", time);
            Sfx.Instance?.Attack(windSound, transform.position + Vector3.up * 0.7f, 1f, "wound_wind");
        }

        void WoundTight()
        {
            phase = Phase.Running;
            Tension = 1f;
            KeyLoose = false;
            Winds++;
            Sfx.Instance?.Attack(woundSound, transform.position + Vector3.up * 0.7f, 1f, "wound_wound");
        }

        // The Stroke's wave knocks its key loose: run down at once; winding it back takes longer.
        public override HitResult TakeWave(Vector3 push, float damage, Vector3 from, float stagger)
        {
            var result = base.TakeWave(push, damage, from, stagger);
            if (result == HitResult.Landed && IsAlive)
            {
                Tension = 0f;
                KeyLoose = true;
                phase = Phase.RunningDown;
                Sfx.Instance?.Attack(looseSound, transform.position + Vector3.up * 0.7f, 1f, "wound_loose");
            }
            return result;
        }

        void Whirr()
        {
            if (running == null || whirr == null)
                return;
            bool on = Hunting && IsAlive;
            if (on && !running.isPlaying)
                running.Play();
            else if (!on && running.isPlaying)
                running.Stop();
            running.pitch = Pace;
            running.volume = Settings.Effects * (0.25f + 0.45f * Tension);
        }

        // The key turns as the spring runs (fast when tight, slowing), and is cranked round the other way as it winds.
        void LateUpdate()
        {
            if (key == null)
                return;
            if (Winding)
                keyAngle -= windSpin * Time.deltaTime;
            else if (Hunting)
                keyAngle += keySpin * Tension * Time.deltaTime;
            float wobble = KeyLoose ? 18f * Mathf.Sin(Time.time * 23f) : 0f;
            key.rotation = Quaternion.AngleAxis(keyAngle, transform.up) * Quaternion.AngleAxis(wobble, transform.right) * key.rotation;
        }

        public override void Reform()
        {
            base.Reform();
            Tension = 1f;
            KeyLoose = false;
            phase = Phase.Running;
        }
    }
}
