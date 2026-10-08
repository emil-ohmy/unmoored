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
