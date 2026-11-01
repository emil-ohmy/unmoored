using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    // What a trap of the Hall of the Last Hour does to whoever it catches (its machinery moving again: a pendulum's
    // blade, the rolling gear, a jack's hammer, the grinding gears' mesh): a heavy blow and a throw. No guard stops
    // machinery: only a dodge slips it. It doesn't care whose it is: a Soured lured into one takes it just the same.
    public sealed class TrapBlows
    {
        public float harm = 34f;          // the Apprentice's (a third of them, new)
        public float souredHarm = 45f;
        public float stagger = 1.0f;      // a Soured's (seconds)
        public float every = 0.8f;        // the same body, caught again no sooner

        public int Landed { get; private set; }        // (tests) on anyone
        public int OnApprentice { get; private set; }
        public int OnSoured { get; private set; }

        readonly Dictionary<Object, float> last = new Dictionary<Object, float>();
        Apprentice apprentice;

        public const float Height = 1.8f; // (a body's, from its feet: what a trap can reach)
        const float ApprenticeRadius = 0.35f;

        // Whoever's caught: `inside(feet, radius)` says whether a body standing there is; `push(feet)` is how they're
        // thrown (a knockback velocity, flat).
        public void Sweep(System.Func<Vector3, float, bool> inside, System.Func<Vector3, Vector3> push)
        {
            if (apprentice == null)
                apprentice = Object.FindAnyObjectByType<Apprentice>();
            if (apprentice != null && apprentice.IsAlive && Ready(apprentice) && inside(apprentice.transform.position, ApprenticeRadius))
            {
                last[apprentice] = Time.time;
                var result = apprentice.TakeHit(harm, Flat(push(apprentice.transform.position)), null, blockable: false);
                if (result != Defence.Dodged)
                {
                    Landed++;
                    OnApprentice++;
                }
            }
            foreach (var s in new List<Soured>(WorldState.AllSoured)) // (one may fall as it's struck)
            {
                if (s == null || !s.IsAlive || !Ready(s) || !inside(s.transform.position, s.radius))
                    continue;
                last[s] = Time.time;
                Vector3 at = s.transform.position;
                if (s.TakeTrap(Flat(push(at)), souredHarm, at - Flat(push(at)).normalized, stagger) == HitResult.Landed)
                {
                    Landed++;
                    OnSoured++;
                    Sfx.Instance?.OarHit(at + Vector3.up * 1.1f, true);
                }
            }
        }

        bool Ready(Object body) => !last.TryGetValue(body, out float t) || Time.time - t >= every;

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        // Whether a point (with `reach` about it) touches a body standing at `feet`: an upright capsule, `radius` round.
        public static bool Touches(Vector3 point, float reach, Vector3 feet, float radius)
        {
            var axis = new Vector3(feet.x, Mathf.Clamp(point.y, feet.y + radius, feet.y + Height - radius), feet.z);
            return (point - axis).sqrMagnitude <= (reach + radius) * (reach + radius);
        }

        // A looping sound on `host`, heard near it (a trap's running noise).
        public static AudioSource LoopSource(GameObject host, float maxDistance = 24f)
        {
            var source = host.AddComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 3f;
            source.maxDistance = maxDistance;
            source.dopplerLevel = 0f;
            return source;
        }

        // Keeps a loop playing at `volume` (0: stopped).
        public static void Keep(AudioSource source, AudioClip[] clips, float volume, float pitch = 1f)
        {
            if (source == null)
                return;
            if (volume <= 0.01f || clips == null || clips.Length == 0 || clips[0] == null)
            {
                if (source.isPlaying)
                    source.Stop();
                return;
            }
            if (source.clip != clips[0])
                source.clip = clips[0];
            source.volume = volume * Settings.Effects;
            source.pitch = pitch;
            if (!source.isPlaying)
                source.Play();
        }
    }

    // Left out of the Soured's ways (NavMeshBaker): something that moves (a trap), whose shape when the scene loads
    // isn't where the ways should go.
    public sealed class NavMeshIgnore : MonoBehaviour { }
}
