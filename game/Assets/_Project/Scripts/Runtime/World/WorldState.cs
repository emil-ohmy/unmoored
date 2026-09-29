using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    // Where the Apprentice wakes after dying, and every Soured that re-forms on Rest or death.
    public sealed class WorldState : MonoBehaviour
    {
        public static WorldState Instance { get; private set; }
        static readonly List<Soured> soured = new List<Soured>();
        public static IReadOnlyList<Soured> AllSoured => soured;

        public Transform startPoint;
        public MooringPost LastRested { get; private set; }

        public Vector3 RespawnPosition =>
            LastRested != null ? LastRested.RespawnPoint : (startPoint != null ? startPoint.position : Vector3.zero);

        void Awake()
        {
            Instance = this;
            BossFight.ResetWorld();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public static void Register(Soured s)
        {
            if (!soured.Contains(s))
                soured.Add(s);
        }

        public static void Unregister(Soured s) => soured.Remove(s);

        public void OnRest(MooringPost post)
        {
            LastRested = post;
            ReformAllSoured();
        }

        public void ReformAllSoured()
        {
            foreach (var s in soured.ToArray())
                s.Reform();
        }
    }
}
