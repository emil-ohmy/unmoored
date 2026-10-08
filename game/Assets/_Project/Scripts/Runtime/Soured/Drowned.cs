using UnityEngine;

namespace MyGame
{
    // The Drowned: a Soured that couldn't let go of life. It waits under the water, only bubbles showing, and nothing
    // reaches it there (no lock-on, no Hook, no blows). When it can see the Apprentice, and they're in range, it rises
    // waist-high, gathers itself (the tell) and spits a glob of poison at them, then sinks and waits to spit again. A
    // perfect block turns the spit back, and a Drowned struck by its own spit is slain outright. Hide from it (out of
    // its sight) and it swims to the shore nearest you and comes up there, hanging on the edge, looking for you: open to
    // the oar. Weak to the Hook: caught while it's up, it's hauled out over the Apprentice onto the ground, stranded and
    // reeling; then it crawls back to the water and slips in.
    public sealed class Drowned : Soured
    {
        [Header("The Drowned")]
        public float surfaceY = 0f;          // the water's surface (the model's origin, risen, is at the waterline)
        public float submergedDepth = 1.9f;  // how far under it waits
        public float riseSpeed = 4f, sinkSpeed = 3f;
        public float edgeClearance = 0.45f;  // how far out from an edge it comes up at the shore
        public float spitRange = 9f;
        public float spitCooldown = 2.2f;    // under the water between spits
        public float ashoreTime = 5f;        // come to the shore after someone hiding: how long it looks for them
        public float strandedStagger = 3f;   // hooked out: reeling on dry ground
        public float crawlSpeed = 0.9f;
        public float spitSpeed = 9f;
        public AudioClip[] bubbleSound;

        public bool Submerged => height < 0.35f && !stranded;
        public bool Stranded => stranded;
        public bool Ashore => ashore;          // come up at the shore, looking for someone out of sight
        public override bool Targetable => IsAlive && !Submerged;
        public static event System.Action<Drowned, Spit> Spat; // (tests)

        float height = 0f;      // 0 under, 1 risen
        bool stranded, sinking, stayDown, ashore;
        float nextSpit, ashoreUntil = -1f, sinkAt = -1f, nextBubble, nextSurfaceCheck, sawAt = -10f;
        int sightFrame = -1;
        bool hadSight;
        Vector3 lurkAt;
        Material glob;

        protected override void Awake()
        {
            base.Awake();
            lurkAt = transform.position;
            foreach (var r in GetComponentsInChildren<Renderer>())
                foreach (var m in r.sharedMaterials)
                    if (m != null && m.name.StartsWith("SouredEye_Glow"))
                        glob = m;
        }

        protected override void Start()
        {
            base.Start();
            PlaceUnder(transform.position);
        }

        protected override bool Surface() => true; // it lives there
        protected override bool Floats => !stranded; // (it swims: there's nothing under it to stand on)
        protected override bool Unhittable => Submerged;
        protected override Vector3 ChaseStep(Vector3 toTarget, float distance) => Vector3.zero; // (it swims: Tick)
        protected override string IdleClip => stranded ? "Stranded" : ashore ? "Clutch" : "Idle";
        protected override string ChaseClip(float distance) => stranded ? "Crawl" : ashore || height > 0.5f ? "Clutch" : "Swim";
        // (its level is the water's: someone on the edge above it is on its level)
        protected override bool OnItsLevel() => target != null && Mathf.Abs(target.transform.position.y - surfaceY) < 1.3f;
        protected override bool LosesTarget(float distance) => !stranded && base.LosesTarget(distance); // (stranded, all it wants is the water)

        // Spit, when it can see them, they're in range, and it's had its breath.
        protected override SouredAttack ChooseAttack(Vector3 toTarget, float distance)
        {
            if (stranded || sinking || target == null || Time.time < nextSpit || distance > spitRange || !CanSee())
                return null;
            return primary;
        }

        protected override void OnEntered(State state)
        {
            if (state == State.Telegraph)
            {
                stayDown = false;
                ashore = false;
            }
            if (state == State.Recover)
            {
                nextSpit = Time.time + primary.recover + spitCooldown;
                sinkAt = Time.time + primary.recover * 0.8f; // (spat: back under)
            }
            if (state == State.Chase && !stranded && height > 0.5f && sinkAt < 0f && !ashore)
                sinkAt = Time.time; // (after a stagger, back under)
        }

        // The spit, along its aim (sidestepping escapes it), at the height they were at.
        protected override void Shoot(SouredAttack attack, Vector3 direction, int index)
        {
            Vector3 from = transform.position + Vector3.up * 0.8f + transform.forward * 0.25f;
            Vector3 chest = target.transform.position + Vector3.up * 0.65f;
            float flat = Flat(chest - from).magnitude;
            Vector3 at = from + Flat(direction).normalized * flat;
            at.y = chest.y;
            var spit = Spit.Fire(from, at, spitSpeed, target, this, glob);
            Spat?.Invoke(this, spit);
        }

