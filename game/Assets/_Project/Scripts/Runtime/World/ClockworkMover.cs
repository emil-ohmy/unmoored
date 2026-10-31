using UnityEngine;

namespace MyGame
{
    // Something a Clockwork drives: while it runs, this is driven out (`travel` from where it rests) over travelTime; as
    // it stops, back again. A gear door rolls as it goes (turning about rollAxis as a wheel of rollRadius would); a lift
    // carries whoever stands on its deck (its origin at the deck's top). Its clockwork running for good (the Hall's time
    // set going), one that `cycles` goes out and back on its own, waiting `dwell` at each end (a lift: up, down, up).
    public sealed class ClockworkMover : MonoBehaviour
    {
        public Clockwork clockwork;
        public Vector3 travel;              // world, from rest to driven
        public float travelTime = 2.5f;
        public Vector3 rollAxis = Vector3.right;
        public float rollRadius;            // 0: it doesn't roll
        public bool carries;
        public Vector2 deck = new Vector2(1.6f, 1.6f); // (x, z) the deck's size, centred on its origin
        public bool cycles;
        public float dwell = 3f;

        public float Progress { get; private set; } // 0 at rest .. 1 driven out
        public bool Out => Progress >= 1f;
        public bool Home => Progress <= 0f;

        Vector3 rest;
        Quaternion restRotation;
        Apprentice rider;
        float cycle;

        void Start()
        {
            rest = transform.position;
            restRotation = transform.rotation;
        }

        void Update()
        {
            float target = clockwork != null && clockwork.Running ? 1f : 0f;
            if (cycles && clockwork != null && clockwork.Forever)
            {
                cycle += Time.deltaTime; // out, a wait, back, a wait
                target = cycle % (2f * (travelTime + dwell)) < travelTime + dwell ? 1f : 0f;
            }
            float before = Progress;
            Progress = Mathf.MoveTowards(Progress, target, Time.deltaTime / Mathf.Max(0.01f, travelTime));
            if (Progress == before)
                return;
            float e = UiKit.Smooth(0f, 1f, Progress), eb = UiKit.Smooth(0f, 1f, before);
            if (carries)
                Carry(travel * (e - eb));
            transform.position = rest + travel * e;
            if (rollRadius > 0f)
                transform.rotation = Quaternion.AngleAxis(travel.magnitude * e / rollRadius * Mathf.Rad2Deg, rollAxis) * restRotation;
        }

        // Whoever stands on the deck rides it.
        void Carry(Vector3 delta)
        {
            if (rider == null)
                rider = FindAnyObjectByType<Apprentice>();
            if (rider == null || !rider.IsAlive)
                return;
            Vector3 local = rider.transform.position - transform.position;
            if (Mathf.Abs(local.x) <= deck.x * 0.5f && Mathf.Abs(local.z) <= deck.y * 0.5f && Mathf.Abs(local.y) < 0.4f)
                rider.Carry(delta);
        }
    }
}
