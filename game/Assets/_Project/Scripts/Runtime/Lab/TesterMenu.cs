using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    // The Tester's menu (the lab only). Two pages. Spawn: any kind of Soured (as many as chosen, aggressive or idle),
    // clear them all, reset the readout, reset the Apprentice. Give: any Charm, a Charm slot, the Hook, Obols, a
    // Healing charge. The rows are actions; confirming does them (a count or a mood cycles).
    public sealed class TesterMenu : Menu
    {
        enum Page { Spawn, Give }

        readonly TesterPost post;
        readonly Apprentice apprentice;
        readonly List<Row> rows = new List<Row>();
        readonly List<(string description, System.Action action)> actions = new List<(string, System.Action)>();
        Page page;

        public TesterMenu(TesterPost post, Apprentice apprentice)
        {
            this.post = post;
            this.apprentice = apprentice;
            Say("Nothing here is kept. Break whatever you like.");
        }

        public override string Title => page == Page.Spawn ? "The Tester's Post" : "Give the Apprentice";
        public override string ConfirmLabel => "Do it";
        public override IReadOnlyList<Row> Rows
        {
            get
            {
                Build();
                return rows;
            }
        }
        public override string Description
        {
            get
            {
                Build();
                return Selected < actions.Count ? actions[Selected].description : "";
            }
        }

        LabSpawner Spawner => post.spawner;

        void Build()
        {
            rows.Clear();
            actions.Clear();
            if (page == Page.Spawn)
            {
                for (int i = 0; i < Spawner.kinds.Length; i++)
                {
                    int kind = i;
                    string name = Spawner.kinds[i].name;
                    Add(name, $"x{Spawner.Count}", $"Set down {Spawner.Count} {name} in front of you, {(Spawner.Aggressive ? "aggressive" : "idle")}.", () =>
                    {
                        Spawner.Spawn(kind, apprentice);
                        Say(Spawner.Count == 1 ? $"A {name}." : $"{Spawner.Count} of them. As ordered.");
                    });
                }
                Add("How many", Spawner.Count.ToString(), "How many to set down at once.", Spawner.NextCount);
                Add("They start", Spawner.Aggressive ? "aggressive" : "idle", "Aggressive: they know you're there. Idle: they never notice you, and never fight back.",
                    () => Spawner.Aggressive = !Spawner.Aggressive);
                Add("Clear them all", $"{Spawner.Spawned.Count}", "Every Soured set down here, gone.", () =>
                {
                    Spawner.ClearSpawned();
                    Say("Cleared.");
                });
                Add("Reset the readout", "", "Start the damage counts again.", () => post.readout?.ResetCounts());
                Add("Give the Apprentice...", "", "Charms, Charm slots, the Hook, Obols, Healing charges.", () => Go(Page.Give));
                Add("Reset the Apprentice", "", "As they started: no Charms but the Tester's Knot, one slot, no Hook, no Obols, three Healing charges.", ResetApprentice);
            }
            else
            {
                var equipment = apprentice.Equipment;
                foreach (var charm in Charm.All)
                {
                    var c = charm;
                    bool owned = equipment.Owns(c);
                    Add(c.Name, owned ? "Owned" : "Take", c.Description, () =>
                    {
                        equipment.Own(c);
                        Say($"{c.Name}. Fit it at the Mooring post.");
                    }, owned);
                }
                Add("A Charm slot", $"{equipment.charmSlots} of 3", "One more Charm fitted at once (three at most, as bought in the game).", () =>
                {
                    equipment.charmSlots = Mathf.Min(3, equipment.charmSlots + 1);
                }, equipment.charmSlots >= 3);
                Add("The Hook", apprentice.HasHook ? "Owned" : "Take", "Pull to hook posts, yank Soured.", apprentice.GiveHook, apprentice.HasHook);
                Add("500 Obols", $"{apprentice.Obols}", "Obols (souls, basically).", () => apprentice.GainObols(500));
                Add("A Healing charge", $"{apprentice.maxHealingCharges}", "One more flame to heal with.", apprentice.AddHealingCharge);
                Add("Back", "", "", () => Go(Page.Spawn));
            }
        }

        void Add(string label, string detail, string description, System.Action action, bool dim = false)
        {
            rows.Add(new Row { Label = label, Detail = detail, Dim = dim });
            actions.Add((description, action));
        }

        void Go(Page to)
        {
            page = to;
            Selected = 0;
            Sfx.Instance?.UiTick();
        }

        void ResetApprentice()
        {
            apprentice.Equipment.ResetToStart();
            apprentice.Restore(0, 3, false);
            Say("As new. Or as new as anyone gets down here.");
        }

        public string PageName => page.ToString();

        // (tests) As if this row (by its label) were chosen and confirmed.
        public void Do(string label)
        {
            Build();
            int i = rows.FindIndex(r => r.Label == label);
            if (i < 0)
                throw new System.ArgumentException($"no row {label} on {page}");
            Selected = i;
            Confirm(rows[i]);
        }

        protected override void Confirm(Row row)
        {
            Build();
            if (Selected < actions.Count)
            {
                actions[Selected].action();
                Sfx.Instance?.UiTick();
            }
        }
    }
}
