using UnityEngine;

namespace MyGame
{
    // One-shot visual effects, built in code from particle systems. Materials come from the scene builder.
    public sealed class Effects : MonoBehaviour
    {
        public static Effects Instance { get; private set; }

        public Material sparkMaterial; // MyGame/Particle with a soft dot
        public Material flashMaterial; // MyGame/Particle with a ring

        ParticleSystem sparks, flash;

        public int LiveBlockSparks => sparks != null ? sparks.particleCount : 0;

        void Awake()
        {
            Instance = this;
            sparks = Build("Block Sparks", sparkMaterial, stretch: true);
            flash = Build("Block Flash", flashMaterial, stretch: false);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // A perfect block: a big golden ring and a burst of bright sparks.
        public void ParryFlash(Vector3 position, Vector3 normal)
        {
            var rotation = Quaternion.LookRotation(normal.sqrMagnitude > 0.001f ? normal : Vector3.up);
            sparks.transform.SetPositionAndRotation(position, rotation);
            flash.transform.position = position;
            flash.Emit(new ParticleSystem.EmitParams { startSize = 1.5f, startColor = new Color(1f, 0.85f, 0.45f, 1f), applyShapeToPosition = false }, 1);
            sparks.Emit(new ParticleSystem.EmitParams { startColor = new Color(1f, 0.95f, 0.7f, 1f) }, 45);
        }

        // A Soured notices the Apprentice: its eyes flare sickly green.
        public void AlertFlare(Vector3 position)
        {
            flash.transform.position = position;
            var p = new ParticleSystem.EmitParams { startSize = 0.6f, startColor = new Color(0.7f, 1f, 0.45f, 1f), applyShapeToPosition = false };
            flash.Emit(p, 1);
        }

        // The moment a guard comes up: a small, quick glint on the shield.
        public void GuardGlint(Vector3 position)
        {
            flash.transform.position = position;
            var p = new ParticleSystem.EmitParams { startSize = 0.35f, startColor = new Color(1f, 0.95f, 0.8f, 0.8f), applyShapeToPosition = false };
            flash.Emit(p, 1);
        }

        // A blocked hit: a spray of sparks off the surface plus a quick expanding ring.
        public void BlockSpark(Vector3 position, Vector3 normal)
        {
            var rotation = Quaternion.LookRotation(normal.sqrMagnitude > 0.001f ? normal : Vector3.up);
            sparks.transform.SetPositionAndRotation(position, rotation);
            flash.transform.SetPositionAndRotation(position, rotation);
            sparks.Emit(26);
            flash.Emit(1);
        }

        ParticleSystem Build(string name, Material material, bool stretch)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 128;
            var emission = ps.emission;
            emission.enabled = false;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            if (stretch)
            {
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.32f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 10f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.14f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.75f), new Color(1f, 0.7f, 0.35f));
                main.gravityModifier = 1.2f;
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 55f;
                shape.radius = 0.05f;
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.velocityScale = 0.04f;
                renderer.lengthScale = 1.5f;
            }
            else
            {
                main.startLifetime = 0.2f;
                main.startSpeed = 0f;
                main.startSize = 0.7f;
                main.startColor = new Color(1f, 0.85f, 0.55f);
                var shape = ps.shape;
                shape.enabled = false;
                var size = ps.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 2.8f));
                var colour = ps.colorOverLifetime;
                colour.enabled = true;
                var fade = new Gradient();
                fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                             new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
                colour.color = fade;
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
            }
            return ps;
        }
    }
}
