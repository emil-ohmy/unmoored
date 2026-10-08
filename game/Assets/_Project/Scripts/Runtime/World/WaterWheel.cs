using UnityEngine;

namespace MyGame
{
    // A mill's water wheel. Still, until a surge comes down its race and breaks on it (MillRace): then it turns, a
    // couple of turns, slowing, and as it turns it winds its chain: the sluice gate it's chained to rises a stage
    // (SluiceGate). Its axle is the world's `axis`; turned by water poured over its top. Any other wave that reaches it
    // (from the yard, or across the race) only rocks it: a lurch and a creak, a little way round and back to still, no
    // more: it wants the race's water behind it. Struck with the oar (Stroke or no Stroke) it answers too: a thump, a
    // creak, a short rock round: something can be done here.
    public sealed class WaterWheel : MonoBehaviour, IWaveable, IStrikable
    {
        public Transform wheel;
        public Vector3 axis = Vector3.forward;
        public float kick = 420f;          // degrees a second, as a surge breaks on it
        public float slowing = 0.55f;
        public SluiceGate gate;
        public float radius = 1.3f;
        public AudioClip[] turnSound, creakSound;

        public float Spin { get; private set; }     // degrees a second now
        public float Turned { get; private set; }   // degrees, in all
        public int Turns { get; private set; }      // surges taken (tests)
        public int Lurches { get; private set; }    // other waves that rocked it (tests)
        public int Knocks { get; private set; }     // oar blows (tests)
        public float Rocking => lurch;

        float lurch; // degrees a second, of a rocking (dies away fast)

        void OnEnable() { Waveables.Register(this); Strikables.Register(this); }
        void OnDisable() { Waveables.Unregister(this); Strikables.Unregister(this); }

        public Vector3 StrikePoint => transform.position;
        public float StrikeRadius => radius;

        // An oar blow on its paddles: it rocks a little way round (more for a heavy), creaking.
        public void Struck(Apprentice by, bool heavy)
        {
            Knocks++;
            lurch = Mathf.Max(lurch, heavy ? 110f : 65f);
            Vector3 at = transform.position + Vector3.ClampMagnitude(by.transform.position - transform.position, radius) + Vector3.up * 0.4f;
            Sfx.Instance?.OarHit(at, heavy);
            Sfx.Instance?.Attack(creakSound, transform.position, heavy ? 0.7f : 0.5f, "mill_wheel_creak");
            Effects.Instance?.OarDip(new Vector3(at.x, transform.position.y + 0.1f, at.z)); // (drops shaken off it)
            CameraRig.Instance?.Shake(heavy ? 0.12f : 0.06f);
        }

        public Vector3 WavePoint => transform.position;
        public float WaveRadius => radius;

        // A wave reaching it some other way than down its race: it rocks, and creaks, and that's all.
        public void TakeWave(StrokeWave wave, Vector3 push)
        {
            Lurches++;
            lurch = 160f;
            Sfx.Instance?.Attack(creakSound, transform.position, 0.9f, "mill_wheel_creak");
            if (Effects.Instance != null)
                Effects.Instance.Splash(transform.position + Vector3.up * 0.1f);
        }

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
            float dt = Time.deltaTime;
            if (lurch > 0.5f)
            {
                if (wheel != null)
                    wheel.Rotate(axis, -lurch * dt, Space.World);
                lurch *= Mathf.Exp(-4f * dt); // (about forty degrees, then it settles)
            }
            else
                lurch = 0f;
            if (Spin < 0.5f)
            {
                Spin = 0f;
                return;
            }
            float step = Spin * dt;
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
