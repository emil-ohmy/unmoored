using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    // The Stroke's wave: the oar pulls like the tide, and a wave of force rolls out ahead of the Apprentice in an arc,
    // low along the ground, edged in silver foam (the Drowned Moon's), spray thrown up along its crest. Every Soured its
    // front passes is struck once (Soured.TakeWave: a shove and a stagger, little harm, through any guard); it's spent
    // at its reach. From shallows it carries water (Wet): further, wider, harder, and it puts out what burns (a Soured
    // alight, burning pitch, a fireball), with a hiss of steam. Whatever else it meets (IWaveable) it moves or puts out
    // too. Drawn with the effects' additive soft dot, as bands of vertex colour lying flat.
    public sealed class StrokeWave : MonoBehaviour
    {
        public static int Live { get; private set; } // (tests)

        public float Radius { get; private set; }
        public float Reach { get; private set; }
        public IReadOnlyCollection<Soured> Struck => struck;
        public bool Wet { get; private set; }
        public Vector3 Origin => origin;
        public Vector3 Forward => forward;
        public int Doused { get; private set; } // (tests)
        public AudioClip[] douseSound;

        Vector3 origin, forward;
        float arc, speed, damage, push, stagger, fade = 1f;
        readonly HashSet<Soured> struck = new HashSet<Soured>();
        readonly HashSet<IWaveable> met = new HashSet<IWaveable>();
        Mesh crest, body;
        Light glow;
        bool spent;

        const int Segments = 28;

        public static StrokeWave Send(Vector3 origin, Vector3 forward, float reach, float arcDegrees, float speed,
                                      float damage, float push, float stagger, bool wet = false)
        {
            var go = new GameObject("Stroke Wave");
            go.transform.position = origin;
            var wave = go.AddComponent<StrokeWave>();
            forward.y = 0f;
            wave.origin = origin;
            wave.forward = forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
            wave.Reach = reach;
            wave.arc = arcDegrees;
            wave.speed = speed;
            wave.damage = damage;
            wave.push = push;
            wave.stagger = stagger;
            wave.Radius = 0.5f;
            wave.Wet = wet;
            Material dot = Effects.Instance != null ? Effects.Instance.sparkMaterial : null;
            wave.crest = wave.Band("Foam", dot, 0.06f);
            wave.body = wave.Band("Water", dot, 0.04f);
            wave.glow = new GameObject("Wave Light").AddComponent<Light>();
            wave.glow.transform.SetParent(go.transform, false);
            wave.glow.type = LightType.Point;
            wave.glow.color = DrownedMoon.Moonlight;
            wave.glow.range = 4f;
            wave.glow.shadows = LightShadows.None;
            wave.Draw();
            return wave;
        }

        Mesh Band(string name, Material material, float lift)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.up * lift;
            var mesh = new Mesh { name = name };
            mesh.MarkDynamic();
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return mesh;
        }

        // An arc of the ring between `inner` and `outer` (round `forward`, `arc` across), the soft dot's profile across
        // its width (soft at both edges), `colour` fading towards the arc's ends.
        void Fill(Mesh mesh, float inner, float outer, Color colour)
        {
            var verts = new Vector3[(Segments + 1) * 2];
            var uv = new Vector2[verts.Length];
            var colours = new Color[verts.Length];
            var tris = new int[Segments * 6];
            float yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            for (int k = 0; k <= Segments; k++)
            {
                float u = k / (float)Segments;
                float a = (yaw + Mathf.Lerp(-arc * 0.5f, arc * 0.5f, u)) * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                verts[k * 2] = dir * Mathf.Max(0f, inner);
                verts[k * 2 + 1] = dir * outer;
                uv[k * 2] = new Vector2(0.5f, 0.02f);
                uv[k * 2 + 1] = new Vector2(0.5f, 0.98f);
                var c = colour;
                c.a *= Mathf.Sin(u * Mathf.PI) * 0.75f + 0.25f; // (thinning towards its ends)
                colours[k * 2] = colours[k * 2 + 1] = c;
            }
            for (int k = 0; k < Segments; k++)
            {
                int o = k * 2;
                tris[k * 6] = o; tris[k * 6 + 1] = o + 1; tris[k * 6 + 2] = o + 2;
                tris[k * 6 + 3] = o + 1; tris[k * 6 + 4] = o + 3; tris[k * 6 + 5] = o + 2;
            }
            mesh.Clear();
            mesh.vertices = verts;
            mesh.uv = uv;
            mesh.colors = colours;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
        }

        void Draw()
        {
            float u = Mathf.Clamp01(Radius / Reach);
            // The crest (foam: silver-white, bright) and the water behind it (a cold, fainter band, widening).
            Fill(crest, Radius - 0.45f, Radius + 0.1f, new Color(0.88f, 0.95f, 1f, 0.95f * fade));
            Fill(body, Radius - 0.6f - (Wet ? 2.4f : 1.4f) * u, Radius - 0.25f,
                 new Color(0.4f, 0.58f, 0.9f, (Wet ? 0.65f : 0.4f) * fade * (1f - 0.5f * u))); // (carrying water: a deeper body behind)
            glow.transform.position = origin + forward * Radius + Vector3.up * 0.5f;
            glow.intensity = 2.2f * fade;
        }

        void OnEnable() => Live++;
        void OnDisable() => Live--;

        void Update()
        {
            float dt = Time.deltaTime;
            if (!spent)
            {
                Radius = Mathf.Min(Reach, Radius + speed * dt);
                Strike();
                Spray(dt);
                if (Radius >= Reach)
                    spent = true;
            }
            else
            {
                fade = Mathf.MoveTowards(fade, 0f, dt / 0.3f);
                Radius += speed * 0.25f * dt; // (spreading out as it sinks)
                if (fade <= 0f)
                {
                    Destroy(gameObject);
                    return;
                }
            }
            Draw();
        }

        // Every Soured its front reaches, inside the arc and about its level: once each.
        void Strike()
        {
            foreach (var s in new List<Soured>(WorldState.AllSoured)) // (one may fall as it's struck)
            {
                if (s == null || !s.IsAlive || struck.Contains(s))
                    continue;
                Vector3 to = s.transform.position - origin;
                if (Mathf.Abs(to.y) > 1.6f)
                    continue;
                to.y = 0f;
                if (to.magnitude > Radius + s.radius || Vector3.Angle(forward, to) > arc * 0.5f + 8f)
                    continue;
                struck.Add(s);
                Vector3 away = to.sqrMagnitude > 0.01f ? to.normalized : forward;
                if (s.TakeWave(away * push, damage, origin, stagger) == HitResult.Landed)
                    Effects.Instance?.Splash(s.transform.position + Vector3.up * 0.3f);
                if (Wet && s.TryGetComponent(out Burning burning) && burning.Alight)
                {
                    burning.Douse();
                    Steam(s.transform.position + Vector3.up * 1f);
                }
            }
            foreach (var w in new List<IWaveable>(Waveables.All))
            {
                if (w == null || met.Contains(w))
                    continue;
                Vector3 to = w.WavePoint - origin;
                if (Mathf.Abs(to.y) > 2f)
                    continue;
                to.y = 0f;
                bool on = w is IStoodOnWaveable && (to + forward * 0.5f).magnitude <= w.WaveRadius; // (sent from on it)
                if (!on && (to.magnitude > Radius + w.WaveRadius || Vector3.Angle(forward, to) > arc * 0.5f + 8f))
                    continue;
                met.Add(w);
                w.TakeWave(this, (to.sqrMagnitude > 0.01f ? to.normalized : forward) * push);
            }
        }

        // Something put out by the water it carries: a hiss and a puff of steam.
        public void Steam(Vector3 at)
        {
            Doused++;
            var fx = Effects.Instance;
            if (fx != null)
                for (int i = 0; i < 14; i++)
                    fx.Wisp(at + Random.insideUnitSphere * 0.4f, new Color(0.8f, 0.82f, 0.85f, 0.8f),
                            new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(1.2f, 2.4f), Random.Range(-0.4f, 0.4f)), Random.Range(0.8f, 1.6f));
            Sfx.Instance?.Attack(douseSound, at, 0.9f, "douse");
        }

        // Spray thrown up along the crest: silver droplets, a mote of moonlight now and then.
        void Spray(float dt)
        {
            var fx = Effects.Instance;
            if (fx == null)
                return;
            int n = Mathf.CeilToInt((Wet ? 70f : 40f) * dt);
            for (int i = 0; i < n; i++)
            {
                float a = Random.Range(-arc * 0.5f, arc * 0.5f);
                var dir = Quaternion.Euler(0f, a, 0f) * forward;
                Vector3 at = origin + dir * Radius + Vector3.up * 0.15f;
                fx.Streak(at, dir * speed * 0.35f + Vector3.up * Random.Range(1.5f, 3f), new Color(0.85f, 0.93f, 1f, 1f), Random.Range(0.2f, 0.35f), Random.Range(0.05f, 0.1f));
                if (Random.value < 0.25f)
                    fx.Wisp(at, DrownedMoon.Moonlight);
            }
        }
    }
}
