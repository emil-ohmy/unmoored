using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    // The merchant soul: sells Equipment for Obols. In the slice it waits on the landing by the Mooring post (standing
    // in for the Hub) with a single Charm. It sways a little, as souls do.
    public sealed class Merchant : MonoBehaviour, IInteractable
    {
        public float interactRadius = 1.5f;
        public string[] stockIds = { "eel_skin" };
        public string[] provisionIds = { "pitch_pot", "ember_tar", "way_back", "warm_broth", "bitter_root" }; // (as many as the Satchel takes)

        public IEnumerable<Provision> Provisions
        {
            get
            {
                foreach (var id in provisionIds)
                    if (Provision.ById(id) is Provision p)
                        yield return p;
            }
        }

        public IEnumerable<Charm> Stock
        {
            get
            {
                foreach (var id in stockIds)
                    foreach (var charm in Charm.All)
                        if (charm.Id == id)
                            yield return charm;
            }
        }

        Vector3 basePosition;
        Quaternion baseRotation;

        void Awake()
        {
            basePosition = transform.position;
            baseRotation = transform.rotation;
        }

        void OnEnable() => Interactables.Register(this);
        void OnDisable() => Interactables.Unregister(this);

        public Vector3 InteractPosition => basePosition;
        public float InteractRadius => interactRadius;
        public bool Available => true;
        public string PromptLabel => "Trade";
        public void Interact(Apprentice apprentice) => apprentice.ShowMenu(Open(apprentice));

        void Update()
        {
            float t = Time.time;
            transform.SetPositionAndRotation(basePosition + Vector3.up * (0.03f * Mathf.Sin(t * 1.1f)),
                baseRotation * Quaternion.Euler(1.5f * Mathf.Sin(t * 0.7f), 0f, 2f * Mathf.Sin(t * 0.9f + 1f)));
        }

        public Menu Open(Apprentice apprentice) => new ShopMenu(this, apprentice);
    }

    // The merchant's wares: buy with Interact. Bought Charms still have to be fitted at a Mooring post; Provisions go
    // in the Satchel, one at a time, up to each one's cap.
    public sealed class ShopMenu : Menu
    {
        readonly Merchant merchant;
        readonly Apprentice apprentice;
        readonly List<Charm> wares;
        readonly List<Provision> provisions;
        readonly List<Row> rows = new List<Row>();

        public ShopMenu(Merchant merchant, Apprentice apprentice)
        {
            this.merchant = merchant;
            this.apprentice = apprentice;
            wares = new List<Charm>(merchant.Stock);
            provisions = new List<Provision>(merchant.Provisions);
            // Its patter (the game's own voice: docs/brief.md, Tone and voice).
            Say(BossFight.Beaten > 0 ? "Everything must go. Unlike some people." : "Wares for the crossing. Such as it is.");
        }

        public override string Title => "The Merchant Soul";
        public override string ConfirmLabel => "Buy";

        public override IReadOnlyList<Row> Rows
        {
            get
            {
                rows.Clear();
                foreach (var charm in wares)
                {
                    bool owned = apprentice.Equipment.Owns(charm);
                    rows.Add(new Row { Label = charm.Name, Detail = owned ? "Owned" : $"{charm.Price} Obols",
                                       Dim = owned || apprentice.Obols < charm.Price });
                }
                foreach (var p in provisions)
                {
                    int carried = apprentice.Equipment.Carried(p);
                    rows.Add(new Row { Label = $"{p.Name}  ({carried}/{p.Cap})", Detail = carried >= p.Cap ? "Full" : $"{p.Price} Obols",
                                       Dim = carried >= p.Cap || apprentice.Obols < p.Price });
                }
                return rows;
            }
        }

        public override string Description => Selected < wares.Count
            ? wares[Selected].Description + (apprentice.Equipment.Owns(wares[Selected]) ? "" : "\nFit it at a Mooring post.")
            : Selected - wares.Count < provisions.Count ? $"{provisions[Selected - wares.Count].Description}\n{provisions[Selected - wares.Count].Effect}"
            : "Nothing left to sell.";

        protected override void Confirm(Row row)
        {
            if (Selected >= wares.Count)
            {
                var p = provisions[Selected - wares.Count];
                if (apprentice.Equipment.Carried(p) >= p.Cap)
                    Say("You can't carry more of those.");
                else if (!apprentice.SpendObols(p.Price))
                    Say("Not enough Obols.");
                else
                {
                    apprentice.Equipment.AddProvision(p);
                    Sfx.Instance?.ObolClink(merchant.transform.position + Vector3.up);
                    Say(p == Provision.WayBack ? "Use it wisely. Or at all." : $"{p.Name}. Into the Satchel.");
                }
                return;
            }
            var charm = wares[Selected];
            if (apprentice.Equipment.Owns(charm))
                Say("You already carry it.");
            else if (!apprentice.SpendObols(charm.Price))
                Say("Not enough Obols.");
            else
            {
                apprentice.Equipment.Own(charm);
                Sfx.Instance?.ObolClink(merchant.transform.position + Vector3.up);
                Say($"{charm.Name}: yours.");
            }
        }
    }
}
