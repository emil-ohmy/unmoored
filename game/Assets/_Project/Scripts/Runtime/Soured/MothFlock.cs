using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    // A flock of Moths: it circles its light (a lantern), slow and loose, until one of them senses the Apprentice;
    // then they all come, circling them just out of reach, and send one in at a time (never two at once).
    public sealed class MothFlock : MonoBehaviour
    {
        public Transform lamp;               // the light it circles while nothing's near (none: where it was set down)
        public float ringRadius = 1.4f, ringHeight = 0.5f;     // round the lamp
        public float chaseRadius = 2.9f, chaseHeight = 1.9f;   // round the Apprentice
        public float turnSpeed = 0.7f;       // how fast the ring goes round (radians a second)
        public float diveInterval = 1.3f;    // between one dive and the next

        readonly List<Moth> moths = new List<Moth>();
        public IReadOnlyList<Moth> Moths => moths;
        public int Alive => moths.FindAll(m => m != null && m.IsAlive).Count;
        public bool Chasing { get; private set; }

        Apprentice apprentice;
        Moth diver;
        float nextDive;
        Vector3 home;

        void Awake()
        {
            home = transform.position;
            foreach (var m in GetComponentsInChildren<Moth>(true))
            {
                m.Flock = this;
                moths.Add(m);
            }
        }

        void Start() => apprentice = FindAnyObjectByType<Apprentice>();

        void Update()
        {
            Chasing = false;
            foreach (var m in moths)
                if (m != null && m.IsAlive && m.CurrentState != Soured.State.Idle && m.CurrentState != Soured.State.Returning)
                    Chasing = true;
            if (Chasing)
                foreach (var m in moths)
                    if (m != null && m.IsAlive)
                        m.Alert(); // (one sees you: they all come)
            if (diver != null && (!diver.IsAlive || diver.CurrentState == Soured.State.Chase || diver.CurrentState == Soured.State.Idle))
                diver = null;
        }

        // Out (from a scarecrow's straw; to the Watchman's lantern): the flock comes, whole, to `at`, and comes for the
        // Apprentice at once.
        public void Release(Vector3 at)
        {
            gameObject.SetActive(true);
            home = at;
            transform.position = at;
            for (int i = 0; i < moths.Count; i++)
            {
                float a = i * Mathf.PI * 2f / moths.Count;
                moths[i].SetSpawn(at + new Vector3(Mathf.Cos(a) * 0.5f, 0f, Mathf.Sin(a) * 0.5f), Quaternion.Euler(0f, i * 47f, 0f));
                moths[i].Reform();
                moths[i].Alert();
            }
            diver = null;
        }

        // Gone again, until it's released (the Apprentice Rested: the scarecrow is whole).
        public void Withdraw() => gameObject.SetActive(false);

        // Its place in the ring now: round the lamp, or round the Apprentice when they've come; each its own, a little
        // bob and wander of its own.
        public Vector3 PlaceFor(Moth m)
        {
            int i = Mathf.Max(0, moths.IndexOf(m)), n = Mathf.Max(1, moths.Count);
            float a = Time.time * turnSpeed * (Chasing ? 1.6f : 1f) + i * Mathf.PI * 2f / n;
            bool round = Chasing && apprentice != null;
            Vector3 centre = round ? apprentice.transform.position + Vector3.up * chaseHeight
                           : (lamp != null ? lamp.position : home) + Vector3.up * ringHeight;
            float r = (round ? chaseRadius : ringRadius) * (0.85f + 0.15f * Mathf.Sin(Time.time * 1.3f + i * 2.1f));
            return centre + new Vector3(Mathf.Cos(a) * r, 0.25f * Mathf.Sin(Time.time * 2.2f + i * 1.7f), Mathf.Sin(a) * r);
        }

        // Whether this one may dive now (one at a time, a breath between).
        public bool MayDive(Moth m)
        {
            if (diver != null || Time.time < nextDive || !m.IsAlive)
                return false;
            diver = m;
            nextDive = Time.time + diveInterval;
            return true;
        }
    }
}
