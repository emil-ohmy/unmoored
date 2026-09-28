using UnityEngine;

namespace MyGame
{
    // A Laden archer's arrow: flies straight and level, glowing sickly green (with its own little light, so it can
    // be seen coming in the dark), trailing a faint streak. On the Apprentice: a dash's i-frames let it pass, a
    // perfect block nullifies it (it bursts into nothing), a normal block stops it at the usual cost, otherwise it
    // hits. It sticks where it strikes the world, and fades; out of range it simply fades away.
    public sealed class Arrow : MonoBehaviour
    {
        public static int Live { get; private set; }
        public static event System.Action<Arrow, Defence> Resolved; // what the Apprentice did about it (tests)

        public Vector3 Velocity => velocity;
        public bool Stuck => stuckAt >= 0f;

        const float Radius = 0.42f, MaxRange = 18f, StuckLife = 2.5f;

        Vector3 velocity, start;
        float damage, knockback, stuckAt = -1f;
        Apprentice target;
        bool passed; // dodged: it flies on past
        TrailRenderer trail;
        Light glow;

        public static Arrow Fire(Vector3 from, Vector3 direction, float speed, float damage, float knockback,
                                 Apprentice target, Material wood, Material tipGlow, Material trailMaterial)
        {
            direction.y = 0f;
            direction.Normalize();
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
            if (!passed && target != null && target.IsAlive && Reaches(from, to, target.transform.position))
            {
                var defence = target.TakeHit(damage, velocity.normalized * knockback);
                Resolved?.Invoke(this, defence);
                if (defence == Defence.Dodged)
                    passed = true; // through the i-frames: it flies on
                else
                {
                    if (defence == Defence.Parried)
                        Effects.Instance?.AlertFlare(to); // nullified: it bursts into nothing
                    Destroy(gameObject);
                    return;
                }
            }
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

        // Does this step pass within reach of the Apprentice (flat distance to the segment; it flies at chest height)?
        static bool Reaches(Vector3 from, Vector3 to, Vector3 who)
        {
            Vector2 a = new Vector2(from.x, from.z), b = new Vector2(to.x, to.z), p = new Vector2(who.x, who.z);
            Vector2 ab = b - a;
            float t = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
            return Vector2.Distance(a + ab * t, p) < Radius && Mathf.Abs(from.y - (who.y + 0.6f)) < 1.2f;
        }
    }
}
