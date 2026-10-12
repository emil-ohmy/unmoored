using UnityEngine;

namespace MyGame
{
    // Time stopped here: dust hanging in the air, not drifting, not falling; drops of water held where they fell. A
    // particle system filled once and then left: nothing in it moves. (`drops`: points where a drop hangs, a little
    // larger and bluer, catching the light.)
    public sealed class StillAir : MonoBehaviour
    {
        public Vector3 size = new Vector3(10f, 3f, 10f); // the box the dust hangs in, centred here
        public int motes = 120;
        public Color dust = new Color(0.95f, 0.9f, 0.78f, 0.34f); // (motes catching the lamplight)
        public Vector3[] drops = new Vector3[0];         // (world points)
        public Material material;

        public int Count => system != null ? system.particleCount : 0; // (tests)

        ParticleSystem system;

        ParticleSystem.Particle[] buffer;
        Vector3[] home; // (where each was held: how far it may drift)

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
            system = gameObject.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startSpeed = 0f;
            main.gravityModifier = 0f;
            main.maxParticles = motes + drops.Length + 8;
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
            Vector3 p = transform.position; // (seeded by where it is: the same dust every time)
            var rng = new System.Random(Mathf.RoundToInt(p.x * 73f + p.y * 31f + p.z * 17f));
            float R() => (float)rng.NextDouble() - 0.5f;
            for (int i = 0; i < motes; i++)
                system.Emit(new ParticleSystem.EmitParams
                {
                    position = transform.position + Vector3.Scale(size, new Vector3(R(), R(), R())),
                    velocity = Vector3.zero,
                    startLifetime = 1e6f,
                    startSize = 0.025f + 0.04f * (float)rng.NextDouble(),
                    startColor = dust * (0.6f + 0.6f * (float)rng.NextDouble()),
                    applyShapeToPosition = false,
                }, 1);
            foreach (var d in drops)
                system.Emit(new ParticleSystem.EmitParams
                {
                    position = d, velocity = Vector3.zero, startLifetime = 1e6f, startSize = 0.09f,
                    startColor = new Color(0.66f, 0.8f, 1f, 0.9f), applyShapeToPosition = false,
                }, 1);
        }
    }
}
