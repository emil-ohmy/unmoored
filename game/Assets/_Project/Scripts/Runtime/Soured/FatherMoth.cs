using UnityEngine;

namespace MyGame
{
    // Father Moth, Who Gathered Autumn: the Gardener, the Orchard's Boss (docs/brief.md). The man who tended the orchard
    // and its glasshouse, grown into the greatest of the Moths: wings like dead leaves, six metres across, the stooped
    // gardener rising from them, his forearms grown into forelegs ending in pruning shears, a caged lantern in his ribs.
    // He hangs folded on the dead orange tree, like a leaf; come into the Glasshouse and he wakes.
    // His dive: a stall, wings flung wide, the lantern in his ribs flaring (the tell); then down at you. Dodge it and he
    // lands where you stood, his shears snapping round him at once (back off, block, or parry them: a perfect block
    // knocks him down); then he's on the floor a moment, open, before he's up again. Turn the dive itself with a perfect
    // block and he's knocked down, stunned, every blow a riposte. The Hook catches him only in his stall: dragged down
    // out of it, the dive never comes. On the wing the oar passes under him.
    // Phase 2 (at half): he tears the lamps down one by one; in the dark (the moon, and your bow lantern) his dives come
    // in pairs, the second straight after the first with a shorter tell (he lands only after the second; parry the first
    // and the second never comes). Fire burns him badly (it doesn't lure him).
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
        public Vector2 reach = new Vector2(10f, 7.5f); // how far he flies from the middle (half its width, half its depth)
        public float wakeLine;                // past this (z), inside, he wakes
        public Transform[] lampModels;        // the hung lamps (torn down in phase 2)
        public Light[] lamps;                 // their lights

        [Header("On the wing")]
        public float flySpeed = 7f, flyAccel = 14f;
        public float circleRadius = 2.6f, circleHeight = 3.2f, circleTurn = 0.6f; // round a lamp (not too high: seen from
        public float huntRadius = 6.5f, huntHeight = 3.5f, huntTurn = 0.8f;       // above, height reads as further away)
        public float reachedAbove = 1.9f;     // higher than this above the floor, the oar passes under him

        [Header("His dive, and his shears when he lands")]
        public SouredAttack dive = new SouredAttack();
        public SouredAttack shears = new SouredAttack(); // snapped round him the moment he lands (its recovery: the window)
        public float diveSpeed = 15f, diveContact = 1.4f, diveDamage = 22f, diveKnock = 8f;
        public float diveEvery = 4.5f, huntDiveEvery = 3.2f;
        public float stallTime = 0.85f, huntStallTime = 0.75f, secondStallTime = 0.45f; // his tell: phase 1; phase 2, the first of a pair, the second
        public float downedTime = 3.2f;       // a dive turned on a perfect block: down this long
        public float hookDown = 2.0f;         // dragged down out of his stall on the Hook

        [Header("Look and sound")]
        public Light chest;                   // the lantern in his ribs
        public float chestIntensity = 1.4f;
        public float cameraLean = 0.5f, cameraZoom = 1.45f, sleepingLean = 0.45f, sleepingZoom = 1.15f; // his fight: the camera leans to him, and takes in more of the floor
        public AudioClip[] wingSound, wakeSound, stallSound, diveSound, landSound, downedSound, glanceSound, tearSound, shatterSound, deathSound;

        public enum Intent { Circle, Tearing }

        public bool Woken { get; private set; }
        public int CurrentPhase { get; private set; } = 1;
        public bool Grounded { get; private set; }
        public bool Landed => Grounded && landed;    // a dive come to the floor: his shears, then a moment open
        public bool Shearing => CurrentAttack == shears && (CurrentState == State.Telegraph || CurrentState == State.Strike);
        public Intent Doing => intent;
        public int LampsLeft { get; private set; }
        public bool Downed => Grounded && (CurrentState == State.Stunned || CurrentState == State.Staggered);
        public bool Diving => CurrentState == State.Strike && CurrentAttack == dive;
        public bool Stalling => CurrentState == State.Telegraph && CurrentAttack == dive;
        public bool SecondOfPair => secondOfPair;
        public float Height => transform.position.y - floorY;
        public static event System.Action<FatherMoth> Woke, Dived, LandedAfterDive, PhaseTwo; // (tests)

