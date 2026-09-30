using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    // The lab's spawner: a copy of any kind of Soured, set down a few metres in front of the Apprentice (facing them),
    // as many as asked, aggressive (it knows you're there) or idle (a dummy that never fights back). The kinds are
    // templates kept switched off in the scene (MainSceneBuilder.Lab), so every copy is built exactly as in the world.
    // Opening a menu here (the Tester's Post, the Mooring post) resets every Soured to where it was set down, and they
    // leave you be until it's closed. Waking after dying, the same: they leave you be until you step off the spot (or
    // a few seconds pass).
    public sealed class LabSpawner : MonoBehaviour
    {
        [Serializable]
        public sealed class Kind
        {
            public string name;
            public GameObject template; // inactive; its dropped lantern (if any) inactive too
            public int group = 1;       // set down this many at a time
            public bool listed = true;  // a row of its own on the Spawn page (a miniboss is on the Formations page)
            public bool inWater;        // set down in the water nearest the Apprentice (a Drowned), not on the ground
        }

        public Kind[] kinds = new Kind[0];
        public float ahead = 7f, spacing = 1.8f;
        public float followRange = 80f; // set down here, a Soured follows you anywhere in the lab (up the heights too)

        public static readonly int[] Counts = { 1, 2, 3, 5 };
        public int Count { get; set; } = 1;
        public bool Aggressive { get; set; } = true;
        public IReadOnlyList<Soured> Spawned => spawned;

        readonly List<Soured> spawned = new List<Soured>();
        readonly List<GameObject> roots = new List<GameObject>();
        public float wakeGrace = 3f, wakeStep = 1.5f;
        public bool Holding => inMenu || wokeAt >= 0f;

        Apprentice apprentice;
        bool inMenu;
        float wokeAt = -1f;
        Vector3 wokeWhere;

        void Start()
        {
            apprentice = FindAnyObjectByType<Apprentice>();
            if (apprentice != null)
                apprentice.Woke += OnWoke;
        }

        void OnDestroy()
        {
            if (apprentice != null)
                apprentice.Woke -= OnWoke;
        }

        // Woken at the post: everything set down is back where it was (the waking re-forms them), and calm for now.
        void OnWoke()
        {
            wokeAt = Time.time;
            wokeWhere = apprentice.transform.position;
            foreach (var s in WorldState.AllSoured)
            {
                s.Reform();
                s.Held = true;
            }
        }

        void Update()
        {
            if (apprentice == null)
                return;
            if (wokeAt >= 0f && (Time.time - wokeAt > wakeGrace || Vector3.Distance(apprentice.transform.position, wokeWhere) > wakeStep))
            {
                wokeAt = -1f;
                if (!inMenu)
                    foreach (var s in WorldState.AllSoured)
                        s.Held = false;
            }
            bool now = apprentice.OpenMenu != null;
            if (now == inMenu)
                return;
            inMenu = now;
            foreach (var s in WorldState.AllSoured)
            {
                if (now)
                    s.Reform(); // back where it was set down, whole, calm
                s.Held = now || wokeAt >= 0f;
            }
        }

        public void NextCount() => Count = Counts[(Array.IndexOf(Counts, Count) + 1) % Counts.Length];

        // Ready-made mixes (the Tester's Formations page): kinds by name, set down at (right, ahead) of the Apprentice.
        public static readonly (string name, string description, (string kind, float right, float ahead)[] places)[] Formations =
        {
            ("The Unparted (miniboss)", "Hold, and Fast on its back. Scatter one first: the other won't take it well.",
                new[] { ("The Unparted", 0f, 8f) }),
            ("Shield and bow", "A Laden up front, an archer behind it: the quay's lesson.",
                new[] { ("Laden", 0f, 6f), ("Laden archer", 0f, 11f) }),
            ("The crossroads, doubled", "Two Laden holding the front, two archers covering them.",
                new[] { ("Laden", -1.8f, 6f), ("Laden", 1.8f, 6f), ("Laden archer", -3f, 11f), ("Laden archer", 3f, 11f) }),
            ("A Laden and a pair of Clingers", "The heap in front of you, the pair round the sides.",
                new[] { ("Laden", 0f, 7f), ("Clinger", -3f, 5f), ("Clinger", 3f, 5f) }),
            ("Bow and Clingers", "An archer at range while a pair wears you down.",
                new[] { ("Laden archer", 0f, 11f), ("Clinger", -2.5f, 6f), ("Clinger", 2.5f, 6f) }),
            ("Two pairs of Clingers", "Four of them, two pairs.",
                new[] { ("Clinger", -4f, 6f), ("Clinger", -2.5f, 7f), ("Clinger", 2.5f, 7f), ("Clinger", 4f, 6f) }),
            ("Everything", "A Laden, an archer and a pair of Clingers.",
                new[] { ("Laden", 0f, 7f), ("Laden archer", 0f, 12f), ("Clinger", -3f, 5f), ("Clinger", 3f, 5f) }),
        };

        // `Count` of this kind, each on a free spot on an arc in front of the Apprentice (clear of what's there already).
        public void Spawn(int kind, Apprentice near)
        {
            Frame(near, out Vector3 origin, out Vector3 forward);
            int count = Count * Mathf.Max(1, kinds[kind].group);
            for (int i = 0; i < count; i++)
                SpawnOne(kind, kinds[kind].inWater ? WaterSpot(near.transform.position) : FreeSpot(origin, forward), forward);
        }

        // (tests) One of a kind, set down right here, facing `facing`.
        public Soured SpawnAt(string kindName, Vector3 at, Vector3 facing)
        {
            int kind = Array.FindIndex(kinds, k => k.name == kindName);
            int before = spawned.Count;
            SpawnOne(kind, at, -Flat(facing).normalized);
            return spawned[before];
        }

        public void SpawnFormation(int formation, Apprentice near)
        {
            Frame(near, out Vector3 origin, out Vector3 forward);
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            foreach (var (kindName, r, a) in Formations[formation].places)
            {
                int kind = Array.FindIndex(kinds, k => k.name == kindName);
                if (kind >= 0)
                    SpawnOne(kind, Ground(origin + right * r + forward * a, origin), forward);
            }
        }

        static void Frame(Apprentice near, out Vector3 origin, out Vector3 forward)
        {
            origin = near.transform.position;
            forward = Flat(near.transform.forward).normalized;
            if (forward.sqrMagnitude < 0.01f)
                forward = Vector3.forward;
        }

        // A spot on standing ground in front of the Apprentice, a little way off, clear of everything set down so far:
        // along an arc at `ahead`, then further out, then nearer.
        Vector3 FreeSpot(Vector3 origin, Vector3 forward)
        {
            foreach (float radius in new[] { ahead, ahead + 2.5f, ahead + 5f, ahead - 2f })
                foreach (float angle in new[] { 0f, 15f, -15f, 30f, -30f, 45f, -45f, 60f, -60f, 80f, -80f })
                {
                    Vector3 p = Ground(origin + Quaternion.Euler(0f, angle, 0f) * forward * radius, origin);
                    if (Flat(p - origin).magnitude < 3f)
                        continue; // (the ground ran out: too close)
                    bool free = true;
                    foreach (var s in spawned)
                        if (s != null && Flat(s.transform.position - p).magnitude < spacing)
                            free = false;
                    if (free)
                        return p;
                }
            return Ground(origin + forward * ahead, origin);
        }

        // The open water nearest `origin` (just off an edge), clear of any already set down there.
        Vector3 WaterSpot(Vector3 origin)
        {
            for (float r = 1f; r <= 14f; r += 0.5f)
                for (int i = 0; i < 24; i++)
                {
                    Vector3 p = new Vector3(origin.x, 0f, origin.z) + Quaternion.Euler(0f, i * 15f, 0f) * Vector3.forward * r;
                    if (!Drowned.IsOpenWater(p, 0.45f))
                        continue;
                    bool free = true;
                    foreach (var s in spawned)
                        if (s != null && Flat(s.transform.position - p).magnitude < spacing * 1.5f)
                            free = false;
                    if (free)
                        return p;
                }
            return origin + Vector3.forward * 8f;
        }

        void SpawnOne(int kind, Vector3 spot, Vector3 forward)
        {
            var template = kinds[kind].template;
            var go = Instantiate(template, spot, template.transform.rotation);
            go.name = $"{kinds[kind].name} {roots.Count + 1}";
            // Face the Apprentice (turning about the vertical only: a rigged model's root keeps its own rotation).
            float yaw = Vector3.SignedAngle(Flat(go.transform.forward), -forward, Vector3.up);
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f) * go.transform.rotation;
            foreach (var soured in go.GetComponentsInChildren<Soured>(true)) // (one, or a miniboss's two)
            {
                // Its dropped lantern lives outside it: a copy of its own, carrying this one's light.
                if (soured.droppedLantern != null)
                {
                    var lantern = Instantiate(soured.droppedLantern);
                    lantern.carried = soured.GetComponentInChildren<Light>(true);
                    lantern.gameObject.SetActive(true);
                    soured.droppedLantern = lantern;
                }
                soured.passive = !Aggressive;
                if (Aggressive)
                    soured.aggroRadius = Mathf.Max(soured.aggroRadius, ahead + 8f); // it knows you're there
                soured.leashRadius = Mathf.Max(soured.leashRadius, followRange);
                soured.giveUpRange = followRange;
                spawned.Add(soured);
            }
            go.SetActive(true);
            roots.Add(go);
        }

        public void ClearSpawned()
        {
            foreach (var s in spawned)
            {
                if (s == null)
                    continue;
                if (s.droppedLantern != null)
                    Destroy(s.droppedLantern.gameObject);
                Destroy(s.gameObject);
            }
            foreach (var root in roots)
                if (root != null)
                    Destroy(root);
            spawned.Clear();
            roots.Clear();
        }

        // Standing ground where asked, or back towards the Apprentice until there is some (never in the water).
        static Vector3 Ground(Vector3 spot, Vector3 from)
        {
            for (float t = 0f; t <= 1f; t += 0.1f)
            {
                Vector3 p = Vector3.Lerp(spot, from, t);
                if (Physics.Raycast(p + Vector3.up * 3f, Vector3.down, out var hit, 6f, ~0, QueryTriggerInteraction.Ignore)
                    && !(hit.collider is CharacterController) && hit.point.y > 0f)
                    return hit.point;
            }
            return from;
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
