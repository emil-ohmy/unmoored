using UnityEngine;

namespace MyGame
{
    // The Watchman's fire, breathed out of his lantern: a ball of flame flying flat and straight at the Apprentice,
    // licking flames behind it. A dash's i-frames let it pass, and so does a sidestep. A block stops it (a little
    // gets through); a perfect block turns it back, pale, straight at him (through anything between: it's his):
    // and he's straw (Watchman.Scorch). A small one (`glances`) a perfect block only knocks aside, glancing off to
    // one side to gutter out harmlessly. `parryable` false: no guard stops it. It gutters out where it strikes the world.
    public sealed class Fireball : MonoBehaviour, IWaveable
    {
        // The Stroke's wave carrying water puts it out where it meets it.
        public Vector3 WavePoint => transform.position;
        public float WaveRadius => radius;
        public void TakeWave(StrokeWave wave, Vector3 push)
        {
            if (!wave.Wet)
                return;
            wave.Steam(transform.position);
            Finish(Defence.Dodged);
            Destroy(gameObject);
        }

        public static int Live { get; private set; }
        public static event System.Action<Fireball, Defence> Resolved; // what the Apprentice did about it (tests)

        public const float MaxRange = 18f;
        public float damage = 18f, radius = 0.4f;
        public bool parryable = true;
        public bool glances;                  // perfect-blocked, it's knocked aside (not back at him)
        public System.Action<Fireball, Defence> Done;
        public AudioClip[] gutterSound;       // where it strikes // what became of it, once (Dodged: it missed, or passed through a dash)
        public bool Deflected { get; private set; }
        public Soured Owner => owner;

        static readonly Color Flame = new Color(1f, 0.55f, 0.2f), DeflectedColour = new Color(0.75f, 0.85f, 1f);

        Vector3 velocity, start;
        Apprentice target;
        Soured owner;
        bool passed, done;
        Light glow;
        Renderer body;
        float size, emitDebt;

        // Breathed from `from` along `direction` (down to their chest as it comes, if it starts higher) at `speed`.
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

        void OnEnable() { Live++; Waveables.Register(this); }
        void OnDisable() { Live--; Waveables.Unregister(this); }

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
                Finish(defence);
                if (defence == Defence.Parried)
                {
                    if (glances)
                        Glance(from);
                    else
                        Deflect(from);
                    return;
                }
                if (defence == Defence.Dodged)
                    passed = true; // through the i-frames: it flies on
                else
                {
                    if (defence == Defence.Taken)
                        target.SetAlight(); // (taken full: you're alight)
                    Gutter(from);
                    return;
                }
            }
            if (Deflected && !glances && Homing(dt, from) && StrikeSoured(from, to))
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

        void Finish(Defence defence)
        {
            if (done)
                return;
            done = true;
            Done?.Invoke(this, defence);
        }

        // Knocked aside by a perfect block: off to one side and a little back, pale, to gutter out on its own.
        void Glance(Vector3 at)
        {
            Deflected = passed = true;
            Vector3 back = -velocity.normalized;
            Vector3 side = Vector3.Cross(Vector3.up, back) * (Random.value < 0.5f ? 1f : -1f);
            velocity = (side * 0.85f + back * 0.5f).normalized * 9f;
            start = at;
            glow.color = DeflectedColour;
            Tint(DeflectedColour, DeflectedColour * 1.6f);
        }

        // Turned back, it bends after him wherever he's got to.
        bool Homing(float dt, Vector3 from)
        {
            if (owner != null && owner.IsAlive)
            {
                Vector3 aim = owner.transform.position + Vector3.up * 1.1f - from;
                velocity = Vector3.RotateTowards(velocity, aim.normalized * velocity.magnitude, 6f * dt, 0f);
            }
            return true;
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

        // Turned back, it strikes the one who breathed it (passing through anything else: it's his): the Watchman is
        // scorched (a third of him burns away); any other is set alight.
        bool StrikeSoured(Vector3 from, Vector3 to)
        {
            var soured = owner;
            if (soured == null || !soured.IsAlive || !soured.isActiveAndEnabled
                || !Near(from, to, soured.transform.position + Vector3.up * 1.0f, soured.radius + 0.45f))
                return false;
            soured.StruckByOwnFire(from); // (the Watchman: scorched; a Gleaner: up at once)
            Gutter(to);
            return true;
        }

        void Gutter(Vector3 at)
        {
            Finish(Defence.Dodged); // (if nothing else became of it: it missed)
            for (int i = 0; i < 18; i++)
                Effects.Instance?.Flame(at + Random.insideUnitSphere * 0.3f, Random.insideUnitSphere * 2f + Vector3.up * 1.5f, Random.Range(0.25f, 0.5f), size * Random.Range(0.6f, 1f));
            Sfx.Instance?.Attack(gutterSound, at, 0.8f, "fireball_gutter");
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
