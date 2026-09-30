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
            carried.Clear();
            belt.Clear();
            BeltChosen = 0;
            owned.Clear();
            fitted.Clear();
            keepsakes.Clear();
            charmSlots = 1;
            foreach (var id in startingCharms)
                if (Charm.ById(id) is Charm charm)
                    Own(charm);
            Changed?.Invoke();
        }

        // ---- the Satchel: Provisions carried (up to each one's cap), and the belt: up to four of them, in order, one
        // chosen (left/right choose, up uses it). One used up stays on the belt, dim, until more are found or it's
        // taken off (at the Satchel page).

        readonly Dictionary<Provision, int> carried = new Dictionary<Provision, int>();
        readonly List<Provision> belt = new List<Provision>();

        public int Carried(Provision p) => carried.TryGetValue(p, out int n) ? n : 0;
        public IReadOnlyList<Provision> Belt => belt;
        public int BeltChosen { get; private set; }
        public Provision Chosen => belt.Count > 0 ? belt[BeltChosen] : null;
        public int BeltSlotOf(Provision p) => belt.IndexOf(p);
        public bool OnBelt(Provision p) => belt.Contains(p);

        // As many as there's room for (its cap); how many were taken. The first of a kind goes on the belt, if there's
        // room on it.
        public int AddProvision(Provision p, int count = 1)
        {
            int room = UnityEngine.Mathf.Max(0, p.Cap - Carried(p));
            int taken = UnityEngine.Mathf.Min(room, count);
            if (taken <= 0)
                return 0;
            carried[p] = Carried(p) + taken;
            if (!belt.Contains(p) && belt.Count < Provision.BeltSlots)
                belt.Add(p);
            Changed?.Invoke();
            return taken;
        }

        public bool SpendProvision(Provision p)
        {
            if (Carried(p) <= 0)
                return false;
            carried[p] = Carried(p) - 1;
            Changed?.Invoke();
            return true;
        }

        // Onto the belt (at its end, if there's room) or off it. False if the belt is full.
        public bool ToggleOnBelt(Provision p)
        {
            int at = belt.IndexOf(p);
            if (at >= 0)
            {
                belt.RemoveAt(at);
                if (BeltChosen > at || BeltChosen >= belt.Count)
                    BeltChosen = UnityEngine.Mathf.Max(0, BeltChosen - 1);
            }
            else if (belt.Count >= Provision.BeltSlots)
                return false;
            else
                belt.Add(p);
            Changed?.Invoke();
            return true;
        }

        // The next or the one before along the belt (round and round).
        public void ChooseOnBelt(int by)
        {
            if (belt.Count == 0)
                return;
            BeltChosen = (BeltChosen + by % belt.Count + belt.Count) % belt.Count;
            Changed?.Invoke();
        }

        // Loading a save.
        public void RestoreSatchel(string[] ids, int[] counts, string[] beltIds, int chosen)
        {
            carried.Clear();
            belt.Clear();
            for (int i = 0; ids != null && i < ids.Length && i < counts.Length; i++)
                if (Provision.ById(ids[i]) is Provision p)
                    carried[p] = UnityEngine.Mathf.Clamp(counts[i], 0, p.Cap);
            if (beltIds != null)
                foreach (var id in beltIds)
                    if (Provision.ById(id) is Provision p && !belt.Contains(p) && belt.Count < Provision.BeltSlots)
                        belt.Add(p);
            BeltChosen = UnityEngine.Mathf.Clamp(chosen, 0, UnityEngine.Mathf.Max(0, belt.Count - 1));
            Changed?.Invoke();
        }

        public (string[] ids, int[] counts, string[] belt, int chosen) SatchelForSave()
        {
            var ids = new List<string>();
            var counts = new List<int>();
            foreach (var kv in carried)
            {
                ids.Add(kv.Key.Id);
                counts.Add(kv.Value);
            }
            return (ids.ToArray(), counts.ToArray(), belt.ConvertAll(p => p.Id).ToArray(), BeltChosen);
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
