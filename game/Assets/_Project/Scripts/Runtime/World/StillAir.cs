using UnityEngine;

namespace MyGame
{
    // Time stopped here: dust hanging in the air, not drifting, not falling; drops of water held where they fell. A
    // particle system filled once and then left: nothing in it moves, but round the Apprentice (who carries time with
    // them): there it swirls in slow eddies as they pass, and hangs still again behind them wherever it got to. In the
    // Hall of the Last Hour (HallTime) it's grey while time's stopped; set going, the wave of time passing through it, it
    // falls and is gone. (`drops`: points where a drop hangs, a little larger and bluer, catching the light.)
    public sealed class StillAir : MonoBehaviour
    {
        public Vector3 size = new Vector3(10f, 3f, 10f); // the box the dust hangs in, centred here
        public int motes = 120;
        public float density = 2.4f;                     // (motes x this: thick enough to be seen)
        public float stirRadius = 2.2f;                  // round the Apprentice, it swirls
        public float stirSpeed = 1.6f;                   // how fast, at the middle, at a run
        public Color dust = new Color(0.95f, 0.9f, 0.78f, 0.34f); // (motes catching the lamplight)
        public Vector3[] drops = new Vector3[0];         // (world points)
        public Material material;

        public int Count => system != null ? system.particleCount : 0; // (tests)

        ParticleSystem system;

        ParticleSystem.Particle[] buffer;
        Vector3[] home; // (where each was held: how far it may drift)
        Apprentice apprentice;
        Vector3 apprenticeWas;
        HallTime hall;

        // How many motes have moved from where they hung (tests: stirred, or fallen).
        public int Moved(float atLeast)
        {
            if (system == null || home == null)
                return 0;
            int n = system.particleCount, moved = 0;
            if (buffer == null || buffer.Length < n)
                buffer = new ParticleSystem.Particle[n];
            system.GetParticles(buffer, n);
            for (int i = 0; i < n && i < home.Length; i++)
                if ((buffer[i].position - home[i]).sqrMagnitude > atLeast * atLeast)
                    moved++;
            return moved;
        }

        // Time moving here a while (a machine running near, `amount` 0..1): the dust within `radius` of `centre` drifts
        // and settles a little, the drops fall a little; when it stops, they hang still again wherever they've got to.
        public void Thaw(Vector3 centre, float radius, float amount, float dt)
        {
            if (system == null || amount <= 0f)
                return;
            int n = system.particleCount;
            if (buffer == null || buffer.Length < n)
                buffer = new ParticleSystem.Particle[n];
            system.GetParticles(buffer, n);
            if (home == null || home.Length != n)
            {
                home = new Vector3[n];
                for (int i = 0; i < n; i++)
                    home[i] = buffer[i].position;
            }
            float r2 = radius * radius;
            for (int i = 0; i < n; i++)
            {
                Vector3 p = buffer[i].position;
                if ((p - centre).sqrMagnitude > r2)
                    continue;
                bool drop = i >= n - drops.Length;
                float fall = drop ? 0.8f : 0.06f, limit = drop ? 0.7f : 0.5f;
                float phase = i * 1.37f;
                p += new Vector3(Mathf.Sin(Time.time * 0.7f + phase) * 0.05f, -fall, Mathf.Cos(Time.time * 0.6f + phase) * 0.05f) * amount * dt;
                p.y = Mathf.Max(p.y, home[i].y - limit);
                buffer[i].position = p;
            }
            system.SetParticles(buffer, n);
        }

        void Start()
        {
            apprentice = FindAnyObjectByType<Apprentice>();
            if (apprentice != null)
                apprenticeWas = apprentice.transform.position;
            hall = HallTime.Instance;
            if (hall != null)
                hall.Changed += running =>
                {
                    if (!running)
                        Fill(); // (stopped again: the dust hangs again)
                };
            system = gameObject.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startSpeed = 0f;
            main.gravityModifier = 0f;
            main.maxParticles = Mathf.RoundToInt(motes * density) + drops.Length + 8;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = system.emission;
            emission.enabled = false;
            var shape = system.shape;
            shape.enabled = false;
            var renderer = GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material != null ? material : Effects.Instance != null ? Effects.Instance.sparkMaterial : null;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            system.Play();
            Fill();
        }

