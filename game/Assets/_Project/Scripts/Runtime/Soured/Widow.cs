using UnityEngine;

namespace MyGame
{
    // The Widow Hale, Who Kept the Minute: the Hall of the Last Hour's miniboss (docs/brief.md). The widow of the Hall,
    // who stopped the great clock at the minute of the death; she keeps vigil at its central engine in the belfry so the
    // hour never comes, and while she does, its plate can't be struck (CentralEngine.keeper).
    //   Held in the stillness: she moves through the stopped Hall, but a blow passes through her as through a
    //     photograph. Only in time can she be touched: inside a pocket of time round one of the engine's three parts
    //     (the escapement, the winding drum, the hour bell) set going a while with the Stroke (TimePocket), or anywhere
    //     once the Hall's time runs (the lab's switch).
    //   She goes to stop it: a running part draws her; she lays her hands on its drive (Tend, the tell) and holds it
    //     until it stops, its pocket gone. So she comes into the pocket: strike her there and she reels (her hold
    //     broken: she has to begin again).
    //   Her blows, between: a great knell (her family's, at its fullest: a breath, then four rings rolling across the
    //     floor; dash through them) and the veil (her crape cast out low along the floor, right to left: vault it or
    //     dodge; caught, you're slowed a while, held half in the stillness).
    //   At half, she's quicker: her hands stop a part sooner, and she casts the veil at you on her way to one.
    // Beaten, she lets the minute go: the engine's plate is free (starting it stays the Apprentice's choice).
    public sealed class Widow : Soured, IBoss
    {
        [System.Serializable]
        public sealed class Part
        {
            public TimePocket pocket;   // round its drive (its clockwork's)
            public Transform tendAt;    // where she stands to lay her hands on it (facing it)
        }

        [Header("The Widow")]
        public string title = "The Widow Hale";
        public string epithet = "Who Kept the Minute";
        public string farewell = "GOOD MOURNING";
        public string farewellLine = "She let the minute pass.";
        public AudioClip[] music;
        public Part[] parts = new Part[0];
        public float fightRange = 13f;      // the fight's on while you're this near, on her floor
        public float glideSpeed = 1.3f;     // keeping her vigil, drifting after you
        public float hurrySpeed = 2.8f;     // to a part set going
        public float tendTime = 2.6f;       // her hands on it, until it stops (at half: tendTimeLate)
        public float tendTimeLate = 1.8f;

        [Header("The knell (primary)")]
        public float knellFrom = 3.5f, knellTo = 12f;
        public float knellEvery = 6.5f;
        public float ringRadius = 11f, ringSpeed = 5.5f, ringWidth = 0.9f, ringDamage = 16f, ringKnockback = 6f;
        public Color ringTint = new Color(0.72f, 0.74f, 0.88f, 0.85f);

        [Header("The veil")]
        public SouredAttack veil = new SouredAttack();
        public float veilEvery = 3.5f;
        public float veilSlow = 0.45f, veilSlowFor = 2.6f;

        [Header("Sounds")]
        public AudioClip[] knellSound, throughSound, struckSound, tendSound, deathSound;

        public string Title => title;
        public string Epithet => epithet;
        public float[] PhaseMarks => new[] { 0.5f };
        public int Phase => HealthFraction <= 0.5f ? 2 : 1;
        public AudioClip[] Music => music;
        public string Farewell => farewell;
        public string FarewellLine => farewellLine;

        // Whether she's in time now (a blow can touch her): inside a pocket of time, or the Hall's time running.
        public bool InTime => (HallTime.Instance != null && HallTime.Instance.Running) || TimePocket.InAny(transform.position + Vector3.up * 0.5f, 0.25f);
        public Part Tending { get; private set; }     // the part her hands are on
        public Part Seeking { get; private set; }     // the part she's going to
        public int Stopped { get; private set; }      // (tests) parts she's stopped
        public int Passed { get; private set; }       // (tests) blows that passed through her
        public int Rings { get; private set; }        // (tests)
        public static event System.Action<Widow, Part> Stopping, Stilled; // her hands on a part; it stops (tests)

        float nextKnell, nextVeil, tendFor;
        bool fightOn;
        Shockwave.Band band;
        AudioSource tendVoice;

        float TendTime => Phase >= 2 ? tendTimeLate : tendTime;
        const float ReachFade = 0.35f;

