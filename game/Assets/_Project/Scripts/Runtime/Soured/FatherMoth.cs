using UnityEngine;

namespace MyGame
{
    // Father Moth, Who Gathered Autumn: the Gardener, the Orchard's Boss (docs/brief.md). The man who tended the orchard
    // and its glasshouse, grown into the greatest of the Moths: wings like dead leaves, six metres across, the stooped
    // gardener rising from them, his forearms grown into forelegs ending in pruning shears, a caged lantern in his ribs.
    // He hangs folded on the dead orange tree, like a leaf; come into the Glasshouse and he wakes.
    // He keeps to one place, hovering over the north of the floor, out of the oar's reach, and beats his wings: each beat
    // (his wings raised high, the lantern in his ribs flaring: the tell) throws dead leaves at you in a pattern, to dodge,
    // block or step between. After so many beats he's spent: he sinks to the floor, his wings slack, and lies there a
    // while, open to the oar; then he's up again. Four phases (at three quarters, half, a quarter), each its own patterns,
    // more and more of them: only phase 1's are written yet (the later phases use them for now). Fire burns him badly.
    // The Hook glances off him.
    public sealed class FatherMoth : Soured, IBoss
    {
        [Header("Father Moth")]
        public string title = "Father Moth";
        public string epithet = "Who Gathered Autumn";
        public string farewell = "MOTHBALLED";
        public string farewellLine = "Winter is nigh.";
        public AudioClip[] music;             // one track per phase

        [Header("His glasshouse")]
        public float floorY;                  // its floor
        public Vector3 middle;                // the middle of its floor
        public Vector2 reach = new Vector2(10f, 7.5f); // how far from the middle he flies (half a width, half a depth)
        public Vector2 walls = new Vector2(11.6f, 8.6f); // half its width and depth, inside the walls (his leaves go no further)
        public float wakeLine;                // past this (z), inside, he wakes
        public Vector3 hover;                 // where he keeps to on the wing (on the floor: he's up above it)
        public float hoverHeight = 3.2f;

        [Header("His wingbeats")]
        public SouredAttack beat = new SouredAttack(); // one wingbeat: its tell, its throw, a breath after
        public int beatsBeforeSpent = 6;      // then he sinks, exhausted
        public float leafSpeed = 6.5f, leafDamage = 10f;
        public float fanSpread = 70f;         // the fan: this wide
        public int fanCount = 5, ringCount = 14, volleyCount = 3;
        public float volleyInterval = 0.35f;  // the aimed ones: one after another
        public Material leafMaterial;

        [Header("Spent")]
        public float sinkTime = 1.2f, spentTime = 5f, riseTime = 0.9f;

        [Header("Look and sound")]
        public Light chest;                   // the lantern in his ribs
        public float chestIntensity = 1.4f;
        public float cameraLean = 0.5f, cameraZoom = 1.45f, sleepingLean = 0.45f, sleepingZoom = 1.15f; // his fight: the camera leans to him, and takes in more of the floor
        public AudioClip[] wingSound, wakeSound, beatSound, downedSound, glanceSound, deathSound;

        public enum Pattern { Fan, Volley, Ring }
        enum Mode { Hovering, Sinking, Spent, Rising }

        // Phase 1's run of beats, in order (then he's spent).
        static readonly Pattern[] PhaseOne = { Pattern.Fan, Pattern.Volley, Pattern.Fan, Pattern.Ring, Pattern.Volley, Pattern.Fan };

        public bool Woken { get; private set; }
        public bool Grounded => mode == Mode.Spent;
        public bool Spent => mode == Mode.Spent;
        public bool Beating => CurrentAttack == beat && (CurrentState == State.Telegraph || CurrentState == State.Strike);
        public Pattern? Thrown { get; private set; } // the pattern of the beat now (or last)
        public int BeatsLeft => beatsBeforeSpent - beatsDone;
        public float Height => transform.position.y - floorY;
        public static event System.Action<FatherMoth> Woke, Exhausted; // (tests)
        public static event System.Action<FatherMoth, Pattern> Beat;

