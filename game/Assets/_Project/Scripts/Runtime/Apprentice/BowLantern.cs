using UnityEngine;

namespace MyGame
{
    // The bow lantern, carried at the Apprentice's hip: a cold, pale light that grows with every Mooring post
    // lit (see docs/brief.md). Fully powered, it will one day unlock the secret ending.
    public sealed class BowLantern : MonoBehaviour
    {
        public Light lanternLight;
        public float minRange = 3.5f, maxRange = 8f;
        public float minIntensity = 2.2f, maxIntensity = 4f;
        public float easeSpeed = 1.5f; // how quickly it swells after a post is lit
        public float wickRange = 1.5f, wickIntensity = 1.35f; // Hesper's Wick fitted

        public float Power { get; private set; }

        LightFlicker flicker;
        Equipment equipment;
        bool Wick => equipment != null && equipment.IsFitted(Charm.HespersWick);
        public float Range => lanternLight != null ? lanternLight.range : 0f;
        float pulse;
        float snuffAt = -1f, snuffTime = 1f, nextMote; // going out (the Apprentice has fallen)

        public float Lit { get; private set; } = 1f; // 1 burning, 0 out

        // The Apprentice falls: a last flare, then after `delay` it gutters and goes out over `duration`, its light
        // drifting away as pale motes.
        public void Snuff(float delay, float duration)
        {
            Pulse(3f);
            snuffAt = Time.time + delay;
            snuffTime = duration;
        }

        public void Relight()
        {
            snuffAt = -1f;
            Lit = 1f;
        }

        // A brief flare (a defeated Soured's fare arriving).
        public void Pulse(float amount) => pulse = Mathf.Min(pulse + amount, 4f);

        void Start()
        {
            equipment = GetComponent<Equipment>();
            if (lanternLight != null)
                flicker = lanternLight.GetComponent<LightFlicker>();
            Power = MooringPost.LitFraction;
            Apply();
            if (lanternLight != null)
                Effects.Instance?.SetFareTarget(lanternLight.transform, this);
        }

        void Update()
        {
            Power = Mathf.MoveTowards(Power, MooringPost.LitFraction, easeSpeed * Time.deltaTime);
            pulse = Mathf.MoveTowards(pulse, 0f, 3f * Time.deltaTime);
            if (snuffAt >= 0f && Time.time >= snuffAt)
            {
                float t = Mathf.Clamp01((Time.time - snuffAt) / snuffTime);
                // Guttering: a quickening, ragged flutter as it fades.
                float gutter = 0.6f + 0.4f * Mathf.Sin(Time.time * (14f + 40f * t)) * Mathf.Sin(Time.time * 23f);
                Lit = (1f - t) * (1f - t) * Mathf.Lerp(1f, gutter, t);
                if (Time.time >= nextMote && t < 0.95f && lanternLight != null)
                {
                    nextMote = Time.time + 0.05f;
                    Effects.Instance?.Wisp(lanternLight.transform.position);
                }
            }
            Apply();
        }

        void Apply()
        {
            if (lanternLight == null)
                return;
            lanternLight.range = Mathf.Lerp(minRange, maxRange, Power) * (Wick ? wickRange : 1f);
            float intensity = (Mathf.Lerp(minIntensity, maxIntensity, Power) * (Wick ? wickIntensity : 1f) + pulse) * Lit;
            if (flicker != null)
                flicker.BaseIntensity = intensity;
            else
                lanternLight.intensity = intensity;
        }
    }
}
