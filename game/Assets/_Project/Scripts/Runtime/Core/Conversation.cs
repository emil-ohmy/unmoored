using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    // Speaking with a soul (a Lingerer): their lines one at a time, each written out as they say it (Interact first
    // finishes the line, then goes on), and at the end, if any, a choice (Up/Down, Interact). Dash / Esc leaves. It
    // holds the Apprentice still like any menu; the HUD draws it as a band of words at the bottom of the screen.
    public sealed class Conversation : Menu
    {
        public const float CharactersPerSecond = 42f;

        public struct Choice
        {
            public string Label;
            public bool Dim;
            public System.Action Chosen;
        }

        public readonly string Speaker;
        public readonly Vector3 SpeakerPosition;
        readonly IReadOnlyList<string> lines;
        readonly List<Choice> choices;
        readonly System.Action finished;
        readonly List<Row> rows = new List<Row>();
        int line;
        float typed;

        public Conversation(string speaker, Vector3 at, IReadOnlyList<string> lines, List<Choice> choices = null, System.Action finished = null)
        {
            Speaker = speaker;
            SpeakerPosition = at;
            this.lines = lines;
            this.choices = choices ?? new List<Choice>();
            this.finished = finished;
            Sfx.Instance?.Murmur(at);
        }

        public int Line => line;
        public int LineCount => lines.Count;
        public string CurrentLine => line < lines.Count ? lines[line] : "";
        public string Shown => CurrentLine.Substring(0, Mathf.Min(CurrentLine.Length, Mathf.FloorToInt(typed)));
        public bool LineDone => typed >= CurrentLine.Length;
        public bool Choosing => line >= lines.Count - 1 && LineDone && choices.Count > 0;

        public override string Title => Speaker;
        public override string ConfirmLabel => Choosing ? "Choose" : "Go on";
        public override string Description => CurrentLine;
        public override IReadOnlyList<Row> Rows
        {
            get
            {
                rows.Clear();
                if (Choosing)
                    foreach (var c in choices)
                        rows.Add(new Row { Label = c.Label, Dim = c.Dim });
                return rows;
            }
        }

        public override void Tick(IApprenticeInput input)
        {
            typed += CharactersPerSecond * UiKit.DeltaTime;
            if (Choosing)
            {
                base.Tick(input); // Up/Down to choose, Interact to confirm (Confirm), Dash to leave
                return;
            }
            if (input.DashPressed)
            {
                Close();
                return;
            }
            if (!input.InteractPressed)
                return;
            if (!LineDone)
                typed = CurrentLine.Length; // finish the line first
            else if (line < lines.Count - 1)
            {
                line++;
                typed = 0f;
                Sfx.Instance?.Murmur(SpeakerPosition);
            }
            else
                Close();
        }

        protected override void Confirm(Row row)
        {
            var choice = choices[Selected];
            Close();
            choice.Chosen?.Invoke();
        }

        protected override void OnClosed() => finished?.Invoke();

        // (tests) Straight through every line, as if Interact were pressed until a choice or the end.
        public void Skip()
        {
            line = Mathf.Max(0, lines.Count - 1);
            typed = CurrentLine.Length;
        }

        public void Choose(int index)
        {
            Selected = index;
            if (Choosing)
                Confirm(default);
        }
    }
}
