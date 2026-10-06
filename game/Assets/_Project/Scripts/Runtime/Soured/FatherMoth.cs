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
    // while, open to the oar; then he's up again. Four phases (at three quarters, half, a quarter), each its own run of
    // patterns, more and more of them: phase 1 leaves alone (a fan, three aimed, a ring); phase 2 more at once, and seed
    // pods that burst into rings, withered oranges shaken down from the vault, a spiral stream (phases 3 and 4 use phase
    // 2's for now). No blow carries him through a phase: brought down to its mark, he's untouchable until he's up again,
    // and his next phase begins. Fire burns him badly. The Hook glances off him.
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
        public float phaseOneTell = 0.6f, phaseOneBreath = 0.8f; // phase 1's beats: the tell, the breath after
        public float leafSpeed = 6.5f, leafDamage = 10f;
        public float fanSpread = 70f;         // the fan: this wide
        public int fanCount = 5, ringCount = 14, volleyCount = 3;
        public float volleyInterval = 0.35f;  // the aimed ones: one after another
        public Material leafMaterial;

        [Header("Phase 2 on: more at once, and more kinds")]
        public float hecticSpeed = 7.5f;      // their leaves, quicker
        public float hecticTell = 0.5f, hecticBreath = 0.5f; // his beats, quicker (phase 1: the beat's own)
        public int wideFanCount = 9, wideRingCount = 18;
        public float wideFanSpread = 110f;
        public float spiralTime = 2.2f, spiralEvery = 0.09f, spiralTurn = 150f; // a stream from both wingtips, as he turns
        public float ringTime = 0.6f;         // a ring: once round, this quickly
        public int podCount = 3, podLeaves = 6;
        public int fruitCount = 5;
        public float fruitFall = 1.1f, fruitRadius = 1.1f, fruitDamage = 14f;
        public Material podMaterial, fruitMaterial;
        public AudioClip[] podSound, fruitSound;

        [Header("Spent")]
        public float sinkTime = 1.8f, collapseTime = 0.5f, spentTime = 5f, riseTime = 0.9f;
        public float tiredSag = 0.9f;         // how far he's sunk by his last beat

        [Header("Look and sound")]
        public Light chest;                   // the lantern in his ribs
        public float chestIntensity = 1.4f;
        public float cameraLean = 0.5f, cameraZoom = 1.55f, sleepingLean = 0.45f, sleepingZoom = 1.15f; // his fight: the camera leans to him, and takes in more of the floor
        public AudioClip[] wingSound, wakeSound, beatSound, downedSound, glanceSound, deathSound;

        public enum Pattern { Fan, Volley, Ring, WideFan, WideRing, Spiral, SpiralBack, Pods, Fruit }
        enum Mode { Hovering, Sinking, Spent, Rising }

        // Each phase's run of beats, in order (then he's spent); a beat throws everything in it at once.
        static readonly Pattern[][] PhaseOne =
        {
            new[] { Pattern.Fan }, new[] { Pattern.Volley }, new[] { Pattern.Fan }, new[] { Pattern.Ring }, new[] { Pattern.Volley }, new[] { Pattern.Fan },
        };
        static readonly Pattern[][] PhaseTwo =
        {
            new[] { Pattern.Spiral }, new[] { Pattern.Pods }, new[] { Pattern.Fruit, Pattern.Fan }, new[] { Pattern.WideRing, Pattern.Volley },
            new[] { Pattern.SpiralBack }, new[] { Pattern.Fruit, Pattern.Pods }, new[] { Pattern.WideFan },
        };
        Pattern[][] Run => CurrentPhase == 1 ? PhaseOne : PhaseTwo; // (phases 3 and 4: their own runs, still to come)

        public bool Woken { get; private set; }
        public bool Grounded => mode == Mode.Spent && !collapsing;
        public bool Spent => mode == Mode.Spent && !collapsing;  // sprawled on the floor (not still dropping)
        public bool Beating => CurrentAttack == beat && (CurrentState == State.Telegraph || CurrentState == State.Strike);
        public Pattern[] Thrown { get; private set; } // the patterns of the beat now (or last)
        public int BeatsLeft => Run.Length - beatsDone;
        public int CurrentPhase { get; private set; } = 1;
        public bool Untouchable => CurrentPhase < 4 && Health <= Floor + 0.01f; // (brought to his phase's mark: until he's up)
        public float Height => transform.position.y - floorY;
        public static event System.Action<FatherMoth> Woke, Exhausted, PhaseBegun; // (tests)
        public static event System.Action<FatherMoth, Pattern> Beat;

        public string Title => title;
        public string Epithet => epithet;
        public float[] PhaseMarks => new[] { 0.75f, 0.5f, 0.25f };
        public int Phase => CurrentPhase;
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
        float nextVolley, nextWing, streamUntil, nextStream, streamAngle, streamSign, streamEvery, streamTurn;
        int flicks;
        bool collapsing;                       // spent: hitting the floor (then sprawled)
        bool stirred;                          // brought to his mark, spent: he's stirring, and soon up
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

        Vector3 HoverPoint => new Vector3(hover.x + Mathf.Sin(Time.time * 0.7f) * 0.6f, floorY + hoverHeight - tiredSag * Tiredness + Mathf.Sin(Time.time * 1.9f) * 0.15f, hover.z);

        // How spent he is (0 fresh, 1 at his last beat): he sags, his beats grow heavy, his lantern dims, he wobbles.
        public float Tiredness => mode == Mode.Hovering ? Mathf.Clamp01(beatsDone / (float)Mathf.Max(1, Run.Length)) : 0f;

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
            if (!Woken || mode != Mode.Hovering || (transform.position - HoverPoint).magnitude > 1.5f || volleyLeft > 0 || Time.time < streamUntil)
                return null;
            var run = Run;
            if (beatsDone >= run.Length)
            {
                Sink();
                return null;
            }
            Thrown = run[beatsDone];
            beatsDone++;
            // (each throw its own motion: a beat forward for a fan; a spin for a ring or a spiral; a wing flicked for each
            // aimed one; a shudder for pods; reaching up to strike the vault for its oranges)
            switch (Thrown[0])
            {
                case Pattern.Ring: case Pattern.WideRing: case Pattern.Spiral: case Pattern.SpiralBack:
                    beat.windupClip = "GustDraw"; beat.strikeClip = "Spin"; break;
                case Pattern.Volley:
                    beat.windupClip = "GustDraw"; beat.strikeClip = "FlickL"; break;
                case Pattern.Pods:
                    beat.windupClip = "Shake"; beat.strikeClip = "FlickR"; break;
                case Pattern.Fruit:
                    beat.windupClip = "Tear"; beat.strikeClip = "Gust"; break;
                default:
                    beat.windupClip = "GustDraw"; beat.strikeClip = "Gust"; break;
            }
            beat.telegraph = CurrentPhase == 1 ? phaseOneTell : hecticTell;
            beat.recover = CurrentPhase == 1 ? phaseOneBreath : hecticBreath;
            return beat;
        }

        protected override void OnEntered(State state)
        {
            if (state == State.Telegraph && CurrentAttack == beat)
            {
                Effects.Instance?.AlertFlare(ChestPoint);
                Sfx.Instance?.Attack(wingSound, transform.position + Vector3.up * 2f, 0.8f, "father_moth_wing");
            }
            if (state == State.Strike && CurrentAttack == beat && Thrown != null)
            {
                Sfx.Instance?.Attack(beatSound, transform.position + Vector3.up * 2f, 1f, "father_moth_beat");
                Effects.Instance?.Leaves(transform.position + Vector3.up * 1.5f, Vector3.zero, 25);
                foreach (var pattern in Thrown)
                    Throw(pattern);
            }
            if (state == State.Dying)
                Died();
        }

        Vector3 ChestPoint => chest != null ? chest.transform.position : transform.position + Vector3.up * 2.5f;

        // The leaves (and pods, and fruit), thrown from under him at chest height.
        void Throw(Pattern pattern)
        {
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
                    Spin(aim, 360f / ringTime, ringTime / (ringCount / 2f), ringTime, Random.value < 0.5f ? 1f : -1f);
                    break;
                case Pattern.Volley:
                    volleyLeft = volleyCount;
                    nextVolley = Time.time;
                    break;
                case Pattern.WideFan:
                    for (int i = 0; i < wideFanCount; i++)
                        Leaf(Quaternion.Euler(0f, Mathf.Lerp(-wideFanSpread / 2f, wideFanSpread / 2f, i / (wideFanCount - 1f)), 0f) * aim);
                    break;
                case Pattern.WideRing:
                    Spin(aim, 360f / ringTime, ringTime / (wideRingCount / 2f), ringTime, Random.value < 0.5f ? 1f : -1f);
                    break;
                case Pattern.Spiral:
                case Pattern.SpiralBack:
                    Spin(aim, spiralTurn, spiralEvery, spiralTime, pattern == Pattern.Spiral ? 1f : -1f);
                    break;
                case Pattern.Pods:
                    for (int i = 0; i < podCount; i++)
                    {
                        Vector3 spot = Inside(target != null ? target.transform.position + (i == 0 ? Vector3.zero : Flat(Random.insideUnitSphere).normalized * Random.Range(1.5f, 3f)) : middle);
                        SeedPod.Lob(new Vector3(transform.position.x, floorY + 1.6f, transform.position.z), new Vector3(spot.x, floorY + 0.9f, spot.z), target,
                            podMaterial, leafMaterial, podLeaves, LeafSpeed, leafDamage, middle, walls, podSound);
                    }
                    break;
                case Pattern.Fruit:
                    for (int i = 0; i < fruitCount; i++)
                    {
                        Vector3 spot = Inside(target != null ? target.transform.position + (i == 0 ? Vector3.zero : Flat(Random.insideUnitSphere).normalized * Random.Range(1.6f, 4f)) : middle);
                        FallingFruit.Drop(new Vector3(spot.x, floorY, spot.z), target, fruitMaterial, fruitFall + i * 0.12f, fruitRadius, fruitDamage, fruitSound);
                    }
                    break;
            }
        }

        // He spins, his wings flat out, a leaf off each wingtip as it sweeps round (a ring: once round, quickly; a spiral:
        // slower, longer).
        void Spin(Vector3 aim, float turn, float every, float time, float sign)
        {
            streamUntil = Time.time + time;
            nextStream = Time.time;
            streamEvery = every;
            streamTurn = turn;
            streamAngle = Vector3.SignedAngle(Vector3.forward, aim, Vector3.up);
            streamSign = sign;
        }

        // Somewhere on the floor, inside the walls.
        Vector3 Inside(Vector3 p) => new Vector3(Mathf.Clamp(p.x, middle.x - walls.x + 1f, middle.x + walls.x - 1f), p.y,
                                                 Mathf.Clamp(p.z, middle.z - walls.y + 1f, middle.z + walls.y - 1f));

        float LeafSpeed => CurrentPhase == 1 ? leafSpeed : hecticSpeed;

        void Leaf(Vector3 direction, float outFrom = 1.2f)
        {
            Vector3 from = new Vector3(transform.position.x, floorY + 0.9f, transform.position.z) + direction * outFrom;
            DriftLeaf.Fling(from, direction, LeafSpeed, leafDamage, target, leafMaterial, middle, walls);
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
                if (Time.time >= streamUntil) // (spinning, he throws them from the spin)
                    clips.PlayOnce(flicks++ % 2 == 0 ? "FlickL" : "FlickR", 0.3f); // (a wing snapped down for each)
                Sfx.Instance?.Attack(wingSound, transform.position + Vector3.up * 2f, 0.4f, "father_moth_wing", 1.3f);
            }
            // Spinning: a leaf off each wingtip as it sweeps round (his wings point along the stream).
            if (Time.time < streamUntil)
            {
                streamAngle += streamSign * streamTurn * Time.deltaTime;
                transform.rotation = Quaternion.Euler(0f, streamAngle - 90f, 0f);
                if (clips.Current != "Spin")
                    clips.Loop("Spin", 0.1f);
                if (Time.time >= nextStream)
                {
                    nextStream = Time.time + streamEvery;
                    for (int arm = 0; arm < 2; arm++)
                    {
                        Vector3 dir = Quaternion.Euler(0f, streamAngle + arm * 180f, 0f) * Vector3.forward;
                        Leaf(dir, 2.6f);
                    }
                }
            }
            // Tired, near the end of his run: he wobbles in the air.
            else if (mode == Mode.Hovering && Tiredness > 0.6f)
            {
                float lean = Mathf.Sin(Time.time * 3.1f) * 14f * (Tiredness - 0.6f) / 0.4f;
                transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, lean);
            }
            // Brought to his mark while spent: he stirs, and is soon up (untouchable meanwhile).
            if (mode == Mode.Spent && !collapsing && Untouchable && !stirred && CurrentState == State.Busy)
            {
                stirred = true;
                if (StateTime < spentTime - 1.2f)
                    BeBusy("Downed", 1.2f);
            }
            switch (mode)
            {
                case Mode.Sinking:
                    // Faltering, wing over wing, lower and lower (the last of it, a drop).
                    float u = Mathf.Clamp01(StateTime / sinkTime);
                    Vector3 down = new Vector3(transform.position.x, Mathf.Lerp(floorY + hoverHeight - tiredSag, floorY + 0.6f, u * u), transform.position.z);
                    Hold(new Vector3(transform.position.x, Mathf.MoveTowards(transform.position.y, down.y, 4f * dt), transform.position.z));
                    transform.rotation = Quaternion.Euler(Mathf.Sin(Time.time * 7f) * 6f, transform.eulerAngles.y, Mathf.Sin(Time.time * 4.3f) * 12f);
                    if (CurrentState != State.Busy)
                        Spend();
                    break;
                case Mode.Spent:
                    if (collapsing && CurrentState == State.Busy)
                        Hold(new Vector3(transform.position.x, Mathf.MoveTowards(transform.position.y, floorY, 6f * dt), transform.position.z));
                    else
                        Hold(new Vector3(transform.position.x, floorY, transform.position.z));
                    if (CurrentState != State.Busy && CurrentState != State.Staggered)
                    {
                        if (collapsing)
                            Sprawl();
                        else
                            RiseAgain();
                    }
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
                        // (his beats heavier as he tires: slower, lower)
                        nextWing = Time.time + 0.9f + 0.5f * Tiredness;
                        Sfx.Instance?.Attack(wingSound, transform.position + Vector3.up * 2f, 0.5f, "father_moth_wing", 1f - 0.2f * Tiredness);
                    }
                    clips.SetLoopSpeed(clips.Current == "Fly" ? 1f - 0.4f * Tiredness : 1f);
                    break;
            }
            Chest();
        }

        // Spent: his wings falter, beat by weak beat, and he sinks.
        void Sink()
        {
            mode = Mode.Sinking;
            clips.SetLoopSpeed(1f);
            BeBusy("Falter", sinkTime);
            Exhausted?.Invoke(this);
        }

        // The last of it: he drops, and hits the floor.
        void Spend()
        {
            mode = Mode.Spent;
            collapsing = true;
            BeBusy("Collapse", collapseTime);
        }

        // On the floor, sprawled, heaving: open to the oar a while.
        void Sprawl()
        {
            collapsing = false;
            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
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
            stirred = false;
            if (Untouchable)
            {
                // Up, and his next phase begins: a burst of leaves off him, the lantern flaring.
                CurrentPhase++;
                Effects.Instance?.Leaves(transform.position + Vector3.up * 1.5f, Vector3.up * 3f, 60);
                Effects.Instance?.AlertFlare(ChestPoint);
                CameraRig.Instance?.Shake(0.3f);
                PhaseBegun?.Invoke(this);
            }
            BeBusy("Rise", riseTime);
        }

        // As low as his health goes in this phase: its mark (the last phase, none).
        protected override float Floor => CurrentPhase < 4 ? maxHealth * PhaseMarks[CurrentPhase - 1] : 0f;

        // At his mark: nothing gets through till he's up (a puff of dust off him, no harm).
        protected override bool Absorbs(Hit hit, bool fire)
        {
            if (!Untouchable)
                return false;
            Vector3 towards = Flat(hit.From - transform.position);
            Effects.Instance?.Leaves(transform.position + Vector3.up * 1.0f + towards.normalized * 0.8f, Vector3.zero, fire ? 2 : 10);
            if (!fire)
                Sfx.Instance?.Attack(glanceSound, transform.position + Vector3.up, 0.5f, "father_moth_untouchable", 0.7f);
            return true;
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
            float k = Untouchable && mode == Mode.Spent ? 2.2f + 0.6f * Mathf.Sin(Time.time * 14f) // (stirring: the light coming back)
                    : CurrentState == State.Telegraph && CurrentAttack == beat ? 1f + 2.5f * Mathf.Clamp01(StateTime / Mathf.Max(0.01f, beat.telegraph))
                    : mode == Mode.Spent || mode == Mode.Sinking ? 0.35f + 0.15f * Mathf.Sin(Time.time * 3f)
                    : (0.8f + 0.2f * Mathf.PerlinNoise(Time.time * 5f, 0.4f)) * (1f - 0.5f * Tiredness); // (dimming as he tires)
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
            streamUntil = 0f;
            Sfx.Instance?.Attack(deathSound, transform.position + Vector3.up * 2f, 1f, "father_moth_death");
            Effects.Instance?.Leaves(transform.position + Vector3.up * 2f, Vector3.zero, 80);
            Clear();
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
            collapsing = false;
            CurrentPhase = 1;
            beatsDone = volleyLeft = 0;
            streamUntil = 0f;
            stirred = false;
            Thrown = null;
            velocity = Vector3.zero;
            Clear();
            BossFight.End(this);
            fightOn = false;
        }

        // Everything he's thrown, gone (his leaves, pods and fruit).
        static void Clear()
        {
            foreach (var leaf in FindObjectsByType<DriftLeaf>(FindObjectsSortMode.None))
                Destroy(leaf.gameObject);
            foreach (var pod in FindObjectsByType<SeedPod>(FindObjectsSortMode.None))
                Destroy(pod.gameObject);
            foreach (var fruit in FindObjectsByType<FallingFruit>(FindObjectsSortMode.None))
                Destroy(fruit.gameObject);
        }

        void OnDestroy()
        {
            if (focus != null)
                Destroy(focus.gameObject);
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