        public string Title => title;
        public string Epithet => epithet;
        public float[] PhaseMarks => new[] { 0.5f };
        public int Phase => CurrentPhase;
        public AudioClip[] Music => music;
        public string Farewell => farewell;
        public string FarewellLine => farewellLine;

        public override bool Flies => true;
        protected override bool Floats => true;
        protected override bool Surface() => true;
        public override bool OutOfReach => !Grounded && Height > reachedAbove;
        protected override bool OnItsLevel() => true; // (he comes at you from above)
        protected override string IdleClip => Woken ? "Fly" : "Perched";
        protected override string ChaseClip(float distance) => Grounded ? "Land" : "Fly";
        protected override Vector3 ChaseStep(Vector3 toTarget, float distance) => Vector3.zero; // (he flies: Tick)
        protected override bool LosesTarget(float distance) => !InHouse(target.transform.position);

        Vector3 velocity, diveDir, perch;
        Quaternion perchRotation;
        Intent intent;
        int nextLamp, lampCircled, pairLeft;
        float nextDive, nextAttack, nextWing, nextLampSwitch;
        float dropAt = -1f, orbit;
        bool rising, landed, sheared, struckThisDive, pullingUp, secondOfPair, fightOn, pendingTwo;
        State was;
        string forced;                         // a clip held over the base's (sprawled on the floor, not staggering)
        (Transform lamp, Vector3 position, Quaternion rotation, float intensity)[] hung;
        float[] fallSpeed;
        Transform focus;                       // his body (the camera leans to it: halfway, so you're both in view)
        int framing;                           // 0 not, 1 asleep on his tree, 2 his fight
        Light moon;
        float moonIntensity, darkness = 1f, darkTarget = 1f;
        Color sky, equator, ground;

