using UnityEngine;

namespace MyGame
{
    // One of the great hall's pendulums, armed (a trap): a silver crescent on a long rod, swung by its Clockwork (its
    // Swinger) low across the aisle, its edge a hand's breadth over the floor at the bottom of the swing. What's in its
    // way as it sweeps through is cut and thrown along the swing. It's low where it's fastest: it can be vaulted there,
    // or dashed through; out at the ends of its swing it passes overhead. Held still (time stopped), it does no harm,
    // and it's solid.
    public sealed class PendulumBlade : MonoBehaviour
    {
        public Clockwork clockwork;
        public float edgeBelow = 7.7f;       // the pivot to the edge's lowest point (the model's)
        public float crescentRadius = 1.1f;  // its edge's curve
        public float harmfulSpeed = 2.5f;    // the edge moving this fast cuts
        public float knock = 26f;
        public AudioClip[] swishSound;

        public TrapBlows Blows { get; } = new TrapBlows();
        public float EdgeSpeed { get; private set; }  // (tests) its fastest point's, m/s
        public int Swishes { get; private set; }      // (tests) passes through the bottom, cutting

        // Round the crescent's edge, degrees in its plane (270: the bottom).
        static readonly float[] Along = { 200f, 235f, 270f, 305f, 340f };
        readonly Vector3[] edge = new Vector3[5], was = new Vector3[5], moving = new Vector3[5];
        Clockwork.Swinger swinger;
        Collider[] solids;
        bool primed;
        float lastSide;

        void Start()
        {
            if (clockwork != null)
                foreach (var s in clockwork.swingers)
                    if (s.part == transform)
                        swinger = s;
            solids = GetComponentsInChildren<Collider>();
        }

        // Where the edge is now (the swing's angle about its axis from hanging straight).
        void Edge()
        {
            float angle = swinger != null ? swinger.angle : 0f;
            Vector3 axis = swinger != null ? swinger.axis : Vector3.forward;
            var q = Quaternion.AngleAxis(angle, axis);
            Vector3 across = Vector3.Cross(Vector3.up, axis).normalized; // (in the swing's plane, level at rest)
            if (across.sqrMagnitude < 0.01f)
                across = Vector3.right;
            Vector3 centre = transform.position + q * (Vector3.down * (edgeBelow - crescentRadius));
            for (int i = 0; i < edge.Length; i++)
            {
                float a = Along[i] * Mathf.Deg2Rad;
                edge[i] = centre + q * (across * Mathf.Cos(a) + Vector3.up * Mathf.Sin(a)) * crescentRadius;
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            Edge();
            EdgeSpeed = 0f;
            if (primed && dt > 0f)
                for (int i = 0; i < edge.Length; i++)
                {
                    moving[i] = (edge[i] - was[i]) / dt;
                    EdgeSpeed = Mathf.Max(EdgeSpeed, moving[i].magnitude);
                }
            for (int i = 0; i < edge.Length; i++)
                was[i] = edge[i];
            primed = true;

            bool cutting = EdgeSpeed >= harmfulSpeed;
            foreach (var c in solids) // (still, it's in the way; swinging, it passes through what it hits)
                if (c != null)
                    c.enabled = clockwork == null || clockwork.Run <= 0.01f;
            if (!cutting)
                return;
            // Through the bottom of its swing: the swish.
            float side = Mathf.Sign(swinger != null ? swinger.angle : 0f);
            if (lastSide != 0f && side != lastSide)
            {
                Swishes++;
                Sfx.Instance?.Attack(swishSound, edge[2], Mathf.Clamp01(EdgeSpeed / 9f), "trap_blade_swish");
            }
            lastSide = side;
            Blows.Sweep((feet, radius) =>
            {
                foreach (var p in edge)
                    if (TrapBlows.Touches(p, 0.2f, feet, radius))
                        return true;
                return false;
            }, feet =>
            {
                int nearest = 0;
                for (int i = 1; i < edge.Length; i++)
                    if ((edge[i] - feet).sqrMagnitude < (edge[nearest] - feet).sqrMagnitude)
                        nearest = i;
                Vector3 along = moving[nearest];
                along.y = 0f;
                return along.normalized * knock;
            });
        }
    }
}
