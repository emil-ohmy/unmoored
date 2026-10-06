using UnityEngine;

namespace MyGame
{
    // A dead leaf thrown out of Father Moth's autumn hollow: pointed, with its stalk, its edges smouldering faintly (seen in
    // the dark); out of the hollow it sinks to chest height, then drifts straight on, tumbling over as it goes. It strikes whoever it reaches: a dodge goes through
    // it, a block stops it (a perfect block knocks it away), and it's gone at the glasshouse's walls.
    public sealed class DriftLeaf : MonoBehaviour
    {
        public static int Live { get; private set; } // (tests)

        Apprentice target;
        Vector3 velocity;
        float damage, knock, life, spin, tumble, settleY, sink;
        bool passed;                           // dodged through: it goes on, and never strikes them
        Vector3 middle;
        Vector2 bounds;
        Transform body;

        static Mesh shape;

        public static DriftLeaf Fling(Vector3 from, Vector3 direction, float speed, float damage, Apprentice target, Material material,
                                      Vector3 middle, Vector2 bounds, float size = 1.25f, float settleY = float.NaN)
        {
            var go = new GameObject("Drift Leaf");
            direction.y = 0f;
            var leaf = go.AddComponent<DriftLeaf>();
            leaf.velocity = (direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward) * speed;
            leaf.target = target;
            leaf.damage = damage;
            leaf.knock = 2.5f;
            leaf.middle = middle;
            leaf.bounds = bounds;
            leaf.spin = Random.Range(-90f, 90f);
            leaf.tumble = Random.Range(0f, 360f);
            go.transform.position = from;
            leaf.settleY = float.IsNaN(settleY) ? from.y : settleY;
            leaf.sink = Mathf.Max(0f, from.y - leaf.settleY) / 0.45f; // (down to chest height in about half a second)
            go.transform.rotation = Quaternion.LookRotation(leaf.velocity);
            // The leaf itself, lying along its way, tumbling about its length (its own child, so the drift stays straight).
            var body = new GameObject("Leaf", typeof(MeshFilter), typeof(MeshRenderer));
            body.transform.SetParent(go.transform, false);
            body.transform.localScale = Vector3.one * size;
            body.GetComponent<MeshFilter>().sharedMesh = Shape();
            var renderer = body.GetComponent<MeshRenderer>();
            if (material != null)
                renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            leaf.body = body.transform;
            return leaf;
        }

        // A leaf: pointed at the tip, broadest a little below the middle, a short stalk; both faces, flat in its own XZ plane
        // (length along +Z), half a metre long.
        static Mesh Shape()
        {
            if (shape != null)
                return shape;
            var outline = new System.Collections.Generic.List<Vector3>();
            const int half = 7;
            for (int i = 0; i <= half; i++) // (one side, base to tip; the other mirrored)
            {
                float t = i / (float)half;
                float z = Mathf.Lerp(-0.22f, 0.3f, t);
                float w = 0.15f * Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.8f)) * (1f - 0.25f * t);
                outline.Add(new Vector3(w, 0f, z));
            }
            var verts = new System.Collections.Generic.List<Vector3> { new Vector3(0f, 0f, 0.02f) }; // (the middle)
            verts.AddRange(outline);
            for (int i = half - 1; i >= 1; i--)
                verts.Add(new Vector3(-outline[i].x, 0f, outline[i].z));
            int rim = verts.Count - 1;
            // The stalk: a thin sliver behind the base.
            int s0 = verts.Count;
            verts.Add(new Vector3(-0.012f, 0f, -0.22f));
            verts.Add(new Vector3(0.012f, 0f, -0.22f));
            verts.Add(new Vector3(0.008f, 0f, -0.32f));
            verts.Add(new Vector3(-0.008f, 0f, -0.32f));
            var tris = new System.Collections.Generic.List<int>();
            for (int i = 1; i <= rim; i++)
            {
                int a = i, b = i % rim + 1;
                tris.AddRange(new[] { 0, a, b, 0, b, a }); // (both faces)
            }
            tris.AddRange(new[] { s0, s0 + 1, s0 + 2, s0, s0 + 2, s0 + 3, s0, s0 + 2, s0 + 1, s0, s0 + 3, s0 + 2 });
            shape = new Mesh { name = "Drift Leaf" };
            shape.SetVertices(verts);
            shape.SetTriangles(tris, 0);
            var normals = new Vector3[verts.Count];
            for (int i = 0; i < normals.Length; i++)
                normals[i] = Vector3.up;
            shape.normals = normals;
            shape.RecalculateBounds();
            return shape;
        }

        void OnEnable() => Live++;
        void OnDisable() => Live--;

        void Update()
        {
            float dt = Time.deltaTime;
            life += dt;
            transform.position += velocity * dt;
            if (transform.position.y > settleY)
                transform.position = new Vector3(transform.position.x, Mathf.MoveTowards(transform.position.y, settleY, sink * dt), transform.position.z);
            // Tumbling over about its length, and a slow turn: a leaf on the wind.
            tumble += 420f * dt;
            if (body != null)
                body.localRotation = Quaternion.Euler(0f, Mathf.Sin(life * 3f) * 25f + spin * life, 0f) * Quaternion.Euler(0f, 0f, tumble);
            Vector3 p = transform.position;
            if (Mathf.Abs(p.x - middle.x) > bounds.x || Mathf.Abs(p.z - middle.z) > bounds.y || life > 8f)
            {
                Gone(false);
                return;
            }
            if (passed || target == null || !target.IsAlive)
                return;
            Vector3 to = target.transform.position + Vector3.up * 0.9f - p;
            if (new Vector2(to.x, to.z).magnitude > 0.55f || Mathf.Abs(to.y) > 1.1f)
                return;
            var defence = target.TakeHit(damage, velocity.normalized * knock);
            if (defence == Defence.Dodged)
                passed = true;
            else
                Gone(true);
        }

        // Struck down (stopped, or at a wall): a puff of leaves.
        void Gone(bool struck)
        {
            Effects.Instance?.Leaves(transform.position, struck ? -velocity * 0.2f : Vector3.zero, struck ? 6 : 3);
            Destroy(gameObject);
        }
    }
}
