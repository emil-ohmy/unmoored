using UnityEngine;

namespace MyGame
{
    // One-shot visual effects, built in code from particle systems. Materials come from the scene builder.
    public sealed class Effects : MonoBehaviour
    {
        public static Effects Instance { get; private set; }

        public Material sparkMaterial; // MyGame/Particle with a soft dot
        public Material flashMaterial; // MyGame/Particle with a ring

        ParticleSystem sparks, flash, fare;
        ParticleSystem.Particle[] fareBuffer = new ParticleSystem.Particle[256];
        Transform fareTarget;
        BowLantern fareLantern;

        public int LiveBlockSparks => sparks != null ? sparks.particleCount : 0;
        public int LiveFare => fare != null ? fare.particleCount : 0;

        void Awake()
        {
            Instance = this;
            sparks = Build("Block Sparks", sparkMaterial, stretch: true);
            flash = Build("Block Flash", flashMaterial, stretch: false);
            fare = BuildFare();
        }

        // Where a defeated Soured's fare flies to: the Apprentice's lantern.
        public void SetFareTarget(Transform target, BowLantern lantern)
        {
            fareTarget = target;
            fareLantern = lantern;
        }

        // One mote of a defeated Soured's fare: it bursts out, then spirals into the Apprentice's lantern.
        public void EmitFare(Vector3 at)
        {
            fare.Emit(new ParticleSystem.EmitParams
            {
                position = at,
                velocity = Random.insideUnitSphere * 1.2f + Vector3.up * 1.5f, // a small puff, then the pull takes over
                startLifetime = 5f,
                startSize = Random.Range(0.08f, 0.15f),
                startColor = Color.Lerp(new Color(0.75f, 1f, 0.5f), new Color(1f, 0.85f, 0.4f), Random.value),
                applyShapeToPosition = false,
            }, 1);
        }

        void LateUpdate()
        {
            if (fare == null || fare.particleCount == 0 || fareTarget == null)
                return;
            int count = fare.GetParticles(fareBuffer);
            float dt = Time.deltaTime;
            int arrived = 0;
            for (int i = 0; i < count; i++)
            {
                var p = fareBuffer[i];
                Vector3 to = fareTarget.position - p.position;
                float distance = to.magnitude;
                if (distance < 0.3f)
                {
                    p.remainingLifetime = 0f;
                    arrived++;
                }
                else
                {
                    float age = p.startLifetime - p.remainingLifetime;
                    // Sucked in: an accelerating pull with a slight spiral, so the fare arrives as one stream in ~1 s.
                    Vector3 seek = to / distance * (8f + age * 18f);
                    Vector3 swirl = Vector3.Cross(to, Vector3.up).normalized * Mathf.Clamp(distance * 0.8f, 0f, 2f);
                    p.velocity = Vector3.Lerp(p.velocity, seek + swirl, Mathf.Clamp01(dt * (5f + age * 10f)));
                }
                fareBuffer[i] = p;
            }
            fare.SetParticles(fareBuffer, count);
            if (arrived > 0)
                fareLantern?.Pulse(0.4f * arrived);
        }

        ParticleSystem BuildFare()
        {
            var ps = Build("Fare Motes", sparkMaterial, stretch: false);
            var main = ps.main;
            main.maxParticles = 256;
            var size = ps.sizeOverLifetime;
            size.enabled = false;
            var colour = ps.colorOverLifetime;
            colour.enabled = false;
            var trail = ps.trails;
            trail.enabled = false;
            return ps;
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

        // A Healing charge takes hold: a warm ring spreading at the Apprentice's feet.
        public void HealRing(Vector3 position)
        {
            flash.transform.position = position;
            var p = new ParticleSystem.EmitParams { startSize = 2.2f, startLifetime = 0.45f, startColor = new Color(1f, 0.55f, 0.24f, 0.85f), applyShapeToPosition = false };
            flash.Emit(p, 1);
        }

        // Something falls into the water: droplets thrown up and a pale ring on the surface.
        public void Splash(Vector3 position)
        {
            sparks.transform.SetPositionAndRotation(position, Quaternion.LookRotation(Vector3.up));
            for (int i = 0; i < 34; i++)
                sparks.Emit(new ParticleSystem.EmitParams
                {
                    startColor = Color.Lerp(new Color(0.55f, 0.68f, 0.82f, 0.9f), new Color(0.85f, 0.92f, 1f, 0.9f), Random.value),
                    startSize = Random.Range(0.06f, 0.13f),
                    startLifetime = Random.Range(0.35f, 0.7f),
                }, 1);
            flash.transform.position = position;
            flash.Emit(new ParticleSystem.EmitParams { startSize = 1.8f, startLifetime = 0.5f, startColor = new Color(0.6f, 0.75f, 0.9f, 0.7f), applyShapeToPosition = false }, 1);
        }

        // Wet iron catching the moonlight: one small, quick, cold sparkle (hook post rings).
        public void MoonGlint(Vector3 position)
        {
            flash.transform.position = position;
            var p = new ParticleSystem.EmitParams { startSize = 0.42f, startLifetime = 0.18f, startColor = new Color(0.85f, 0.92f, 1f, 0.95f), applyShapeToPosition = false };
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