        // The Stroke's wave, up (risen to spit, or at the shore): knocked under, its spit broken off, and held down a
        // while (heldUnder) before it can rise again; a little harm. Under, it passes over. Stranded: as anyone.
        public float heldUnder = 2f;
        public bool KnockedUnder => Time.time < heldUntil;
        float heldUntil = -10f;

        public override HitResult TakeWave(Vector3 push, float damage, Vector3 from, float stagger)
        {
            if (stranded)
                return base.TakeWave(push, damage, from, stagger);
            if (!IsAlive || Submerged)
                return HitResult.Missed;
            var result = base.TakeWave(Vector3.zero, damage, from, 0.01f); // (the harm; it's the water's to move)
            if (!IsAlive)
                return result;
            heldUntil = Time.time + heldUnder;
            nextSpit = Mathf.Max(nextSpit, heldUntil + 0.5f);
            ashore = false;
            sinking = stayDown = true;
            sinkAt = -1f;
            BeBusy("Sink", heldUnder);
            Effects.Instance?.Splash(new Vector3(transform.position.x, surfaceY, transform.position.z));
            return result;
        }

        // The Hook: under the water, nothing to catch; up, it's hauled out onto the ground, stranded.
        public override HookResult OnHooked(Apprentice by)
        {
            if (Submerged || stranded)
                return HookResult.Shrugged;
            yankStagger = strandedStagger;
            var result = base.OnHooked(by);
            if (result == HookResult.Yanked)
            {
                // Hauled right out and over, onto the ground beyond the Apprentice (not dropped back at the edge).
                Vector3 over = Flat(by.transform.position - transform.position);
                over = over.sqrMagnitude > 0.01f ? over.normalized : by.transform.forward;
                RetargetYank(by.transform.position + over * 1.4f);
                stranded = true; // (out on the ground now: it falls, and crawls)
                sinking = ashore = false;
                sinkAt = -1f;
                Sfx.Instance?.Splash(transform.position);
                Effects.Instance?.Splash(transform.position);
            }
            return result;
        }

