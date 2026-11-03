using UnityEngine;

namespace MyGame
{
    // A Boss's fall, made a moment of (every Boss: the Anchoress, the Unparted, the Watchman, Father Moth, the Widow, the
    // One Who Waits, the Hold): the killing blow holds, time slows and eases back, the screen shakes, and the camera closes
    // in on it as it goes; then back to the Apprentice. For a Boss of parts (the Unparted), the blow that fells the last of
    // them. A Soured Boss gets it by falling; a Boss that isn't one (the Hold) calls Moment as it dies.
    public static class BossFall
    {
        public static int Fallen { get; private set; } // (tests) how many falls it has framed
        public static bool Framing => runner != null && runner.on;

        static Runner runner;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Hook()
        {
            Soured.Fell -= OnFell;
            Soured.Fell += OnFell;
        }

        static void OnFell(Soured s)
        {
            if (!Final(s))
                return;
            Moment(s.transform, s.deathDuration * 0.45f + s.dissolveDuration + 0.3f);
        }

        // The moment of a Boss's fall: on `at` (where the camera closes in), for `seconds`.
        public static void Moment(Transform at, float seconds)
        {
            Fallen++;
            HitStop.Trigger(0.35f);
            HitStop.Slow(0.3f, 2.4f);
            CameraRig.Instance?.Shake(0.6f);
            CameraRig.Instance?.SetFightFocus(at, 0.55f); // (on it, as it goes)
            CameraRig.Instance?.SetZoom(0.85f);
            if (runner == null)
                runner = new GameObject("Boss Fall").AddComponent<Runner>();
            runner.Frame(seconds);
        }

        // A Boss itself; or a part of the Boss now fought, the last of its parts to fall.
        static bool Final(Soured s)
        {
            if (s is IBoss)
                return true;
            if (BossFight.Current is IBossParts parts && parts.Owns(s))
            {
                foreach (var (_, fraction) in parts.Parts)
                    if (fraction > 0f)
                        return false;
                return true;
            }
            return false;
        }

        sealed class Runner : MonoBehaviour
        {
            public bool on;
            float until;

            public void Frame(float seconds)
            {
                on = true;
                until = Time.time + seconds;
            }

            void Update()
            {
                if (!on || Time.time < until)
                    return;
                on = false; // (gone: the camera back to the Apprentice)
                CameraRig.Instance?.SetFightFocus(null, 0f);
                CameraRig.Instance?.SetZoom(1f);
            }
        }
    }
}
