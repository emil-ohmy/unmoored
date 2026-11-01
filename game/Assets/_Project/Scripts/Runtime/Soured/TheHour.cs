using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    // The One Who Waits: the Hour, the Hall of the Last Hour's Boss (docs/brief.md). The Hall's last hour, which never
    // struck, given a body: robed, its face a dial, its arms levers ending in a hammer and a crescent blade, its works
    // hanging below it; it hangs from its contraption over the middle of its chamber, behind the great dial, and never
    // leaves it (rooted: the room is its reach).
    // Time stopped (the Hall as you find it), it's a husk on its cables: limp, brittle, now and then jerking an arm in a
    // slow weak swing; no phases, no tricks, its machines still; it's quickly killed, and it should feel wrong.
    // Time running (the central engine set going), it's the fight: four quarters (at three quarters, half and a quarter
    // of its health it strikes the quarter, untouchable a moment, and grows), each adding one thing:
    //   1  its hammer brought down on the floor in front of it, its blade swept round low (vault it, or dodge), its works
    //      ground round under it (don't stand there);
    //   2  + the floor dial's hands sweeping round the chamber, low (vault them);
    //   3  + Stop You (its mask's hands spin: it stops you in time, a beat, grey and still, and brings the hammer down on
    //      you; a dodge just before slips it) and the pendulums swinging across the chamber;
    //   4  the last hour: everything quicker, the hands faster, and it rewinds sooner.
    // All through (time running), Rewind: take too much from it at once and it winds itself back (its mask's hands
    // spinning backwards), mending what you did in those seconds, unless you strike it while it winds.
    // Beaten running, the hour strikes at last (the way on: the true ending, later); beaten stopped, it never does.
    public sealed class TheHour : Soured, IBoss
    {
        [Header("The Hour")]
        public string title = "The One Who Waits";
        public string epithet = "the Hour That Never Struck";
        public AudioClip[] music;                 // its fight's (time running; stopped, the Hall's own plays on)
        public float floorY;
        public Vector3 middle;                    // the chamber's middle, on its floor (it hangs over it)
        public float chamberRadius = 9.6f;
        public float wakeRadius = 8.5f;           // this near, it wakes

        [Header("Its blows (time running)")]
        public SouredAttack hammer = new SouredAttack(), blade = new SouredAttack(), grind = new SouredAttack();
        public float hammerOut = 4.6f, hammerRadius = 1.9f, hammerDamage = 32f;
        public float bladeNear = 1.4f, bladeFar = 7.2f, bladeArc = 115f, bladeDamage = 24f;
        public float grindRadius = 2.7f, grindDamage = 18f;

        [Header("Stop You (from the third quarter)")]
        public SouredAttack stop = new SouredAttack(), stopSlam = new SouredAttack();
        public float stopReach = 9f, stopHold = 1.2f, stopEvery = 9f, stopSlamDamage = 38f;

        [Header("Rewind (time running)")]
        public float rewindWindow = 4f;           // the harm it remembers
        public float rewindBurst = 90f, lastHourBurst = 60f; // this much within it, and it winds back
        public float rewindTime = 1.4f, rewindRest = 7f;

        [Header("The quarters")]
        public float chimeTime = 2.2f;
        public float lastHourPace = 0.72f;        // its tells and its rests, in the last hour, times this

        [Header("The floor dial's hands (from the second quarter)")]
        public Transform minuteHand, hourHand;
        public float minuteLength = 9f, hourLength = 6f, minuteWidth = 0.36f, hourWidth = 0.5f;
        public float minuteSpeed = 30f, hourSpeed = 12f, lastHourHands = 1.4f; // degrees a second (clockwise), and how much quicker last
        public float handDamage = 22f;
        public AudioClip[] handSound;

        [Header("The pendulums (from the third quarter)")]
        public Clockwork[] pendulums = new Clockwork[0];

        [Header("Limp (time stopped)")]
        public SouredAttack twitch = new SouredAttack();
        public float limpVulnerability = 2.5f, twitchEvery = 6f, twitchReach = 5.5f, twitchDamage = 8f;

        [Header("Look and sound")]
        public float cameraLean = 0.45f, cameraZoom = 1.7f;
        public AudioClip[] wakeSound, chimeSound, slamSound, rewindSound, stopSound, stoppedSound, deathSound, limpDeathSound, glanceSound;

        public string Title => title;
        public string Epithet => epithet;
        public float[] PhaseMarks => new[] { 0.75f, 0.5f, 0.25f };
        public int Phase => CurrentPhase;
        public AudioClip[] Music => Limp ? null : music;
        public string Farewell => BeatenRunning ? "TIME'S UP" : "TIME OUT";
        public string FarewellLine => BeatenRunning ? "The hour struck at last." : "The hour never came.";

        public bool Woken { get; private set; }
        public int CurrentPhase { get; private set; } = 1;
        public bool Limp => HallTime.Instance != null && !HallTime.Instance.Running; // (time stopped: a husk)
        public bool Chiming { get; private set; }
        public bool Rewinding { get; private set; }
        public int Rewinds { get; private set; }            // (tests) wound back whole
        public int RewindsBroken { get; private set; }      // (tests) struck while winding
        public int Stops { get; private set; }              // (tests) you, stopped in time
        public bool HandsTurning => handsOn;
        public bool BeatenRunning { get; private set; }     // beaten with the Hall's time going (the way on)
        public TrapBlows HandBlows { get; } = new TrapBlows { harm = 22f, souredHarm = 0f, every = 0.9f };
        public static event System.Action<TheHour, bool> Beaten; // (the endings, later) and whether time ran
        public static event System.Action<TheHour, int> QuarterStruck; // (tests) the phase begun

        public override bool OutOfReach => false;
        protected override bool Floats => true;
        protected override bool Surface() => true;
        protected override bool OnItsLevel() => true;
        protected override Vector3 ChaseStep(Vector3 toTarget, float distance) => Vector3.zero; // (rooted)
        protected override string IdleClip => Woken && !Limp ? "Idle" : "Limp";
        protected override string DeathClip => Limp && clips.Has("LimpDeath") ? "LimpDeath" : "Death"; // (the husk just gives out)
        protected override string ChaseClip(float distance) => IdleClip;
        protected override bool LosesTarget(float distance) => target == null || !InChamber(target.transform.position);
        protected override float Floor => !Limp && Woken && CurrentPhase < 4 ? maxHealth * PhaseMarks[CurrentPhase - 1] : 0f;
        protected override float Vulnerability => Limp ? limpVulnerability : 1f;

        readonly List<(float time, float harm)> harms = new List<(float, float)>();
        float rewindReady, rewindMend, chimeEnds, rewindEnds, nextTwitch, nextStop, handsAngle, lastHit;
        bool landed, slamHeld, handsOn, fightOn;
        float minuteAt, hourAt, dialHourAngle = -1.5f, dialMinuteAngle = -18f;
        Quaternion minuteRest, hourRest, dialHourRest, dialMinuteRest;
        Transform dialHour, dialMinute, dialTip, focus;
        int framing;
        AudioSource handVoice;
        readonly Dictionary<SouredAttack, SouredAttack> paced = new Dictionary<SouredAttack, SouredAttack>();

        protected override void Awake()
        {
            base.Awake();
            focus = new GameObject("The Hour (camera focus)").transform;
            Struck += OnStruck;
        }

        protected override void Start()
        {
            base.Start();
            foreach (var t in GetComponentsInChildren<Transform>())
            {
                if (t.name.EndsWith("DialHour")) dialHour = t;
                else if (t.name.EndsWith("DialMinute")) dialMinute = t;
                else if (t.name.EndsWith("DialTip")) dialTip = t;
            }
            if (dialHour != null)
                dialHourRest = dialHour.localRotation;
            if (dialMinute != null)
                dialMinuteRest = dialMinute.localRotation;
            if (minuteHand != null)
                minuteRest = minuteHand.rotation;
            if (hourHand != null)
                hourRest = hourHand.rotation;
            handVoice = TrapBlows.LoopSource(gameObject, 30f);
            Reform();
        }

        void OnDestroy()
        {
            Struck -= OnStruck;
            if (focus != null)
                Destroy(focus.gameObject);
        }

        bool InChamber(Vector3 p) => Flat(p - middle).magnitude < chamberRadius + 0.6f && p.y > floorY - 1.5f && p.y < floorY + 6f;

        // ---- waking, and its blows

        void Wake()
        {
            Woken = true;
            passive = false;
            if (Limp)
            {
                // (time stopped: it barely stirs, its head lolling up and down again)
                Sfx.Instance?.Attack(stoppedSound, transform.position + Vector3.up * 4f, 0.6f, "the_hour_stir");
                nextTwitch = Time.time + 2f;
                return;
            }
            BeBusy("Wake", 2.0f);
            Sfx.Instance?.Attack(wakeSound, transform.position + Vector3.up * 5f, 1f, "the_hour_wake");
            CameraRig.Instance?.Shake(0.25f);
        }

        protected override SouredAttack ChooseAttack(Vector3 toTarget, float distance)
        {
            if (!Woken || Chiming || Rewinding)
                return null;
            if (Limp)
            {
                if (Time.time < nextTwitch || distance > twitchReach)
                    return null;
                nextTwitch = Time.time + twitchEvery;
                return twitch;
            }
            if (slamHeld)
            {
                slamHeld = false; // (you, held: the hammer, on you)
                return stopSlam;
            }
            if (CurrentPhase >= 3 && Time.time >= nextStop && distance < stopReach && target != null && target.IsAlive)
            {
                nextStop = Time.time + stopEvery * (CurrentPhase == 4 ? 0.75f : 1f);
                return Paced(stop);
            }
            if (distance < grindRadius - 0.3f)
                return Paced(grind);
            if (distance >= 3.0f && distance <= hammerOut + hammerRadius && Random.value < 0.55f)
                return Paced(hammer);
            if (distance >= bladeNear && distance <= bladeFar)
                return Paced(blade);
            return null; // (out of its reach: its hands and pendulums keep at you)
        }

        // In the last hour, its tells and rests are shorter.
        SouredAttack Paced(SouredAttack attack)
        {
            if (CurrentPhase < 4)
                return attack;
            if (!paced.TryGetValue(attack, out var quick))
            {
                quick = attack.Copy();
                quick.telegraph *= lastHourPace;
                quick.recover *= lastHourPace;
                paced[attack] = quick;
            }
            return quick;
        }

        bool Is(SouredAttack attack, SouredAttack kind) => attack == kind || (paced.TryGetValue(kind, out var quick) && attack == quick);

        // Its blows land (each its own way: the Soured's own reach and arc don't fit a giant hung over a room).
        void Land(SouredAttack attack)
        {
            if (target == null)
                return;
            Vector3 at = target.transform.position, to = Flat(at - transform.position);
            bool low = at.y < floorY + 0.5f; // (vaulting, you're over what sweeps the floor)
            if (Is(attack, hammer) || Is(attack, stopSlam))
            {
                Vector3 point = Is(attack, stopSlam) && target.HeldInTime ? Flat(at) + Vector3.up * floorY
                              : Flat(transform.position + transform.forward * hammerOut) + Vector3.up * floorY;
                Effects.Instance?.Rubble(point, 1.6f);
                Sfx.Instance?.Attack(slamSound, point, 1f, "the_hour_slam");
                if (Flat(at - point).magnitude < 12f)
                    CameraRig.Instance?.Shake(0.3f);
                if (target.IsAlive && Flat(at - point).magnitude <= hammerRadius && at.y < floorY + 1.6f)
                    target.TakeHit(Is(attack, stopSlam) ? stopSlamDamage : hammerDamage, Flat(at - point).normalized * 10f, this, blockable: false);
            }
            else if (Is(attack, blade))
            {
                float d = to.magnitude;
                if (target.IsAlive && low && d >= bladeNear && d <= bladeFar && Vector3.Angle(transform.forward, to) <= bladeArc)
                    target.TakeHit(bladeDamage, -transform.right * 12f, this);
            }
            else if (Is(attack, grind))
            {
                Effects.Instance?.Rubble(Flat(transform.position) + Vector3.up * floorY, 1f);
                if (target.IsAlive && to.magnitude <= grindRadius && at.y < floorY + 2f)
                    target.TakeHit(grindDamage, to.normalized * 14f, this);
            }
            else if (Is(attack, stop))
            {
                if (target.IsAlive && to.magnitude <= stopReach && target.StopInTime(stopHold))
                {
                    Stops++;
                    slamHeld = true;
                    Sfx.Instance?.Attack(stoppedSound, at + Vector3.up, 1f, "the_hour_stopped");
                }
                else
                    BeBusy("Idle", 1.2f); // (slipped: it overreached, open a moment)
            }
            else if (Is(attack, twitch))
            {
                float d = to.magnitude;
                if (target.IsAlive && d <= twitchReach && Vector3.Angle(transform.forward, to) <= 60f)
                    target.TakeHit(twitchDamage, to.normalized * 4f, this);
            }
        }

        protected override void OnEntered(State state)
        {
            if (state == State.Telegraph && CurrentAttack != null && Is(CurrentAttack, stop))
            {
                Sfx.Instance?.Attack(stopSound, transform.position + Vector3.up * 6f, 1f, "the_hour_stop");
                Effects.Instance?.AlertFlare(MaskPoint);
            }
            if (state == State.Dying)
                Died();
        }

        Vector3 MaskPoint => dialHour != null ? dialHour.position : transform.position + Vector3.up * 6.4f;

        // ---- every frame: waking, the quarters, the rewind, its hands and pendulums, its blows landing

        protected override void Tick(float dt)
        {
            if (!IsAlive)
            {
                Machines(false, dt);
                return;
            }
            if (!Woken)
            {
                if (target != null && target.IsAlive && InChamber(target.transform.position)
                    && Flat(target.transform.position - middle).magnitude < wakeRadius)
                    Wake();
                Machines(false, dt);
                return;
            }
            // Its blows land at their moment (scripted: the Soured's own impact doesn't fit it).
            if (CurrentState == State.Strike && CurrentAttack != null && !landed && StateTime >= CurrentAttack.strike * CurrentAttack.impactAt)
            {
                landed = true;
                Land(CurrentAttack);
            }
            if (CurrentState != State.Strike)
                landed = false;
            if (Limp)
            {
                Chiming = Rewinding = false;
                Machines(false, dt);
                return;
            }
            // A quarter: brought to its mark, it strikes it (untouchable a moment), and grows.
            if (!Chiming && CurrentPhase < 4 && Health <= Floor + 0.01f)
                Chime();
            if (Chiming && Time.time >= chimeEnds)
            {
                Chiming = false;
                CurrentPhase++;
                harms.Clear();
                QuarterStruck?.Invoke(this, CurrentPhase);
                if (CurrentPhase >= 3)
                    nextStop = Time.time + 2.5f;
            }
            // Too much at once: it winds itself back.
            if (!Chiming && !Rewinding && Time.time >= rewindReady && CurrentState != State.Staggered)
            {
                harms.RemoveAll(h => Time.time - h.time > rewindWindow);
                float burst = 0f;
                foreach (var h in harms)
                    burst += h.harm;
                if (burst >= (CurrentPhase == 4 ? lastHourBurst : rewindBurst))
                    StartRewind(burst);
            }
            if (Rewinding && Time.time >= rewindEnds)
            {
                Rewinding = false;
                Rewinds++;
                float top = CurrentPhase == 1 ? maxHealth : maxHealth * PhaseMarks[CurrentPhase - 2];
                SetHealth(Mathf.Min(top, Health + rewindMend));
                Effects.Instance?.StrokeCharged(MaskPoint);
            }
            Machines(true, dt);
        }

        void Chime()
        {
            Chiming = true;
            Rewinding = false;
            chimeEnds = Time.time + chimeTime;
            BeBusy("Chime", chimeTime);
            Sfx.Instance?.Attack(chimeSound, transform.position + Vector3.up * 6f, 1f, "the_hour_chime");
            Effects.Instance?.KeyItemRing(MaskPoint);
            CameraRig.Instance?.Shake(0.35f);
        }

        void StartRewind(float burst)
        {
            Rewinding = true;
            rewindMend = burst;
            rewindEnds = Time.time + rewindTime;
            rewindReady = Time.time + rewindTime + rewindRest;
            harms.Clear();
            BeBusy("Rewind", rewindTime);
            Sfx.Instance?.Attack(rewindSound, transform.position + Vector3.up * 5f, 1f, "the_hour_rewind");
        }

        // Every blow that lands on it: remembered (the rewind's); struck while it winds, its winding's broken.
        void OnStruck(Soured soured, HitResult result, float dealt)
        {
            if (soured != this || result != HitResult.Landed || dealt <= 0f)
                return;
            lastHit = Time.time;
            harms.Add((Time.time, dealt));
            if (Rewinding)
            {
                Rewinding = false;
                RewindsBroken++;
                harms.Clear();
                Sfx.Instance?.Attack(glanceSound, MaskPoint, 1f, "the_hour_rewind_broken");
                Effects.Instance?.BlockSpark(MaskPoint, Vector3.up);
                StaggerNow(1.6f);
            }
        }

        // Struck the quarter: nothing gets through till it's done (a ring off it, no harm).
        protected override bool Absorbs(Hit hit, bool fire)
        {
            if (!Chiming)
                return false;
            Effects.Instance?.BlockSpark(transform.position + Vector3.up * 2f + Flat(hit.From - transform.position).normalized * radius, Vector3.up);
            Sfx.Instance?.Attack(glanceSound, transform.position + Vector3.up * 2f, 0.5f, "the_hour_untouchable");
            return true;
        }

        public override HookResult OnHooked(Apprentice by)
        {
            Effects.Instance?.BlockSpark(transform.position + Vector3.up * 2f, Flat(by.transform.position - transform.position).normalized);
            Sfx.Instance?.Attack(glanceSound, transform.position + Vector3.up * 2f, 0.8f, "the_hour_glance");
            return HookResult.Shrugged;
        }

        // Its machines: the floor dial's hands (the second quarter on) and the pendulums (the third on), going while
        // it fights with time running; held otherwise.
        void Machines(bool going, float dt)
        {
            bool hands = going && CurrentPhase >= 2 && !Chiming;
            handsOn = hands;
            if (hands)
            {
                float quick = CurrentPhase == 4 ? lastHourHands : 1f;
                minuteAt += minuteSpeed * quick * dt;
                hourAt += hourSpeed * quick * dt;
                HandBlows.Sweep((feet, r) => OnHand(feet, r, minuteAt, minuteLength, minuteWidth) || OnHand(feet, r, hourAt, hourLength, hourWidth),
                                feet => Tangent(feet) * 14f);
            }
            if (minuteHand != null)
                minuteHand.rotation = Quaternion.AngleAxis(minuteAt, Vector3.up) * minuteRest;
            if (hourHand != null)
                hourHand.rotation = Quaternion.AngleAxis(hourAt, Vector3.up) * hourRest;
            TrapBlows.Keep(handVoice, handSound, hands ? 0.5f : 0f, CurrentPhase == 4 ? 1.15f : 1f);
            bool swing = going && CurrentPhase >= 3;
            foreach (var p in pendulums)
            {
                if (p == null)
                    continue;
                if (swing && !p.Forever)
                    p.RunForever();
                else if (!swing && p.Running)
                    p.Hold();
            }
        }

        // On a floor hand (lying low along it, from the pivot out): `angle` its heading, clockwise from where it rests.
        bool OnHand(Vector3 feet, float r, float angle, float length, float width)
        {
            if (feet.y > floorY + 0.45f)
                return false; // (over it: vaulted)
            Vector3 d = Flat(feet - middle);
            if (d.magnitude < 1.3f)
                return false; // (its own works hang there: not the floor's to sweep)
            Vector3 along = Quaternion.AngleAxis(angle, Vector3.up) * HandRestDirection;
            float t = Vector3.Dot(d, along);
            if (t < 0f || t > length + r)
                return false;
            return (d - along * t).magnitude <= width * 0.5f + r;
        }

        Vector3 HandRestDirection => Vector3.forward; // (the hands lie pointing north as built)

        Vector3 Tangent(Vector3 feet)
        {
            Vector3 d = Flat(feet - middle);
            return Vector3.Cross(Vector3.up, d).normalized; // (clockwise from above: the way the hands go)
        }

        // ---- its mask's hands; the camera

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            // The mask's hands: stopped at three to twelve (limp), turning a little (poised), spinning on before it
            // stops you, spinning back as it rewinds.
            float spin = !IsAlive || (Limp && Woken) || !Woken ? 0f
                       : Rewinding ? -900f
                       : CurrentState == State.Telegraph && CurrentAttack != null && Is(CurrentAttack, stop) ? 720f
                       : Chiming ? 360f : 6f;
            dialMinuteAngle += spin * dt;
            dialHourAngle += spin / 12f * dt;
            if (dialTip != null && dialHour != null && dialMinute != null)
            {
                dialHour.localRotation = dialHourRest; // (only ever at rest in its clips: turned from there each frame)
                dialMinute.localRotation = dialMinuteRest;
                Vector3 axis = (dialTip.position - dialHour.position).normalized;
                dialHour.rotation = Quaternion.AngleAxis(-dialHourAngle, axis) * dialHour.rotation;
                dialMinute.rotation = Quaternion.AngleAxis(-dialMinuteAngle, axis) * dialMinute.rotation;
            }
            bool on = Woken && IsAlive && target != null && target.IsAlive && InChamber(target.transform.position);
            if (on)
                BossFight.Begin(this);
            else if (fightOn)
                BossFight.End(this);
            fightOn = on;
            if (focus != null)
                focus.position = Flat(transform.position) + Vector3.up * (floorY + 2.5f);
            int frame = on || (CurrentState == State.Dying && target != null && target.IsAlive) ? 2 : 0;
            if (frame != framing)
            {
                framing = frame;
                CameraRig.Instance?.SetFightFocus(frame > 0 ? focus : null, cameraLean);
                CameraRig.Instance?.SetZoom(frame > 0 ? cameraZoom : 1f);
            }
        }

        // Beaten: running, the hour strikes at last (a great toll); stopped, it only sags on its cables.
        void Died()
        {
            BeatenRunning = !Limp;
            Rewinding = Chiming = false;
            handsOn = false;
            Sfx.Instance?.Attack(BeatenRunning ? deathSound : limpDeathSound, transform.position + Vector3.up * 5f, 1f, "the_hour_death");
            if (BeatenRunning)
                CameraRig.Instance?.Shake(0.4f);
            BossFight.Defeat(this);
            BossFight.End(this);
            fightOn = false;
            Beaten?.Invoke(this, BeatenRunning);
        }

        // The lab: begun at a later quarter (time running).
        public void BeginAt(int phase)
        {
            phase = Mathf.Clamp(phase, 1, 4);
            Reform();
            Woken = true;
            passive = false;
            CurrentPhase = phase;
            SetHealth(phase == 1 ? maxHealth : maxHealth * PhaseMarks[phase - 2]);
            if (phase >= 3)
                nextStop = Time.time + 2.5f;
        }

        public override void Reform()
        {
            base.Reform();
            Woken = false;
            passive = true;
            CurrentPhase = 1;
            Chiming = Rewinding = slamHeld = false;
            BeatenRunning = false;
            harms.Clear();
            rewindReady = 0f;
            minuteAt = hourAt = 0f;
            dialHourAngle = -1.5f;
            dialMinuteAngle = -18f;
            handsOn = false;
            Machines(false, 0f);
            BossFight.End(this);
            fightOn = false;
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
