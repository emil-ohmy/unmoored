using UnityEngine;

namespace MyGame
{
    // The Anchoress's Undertow: sour black water welling up from the floor around her while she prays. It spreads out
    // to its full size, churns (bubbles, a sickly green edge you can read), and recedes when she rises. Inside it the
    // Apprentice is slowed and dragged towards her, and hurt a little every tick (no stagger: you can always wade or
    // dash out). A dash through it slips the ticks.
    public sealed class UndertowPool : MonoBehaviour
    {
        const int Rings = 6, Segments = 48;
        static readonly Color Edge = new Color(0.38f, 0.8f, 0.24f, 0.5f); // (additive: kept dim, or it burns white)
        static readonly Color Bubble = new Color(0.66f, 0.92f, 0.46f, 1f);

        public float Radius { get; private set; }
        public bool Receding { get; private set; }
        public int Ticks { get; private set; } // times it hurt the Apprentice

        Transform centre;
        Apprentice target;
        float maxRadius, spreadTime, tickInterval, tickDamage, wade, drag;
        float bornAt, recedeFrom, recedeAt, nextTick, nextBubble;
        Transform disc, rim;
        Mesh rimMesh, discMesh;
        Vector3[] rimVertices;
        Color[] rimColours;

        public static UndertowPool Well(Transform centre, Apprentice target, Material water, float radius, float spreadTime,
                                        float tickInterval, float tickDamage, float wade, float drag)
        {
            var pool = new GameObject("Undertow").AddComponent<UndertowPool>();
            pool.transform.position = Floor(centre);
            pool.centre = centre;
            pool.target = target;
            pool.maxRadius = radius;
            pool.spreadTime = spreadTime;
            pool.tickInterval = tickInterval;
            pool.tickDamage = tickDamage;
            pool.wade = wade;
            pool.drag = drag;
            pool.bornAt = Time.time;
            pool.nextTick = Time.time + tickInterval * 0.5f;
            pool.Build(water);
            return pool;
        }

        public void Recede(float time)
        {
            if (Receding)
                return;
            Receding = true;
            recedeFrom = Radius;
            recedeAt = Time.time;
            spreadTime = Mathf.Max(0.05f, time);
        }

        void Build(Material water)
        {
            // The water: a subdivided disc (so the shader's waves and ripples move it), just above the floor.
            disc = new GameObject("Water", typeof(MeshFilter), typeof(MeshRenderer)).transform;
            disc.SetParent(transform, false);
            disc.localPosition = Vector3.up * 0.03f;
            disc.GetComponent<MeshFilter>().sharedMesh = discMesh = DiscMesh();
            var renderer = disc.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = water;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // Its edge: a sickly green band, pulsing, so you can see where it ends.
            rim = new GameObject("Edge", typeof(MeshFilter), typeof(MeshRenderer)).transform;
            rim.SetParent(transform, false);
            rim.localPosition = Vector3.up * 0.06f;
            rimMesh = RimMesh();
            rim.GetComponent<MeshFilter>().sharedMesh = rimMesh;
            var rimRenderer = rim.GetComponent<MeshRenderer>();
            rimRenderer.sharedMaterial = Effects.Instance != null ? Effects.Instance.sparkMaterial : null;
            rimRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Apply(0.01f);
        }

        void Update()
        {
            float t = Time.time;
            if (!Receding)
                Radius = maxRadius * UiKit.Smooth(0f, 1f, (t - bornAt) / spreadTime);
            else
            {
                Radius = recedeFrom * (1f - UiKit.Smooth(0f, 1f, (t - recedeAt) / spreadTime));
                if (Radius <= 0.02f)
                {
                    Destroy(gameObject);
                    return;
                }
            }
            if (centre != null)
                transform.position = Floor(centre);
            Apply(Mathf.Max(0.01f, Radius));

            // Churning: bubbles breaking all over it.
            if (t >= nextBubble && Radius > 0.3f)
            {
                nextBubble = t + 0.04f;
                Vector2 p = Random.insideUnitCircle * Radius * 0.95f;
                Effects.Instance?.Wisp(transform.position + new Vector3(p.x, 0.08f, p.y), Bubble);
            }

            if (target == null || !target.IsAlive || Receding)
                return;
            Vector3 toHer = transform.position - target.transform.position;
            toHer.y = 0f;
            if (toHer.magnitude > Radius || Mathf.Abs(target.transform.position.y - transform.position.y) > 0.8f)
                return;
            // In it: dragged towards her, slowed, and hurt a little every tick.
            target.Wade(wade, toHer.sqrMagnitude > 0.01f ? toHer.normalized * drag : Vector3.zero);
            if (t >= nextTick)
            {
                nextTick = t + tickInterval;
                if (target.TakeTickDamage(tickDamage))
                    Ticks++;
            }
        }

