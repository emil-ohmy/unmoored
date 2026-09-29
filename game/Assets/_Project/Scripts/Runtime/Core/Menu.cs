using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    // A small in-world menu (the merchant's wares, the Equipment at a Mooring post). While one is open, the Apprentice
    // stands still and its input goes here: Move up/down to choose, Interact to confirm, Dash to leave.
    // The HUD draws it; this is only the logic.
    public abstract class Menu
    {
        public struct Row
        {
            public string Label, Detail;
            public bool Dim; // e.g. can't afford, already owned
        }

        public abstract string Title { get; }
        public abstract IReadOnlyList<Row> Rows { get; }
        public abstract string Description { get; } // of the selected row
        public abstract string ConfirmLabel { get; } // "Buy", "Fit", "Remove"
        public virtual string LeaveLabel => "Leave";
        public int Selected { get; protected set; }
        public bool IsOpen { get; private set; } = true;
        public string Notice { get; private set; } = ""; // a short answer to the last confirm ("Not enough Obols")
        public float NoticeAt { get; private set; } = -100f;

        bool stickArmed;

        public virtual void Tick(IApprenticeInput input)
        {
            float y = input.Move.y;
            if (Mathf.Abs(y) < 0.35f)
                stickArmed = true;
            else if (stickArmed && Rows.Count > 1)
            {
                stickArmed = false;
                Selected = (Selected + (y > 0f ? -1 : 1) + Rows.Count) % Rows.Count;
                Sfx.Instance?.UiTick();
            }
            if (input.InteractPressed && Rows.Count > 0)
                Confirm(Rows[Selected]);
            else if (input.DashPressed)
                Close();
        }

        public void Close()
        {
            if (!IsOpen)
                return;
            IsOpen = false;
            OnClosed();
        }

        protected abstract void Confirm(Row row);
        protected virtual void OnClosed() { }

        protected void Say(string notice)
        {
            Notice = notice;
            NoticeAt = UiKit.Now;
        }
    }
}
