using UnityEngine;

namespace MyGame
{
    // The Drowned: a Soured that couldn't let go of life. It waits under the water at an edge, only bubbles showing,
    // and follows the Apprentice along the shore below the surface (within its stretch of water). At the edge nearest
    // them it bursts up waist-high, arms flung up (the tell), and grabs: no guard stops it; a dash through it does. Caught,
    // they're dragged under (damage, then washed ashore: never a death by itself). Missed, it hangs over the edge,
    // clawing, open to blows, then sinks back. Weak to the Hook: hooked while it's up, it's hauled out onto dry ground,
    // stranded, reeling, then it crawls back and slips in. Under the water nothing reaches it (no lock-on, no Hook).
    public sealed class Drowned : Soured
    {
        [Header("The Drowned")]
        public float surfaceY = 0f;          // the water's surface (the model's origin, risen, is at the waterline)
        public float submergedDepth = 1.9f;  // how far under it waits
        public float riseSpeed = 5f, sinkSpeed = 3f;
        public float edgeClearance = 0.45f;  // how far out from an edge it rises (its arms reach the rest)
        public float grabDamage = 25f;
        public float clutchTime = 1.5f;      // missed: hanging over the edge, open to blows
        public float strandedStagger = 3f;   // hooked out: reeling on dry ground
        public float crawlSpeed = 0.9f;
        public float afterCatch = 4f;        // caught someone: it lets them get up before it tries again
        public AudioClip[] bubbleSound, surgeSound;

        public bool Submerged => height < 0.35f && !stranded;
        public bool Stranded => stranded;
        public bool Caught { get; private set; } // (tests: its last grab caught someone)
        public override bool Targetable => IsAlive && !Submerged;

        float height = 0f;      // 0 under, 1 risen
        bool stranded, sinking, stayDown;
        float nextGrab;
        float sinkAt = -1f, nextBubble, nextSurfaceCheck;
        Vector3 lurkAt;

        protected override void Awake()
        {
            base.Awake();
            lurkAt = transform.position;
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
        protected override string IdleClip => stranded ? "Stranded" : "Idle";
        protected override string ChaseClip(float distance) => stranded ? "Crawl" : "Swim";

        protected override SouredAttack ChooseAttack(Vector3 toTarget, float distance)
        {
            if (stranded || sinking || target == null || target.CurrentState == Apprentice.State.Sinking || Time.time < nextGrab)
                return null;
            // Only at the edge, right by them.
            return distance <= attackRange && Flat(lurkAt - transform.position).magnitude < 0.4f ? primary : null;
        }

        protected override void OnEntered(State state)
        {
            if (state == State.Telegraph)
                stayDown = false;
            if (state == State.Chase && !stranded && height > 0.5f && sinkAt < 0f)
                sinkAt = Time.time; // (after a stagger, back under)
        }

        // (its level is the water's: it rises to meet someone on the edge above it)
        protected override bool OnItsLevel() => target != null && Mathf.Abs(target.transform.position.y - surfaceY) < 1.3f;

        protected override bool LosesTarget(float distance) => !stranded && base.LosesTarget(distance); // (stranded, all it wants is the water)

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
                sinking = false;
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
            bool up = CurrentState == State.Telegraph || CurrentState == State.Strike || CurrentState == State.Recover
                      || CurrentState == State.Staggered || CurrentState == State.Stunned;
            if (sinkAt >= 0f && Time.time >= sinkAt && (CurrentState == State.Recover || CurrentState == State.Chase))
            {
                sinking = stayDown = true; // (and it stays under until it next rises to grab)
                sinkAt = -1f;
                clips.PlayOnce("Sink", 0.6f);
            }
            if (sinking && height <= 0.02f)
                sinking = false;
            up &= !stayDown;
            height = Mathf.MoveTowards(height, up && !sinking ? 1f : 0f, (up ? riseSpeed : sinkSpeed) * dt);

            // Under, it swims to the edge nearest the Apprentice (within its stretch of water), or back to where it waits.
            if (!up && !sinking)
            {
                if (Time.time >= nextSurfaceCheck)
                {
                    nextSurfaceCheck = Time.time + 0.2f;
                    lurkAt = CurrentState == State.Chase && target != null ? EdgeNear(target.transform.position) : Home;
                }
                Vector3 to = Flat(lurkAt - transform.position);
                float step = Mathf.Min(to.magnitude, chaseSpeed * dt);
                Vector3 next = transform.position + (to.sqrMagnitude > 1e-4f ? to.normalized * step : Vector3.zero);
                if (to.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to), turnSpeed * dt);
                PlaceAt(new Vector3(next.x, transform.position.y, next.z));
            }
            if ((up || CurrentState == State.Chase) && target != null && !sinking)
            {
                Vector3 look = Flat(target.transform.position - transform.position);
                if (look.sqrMagnitude > 0.01f && (up || height > 0.5f))
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(look), turnSpeed * 0.5f * dt);
            }
            var p = transform.position;
            PlaceAt(new Vector3(p.x, Mathf.Lerp(surfaceY - submergedDepth, surfaceY, Ease(height)), p.z));
            // Its grab: the moment the blow would land.
            if (CurrentState == State.Strike && StateTime >= primary.strike * primary.impactAt && !grabbed)
                Grab();
            if (CurrentState != State.Strike)
                grabbed = false;
            Bubbles();
        }

        bool grabbed;

        void Grab()
        {
            grabbed = true;
            Caught = false;
            if (target == null || !target.IsAlive)
                return;
            Vector3 to = Flat(target.transform.position - transform.position);
            if (to.magnitude > primary.reach + 0.35f || Vector3.Angle(transform.forward, to) > primary.arc || !OnItsLevel())
                return;
            Effects.Instance?.Splash(transform.position + transform.forward * 0.8f);
            if (target.DragUnder(this, grabDamage))
            {
                Caught = true;
                nextGrab = Time.time + afterCatch;
                clips.PlayOnce("Drag", 1.2f);
                sinkAt = Time.time + 0.4f; // down with them
            }
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
            // (dragging itself along the ground: the base keeps it off nothing, so it does its own stepping here)
            PlaceAt(transform.position + to.normalized * Mathf.Min(to.magnitude, crawlSpeed * dt));
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to), turnSpeed * 0.5f * dt);
        }

        void SlipIn(Vector3 at)
        {
            stranded = false;
            Effects.Instance?.Splash(new Vector3(at.x, surfaceY, at.z));
            Sfx.Instance?.Splash(new Vector3(at.x, surfaceY, at.z));
            height = 0.3f;
            sinking = true;
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
            Vector3 home = Home;
            Vector3 from = Flat(transform.position) + Vector3.up * surfaceY;
            Vector3 best = Flat(transform.position) + Vector3.up * transform.position.y;
            Vector3 dir = Flat(from - near);
            if (dir.sqrMagnitude < 0.01f)
                dir = -transform.forward;
            dir.Normalize();
            // Walk out from them towards it until it's open water with room round it.
            for (float d = 0.3f; d < 6f; d += 0.25f)
            {
                Vector3 p = new Vector3(near.x, surfaceY, near.z) + dir * d;
                if (!IsOpenWater(p, edgeClearance))
                    continue;
                if (Flat(p - home).magnitude > leashRadius)
                    break;
                return p;
            }
            return best;
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
            stranded = sinking = stayDown = false;
            nextGrab = 0f;
            height = 0f;
            sinkAt = -1f;
            PlaceUnder(transform.position);
            lurkAt = transform.position;
        }
    }

    static class VectorExtensions
    {
        public static Vector3 WithY(this Vector3 v, float y) => new Vector3(v.x, y, v.z);
    }
}
