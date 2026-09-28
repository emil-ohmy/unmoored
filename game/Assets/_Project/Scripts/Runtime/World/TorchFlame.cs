using UnityEngine;

namespace MyGame
{
    // Embers drifting up from a torch flame, and its crackle loop (the light's flicker is LightFlicker's job).
    public sealed class TorchFlame : MonoBehaviour
    {
        public Material emberMaterial;
        public float rate = 7f;

        void Start()
        {
            var ps = gameObject.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.75f, 0.35f), new Color(1f, 0.45f, 0.15f));
            main.gravityModifier = -0.15f; // rise
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 40;
            var emission = ps.emission;
            emission.rateOverTime = rate;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f;
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.4f;
            noise.frequency = 0.8f;
            var colour = ps.colorOverLifetime;
            colour.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            colour.color = fade;
            var renderer = GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = emberMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();

            BuildTongues();

            if (TryGetComponent(out AudioSource crackle) && crackle.clip != null)
            {
                crackle.time = Random.Range(0f, crackle.clip.length); // so neighbouring torches don't crackle in unison
                crackle.Play();
            }
        }

        // Soft, glowing tongues of flame licking up from the fire: they rise, shrink and redden as they fade.
        void BuildTongues()
        {
            var go = new GameObject("Flame Tongues");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.down * 0.25f;
            go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f); // the cone emits along +Z: point it up
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.22f, 0.36f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.75f, 0.3f), new Color(1f, 0.52f, 0.16f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 30;
            var emission = ps.emission;
            emission.rateOverTime = 24f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 9f;
            shape.radius = 0.07f;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.15f));
            var colour = ps.colorOverLifetime;
            colour.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.9f, 0.6f), 0f), new GradientColorKey(new Color(1f, 0.3f, 0.1f), 1f) },
                         new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.7f, 0.4f), new GradientAlphaKey(0f, 1f) });
            colour.color = fade;
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.25f;
            noise.frequency = 1.5f;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = emberMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
        }
    }
}
