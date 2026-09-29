using UnityEngine;

namespace MyGame
{
    // Everything that can be picked up glows (a design rule: see docs/brief.md). Items glow softly and warm, with a
    // few embers rising; key items (Abilities) glow brightly in the Apprentice's cold pale blue, the object itself lit,
    // a column of motes rising, so they read from across a quay. Not yet takeable (sealed): only a faint glow.
    public sealed class PickupGlow : MonoBehaviour
    {
        public enum Tier { Item, Key }

        public Tier tier = Tier.Item;
        public Color colour = new Color(1f, 0.67f, 0.32f);
        public float height = 1f;          // where the light sits, above the pickup's base
        public Renderer[] glowing;         // the object itself, lit up (optional)

        public bool Lit { get; set; } = true; // false: sealed, only a faint glow
        public float Intensity => lamp != null ? lamp.intensity : 0f;

        Light lamp;
        ParticleSystem motes;
        MaterialPropertyBlock block;
        float emitDebt, phase;

        bool Key => tier == Tier.Key;

        void Awake()
        {
            phase = Random.value * 10f;
            block = new MaterialPropertyBlock();
            lamp = new GameObject("Pickup Glow").AddComponent<Light>();
            lamp.transform.SetParent(transform, false);
            lamp.transform.localPosition = Vector3.up * height;
            lamp.type = LightType.Point;
            lamp.color = colour;
            lamp.range = Key ? 6.5f : 3.2f;
            lamp.intensity = 0f;
            lamp.shadows = LightShadows.None;
        }

        void Start()
        {
            var go = new GameObject("Pickup Motes");
            go.transform.SetParent(transform, false);
            motes = go.AddComponent<ParticleSystem>();
            motes.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = motes.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 96;
            var emission = motes.emission;
            emission.enabled = false;
            var size = motes.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = Effects.Instance != null ? Effects.Instance.sparkMaterial : null;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void OnDisable()
        {
            if (glowing == null)
                return;
            foreach (var r in glowing)
                if (r != null)
                    r.SetPropertyBlock(null);
        }

        void Update()
        {
            float t = Time.time + phase;
            float breathe = 0.82f + 0.18f * Mathf.Sin(t * (Key ? 2.2f : 1.6f));
            float target = Lit ? (Key ? 4.6f : 1.5f) * breathe : (Key ? 0.35f : 0.2f);
            lamp.intensity = Mathf.MoveTowards(lamp.intensity, target, Time.deltaTime * 4f);

            if (glowing != null)
            {
                block.SetColor("_EmissionColor", colour * (Lit ? (Key ? 1.4f : 0.5f) * breathe : 0.08f));
                foreach (var r in glowing)
                    if (r != null)
                        r.SetPropertyBlock(block);
            }

            if (motes == null)
                return;
            emitDebt += Time.deltaTime * (Lit ? (Key ? 16f : 5f) : 0.8f);
            for (; emitDebt >= 1f; emitDebt -= 1f)
            {
                Vector2 ring = Random.insideUnitCircle * (Key ? 0.45f : 0.25f);
                motes.Emit(new ParticleSystem.EmitParams
                {
                    position = transform.position + new Vector3(ring.x, height * 0.6f, ring.y),
                    velocity = new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(0.5f, Key ? 1.4f : 0.8f), Random.Range(-0.1f, 0.1f)),
                    startLifetime = Random.Range(0.9f, Key ? 2.2f : 1.4f),
                    startSize = Random.Range(Key ? 0.07f : 0.04f, Key ? 0.15f : 0.07f),
                    startColor = Color.Lerp(colour, Color.white, Random.Range(0.1f, 0.5f)),
                    applyShapeToPosition = false,
                }, 1);
            }
        }
    }
}
