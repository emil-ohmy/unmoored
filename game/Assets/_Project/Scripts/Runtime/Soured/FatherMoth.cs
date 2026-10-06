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
    // Phase 3 (from half): he doesn't fly up again. He heaves himself onto his forelegs and comes after you on foot,
    // relentless: quick snips (two or three at a time), a wide sweep of his shears, a lunge across the floor when you're
    // off. His shears turn your blows; two ways through: dodge him till he's spent (a dozen strikes, and he collapses,
    // open, as ever), or turn a strike with a perfect block (stunned, a riposte's chance). Brought to the quarter mark,
    // he takes to the air again: phase 4, the last. He tears the lamps down one by one, and it goes dark, darker than the
    // night outside: the hollow in his belly, your bow lantern, his smouldering leaves. He no longer tires of his own
    // accord; among his throws come embers, leaves burning bright: turn one with a perfect block and it flies back into
    // his hollow, and he chokes on it. Three, and down he comes.
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

        [Header("Phase 3: afoot, relentless")]
        public SouredAttack snip = new SouredAttack();   // quick, often two or three at a time
        public SouredAttack sweep = new SouredAttack();  // wide
        public SouredAttack lunge = new SouredAttack();  // across the floor at you when you're off
        public float footSpeed = 4.2f;
        public int strikesBeforeSpent = 12;
        public float meleeStun = 1.6f;        // a strike turned on a perfect block: stunned this long
        public float roused = 1.5f;           // up off the floor onto his forelegs, wings flung wide

        [Header("Phase 4: the dark, and his embers")]
        public Transform[] lampModels;        // the hung lamps (torn down as phase 4 begins)
        public Light[] lamps;                 // their lights
        public float darkest = 0.07f;         // the night's light with every lamp down (the moon, the ambient)
        public int embersToFell = 3;          // embers turned back into him: then he's down
        public float emberSpeed = 5.5f, emberDamage = 18f;
        public Material emberMaterial;
        public AudioClip[] tearSound, shatterSound, emberTurnSound, chokeSound;

        [Header("Spent")]
        public float sinkTime = 1.8f, collapseTime = 0.5f, spentTime = 5f, riseTime = 0.9f;
        public float tiredSag = 0.9f;         // how far he's sunk by his last beat

        [Header("Look and sound")]
        public Light chest;                   // the lantern in his ribs
        public Transform hollow;              // the autumn hollow on his back: his leaves come out of it (it throbs)
        public Light hollowLight;
        public float hollowIntensity = 2f;
        public float chestIntensity = 1.4f;
        public float cameraLean = 0.5f, cameraZoom = 1.55f, sleepingLean = 0.45f, sleepingZoom = 1.15f; // his fight: the camera leans to him, and takes in more of the floor
        public AudioClip[] wingSound, wakeSound, beatSound, downedSound, glanceSound, deathSound;

        public enum Pattern { Fan, Volley, Ring, WideFan, WideRing, Spiral, SpiralBack, Pods, Fruit, Ember }
        enum Mode { Hovering, Sinking, Spent, Rising, Afoot }

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
        static readonly Pattern[][] PhaseFour =
        {
            new[] { Pattern.Ember, Pattern.Fan }, new[] { Pattern.Spiral }, new[] { Pattern.Ember, Pattern.Pods }, new[] { Pattern.Fruit, Pattern.Volley },
            new[] { Pattern.Ember, Pattern.WideRing }, new[] { Pattern.SpiralBack }, new[] { Pattern.Ember, Pattern.Fruit }, new[] { Pattern.WideFan, Pattern.Volley },
        };
        Pattern[][] Run => CurrentPhase == 1 ? PhaseOne : CurrentPhase == 4 ? PhaseFour : PhaseTwo; // (phase 3 is afoot: no run)

        public bool Woken { get; private set; }
        public int LampsLeft { get; private set; }
        public int Swallowed { get; private set; }     // phase 4: embers turned back into him, this time
        public bool Afoot => mode == Mode.Afoot;       // phase 3: on the floor, coming after you
        public int Strikes => strikes;
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
        protected override bool BlocksHit(Hit hit) => mode == Mode.Afoot && CurrentState != State.Stunned; // (afoot, his shears turn your blows)
        protected override Vector3 ChaseStep(Vector3 toTarget, float distance) =>
            mode == Mode.Afoot && distance > 2.4f && target != null ? PathDirection(target.transform.position) * footSpeed * (1f - 0.3f * Tiredness) : Vector3.zero;
        protected override bool OnItsLevel() => true;
        protected override string IdleClip => Woken ? "Fly" : "Perched";
        protected override string ChaseClip(float distance) => mode == Mode.Afoot ? (distance > 2.4f ? "Scuttle" : "Land") : "Fly";
        protected override bool LosesTarget(float distance) => !InHouse(target.transform.position);

        Vector3 velocity, perch;
        Quaternion perchRotation;
        Mode mode;
        int beatsDone, volleyLeft;
        float nextVolley, nextWing, streamUntil, nextStream, streamAngle, streamSign, streamEvery, streamTurn;
        int flicks, strikes, snipsLeft;
        float kick;                            // the hollow's throb from a leaf just thrown
        bool collapsing;                       // spent: hitting the floor (then sprawled)
        bool stirred;                          // brought to his mark, spent: he's stirring, and soon up
        bool fightOn;
        (Transform lamp, Vector3 position, Quaternion rotation, float intensity)[] hung;
        float[] fallSpeed;
        int nextLamp;
        float dropAt = -1f;
        Light moon;
        float moonIntensity, darkness = 1f, darkTarget = 1f;
        Color sky, equator, ground;
        Transform focus;                       // his body (the camera leans to it: halfway, so you're both in view)
        int framing;                           // 0 not, 1 asleep on his tree, 2 his fight

        protected override void Awake()
        {
            base.Awake();
            perch = transform.position;
            perchRotation = transform.rotation;
            focus = new GameObject("Father Moth (camera focus)").transform;
            stunDuration = meleeStun;
            hung = new (Transform, Vector3, Quaternion, float)[lamps?.Length ?? 0];
            fallSpeed = new float[hung.Length];
            for (int i = 0; i < hung.Length; i++)
            {
                var root = lampModels != null && i < lampModels.Length ? lampModels[i] : null;
                var flicker = lamps[i] != null ? lamps[i].GetComponent<LightFlicker>() : null;
                float intensity = flicker != null ? flicker.BaseIntensity : lamps[i] != null ? lamps[i].intensity : 0f;
                hung[i] = (root, root != null ? root.position : Vector3.zero, root != null ? root.rotation : Quaternion.identity, intensity);
            }
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional)
                    moon = l;
            moonIntensity = moon != null ? moon.intensity : 0f;
            sky = RenderSettings.ambientSkyColor;
            equator = RenderSettings.ambientEquatorColor;
            ground = RenderSettings.ambientGroundColor;
        }

        protected override void Start()
        {
            base.Start();
            Reform();
        }

        // Inside his glasshouse (its walls, not just where he flies).
        bool InHouse(Vector3 p) => Mathf.Abs(p.x - middle.x) < walls.x + 0.5f && Mathf.Abs(p.z - middle.z) < walls.y + 0.5f && p.y > floorY - 1f && p.y < floorY + 8f;

        Vector3 HoverPoint => new Vector3(hover.x + Mathf.Sin(Time.time * 0.7f) * 0.6f, floorY + hoverHeight - tiredSag * Tiredness + Mathf.Sin(Time.time * 1.9f) * 0.15f, hover.z);

        // How spent he is (0 fresh, 1 at his last beat): he sags, his beats grow heavy, his lantern dims, he wobbles.
        public float Tiredness => mode == Mode.Hovering && CurrentPhase == 4 ? Mathf.Clamp01(Swallowed / (float)Mathf.Max(1, embersToFell))
                                : mode == Mode.Hovering ? Mathf.Clamp01(beatsDone / (float)Mathf.Max(1, Run.Length))
                                : mode == Mode.Afoot ? Mathf.Clamp01(strikes / (float)Mathf.Max(1, strikesBeforeSpent)) : 0f;

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
            if (mode == Mode.Afoot)
                return Strike(distance);
            if (!Woken || mode != Mode.Hovering || (transform.position - HoverPoint).magnitude > 1.5f || volleyLeft > 0 || Time.time < streamUntil)
                return null;
            if (CurrentPhase == 4 && nextLamp < hung.Length)
                return null; // (the lamps first)
            var run = Run;
            if (beatsDone >= run.Length)
            {
                if (CurrentPhase == 4)
                    beatsDone = 0; // (in the dark he doesn't tire of himself: only his embers bring him down)
                else
                {
                    Sink();
                    return null;
                }
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
                case Pattern.Ember:
                    Leaf(aim, emberSpeed, emberDamage).Kindle(this, emberMaterial, emberTurnSound);
                    break;
                case Pattern.Pods:
                    for (int i = 0; i < podCount; i++)
                    {
                        Vector3 spot = Inside(target != null ? target.transform.position + (i == 0 ? Vector3.zero : Flat(Random.insideUnitSphere).normalized * Random.Range(1.5f, 3f)) : middle);
                        SeedPod.Lob(HollowPoint + Vector3.up * 0.2f, new Vector3(spot.x, floorY + 0.9f, spot.z), target,
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

        // A leaf out of the autumn hollow on his back: up out of it, then sinking to chest height as it comes.
        DriftLeaf Leaf(Vector3 direction, float speed = -1f, float damage = -1f)
        {
            Vector3 from = HollowPoint + Vector3.up * 0.2f + direction * 0.4f;
            kick = Mathf.Min(kick + 0.35f, 0.6f); // (the hollow throbs with each)
            return DriftLeaf.Fling(from, direction, speed > 0f ? speed : LeafSpeed, damage > 0f ? damage : leafDamage, target, leafMaterial, middle, walls,
                settleY: floorY + 0.9f);
        }

        // An ember turned back into his hollow: he chokes on it (a gout of flame, his flight faltering); enough, and down.
        public void SwallowEmber()
        {
            if (!IsAlive || mode != Mode.Hovering)
                return;
            Swallowed++;
            for (int i = 0; i < 30; i++)
                Effects.Instance?.Flame(HollowPoint + Random.insideUnitSphere * 0.3f, Random.insideUnitSphere * 2f + Vector3.up * 1.5f, Random.Range(0.3f, 0.6f), Random.Range(0.12f, 0.25f));
            Effects.Instance?.AlertFlare(ChestPoint);
            Sfx.Instance?.Attack(chokeSound, HollowPoint, 1f, "father_moth_choke");
            CameraRig.Instance?.Shake(0.3f);
            HitStop.Trigger(0.12f);
            volleyLeft = 0;
            streamUntil = 0f;
            if (Swallowed >= embersToFell)
            {
                Swallowed = 0;
                Sink();
            }
            else
                BeBusy("Stagger", 0.8f);
        }

        public Vector3 HollowPoint => hollow != null ? hollow.position : transform.position + Vector3.up * 1.8f;

        // The hollow: throbbing slowly; swelling and brightening through a tell; a kick with every leaf; a trickle of
        // leaves out of it always (more as it swells).
        void Hollow()
        {
            kick = Mathf.MoveTowards(kick, 0f, Time.deltaTime * 2.5f);
            bool telling = CurrentState == State.Telegraph && CurrentAttack == beat;
            float swell = telling ? 0.35f * Mathf.Clamp01(StateTime / Mathf.Max(0.01f, beat.telegraph)) : 0f;
            float dim = !IsAlive ? 0f : mode == Mode.Spent || mode == Mode.Sinking ? 0.3f : 1f - 0.4f * Tiredness;
            float pulse = 1f + 0.06f * Mathf.Sin(Time.time * 2.4f) + swell + kick;
            if (hollow != null)
                hollow.localScale = Vector3.one * (dim > 0f ? pulse : 0.7f);
            if (hollowLight != null)
            {
                hollowLight.enabled = dim > 0f;
                hollowLight.intensity = hollowIntensity * (CurrentPhase == 4 ? 1.6f : 1f) * dim * (0.7f + 1.6f * (swell + kick) + 0.15f * Mathf.Sin(Time.time * 2.4f));
            }
            if (Woken && IsAlive && Random.value < (telling ? 26f : 3f) * dim * Time.deltaTime)
                Effects.Instance?.Leaves(HollowPoint + Vector3.up * 0.15f, Vector3.up * Random.Range(1f, 2.2f), 1);
        }

        // Afoot (phase 3): snips (two or three at a time), a sweep, a lunge when you're off; spent after so many.
        SouredAttack Strike(float distance)
        {
            if (strikes >= strikesBeforeSpent)
            {
                Spend();
                return null;
            }
            SouredAttack next = null;
            if (snipsLeft > 0 && distance <= snip.reach + 1f)
            {
                snipsLeft--;
                next = snip;
            }
            else if (distance > 4.5f && distance < 9f)
                next = lunge;
            else if (distance <= sweep.reach)
            {
                if (Random.value < 0.55f)
                {
                    snipsLeft = Random.Range(1, 3); // (two or three in all)
                    next = snip;
                }
                else
                    next = sweep;
            }
            if (next != null)
                strikes++;
            return next;
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
                        Leaf(dir);
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
            if (mode == Mode.Afoot && Untouchable && CurrentState != State.Busy)
                TakeWing();
            switch (mode)
            {
                case Mode.Afoot:
                    Hold(new Vector3(transform.position.x, floorY, transform.position.z));
                    break;
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
                    if (CurrentPhase == 4 && nextLamp < hung.Length)
                    {
                        TearStep(dt);
                        break;
                    }
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
            Lights();
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
            beatsDone = 0;
            stirred = false;
            // Phase 3: not up into the air, but onto his forelegs, after you (and so on, till the quarter mark).
            if ((CurrentPhase == 2 && Untouchable) || (CurrentPhase == 3 && !Untouchable))
            {
                if (CurrentPhase == 2)
                {
                    CurrentPhase = 3;
                    PhaseBegun?.Invoke(this);
                }
                Rouse();
                return;
            }
            mode = Mode.Rising;
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

        // ---- the lamps torn down (phase 4), and the dark

        // To the next lamp, up under it; there, he tears it down.
        void TearStep(float dt)
        {
            if (CurrentState == State.Busy)
            {
                Hover(dt);
                if (dropAt > 0f && Time.time >= dropAt)
                    TearDown();
                return;
            }
            if (hung[nextLamp].lamp == null || lamps[nextLamp] == null)
            {
                nextLamp++;
                return;
            }
            Vector3 goal = lamps[nextLamp].transform.position + Vector3.down * 0.9f;
            Steer(goal, dt, 1f);
            if ((transform.position - goal).magnitude < 0.8f && CurrentState == State.Chase)
            {
                BeBusy("Tear", 1.3f);
                dropAt = Time.time + 0.8f;
                Sfx.Instance?.Attack(tearSound, goal, 1f, "father_moth_tear");
            }
        }

        void TearDown()
        {
            dropAt = -1f;
            int i = nextLamp++;
            if (i < 0 || i >= hung.Length)
                return;
            fallSpeed[i] = 0.01f; // (falling: Lights)
            LampsLeft = Mathf.Max(0, LampsLeft - 1);
            darkTarget = Mathf.Lerp(darkest, 1f, LampsLeft / (float)Mathf.Max(1, hung.Length));
        }

        // The lamps (falling, out), the dark (the moon and the ambient, at `darkness` of themselves).
        void Lights()
        {
            for (int i = 0; i < hung.Length; i++)
            {
                if (fallSpeed[i] <= 0f || hung[i].lamp == null)
                    continue;
                var lamp = hung[i].lamp;
                fallSpeed[i] += 18f * Time.deltaTime;
                lamp.position += Vector3.down * fallSpeed[i] * Time.deltaTime;
                lamp.rotation = Quaternion.RotateTowards(lamp.rotation, hung[i].rotation * Quaternion.Euler(70f, 0f, 20f), 200f * Time.deltaTime);
                Intensity(lamps[i], hung[i].intensity * Mathf.Clamp01(1f - fallSpeed[i] / 8f));
                if (lamps[i].transform.position.y <= floorY + 0.25f)
                {
                    fallSpeed[i] = -1f; // (shattered)
                    Intensity(lamps[i], 0f);
                    lamps[i].enabled = false;
                    foreach (var r in lamp.GetComponentsInChildren<Renderer>())
                        r.enabled = false;
                    Effects.Instance?.Rubble(new Vector3(lamps[i].transform.position.x, floorY, lamps[i].transform.position.z), 0.6f);
                    Sfx.Instance?.Attack(shatterSound, lamps[i].transform.position, 1f, "glass_lamp_shatter");
                }
            }
            if (IsAlive)
            {
                darkness = Mathf.MoveTowards(darkness, darkTarget, Time.deltaTime * 0.5f);
                Dim(darkness);
            }
        }

        static void Intensity(Light light, float value)
        {
            var flicker = light.GetComponent<LightFlicker>();
            if (flicker != null)
                flicker.BaseIntensity = value;
            else
                light.intensity = value;
        }

        // The night's light, at `k` of itself (the moon, the ambient); never his lamps (they go one by one), nor your lantern.
        void Dim(float k)
        {
            if (moon != null)
                moon.intensity = moonIntensity * k;
            RenderSettings.ambientSkyColor = sky * k;
            RenderSettings.ambientEquatorColor = equator * k;
            RenderSettings.ambientGroundColor = ground * k;
        }

        // He's gone: the night comes back in (the moon, the ambient; not his lamps).
        System.Collections.IEnumerator NightReturns()
        {
            float from = darkness;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 3f)
            {
                darkness = Mathf.Lerp(from, 1f, t);
                Dim(darkness);
                yield return null;
            }
            darkness = darkTarget = 1f;
            Dim(1f);
        }

        // Up onto his forelegs, wings flung wide, the hollow blazing: then after you.
        void Rouse()
        {
            mode = Mode.Afoot;
            strikes = snipsLeft = 0;
            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            BeBusy("Rouse", roused);
            Effects.Instance?.Leaves(transform.position + Vector3.up * 1.2f, Vector3.up * 2.5f, 50);
            Effects.Instance?.AlertFlare(ChestPoint);
            Sfx.Instance?.Attack(wakeSound, transform.position + Vector3.up * 2f, 1f, "father_moth_wake");
            CameraRig.Instance?.Shake(0.35f);
        }

        // Brought to the quarter mark afoot: he takes to the air for the last of it (phase 4).
        void TakeWing()
        {
            CurrentPhase = 4;
            mode = Mode.Rising;
            beatsDone = 0;
            Effects.Instance?.Leaves(transform.position + Vector3.up * 1.5f, Vector3.up * 3f, 60);
            Effects.Instance?.AlertFlare(ChestPoint);
            CameraRig.Instance?.Shake(0.3f);
            BeBusy("Rise", riseTime);
            PhaseBegun?.Invoke(this);
        }

        // (the lab, the tests) His fight begun at `phase`: at its health, in its place (afoot for phase 3).
        public void BeginAt(int phase)
        {
            phase = Mathf.Clamp(phase, 1, 4);
            Reform();
            Woken = true;
            passive = false;
            CurrentPhase = phase;
            SetHealth(phase == 1 ? maxHealth : maxHealth * PhaseMarks[phase - 2]);
            if (phase == 3)
            {
                PlaceAt(new Vector3(hover.x, floorY, hover.z));
                Rouse();
            }
            else
            {
                PlaceAt(new Vector3(hover.x, floorY + hoverHeight, hover.z));
                mode = Mode.Rising;
                BeBusy("Rise", riseTime);
            }
            Woke?.Invoke(this);
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

        void Hover(float dt)
        {
            velocity = Vector3.MoveTowards(velocity, Vector3.zero, 14f * dt);
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
            Hollow(); // (after the clips: the hollow's own swell)
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
            StartCoroutine(NightReturns());
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
            strikes = snipsLeft = 0;
            Swallowed = 0;
            CurrentPhase = 1;
            StopAllCoroutines();
            nextLamp = 0;
            dropAt = -1f;
            for (int i = 0; i < hung.Length; i++)
            {
                if (hung[i].lamp == null)
                    continue;
                hung[i].lamp.SetPositionAndRotation(hung[i].position, hung[i].rotation);
                foreach (var r in hung[i].lamp.GetComponentsInChildren<Renderer>())
                    r.enabled = true;
                lamps[i].enabled = true;
                Intensity(lamps[i], hung[i].intensity);
                fallSpeed[i] = 0f;
            }
            LampsLeft = hung.Length;
            darkness = darkTarget = 1f;
            Dim(1f);
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
            if (moon != null)
                Dim(1f); // (never leave the night dark)
            if (focus != null)
                Destroy(focus.gameObject);
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