        void Apply(float radius)
        {
            disc.localScale = new Vector3(radius, 1f, radius);
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * 7f);
            float half = 0.22f;
            for (int i = 0; i < Segments; i++)
            {
                float a = i * Mathf.PI * 2f / Segments;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                float wobble = 0.05f * Mathf.Sin(a * 5f + Time.time * 3f);
                rimVertices[i * 3] = dir * Mathf.Max(0f, radius - half + wobble);
                rimVertices[i * 3 + 1] = dir * (radius + wobble);
                rimVertices[i * 3 + 2] = dir * (radius + half * 0.4f + wobble);
                rimColours[i * 3] = Edge.WithAlpha(0f);
                rimColours[i * 3 + 1] = Edge.WithAlpha(Edge.a * pulse * (Receding ? 0.5f : 1f));
                rimColours[i * 3 + 2] = Edge.WithAlpha(0f);
            }
            rimMesh.vertices = rimVertices;
            rimMesh.colors = rimColours;
            rimMesh.RecalculateBounds();
        }

        // A unit disc in rings, so it has vertices to wave.
        static Mesh DiscMesh()
        {
            var vertices = new Vector3[1 + Rings * Segments];
            var triangles = new System.Collections.Generic.List<int>();
            for (int r = 1; r <= Rings; r++)
                for (int i = 0; i < Segments; i++)
                {
                    float a = i * Mathf.PI * 2f / Segments;
                    vertices[1 + (r - 1) * Segments + i] = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * ((float)r / Rings);
                }
            for (int i = 0; i < Segments; i++)
            {
                int j = (i + 1) % Segments;
                triangles.AddRange(new[] { 0, 1 + j, 1 + i });
                for (int r = 1; r < Rings; r++)
                {
                    int a = 1 + (r - 1) * Segments + i, b = 1 + (r - 1) * Segments + j, c = a + Segments, d = b + Segments;
                    triangles.AddRange(new[] { a, b, c, b, d, c });
                }
            }
            var mesh = new Mesh { name = "Undertow", vertices = vertices, triangles = triangles.ToArray() };
            var normals = new Vector3[vertices.Length];
            for (int i = 0; i < normals.Length; i++)
                normals[i] = Vector3.up;
            mesh.normals = normals;
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(2f, 0.5f, 2f));
            return mesh;
        }

        Mesh RimMesh()
        {
            var mesh = new Mesh { name = "Undertow Edge" };
            rimVertices = new Vector3[Segments * 3];
            rimColours = new Color[rimVertices.Length];
            var uv = new Vector2[rimVertices.Length];
            for (int i = 0; i < uv.Length; i++)
                uv[i] = new Vector2(0.5f, 0.5f);
            var triangles = new int[Segments * 12];
            for (int i = 0, t = 0; i < Segments; i++)
            {
                int j = (i + 1) % Segments;
                for (int band = 0; band < 2; band++)
                {
                    int a = i * 3 + band, b = j * 3 + band;
                    triangles[t++] = a; triangles[t++] = b; triangles[t++] = a + 1;
                    triangles[t++] = a + 1; triangles[t++] = b; triangles[t++] = b + 1;
                }
            }
            mesh.vertices = rimVertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.MarkDynamic();
            return mesh;
        }

        static Vector3 Floor(Transform centre) => centre.position; // (her feet)

        void OnDestroy()
        {
            if (rimMesh != null)
                Destroy(rimMesh);
            if (discMesh != null)
                Destroy(discMesh);
        }
    }
}
