using System.Collections.Generic;

namespace MyGame
{
    // Resting at a Mooring post with Equipment to fit: choose which Charms fill the Charm slots. Leaving stands up.
    public sealed class PostMenu : Menu
    {
        readonly Apprentice apprentice;
        readonly List<Row> rows = new List<Row>();

        public PostMenu(Apprentice apprentice) => this.apprentice = apprentice;

        Equipment Equipment => apprentice.Equipment;

        public override string Title => "Equipment";
        public override string LeaveLabel => "Stand up";
        public override string ConfirmLabel => Equipment.Owned.Count > 0 && Equipment.IsFitted(Equipment.Owned[Selected]) ? "Remove" : "Fit";

        public override IReadOnlyList<Row> Rows
        {
            get
            {
                rows.Clear();
                foreach (var charm in Equipment.Owned)
                    rows.Add(new Row { Label = charm.Name, Detail = Equipment.IsFitted(charm) ? "Fitted" : "", Dim = !Equipment.IsFitted(charm) });
                return rows;
            }
        }

        public override string Description =>
            Equipment.Owned.Count > 0 ? Equipment.Owned[Selected].Description : "";

        public string SlotsText => $"Charm slots  {Equipment.Fitted.Count} / {Equipment.charmSlots}";

        protected override void Confirm(Row row)
        {
            var charm = Equipment.Owned[Selected];
            bool was = Equipment.IsFitted(charm);
            Equipment.Toggle(charm);
            Sfx.Instance?.CharmFit(apprentice.transform.position + UnityEngine.Vector3.up);
            Say(was ? $"{charm.Name} taken off." : $"{charm.Name} fitted.");
        }
    }
}
