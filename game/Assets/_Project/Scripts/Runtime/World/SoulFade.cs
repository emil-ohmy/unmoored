using UnityEngine;

namespace MyGame
{
    // A soul thinning away into warm wisps, or gathering again out of them: the Toon shader's dissolve over every
    // renderer below it (with a warm edge), its lights dimming with it. Amount 0: all there; 1: gone. Optionally the
    // wisps drift towards somewhere as it goes (a soul going on to the Far shore's light).
    public sealed class SoulFade : MonoBehaviour
    {
        public Color edge = new Color(2.4f, 1.6f, 0.8f);
        public Color wisp = new Color(1f, 0.8f, 0.55f, 1f);

        public float Amount { get; private set; }
        public bool Busy { get; private set; }
        public Vector3? StreamTo { get; set; }

        Renderer[] renderers;
        Light[] lights;
        float[] bases;
        MaterialPropertyBlock block;
        float from, to, start, duration;

        void Awake() => Collect();

        void Collect()
        {
            if (renderers != null)
                return;
            renderers = GetComponentsInChildren<Renderer>(true);
            lights = GetComponentsInChildren<Light>(true);
            block = new MaterialPropertyBlock();
            TakeBases();
        }

        void TakeBases()
        {
            bases = new float[lights.Length];
            for (int i = 0; i < lights.Length; i++)
                bases[i] = lights[i].TryGetComponent<LightFlicker>(out var f) ? f.BaseIntensity : lights[i].intensity;
        }

        public void Out(float seconds) => Run(1f, seconds);
        public void In(float seconds) => Run(0f, seconds);

        public void Set(float amount)
        {
            Collect();
            if (Amount <= 0f && amount > 0f)
                TakeBases();
            Busy = false;
            Amount = amount;
            Apply();
        }

        void Run(float target, float seconds)
        {
            Collect();
            if (Amount <= 0f)
                TakeBases(); // (fully there: its lights are at their own brightness now)
            from = Amount;
            to = target;
            start = Time.time;
            duration = Mathf.Max(0.01f, seconds);
            Busy = true;
        }

        void Update()
        {
            if (!Busy)
                return;
            float t = Mathf.Clamp01((Time.time - start) / duration);
            Amount = Mathf.Lerp(from, to, UiKit.Smooth(0f, 1f, t));
            if (Amount > 0.02f && Amount < 0.98f && Effects.Instance != null && Random.value < 0.6f)
            {
                Vector3 at = transform.position + Vector3.up * Random.Range(0.2f, 1.1f) + Random.insideUnitSphere * 0.35f;
                if (StreamTo is Vector3 target)
                    Effects.Instance.Wisp(at, wisp, (target - at).normalized * Random.Range(1.6f, 2.6f) + Vector3.up * 0.3f, Random.Range(2.2f, 3.2f));
                else
                    Effects.Instance.Wisp(at, wisp);
            }
            Apply();
            if (t >= 1f)
                Busy = false;
        }

        void Apply()
        {
            for (int i = 0; i < lights.Length; i++) // its light goes with it
            {
                float intensity = bases[i] * (1f - Amount);
                if (lights[i].TryGetComponent<LightFlicker>(out var flicker))
                    flicker.BaseIntensity = intensity;
                else
                    lights[i].intensity = intensity;
            }
            foreach (var r in renderers)
            {
                if (Amount <= 0f)
                {
                    r.SetPropertyBlock(null);
                    continue;
                }
                block.Clear();
                block.SetFloat("_Dissolve", Mathf.Lerp(0.02f, 1f, Amount));
                block.SetColor("_DissolveEdgeColor", edge);
                r.SetPropertyBlock(block);
            }
        }
    }
}
