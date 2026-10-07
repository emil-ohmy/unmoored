using UnityEngine;

namespace MyGame
{
    // A mill's water wheel. Still, until a surge comes down its race and breaks on it (MillRace): then it turns, a
    // couple of turns, slowing, and as it turns it winds its chain: the sluice gate it's chained to rises a stage
    // (SluiceGate). Its axle is the world's `axis`; turned by water poured over its top.
    public sealed class WaterWheel : MonoBehaviour
    {
        public Transform wheel;
        public Vector3 axis = Vector3.forward;
        public float kick = 420f;          // degrees a second, as a surge breaks on it
        public float slowing = 0.55f;
        public SluiceGate gate;
        public AudioClip[] turnSound;

        public float Spin { get; private set; }     // degrees a second now
        public float Turned { get; private set; }   // degrees, in all
        public int Turns { get; private set; }      // surges taken (tests)

        // A surge breaking over its paddles.
        public void Turn()
        {
            Turns++;
            Spin += kick;
            Sfx.Instance?.Attack(turnSound, transform.position, 1f, "mill_wheel");
            gate?.Raise();
        }

        void Update()
        {
            if (Spin < 0.5f)
            {
                Spin = 0f;
                return;
            }
            float dt = Time.deltaTime, step = Spin * dt;
            Turned += step;
            if (wheel != null)
                wheel.Rotate(axis, -step, Space.World); // (its top towards the spout's far side: the water carries it over)
            Spin *= Mathf.Exp(-slowing * dt);
            if (Effects.Instance != null && Random.value < Spin / 400f) // (the paddles throwing water as they come up)
                Effects.Instance.Streak(transform.position + Vector3.up * 0.2f + Random.insideUnitSphere * 0.9f,
                                        Vector3.up * Random.Range(1f, 2f) + Random.insideUnitSphere, new Color(0.8f, 0.9f, 1f, 1f), 0.3f, 0.06f);
        }
    }
}
