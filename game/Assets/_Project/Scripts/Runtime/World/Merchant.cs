using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    // The merchant soul: sells Equipment for Obols. In the slice it waits on the landing by the Mooring post (standing
    // in for the Hub) with a single Charm. It sways a little, as souls do.
    public sealed class Merchant : MonoBehaviour
    {
        static readonly List<Merchant> all = new List<Merchant>();

        public float interactRadius = 1.5f;
        public string[] stockIds = { "eel_skin" };

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

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        void Update()
        {
            float t = Time.time;
            transform.SetPositionAndRotation(basePosition + Vector3.up * (0.03f * Mathf.Sin(t * 1.1f)),
                baseRotation * Quaternion.Euler(1.5f * Mathf.Sin(t * 0.7f), 0f, 2f * Mathf.Sin(t * 0.9f + 1f)));
        }

        public static Merchant Nearest(Vector3 position, out float distance)
        {
            Merchant best = null;
            distance = float.MaxValue;
            foreach (var m in all)
            {
                float d = Vector3.Distance(position, m.basePosition);
                if (d <= m.interactRadius && d < distance)
                {
                    best = m;
                    distance = d;
                }
            }
            return best;
        }

        public Menu Open(Apprentice apprentice) => new ShopMenu(this, apprentice);
    }

    // The merchant's wares: buy with Interact. Bought Charms still have to be fitted at a Mooring post.
    public sealed class ShopMenu : Menu
    {
        readonly Merchant merchant;
        readonly Apprentice apprentice;
        readonly List<Charm> wares;
        readonly List<Row> rows = new List<Row>();

        public ShopMenu(Merchant merchant, Apprentice apprentice)
        {
            this.merchant = merchant;
            this.apprentice = apprentice;
            wares = new List<Charm>(merchant.Stock);
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
                return rows;
            }
        }

        public override string Description => wares.Count > 0
            ? wares[Selected].Description + (apprentice.Equipment.Owns(wares[Selected]) ? "" : "\nFit it at a Mooring post.")
            : "Nothing left to sell.";

        protected override void Confirm(Row row)
        {
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
