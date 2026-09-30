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

        // ---- the oar, and what's been found to make the Apprentice stronger

        public int OarStep { get; private set; }      // 0 .. Oar.Last
        public int FerryNails { get; private set; }   // carried, not yet spent
        public int WarmStones { get; private set; }   // found: more health
        public int HeldBreaths { get; private set; }  // found: more stamina
        public Oar Oar => Oar.Steps[OarStep];
        public Oar NextOar => OarStep < Oar.Last ? Oar.Steps[OarStep + 1] : null;
        public bool CanStrengthen => NextOar != null && FerryNails >= NextOar.Cost;
        public event System.Action Changed; // (the oar's look, the character screen)

        public void AddFerryNail(int count = 1)
        {
            FerryNails += count;
            Changed?.Invoke();
        }

        // At a Mooring post: the nails for the next step, and the oar is stronger (and looks it).
        public bool Strengthen()
        {
            if (!CanStrengthen)
                return false;
            FerryNails -= NextOar.Cost;
            OarStep++;
            Changed?.Invoke();
            return true;
        }

        public void AddWarmStone() { WarmStones++; Changed?.Invoke(); }
        public void AddHeldBreath() { HeldBreaths++; Changed?.Invoke(); }

        // Loading a save.
        public void Restore(int oarStep, int ferryNails, int warmStones, int heldBreaths)
        {
            OarStep = UnityEngine.Mathf.Clamp(oarStep, 0, Oar.Last);
            FerryNails = ferryNails;
            WarmStones = warmStones;
            HeldBreaths = heldBreaths;
            Changed?.Invoke();
        }

        // (the lab) Back to how the Apprentice started: the starting Charms owned, nothing fitted, one slot.
        public void ResetToStart()
        {
            OarStep = FerryNails = WarmStones = HeldBreaths = 0;
            owned.Clear();
            fitted.Clear();
            keepsakes.Clear();
            charmSlots = 1;
            foreach (var id in startingCharms)
                if (Charm.ById(id) is Charm charm)
                    Own(charm);
            Changed?.Invoke();
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
