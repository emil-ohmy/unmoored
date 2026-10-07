using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    // A mill-race: a channel of shallow water running down to a wheel. The Stroke let go in it, down it (the wave
    // carrying water), sends a surge the length of the race, however long: a crest of foam running down it, which
    // breaks over the spout onto the wheel and turns it. Let go up it, across it, or from outside it: nothing more than
    // any wave. (It's shallows too: a Shallows sits on the same water.)
    public sealed class MillRace : MonoBehaviour
    {
        public Transform head, mouth;     // its upper end and its spout (the wave runs head -> mouth)
        public float width = 1f;
        public float surgeSpeed = 10f;
        public WaterWheel wheel;
        public AudioClip[] surgeSound;

        public int Surges { get; private set; } // (tests)
        public bool Surging => surges.Count > 0;

        static readonly List<MillRace> all = new List<MillRace>();

        class Surge
        {
            public float at;               // how far along (from the head)
            public Mesh crest;
            public Transform body;
        }
        readonly List<Surge> surges = new List<Surge>();

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        Vector3 Down => Flat(mouth.position - head.position).normalized;
        float Length => Flat(mouth.position - head.position).magnitude;

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        // A wave just let go: carried down a race it was let go in, aimed down it.
        public static bool Carry(StrokeWave wave)
        {
            if (wave == null || !wave.Wet)
                return false;
            foreach (var race in all)
                if (race.Takes(wave.Origin, wave.Forward))
                {
                    race.Begin(wave.Origin);
                    return true;
                }
            return false;
        }

        // Whether a wave let go at `at`, aimed `forward`, runs down this race.
        public bool Takes(Vector3 at, Vector3 forward)
        {
            Vector3 down = Down, from = Flat(at - head.position);
            float along = Vector3.Dot(from, down), across = Mathf.Abs(Vector3.Dot(from, Vector3.Cross(Vector3.up, down)));
            return along > -0.6f && along < Length && across < width * 0.5f + 0.5f && Mathf.Abs(at.y - head.position.y) < 1.2f
                   && Vector3.Angle(Flat(forward), down) < 40f;
        }

        void Begin(Vector3 at)
        {
            var go = new GameObject("Race Surge", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(transform, true);
            var mesh = new Mesh { name = "Surge" };
            float w = width * 0.5f;
            mesh.vertices = new[] { new Vector3(-w, 0f, -0.45f), new Vector3(-w, 0f, 0.45f), new Vector3(w, 0f, 0.45f), new Vector3(w, 0f, -0.45f) };
            mesh.uv = new[] { new Vector2(0.02f, 0.5f), new Vector2(0.98f, 0.5f), new Vector2(0.98f, 0.5f), new Vector2(0.02f, 0.5f) };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            mesh.RecalculateBounds();
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = Effects.Instance != null ? Effects.Instance.sparkMaterial : null;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            surges.Add(new Surge { at = Mathf.Max(0f, Vector3.Dot(Flat(at - head.position), Down)), crest = mesh, body = go.transform });
            Surges++;
            Sfx.Instance?.Attack(surgeSound, at, 0.9f, "race_surge");
        }

        void Update()
        {
            if (surges.Count == 0)
                return;
            Vector3 down = Down;
            float length = Length, dt = Time.deltaTime;
            for (int i = surges.Count - 1; i >= 0; i--)
            {
                var s = surges[i];
                s.at += surgeSpeed * dt;
                Vector3 p = head.position + down * Mathf.Min(s.at, length) + Vector3.up * 0.04f;
                s.body.SetPositionAndRotation(p, Quaternion.LookRotation(down));
                var fx = Effects.Instance;
                if (fx != null)
                    for (int k = 0; k < 2; k++)
                        fx.Streak(p + Vector3.Cross(Vector3.up, down) * Random.Range(-width * 0.45f, width * 0.45f),
                                  down * surgeSpeed * 0.4f + Vector3.up * Random.Range(1f, 2.2f), new Color(0.85f, 0.93f, 1f, 1f), 0.25f, 0.07f);
                if (s.at >= length)
                {
                    // Over the spout and onto the wheel.
                    if (fx != null)
                    {
                        fx.Splash(mouth.position + down * 0.5f + Vector3.down * 0.3f);
                        for (int k = 0; k < 16; k++)
                            fx.Streak(mouth.position, down * Random.Range(2f, 4f) + Random.insideUnitSphere * 1.5f + Vector3.up * 1.2f,
                                      new Color(0.85f, 0.93f, 1f, 1f), Random.Range(0.3f, 0.5f), 0.08f);
                    }
                    wheel?.Turn();
                    Destroy(s.body.gameObject);
                    surges.RemoveAt(i);
                }
            }
        }
    }
}