        protected override void Awake()
        {
            base.Awake();
            perch = transform.position;
            perchRotation = transform.rotation;
            hung = new (Transform, Vector3, Quaternion, float)[lamps?.Length ?? 0];
            fallSpeed = new float[hung.Length];
            for (int i = 0; i < hung.Length; i++)
            {
                var root = lampModels != null && i < lampModels.Length ? lampModels[i] : null;
                var flicker = lamps[i] != null ? lamps[i].GetComponent<LightFlicker>() : null;
                float intensity = flicker != null ? flicker.BaseIntensity : lamps[i] != null ? lamps[i].intensity : 0f;
                hung[i] = (root, root != null ? root.position : Vector3.zero, root != null ? root.rotation : Quaternion.identity, intensity);
            }
            stunDuration = downedTime;
            focus = new GameObject("Father Moth (camera focus)").transform;
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

        bool InHouse(Vector3 p) => Mathf.Abs(p.x - middle.x) < reach.x + 2f && Mathf.Abs(p.z - middle.z) < reach.y + 2f && p.y > floorY - 1f && p.y < floorY + 8f;

        // ---- waking

        void Wake()
        {
            Woken = true;
            passive = false;
            BeBusy("Wake", 2.0f);
            rising = true;
            Sfx.Instance?.Attack(wakeSound, transform.position + Vector3.up * 2f, 1f, "father_moth_wake");
            Effects.Instance?.Leaves(transform.position + Vector3.up * 2f, Vector3.up * 2f, 40);
            CameraRig.Instance?.Shake(0.2f);
            float now = Time.time;
            nextDive = now + 3.5f;
            nextAttack = now + 3f;
            Woke?.Invoke(this);
        }

        // ---- choosing: his dive (and on the floor after it, his shears)

        protected override SouredAttack ChooseAttack(Vector3 toTarget, float distance)
        {
            if (Grounded && landed && !sheared)
            {
                sheared = true;
                return shears; // (the moment he's down: all round him)
            }
            if (!Woken || Grounded || rising || intent == Intent.Tearing || pendingTwo)
                return null;
            float now = Time.time;
            if (pairLeft > 0) // (phase 2: the second of a pair, straight after the first)
            {
                pairLeft--;
                secondOfPair = true;
                dive.telegraph = secondStallTime;
                return dive;
            }
            if (now < nextAttack || now < nextDive || distance > 18f)
                return null;
            nextDive = now + (CurrentPhase == 1 ? diveEvery : huntDiveEvery) * Random.Range(0.85f, 1.2f);
            secondOfPair = false;
            pairLeft = CurrentPhase == 2 ? 1 : 0;
            dive.telegraph = CurrentPhase == 1 ? stallTime : huntStallTime;
            return dive;
        }

        // ---- every frame

        protected override void Tick(float dt)
        {
            if (!IsAlive)
            {
                Fall(dt);
                Lights(dt);
                return;
            }
            Transitions();
            if (!Woken)
            {
                if (target != null && target.IsAlive && InHouse(target.transform.position) && target.transform.position.z > wakeLine)
                    Wake();
                Lights(dt);
                return;
            }
            if (CurrentPhase == 1 && HealthFraction <= 0.5f)
                pendingTwo = true;
            if (pendingTwo && !Grounded && (CurrentState == State.Chase || CurrentState == State.Recover))
                BeginTearing();

            switch (CurrentState)
            {
                case State.Strike when CurrentAttack == dive:
                    Dive(dt);
                    break;
                case State.Stunned:
                case State.Staggered:
                case State.Busy when Grounded:
                case State.Telegraph when Grounded:
                case State.Strike when Grounded:
                case State.Recover when Grounded:
                case State.Chase when Grounded:
                    Hold(new Vector3(transform.position.x, floorY, transform.position.z));
                    break;
                case State.Busy when rising:
                    Steer(new Vector3(transform.position.x, floorY + circleHeight, transform.position.z), dt, 0.6f);
                    break;
                case State.Busy when intent == Intent.Tearing:
                    if (dropAt > 0f && Time.time >= dropAt)
                        TearDown();
                    Hover(dt);
                    break;
                case State.Telegraph:
                    Hover(dt);
                    if (CurrentAttack == dive && Random.value < 30f * Time.deltaTime)
                        Effects.Instance?.Flame(ChestPoint + Random.insideUnitSphere * 0.1f, Vector3.up * 0.6f, 0.25f, 0.08f);
                    break;
                case State.Chase:
                case State.Recover:
                case State.Strike:
                    Fly(dt);
                    break;
            }
            // His wingbeats, heard when he's aloft.
            if (!Grounded && Time.time >= nextWing && CurrentState != State.Strike)
            {
                nextWing = Time.time + 0.9f;
                Sfx.Instance?.Attack(wingSound, transform.position + Vector3.up * 2f, 0.55f, "father_moth_wing");
            }
            if (forced != null && clips.Current != forced)
                clips.PlayOnce(forced, Mathf.Max(0.1f, (CurrentState == State.Stunned ? downedTime : hookDown) - StateTime));
            Lights(dt);
        }

        // What follows a state's end: up off the floor (downed, or landed after a dive).
        void Transitions()
        {
            var now = CurrentState;
            if (now == was)
                return;
            if ((was == State.Stunned || was == State.Staggered) && Grounded && IsAlive && now != State.Stunned && now != State.Staggered)
            {
                forced = null;
                LiftOff();
            }
            else if (was == State.Recover && Grounded && landed && sheared && IsAlive && now == State.Chase)
                LiftOff(); // (the shears swung, the moment open gone: up again)
            else if (was == State.Busy && rising)
                rising = false;
            if (was == State.Busy && intent == Intent.Tearing && now == State.Chase && dropAt < 0f)
                nextLamp++;
            was = now;
        }

        void LiftOff()
        {
            Grounded = landed = sheared = false;
            pairLeft = 0;
            BeBusy("Rise", 0.8f);
            rising = true;
            nextAttack = Mathf.Max(nextAttack, Time.time + 1.0f);
        }

        // On the wing: round a lamp (phase 1), round you (phase 2), to a lamp to tear down.
        void Fly(float dt)
        {
            Vector3 goal;
            if (intent == Intent.Tearing)
            {
                if (nextLamp >= hung.Length)
                {
                    intent = Intent.Circle;
                    nextAttack = Time.time + 1.5f;
                    goal = Orbit();
                }
                else
                {
                    goal = hung[nextLamp].lamp != null ? lamps[nextLamp].transform.position + Vector3.down * 0.9f : transform.position;
                    if ((transform.position - goal).magnitude < 0.8f && CurrentState == State.Chase)
                    {
                        BeBusy("Tear", 1.3f);
                        dropAt = Time.time + 0.8f;
                        Sfx.Instance?.Attack(tearSound, goal, 1f, "father_moth_tear");
                        return;
                    }
                }
            }
            else
                goal = Orbit();
            Steer(goal, dt, CurrentState == State.Recover && secondOfPair ? 0.4f : 1f);
        }

        // Where he'd be now on his circle: round the lamp he's at (another, now and then), or round you in the dark.
        Vector3 Orbit()
        {
            bool hunting = CurrentPhase == 2 || LampsLeft == 0;
            orbit += Time.deltaTime * (hunting ? huntTurn : circleTurn);
            Vector3 centre;
            float radius, height;
            if (hunting && target != null)
            {
                centre = target.transform.position;
                radius = huntRadius;
                height = huntHeight;
            }
            else
            {
                if (Time.time >= nextLampSwitch && hung.Length > 0)
                {
                    nextLampSwitch = Time.time + Random.Range(7f, 11f);
                    lampCircled = (lampCircled + 1 + Random.Range(0, Mathf.Max(1, hung.Length - 1))) % hung.Length;
                }
                centre = hung.Length > 0 && lamps[lampCircled] != null ? lamps[lampCircled].transform.position : middle;
                radius = circleRadius;
                height = circleHeight;
            }
            var p = centre + new Vector3(Mathf.Cos(orbit) * radius, 0f, Mathf.Sin(orbit) * radius);
            p.y = floorY + height;
            return p;
        }

        // Towards `goal`, easing in, never out of his glasshouse nor up through its vault.
        void Steer(Vector3 goal, float dt, float pace)
        {
            Vector3 to = goal - transform.position;
            Vector3 want = Vector3.ClampMagnitude(to * 2f, flySpeed * pace);
            velocity = Vector3.MoveTowards(velocity, want, flyAccel * dt);
            Hold(transform.position + velocity * dt);
        }

        void Hover(float dt)
        {
            velocity = Vector3.MoveTowards(velocity, Vector3.zero, flyAccel * dt);
            Hold(transform.position + velocity * dt + Vector3.up * Mathf.Sin(Time.time * 4f) * 0.15f * dt);
        }

        // (outside his bounds, as off his tree, he's drawn back in, not snapped)
        void Hold(Vector3 p)
        {
            float step = 6f * Time.deltaTime;
            p.x = Mathf.MoveTowards(p.x, Mathf.Clamp(p.x, middle.x - reach.x, middle.x + reach.x), step);
            float north = middle.z + reach.y - (intent == Intent.Tearing ? 0f : Mathf.Max(0f, p.y - floorY) * 0.85f); // (seen from above, the higher he is the further north he looks: never over the wall; but he goes to the lamps to tear them down)
            p.z = Mathf.MoveTowards(p.z, Mathf.Clamp(p.z, middle.z - reach.y, Mathf.Max(middle.z - reach.y, north)), step);
            p.y = Mathf.Clamp(p.y, floorY, floorY + 6f);
            PlaceAt(p);
        }

        // ---- his dive

        protected override void OnEntered(State state)
        {
            if (state == State.Telegraph && CurrentAttack == dive)
            {
                Effects.Instance?.AlertFlare(ChestPoint);
                Sfx.Instance?.Attack(stallSound, ChestPoint, 1f, "father_moth_stall");
            }
            if (state == State.Strike && CurrentAttack == dive && target != null)
            {
                diveDir = (target.transform.position - transform.position).normalized; // (to where you stand: he comes down there)
                struckThisDive = pullingUp = false;
                Sfx.Instance?.Attack(diveSound, transform.position, 1f, "father_moth_dive");
                Dived?.Invoke(this);
            }
            if (state == State.Strike && CurrentAttack == shears)
                Effects.Instance?.Leaves(transform.position + Vector3.up * 0.8f, Vector3.zero, 25);
            if (state == State.Stunned)
                Down();
            if (state == State.Staggered && !Grounded)
                Down(); // (struck low out of the air: down he comes)
            if (state == State.Dying)
                Died();
        }

        Vector3 ChestPoint => chest != null ? chest.transform.position : transform.position + Vector3.up * 2.5f;

        // Down at you and through where you stood, striking if he passes you close: then, on the floor, a moment (a
        // perfect block: knocked down). The first of a pair (phase 2) pulls up off the floor instead.
        void Dive(float dt)
        {
            if (Grounded)
                return;
            Vector3 next = transform.position + diveDir * diveSpeed * dt;
            bool floor = !pullingUp && next.y <= floorY + 0.1f;
            if (floor)
                next.y = floorY + 0.1f;
            if (pullingUp && next.y > floorY + 2.6f)
                next.y = floorY + 2.6f; // (up only so far: the next comes from there)
            Hold(next);
            velocity = diveDir * diveSpeed * 0.4f;
            if (!struckThisDive && target != null && target.IsAlive
                // (his shears and chest pass through where you stand: near enough across, and low enough)
                && Flat(target.transform.position - transform.position).magnitude <= diveContact && transform.position.y - target.transform.position.y <= 1.2f)
            {
                struckThisDive = true;
                var defence = HitTarget(diveDamage, Flat(diveDir).normalized * diveKnock);
                if (defence == Defence.Parried)
                {
                    // (turned on the oar: down he goes, in front of you, the way he came; a second dive never comes)
                    pairLeft = 0;
                    Vector3 back = -Flat(diveDir).normalized;
                    Vector3 at = target.transform.position + (back.sqrMagnitude > 0.01f ? back : -target.transform.forward) * (radius + 1.6f);
                    Hold(new Vector3(at.x, floorY, at.z));
                    Stun();
                    return;
                }
            }
            if (!floor)
                return;
            if (pairLeft > 0)
            {
                pullingUp = true; // (skimming the floor, and up for the next)
                diveDir = new Vector3(diveDir.x, 0f, diveDir.z).normalized * 0.6f + Vector3.up * 0.8f;
            }
            else
                Land();
        }

        // A dive come to the floor: his shears at once (ChooseAttack), then a moment open before he's up again.
        void Land()
        {
            Grounded = landed = true;
            sheared = false;
            velocity = Vector3.zero;
            Hold(new Vector3(transform.position.x, floorY, transform.position.z));
            BeBusy("Land", 0.05f); // (a beat, then the shears)
            Effects.Instance?.Leaves(transform.position + Vector3.up * 0.5f, Vector3.zero, 20);
            Sfx.Instance?.Attack(landSound, transform.position, 0.9f, "father_moth_land");
            CameraRig.Instance?.Shake(0.15f);
            LandedAfterDive?.Invoke(this);
        }

        // On the floor, sprawled (a perfect block on his dive; the Hook): open, and every blow a riposte while stunned.
        void Down()
        {
            Grounded = true;
            landed = rising = false;
            velocity = Vector3.zero;
            forced = "Downed";
            Hold(new Vector3(transform.position.x, floorY, transform.position.z));
            Effects.Instance?.Rubble(new Vector3(transform.position.x, floorY, transform.position.z), 1.0f);
            Effects.Instance?.Leaves(transform.position + Vector3.up, Vector3.zero, 30);
            Sfx.Instance?.Attack(downedSound, transform.position + Vector3.up, 1f, "father_moth_downed");
            CameraRig.Instance?.Shake(0.35f);
        }

        // The Hook: in his stall, it catches him and drags him down (the dive never comes); any other time it glances off.
        public override HookResult OnHooked(Apprentice by)
        {
            if (!IsAlive || !Woken || !Stalling)
            {
                Effects.Instance?.BlockSpark(ChestPoint, Flat(by.transform.position - transform.position).normalized);
                Sfx.Instance?.Attack(glanceSound, ChestPoint, 0.8f, "father_moth_glance");
                return HookResult.Shrugged;
            }
            pairLeft = 0;
            Vector3 away = Flat(transform.position - by.transform.position);
            Vector3 at = by.transform.position + (away.sqrMagnitude > 0.01f ? away.normalized : by.transform.forward) * 2.6f;
            Hold(new Vector3(at.x, floorY, at.z));
            StaggerNow(hookDown);
            Down();
            return HookResult.Ripped;
        }

        // ---- phase 2: the lamps torn down, the dark

        void BeginTearing()
        {
            pendingTwo = false;
            CurrentPhase = 2;
            intent = Intent.Tearing;
            nextLamp = 0;
            dropAt = -1f;
            PhaseTwo?.Invoke(this);
        }

        void TearDown()
        {
            dropAt = -1f;
            int i = nextLamp;
            if (i < 0 || i >= hung.Length)
                return;
            fallSpeed[i] = 0.01f; // (falling: Lights)
            Sfx.Instance?.Attack(tearSound, lamps[i].transform.position, 1f, "father_moth_tear");
            LampsLeft = Mathf.Max(0, LampsLeft - 1);
            darkTarget = Mathf.Lerp(0.3f, 1f, LampsLeft / (float)Mathf.Max(1, hung.Length));
        }

        // The lamps (falling, out), the dark (the moon and the ambient, at `darkness` of themselves), his chest lantern.
        void Lights(float dt)
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
                darkness = Mathf.MoveTowards(darkness, darkTarget, Time.deltaTime * 0.6f);
                Dim(darkness);
            }
            if (chest != null)
            {
                bool telling = Stalling;
                float k = telling ? 1f + 3.5f * Mathf.Clamp01(StateTime / Mathf.Max(0.01f, dive.telegraph))
                        : 0.8f + 0.2f * Mathf.PerlinNoise(Time.time * 5f, 0.4f);
                chest.enabled = IsAlive || StateTime < deathDuration * 0.5f;
                chest.intensity = chestIntensity * k * (IsAlive ? 1f : 1f - Mathf.Clamp01(StateTime / (deathDuration * 0.5f)));
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
                moon.intensity = moonIntensity * Mathf.Lerp(0.5f, 1f, k);
            RenderSettings.ambientSkyColor = sky * k;
            RenderSettings.ambientEquatorColor = equator * k;
            RenderSettings.ambientGroundColor = ground * k;
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

        // Beaten: he folds and falls; the lantern in his ribs goes out.
        void Died()
        {
            forced = null;
            Sfx.Instance?.Attack(deathSound, transform.position + Vector3.up * 2f, 1f, "father_moth_death");
            Effects.Instance?.Leaves(transform.position + Vector3.up * 2f, Vector3.zero, 80);
            BossFight.Defeat(this);
            BossFight.End(this);
            fightOn = false;
            StartCoroutine(NightReturns());
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
            CurrentPhase = 1;
            StopAllCoroutines();
            Grounded = rising = landed = sheared = pendingTwo = pullingUp = secondOfPair = false;
            intent = Intent.Circle;
            nextLamp = lampCircled = pairLeft = 0;
            dropAt = -1f;
            forced = null;
            velocity = Vector3.zero;
            was = CurrentState;
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
            BossFight.End(this);
            fightOn = false;
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
