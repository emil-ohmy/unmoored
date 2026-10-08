using System;
using UnityEngine;

namespace MyGame
{
    // The Ferryman's boat. Moored at a dock, it offers "Row": the Apprentice climbs in, the bow lantern goes up on its
    // pole, and the stick rows it where it points. It has weight: it gathers way slowly, glides on when you stop, and
    // turns better under way. It slides along whatever it meets (a quay, a post, an islet) and the lagoon's edge turns
    // it back. Near a dock, "Step ashore". Nothing on the water can hurt the Apprentice (the brief). Waking at a
    // Mooring post, the boat is back at its home dock. Fast water (Current) carries it; the Stroke's surge drives it on
    // harder than any rowing (Surge), the river bending to the blade in a silver wake.
    public sealed class Boat : MonoBehaviour, IInteractable
    {
        public Dock home;
        public Dock[] docks = new Dock[0];
        public Transform model;          // the hull (it bobs and leans; the root carries the course)
        public Transform seat;           // the Apprentice's feet, facing the bow
        public Transform passengerSeat;  // a Lingerer being rowed across
        public Transform stern;          // the wake trails from here
        public GameObject bowLantern;    // hung on the bow pole while rowing
        public Transform bowLanternGlass;

        [Header("Rowing")]
        public float maxSpeed = 3.6f;
        public float acceleration = 2.2f;
        public float drag = 0.65f;       // per second: how long it glides on
        public float turnRate = 75f;     // degrees per second, under way
        public float hullRadius = 0.55f; // each of three spheres, bow to stern, that meet the world
        public float hullHeight = 0.25f; // above the waterline
        public float coastSpeed = 1.1f;  // resting the oar near a dock (a Crossing's talk): drifting in on its own
        public Rect lagoon = new Rect(-52f, -54f, 74f, 74f); // x, z: rowing further, the water turns you back
        public float interactRadius = 0.8f;

        public event Action Boarded, WentHome;
        public event Action<Dock> SteppedAshore;

        public bool Rowing => rower != null;
        public Apprentice Rower => rower;
        public Dock MooredAt { get; private set; }
        public float Speed => speed;
        public float SurgeSpeed => surge;               // the Stroke's surge, on top of the rowing
        public bool Surging => surge > 0.5f;
        public float Effort { get; private set; }       // how hard it's being rowed, 0..1 (the stroke's pace)
        public float StrokeSpeed => Mathf.Lerp(0.8f, 1.35f, Effort); // the Row clip's pace (one stroke a second at 1)
        public bool Coasting => coastTo != null;
        public int Strokes { get; private set; }
        public Vector3 Bow => transform.forward;

        // The dock the boat is near enough to put in at (none while resting the oar for a talk).
        public Dock DockInReach
        {
            get
            {
                if (!Rowing)
                    return null;
                foreach (var d in docks)
                    if (d != null && Flat(d.mooring.position - transform.position).magnitude <= d.reach)
                        return d;
                return null;
            }
        }

        Apprentice rower;
        float speed, strokePhase, mooredAt = -10f, bumpedAt = -10f, surge, moorTime = MoorTime;
        Apprentice walker; // (the one who might step into it, moored)
        Vector3 mooringFrom;
        Quaternion mooringFromRotation;
        Dock coastTo;
        TrailRenderer wake;
        Transform lanternHome;
        Vector3 lanternHomeLocal;
        Collider[] own;
        const float MoorTime = 0.8f;

        void Awake()
        {
            own = GetComponentsInChildren<Collider>(true);
            if (model != null)
                modelRest = model.localRotation;
            if (stern != null)
                wake = stern.GetComponent<TrailRenderer>();
            if (bowLantern != null)
                bowLantern.SetActive(false);
        }

        void Start()
        {
            if (home != null)
                MoorAt(home, instantly: true);
            var a = FindAnyObjectByType<Apprentice>();
            if (a != null)
                a.Woke += ReturnHome;
        }

        void OnDestroy()
        {
            var a = FindAnyObjectByType<Apprentice>();
            if (a != null)
                a.Woke -= ReturnHome;
        }

        void OnEnable() => Interactables.Register(this);
        void OnDisable() => Interactables.Unregister(this);

