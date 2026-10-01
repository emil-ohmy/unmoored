using System.Collections.Generic;

namespace MyGame
{
    // Resting at a Mooring post: strengthen the oar (Ferry Nails, in order: the next step only), and choose which
    // Charms fill the Charm slots. Leaving stands up.
    public sealed class PostMenu : Menu
    {
        readonly Apprentice apprentice;
        readonly List<Row> rows = new List<Row>();

        public PostMenu(Apprentice apprentice) => this.apprentice = apprentice;

        Equipment Equipment => apprentice.Equipment;
        bool OnOar => Selected == 0;
        Charm SelectedCharm => Selected > 0 && Selected - 1 < Equipment.Owned.Count ? Equipment.Owned[Selected - 1] : null;

        public override string Title => "Equipment";
        public override string ConfirmLabel => OnOar ? "Strengthen" : SelectedCharm != null && Equipment.IsFitted(SelectedCharm) ? "Remove" : "Fit";

        public override IReadOnlyList<Row> Rows
        {
            get
            {
                rows.Clear();
                var next = Equipment.NextOar;
                rows.Add(next != null
                    ? new Row { Label = "Strengthen the oar", Detail = $"{Equipment.FerryNails} / {next.Cost} Ferry Nails", Dim = !Equipment.CanStrengthen }
                    : new Row { Label = Equipment.Oar.Name, Detail = "At its strongest", Dim = true });
                foreach (var charm in Equipment.Owned)
                    rows.Add(new Row { Label = charm.Name, Detail = Equipment.IsFitted(charm) ? "Fitted" : "", Dim = !Equipment.IsFitted(charm) });
                return rows;
            }
        }

        public override string Description
        {
            get
            {
                if (OnOar)
                {
                    var next = Equipment.NextOar;
                    return next == null ? Equipment.Oar.Description
                        : $"{Equipment.Oar.Name}, made into {next.Name}. {next.Cost} Ferry Nail{(next.Cost > 1 ? "s" : "")}.";
                }
                return SelectedCharm != null ? SelectedCharm.Description : "";
            }
        }

        public string SlotsText => $"Charm slots  {Equipment.Fitted.Count} / {Equipment.charmSlots}";

        protected override void Confirm(Row row)
        {
            if (OnOar)
            {
                var next = Equipment.NextOar;
                if (next == null)
                    return;
                if (!apprentice.StrengthenOar()) // (it stands, and holds the oar up: a moment of its own)
                    Say($"Not enough Ferry Nails ({Equipment.FerryNails} of {next.Cost}).");
                return;
            }
            var charm = SelectedCharm;
            if (charm == null)
                return;
            bool was = Equipment.IsFitted(charm);
            Equipment.Toggle(charm);
            Sfx.Instance?.CharmFit(apprentice.transform.position + UnityEngine.Vector3.up);
            Say(was ? $"{charm.Name} taken off." : $"{charm.Name} fitted.");
        }
    }
}
