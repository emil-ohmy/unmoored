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

            if (TryGetComponent(out AudioSource crackle) && crackle.clip != null)
            {
                crackle.time = Random.Range(0f, crackle.clip.length); // so neighbouring torches don't crackle in unison
                crackle.Play();
            }
        }
    }
}