        public Vector3 InteractPosition => MooredAt != null && MooredAt.ashore != null ? MooredAt.ashore.position : transform.position;
        public float InteractRadius => interactRadius;
        public bool Available => !Rowing && MooredAt != null;
        public string PromptLabel => "Row";
        public void Interact(Apprentice apprentice) => apprentice.Board(this);

        // ---- the Apprentice in and out

        public void TakeRower(Apprentice apprentice)
        {
            rower = apprentice;
            MooredAt = null;
            speed = 0f;
            strokePhase = 0f;
            coastTo = null;
            if (bowLantern != null)
                bowLantern.SetActive(true);
            var lantern = apprentice.GetComponent<BowLantern>();
            if (lantern != null && lantern.lanternLight != null && bowLanternGlass != null)
            {
                var light = lantern.lanternLight.transform; // the lantern's own light, hung on the bow with it
                lanternHome = light.parent;
                lanternHomeLocal = light.localPosition;
                light.SetParent(bowLanternGlass, false);
                light.localPosition = Vector3.zero;
                ResetFlickerHome(light);
            }
            CameraRig.Instance?.SetZoom(1.25f); // a little more water in view
            Boarded?.Invoke();
        }

        public void ReleaseRower(Dock at)
        {
            if (rower != null)
            {
                var lantern = rower.GetComponent<BowLantern>();
                if (lantern != null && lantern.lanternLight != null && lanternHome != null)
                {
                    var light = lantern.lanternLight.transform;
                    light.SetParent(lanternHome, false);
                    light.localPosition = lanternHomeLocal;
                    ResetFlickerHome(light);
                }
            }
            rower = null;
            coastTo = null;
            if (bowLantern != null)
                bowLantern.SetActive(false);
            CameraRig.Instance?.SetZoom(1f);
            MoorAt(at, instantly: false);
            SteppedAshore?.Invoke(at);
        }

        // (a flicker's wobble is about where the light sat when it woke: tell it where it is now)
        static void ResetFlickerHome(Transform light)
        {
            if (light.TryGetComponent<LightFlicker>(out var flicker))
                flicker.Rehome();
        }

        // Waking at a Mooring post: the boat is back at its home dock (nobody's left stranded across the water).
        public void ReturnHome()
        {
            if (Rowing || home == null)
                return;
            MoorAt(home, instantly: true);
            WentHome?.Invoke();
        }

        void MoorAt(Dock dock, bool instantly, float time = MoorTime)
        {
            MooredAt = dock;
            moorTime = time;
            speed = surge = 0f;
            mooringFrom = transform.position;
            mooringFromRotation = transform.rotation;
            mooredAt = instantly ? -10f : Time.time;
            if (instantly)
                transform.SetPositionAndRotation(dock.mooring.position, dock.mooring.rotation);
        }

        // Called to a dock (its bell): it comes gliding in from out on the water, bow first, and ties up there.
        public void Summon(Dock dock)
        {
            if (Rowing || dock == null || MooredAt == dock)
                return;
            Vector3 offshore = dock.mooring.position - dock.mooring.forward * 6f;
            transform.SetPositionAndRotation(new Vector3(offshore.x, transform.position.y, offshore.z), dock.mooring.rotation);
            Effects.Instance?.Splash(transform.position);
            for (int i = 0; i < 8; i++)
                Effects.Instance?.Wisp(transform.position + UnityEngine.Random.insideUnitSphere * 0.8f, DrownedMoon.Moonlight);
            MoorAt(dock, instantly: false, time: 2.2f);
        }

        // Resting the oar and drifting in to a dock on its own (a Lingerer's talk on the water); null: row again.
        public void Coast(Dock to) => coastTo = to;

        // (tests) Straight onto the water here, heading `bow`.
        public void PlaceAt(Vector3 position, Vector3 bow)
        {
            transform.SetPositionAndRotation(new Vector3(position.x, transform.position.y, position.z), Quaternion.LookRotation(Flat(bow)));
            speed = 0f;
        }

        // ---- rowing (ticked by the Apprentice, while in the boat)

        // The Stroke let go from the boat: driven on along its bow, hard, the surge dying away over a second or two.
        public void Surge(float by) => surge = Mathf.Max(surge, by);