        public string Title => title;
        public string Epithet => epithet;
        public float[] PhaseMarks => new[] { 0.75f, 0.5f, 0.25f };
        public int Phase => HealthFraction > 0.75f ? 1 : HealthFraction > 0.5f ? 2 : HealthFraction > 0.25f ? 3 : 4;
        public AudioClip[] Music => music;
        public string Farewell => farewell;
        public string FarewellLine => farewellLine;

        public override bool Flies => true;
        protected override bool Floats => true;
        protected override bool Surface() => true;
        public override bool OutOfReach => Height > 1.9f;
        protected override bool OnItsLevel() => true;
        protected override string IdleClip => Woken ? "Fly" : "Perched";
        protected override string ChaseClip(float distance) => "Fly";
        protected override Vector3 ChaseStep(Vector3 toTarget, float distance) => Vector3.zero; // (he flies: Tick)
        protected override bool LosesTarget(float distance) => !InHouse(target.transform.position);

        Vector3 velocity, perch;
        Quaternion perchRotation;
        Mode mode;
        int beatsDone, volleyLeft;
        float nextVolley, nextWing;
        bool fightOn;
        Transform focus;                       // his body (the camera leans to it: halfway, so you're both in view)
        int framing;                           // 0 not, 1 asleep on his tree, 2 his fight

        protected override void Awake()
        {
            base.Awake();
            perch = transform.position;
            perchRotation = transform.rotation;
            focus = new GameObject("Father Moth (camera focus)").transform;
        }

        protected override void Start()
        {
            base.Start();
            Reform();
        }

        bool InHouse(Vector3 p) => Mathf.Abs(p.x - middle.x) < reach.x + 2f && Mathf.Abs(p.z - middle.z) < reach.y + 2f && p.y > floorY - 1f && p.y < floorY + 8f;

        Vector3 HoverPoint => new Vector3(hover.x + Mathf.Sin(Time.time * 0.7f) * 0.6f, floorY + hoverHeight + Mathf.Sin(Time.time * 1.9f) * 0.15f, hover.z);

        // ---- waking

        void Wake()
        {
            Woken = true;
            passive = false;
            BeBusy("Wake", 2.0f);
            mode = Mode.Rising;
            Sfx.Instance?.Attack(wakeSound, transform.position + Vector3.up * 2f, 1f, "father_moth_wake");
            Effects.Instance?.Leaves(transform.position + Vector3.up * 2f, Vector3.up * 2f, 40);
            CameraRig.Instance?.Shake(0.2f);
            beatsDone = 0;
            Woke?.Invoke(this);
        }

        // ---- his beats

        protected override SouredAttack ChooseAttack(Vector3 toTarget, float distance)
        {
            if (!Woken || mode != Mode.Hovering || (transform.position - HoverPoint).magnitude > 1.5f || volleyLeft > 0)
                return null;
            if (beatsDone >= beatsBeforeSpent)
            {
                Sink();
                return null;
            }
            var run = PhaseOne; // (phases 2 to 4: their own runs, still to come)
            Thrown = run[beatsDone % run.Length];
            beatsDone++;
            return beat;
        }

        protected override void OnEntered(State state)
        {
            if (state == State.Telegraph && CurrentAttack == beat)
            {
                Effects.Instance?.AlertFlare(ChestPoint);
                Sfx.Instance?.Attack(wingSound, transform.position + Vector3.up * 2f, 0.8f, "father_moth_wing");
            }
            if (state == State.Strike && CurrentAttack == beat && Thrown.HasValue)
                Throw(Thrown.Value);
            if (state == State.Dying)
                Died();
        }

        Vector3 ChestPoint => chest != null ? chest.transform.position : transform.position + Vector3.up * 2.5f;

        // The leaves, thrown from under him at chest height.
        void Throw(Pattern pattern)
        {
            Sfx.Instance?.Attack(beatSound, transform.position + Vector3.up * 2f, 1f, "father_moth_beat");
            Effects.Instance?.Leaves(transform.position + Vector3.up * 1.5f, Vector3.zero, 25);
            Beat?.Invoke(this, pattern);
            Vector3 at = Flat(target != null ? target.transform.position - transform.position : transform.forward);
            Vector3 aim = at.sqrMagnitude > 0.01f ? at.normalized : Vector3.back;
            switch (pattern)
            {
                case Pattern.Fan:
                    for (int i = 0; i < fanCount; i++)
                        Leaf(Quaternion.Euler(0f, Mathf.Lerp(-fanSpread / 2f, fanSpread / 2f, fanCount > 1 ? i / (fanCount - 1f) : 0.5f), 0f) * aim);
                    break;
                case Pattern.Ring:
                    float turn = Random.Range(0f, 360f / ringCount);
                    for (int i = 0; i < ringCount; i++)
                        Leaf(Quaternion.Euler(0f, turn + i * 360f / ringCount, 0f) * Vector3.forward);
                    break;
                case Pattern.Volley:
                    volleyLeft = volleyCount;
                    nextVolley = Time.time;
                    break;
            }
        }

