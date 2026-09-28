using UnityEngine;

namespace MyGame
{
    // Healing, made visible: while the Apprentice heals, warm embers rise from the ground and draw in to its chest and
    // a warm light swells; when the charge takes effect, a burst of embers and a warm ring go out, and the body
    // glows for a moment. A heal cut short by a blow just gutters out.
    [RequireComponent(typeof(Apprentice))]
    public sealed class HealGlow : MonoBehaviour
    {
        public Color colour = new Color(1f, 0.72f, 0.4f);
        public float chestHeight = 0.65f;

        Apprentice apprentice;
        ParticleSystem embers;
        Light glow;
        SkinnedMeshRenderer body;
        MaterialPropertyBlock block;
        float burstAt = -10f, emitDebt;

        public int LiveEmbers => embers != null ? embers.particleCount : 0;
        public float GlowIntensity => glow != null ? glow.intensity : 0f;

        void Awake()
        {
            apprentice = GetComponent<Apprentice>();
            body = GetComponentInChildren<SkinnedMeshRenderer>();
            block = new MaterialPropertyBlock();
            glow = new GameObject("Heal Light").AddComponent<Light>();
            glow.transform.SetParent(transform, false);
            glow.transform.localPosition = Vector3.up * chestHeight;
            glow.type = LightType.Point;
            glow.color = colour;
            glow.range = 3.5f;
            glow.intensity = 0f;
            glow.shadows = LightShadows.None;
            glow.enabled = false;
        }

        void Start()
        {
            var go = new GameObject("Heal Embers");
            go.transform.SetParent(transform, false);
            embers = go.AddComponent<ParticleSystem>();
            embers.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = embers.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 160;
            var emission = embers.emission;
            emission.enabled = false;
            var size = embers.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = Effects.Instance != null ? Effects.Instance.sparkMaterial : null;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            apprentice.Healed += Burst;
        }

        void OnDestroy()
        {
            if (apprentice != null)
                apprentice.Healed -= Burst;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            bool healing = apprentice.CurrentState == Apprentice.State.Healing;
            Vector3 chest = transform.position + Vector3.up * chestHeight;
            float since = Time.time - burstAt;
            float target = 0f;
            if (healing)
            {
                // Gathering: embers rise from a ring at the feet and draw in to the chest.
                emitDebt += dt * 70f;
                for (; emitDebt >= 1f; emitDebt -= 1f)
                {
                    Vector2 ring = Random.insideUnitCircle.normalized * Random.Range(0.6f, 1.0f);
                    Vector3 from = transform.position + new Vector3(ring.x, Random.Range(0.02f, 0.3f), ring.y);
                    float life = Random.Range(0.4f, 0.6f);
                    embers.Emit(new ParticleSystem.EmitParams
                    {
                        position = from,
                        velocity = (chest - from) / life,
                        startLifetime = life,
                        startSize = Random.Range(0.05f, 0.11f),
                        startColor = Color.Lerp(colour, new Color(1f, 0.93f, 0.7f), Random.value),
                        applyShapeToPosition = false,
                    }, 1);
                }
                target = 2.2f;
            }
            if (since < 1f)
                target = Mathf.Max(target, 5f * Mathf.Exp(-since * 5f)); // the flash of the heal taking hold
            glow.intensity = Mathf.MoveTowards(glow.intensity, target, (healing ? 4f : 12f) * dt);
            glow.enabled = glow.intensity > 0.01f;

            float warmth = since < 0.6f ? 0.9f * Mathf.Exp(-since * 6f) : 0f;
            if (warmth > 0.01f)
            {
                block.SetColor("_EmissionColor", colour * warmth);
                body.SetPropertyBlock(block);
            }
            else if (since < 1f)
                body.SetPropertyBlock(null);
        }

        void Burst()
        {
            burstAt = Time.time;
            Vector3 chest = transform.position + Vector3.up * chestHeight;
            for (int i = 0; i < 40; i++)
            {
                Vector3 out_ = Random.onUnitSphere;
                out_.y = Mathf.Abs(out_.y) * 0.6f + 0.3f;
                embers.Emit(new ParticleSystem.EmitParams
                {
                    position = chest + out_ * 0.2f,
                    velocity = out_ * Random.Range(1.5f, 3.5f),
                    startLifetime = Random.Range(0.5f, 1.0f),
                    startSize = Random.Range(0.06f, 0.14f),
                    startColor = Color.Lerp(colour, new Color(1f, 0.95f, 0.75f), Random.value),
                    applyShapeToPosition = false,
                }, 1);
            }
            Effects.Instance?.HealRing(transform.position + Vector3.up * 0.1f);
        }
    }
}
