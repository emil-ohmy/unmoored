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
        public float sinkTime = 1.3f;
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
            public Vector3 centre, extents, hinge, axis;
            public bool broken, gaveWay;
            public float since;
            public int aftershocks;
        }

        sealed class Chunk
        {
            public Transform transform;
            public Vector3 velocity, spin;
            public bool splashed;
        }

        readonly List<Section> sections = new List<Section>();
        readonly List<Chunk> chunks = new List<Chunk>();
        float nextGrit;

        public int Broken { get; private set; }
        public int Sections => sections.Count;
        public int FallingChunks => chunks.Count;

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
                    extents = renderer != null ? renderer.bounds.extents : Vector3.one,
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
            s.gaveWay = false;
            s.aftershocks = 0;
            s.since = Time.time;
            // It tips like a trapdoor about one of its edges (towards the middle, or away), then slides under.
            Vector3 radial = Flat(s.centre - transform.position);
            radial = radial.sqrMagnitude > 0.01f ? radial.normalized : Vector3.forward;
            float side = Random.value < 0.5f ? -1f : 1f;
            float reach = Mathf.Max(s.extents.x, s.extents.z) * 0.8f;
            s.hinge = s.centre + radial * (side * reach) + Vector3.up * s.extents.y;
            s.axis = Vector3.Cross(Vector3.up, radial) * -side;
            Broken++;
            Sfx.Instance?.Attack(crackSound, s.centre, 1f, "floor_crack");
            CameraRig.Instance?.Shake(0.12f);
            return true;
        }

        public void Restore()
        {
            foreach (var s in sections)
            {
                s.broken = s.gaveWay = false;
                s.transform.localPosition = s.home;
                s.transform.localRotation = s.homeRotation;
                if (s.collider != null)
                    s.collider.enabled = true;
                if (s.renderer != null)
                    s.renderer.enabled = true;
            }
            foreach (var c in chunks)
                if (c.transform != null)
                    Destroy(c.transform.gameObject);
            chunks.Clear();
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
                    Shudder(s, t / crumbleTime);
                else
                    Fall(s, t - crumbleTime);
            }
            UpdateChunks(Time.deltaTime);
        }

        // The warning: a shudder growing into a rumble you feel (the camera too), grit kicked up from its seams.
        void Shudder(Section s, float t)
        {
            float shake = 0.04f * t;
            s.transform.localPosition = s.home + new Vector3(Random.Range(-shake, shake), -0.04f * t, Random.Range(-shake, shake));
            s.transform.localRotation = s.homeRotation;
            CameraRig.Instance?.Shake(0.05f + 0.12f * t);
            if (Time.time >= nextGrit)
            {
                nextGrit = Time.time + 0.03f;
                Effects.Instance?.Grit(RandomPointOn(s, 0.05f));
            }
        }

        // It gives way: a heavy jolt, dust, chunks of it breaking off; it tips on its hinge, drops and slides under
        // the water with a splash, the water churning after it.
        void Fall(Section s, float t)
        {
            if (!s.gaveWay)
            {
                s.gaveWay = true;
                if (s.collider != null)
                    s.collider.enabled = false;
                Sfx.Instance?.Attack(breakSound, s.centre, 1f, "floor_break");
                CameraRig.Instance?.Shake(0.5f);
                HitStop.Trigger(0.05f);
                Effects.Instance?.Rubble(s.centre + Vector3.up * s.extents.y, Mathf.Max(s.extents.x, s.extents.z) * 0.7f);
                int count = Random.Range(10, 15);
                for (int i = 0; i < count; i++)
                    SpawnChunk(s);
            }
            float f = Mathf.Clamp01(t / sinkTime);
            s.transform.localPosition = s.home;
            s.transform.localRotation = s.homeRotation;
            s.transform.RotateAround(s.hinge, s.axis, 58f * UiKit.Smooth(0f, 0.55f, f)); // tipping over its edge
            s.transform.position += Vector3.down * (3.2f * f * f);                       // and down, gathering speed
            if (s.aftershocks < 3 && t >= 0.25f + s.aftershocks * 0.3f)
            {
                // Hitting the water, then the water closing over it.
                Vector3 at = RandomPointOn(s, 0f);
                Effects.Instance?.Splash(new Vector3(at.x, 0.05f, at.z));
                if (s.aftershocks == 0)
                {
                    Sfx.Instance?.Splash(new Vector3(s.centre.x, 0.05f, s.centre.z));
                    Shockwave.Spread(new Vector3(s.centre.x, 0f, s.centre.z), null, 4.5f, 5f, 0.5f, 0f, 0f, new Color(0.55f, 0.72f, 0.8f, 0.6f));
                    CameraRig.Instance?.Shake(0.25f);
                }
                s.aftershocks++;
            }
            if (f >= 1f)
                s.renderer.enabled = false;
        }

        void SpawnChunk(Section s)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(go.GetComponent<Collider>());
            go.name = "Rubble";
            go.transform.SetPositionAndRotation(RandomPointOn(s, 0.05f), Random.rotation);
            go.transform.localScale = new Vector3(Random.Range(0.18f, 0.45f), Random.Range(0.1f, 0.25f), Random.Range(0.18f, 0.45f));
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = s.renderer.sharedMaterials[Random.Range(0, s.renderer.sharedMaterials.Length)];
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            chunks.Add(new Chunk
            {
                transform = go.transform,
                velocity = new Vector3(Random.Range(-1.2f, 1.2f), Random.Range(1f, 3.5f), Random.Range(-1.2f, 1.2f)),
                spin = Random.insideUnitSphere * 540f,
            });
        }

        void UpdateChunks(float dt)
        {
            for (int i = chunks.Count - 1; i >= 0; i--)
            {
                var c = chunks[i];
                if (c.transform == null)
                {
                    chunks.RemoveAt(i);
                    continue;
                }
                c.velocity += Vector3.down * 18f * dt;
                c.transform.position += c.velocity * dt;
                c.transform.rotation = Quaternion.Euler(c.spin * dt) * c.transform.rotation;
                if (!c.splashed && c.transform.position.y < 0.05f && c.velocity.y < 0f)
                {
                    c.splashed = true;
                    c.velocity *= 0.3f; // into the water: it slows as it sinks
                    if (i % 2 == 0)
                        Effects.Instance?.Splash(new Vector3(c.transform.position.x, 0.05f, c.transform.position.z));
                }
                if (c.transform.position.y < -1.5f)
                {
                    Destroy(c.transform.gameObject);
                    chunks.RemoveAt(i);
                }
            }
        }

        static Vector3 RandomPointOn(Section s, float above) =>
            s.centre + new Vector3(Random.Range(-s.extents.x, s.extents.x) * 0.75f, s.extents.y + above, Random.Range(-s.extents.z, s.extents.z) * 0.75f);

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