        // Gliding; to a part, hurrying.
        protected override float ChaseSpeed(float distance) => Seeking != null ? hurrySpeed : glideSpeed;
        protected override string ChaseClip(float distance) => Seeking != null || distance > knellFrom ? "Walk" : "Idle";

        // A part set going (the nearest of them, if more than one): she goes to it, whatever else.
        Part Running()
        {
            Part best = null;
            float bestD = float.MaxValue;
            foreach (var p in parts)
            {
                if (p?.pocket == null || p.pocket.clockwork == null || !p.pocket.clockwork.Running || p.pocket.clockwork.Forever)
                    continue;
                float d = Flat(p.tendAt.position - transform.position).magnitude;
                if (d < bestD)
                {
                    bestD = d;
                    best = p;
                }
            }
            return best;
        }

        // A part set going is where she belongs: given up on you (or never after you), she goes back to it, not her post.
        protected override Vector3 Home => Seeking != null ? Seeking.tendAt.position : base.Home;

        protected override bool ArrivedHome()
        {
            if (Seeking == null)
                return false;
            BeginTending(Seeking);
            return true;
        }

        protected override Vector3 ChaseStep(Vector3 toTarget, float distance)
        {
            if (Seeking != null)
            {
                Vector3 to = Flat(Seeking.tendAt.position - transform.position);
                if (to.magnitude < 0.25f)
                    return Vector3.zero;
                return PathDirection(Seeking.tendAt.position) * Mathf.Min(hurrySpeed, to.magnitude / Mathf.Max(Time.deltaTime, 1e-3f));
            }
            // Her vigil: she drifts after you, never too close (the veil's for the near; the knell for the rest).
            if (distance < 2.6f)
                return Vector3.zero;
            return base.ChaseStep(toTarget, distance);
        }

        protected override SouredAttack ChooseAttack(Vector3 toTarget, float distance)
        {
            if (Seeking != null)
            {
                // Late on, she lashes out with the veil on her way to a part, if you're in her way.
                if (Phase >= 2 && distance <= veil.reach && Time.time >= nextVeil)
                {
                    nextVeil = Time.time + veilEvery;
                    return veil;
                }
                return null;
            }
            if (distance <= veil.reach && Time.time >= nextVeil)
            {
                nextVeil = Time.time + veilEvery;
                return veil;
            }
            if (distance >= knellFrom && distance <= knellTo && Time.time >= nextKnell && OnItsLevel())
            {
                nextKnell = Time.time + knellEvery;
                return primary;
            }
            return null;
        }

        protected override void Tick(float dt)
        {
            if (!IsAlive)
                return;
            // Her hands on a part: held until it stops (or she's struck off it).
            if (Tending != null)
            {
                if (CurrentState != State.Busy)
                    EndTending(stopped: false);
                else
                {
                    Turn(Flat(Tending.pocket.transform.position - transform.position), 240f * dt);
                    if (StateTime >= tendFor - 0.05f || !Tending.pocket.clockwork.Running)
                        EndTending(stopped: true);
                }
            }
            // A part set going: to it (unless mid-blow, or reeling).
            if (Tending == null && (CurrentState == State.Chase || CurrentState == State.Idle || CurrentState == State.Returning))
            {
                Seeking = Running();
                if (Seeking != null)
                {
                    if (CurrentState != State.Chase)
                        Alert();
                    if (Flat(Seeking.tendAt.position - transform.position).magnitude < 0.35f)
                        BeginTending(Seeking);
                }
            }
            else if (Tending == null && CurrentState != State.Telegraph && CurrentState != State.Strike && CurrentState != State.Recover)
                Seeking = null;
            // The veil, cast: its blow (low: vaulted, it passes under; caught, slowed).
            if (CurrentAttack == veil && CurrentState == State.Strike && !veilLanded && StateTime >= veil.strike * veil.impactAt)
            {
                veilLanded = true;
                VeilLands();
            }
            if (CurrentState != State.Strike)
                veilLanded = false;
        }