        void Leaf(Vector3 direction)
        {
            Vector3 from = new Vector3(transform.position.x, floorY + 0.9f, transform.position.z) + direction * 1.2f;
            DriftLeaf.Fling(from, direction, leafSpeed, leafDamage, target, leafMaterial, middle, walls);
        }

        // ---- every frame

        protected override void Tick(float dt)
        {
            if (!IsAlive)
            {
                Fall(dt);
                Chest();
                return;
            }
            if (!Woken)
            {
                if (target != null && target.IsAlive && InHouse(target.transform.position) && target.transform.position.z > wakeLine)
                    Wake();
                Chest();
                return;
            }
            // The aimed ones of a volley, one after another, each at where you are now.
            if (volleyLeft > 0 && Time.time >= nextVolley && target != null)
            {
                volleyLeft--;
                nextVolley = Time.time + volleyInterval;
                Leaf(Flat(target.transform.position - transform.position).normalized);
                Sfx.Instance?.Attack(wingSound, transform.position + Vector3.up * 2f, 0.4f, "father_moth_wing", 1.3f);
            }
            switch (mode)
            {
                case Mode.Sinking:
                    Steer(new Vector3(hover.x, floorY, hover.z), dt, 0.5f);
                    if (CurrentState != State.Busy)
                        Spend();
                    break;
                case Mode.Spent:
                    Hold(new Vector3(transform.position.x, floorY, transform.position.z));
                    if (CurrentState != State.Busy && CurrentState != State.Staggered)
                        RiseAgain();
                    break;
                case Mode.Rising:
                    Steer(HoverPoint, dt, 0.7f);
                    if (CurrentState != State.Busy)
                        mode = Mode.Hovering;
                    break;
                default:
                    Steer(HoverPoint, dt, 1f);
                    if (Time.time >= nextWing && CurrentState == State.Chase)
                    {
                        nextWing = Time.time + 0.9f;
                        Sfx.Instance?.Attack(wingSound, transform.position + Vector3.up * 2f, 0.5f, "father_moth_wing");
                    }
                    break;
            }
            Chest();
        }

        // Spent: down he sinks, his wings slack.
        void Sink()
        {
            mode = Mode.Sinking;
            BeBusy("Land", sinkTime);
            Exhausted?.Invoke(this);
        }

        // On the floor, sprawled, heaving: open to the oar a while.
        void Spend()
        {
            mode = Mode.Spent;
            Hold(new Vector3(transform.position.x, floorY, transform.position.z));
            BeBusy("Downed", spentTime);
            Effects.Instance?.Rubble(new Vector3(transform.position.x, floorY, transform.position.z), 1.0f);
            Effects.Instance?.Leaves(transform.position + Vector3.up, Vector3.zero, 30);
            Sfx.Instance?.Attack(downedSound, transform.position + Vector3.up, 1f, "father_moth_downed");
            CameraRig.Instance?.Shake(0.3f);
        }

        void RiseAgain()
        {
            mode = Mode.Rising;
            beatsDone = 0;
            BeBusy("Rise", riseTime);
        }

        // The Hook: he's too big, and too high; it glances off.
        public override HookResult OnHooked(Apprentice by)
        {
            Effects.Instance?.BlockSpark(ChestPoint, Flat(by.transform.position - transform.position).normalized);
            Sfx.Instance?.Attack(glanceSound, ChestPoint, 0.8f, "father_moth_glance");
            return HookResult.Shrugged;
        }

        // ---- moving

