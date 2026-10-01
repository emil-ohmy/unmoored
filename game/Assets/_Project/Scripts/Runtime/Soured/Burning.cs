using UnityEngine;

namespace MyGame
{
    // A Soured on fire (burning pitch, an Ember-tarred oar): it burns for a while, a little harm at a time that no guard
    // stops, with flames licking up it and a warm flicker of light. Set again, it burns on (the longer, the hotter).
    public sealed class Burning : MonoBehaviour
    {
        const float Tick = 0.5f;

        Soured soured;
        float until, perSecond, nextTick, emitDebt;
        Light glow;

        public bool Alight => Time.time < until;

        // Sets `soured` alight for `duration` seconds at `perSecond` harm (or keeps it burning, if it's hotter or longer).
        public static void Set(Soured soured, float duration, float perSecond)
        {
            if (soured == null || !soured.IsAlive)
                return;
            var burning = soured.GetComponent<Burning>() ?? soured.gameObject.AddComponent<Burning>();
            burning.until = Mathf.Max(burning.until, Time.time + duration);
            burning.perSecond = Mathf.Max(burning.Alight ? burning.perSecond : 0f, perSecond);
            if (burning.nextTick < Time.time)
                burning.nextTick = Time.time + Tick;
            burning.enabled = true;
        }

        void Awake()
        {
            soured = GetComponent<Soured>();
            glow = new GameObject("Burning Light").AddComponent<Light>();
            glow.transform.SetParent(transform, false);
            glow.transform.localPosition = Vector3.up * 1.1f;
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.55f, 0.2f);
            glow.range = 3.5f;
            glow.shadows = LightShadows.None;
        }

        void Update()
        {
            if (!Alight || soured == null || !soured.IsAlive)
            {
                glow.enabled = false;
                enabled = false;
                return;
            }
            glow.enabled = true;
            glow.intensity = 1.6f + 0.6f * Mathf.PerlinNoise(Time.time * 9f, 0.3f);
            emitDebt += Time.deltaTime * 30f;
            for (; emitDebt >= 1f; emitDebt -= 1f)
                Effects.Instance?.Flame(transform.position + new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(0.2f, 1.5f), Random.Range(-0.35f, 0.35f)),
                    Vector3.up * Random.Range(1.2f, 2.2f), Random.Range(0.3f, 0.55f), Random.Range(0.14f, 0.26f));
            if (Time.time >= nextTick)
            {
                nextTick = Time.time + Tick;
                soured.TakeBurn(perSecond * Tick, transform.position);
            }
        }
    }
}
