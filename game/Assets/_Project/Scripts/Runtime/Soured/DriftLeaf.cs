using UnityEngine;

namespace MyGame
{
    // A dead leaf flung from Father Moth's wings: glowing faintly at its edges (seen in the dark), drifting straight on at
    // chest height, turning as it goes. It strikes whoever it reaches: a dodge goes through it, a block stops it (a
    // perfect block knocks it away), and it's gone at the glasshouse's walls.
    public sealed class DriftLeaf : MonoBehaviour
    {
        public static int Live { get; private set; } // (tests)

        Apprentice target;
        Vector3 velocity;
        float damage, knock, life;
        bool passed;                           // dodged through: it goes on, and never strikes them
        Vector3 middle;
        Vector2 bounds;

        public static DriftLeaf Fling(Vector3 from, Vector3 direction, float speed, float damage, Apprentice target, Material material,
                                      Vector3 middle, Vector2 bounds)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "Drift Leaf";
            Destroy(go.GetComponent<Collider>());
            direction.y = 0f;
            var leaf = go.AddComponent<DriftLeaf>();
            leaf.velocity = (direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward) * speed;
            leaf.target = target;
            leaf.damage = damage;
            leaf.knock = 2.5f;
            leaf.middle = middle;
            leaf.bounds = bounds;
            go.transform.position = from;
            go.transform.rotation = Quaternion.LookRotation(Vector3.down, leaf.velocity) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            go.transform.localScale = new Vector3(0.38f, 0.55f, 1f);
            var renderer = go.GetComponent<MeshRenderer>();
            if (material != null)
                renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return leaf;
        }

        void OnEnable() => Live++;
        void OnDisable() => Live--;

        void Update()
        {
            float dt = Time.deltaTime;
            life += dt;
            transform.position += velocity * dt;
            transform.Rotate(0f, 0f, 260f * dt, Space.Self); // (turning over as it drifts)
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