        protected override void Tick(float dt)
        {
            if (!IsAlive)
                return;
            if (stranded)
            {
                TickStranded(dt);
                return;
            }
            bool attacking = CurrentState == State.Telegraph || CurrentState == State.Strike || CurrentState == State.Recover
                             || CurrentState == State.Staggered || CurrentState == State.Stunned;
            if (sinkAt >= 0f && Time.time >= sinkAt && (CurrentState == State.Recover || CurrentState == State.Chase))
            {
                sinking = stayDown = true; // (and it stays under until it next rises)
                sinkAt = -1f;
                clips.PlayOnce("Sink", 0.6f);
            }
            if (sinking && height <= 0.02f)
                sinking = false;

            // Where to be: under where it waits; after someone it can see, where it is (it rises to spit); after someone
            // hiding, the shore nearest them, where it comes up and looks.
            if (!attacking && !sinking && Time.time >= nextSurfaceCheck)
            {
                nextSurfaceCheck = Time.time + 0.2f;
                if (CurrentState != State.Chase || target == null)
                {
                    lurkAt = Home;
                    ashore = false;
                }
                else if (CanSee() && Flat(target.transform.position - transform.position).magnitude <= spitRange)
                {
                    sawAt = Time.time;
                    if (!ashore)
                        lurkAt = transform.position;
                }
                else if (!ashore)
                {
                    lurkAt = EdgeNear(target.transform.position);
                    if (Flat(lurkAt - transform.position).magnitude < 0.4f)
                    {
                        ashore = true; // at the shore: up, looking for them
                        ashoreUntil = Time.time + ashoreTime;
                        stayDown = false;
                        Effects.Instance?.Splash(new Vector3(transform.position.x, surfaceY, transform.position.z));
                    }
                }
                if (ashore && Time.time >= ashoreUntil && Time.time - sawAt > 1f)
                {
                    ashore = false; // nobody: back under, and home
                    sinking = stayDown = true;
                    lurkAt = Home;
                }
            }
            bool up = (attacking || ashore) && !stayDown && !KnockedUnder;
            height = Mathf.MoveTowards(height, up && !sinking ? 1f : 0f, (up ? riseSpeed : sinkSpeed) * dt);

            if (!up && !sinking)
            {
                Vector3 to = Flat(lurkAt - transform.position);
                float step = Mathf.Min(to.magnitude, chaseSpeed * dt);
                if (to.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to), turnSpeed * dt);
                    var next = transform.position + to.normalized * step;
                    PlaceAt(new Vector3(next.x, transform.position.y, next.z));
                }
            }
            if ((up || height > 0.3f) && target != null && !sinking && CurrentState != State.Strike)
            {
                Vector3 look = Flat(target.transform.position - transform.position);
                if (look.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(look), turnSpeed * 0.5f * dt);
            }
            var p = transform.position;
            PlaceAt(new Vector3(p.x, Mathf.Lerp(surfaceY - submergedDepth, surfaceY, Ease(height)), p.z));
            Bubbles();
        }

        // A clear line from its head, up out of the water, to the Apprentice (walls, crates, railings hide them).
        bool CanSee()
        {
            if (sightFrame == Time.frameCount)
                return hadSight;
            sightFrame = Time.frameCount;
            hadSight = false;
            if (target == null)
                return false;
            Vector3 from = new Vector3(transform.position.x, surfaceY + 0.9f, transform.position.z), to = target.transform.position + Vector3.up * 0.7f;
            Vector3 d = to - from;
            hadSight = true;
            foreach (var hit in Physics.RaycastAll(from, d.normalized, d.magnitude, ~0, QueryTriggerInteraction.Ignore))
                if (!(hit.collider is CharacterController))
                {
                    hadSight = false;
                    break;
                }
            return hadSight;
        }

        // Hooked out: reeling (the stagger), then it drags itself back to the nearest water and slips in.
        void TickStranded(float dt)
        {
            if (CurrentState != State.Chase && CurrentState != State.Idle && CurrentState != State.Returning)
                return;
            Vector3 water = WaterNear(transform.position);
            Vector3 to = Flat(water - transform.position);
            if (to.magnitude < 0.5f)
            {
                SlipIn(water);
                return;
            }
            PlaceAt(transform.position + to.normalized * Mathf.Min(to.magnitude, crawlSpeed * dt));
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to), turnSpeed * 0.5f * dt);
        }

        void SlipIn(Vector3 at)
        {
            stranded = false;
            Effects.Instance?.Splash(new Vector3(at.x, surfaceY, at.z));
            Sfx.Instance?.Splash(new Vector3(at.x, surfaceY, at.z));
            height = 0.3f;
            sinking = stayDown = true;
            PlaceUnder(at);
            lurkAt = at;
        }

        void PlaceUnder(Vector3 at) => PlaceAt(new Vector3(at.x, surfaceY - submergedDepth * (1f - Ease(height)), at.z));

        // Now and then, a few bubbles where it waits (and a gurgle, close by): the only sign of it.
        void Bubbles()
        {
            if (!Submerged || Time.time < nextBubble)
                return;
            nextBubble = Time.time + Random.Range(1.2f, 2.6f);
            Vector3 at = new Vector3(transform.position.x, surfaceY, transform.position.z) + Random.insideUnitSphere.WithY(0f) * 0.4f;
            Effects.Instance?.OarDip(at);
            if (target != null && Vector3.Distance(target.transform.position, at) < 6f && Random.value < 0.5f)
                Sfx.Instance?.Attack(bubbleSound, at, 0.6f, "drowned_bubbles");
        }

        // The point in the water at the edge nearest `near`, out from the edge by edgeClearance, within its leash.
        Vector3 EdgeNear(Vector3 near)
        {
            Vector3 dir = Flat(transform.position - near);
            if (dir.sqrMagnitude < 0.01f)
                dir = -transform.forward;
            dir.Normalize();
            for (float d = 0.3f; d < 8f; d += 0.25f)
            {
                Vector3 p = new Vector3(near.x, surfaceY, near.z) + dir * d;
                if (!IsOpenWater(p, edgeClearance))
                    continue;
                if (Flat(p - Home).magnitude > leashRadius)
                    break;
                return p;
            }
            return transform.position;
        }

        // The nearest open water to a point on land (for crawling back).
        Vector3 WaterNear(Vector3 at)
        {
            Vector3 best = Home;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < 16; i++)
            {
                Vector3 dir = Quaternion.Euler(0f, i * 22.5f, 0f) * Vector3.forward;
                for (float d = 0.5f; d < 6f; d += 0.25f)
                {
                    Vector3 p = new Vector3(at.x, surfaceY, at.z) + dir * d;
                    if (IsOpenWater(p, 0.2f))
                    {
                        if (d < bestDistance)
                        {
                            bestDistance = d;
                            best = p;
                        }
                        break;
                    }
                }
            }
            return best;
        }

        // Open water here: nothing to stand on within `clear` of the point.
        public static bool IsOpenWater(Vector3 p, float clear)
        {
            foreach (var o in new[] { Vector3.zero, Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
            {
                Vector3 at = p + o * clear;
                foreach (var hit in Physics.RaycastAll(at + Vector3.up * 3f, Vector3.down, 3.4f, ~0, QueryTriggerInteraction.Ignore))
                    if (!(hit.collider is CharacterController))
                        return false;
            }
            return true;
        }

        static float Ease(float t) => t * t * (3f - 2f * t);
        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        public override void Reform()
        {
            base.Reform();
            stranded = sinking = stayDown = ashore = false;
            nextSpit = 0f;
            height = 0f;
            sinkAt = ashoreUntil = -1f;
            PlaceUnder(transform.position);
            lurkAt = transform.position;
        }
    }

    static class VectorExtensions
    {
        public static Vector3 WithY(this Vector3 v, float y) => new Vector3(v.x, y, v.z);
    }
}