        // Rowing (the stick), or with the oar drawn back for the Stroke (`drawing`: no rowing, only the helm).
        public void Tick(Vector3 steer, float dt, bool drawing = false)
        {
            Vector3 want;
            if (drawing)
            {
                want = Flat(steer);
                Effort = Mathf.MoveTowards(Effort, 0f, 3f * dt);
                if (want.sqrMagnitude > 0.0025f)
                    Turn(want.normalized, 0.6f, dt);
            }
            else if (coastTo != null)
            {
                Vector3 to = Flat(coastTo.mooring.position - transform.position);
                want = to.normalized;
                float target = to.magnitude < coastTo.reach * 0.6f ? 0f : coastSpeed;
                speed = Mathf.MoveTowards(speed, target, 1.2f * dt);
                Effort = 0f;
                Turn(want, 0.6f, dt);
            }
            else
            {
                want = Flat(steer);
                float push = Mathf.Clamp01(want.magnitude);
                if (push > 0.05f)
                {
                    Turn(want.normalized, 1f, dt);
                    // Rowing hardest along the bow; turning in place still makes a little way.
                    float along = Mathf.Max(0.3f, Vector3.Dot(Bow, want.normalized));
                    Effort = push * along;
                }
                else
                    Effort = Mathf.MoveTowards(Effort, 0f, 3f * dt);
                speed += Effort * acceleration * dt;
            }
            speed *= Mathf.Exp(-drag * dt);
            speed = Mathf.Min(speed, maxSpeed);
            surge *= Mathf.Exp(-1.2f * dt);
            Move((Bow * (speed + surge) + Current.At(transform.position)) * dt); // (fast water carries it, whatever the oar does)
            KeepToTheLagoon(dt);
            if (Surging)
                Wake(dt);
            Strokes_(dt);
        }

        void Turn(Vector3 towards, float factor, float dt)
        {
            if (towards.sqrMagnitude < 0.0001f)
                return;
            float rate = turnRate * factor * Mathf.Lerp(0.55f, 1f, speed / maxSpeed); // it answers the helm better under way
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(towards), rate * dt);
        }

        // The surge's wake: silver spray thrown off both sides, motes of the Drowned Moon's light left on the water.
        void Wake(float dt)
        {
            var fx = Effects.Instance;
            if (fx == null)
                return;
            int n = Mathf.CeilToInt(surge * 4f * dt);
            for (int i = 0; i < n; i++)
            {
                float side = UnityEngine.Random.value < 0.5f ? -1f : 1f;
                Vector3 at = transform.position + transform.right * side * 0.6f + Bow * UnityEngine.Random.Range(-0.4f, 0.9f) + Vector3.up * 0.1f;
                fx.Streak(at, transform.right * side * UnityEngine.Random.Range(1.5f, 3f) + Vector3.up * UnityEngine.Random.Range(1f, 2.2f) - Bow * surge * 0.2f,
                          new Color(0.85f, 0.93f, 1f, 1f), UnityEngine.Random.Range(0.25f, 0.45f), UnityEngine.Random.Range(0.05f, 0.09f));
                if (UnityEngine.Random.value < 0.3f)
                    fx.Wisp(transform.position - Bow * 1.2f + Vector3.up * 0.05f, DrownedMoon.Moonlight, Vector3.up * 0.3f, UnityEngine.Random.Range(1f, 1.8f));
            }
        }

        // Every stroke: the blade dipping in on alternate sides, its sound and a few drops.
        void Strokes_(float dt)
        {
            if (Effort <= 0.05f || coastTo != null)
                return;
            float before = strokePhase;
            strokePhase += StrokeSpeed * dt;
            if (Mathf.Floor(strokePhase) > Mathf.Floor(before) || before == 0f)
            {
                Strokes++;
                float side = (Strokes % 2 == 0) ? 1f : -1f;
                Vector3 dip = transform.position + transform.right * (0.95f * side) + Bow * 0.35f;
                dip.y = transform.position.y + 0.08f;
                Sfx.Instance?.OarStroke(dip);
                Effects.Instance?.OarDip(dip);
            }
        }

        // Moored and still: walking (or dashing) off the jetty into it is getting in (nobody steps into the river by it).
        void StepIn()
        {
            if (Rowing || MooredAt == null || mooredAt >= 0f)
                return;
            if (walker == null)
                walker = FindAnyObjectByType<Apprentice>();
            if (walker == null || !walker.IsAlive)
                return;
            Vector3 local = transform.InverseTransformPoint(walker.transform.position);
            if (Mathf.Abs(local.x) < 0.6f && Mathf.Abs(local.z) < 1.2f && local.y > -0.5f && local.y < 1.2f)
                walker.Board(this);
        }

