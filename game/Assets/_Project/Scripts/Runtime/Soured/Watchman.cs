using UnityEngine;

namespace MyGame
{
    // The Watchman, of the Unreaped Rows: the Orchard's miniboss. The man who worked this orchard and couldn't let go of
    // it: a scarecrow in the top rows among others like him (Scarecrow), still keeping watch over a harvest that never
    // came. He moves only while he isn't watched: while the Apprentice doesn't face him (watchHalfAngle either side of
    // where they face).
    // Finding him: he creeps a little whenever he's behind the Apprentice, turning to them; strike the right scarecrow
    // and he tears free of his stake, his lantern flares, and the fight is on (strike a wrong one, and Moths).
    // Facing him down: watched, he stands stock still, mid-stride or mid-swing; unwatched, he lurches at them in jerky
    // bursts and sweeps his reaping hook. His lantern calls the Moths (a flock when he's found, a second at half; when
    // one's scattered, it comes back). Alight, he can't hold still: he thrashes at them, always moving, while the fire
    // eats him. Beaten, his lantern goes out, and its Moths fall with it.
    public sealed class Watchman : Soured, IBoss
    {
        [Header("The Watchman")]
        public string title = "The Watchman";
        public string epithet = "of the Unreaped Rows";
        public string farewell = "THE WATCH IS ENDED";
        public string farewellLine = "Nothing came.";
        public AudioClip[] music;             // (none: the Orchard's own plays on)
        [Range(0f, 180f)] public float watchHalfAngle = 60f;
        public float watchRange = 18f;
        public float leash = 20f;             // further than this from him, the fight is off

        [Header("Unfound")]
        public Scarecrow[] field;             // the scarecrows he stands among: he takes the place of one, each time he re-forms
        public Transform pole;                // his stake: it goes with him while he creeps, and stays where he tore free of it
        public float creepReach = 12f;        // he creeps only while the Apprentice is this near
        public float creepNearest = 2.5f;     // and never nearer to them than this
        public float creepStep = 0.3f, creepTurn = 20f;
        public Vector2 creepEvery = new Vector2(1.4f, 2.6f);

        [Header("Found")]
        public float lurchSpeed = 6.5f, lurchTime = 0.22f, lurchPause = 0.32f;
        public float thrashSpeed = 3.4f;
        public Light lantern;                 // his lantern's light: dark until he's found
        public float lanternIntensity = 3.4f;
        public MothFlock[] flocks;            // to his lantern: the first when he's found, the second at half
        public float flocksReturn = 10f;      // scattered, a flock comes back to it after this long
        public AudioClip[] creakSound, revealSound;

        public bool Hidden { get; private set; } = true;
        public bool Watched { get; private set; }
        public bool Thrashing => burning != null && burning.Alight && IsAlive;
        public bool Frozen => !Hidden && Watched && !Thrashing && IsAlive
            && CurrentState != State.Busy && CurrentState != State.Yanked && CurrentState != State.Stunned && CurrentState != State.Staggered; // (reeling, he reels)
        public static event System.Action<Watchman> Crept, Revealed; // (tests)

        public string Title => title;
        public string Epithet => epithet;
        public float[] PhaseMarks => new[] { 0.5f };
        public int Phase => spilled ? 2 : 1;
        public AudioClip[] Music => music;
        public string Farewell => farewell;
        public string FarewellLine => farewellLine;

        Animation anim;
        Burning burning;
        Renderer glassRenderer;
        int glassSlot = -1;
        Material darkGlass, litGlass;
        (Vector3 position, Quaternion rotation)[] spots; // where the field stands: his place and theirs
        float creepClock, burstClock;
        float[] flockGone;                    // when each flock was last scattered (or -1: out, or not yet called)
        bool spilled, fightOn;

        protected override float Pace => Frozen ? 0f : 1f;
        protected override string IdleClip => "Idle";
        protected override string ChaseClip(float distance) => Thrashing ? "Thrash" : Lurching ? "Lurch" : "Idle";
        bool Lurching => burstClock % (lurchTime + lurchPause) < lurchTime;

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
            flockGone = new float[flocks != null ? flocks.Length : 0];
            Hide();
        }

        protected override void Start()
        {
            base.Start();
            Reform(); // (he's never in the same place twice: not even the first time)
        }

        // ---- unfound

