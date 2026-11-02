using UnityEngine;

namespace MyGame
{
    // A face or a hand of the kept, under the Hold's living wall: now and then, while the Apprentice is near, it presses
    // slowly out of the stone (`body` moved out along this transform's forward, out of the wall), holds a moment, and
    // sinks back in. Heard: stone moving like slow clay.
    public sealed class WallPresence : MonoBehaviour
    {
        public Transform body;
        public float inside = -0.3f, outside = 0.06f; // (along forward: hidden in the wall .. pressed out)
        public float near = 11f;
        public Vector2 every = new Vector2(6f, 16f);
        public float rise = 2.6f, hold = 1.6f, sink = 3.2f;
        public AudioClip[] pressSound;

        public int Presses { get; private set; } // (tests)
        public float Out { get; private set; }   // 0 hidden .. 1 pressed out

        Transform apprentice;
        float next, began = -100f;

        void Start()
        {
            var a = FindAnyObjectByType<Apprentice>();
            apprentice = a != null ? a.transform : null;
            next = Time.time + Random.Range(1f, every.y);
            Place(0f);
        }

        void Update()
        {
            float t = Time.time - began;
            float length = rise + hold + sink;
            if (t > length && Time.time >= next && apprentice != null
                && Vector3.Distance(apprentice.position, transform.position) < near)
            {
                began = Time.time;
                next = Time.time + length + Random.Range(every.x, every.y);
                Presses++;
                Sfx.Instance?.Attack(pressSound, transform.position, 0.9f, "hold_stone_press");
                t = 0f;
            }
            Out = t < rise ? Mathf.SmoothStep(0f, 1f, t / rise)
                : t < rise + hold ? 1f
                : t < length ? 1f - Mathf.SmoothStep(0f, 1f, (t - rise - hold) / sink) : 0f;
            Place(Out);
        }

        // Pressed out now (a Shade taken back into the stone near it: a face, a moment after), unless it already is.
        public void PressNow()
        {
            if (Time.time - began < rise + hold + sink)
                return;
            began = Time.time;
            next = Time.time + rise + hold + sink + Random.Range(every.x, every.y);
            Presses++;
            Sfx.Instance?.Attack(pressSound, transform.position, 0.9f, "hold_stone_press");
        }

        void Place(float u)
        {
            if (body != null)
                body.localPosition = Vector3.forward * Mathf.Lerp(inside, outside, u);
        }
    }
}
