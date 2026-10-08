using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    // Fast water: a stretch where the river runs hard one way (along this transform's forward), carrying the boat and
    // whatever floats with it. A strong one is faster than anyone can row (the boat's top speed), so against it the
    // oar alone loses ground: only the Stroke's surge climbs it, a surge at a time. Its water shows which way it runs:
    // foam drifting with it, streaks along its edges; and it's heard, a rush.
    public sealed class Current : MonoBehaviour
    {
        public Vector2 size = new Vector2(5f, 10f); // across (x) and along (z), centred on this transform
        public float speed = 4f;

        static readonly List<Current> all = new List<Current>();
        float flowDebt;

        void Start()
        {
            if (TryGetComponent(out AudioSource rush) && rush.clip != null) // (its rush, from somewhere in the loop)
            {
                rush.time = Random.value * rush.clip.length;
                rush.Play();
            }
        }

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        public Vector3 Flow => transform.forward * speed;

        // Whether `p` is in it (its height ignored).
        public bool Holds(Vector3 p)
        {
            Vector3 local = transform.InverseTransformPoint(new Vector3(p.x, transform.position.y, p.z));
            return Mathf.Abs(local.x) <= size.x * 0.5f && Mathf.Abs(local.z) <= size.y * 0.5f;
        }

        // The water's own movement at `p` (none, outside every current).
        public static Vector3 At(Vector3 p)
        {
            Vector3 flow = Vector3.zero;
            foreach (var c in all)
                if (c.Holds(p))
                    flow += c.Flow;
            return flow;
        }

        // Foam drifting with it, thicker along its edges (where it tears at the walls).
        void Update()
        {
            var fx = Effects.Instance;
            if (fx == null)
                return;
            flowDebt += Time.deltaTime * size.x * size.y * 0.9f;
            while (flowDebt >= 1f)
            {
                flowDebt -= 1f;
                float across = Random.value < 0.35f ? Mathf.Sign(Random.value - 0.5f) * Random.Range(0.38f, 0.48f) : Random.Range(-0.45f, 0.45f);
                Vector3 at = transform.TransformPoint(new Vector3(across * size.x, 0.03f, Random.Range(-0.5f, 0.45f) * size.y));
                fx.Streak(at, Flow * Random.Range(0.8f, 1.1f), new Color(0.82f, 0.9f, 1f, 0.75f), Random.Range(0.5f, 0.9f), Random.Range(0.05f, 0.09f));
            }
        }
    }
}
