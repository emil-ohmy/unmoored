using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    // A Mooring post: the checkpoint. Resting lights its lantern, heals fully, refills Healing charges, makes it
    // the respawn point and re-forms every Soured except defeated Bosses.
    public sealed class MooringPost : MonoBehaviour, IInteractable
    {
        static readonly List<MooringPost> all = new List<MooringPost>();
        public static event System.Action<MooringPost, bool> Rested; // bool: this Rest lit the lantern for the first time

        public Transform respawnPoint;
        public Light lanternLight;
        public float interactRadius = 2.2f;
        public bool litAtStart;

        public bool IsLit { get; private set; }
        public string SaveId { get; private set; }
        public Vector3 RespawnPoint => respawnPoint != null ? respawnPoint.position : transform.position;

        Material lantern;
        Color lanternEmission;

        void Awake()
        {
            SaveId = SaveKey.Of(this);
            foreach (var r in GetComponentsInChildren<Renderer>())
                foreach (var m in r.materials)
                    if (m.name.StartsWith("Lantern_Glow"))
                    {
                        lantern = m;
                        lanternEmission = m.GetColor("_EmissionColor");
                    }
            SetLit(litAtStart);
        }

        void OnEnable()
        {
            all.Add(this);
            Interactables.Register(this);
        }

        void OnDisable()
        {
            all.Remove(this);
            Interactables.Unregister(this);
        }

        public Vector3 InteractPosition => transform.position;
        public float InteractRadius => interactRadius;
        public bool Available => true;
        public string PromptLabel => "Rest";
        public void Interact(Apprentice apprentice) => Rest(apprentice);

        // Share of Mooring posts in the loaded world that have been lit (the bow lantern grows with it).
        public static System.Collections.Generic.IReadOnlyList<MooringPost> All => all;

        // Loading a save: lit, quietly (no sound, no banner).
        public void RestoreLit() => SetLit(true);

        public static int LitCount
        {
            get
            {
                int lit = 0;
                foreach (var post in all)
                    if (post.IsLit)
                        lit++;
                return lit;
            }
        }

        public static float LitFraction
        {
            get
            {
                if (all.Count == 0)
                    return 0f;
                int lit = 0;
                foreach (var post in all)
                    if (post.IsLit)
                        lit++;
                return (float)lit / all.Count;
            }
        }

        public static MooringPost Nearest(Vector3 position)
        {
            MooringPost best = null;
            float bestDistance = float.MaxValue;
            foreach (var post in all)
            {
                float d = Vector3.Distance(position, post.transform.position);
                if (d <= post.interactRadius && d < bestDistance)
                {
                    best = post;
                    bestDistance = d;
                }
            }
            return best;
        }

        public void Rest(Apprentice apprentice)
        {
            bool firstLight = !IsLit;
            SetLit(true);
            apprentice.RestoreFully();
            WorldState.Instance?.OnRest(this);
            if (firstLight)
                Sfx.Instance?.LanternLit(transform.position);
            Rested?.Invoke(this, firstLight);
            apprentice.ShowMenu(new PostMenu(apprentice)); // sit: the oar, and the Charms to fit
        }

        void SetLit(bool lit)
        {
            IsLit = lit;
            if (lantern != null)
                lantern.SetColor("_EmissionColor", lit ? lanternEmission : lanternEmission * 0.04f);
            if (lanternLight != null)
                lanternLight.enabled = lit;
        }
    }
}
