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
            System.Array.Clear(belt, 0, belt.Length);
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

        // ---- the Satchel: Provisions carried (up to each one's cap), and the belt: four slots, each holding one kind (or
        // none), one of them chosen (left/right choose along it, skipping the empty ones; up uses it). One used up
        // stays in its slot, dim, until more are found or it's taken out (the Satchel page).

        readonly Dictionary<Provision, int> carried = new Dictionary<Provision, int>();
        readonly Provision[] belt = new Provision[Provision.BeltSlots];

        public int Carried(Provision p) => carried.TryGetValue(p, out int n) ? n : 0;
        public IReadOnlyList<Provision> Belt => belt; // (four slots; null: empty)
        public int BeltChosen { get; private set; }
        public Provision Chosen => belt[BeltChosen];
        public int BeltSlotOf(Provision p) => System.Array.IndexOf(belt, p);
        public bool OnBelt(Provision p) => BeltSlotOf(p) >= 0;
        public bool BeltEmpty => System.Array.TrueForAll(belt, b => b == null);

        // As many as there's room for (its cap); how many were taken. The first of a kind goes in the belt's first empty
        // slot, if it has one.
        public int AddProvision(Provision p, int count = 1)
        {
            int room = UnityEngine.Mathf.Max(0, p.Cap - Carried(p));
            int taken = UnityEngine.Mathf.Min(room, count);
            if (taken <= 0)
                return 0;
            carried[p] = Carried(p) + taken;
            if (!OnBelt(p))
            {
                int empty = System.Array.IndexOf(belt, null);
                if (empty >= 0)
                {
                    bool wasEmpty = BeltEmpty;
                    belt[empty] = p;
                    if (wasEmpty)
                        BeltChosen = empty;
                }
            }
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

        // Puts `p` in a slot (null: empties it). If it was in another slot, the two swap places.
        public void SetBeltSlot(int slot, Provision p)
        {
            if (slot < 0 || slot >= belt.Length)
                return;
            int was = p != null ? BeltSlotOf(p) : -1;
            if (was >= 0)
                belt[was] = belt[slot];
            belt[slot] = p;
            if (belt[BeltChosen] == null)
                ChooseOnBelt(1);
            if (belt[BeltChosen] == null || p != null && BeltEmptyBut(slot))
                BeltChosen = slot;
            Changed?.Invoke();
        }

        bool BeltEmptyBut(int slot)
        {
            for (int i = 0; i < belt.Length; i++)
                if (i != slot && belt[i] != null)
                    return false;
            return true;
        }

        // The next or the one before along the belt (round and round), past the empty slots.
        public void ChooseOnBelt(int by)
        {
            if (BeltEmpty)
                return;
            int dir = by >= 0 ? 1 : -1;
            for (int step = 0; step < belt.Length; step++)
            {
                BeltChosen = (BeltChosen + dir + belt.Length) % belt.Length;
                if (belt[BeltChosen] != null)
                    break;
            }
            Changed?.Invoke();
        }

        // Loading a save.
        public void RestoreSatchel(string[] ids, int[] counts, string[] beltIds, int chosen)
        {
            carried.Clear();
            System.Array.Clear(belt, 0, belt.Length);
            for (int i = 0; ids != null && i < ids.Length && i < counts.Length; i++)
                if (Provision.ById(ids[i]) is Provision p)
                    carried[p] = UnityEngine.Mathf.Clamp(counts[i], 0, p.Cap);
            for (int i = 0; beltIds != null && i < beltIds.Length && i < belt.Length; i++)
                if (Provision.ById(beltIds[i]) is Provision p && !OnBelt(p))
                    belt[i] = p;
            BeltChosen = UnityEngine.Mathf.Clamp(chosen, 0, belt.Length - 1);
            if (belt[BeltChosen] == null)
                ChooseOnBelt(1);
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
            var beltIds = new string[belt.Length];
            for (int i = 0; i < belt.Length; i++)
                beltIds[i] = belt[i]?.Id ?? "";
            return (ids.ToArray(), counts.ToArray(), beltIds, BeltChosen);
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
