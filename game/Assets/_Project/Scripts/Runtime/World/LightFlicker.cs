using UnityEngine;

namespace MyGame
{
    // Makes a point light breathe like a flame: smooth noise on intensity plus a small positional wobble.
    [RequireComponent(typeof(Light))]
    public sealed class LightFlicker : MonoBehaviour
    {
        [Range(0f, 1f)] public float amount = 0.25f;
        public float speed = 7f;
        public float wobble = 0.03f;

        Light lamp;
        float baseIntensity;
        Vector3 basePosition;
        float seed;

        void Awake()
        {
            lamp = GetComponent<Light>();
            baseIntensity = lamp.intensity;
            basePosition = transform.localPosition;
            seed = Random.value * 100f;
        }

        void Update()
        {
            float t = Time.time * speed + seed;
            float n = Mathf.PerlinNoise(t, seed) * 2f - 1f;
            lamp.intensity = baseIntensity * (1f + n * amount);
            transform.localPosition = basePosition + new Vector3(
                Mathf.PerlinNoise(seed, t) - 0.5f, Mathf.PerlinNoise(t, t) - 0.5f, Mathf.PerlinNoise(seed + 5f, t) - 0.5f) * (2f * wobble);
        }

        // Lets other components (the bow lantern) change the steady brightness without fighting the flicker.
        public float BaseIntensity
        {
            get => baseIntensity;
            set => baseIntensity = value;
        }
    }
}
