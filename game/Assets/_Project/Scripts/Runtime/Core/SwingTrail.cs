using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    // The smear behind a swing: while Emitting, it records where two points on the weapon are (its tip and its inner
    // edge) and draws the band they sweep through as a crescent that fades from the newest end to the oldest over
    // `lifetime`, stepped into hard-edged tones by the MyGame/Swoosh shader (a cel smear frame, not a blur). Samples
    // are smoothed (Catmull-Rom) so a fast swing still draws a clean arc. Its owner (Apprentice, Soured) sets Emitting
    // during the active part of each strike.
    public sealed class SwingTrail : MonoBehaviour
    {
        public Transform tip, inner;     // two points on the weapon (anchored to its bone)
        public Material material;        // MyGame/Swoosh
        public Color colour = Color.white;
        public float lifetime = 0.11f;

        public bool Emitting { get; set; }
        public int Samples => samples.Count;

        struct Sample { public Vector3 tip, inner; public float time; }

        const int Subdivisions = 4;
        readonly List<Sample> samples = new List<Sample>();
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<Color> colours = new List<Color>();
        readonly List<Vector2> uvs = new List<Vector2>();
        readonly List<int> triangles = new List<int>();
        Mesh mesh;
        MeshRenderer view;

        void Start()
        {
            var go = new GameObject(name + " Swoosh", typeof(MeshFilter), typeof(MeshRenderer)); // in world space
            mesh = new Mesh { name = "Swoosh" };
            mesh.MarkDynamic();
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            view = go.GetComponent<MeshRenderer>();
            view.sharedMaterial = material;
            view.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            view.receiveShadows = false;
        }

        void OnDestroy()
        {
            if (view != null)
                Destroy(view.gameObject);
        }

        void LateUpdate() // after animation has posed the weapon this frame
        {
            if (tip == null || inner == null || mesh == null)
                return;
            float now = Time.time;
            if (Emitting)
                samples.Add(new Sample { tip = tip.position, inner = inner.position, time = now });
            samples.RemoveAll(s => now - s.time > lifetime);
            Build(now);
        }

        void Build(float now)
        {
            mesh.Clear();
            view.enabled = samples.Count >= 2;
            if (!view.enabled)
                return;
            vertices.Clear();
            colours.Clear();
            uvs.Clear();
            triangles.Clear();
            for (int i = 0; i < samples.Count - 1; i++)
                for (int k = 0; k < Subdivisions; k++)
                {
                    float t = (float)k / Subdivisions;
                    Add(i, t, now);
                }
            Add(samples.Count - 2, 1f, now);
            for (int v = 0; v + 3 < vertices.Count; v += 2)
            {
                triangles.Add(v); triangles.Add(v + 1); triangles.Add(v + 2);
                triangles.Add(v + 2); triangles.Add(v + 1); triangles.Add(v + 3);
            }
            mesh.SetVertices(vertices);
            mesh.SetColors(colours);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
        }

        // One point along the smoothed curve between samples i and i+1, as a pair of vertices (inner edge, tip edge).
        void Add(int i, float t, float now)
        {
            Sample a = samples[Mathf.Max(0, i - 1)], b = samples[i], c = samples[i + 1], d = samples[Mathf.Min(samples.Count - 1, i + 2)];
            Vector3 tipPoint = CatmullRom(a.tip, b.tip, c.tip, d.tip, t);
            Vector3 innerPoint = CatmullRom(a.inner, b.inner, c.inner, d.inner, t);
            float age = Mathf.Lerp(now - b.time, now - c.time, t) / lifetime;
            float alpha = Mathf.Clamp01(1f - age);
            Color faded = new Color(colour.r, colour.g, colour.b, alpha * alpha);
            vertices.Add(innerPoint);
            colours.Add(faded);
            uvs.Add(new Vector2(1f - alpha, 0f));
            vertices.Add(tipPoint);
            colours.Add(faded);
            uvs.Add(new Vector2(1f - alpha, 1f));
        }

        static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3);
        }
    }
}
