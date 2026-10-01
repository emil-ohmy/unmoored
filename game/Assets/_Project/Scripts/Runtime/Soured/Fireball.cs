using UnityEngine;

namespace MyGame
{
    // The Watchman's fire, breathed out of his lantern: a ball of flame flying flat and straight at the Apprentice,
    // licking flames behind it. A dash's i-frames let it pass, and so does a sidestep. A block stops it (a little
    // gets through); a perfect block turns it back, pale, at him: and he's straw (Watchman.Scorch). The small quick
    // ones (`parryable` false) no guard stops: only a dodge or a sidestep. It gutters out where it strikes the world.
    public sealed class Fireball : MonoBehaviour
    {
        public static int Live { get; private set; }
        public static event System.Action<Fireball, Defence> Resolved; // what the Apprentice did about it (tests)

        public const float MaxRange = 18f;
        public float damage = 18f, radius = 0.4f;
        public bool parryable = true;
        public bool Deflected { get; private set; }
        public Soured Owner => owner;

        static readonly Color Flame = new Color(1f, 0.55f, 0.2f), DeflectedColour = new Color(0.75f, 0.85f, 1f);

        Vector3 velocity, start;
        Apprentice target;
        Soured owner;
        bool passed;
        Light glow;
        Renderer body;
        float size, emitDebt;

        // Breathed from `from` along `direction` (flat) at `speed`.
        public static Fireball Fire(Vector3 from, Vector3 direction, float speed, float size, Apprentice target, Soured owner, Material glob)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Fireball";
            Destroy(go.GetComponent<Collider>());
            go.transform.position = from;
            var ball = go.AddComponent<Fireball>();
            ball.size = size;
            go.transform.localScale = Vector3.one * size;
            ball.body = go.GetComponent<MeshRenderer>();
            if (glob != null)
                ball.body.sharedMaterial = glob;
            ball.body.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ball.Tint(new Color(1f, 0.6f, 0.25f), new Color(1.4f, 0.6f, 0.15f));
            direction.y = 0f;
            ball.velocity = direction.normalized * speed;
            ball.start = from;
            ball.target = target;
            ball.owner = owner;
            ball.radius = Mathf.Max(0.3f, size * 1.1f);
            ball.glow = new GameObject("Fireball Light").AddComponent<Light>();
            ball.glow.transform.SetParent(go.transform, false);
            ball.glow.type = LightType.Point;
            ball.glow.color = Flame;
            ball.glow.range = 3.5f + size * 4f;
            ball.glow.intensity = 2.2f;
            ball.glow.shadows = LightShadows.None;
            return ball;
        }

        void Tint(Color colour, Color emission)
        {
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", colour);
            block.SetColor("_EmissionColor", emission);
            body.SetPropertyBlock(block);
        }

        void OnEnable() => Live++;
        void OnDisable() => Live--;

        void Update()
        {
            float dt = Time.deltaTime;
            Vector3 from = transform.position, step = velocity * dt, to = from + step;
            transform.localScale = Vector3.one * size * (1f + 0.12f * Mathf.Sin(Time.time * 40f)); // (it roils)
            glow.intensity = 2.2f + 0.6f * Mathf.PerlinNoise(Time.time * 12f, 0.4f);
            emitDebt += dt * 60f;
            for (; emitDebt >= 1f; emitDebt -= 1f) // (flames streaming off behind it)
                Effects.Instance?.Flame(from + Random.insideUnitSphere * size * 0.4f, -velocity * 0.15f + Vector3.up * 0.8f,
                    Random.Range(0.2f, 0.35f), size * Random.Range(0.7f, 1.1f));
            if (!passed && !Deflected && target != null && target.IsAlive && Near(from, to, target.transform.position + Vector3.up * 0.7f, radius + 0.3f))
            {
                var defence = target.TakeHit(damage, velocity.normalized * 3f, owner, parryable);
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
                    Gutter(from);
                    return;
                }
            }
            if (Deflected && StrikeSoured(from, to))
                return;
            foreach (var hit in Physics.RaycastAll(from, step.normalized, step.magnitude, ~0, QueryTriggerInteraction.Ignore))
                if (!(hit.collider is CharacterController))
                {
                    Gutter(hit.point);
                    return;
                }
            transform.position = to;
            if (Vector3.Distance(start, to) > MaxRange)
                Gutter(to);
        }

        // Turned back by a perfect block: straight at the one who breathed it (where he is now), faster, pale.
        void Deflect(Vector3 at)
        {
            Deflected = passed = true;
            Vector3 aim = owner != null && owner.IsAlive ? owner.transform.position + Vector3.up * 1.1f : at - velocity;
            velocity = (aim - at).normalized * Mathf.Max(13f, velocity.magnitude * 1.6f);
            start = at;
            glow.color = DeflectedColour;
            Tint(DeflectedColour, DeflectedColour * 1.6f);
        }

        // Turned back, it strikes the first Soured in its way: the Watchman is scorched (a third of him burns away);
        // anything else is set alight.
        bool StrikeSoured(Vector3 from, Vector3 to)
        {
            foreach (var soured in WorldState.AllSoured)
            {
                if (soured == null || !soured.IsAlive || !soured.isActiveAndEnabled
                    || !Near(from, to, soured.transform.position + Vector3.up * 1.0f, soured.radius + 0.45f))
                    continue;
                if (soured is Watchman w)
                    w.Scorch(from);
                else
                {
                    soured.TakeBurn(12f, from);
                    Burning.Set(soured, 2.5f, 5f);
                }
                Gutter(to);
                return true;
            }
            return false;
        }

        void Gutter(Vector3 at)
        {
            for (int i = 0; i < 18; i++)
                Effects.Instance?.Flame(at + Random.insideUnitSphere * 0.3f, Random.insideUnitSphere * 2f + Vector3.up * 1.5f, Random.Range(0.25f, 0.5f), size * Random.Range(0.6f, 1f));
            Sfx.Instance?.Attack((owner as Watchman)?.gutterSound, at, 0.8f, "fireball_gutter");
            Destroy(gameObject);
        }

        static bool Near(Vector3 from, Vector3 to, Vector3 point, float reach)
        {
            Vector3 ab = to - from;
            float t = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector3.Dot(point - from, ab) / ab.sqrMagnitude) : 0f;
            return Vector3.Distance(from + ab * t, point) < reach;
        }
    }
}