        bool veilLanded;

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        void Turn(Vector3 direction, float maxDegrees)
        {
            if (direction.sqrMagnitude < 0.0001f)
                return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction.normalized), maxDegrees);
        }

        void VeilLands()
        {
            if (target == null || !target.IsAlive)
                return;
            Vector3 to = Flat(target.transform.position - transform.position);
            if (to.magnitude > veil.reach + 0.35f || Vector3.Angle(transform.forward, to) > veil.arc || target.Airborne || !OnItsLevel())
                return;
            var defence = HitTarget(veil.damage, to.normalized * veil.knockback);
            if (defence == Defence.Taken)
                target.Slow(veilSlow, veilSlowFor);
            else if (defence == Defence.Parried)
                Stun();
        }

        void BeginTending(Part part)
        {
            Tending = part;
            Seeking = null;
            tendFor = TendTime;
            transform.position = new Vector3(part.tendAt.position.x, transform.position.y, part.tendAt.position.z);
            BeBusy("Tend", tendFor);
            clips.Loop("Tend", ReachFade, now: true); // (a slow blend in: her hands reaching out to it, 0.57 m)
            tendVoice = Sfx.Instance?.Attack(tendSound, part.pocket.transform.position + Vector3.up, 1f, "widow_tend");
            Stopping?.Invoke(this, part);
        }

        void EndTending(bool stopped)
        {
            var part = Tending;
            Tending = null;
            if (tendVoice != null && !stopped)
                tendVoice.Stop();
            tendVoice = null;
            if (IsAlive && CurrentState == State.Busy)
                clips.Loop(IdleClip, ReachFade, now: true); // (and as slowly back: her hands drawn in)
            if (stopped && part != null)
            {
                part.pocket.clockwork.Hold(); // (it winds down where it is, its pocket shrinking)
                Stopped++;
                Stilled?.Invoke(this, part);
            }
        }

        // Out of time, a blow passes through her (a grey shimmer, an airless whoosh); in time, it lands.
        protected override bool Absorbs(Hit hit, bool fire)
        {
            if (InTime)
                return false;
            Passed++;
            Vector3 towards = Flat(hit.From - transform.position).normalized;
            Vector3 at = transform.position + towards * (radius + 0.1f) + Vector3.up * 1.4f;
            Effects.Instance?.Wisp(at, new Color(0.78f, 0.8f, 0.86f, 0.7f));
            Effects.Instance?.Wisp(at + Vector3.up * 0.4f, new Color(0.78f, 0.8f, 0.86f, 0.5f));
            Sfx.Instance?.Attack(throughSound, at, 0.8f, "widow_through");
            return true;
        }

        public override HookResult OnHooked(Apprentice by) => HookResult.Shrugged; // (out of time, nothing to catch; in it, too heavy)

        protected override void OnEntered(State state)
        {
            if (state == State.Staggered || state == State.Stunned)
                Sfx.Instance?.Attack(struckSound, transform.position + Vector3.up * 1.6f, 1f, "widow_struck");
            if (state == State.Dying)
            {
                Seeking = null;
                if (Tending != null)
                    EndTending(stopped: false);
                Sfx.Instance?.Attack(deathSound, transform.position + Vector3.up * 1.6f, 1f, "widow_death");
                BossFight.Defeat(this);
                BossFight.End(this);
                fightOn = false;
            }
        }

        // Each ring of the knell, from where she stands; the four one band, the bell once.
        protected override void Shoot(SouredAttack attack, Vector3 direction, int index)
        {
            if (index == 0)
            {
                band = new Shockwave.Band();
                Sfx.Instance?.Attack(knellSound, transform.position + Vector3.up * 1.6f, 1f, "widow_knell");
            }
            var ring = Shockwave.Spread(transform.position, target, ringRadius, ringSpeed, ringWidth, ringDamage, ringKnockback, ringTint);
            ring.muffled = true;
            ring.band = band;
            Rings++;
        }

        void LateUpdate()
        {
            // The fight: on while you're near, on her floor (the belfry's).
            bool on = IsAlive && target != null && target.IsAlive && OnItsLevel()
                      && Flat(target.transform.position - transform.position).magnitude < fightRange;
            if (on)
            {
                BossFight.Begin(this);
                if (CurrentState == State.Idle)
                    Alert();
            }
            else if (fightOn)
                BossFight.End(this);
            fightOn = on;
        }

        public override void Reform()
        {
            if (Tending != null)
                EndTending(stopped: false);
            Seeking = null;
            base.Reform();
            nextKnell = nextVeil = 0f;
            BossFight.End(this);
            fightOn = false;
        }
    }
}