        // Towards `goal`, easing in, never out of his glasshouse.
        void Steer(Vector3 goal, float dt, float pace)
        {
            Vector3 want = Vector3.ClampMagnitude((goal - transform.position) * 2f, 7f * pace);
            velocity = Vector3.MoveTowards(velocity, want, 14f * dt);
            Hold(transform.position + velocity * dt);
        }

        // (outside his bounds, as off his tree, he's drawn back in, not snapped)
        void Hold(Vector3 p)
        {
            float step = 6f * Time.deltaTime;
            p.x = Mathf.MoveTowards(p.x, Mathf.Clamp(p.x, middle.x - reach.x, middle.x + reach.x), step);
            p.z = Mathf.MoveTowards(p.z, Mathf.Clamp(p.z, middle.z - reach.y, middle.z + reach.y), step);
            p.y = Mathf.Clamp(p.y, floorY, floorY + 6f);
            PlaceAt(p);
        }

        // The lantern in his ribs: flaring as he raises his wings to beat, dim when he's spent.
        void Chest()
        {
            if (chest == null)
                return;
            float k = CurrentState == State.Telegraph && CurrentAttack == beat ? 1f + 2.5f * Mathf.Clamp01(StateTime / Mathf.Max(0.01f, beat.telegraph))
                    : mode == Mode.Spent ? 0.35f + 0.15f * Mathf.Sin(Time.time * 3f)
                    : 0.8f + 0.2f * Mathf.PerlinNoise(Time.time * 5f, 0.4f);
            chest.enabled = IsAlive || StateTime < deathDuration * 0.5f;
            chest.intensity = chestIntensity * k * (IsAlive ? 1f : 1f - Mathf.Clamp01(StateTime / (deathDuration * 0.5f)));
        }

        // ---- the fight, and its end

        void LateUpdate()
        {
            bool on = Woken && IsAlive && target != null && target.IsAlive && InHouse(target.transform.position);
            if (on)
                BossFight.Begin(this);
            else if (fightOn)
                BossFight.End(this);
            fightOn = on;
            // The camera: leaning to him (and out a little) while he's fought, and as he falls; before he wakes, once
            // you're in, a little towards his tree (something there, folded on it).
            bool asleep = !Woken && IsAlive && target != null && target.IsAlive && InHouse(target.transform.position);
            int frame = on || (CurrentState == State.Dying && target != null && target.IsAlive) ? 2 : asleep ? 1 : 0;
            if (focus != null)
                focus.position = transform.position + Vector3.up * 1.5f;
            if (frame != framing)
            {
                framing = frame;
                CameraRig.Instance?.SetFightFocus(frame > 0 ? focus : null, frame == 2 ? cameraLean : sleepingLean);
                CameraRig.Instance?.SetZoom(frame == 2 ? cameraZoom : frame == 1 ? sleepingZoom : 1f);
            }
        }

        // Beaten: he folds and falls; the lantern in his ribs goes out; his leaves fall where they are.
        void Died()
        {
            volleyLeft = 0;
            Sfx.Instance?.Attack(deathSound, transform.position + Vector3.up * 2f, 1f, "father_moth_death");
            Effects.Instance?.Leaves(transform.position + Vector3.up * 2f, Vector3.zero, 80);
            foreach (var leaf in FindObjectsByType<DriftLeaf>(FindObjectsSortMode.None))
                Destroy(leaf.gameObject);
            BossFight.Defeat(this);
            BossFight.End(this);
            fightOn = false;
        }

        void Fall(float dt)
        {
            if (CurrentState != State.Dying || transform.position.y <= floorY + 0.01f)
                return;
            velocity.y -= 14f * dt;
            Hold(transform.position + new Vector3(velocity.x * 0.3f, velocity.y, velocity.z * 0.3f) * dt);
        }

        public override void Reform()
        {
            SetSpawn(perch, perchRotation);
            base.Reform();
            Woken = false;
            passive = true;
            mode = Mode.Hovering;
            beatsDone = volleyLeft = 0;
            Thrown = null;
            velocity = Vector3.zero;
            foreach (var leaf in FindObjectsByType<DriftLeaf>(FindObjectsSortMode.None))
                Destroy(leaf.gameObject);
            BossFight.End(this);
            fightOn = false;
        }

        void OnDestroy()
        {
            if (focus != null)
                Destroy(focus.gameObject);
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
