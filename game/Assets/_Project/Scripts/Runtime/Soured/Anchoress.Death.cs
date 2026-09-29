using UnityEngine;

namespace MyGame
{
    // The Anchoress lets go. The killing blow holds, time slows and the camera closes in on her; she sinks to her knees
    // over the anchor, and the chain snaps, its links scattering over the floor. Her lantern gutters out; the souls she
    // kept waiting rise out of her as she fades (and her fare streams into the Apprentice's lantern); "ANCHOR
    // DOWN...ed", and beneath it, quietly, "She let go."
    // Last, the Ferryman's hook rises out of her in his cold blue light and comes to rest on his mark: take it.
    public sealed partial class Anchoress
    {
        [Header("Her death")]
        public string farewell = "ANCHOR DOWN...|ed"; // the game's joke (the "ed" a beat late)...
        public string farewellLine = "She let go.";     // ...and the truth, beneath it
        public Transform rewardSpot;           // where his hook comes to rest: his mark, at the chapel's heart
        public AudioClip[] chainSnapSound, hookRestSound;

        public string Farewell => farewell;
        public string FarewellLine => farewellLine;
        public FerrymansHook FreedHook { get; private set; }

        static readonly Color Soul = new Color(0.72f, 0.86f, 1f, 1f);
        bool chainSnapped, hookFreed, released;
        float nextSoul;
        Light[] lanternLights;
        float[] lanternIntensity;

        void DeathEntered()
        {
            HitStop.Trigger(0.35f);
            HitStop.Slow(0.3f, 2.4f);
            CameraRig.Instance?.Shake(0.6f);
            CameraRig.Instance?.SetFightFocus(transform, 0.55f); // on her, as she goes
            CameraRig.Instance?.SetZoom(0.85f);
            chainSnapped = hookFreed = released = false;
            lanternLights = anchorBone != null ? anchorBone.GetComponentsInChildren<Light>() : new Light[0];
            lanternIntensity = System.Array.ConvertAll(lanternLights, l => l.intensity);
        }

        void DeathTick()
        {
            float t = StateTime, fadeFrom = deathDuration * 0.45f;
            float fading = Mathf.Clamp01((t - fadeFrom) / dissolveDuration);
            if (!chainSnapped && t >= deathDuration * 0.35f)
            {
                chainSnapped = true; // as she hits the floor
                anchorChain?.Snap(chainStart != null ? chainStart.position : transform.position, transform.position.y);
                Sfx.Instance?.Attack(chainSnapSound, transform.position + Vector3.up, 1f, "chain_snap");
                CameraRig.Instance?.Shake(0.3f);
            }
            // Her lantern gutters out.
            for (int i = 0; i < lanternLights.Length; i++)
                if (lanternLights[i] != null)
                {
                    var flicker = lanternLights[i].GetComponent<LightFlicker>();
                    float lit = (1f - fading) * (1f - fading) * (0.7f + 0.3f * Mathf.Sin(Time.time * 31f));
                    if (flicker != null)
                        flicker.BaseIntensity = lanternIntensity[i] * lit;
                    else
                        lanternLights[i].intensity = lanternIntensity[i] * lit;
                }
            // The souls she kept, rising out of her as she fades.
            if (fading > 0f && fading < 1f && Time.time >= nextSoul)
            {
                nextSoul = Time.time + 0.015f;
                var body = GetComponentInChildren<SkinnedMeshRenderer>().bounds;
                Effects.Instance?.Wisp(body.center + Vector3.Scale(body.extents * 0.8f, Random.insideUnitSphere), Soul);
            }
            if (!hookFreed && fading >= 0.6f)
            {
                hookFreed = true;
                Vector3 spot = rewardSpot != null ? rewardSpot.position : SpawnPosition;
                FreedHook = FerrymansHook.Free(hookModel, chainStart != null ? chainStart.position : transform.position + Vector3.up, spot, hookRestSound);
                BossFight.Defeat(this); // her farewell on screen
            }
            if (!released && fading >= 0.97f)
            {
                released = true; // gone: the camera comes back to the Apprentice
                CameraRig.Instance?.SetFightFocus(null, cameraLean);
                CameraRig.Instance?.SetZoom(1f);
            }
        }
    }
}
