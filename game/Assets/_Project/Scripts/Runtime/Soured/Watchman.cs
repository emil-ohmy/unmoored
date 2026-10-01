using System.Collections;
using UnityEngine;

namespace MyGame
{
    // The Watchman: the Orchard's miniboss. His name plate: Abner Wick, Who Merits Flame. The man who worked this
    // orchard and couldn't let go of it: a scarecrow in the field above it, among others like him (Scarecrow), still keeping watch over a harvest that
    // never came. He's straw: the oar goes through him. Only fire hurts him, and the fire that hurts him most is his
    // own, breathed out of his lantern and turned back at him by a perfect block (Fireball): each one burns a third of
    // him away. Fire the Apprentice brings (pitch, tar) burns him a little.
    // Round 1: of the six scarecrows, one turns on its stake to watch you: strike it and he tears free. Watched (the
    // Apprentice facing him), he stands stock still and breathes fire at you; unwatched, he lurches at you and sweeps
    // his reaping hook. Round 2: scorched, he flings himself back among them; the lanterns gutter out, and when they
    // come back they've all moved, and all of them turn to watch you: he's the one still smouldering. His fire comes
    // as a fan of three, or held back and loosed late. Round 3: he tears free for good, alight from head to foot, and
    // never stops coming; his fire comes three small balls at a time, which a perfect block only knocks aside. Each
    // knocked aside costs him breath: his lantern gutters down a step, and a note rings (rising: one, two, three).
    // Knock all three aside, one after another, and his lantern goes out a beat; then he draws the last great breath
    // he has, and it blazes: turned back, it ends him. Miss one and a sour note sounds, and his lantern flares full.
    public sealed class Watchman : Soured, IBoss
    {
        [Header("The Watchman")]
        public string title = "Abner Wick";
        public string epithet = "Who Merits Flame";
        public string farewell = "FLAME MERITED";
        public string farewellLine = "He kept the light for no one.";
        public AudioClip[] music;             // one track per phase (found; scorched once)
        [Range(0f, 180f)] public float watchHalfAngle = 60f;
        public float watchRange = 18f;
        public float leash = 20f;             // further than this from him, the fight is off

        [Header("Among the scarecrows")]
        public Scarecrow[] field;             // the scarecrows he stands among
        public Transform pole;                // his stake: he goes back on one when he hides again
        public float watchReach = 14f;        // on his stake, he turns to watch the Apprentice this near
        public float turnToWatch = 40f;       // degrees a second
        public Light[] fieldLights;           // the field's lanterns (they gutter out when he hides)

        [Header("Found")]
        public float lurchSpeed = 6.5f, lurchTime = 0.22f, lurchPause = 0.32f;
        public float thrashSpeed = 3.6f;
        public Light lantern;                 // his lantern's light: dark until he's found
        public float lanternIntensity = 3.4f;

        [Header("His fire")]
        public SouredAttack breath = new SouredAttack();  // round 1: one, slow
        public SouredAttack fan = new SouredAttack();     // round 2: three abreast (the middle one at you)
        public SouredAttack feint = new SouredAttack();   // round 2: held, loosed late
        public SouredAttack flurry = new SouredAttack();  // round 3: three small, one after another (knocked aside, not back)
        public SouredAttack finale = new SouredAttack();  // round 3, all three knocked aside: one great one, to turn back
        public float fireCooldown = 2.4f;     // between one breath and the next
        public float fanSpread = 24f;
        public float fireDamage = 18f, smallFireDamage = 10f, smallFireSpeed = 9f;
        public Material fireMaterial;
        public AudioClip[] creakSound, revealSound, strawSound, gutterSound, scorchSound;
        public AudioClip[] chimeSound;        // round 3: one per small fireball knocked aside in a row, rising
        public AudioClip[] sourSound;         // round 3: the row broken

        public int Round { get; private set; }     // 0: not found yet; 1, 2, 3
        public bool Hidden { get; private set; } = true;
        public bool Watched { get; private set; }
        public bool Thrashing => Round == 3 && IsAlive;
        public int Knocked { get; private set; }   // round 3: of this flurry, how many knocked aside so far
        public bool FinaleDrawn => finaleReady;    // all three knocked aside: the great one's next
        public float Breath => breathLeft;         // round 3: his lantern's breath (1; 2/3, 1/3 as they're knocked aside; 0)
        public override bool Lockable => Targetable && !Hidden; // (on his stake he's one of them: nothing to lock on to)
        public bool Frozen => !Hidden && Round < 3 && Watched && IsAlive
            && (CurrentState == State.Chase || (CurrentAttack == primary && (CurrentState == State.Telegraph || CurrentState == State.Strike || CurrentState == State.Recover)));
        public static event System.Action<Watchman> Revealed, Scorched, Lost; // (tests)
        public static event System.Action<Watchman, Fireball> Breathed;

