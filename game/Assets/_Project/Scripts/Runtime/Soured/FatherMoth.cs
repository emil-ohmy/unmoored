using UnityEngine;

namespace MyGame
{
    // Father Moth, Who Gathered Autumn: the Gardener, the Orchard's Boss (docs/brief.md). The man who tended the orchard
    // and its glasshouse, grown into the greatest of the Moths: wings like dead leaves, six metres across, the stooped
    // gardener rising from them, his forearms grown into forelegs ending in pruning shears, a caged lantern in his ribs.
    // He hangs folded on the dead orange tree, like a leaf; come into the Glasshouse and he wakes.
    // Phase 1: he circles the lamps, out of the oar's reach. His wingbeats send gusts of leaves that shove you; over a
    // channel, he beats the water into a wave that rolls across the floor and knocks you down (dodge through it). He
    // dives: a stall, wings flung wide, the lantern in his chest flaring (the tell); a perfect block brings him down,
    // sprawled on the floor, open. Now and then he lands by a bed to tend it, and his shears sweep whoever comes near
    // (the oar reaches him there). He shakes Moths from his wings. Phase 2 (at half): he tears the lamps down one by
    // one, and in the dark (the moon, and your bow lantern) he hunts your light: dive after dive, each a chance to parry.
    // Fire burns him badly (it doesn't lure him). The Hook drags him down out of the air, now and then.
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
        public Vector3[] bedSpots;            // where he lands to tend the beds (on the floor beside each)
        public Vector3[] bedFacing;           // which way he faces there (into the bed)
        public float[] channelX;              // the channels' middles
        public float channelFrom, channelTo;  // their ends (z)
        public MothFlock flock;               // the Moths he shakes from his wings

        [Header("On the wing")]
        public float flySpeed = 7f, flyAccel = 14f;
        public float circleRadius = 2.6f, circleHeight = 3.2f, circleTurn = 0.6f; // round a lamp (not too high: seen from
        public float huntRadius = 6.5f, huntHeight = 3.5f, huntTurn = 0.8f;       // above, height reads as further away)
        public float reachedAbove = 1.9f;     // higher than this above the floor, the oar passes under him

        [Header("His attacks")]
        public SouredAttack dive = new SouredAttack();   // primary: the shears (on the ground)
        public SouredAttack gust = new SouredAttack();
        public SouredAttack wave = new SouredAttack();
        public SouredAttack shake = new SouredAttack();
        public float diveSpeed = 15f, diveContact = 1.4f, diveDamage = 22f, diveKnock = 8f;
        public float diveEvery = 5.5f, huntDiveEvery = 3.0f;
        public float gustEvery = 7f, gustSpeed = 12f, gustWidth = 6f, gustRange = 16f, gustPush = 11f;
        public float waveEvery = 13f, waveSpeed = 7f, waveDamage = 14f, waveKnock = 9f;
        public float tendEvery = 16f, tendFor = 6f, tendTakes = 0.12f; // tending: until this share of him is lost
        public float shakeEvery = 22f;
        public float hookDownEvery = 10f, hookDown = 2.2f;
        public float downedTime = 3.2f;       // a perfect block on his dive: down this long

        [Header("Look and sound")]
        public Light chest;                   // the lantern in his ribs
        public float chestIntensity = 1.4f;
        public float cameraLean = 0.5f, cameraZoom = 1.45f, sleepingLean = 0.45f, sleepingZoom = 1.15f; // his fight: the camera leans to him, and takes in more of the floor
        public AudioClip[] wingSound, wakeSound, stallSound, diveSound, gustSound, waveSound, shakeSound, landSound, downedSound, tearSound, shatterSound, deathSound;

        public enum Intent { Circle, ToChannel, ToBed, Tearing }

        public bool Woken { get; private set; }
        public int CurrentPhase { get; private set; } = 1;
        public bool Grounded { get; private set; }
        public bool Tending => Grounded && tending;
        public Intent Doing => intent;
        public int LampsLeft { get; private set; }
        public bool Downed => Grounded && (CurrentState == State.Stunned || CurrentState == State.Staggered);
        public bool Diving => CurrentState == State.Strike && CurrentAttack == dive;
        public float Height => transform.position.y - floorY;
        public static event System.Action<FatherMoth> Woke, Dived, Landed, Waved, Gusted, PhaseTwo; // (tests)

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
        protected override string ChaseClip(float distance) => Grounded ? (tending ? "Tend" : "Land") : "Fly";
        protected override Vector3 ChaseStep(Vector3 toTarget, float distance) => Vector3.zero; // (he flies: Tick)
        protected override bool LosesTarget(float distance) => !InHouse(target.transform.position);

