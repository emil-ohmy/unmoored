using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    // The Apprentice's Equipment: the Charms owned, and those fitted in the Charm slots (1 at the start; more are
    // bought). Fitting and removing happen only at a Mooring post (the post menu).
    public sealed class Equipment : MonoBehaviour
    {
        public int charmSlots = 1;
        [Tooltip("Charm ids owned from the start (development: the Tester's Knot)")]
        public string[] startingCharms = new string[0];

        readonly List<Charm> owned = new List<Charm>();
        readonly List<string> keepsakes = new List<string>(); // things carried for a Lingerer (their ids)

        public IReadOnlyList<string> Keepsakes => keepsakes;
        public bool Carries(string keepsake) => keepsakes.Contains(keepsake);
        public void Carry(string keepsake)
        {
            if (!keepsakes.Contains(keepsake))
                keepsakes.Add(keepsake);
        }
        public void Give(string keepsake) => keepsakes.Remove(keepsake);
        readonly List<Charm> fitted = new List<Charm>();

        public IReadOnlyList<Charm> Owned => owned;
        public IReadOnlyList<Charm> Fitted => fitted;
        public bool Owns(Charm charm) => owned.Contains(charm);
        public bool IsFitted(Charm charm) => fitted.Contains(charm);

        void Awake()
        {
            foreach (var id in startingCharms)
                if (Charm.ById(id) is Charm charm)
                    Own(charm);
        }

        // (the lab) Back to how the Apprentice started: the starting Charms owned, nothing fitted, one slot.
        public void ResetToStart()
        {
            owned.Clear();
            fitted.Clear();
            keepsakes.Clear();
            charmSlots = 1;
            foreach (var id in startingCharms)
                if (Charm.ById(id) is Charm charm)
                    Own(charm);
        }

        public void Own(Charm charm)
        {
            if (!owned.Contains(charm))
                owned.Add(charm);
        }

        // Fits it, or takes it off if it's fitted. With every slot taken, fitting swaps out the oldest.
        public void Toggle(Charm charm)
        {
            if (fitted.Remove(charm) || !owned.Contains(charm))
                return;
            if (fitted.Count >= charmSlots)
                fitted.RemoveAt(0);
            fitted.Add(charm);
        }
    }
}
