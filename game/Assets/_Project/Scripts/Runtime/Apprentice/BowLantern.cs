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

        public float Power { get; private set; }

        LightFlicker flicker;
        float pulse;

        // A brief flare (a defeated Soured's fare arriving).
        public void Pulse(float amount) => pulse = Mathf.Min(pulse + amount, 4f);

        void Start()
        {
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
            Apply();
        }

        void Apply()
        {
            if (lanternLight == null)
                return;
            lanternLight.range = Mathf.Lerp(minRange, maxRange, Power);
            float intensity = Mathf.Lerp(minIntensity, maxIntensity, Power) + pulse;
            if (flicker != null)
                flicker.BaseIntensity = intensity;
            else
                lanternLight.intensity = intensity;
        }
    }
}
