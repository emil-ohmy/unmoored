using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    // The Tester's menu (the lab only). Spawn: any kind of Soured (as many as chosen, aggressive or idle), clear them
    // all, reset the readout, reset the Apprentice. Formations: ready-made mixes. Give: any Charm, a Charm slot, the
    // Hook, Obols. Strength: a Ferry Nail, a Warm Stone, a Held Breath, a Healing charge, a lantern level. The rows are
    // actions; confirming does them (a count or a mood cycles).
    public sealed class TesterMenu : Menu
    {
        enum Page { Spawn, Formations, Give, Strength }

        readonly TesterPost post;
        readonly Apprentice apprentice;
        readonly List<Row> rows = new List<Row>();
        readonly List<(string description, System.Action action)> actions = new List<(string, System.Action)>();
        Page page;
        static int gardenerPhase = 1;

        public TesterMenu(TesterPost post, Apprentice apprentice)
        {
            this.post = post;
            this.apprentice = apprentice;
            Say("Nothing here is kept. Break whatever you like.");
        }

        public override string Title => page switch
        {
            Page.Spawn => "The Tester's Post",
            Page.Formations => "Formations",
            Page.Give => "Give the Apprentice",
            _ => "Make them stronger",
        };
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
                    if (!Spawner.kinds[i].listed)
                        continue;
                    int kind = i;
                    string name = Spawner.kinds[i].name;
                    int group = Mathf.Max(1, Spawner.kinds[i].group);
                    string many = group > 1 ? $"{Spawner.Count} pair{(Spawner.Count > 1 ? "s" : "")} of {name}" : $"{Spawner.Count} {name}";
                    Add(name, group > 1 ? $"x{Spawner.Count} pair" : $"x{Spawner.Count}", $"Set down {many} in front of you, {(Spawner.Aggressive ? "aggressive" : "idle")}.", () =>
                    {
                        Spawner.Spawn(kind, apprentice);
                        Say(Spawner.Count * group == 1 ? $"A {name}." : group > 1 && Spawner.Count == 1 ? "A pair. They never come alone." : $"{Spawner.Count * group} of them. As ordered.");
                    });
                }
                Add("How many", Spawner.Count.ToString(), "How many to set down at once.", Spawner.NextCount);
                Add("They start", Spawner.Aggressive ? "aggressive" : "idle", "Aggressive: they know you're there. Idle: they never notice you, and never fight back.",
                    () => Spawner.Aggressive = !Spawner.Aggressive);
                Add("Formations...", "", "The miniboss, and ready-made mixes: shield and bow, Clingers with a Laden, everything at once.", () => Go(Page.Formations));
                if (post.residents.Length > 0)
                    Add("The rows' own", post.ResidentsAwake ? "awake" : "asleep", "The Soured placed about this lab (the Gleaners, the Moths): asleep, they're gone, so the new thing can be tried on its own; awake, they're back where they stood.", () =>
                    {
                        post.SetResidents(!post.ResidentsAwake);
                        Say(post.ResidentsAwake ? "Up they get." : "Hush. They'll keep.");
                    });
                var music = Object.FindAnyObjectByType<MusicPlayer>();
                if (music != null && music.woken != null)
                    Add("The Area's music", music.Woken ? "awake" : "as it is", "Its music as it is now, or as it will be woken (the Hall of the Last Hour: time set going again by its central engine, not built yet).", () =>
                    {
                        music.Wake(!music.Woken);
                        Say(music.Woken ? "Time again." : "Held still.");
                    });
                var moth = Object.FindAnyObjectByType<FatherMoth>();
                if (moth != null)
                {
                    Add("Father Moth: phase", $"{gardenerPhase}", "Which of his four phases to face him from: 1 and 2 his leaves, 3 afoot, 4 the last.", () =>
                        gardenerPhase = gardenerPhase % 4 + 1);
                    Add("Face Father Moth", $"phase {gardenerPhase}", "Into the Glasshouse, and his fight begun at the phase chosen (at its health).", () =>
                    {
                        apprentice.Teleport(moth.middle + new Vector3(0f, 0.05f, -5f));
                        moth.BeginAt(gardenerPhase);
                        Say(gardenerPhase == 1 ? "He's waking." : $"Phase {gardenerPhase}. Mind his shears.");
                    });
                }
                Add("Clear them all", $"{Spawner.Spawned.Count}", "Every Soured set down here, gone (and the readout's counts start again).", () =>
                {
                    Spawner.ClearSpawned();
                    post.readout?.ResetCounts();
                    Say("Cleared.");
                });
                Add("Give the Apprentice...", "", "Charms, Charm slots, the Hook, Obols; Ferry Nails, Warm Stones, Held Breaths, Healing charges.", () => Go(Page.Give));
                Add("Reset the Apprentice", "", "As they started: no Charms but the Tester's Knot, one slot, no Hook, no Obols, three Healing charges, the first oar, nothing found (the pieces lie where they were again), nothing in the Ledger.", ResetApprentice);
            }
            else if (page == Page.Formations)
            {
                for (int i = 0; i < LabSpawner.Formations.Length; i++)
                {
                    int f = i;
                    var (name, description, places) = LabSpawner.Formations[i];
                    Add(name, $"{places.Length}", $"{description} ({(Spawner.Aggressive ? "aggressive" : "idle")})", () =>
                    {
                        Spawner.SpawnFormation(f, apprentice);
                        Say($"{name}. As ordered.");
                    });
                }
                Add("Back", "", "", () => Go(Page.Spawn));
            }
            else if (page == Page.Strength)
            {
                var equipment = apprentice.Equipment;
                Add("A Ferry Nail", $"{equipment.FerryNails}", "For the oar: strengthen it at the Mooring post (1, 2, 3, then 4 nails a step).", () =>
                    apprentice.AddFerryNail());
                Add("A Warm Stone", $"{Mathf.RoundToInt(apprentice.maxHealth)}", $"{Mathf.RoundToInt(apprentice.healthPerStone)} more health, for good.", apprentice.AddWarmStone);
                Add("A Held Breath", $"{Mathf.RoundToInt(apprentice.maxStamina)}", $"{Mathf.RoundToInt(apprentice.staminaPerBreath)} more stamina, for good.", apprentice.AddHeldBreath);
                Add("A Healing charge", $"{apprentice.maxHealingCharges}", "One more flame to heal with (an Ember).", apprentice.AddHealingCharge);
                Add("The lantern: a level", $"{Lantern.Level} of {Lantern.Levels}", "As if a quarter of the journey's Mooring posts were lit (dev). See the character screen (I).", () =>
                {
                    Lantern.DevLevels = Mathf.Min(Lantern.Levels - 1, Lantern.DevLevels + 1);
                    Say(Lantern.Line);
                }, Lantern.Level >= Lantern.Levels);
                Add("Back", "", "", () => Go(Page.Give));
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
                Add("The Stroke", apprentice.HasStroke ? "Owned" : "Take", "The Drowned Moon in the oar blade: hold the heavy attack, let go, a wave.", apprentice.GiveStroke, apprentice.HasStroke);
                Add("500 Obols", $"{apprentice.Obols}", "Obols (souls, basically).", () => apprentice.GainObols(500));
                Add("A full Satchel", $"{Provision.All.Length} kinds", "Every Provision, as many as the Satchel takes of each (and on the belt).", () =>
                {
                    foreach (var p in Provision.All)
                        equipment.AddProvision(p, p.Cap);
                    Say("Packed. Use them all, nobody's counting.");
                });
                Add("The whole Ledger", "", "Every Soured, miniboss and Boss written in, as if each had been met once. See the character screen (I).", () =>
                {
                    equipment.FillLedger();
                    Say("Written in. You've met them all, on paper.");
                });
                Add("Strength...", "", "Ferry Nails for the oar, Warm Stones, Held Breaths, Healing charges, the lantern.", () => Go(Page.Strength));
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
            Lantern.DevLevels = 0;
            Object.FindAnyObjectByType<LabFinds>()?.PutBack(); // (everything found, where it was again)
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
