using UnityEngine;

namespace MyGame
{
    // A ring of light closing in on a point (Father Moth charging his blast): from wide, faint, to tight and bright, then
    // gone into him. One after another, faster and faster as the charge builds.
    public sealed class ChargeRing : MonoBehaviour
    {
        const int Segments = 48;

        float from, time, age, width;
        Color tint;
        Mesh mesh;
        Vector3[] vertices;
        Color[] colours;

        public static ChargeRing Close(Vector3 centre, float from, float time, float width, Color tint)
        {
            var go = new GameObject("Charge Ring", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.position = centre;
            var ring = go.AddComponent<ChargeRing>();
            ring.from = from;
            ring.time = time;
            ring.width = width;
            ring.tint = tint;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = Effects.Instance != null ? Effects.Instance.sparkMaterial : null;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.Build();
            return ring;
        }

        void Build()
        {
            mesh = new Mesh { name = "Charge Ring" };
            vertices = new Vector3[Segments * 3];
            colours = new Color[vertices.Length];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[Segments * 24];
            for (int i = 0, t = 0; i < Segments; i++)
            {
                int j = (i + 1) % Segments;
                for (int band = 0; band < 2; band++)
                {
                    int a = i * 3 + band, b = j * 3 + band;
                    triangles[t++] = a; triangles[t++] = b; triangles[t++] = a + 1;
                    triangles[t++] = a + 1; triangles[t++] = b; triangles[t++] = b + 1;
                    triangles[t++] = a; triangles[t++] = a + 1; triangles[t++] = b; // (both faces)
                    triangles[t++] = a + 1; triangles[t++] = b + 1; triangles[t++] = b;
                }
            }
            for (int i = 0; i < uv.Length; i++)
                uv[i] = new Vector2(0.5f, 0.5f);
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = mesh;
            Refresh(0f);
        }

        void Update()
        {
            age += Time.deltaTime;
            float u = age / time;
            if (u >= 1f)
            {
                Destroy(gameObject);
                return;
            }
            Refresh(u);
        }

        void Refresh(float u)
        {
            float radius = Mathf.Lerp(from, 0.2f, u * u); // (closing faster and faster)
            float half = width * 0.5f * Mathf.Lerp(1f, 0.4f, u);
            float alpha = tint.a * Mathf.Sin(u * Mathf.PI * 0.5f + 0.15f);
            for (int i = 0; i < Segments; i++)
            {
                float a = i * Mathf.PI * 2f / Segments;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                vertices[i * 3] = dir * Mathf.Max(0f, radius - half);
                vertices[i * 3 + 1] = dir * radius;
                vertices[i * 3 + 2] = dir * (radius + half);
                colours[i * 3] = new Color(tint.r, tint.g, tint.b, 0f);
                colours[i * 3 + 1] = new Color(tint.r, tint.g, tint.b, alpha);
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