        public string Title => title;
        public string Epithet => epithet;
        public float[] PhaseMarks => new[] { 2f / 3f, 1f / 3f };
        public int Phase => Round >= 2 ? 2 : 1; // (two tracks: found; scorched)
        public AudioClip[] Music => music;
        public string Farewell => farewell;
        public string FarewellLine => farewellLine;

        Animation anim;
        Renderer glassRenderer;
        int glassSlot = -1;
        Material darkGlass, litGlass;
        (Vector3 position, Quaternion rotation)[] spots; // where the field stands: his place and theirs
        float burstClock, nextFire, nextCreak, emitDebt, loseAt = -1f;
        bool fightOn, finaleReady;
        int flurryReleased, flurryDone;       // of this flurry: how many breathed out, how many done with
        bool rowBroken;                       // of this flurry: one not knocked aside
        float breathLeft = 1f, flareUntil, outUntil;
        Light blaze;                          // alight (reeling into round 2; all through round 3)
        Coroutine gutter;
        Light moon;
        float moonIntensity;
        float[] fieldIntensity;
        Color sky, equator, ground;

        protected override float Pace => Frozen ? 0f : 1f;
        protected override string IdleClip => "Idle";
        protected override string ChaseClip(float distance) => Thrashing ? "Thrash" : Lurching ? "Lurch" : "Idle";
        bool Lurching => burstClock % (lurchTime + lurchPause) < lurchTime;
        Vector3 LanternPoint => lantern != null ? lantern.transform.position : transform.position + Vector3.up * 1.4f;

