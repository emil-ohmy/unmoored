using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    // The drowned chapel's floor, whose sections (the model's Floor* objects) can break away into the water: in the
    // Anchoress's second phase her Anchor Drops break the section they land on. A breaking section shudders and throws
    // up grit for a moment (the warning: get off it), then gives way and sinks. Anyone on it falls in. Her middle and
    // the rim never break, and neither does the whole floor: a few sections are always left. Restored when the fight
    // resets (the Apprentice wakes at the Mooring post).
    public sealed class ChapelFloor : MonoBehaviour
    {
        public float crumbleTime = 0.6f; // the shudder before it gives way
        public float sinkTime = 1.2f;
        public int maxBroken = 8;
        public AudioClip[] crackSound;
        public AudioClip[] breakSound;

        sealed class Section
        {
            public Transform transform;
            public Collider collider;
            public Renderer renderer;
            public Vector3 home;
            public Quaternion homeRotation;
            public Vector3 centre, tilt;
            public bool broken;
            public float since;
        }

        readonly List<Section> sections = new List<Section>();
        float nextGrit;

        public int Broken { get; private set; }
        public int Sections => sections.Count;

        void Awake()
        {
            foreach (Transform child in GetComponentsInChildren<Transform>())
            {
                if (!child.name.StartsWith("Floor"))
                    continue;
                var renderer = child.GetComponent<Renderer>();
                sections.Add(new Section
                {
                    transform = child,
                    collider = child.GetComponent<Collider>(),
                    renderer = renderer,
                    home = child.localPosition,
                    homeRotation = child.localRotation,
                    centre = renderer != null ? renderer.bounds.center : child.position,
                });
            }
        }

        // Breaks the section under this point, if there is one left to break. True if it will give way.
        public bool BreakAt(Vector3 point)
        {
            var ray = new Ray(new Vector3(point.x, transform.position.y + 3f, point.z), Vector3.down);
            foreach (var s in sections)
                if (!s.broken && s.collider != null && s.collider.enabled && s.collider.Raycast(ray, out _, 6f))
                    return Break(s);
            return false;
        }

        // Breaks `count` sections of the outer ring, away from `avoid` (the Apprentice: the first breaks are a warning,
        // not a trap).
        public int BreakAwayFrom(Vector3 avoid, int count)
        {
            var candidates = sections.FindAll(s => !s.broken && s.transform.name.StartsWith("FloorC")
                && Flat(s.centre - avoid).magnitude > 4f);
            int done = 0;
            while (done < count && candidates.Count > 0)
            {
                var s = candidates[Random.Range(0, candidates.Count)];
                candidates.Remove(s);
                if (Break(s))
                    done++;
            }
            return done;
        }

        bool Break(Section s)
        {
            if (Broken >= maxBroken)
                return false;
            s.broken = true;
            s.since = Time.time;
            s.tilt = Random.onUnitSphere;
            Broken++;
            Sfx.Instance?.Attack(crackSound, s.centre, 1f, "floor_crack");
            return true;
        }

        public void Restore()
        {
            foreach (var s in sections)
            {
                s.broken = false;
                s.transform.localPosition = s.home;
                s.transform.localRotation = s.homeRotation;
                if (s.collider != null)
                    s.collider.enabled = true;
                if (s.renderer != null)
                    s.renderer.enabled = true;
            }
            Broken = 0;
        }

        void Update()
        {
            foreach (var s in sections)
            {
                if (!s.broken || s.renderer == null || !s.renderer.enabled)
                    continue;
                float t = Time.time - s.since;
                if (t < crumbleTime)
                {
                    // The warning: a shudder, grit kicked up from its seams.
                    float shake = 0.025f * (t / crumbleTime);
                    s.transform.localPosition = s.home + new Vector3(Random.Range(-shake, shake), -0.03f * t / crumbleTime, Random.Range(-shake, shake));
                    if (Time.time >= nextGrit)
                    {
                        nextGrit = Time.time + 0.04f;
                        Vector3 extents = s.renderer.bounds.extents * 0.8f;
                        Vector3 at = s.centre + new Vector3(Random.Range(-extents.x, extents.x), 0.25f, Random.Range(-extents.z, extents.z));
                        Effects.Instance?.Scrape(at, Vector3.up);
                    }
                    continue;
                }
                if (s.collider != null && s.collider.enabled)
                {
                    s.collider.enabled = false; // it gives way
                    Sfx.Instance?.Attack(breakSound, s.centre, 1f, "floor_break");
                    Effects.Instance?.Splash(new Vector3(s.centre.x, 0.05f, s.centre.z));
                    CameraRig.Instance?.Shake(0.15f);
                }
                float sink = Mathf.Clamp01((t - crumbleTime) / sinkTime);
                s.transform.localPosition = s.home + Vector3.down * (2.6f * sink * sink);
                s.transform.localRotation = Quaternion.AngleAxis(14f * sink, Vector3.Cross(Vector3.up, s.tilt).normalized + Vector3.right * 0.001f) * s.homeRotation;
                if (sink >= 1f)
                    s.renderer.enabled = false;
            }
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
