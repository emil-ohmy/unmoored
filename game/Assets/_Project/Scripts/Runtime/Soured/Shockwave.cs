using UnityEngine;

namespace MyGame
{
    // A ring of water thrown out across the floor by a blow that hits the ground (the Anchoress's Anchor Drop). It
    // spreads out and fades; where its crest passes the Apprentice it knocks them outwards, unless they dash through it
    // (the i-frames) or are already beyond its reach. It can be blocked (facing where it came from) but not
    // perfect-blocked back: it isn't her.
    public sealed class Shockwave : MonoBehaviour
    {
        const int Segments = 64;

        public float Radius { get; private set; }
        public bool Struck { get; private set; }

        Apprentice target;
        float maxRadius, speed, width, damage, knockback;
        Mesh mesh;
        Vector3[] vertices;
        Color[] colours;
        Color tint;

        public static Shockwave Spread(Vector3 centre, Apprentice target, float maxRadius, float speed, float width, float damage,
                                       float knockback, Color tint)
        {
            var go = new GameObject("Shockwave", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.position = centre + Vector3.up * 0.06f;
            var wave = go.AddComponent<Shockwave>();
            wave.target = target;
            wave.maxRadius = maxRadius;
            wave.speed = speed;
            wave.width = width;
            wave.damage = damage;
            wave.knockback = knockback;
            wave.tint = tint;
            wave.Radius = 0.3f;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = Effects.Instance != null ? Effects.Instance.sparkMaterial : null;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            wave.Build();
            return wave;
        }

        // Three rings of vertices: a clear inner edge, the bright crest, a clear outer edge (the texture's solid middle).
        void Build()
        {
            mesh = new Mesh { name = "Shockwave" };
            vertices = new Vector3[Segments * 3];
            colours = new Color[vertices.Length];
            var uv = new Vector2[vertices.Length];
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
            for (int i = 0; i < uv.Length; i++)
                uv[i] = new Vector2(0.5f, 0.5f);
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = mesh;
            Refresh();
        }

        void Update()
        {
            Radius += speed * Time.deltaTime;
            if (Radius >= maxRadius)
            {
                Destroy(gameObject);
                return;
            }
            if (!Struck && target != null && target.IsAlive)
            {
                Vector3 to = target.transform.position - transform.position;
                float height = to.y;
                to.y = 0f;
                if (Mathf.Abs(to.magnitude - Radius) <= width * 0.5f && height < 0.8f)
                {
                    Struck = true; // once: the crest passes on
                    Vector3 outwards = to.sqrMagnitude > 0.001f ? to.normalized : Vector3.forward;
                    target.TakeHit(damage, outwards * knockback);
                }
            }
            Refresh();
        }

        void Refresh()
        {
            float fade = 1f - Mathf.Pow(Radius / maxRadius, 2f);
            float half = width * 0.5f;
            for (int i = 0; i < Segments; i++)
            {
                float a = i * Mathf.PI * 2f / Segments;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                vertices[i * 3] = dir * Mathf.Max(0f, Radius - half);
                vertices[i * 3 + 1] = dir * Radius;
                vertices[i * 3 + 2] = dir * (Radius + half * 0.6f); // a sharper front edge
                colours[i * 3] = new Color(tint.r, tint.g, tint.b, 0f);
                colours[i * 3 + 1] = new Color(tint.r, tint.g, tint.b, tint.a * fade);
                colours[i * 3 + 2] = new Color(tint.r, tint.g, tint.b, 0f);
            }
            mesh.vertices = vertices;
            mesh.colors = colours;
            mesh.RecalculateBounds();
        }

        void OnDestroy()
        {
            if (mesh != null)
                Destroy(mesh);
        }
    }
}
