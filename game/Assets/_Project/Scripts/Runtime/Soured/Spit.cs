using UnityEngine;

namespace MyGame
{
    // A Drowned's spit: a glob of poison, sickly green, flying at the Apprentice in a shallow arc and dripping as it goes.
    // A dash's i-frames let it pass. A block stops it (a little poison gets through); taken, it hurts a little and
    // builds a lot of poison. A perfect block turns it back, pale blue, at the one who spat it: a Drowned struck by
    // its own spit is slain outright (any other Soured just takes a hit). It splashes where it strikes the world.
    public sealed class Spit : MonoBehaviour
    {
        public static int Live { get; private set; }
        public static event System.Action<Spit, Defence> Resolved; // what the Apprentice did about it (tests)

        public const float Radius = 0.4f, MaxRange = 16f;
        public float damage = 6f, poisonTaken = 55f, poisonBlocked = 15f; // (two taken: poisoned)
        public bool Deflected { get; private set; }
        public Soured Owner => owner;

        static readonly Color Green = new Color(0.55f, 0.9f, 0.3f), DeflectedColour = new Color(0.62f, 0.77f, 1f);

        Vector3 velocity, start;
        float gravity;
        Apprentice target;
        Soured owner;
        bool passed;
        Light glow;
        Renderer body;
        float nextDrip;

        // Spat from `from` to land on `at` (their chest, where they stand now) at `speed`, in a shallow arc.
        public static Spit Fire(Vector3 from, Vector3 at, float speed, Apprentice target, Soured owner, Material glob)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Spit";
            Destroy(go.GetComponent<Collider>());
            go.transform.position = from;
            go.transform.localScale = Vector3.one * 0.22f;
            var spit = go.AddComponent<Spit>();
            spit.body = go.GetComponent<MeshRenderer>();
            if (glob != null)
                spit.body.sharedMaterial = glob;
            spit.body.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var tint = new MaterialPropertyBlock(); // (a sickly green glob, not a white-hot one)
            tint.SetColor("_BaseColor", new Color(0.35f, 0.55f, 0.18f));
            tint.SetColor("_EmissionColor", new Color(0.35f, 0.7f, 0.15f));
            spit.body.SetPropertyBlock(tint);
            Vector3 flat = new Vector3(at.x - from.x, 0f, at.z - from.z);
            float time = Mathf.Max(0.2f, flat.magnitude / speed);
            spit.gravity = 7f;
            // Up enough to fall onto `at` after `time` (a lob that's still mostly flat).
            spit.velocity = flat / time + Vector3.up * ((at.y - from.y) / time + 0.5f * spit.gravity * time);
            spit.start = from;
            spit.target = target;
            spit.owner = owner;
            spit.glow = new GameObject("Spit Light").AddComponent<Light>();
            spit.glow.transform.SetParent(go.transform, false);
            spit.glow.type = LightType.Point;
            spit.glow.color = Green;
            spit.glow.range = 2.4f;
            spit.glow.intensity = 1.6f;
            spit.glow.shadows = LightShadows.None;
            return spit;
        }

        void OnEnable() => Live++;
        void OnDisable() => Live--;

        void Update()
        {
            float dt = Time.deltaTime;
            velocity += Vector3.down * gravity * dt;
            Vector3 from = transform.position, step = velocity * dt, to = from + step;
            transform.localScale = Vector3.one * (0.22f + 0.03f * Mathf.Sin(Time.time * 30f)); // (it wobbles)
            if (Time.time >= nextDrip)
            {
                nextDrip = Time.time + 0.03f;
                Effects.Instance?.Wisp(from, (Deflected ? DeflectedColour : Green) * 0.9f, Vector3.down * 0.4f + Random.insideUnitSphere * 0.3f, 0.35f);
            }
            if (!passed && !Deflected && target != null && target.IsAlive && Near(from, to, target.transform.position + Vector3.up * 0.6f, Radius + 0.3f))
            {
                var defence = target.TakeHit(damage, velocity.normalized.WithY(0f) * 2f, owner);
                Resolved?.Invoke(this, defence);
                if (defence == Defence.Parried)
                {
                    Deflect(from);
                    return;
                }
                if (defence == Defence.Dodged)
                    passed = true; // through the i-frames: it flies on
                else
                {
                    target.AddPoison(defence == Defence.Blocked ? poisonBlocked : poisonTaken);
                    Splash(from);
                    return;
                }
            }
            if (Deflected && StrikeSoured(from, to))
                return;
            foreach (var hit in Physics.RaycastAll(from, step.normalized, step.magnitude, ~0, QueryTriggerInteraction.Ignore))
                if (!(hit.collider is CharacterController))
                {
                    Splash(hit.point);
                    return;
                }
            transform.position = to;
            if (to.y < -0.2f)
            {
                Effects.Instance?.OarDip(new Vector3(to.x, 0f, to.z)); // into the water: gone
                Destroy(gameObject);
                return;
            }
            if (Vector3.Distance(start, to) > MaxRange)
                Destroy(gameObject);
        }

        // Turned back by a perfect block: straight at the one who spat it (where it is now), fast and flat, pale blue.
        void Deflect(Vector3 at)
        {
            Deflected = passed = true;
            gravity = 0f;
            Vector3 aim = owner != null && owner.IsAlive ? owner.transform.position + Vector3.up * 0.7f : at - velocity;
            velocity = (aim - at).normalized * Mathf.Max(12f, velocity.magnitude * 1.4f);
            start = at;
            glow.color = DeflectedColour;
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", DeflectedColour);
            block.SetColor("_EmissionColor", DeflectedColour * 1.5f);
            body.SetPropertyBlock(block);
        }

        // Turned back, it strikes the first Soured in its way: the one who spat it is slain outright; another takes a hit.
        bool StrikeSoured(Vector3 from, Vector3 to)
        {
            foreach (var soured in WorldState.AllSoured)
            {
                if (soured == null || !soured.IsAlive || !soured.isActiveAndEnabled
                    || !Near(from, to, soured.transform.position + Vector3.up * 0.6f, soured.radius + 0.35f))
                    continue;
                if (soured is Drowned)
                    soured.Slay(from);
                else
                    soured.TakeHit(new Hit { Damage = 20f, Stagger = 30f, Knockback = velocity.normalized * 3f, From = from - velocity.normalized });
                Effects.Instance?.BlockSpark(to, -velocity.normalized);
                Splash(to);
                return true;
            }
            return false;
        }

        void Splash(Vector3 at)
        {
            for (int i = 0; i < 14; i++)
                Effects.Instance?.Wisp(at, Deflected ? DeflectedColour : Green, Random.insideUnitSphere * 2.2f + Vector3.up, Random.Range(0.3f, 0.6f));
            Sfx.Instance?.SpitSplat(at);
            Destroy(gameObject);
        }

        // Does this step pass within `reach` of a point (in 3D: the spit arcs)?
        static bool Near(Vector3 from, Vector3 to, Vector3 point, float reach)
        {
            Vector3 ab = to - from;
            float t = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector3.Dot(point - from, ab) / ab.sqrMagnitude) : 0f;
            return Vector3.Distance(from + ab * t, point) < reach;
        }
    }
}