        protected override bool Absorbs(Hit hit)
        {
            if (!Hidden)
                return false;
            Reveal();
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

        // Found out: he tears free of his stake, his lantern flares, and his Moths come to it.
        void Reveal()
        {
            Hidden = false;
            passive = false;
            Effects.Instance?.Straw(transform.position + Vector3.up * 1.1f, 40);
            Sfx.Instance?.Attack(revealSound, transform.position + Vector3.up, 1f, "watchman_reveal");
            CameraRig.Instance?.Shake(0.2f);
            SetLantern(true);
            BeBusy("Reveal", 1.2f);
            Call(0);
            Revealed?.Invoke(this);
        }

        // While he's behind the Apprentice: now and then, a little nearer, turned a little more to them.
        void Creep(float dt)
        {
            if (target == null || !target.IsAlive || Watched)
                return;
            Vector3 to = target.transform.position - transform.position;
            to.y = 0f;
            float distance = to.magnitude;
            if (distance > creepReach || distance < creepNearest)
                return;
            creepClock -= dt;
            if (creepClock > 0f)
                return;
            creepClock = Random.Range(creepEvery.x, creepEvery.y);
            var look = Quaternion.LookRotation(to / distance);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, creepTurn);
            PlaceAt(transform.position + to / distance * Mathf.Min(creepStep, distance - creepNearest));
            Scarecrow.StandPole(pole, transform);
            PlayShort("Lurch", 0.3f);
            Sfx.Instance?.Attack(creakSound, transform.position + Vector3.up, 0.5f, "watchman_creak");
            Crept?.Invoke(this);
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

        // His lantern: lit, it calls flock `i` to it.
        void Call(int i)
        {
            if (flocks == null || i >= flocks.Length || flocks[i] == null)
                return;
            flocks[i].Release(lantern != null ? lantern.transform.position : transform.position + Vector3.up * 1.5f);
            flockGone[i] = -1f;
        }

        // Half way: straw spills from his coat, his lantern flares, and the second flock comes.
        void Spill()
        {
            spilled = true;
            Effects.Instance?.Straw(transform.position + Vector3.up * 1.0f, 60);
            Sfx.Instance?.Attack(revealSound, transform.position + Vector3.up, 0.8f, "watchman_reveal", 0.85f);
            BeBusy("Stagger", 0.9f);
            Call(1);
        }

        protected override void Tick(float dt)
        {
            if (burning == null)
                burning = GetComponent<Burning>();
            Watched = SeenBy(target);
            if (anim != null)
                anim.enabled = !Frozen; // (stock still: the pose he was in)
            if (!IsAlive)
                return;
            if (Hidden)
            {
                Creep(dt);
                return;
            }
            burstClock += dt;
            if (!spilled && HealthFraction <= 0.5f && CurrentState != State.Busy)
                Spill();
            // Scattered, a flock comes back to the lantern a while later.
            for (int i = 0; i < flockGone.Length; i++)
            {
                if (flocks[i] == null || !flocks[i].gameObject.activeSelf || (i == 1 && !spilled))
                    continue;
                if (flocks[i].Alive > 0)
                    flockGone[i] = -1f;
                else if (flockGone[i] < 0f)
                    flockGone[i] = Time.time;
                else if (Time.time - flockGone[i] >= flocksReturn)
                    Call(i);
            }
            // The lantern: steady while he's watched, guttering when he isn't.
            if (lantern != null)
                lantern.intensity = lanternIntensity * (Frozen ? 0.8f : 0.55f + 0.45f * Mathf.PerlinNoise(Time.time * 14f, 0.7f));
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
            // The fight: on while he's found and they're near.
            bool on = !Hidden && IsAlive && target != null && target.IsAlive && Vector3.Distance(target.transform.position, transform.position) < leash;
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
            // Beaten: his lantern goes out, and its Moths fall with it.
            SetLantern(false);
            foreach (var f in flocks ?? new MothFlock[0])
                if (f != null && f.gameObject.activeSelf)
                    foreach (var m in f.Moths)
                        if (m != null && m.IsAlive)
                            m.Slay(transform.position);
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
            if (glassRenderer != null)
            {
                var materials = glassRenderer.sharedMaterials;
                materials[glassSlot] = lit ? litGlass : darkGlass;
                glassRenderer.sharedMaterials = materials;
            }
        }

        // One of them again, on his stake (in the place of another, at random), his lantern dark.
        void Hide()
        {
            Hidden = true;
            passive = true;
            spilled = false;
            creepClock = Random.Range(creepEvery.x, creepEvery.y);
            SetLantern(false);
            Scarecrow.StandPole(pole, transform);
            for (int i = 0; i < flockGone.Length; i++)
                flockGone[i] = -1f;
        }

        public override void Reform()
        {
            // A new place among them: shuffled, each to a spot (he to one, the scarecrows to the rest).
            var order = new int[spots.Length];
            for (int i = 0; i < order.Length; i++)
                order[i] = i;
            for (int i = order.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
            SetSpawn(spots[order[0]].position, spots[order[0]].rotation);
            for (int i = 0; field != null && i < field.Length; i++)
            {
                field[i].SetSpawn(spots[order[i + 1]].position, spots[order[i + 1]].rotation);
                field[i].Reform();
            }
            base.Reform();
            foreach (var f in flocks ?? new MothFlock[0])
                if (f != null)
                    f.Withdraw();
            if (burning != null)
                burning.Douse();
            Hide();
            BossFight.End(this);
            fightOn = false;
        }
    }
}
