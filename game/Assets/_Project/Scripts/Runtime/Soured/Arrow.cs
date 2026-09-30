using UnityEngine;

namespace MyGame
{
    // A Laden archer's arrow: flies straight and level, glowing sickly green (with its own little light, so it can
    // be seen coming in the dark), trailing a faint streak. On the Apprentice: a dash's i-frames let it pass, a
    // normal block stops it at the usual cost, otherwise it hits. A perfect block deflects it: it turns the
    // Apprentice's cold pale blue and flies back at its archer, faster, and a Soured it strikes takes the hit harder
    // and is stunned (open to a riposte). It sticks where it strikes the world, and fades; out of range it fades away.
    public sealed class Arrow : MonoBehaviour
    {
        public static int Live { get; private set; }
        public static event System.Action<Arrow, Defence> Resolved; // what the Apprentice did about it (tests)

        public Vector3 Velocity => velocity;
        public bool Stuck => stuckAt >= 0f;
        public bool Deflected { get; private set; }
        public static event System.Action<Arrow, Soured> StruckSoured; // a deflected arrow found its mark (tests)

        const float DeflectSpeedFactor = 1.4f, DeflectDamageFactor = 1.5f;
        static readonly Color DeflectedColour = new Color(0.62f, 0.77f, 1f);

        const float Radius = 0.42f, MaxRange = 18f, StuckLife = 2.5f;

        Vector3 velocity, start;
        float damage, knockback, stuckAt = -1f;
        Apprentice target;
        Soured owner;
        bool passed; // dodged: it flies on past
        TrailRenderer trail;
        Light glow;

        public static Arrow Fire(Vector3 from, Vector3 direction, float speed, float damage, float knockback,
                                 Apprentice target, Soured owner, Material wood, Material tipGlow, Material trailMaterial)
        {
            direction.Normalize(); // (up or down at someone on another level: the archer aims it)
            var go = new GameObject("Arrow");
            go.transform.SetPositionAndRotation(from, Quaternion.LookRotation(direction));
            Part(go.transform, wood, new Vector3(0f, 0f, -0.1f), new Vector3(0.025f, 0.025f, 0.7f));
            Part(go.transform, tipGlow, new Vector3(0f, 0f, 0.28f), new Vector3(0.06f, 0.06f, 0.1f));
            Part(go.transform, tipGlow, new Vector3(0f, 0f, -0.42f), new Vector3(0.09f, 0.01f, 0.08f)); // fletching

            var arrow = go.AddComponent<Arrow>();
            arrow.velocity = direction * speed;
            arrow.start = from;
            arrow.damage = damage;
            arrow.knockback = knockback;
            arrow.target = target;
            arrow.owner = owner;

            arrow.trail = go.AddComponent<TrailRenderer>();
            arrow.trail.sharedMaterial = trailMaterial;
            arrow.trail.time = 0.18f;
            arrow.trail.widthMultiplier = 0.07f;
            arrow.trail.minVertexDistance = 0.1f;
            arrow.trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(new Color(0.75f, 1f, 0.5f), 0f), new GradientColorKey(new Color(0.5f, 0.8f, 0.3f), 1f) },
                         new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) });
            arrow.trail.colorGradient = fade;

            arrow.glow = new GameObject("Arrow Light").AddComponent<Light>();
            arrow.glow.transform.SetParent(go.transform, false);
            arrow.glow.transform.localPosition = new Vector3(0f, 0f, 0.25f);
            arrow.glow.type = LightType.Point;
            arrow.glow.color = new Color(0.62f, 0.91f, 0.38f);
            arrow.glow.range = 2.2f;
            arrow.glow.intensity = 1.4f;
            arrow.glow.shadows = LightShadows.None;
            return arrow;
        }

        static void Part(Transform parent, Material material, Vector3 position, Vector3 scale)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            var renderer = part.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void OnEnable() => Live++;
        void OnDisable() => Live--;

        void Update()
        {
            float dt = Time.deltaTime;
            if (Stuck)
            {
                float age = Time.time - stuckAt;
                glow.intensity = Mathf.Max(0f, 0.8f * (1f - age / StuckLife));
                if (age >= StuckLife)
                    Destroy(gameObject);
                return;
            }

            Vector3 from = transform.position, step = velocity * dt, to = from + step;
            if (!passed && target != null && target.IsAlive && Reaches(from, to, target.transform.position, Radius))
            {
                var defence = target.TakeHit(damage, velocity.normalized * knockback);
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
                    Destroy(gameObject);
                    return;
                }
            }
            if (Deflected && StrikeSoured(from, to))
                return;
            // The world: it sticks where it strikes (characters don't stop it; the Apprentice was handled above).
            var hits = Physics.RaycastAll(from, step.normalized, step.magnitude, ~0, QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            RaycastHit? stop = null;
            foreach (var hit in hits)
                if (!(hit.collider is CharacterController) && hit.distance < nearest)
                {
                    nearest = hit.distance;
                    stop = hit;
                }
            if (stop.HasValue)
            {
                transform.position = stop.Value.point - step.normalized * 0.2f; // half the shaft in
                stuckAt = Time.time;
                trail.emitting = false;
                Sfx.Instance?.ArrowThunk(stop.Value.point);
                return;
            }
            transform.position = to;
            if (Vector3.Distance(start, to) > MaxRange)
                Destroy(gameObject);
        }

        // Turned back by a perfect block: at its archer (where it stands now), faster, in the Apprentice's colour.
        void Deflect(Vector3 at)
        {
            Deflected = passed = true;
            Vector3 back = owner != null && owner.IsAlive ? owner.transform.position + Vector3.up * 1.0f - at : -velocity;
            velocity = back.normalized * velocity.magnitude * DeflectSpeedFactor;
            damage *= DeflectDamageFactor;
            start = at;
            transform.rotation = Quaternion.LookRotation(velocity);
            glow.color = DeflectedColour;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(new Color(0.85f, 0.92f, 1f), 0f), new GradientColorKey(DeflectedColour, 1f) },
                         new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = fade;
            trail.Clear();
        }

        // A deflected arrow strikes the first Soured in its path: a harder hit, and it's stunned (unless a guard
        // turns it: then it just clangs off).
        bool StrikeSoured(Vector3 from, Vector3 to)
        {
            foreach (var soured in WorldState.AllSoured)
            {
                if (soured == null || !soured.IsAlive || !soured.isActiveAndEnabled || !Reaches(from, to, soured.transform.position, soured.radius + 0.25f))
                    continue;
                var result = soured.TakeHit(new Hit
                {
                    Damage = damage, Stagger = 30f, Knockback = velocity.normalized * 3f, From = from - velocity.normalized, Heavy = false,
                });
                if (result == HitResult.Landed && soured.IsAlive)
                    soured.Stun();
                if (result != HitResult.Missed)
                {
                    StruckSoured?.Invoke(this, soured);
                    Effects.Instance?.BlockSpark(to, -velocity.normalized);
                    Destroy(gameObject);
                    return true;
                }
            }
            return false;
        }

        // Does this step pass within `reach` of someone (flat distance to the segment; it flies at chest height)?
        static bool Reaches(Vector3 from, Vector3 to, Vector3 who, float reach)
        {
            Vector2 a = new Vector2(from.x, from.z), b = new Vector2(to.x, to.z), p = new Vector2(who.x, who.z);
            Vector2 ab = b - a;
            float t = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
            return Vector2.Distance(a + ab * t, p) < reach && Mathf.Abs(from.y - (who.y + 0.6f)) < 1.2f;
        }
    }
}