        Vector3 velocity, diveDir, perch;
        Quaternion perchRotation;
        Intent intent;
        int channel = -1, bed = -1, nextLamp;
        int lampCircled;
        float nextDive, nextGust, nextWave, nextTend, nextShake, nextHookDown, nextAttack, nextWing, nextLampSwitch;
        float tendUntil, tendHealth, dropAt = -1f, orbit;
        bool tending, rising, landing, struckThisDive, fightOn, pendingTwo;
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
            nextDive = now + 6f;
            nextGust = now + 3.5f;
            nextWave = now + 11f;
            nextShake = now + 8f;
            nextTend = now + 18f;
            nextAttack = now + 2.5f;
            Woke?.Invoke(this);
        }

        // ---- choosing

        protected override SouredAttack ChooseAttack(Vector3 toTarget, float distance)
        {
            if (!Woken || rising || landing || intent == Intent.Tearing || pendingTwo)
                return null;
            if (Grounded)
                return tending && distance <= primary.reach && Vector3.Angle(transform.forward, toTarget) < 80f ? primary : null;
            float now = Time.time;
            if (now < nextAttack)
                return null;
            if (intent == Intent.ToChannel)
            {
                if (channel < 0 || Flat(transform.position - ChannelPoint()).magnitude > 1.2f)
                    return null;
                nextWave = now + waveEvery;
                return Begun(wave, 1.6f);
            }
            if (intent != Intent.Circle)
                return null;
            if (CurrentPhase == 1 && flock != null && now >= nextShake && flock.Alive == 0)
            {
                nextShake = now + shakeEvery;
                return Begun(shake, 1.2f);
            }
            // (the water and the beds first, when their time has come: else his dives and gusts would always be ready first)
            if (CurrentPhase == 1 && now >= nextWave && channelX != null && channelX.Length > 0)
            {
                intent = Intent.ToChannel; // (it comes when he's over the water)
                channel = FarChannel();
                return null;
            }
            if (CurrentPhase == 1 && now >= nextTend && bedSpots != null && bedSpots.Length > 0)
            {
                intent = Intent.ToBed;
                bed = Random.Range(0, bedSpots.Length);
                nextTend = now + tendEvery;
                return null;
            }
            if (now >= nextDive && distance > 2f && distance < 18f)
            {
                nextDive = now + (CurrentPhase == 1 ? diveEvery * Random.Range(0.8f, 1.2f) : huntDiveEvery * Random.Range(0.85f, 1.2f));
                if (CurrentPhase == 2 && HealthFraction < 0.25f && Random.value < 0.5f)
                    nextDive = now + 1.4f; // (near the end: one after another)
                dive.telegraph = CurrentPhase == 1 ? 0.85f : HealthFraction < 0.25f ? 0.62f : 0.72f;
                return Begun(dive, 1.0f);
            }
            if (now >= nextGust && distance < gustRange - 2f)
            {
                nextGust = now + gustEvery * Random.Range(0.8f, 1.3f) * (CurrentPhase == 2 ? 1.4f : 1f);
                return Begun(gust, 1.4f);
            }
            return null;
        }

        SouredAttack Begun(SouredAttack attack, float after)
        {
            nextAttack = Time.time + attack.telegraph + attack.strike + attack.recover + after;
            return attack;
        }

        // The channel across the floor from you (his wave comes at you across it).
        int FarChannel()
        {
            int best = 0;
            float far = -1f;
            for (int i = 0; i < channelX.Length; i++)
            {
                float d = target != null ? Mathf.Abs(target.transform.position.x - channelX[i]) : i;
                if (d > far)
                {
                    far = d;
                    best = i;
                }
            }
            return best;
        }

        Vector3 ChannelPoint()
        {
            float z = target != null ? Mathf.Clamp(target.transform.position.z, channelFrom + 1.5f, channelTo - 1.5f) : (channelFrom + channelTo) / 2f;
            return new Vector3(channelX[channel], floorY + 3.0f, z);
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
                    Hold(new Vector3(transform.position.x, floorY, transform.position.z));
                    break;
                case State.Busy when rising:
                    Steer(new Vector3(transform.position.x, floorY + circleHeight, transform.position.z), dt, 0.6f);
                    break;
                case State.Busy when landing:
                    Steer(bed >= 0 ? bedSpots[bed] : new Vector3(transform.position.x, floorY, transform.position.z), dt, 1.2f);
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
                    if (Grounded)
                        Tend();
                    else
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

        // What follows a state's end: up off the floor after being downed; on the floor at the bed after landing.
        void Transitions()
        {
            var now = CurrentState;
            if (now == was)
                return;
            if ((was == State.Stunned || was == State.Staggered) && Grounded && IsAlive)
            {
                forced = null;
                if (now != State.Stunned && now != State.Staggered)
                    LiftOff();
            }
            if (was == State.Busy && landing)
            {
                landing = false;
                Grounded = true;
                tending = true;
                tendUntil = Time.time + tendFor;
                tendHealth = Health;
                Landed?.Invoke(this);
            }
            if (was == State.Busy && rising)
                rising = false;
            if (was == State.Busy && intent == Intent.Tearing && now == State.Chase && dropAt < 0f)
                nextLamp++;
            was = now;
        }

        void LiftOff()
        {
            Grounded = false;
            tending = false;
            intent = pendingTwo || CurrentPhase == 2 ? intent : Intent.Circle;
            BeBusy("Rise", 0.8f);
            rising = true;
            nextAttack = Mathf.Max(nextAttack, Time.time + 1.2f);
        }

        // On the wing: round a lamp (phase 1), round you (phase 2), to a channel, to a bed, to a lamp to tear down.
        void Fly(float dt)
        {
            Vector3 goal;
            switch (intent)
            {
                case Intent.ToChannel:
                    goal = ChannelPoint();
                    break;
                case Intent.ToBed:
                    goal = bedSpots[bed] + Vector3.up * 2.4f;
                    if (Flat(transform.position - bedSpots[bed]).magnitude < 0.8f && CurrentState == State.Chase)
                    {
                        landing = true;
                        intent = Intent.Circle;
                        BeBusy("Land", 1.0f);
                        Sfx.Instance?.Attack(landSound, bedSpots[bed], 0.9f, "father_moth_land");
                        return;
                    }
                    break;
                case Intent.Tearing:
                    if (nextLamp >= hung.Length)
                    {
                        intent = Intent.Circle;
                        nextAttack = Time.time + 1.5f;
                        goal = Orbit();
                        break;
                    }
                    goal = hung[nextLamp].lamp != null ? lamps[nextLamp].transform.position + Vector3.down * 0.9f : transform.position;
                    if ((transform.position - goal).magnitude < 0.8f && CurrentState == State.Chase)
                    {
                        BeBusy("Tear", 1.3f);
                        dropAt = Time.time + 0.8f;
                        Sfx.Instance?.Attack(tearSound, goal, 1f, "father_moth_tear");
                        return;
                    }
                    break;
                default:
                    goal = Orbit();
                    break;
            }
            Steer(goal, dt, 1f);
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
                diveDir = (target.transform.position + Vector3.up * 0.9f - transform.position - Vector3.up * 1.5f).normalized;
                struckThisDive = false;
                Sfx.Instance?.Attack(diveSound, transform.position, 1f, "father_moth_dive");
                Dived?.Invoke(this);
            }
            if (state == State.Strike && CurrentAttack == gust && target != null)
            {
                Vector3 from = new Vector3(transform.position.x, floorY, transform.position.z);
                Gust.Blow(from, target.transform.position - from, target, gustSpeed, gustWidth, gustRange, gustPush);
                Sfx.Instance?.Attack(gustSound, transform.position + Vector3.up * 2f, 1f, "father_moth_gust");
                Gusted?.Invoke(this);
            }
            if (state == State.Strike && CurrentAttack == wave && channel >= 0)
            {
                float x = channelX[channel], toward = Mathf.Sign(middle.x - x);
                ChannelWave.Roll(x, floorY, channelFrom, channelTo, toward, target, waveSpeed, reach.x * 2f + 1f, waveDamage, waveKnock);
                Sfx.Instance?.Attack(waveSound, new Vector3(x, floorY, transform.position.z), 1f, "father_moth_wave");
                CameraRig.Instance?.Shake(0.2f);
                intent = Intent.Circle;
                channel = -1;
                Waved?.Invoke(this);
            }
            if (state == State.Strike && CurrentAttack == shake && flock != null)
            {
                flock.Release(transform.position + Vector3.up * 1.6f);
                Sfx.Instance?.Attack(shakeSound, transform.position + Vector3.up * 2f, 1f, "father_moth_shake");
                Effects.Instance?.Leaves(transform.position + Vector3.up * 1.8f, Vector3.zero, 30);
            }
            if (state == State.Stunned)
                Down(downedTime);
            if (state == State.Staggered && !Grounded)
                Down(staggerDuration); // (struck low out of the air: down he comes)
            if (state == State.Dying)
                Died();
        }

        Vector3 ChestPoint => chest != null ? chest.transform.position : transform.position + Vector3.up * 2.5f;

        // Down out of the air and on through, striking if he passes you close (a perfect block brings him down).
        void Dive(float dt)
        {
            Vector3 next = transform.position + diveDir * diveSpeed * dt;
            if (next.y < floorY + 0.1f)
            {
                diveDir = new Vector3(diveDir.x, Mathf.Max(diveDir.y, 0.2f), diveDir.z).normalized; // (skimming the floor, pulling up)
                next.y = floorY + 0.1f;
            }
            Hold(next);
            velocity = diveDir * diveSpeed * 0.4f;
            if (struckThisDive || target == null || !target.IsAlive)
                return;
            // (his shears and chest pass through where you stand: near enough across, and low enough)
            if (Flat(target.transform.position - transform.position).magnitude > diveContact || transform.position.y - target.transform.position.y > 1.2f)
                return;
            struckThisDive = true;
            var defence = HitTarget(diveDamage, Flat(diveDir).normalized * diveKnock);
            if (defence == Defence.Parried)
            {
                // (turned on the oar: down he goes, in front of you, the way he came)
                Vector3 back = -Flat(diveDir).normalized;
                Vector3 at = target.transform.position + (back.sqrMagnitude > 0.01f ? back : -target.transform.forward) * (radius + 1.6f);
                Hold(new Vector3(at.x, floorY, at.z));
                Stun();
            }
            else if (defence != Defence.Dodged)
                diveDir = new Vector3(-diveDir.x * 0.3f, 0.9f, -diveDir.z * 0.3f).normalized; // (glancing off, back up)
        }

        // On the floor, sprawled (a perfect block on his dive; the Hook): open, and every blow a riposte while stunned.
        void Down(float time)
        {
            Grounded = true;
            tending = false;
            landing = rising = false;
            velocity = Vector3.zero;
            forced = "Downed";
            Hold(new Vector3(transform.position.x, floorY, transform.position.z));
            Effects.Instance?.Rubble(new Vector3(transform.position.x, floorY, transform.position.z), 1.0f);
            Effects.Instance?.Leaves(transform.position + Vector3.up, Vector3.zero, 30);
            Sfx.Instance?.Attack(downedSound, transform.position + Vector3.up, 1f, "father_moth_downed");
            CameraRig.Instance?.Shake(0.35f);
        }

        // Caught on the Hook on the wing: dragged down to the floor (not too often: his strength comes back to him).
        public override HookResult OnHooked(Apprentice by)
        {
            if (!IsAlive || !Woken || Grounded || intent == Intent.Tearing || CurrentState == State.Busy || Time.time < nextHookDown)
                return HookResult.Shrugged;
            nextHookDown = Time.time + hookDownEvery;
            Vector3 away = Flat(transform.position - by.transform.position);
            Vector3 at = by.transform.position + (away.sqrMagnitude > 0.01f ? away.normalized : by.transform.forward) * 2.6f;
            Hold(new Vector3(at.x, floorY, at.z));
            StaggerNow(hookDown);
            Down(hookDown);
            return HookResult.Ripped;
        }

        // ---- tending the beds

        void Tend()
        {
            if (!tending)
                return;
            Hold(new Vector3(transform.position.x, floorY, transform.position.z));
            // Bent over the bed, until you come near.
            if (CurrentState == State.Chase && bed >= 0 && bedFacing != null && bed < bedFacing.Length && target != null
                && Flat(target.transform.position - transform.position).magnitude > primary.reach + 2f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(bedFacing[bed]), 540f * Time.deltaTime);
            if (CurrentState == State.Chase && (Time.time >= tendUntil || tendHealth - Health >= maxHealth * tendTakes || pendingTwo))
                LiftOff();
        }

        // ---- phase 2: the lamps torn down, the dark

        void BeginTearing()
        {
            pendingTwo = false;
            CurrentPhase = 2;
            intent = Intent.Tearing;
            nextLamp = 0;
            dropAt = -1f;
            if (flock != null)
                foreach (var m in flock.Moths)
                    if (m != null && m.IsAlive)
                        m.Slay(m.transform.position); // (shaken off: they go to the dark)
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
                bool telling = CurrentState == State.Telegraph && CurrentAttack == dive;
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

        // Beaten: he folds and falls; the lantern in his ribs goes out; his Moths go with him.
        void Died()
        {
            forced = null;
            Sfx.Instance?.Attack(deathSound, transform.position + Vector3.up * 2f, 1f, "father_moth_death");
            if (flock != null)
                foreach (var m in flock.Moths)
                    if (m != null && m.IsAlive)
                        m.Slay(m.transform.position);
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
            Grounded = tending = rising = landing = pendingTwo = false;
            intent = Intent.Circle;
            channel = bed = -1;
            nextLamp = lampCircled = 0;
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
            if (flock != null)
                flock.Withdraw();
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