        // Three spheres along the hull, cast along the move: stop short of what they meet and slide along it.
        void Move(Vector3 delta)
        {
            for (int pass = 0; pass < 2 && delta.sqrMagnitude > 1e-8f; pass++)
            {
                float distance = delta.magnitude;
                Vector3 dir = delta / distance;
                if (!Cast(dir, distance + 0.05f, out RaycastHit hit, out float along))
                {
                    transform.position += delta;
                    return;
                }
                float free = Mathf.Max(0f, along - 0.05f);
                transform.position += dir * free;
                Vector3 normal = Flat(hit.normal).normalized;
                if (normal.sqrMagnitude < 0.001f)
                    normal = -dir;
                Vector3 rest = dir * (distance - free);
                delta = rest - normal * Vector3.Dot(rest, normal); // slide along it
                if (Vector3.Dot(dir, normal) < -0.6f)
                {
                    speed *= 0.5f; // a knock head-on
                    if (Time.time - bumpedAt > 0.6f)
                    {
                        bumpedAt = Time.time;
                        CameraRig.Instance?.Shake(0.06f);
                    }
                }
            }
        }

        bool Cast(Vector3 dir, float distance, out RaycastHit best, out float bestDistance)
        {
            best = default;
            bestDistance = float.MaxValue;
            float half = 0.8f;
            foreach (float at in new[] { half, 0f, -half })
            {
                Vector3 origin = transform.position + Bow * at + Vector3.up * hullHeight;
                foreach (var hit in Physics.SphereCastAll(origin, hullRadius, dir, distance, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (hit.distance <= 0f || hit.collider is CharacterController || Array.IndexOf(own, hit.collider) >= 0)
                        continue;
                    if (hit.distance < bestDistance)
                    {
                        bestDistance = hit.distance;
                        best = hit;
                    }
                }
            }
            return bestDistance < float.MaxValue;
        }

        // Past the lagoon's edge, the water itself turns you back.
        void KeepToTheLagoon(float dt)
        {
            Vector3 p = transform.position;
            Vector3 inward = Vector3.zero;
            if (p.x < lagoon.xMin) inward.x = 1f;
            if (p.x > lagoon.xMax) inward.x = -1f;
            if (p.z < lagoon.yMin) inward.z = 1f;
            if (p.z > lagoon.yMax) inward.z = -1f;
            if (inward == Vector3.zero)
                return;
            transform.position += inward.normalized * 2.5f * dt;
            speed *= Mathf.Exp(-2.5f * dt);
        }

        public bool InTheLagoon(Vector3 p, float margin) =>
            p.x >= lagoon.xMin - margin && p.x <= lagoon.xMax + margin && p.z >= lagoon.yMin - margin && p.z <= lagoon.yMax + margin;

        void Update()
        {
            if (MooredAt != null && mooredAt >= 0f)
            {
                float t = Mathf.Clamp01((Time.time - mooredAt) / moorTime); // drawn in alongside and tied up
                float e = UiKit.Smooth(0f, 1f, t);
                transform.SetPositionAndRotation(Vector3.Lerp(mooringFrom, MooredAt.mooring.position, e),
                    Quaternion.Slerp(mooringFromRotation, MooredAt.mooring.rotation, e));
                if (t >= 1f)
                    mooredAt = -10f;
            }
            if (wake != null)
                wake.emitting = speed + surge > 0.4f || MooredAt != null && mooredAt >= 0f;
            StepIn();
        }

        void LateUpdate()
        {
            if (model == null)
                return;
            // Riding the water: a slow bob and roll, a little more under way; leaning into a turn.
            float t = Time.time;
            float way = speed / maxSpeed;
            model.localPosition = Vector3.up * (0.025f * Mathf.Sin(t * 1.3f));
            model.localRotation = Quaternion.Euler(1.2f * Mathf.Sin(t * 0.9f) - 2f * way, 0f, (1.5f + way) * Mathf.Sin(t * 1.1f + 0.7f)) * modelRest;
        }

        Quaternion modelRest = Quaternion.identity; // the hull's own rotation under the root, as it was placed

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
