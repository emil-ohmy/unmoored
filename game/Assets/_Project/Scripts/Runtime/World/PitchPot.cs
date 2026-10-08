using UnityEngine;

namespace MyGame
{
    // A Pitch Pot, thrown (a Provision): it arcs to where it was aimed and bursts, or breaks on the first Soured in its
    // way. The burst scorches whatever's close and leaves a pool of burning pitch; any Soured standing in it burns
    // (Burning: no guard stops fire). Over water there's nothing to burn on: it just sinks.
    public sealed class PitchPot : MonoBehaviour, IWaveable
    {
        // The Stroke's wave carrying water puts its fire out.
        public Vector3 WavePoint => transform.position;
        public float WaveRadius => Radius;
        public void TakeWave(StrokeWave wave, Vector3 push)
        {
            if (!wave.Wet || !Aflame)
                return;
            burstAt = Time.time - PoolTime; // (out: what's left fades)
            wave.Steam(transform.position + Vector3.up * 0.3f);
        }

        void OnEnable() => Waveables.Register(this);
        void OnDisable() => Waveables.Unregister(this);

        public const float Radius = 1.7f, PoolTime = 5f, FlightTime = 0.55f, ArcHeight = 1.4f;
        public const float BurstDamage = 12f, BurnPerSecond = 10f;

        Vector3 from, to;
        float t, burstAt = -1f, nextCatch, emitDebt;
        bool overWater;
        Transform model;
        Light glow;

        public bool Burst => burstAt >= 0f;
        public bool Aflame => Burst && Time.time - burstAt < PoolTime;
        public Vector3 Centre => to;

        // Thrown from `at` towards `target`: it lands on the ground there (the first floor under it).
        public static PitchPot Throw(GameObject modelPrefab, Vector3 at, Vector3 target)
        {
            var pot = new GameObject("Pitch Pot").AddComponent<PitchPot>();
            pot.from = at;
            pot.to = target;
            pot.overWater = true;
            // (a floor under it, not far below: from a little above, so it doesn't land on a roof it's thrown under)
            foreach (var hit in Physics.RaycastAll(target + Vector3.up * 1.2f, Vector3.down, 4f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<Soured>() != null || hit.collider.GetComponentInParent<Apprentice>() != null)
                    continue;
                if (pot.overWater || hit.point.y > pot.to.y)
                    pot.to = hit.point;
                pot.overWater = false;
            }
            pot.transform.position = at;
            if (modelPrefab != null)
            {
                var m = Instantiate(modelPrefab, pot.transform);
                foreach (var c in m.GetComponentsInChildren<Collider>())
                    Destroy(c);
                m.transform.localPosition = Vector3.zero;
                m.transform.localScale = Vector3.one * 0.8f;
                pot.model = m.transform;
            }
            return pot;
        }

        void Update()
        {
            if (!Burst)
            {
                t += Time.deltaTime / FlightTime;
                Vector3 p = Vector3.Lerp(from, to, Mathf.Min(t, 1f)) + Vector3.up * (ArcHeight * 4f * Mathf.Min(t, 1f) * (1f - Mathf.Min(t, 1f)));
                transform.position = p;
                if (model != null)
                    model.Rotate(new Vector3(420f, 90f, 0f) * Time.deltaTime, Space.Self);
                // A Soured in the way: it breaks on them.
                foreach (var s in WorldState.AllSoured)
                    if (s != null && s.IsAlive && Flat(s.transform.position - p).magnitude < s.radius + 0.2f && p.y - s.transform.position.y < 2f && t > 0.15f)
                    {
                        to = s.transform.position;
                        overWater = false;
                        Break();
                        return;
                    }
                if (t >= 1f)
                    Break();
                return;
            }
            float since = Time.time - burstAt;
            if (overWater || since >= PoolTime + 0.6f)
            {
                Destroy(gameObject);
                return;
            }
            float strength = since < PoolTime ? 1f : 1f - (since - PoolTime) / 0.6f;
            glow.intensity = (5f + 1.6f * Mathf.PerlinNoise(Time.time * 8f, 0.5f)) * strength;
            // Flames across the pool, thicker in the middle.
            emitDebt += Time.deltaTime * 150f * strength;
            for (; emitDebt >= 1f; emitDebt -= 1f)
            {
                Vector2 d = Random.insideUnitCircle * Radius * 0.95f;
                float middle = 1.2f - d.magnitude / Radius * 0.6f;
                Effects.Instance?.Flame(to + new Vector3(d.x, 0.05f, d.y), Vector3.up * Random.Range(0.8f, 2.2f) * middle,
                    Random.Range(0.35f, 0.7f), Random.Range(0.25f, 0.5f) * middle);
            }
            if (since < PoolTime && Time.time >= nextCatch)
            {
                nextCatch = Time.time + 0.25f;
                foreach (var s in WorldState.AllSoured)
                    if (InPool(s))
                        Burning.Set(s, 1.2f, BurnPerSecond); // (and on, a moment, after stepping out)
                CatchApprentice(0.25f); // (yours burns you too: careful where you throw it; staying in it, longer)
            }
        }

        // The Apprentice standing in it: alight.
        Apprentice apprentice;

        void CatchApprentice(float more)
        {
            if (apprentice == null)
                apprentice = FindAnyObjectByType<Apprentice>();
            var a = apprentice;
            if (a != null && a.IsAlive && Flat(a.transform.position - to).magnitude < Radius && a.transform.position.y - to.y > -1.2f && a.transform.position.y - to.y < 1.2f)
                a.SetAlight(more);
        }

        public bool InPool(Soured s) =>
            s != null && s.IsAlive && Flat(s.transform.position - to).magnitude < Radius + s.radius * 0.5f
            && s.transform.position.y - to.y > -1.2f && s.transform.position.y - to.y < (s.Flies ? 3.2f : 1.2f); // (the flames reach up to a Moth)

        void Break()
        {
            burstAt = Time.time;
            transform.position = to;
            if (model != null)
                model.gameObject.SetActive(false);
            if (overWater)
            {
                Effects.Instance?.Splash(to);
                Sfx.Instance?.Splash(to);
                return;
            }
            Sfx.Instance?.PitchBurst(to + Vector3.up * 0.3f);
            CameraRig.Instance?.Shake(0.12f);
            for (int i = 0; i < 40; i++)
            {
                Vector3 out_ = Random.onUnitSphere;
                out_.y = Mathf.Abs(out_.y) * 0.8f + 0.2f;
                Effects.Instance?.Flame(to + Vector3.up * 0.2f, out_ * Random.Range(2f, 5f), Random.Range(0.25f, 0.5f), Random.Range(0.2f, 0.4f));
            }
            foreach (var s in WorldState.AllSoured)
                if (InPool(s))
                {
                    s.TakeBurn(BurstDamage, to);
                    Burning.Set(s, 1.2f, BurnPerSecond);
                }
            // Lit from above, a pool of firelight on the ground the size of the pitch (and a little round it).
            glow = new GameObject("Pitch Light").AddComponent<Light>();
            glow.transform.SetParent(transform, false);
            glow.transform.localPosition = Vector3.up * 2.4f;
            glow.transform.rotation = Quaternion.LookRotation(Vector3.down);
            glow.type = LightType.Spot;
            glow.spotAngle = 2f * Mathf.Atan((Radius + 0.9f) / 2.4f) * Mathf.Rad2Deg;
            glow.innerSpotAngle = glow.spotAngle * 0.6f;
            glow.color = new Color(1f, 0.55f, 0.2f);
            glow.range = 5f;
            glow.shadows = LightShadows.None;
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