        protected override void Awake()
        {
            base.Awake();
            anim = GetComponentInChildren<Animation>();
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                int i = System.Array.FindIndex(r.sharedMaterials, m => m != null && m.name.StartsWith("WatchmanLanternGlass"));
                if (i < 0)
                    continue;
                glassRenderer = r;
                glassSlot = i;
                darkGlass = r.sharedMaterials[i];
                litGlass = new Material(darkGlass) { name = "WatchmanLanternGlass (lit)" };
                litGlass.SetColor("_EmissionColor", new Color(1f, 0.62f, 0.3f) * 1.4f);
            }
            var all = new System.Collections.Generic.List<(Vector3, Quaternion)> { (transform.position, transform.rotation) };
            if (field != null)
                foreach (var s in field)
                    all.Add((s.transform.position, s.transform.rotation));
            spots = all.ToArray();
            blaze = new GameObject("Blaze").AddComponent<Light>();
            blaze.transform.SetParent(transform, false);
            blaze.transform.localPosition = Vector3.up * 1.3f;
            blaze.type = LightType.Point;
            blaze.color = new Color(1f, 0.55f, 0.2f);
            blaze.range = 6f;
            blaze.shadows = LightShadows.None;
            blaze.enabled = false;
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional)
                    moon = l;
            moonIntensity = moon != null ? moon.intensity : 0f;
            fieldIntensity = System.Array.ConvertAll(fieldLights ?? new Light[0], l => l != null ? l.intensity : 0f);
            sky = RenderSettings.ambientSkyColor;
            equator = RenderSettings.ambientEquatorColor;
            ground = RenderSettings.ambientGroundColor;
        }

        protected override void Start()
        {
            base.Start();
            Reform(); // (never in the same place twice: not even the first time)
        }

        // ---- among the scarecrows

        // The oar goes through straw (a puff of it, no harm); fire burns. Unfound, any of it finds him out.
        protected override bool Absorbs(Hit hit, bool fire)
        {
            if (Hidden)
            {
                Reveal();
                return true;
            }
            if (fire)
                return false;
            Vector3 towards = hit.From - transform.position;
            towards.y = 0f;
            Effects.Instance?.Straw(transform.position + Vector3.up * 1.1f + towards.normalized * 0.3f, 14);
            Sfx.Instance?.Attack(strawSound, transform.position + Vector3.up, 0.8f, "straw_puff");
            return true;
        }

        public override HookResult OnHooked(Apprentice by)
        {
            if (Hidden)
            {
                Reveal();
                return HookResult.Shrugged;
            }
            return base.OnHooked(by);
        }

        // Found out: he tears free of his stake, his lantern flares, and the fight is on (or on again).
        void Reveal()
        {
            Hidden = false;
            passive = false;
            if (Round == 0)
                Round = 1;
            foreach (var s in field ?? new Scarecrow[0])
                if (s != null)
                    s.Follows = false;
            Effects.Instance?.Straw(transform.position + Vector3.up * 1.1f, 40);
            Sfx.Instance?.Attack(revealSound, transform.position + Vector3.up, 1f, "watchman_reveal");
            CameraRig.Instance?.Shake(0.2f);
            SetLantern(true);
            BeBusy("Reveal", 1.2f);
            nextFire = Time.time + 1.6f;
            Revealed?.Invoke(this);
        }

        // Turns `figure` on its stake towards the Apprentice (near enough), at most `degrees`; true if it turned.
        public static bool TurnToWatch(Transform figure, Apprentice a, float degrees, float reach = 14f)
        {
            if (a == null || !a.IsAlive)
                return false;
            Vector3 to = a.transform.position - figure.position;
            to.y = 0f;
            if (to.magnitude > reach || to.sqrMagnitude < 0.01f)
                return false;
            var look = Quaternion.LookRotation(to);
            if (Quaternion.Angle(figure.rotation, look) < 0.5f)
                return false;
            figure.rotation = Quaternion.RotateTowards(figure.rotation, look, degrees);
            return true;
        }

        // ---- found

        protected override Vector3 ChaseStep(Vector3 toTarget, float distance)
        {
            if (distance < attackRange * 0.7f)
                return Vector3.zero;
            if (Thrashing)
                return PathDirection(target.transform.position) * thrashSpeed;
            return Lurching ? PathDirection(target.transform.position) * lurchSpeed : Vector3.zero;
        }

        // Up close and unwatched (or alight, whatever you do): the hook. Watched (or alight, further off): his fire.
        protected override SouredAttack ChooseAttack(Vector3 toTarget, float distance)
        {
            if (Hidden)
                return null;
            if (distance <= attackRange && (!Watched || Thrashing))
                return primary;
            if (Time.time < nextFire || distance > breath.reach || !(Watched || (Thrashing && distance > attackRange)) || flurryDone < flurryReleased) // (not while his last are still in the air)
                return null;
            nextFire = Time.time + fireCooldown;
            if (Round < 3)
                return Round == 1 ? breath : Random.value < 0.5f ? fan : feint;
            if (finaleReady)
            {
                finaleReady = false;
                return finale;
            }
            Knocked = flurryReleased = flurryDone = 0;
            rowBroken = false;
            breathLeft = 1f;
            return flurry;
        }

        protected override void Shoot(SouredAttack attack, Vector3 direction, int index)
        {
            // From his lantern, wherever his arm has it, straight at their chest: never over their head, nor short.
            Vector3 from = LanternPoint;
            Vector3 aim = direction;
            if (target != null)
                aim = (target.transform.position + Vector3.up * 0.7f - from).normalized;
            if (attack == fan)
                for (int k = -1; k <= 1; k++)
                    Breathe(from, Quaternion.Euler(0f, k * fanSpread, 0f) * aim, attack.projectileSpeed, 0.32f, fireDamage, true);
            else if (attack == flurry)
            {
                flurryReleased++;
                Breathe(from, aim, smallFireSpeed, 0.22f, smallFireDamage, true).glances = true; // (knocked aside, not back)
            }
            else
                Breathe(from, aim, attack.projectileSpeed, attack == finale ? 0.55f : 0.34f, fireDamage, true);
        }

        Fireball Breathe(Vector3 from, Vector3 direction, float speed, float size, float damage, bool parryable)
        {
            var ball = Fireball.Fire(from, direction, speed, size, target, this, fireMaterial);
            ball.damage = damage;
            ball.parryable = parryable;
            if (CurrentAttack == flurry)
                ball.Done = OnSmallDone;
            Breathed?.Invoke(this, ball);
            return ball;
        }

        // Round 3: knock all three of a flurry aside, one after another, and he draws his last great breath.
        void OnSmallDone(Fireball ball, Defence defence)
        {
            if (defence == Defence.Parried && !rowBroken)
            {
                Knocked++;
                breathLeft = 1f - Knocked / (float)flurry.volley; // (his lantern gutters down a step)
                var chime = chimeSound != null && chimeSound.Length > 0 ? chimeSound[Mathf.Clamp(Knocked - 1, 0, chimeSound.Length - 1)] : null;
                if (chime != null)
                    Sfx.Instance?.Attack(new[] { chime }, LanternPoint, 0.9f, "watchman_chime");
                if (Knocked >= flurry.volley)
                    outUntil = Time.time + 0.5f; // (out, a beat: then his last breath)
            }
            else if (!rowBroken && defence != Defence.Parried)
            {
                rowBroken = true;
                breathLeft = 1f;
                flareUntil = Time.time + 0.6f; // (it flares back full)
                Sfx.Instance?.Attack(sourSound, LanternPoint, 0.9f, "watchman_sour");
            }
            flurryDone++;
            if (flurryDone == flurry.volley && IsAlive) // (cut short, it never comes to anything)
            {
                finaleReady = Knocked >= flurry.volley;
                nextFire = Time.time + (finaleReady ? 0.5f : fireCooldown);
            }
        }

        // His own fire, turned back at him: a third of him burns away.
        public void Scorch(Vector3 from)
        {
            if (!IsAlive || Hidden)
                return;
            Vector3 at = transform.position + Vector3.up * 1.1f;
            for (int i = 0; i < 70; i++)
                Effects.Instance?.Flame(at + Random.insideUnitSphere * 0.6f, Random.insideUnitSphere * 2f + Vector3.up * 2.5f, Random.Range(0.4f, 0.8f), Random.Range(0.2f, 0.4f));
            Effects.Instance?.Straw(at, 30);
            Sfx.Instance?.Attack(scorchSound, at, 1f, "watchman_scorch");
            CameraRig.Instance?.Shake(0.3f);
            HitStop.Trigger(0.15f);
            Scorched?.Invoke(this);
            float burn = maxHealth / 3f;
            if (Health - burn <= 0.5f)
            {
                Slay(from);
                return;
            }
            if (Round == 3)
            {
                Slay(from); // (his last breath, turned back: he burns down to his stake)
                return;
            }
            LoseHealth(burn);
        }

        // Scorched (or burnt) past a mark: round 2 (he hides again) or round 3 (he tears free for good, alight).
        void NextRound()
        {
            if (Round == 1 && Health > maxHealth / 3f + 0.5f)
            {
                Round = 2;
                BeBusy("Stagger", 2.0f); // (reeling, alight, till the lanterns gutter)
                loseAt = Time.time + 0.9f;
            }
            else
            {
                Round = 3;
                loseAt = -1f;
                BeBusy("Reveal", 1.2f); // (torn free for good: and alight)
                nextFire = Time.time + 2f;
            }
        }

        // Back among them: the lanterns gutter out, and when they come back they've all moved, and they all watch you.
        void LoseHimself()
        {
            loseAt = -1f;
            if (gutter != null)
                StopCoroutine(gutter);
            gutter = StartCoroutine(GutterOut(() =>
            {
                var living = new System.Collections.Generic.List<Transform> { transform };
                foreach (var s in field)
                    if (s != null && s.IsAlive)
                        living.Add(s.transform);
                var places = living.ConvertAll(t => (t.position, t.rotation));
                Shuffle(places);
                for (int i = 0; i < living.Count; i++)
                {
                    if (living[i] == transform)
                    {
                        PlaceAt(places[i].position);
                        transform.rotation = places[i].rotation;
                    }
                    else
                        living[i].GetComponent<Scarecrow>().Stand(places[i].position, places[i].rotation);
                }
                Douse(); // (the fire that drove him back goes out in the dark)
                Hide();
                BeBusy("Idle", float.MaxValue); // (on his stake again: still)
                foreach (var s in field)
                    if (s != null && s.IsAlive)
                        s.Follows = true;
                Lost?.Invoke(this);
            }));
        }

        public float darkFade = 0.6f, darkHold = 2.4f, darkReturn = 1.2f; // the lanterns guttering out, the dark, the light back

        IEnumerator GutterOut(System.Action atDarkest)
        {
            Sfx.Instance?.Attack(creakSound, transform.position + Vector3.up, 1f, "watchman_creak", 0.7f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / darkFade)
            {
                Dim(1f - t * 0.95f);
                yield return null;
            }
            Dim(0.05f);
            atDarkest();
            // In the dark: stakes creaking and straw rustling here and there across the field, as they move.
            for (float t = 0f; t < darkHold; t += Time.deltaTime)
            {
                if (Random.value < 3f * Time.deltaTime && field != null && field.Length > 0)
                {
                    var s = field[Random.Range(0, field.Length)];
                    Vector3 at = (s != null ? s.transform.position : transform.position) + Random.insideUnitSphere * 2f + Vector3.up;
                    Sfx.Instance?.Attack(Random.value < 0.5f ? creakSound : strawSound, at, 0.5f, "watchman_creak");
                }
                yield return null;
            }
            for (float t = 0f; t < 1f; t += Time.deltaTime / darkReturn)
            {
                Dim(0.05f + t * 0.95f);
                yield return null;
            }
            Dim(1f);
            gutter = null;
        }

        // The night's light, all of it, at `k` of itself (the moon, the ambient, the field's lanterns).
        void Dim(float k)
        {
            if (moon != null)
                moon.intensity = moonIntensity * k;
            var own = target != null ? target.GetComponent<BowLantern>() : null;
            if (own != null)
                own.Dimmed = Mathf.Lerp(0.15f, 1f, k); // (even theirs burns low: a small pool round their feet)
            for (int i = 0; fieldLights != null && i < fieldLights.Length; i++)
            {
                if (fieldLights[i] == null)
                    continue;
                var flicker = fieldLights[i].GetComponent<LightFlicker>();
                if (flicker != null)
                    flicker.BaseIntensity = fieldIntensity[i] * k; // (it flickers about that)
                else
                    fieldLights[i].intensity = fieldIntensity[i] * k;
            }
            RenderSettings.ambientSkyColor = sky * k;
            RenderSettings.ambientEquatorColor = equator * k;
            RenderSettings.ambientGroundColor = ground * k;
        }

        protected override void Tick(float dt)
        {
            Watched = SeenBy(target);
            if (anim != null)
                anim.enabled = !Frozen; // (stock still: the pose he was in)
            if (!IsAlive)
                return;
            if (Hidden)
            {
                // On his stake: he turns to watch you (and in round 2, so do they all; and he smoulders).
                if (TurnToWatch(transform, target, turnToWatch * Time.deltaTime, watchReach) && Time.time >= nextCreak)
                {
                    nextCreak = Time.time + Random.Range(1.8f, 3.2f);
                    Sfx.Instance?.Attack(creakSound, transform.position + Vector3.up, 0.45f, "watchman_creak");
                }
                // Smouldering: a thin thread of smoke off his shoulders and now and then an ember (no glow: in the
                // dark field you see it only if you're near, looking).
                bool smoulder = Round == 2;
                blaze.enabled = false;
                if (smoulder && Random.value < 6f * Time.deltaTime)
                    Effects.Instance?.Smoke(transform.position + new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(1.4f, 1.6f), Random.Range(-0.2f, 0.2f)));
                if (smoulder && Random.value < 0.7f * Time.deltaTime)
                    Effects.Instance?.Wisp(transform.position + new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(0.8f, 1.5f), Random.Range(-0.25f, 0.25f)),
                        new Color(1f, 0.45f, 0.12f) * 0.8f, Vector3.up * Random.Range(0.4f, 0.8f), Random.Range(0.6f, 1.0f));
                return;
            }
            burstClock += dt;
            if ((Round == 1 && Health <= 2f * maxHealth / 3f + 0.01f) || (Round == 2 && Health <= maxHealth / 3f + 0.01f))
                NextRound();
            if (loseAt > 0f && Time.time >= loseAt)
                LoseHimself();
            bool alight = Thrashing || (Round == 2 && loseAt > 0f);
            if (alight)
            {
                emitDebt += Time.deltaTime * (Thrashing ? 80f : 30f);
                for (; emitDebt >= 1f; emitDebt -= 1f)
                    Effects.Instance?.Flame(transform.position + new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(0.3f, 2.0f), Random.Range(-0.4f, 0.4f)),
                        Vector3.up * Random.Range(1.4f, 2.6f), Random.Range(0.35f, 0.65f), Random.Range(0.18f, 0.34f));
                blaze.range = 6f;
                blaze.intensity = 1.8f + 0.7f * Mathf.PerlinNoise(Time.time * 9f, 0.2f);
            }
            blaze.enabled = alight;
            // The lantern: steady while he's watched, guttering when he isn't; drawing breath, its flame roars up.
            bool drawing = CurrentState == State.Telegraph && CurrentAttack != null && CurrentAttack.ranged;
            if (lantern != null)
            {
                float level = drawing ? 1f + (CurrentAttack == finale ? 3f : 1.2f) * Mathf.Clamp01(StateTime / CurrentAttack.telegraph)
                    : Frozen ? 0.8f : 0.55f + 0.45f * Mathf.PerlinNoise(Time.time * 14f, 0.7f);
                if (Round == 3 && CurrentAttack != finale)
                {
                    level *= Mathf.Lerp(0.12f, 1f, breathLeft); // (his breath, spent)
                    if (Time.time < flareUntil)
                        level *= 1f + 1.5f * (flareUntil - Time.time) / 0.6f;
                }
                bool spent = Time.time < outUntil;
                lantern.enabled = !spent;
                lantern.intensity = lanternIntensity * level;
                SetGlass(!spent);
            }
            if (drawing && Random.value < (CurrentAttack == finale ? 90f : 30f) * Time.deltaTime)
                Effects.Instance?.Flame(LanternPoint + Random.insideUnitSphere * 0.08f, Vector3.up * Random.Range(0.6f, 1.4f), Random.Range(0.2f, 0.35f),
                    Mathf.Lerp(0.08f, 0.2f, StateTime / CurrentAttack.telegraph));
        }

        // Whether the Apprentice is facing him (near enough to see him).
        bool SeenBy(Apprentice a)
        {
            if (a == null || !a.IsAlive)
                return false;
            Vector3 to = transform.position - a.transform.position;
            to.y = 0f;
            return to.magnitude < watchRange && Vector3.Angle(a.transform.forward, to) <= watchHalfAngle;
        }

        void LateUpdate()
        {
            // The fight: on from when he's first found (hidden again or not) while they're near.
            bool on = Round >= 1 && IsAlive && target != null && target.IsAlive && Vector3.Distance(target.transform.position, transform.position) < leash;
            if (on)
                BossFight.Begin(this);
            else if (fightOn)
                BossFight.End(this);
            fightOn = on;
        }

        protected override void OnEntered(State state)
        {
            if (state != State.Dying)
                return;
            // Beaten: he burns down to nothing, and his lantern goes out.
            SetLantern(false);
            blaze.enabled = false;
            for (int i = 0; i < 90; i++)
                Effects.Instance?.Flame(transform.position + Vector3.up * 1.0f + Random.insideUnitSphere * 0.6f, Random.insideUnitSphere * 2f + Vector3.up * 2.5f, Random.Range(0.4f, 0.9f), Random.Range(0.2f, 0.4f));
            Effects.Instance?.Straw(transform.position + Vector3.up * 1.0f, 80);
            BossFight.Defeat(this);
            BossFight.End(this);
            fightOn = false;
        }

        void SetLantern(bool lit)
        {
            if (lantern != null)
            {
                lantern.enabled = lit;
                lantern.intensity = lanternIntensity;
            }
            SetGlass(lit);
        }

        void SetGlass(bool lit)
        {
            if (glassRenderer == null || glassRenderer.sharedMaterials[glassSlot] == (lit ? litGlass : darkGlass))
                return;
            var materials = glassRenderer.sharedMaterials;
            materials[glassSlot] = lit ? litGlass : darkGlass;
            glassRenderer.sharedMaterials = materials;
        }

        // One of them again, on his stake, his lantern dark.
        void Hide()
        {
            Hidden = true;
            passive = true;
            SetLantern(false);
            Scarecrow.StandPole(pole, transform);
        }

        void Douse()
        {
            var burning = GetComponent<Burning>();
            if (burning != null)
                burning.Douse();
        }

        static void Shuffle<T>(System.Collections.Generic.IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        public override void Reform()
        {
            // A new place among them: shuffled, each to a spot (he to one, the scarecrows to the rest), all whole.
            var order = new System.Collections.Generic.List<int>();
            for (int i = 0; i < spots.Length; i++)
                order.Add(i);
            Shuffle(order);
            SetSpawn(spots[order[0]].position, spots[order[0]].rotation);
            for (int i = 0; field != null && i < field.Length; i++)
            {
                field[i].SetSpawn(spots[order[i + 1]].position, spots[order[i + 1]].rotation);
                field[i].Reform();
            }
            base.Reform();
            if (gutter != null)
                StopCoroutine(gutter);
            gutter = null;
            Dim(1f);
            Round = 0;
            loseAt = -1f;
            finaleReady = false;
            Knocked = flurryReleased = flurryDone = 0;
            breathLeft = 1f;
            outUntil = flareUntil = 0f;
            blaze.enabled = false;
            Douse();
            Hide();
            BossFight.End(this);
            fightOn = false;
        }

        void OnDestroy()
        {
            if (moon != null)
                Dim(1f); // (never leave the night dark)
        }
    }
}