        void Fill()
        {
            system.Clear();
            Vector3 p = transform.position; // (seeded by where it is: the same dust every time)
            var rng = new System.Random(Mathf.RoundToInt(p.x * 73f + p.y * 31f + p.z * 17f));
            float R() => (float)rng.NextDouble() - 0.5f;
            Color Shown(Color c) => hall != null ? hall.Stilled(c) : c; // (time stopped: grey)
            int count = Mathf.RoundToInt(motes * density);
            for (int i = 0; i < count; i++)
                system.Emit(new ParticleSystem.EmitParams
                {
                    position = transform.position + Vector3.Scale(size, new Vector3(R(), R(), R())),
                    velocity = Vector3.zero,
                    startLifetime = 1e6f,
                    startSize = 0.035f + 0.06f * (float)rng.NextDouble(),
                    startColor = Shown(dust * (0.8f + 0.6f * (float)rng.NextDouble())),
                    applyShapeToPosition = false,
                }, 1);
            foreach (var d in drops)
                system.Emit(new ParticleSystem.EmitParams
                {
                    position = d, velocity = Vector3.zero, startLifetime = 1e6f, startSize = 0.09f,
                    startColor = Shown(new Color(0.66f, 0.8f, 1f, 0.9f)), applyShapeToPosition = false,
                }, 1);
            home = null;
        }

        // Each frame: the dust round the Apprentice stirs (time stopped); where the wave of time has passed, it falls
        // and fades (time running).
        void Update()
        {
            if (system == null)
                return;
            bool stirring = apprentice != null && Near(apprentice.transform.position, stirRadius + 1f);
            bool falling = hall != null && hall.Running;
            if (!stirring && !falling)
            {
                if (apprentice != null)
                    apprenticeWas = apprentice.transform.position;
                return;
            }
            float dt = Time.deltaTime;
            int n = system.particleCount;
            if (buffer == null || buffer.Length < n)
                buffer = new ParticleSystem.Particle[n];
            system.GetParticles(buffer, n);
            if (home == null || home.Length != n)
            {
                home = new Vector3[n];
                for (int i = 0; i < n; i++)
                    home[i] = buffer[i].position;
            }
            Vector3 a = apprentice != null ? apprentice.transform.position + Vector3.up * 0.8f : Vector3.zero;
            float pace = 0f;
            if (apprentice != null && dt > 0f)
            {
                Vector3 moved = apprentice.transform.position - apprenticeWas;
                moved.y = 0f;
                pace = Mathf.Clamp01(moved.magnitude / dt / 5f); // (5: a run)
                apprenticeWas = apprentice.transform.position;
            }
            bool changed = false;
            for (int i = 0; i < n; i++)
            {
                Vector3 p = buffer[i].position;
                if (falling && !hall.StoppedAt(p))
                {
                    // Time again: it falls, drifting, and is gone.
                    float phase = i * 1.37f;
                    p += new Vector3(Mathf.Sin(Time.time * 0.9f + phase) * 0.15f, -0.45f, Mathf.Cos(Time.time * 0.7f + phase) * 0.15f) * dt;
                    var c = buffer[i].startColor;
                    c.a = (byte)Mathf.Max(0f, c.a - 255f * dt / 1.6f);
                    buffer[i].startColor = c;
                    buffer[i].position = p;
                    changed = true;
                    continue;
                }
                if (!stirring)
                    continue;
                Vector3 off = p - a;
                float d = new Vector2(off.x, off.z).magnitude;
                if (d > stirRadius || Mathf.Abs(off.y) > 1.6f)
                    continue;
                // An eddy round them as they pass: round, a little out and up, the stronger the nearer and the quicker
                // they go (standing still, it hangs).
                float k = (1f - d / stirRadius) * stirSpeed * pace;
                if (k <= 0f)
                    continue;
                Vector3 flat = new Vector3(off.x, 0f, off.z) / Mathf.Max(d, 0.05f);
                Vector3 round = Vector3.Cross(Vector3.up, flat);
                p += (round + flat * 0.35f + Vector3.up * (0.25f * Mathf.Sin(Time.time * 2f + i))) * k * dt;
                buffer[i].position = p;
                changed = true;
            }
            if (changed)
                system.SetParticles(buffer, n);
        }

        bool Near(Vector3 point, float margin)
        {
            Vector3 local = point - transform.position;
            return Mathf.Abs(local.x) <= size.x * 0.5f + margin && Mathf.Abs(local.y) <= size.y * 0.5f + margin + 1f
                   && Mathf.Abs(local.z) <= size.z * 0.5f + margin;
        }
    }
}
