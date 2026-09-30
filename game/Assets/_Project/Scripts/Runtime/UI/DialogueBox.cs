using UnityEngine;
using UnityEngine.UI;

namespace MyGame
{
    // A soul speaking (a Conversation): a dark band across the bottom of the screen, their name above it in the title
    // face, their words written out as they say them, a small ember breathing when a line is done, and any choice
    // beneath (the chosen one marked). The world stays in view above.
    public sealed class DialogueBox
    {
        const int MaxChoices = 3;

        readonly RectTransform root;
        readonly CanvasGroup group;
        readonly Text speaker, words, hints;
        readonly Image more;
        readonly (Text label, Image mark)[] choices = new (Text, Image)[MaxChoices];

        readonly RectTransform rule;

        public bool Visible => group.alpha > 0.01f;
        public string SpeakerText => speaker.text;
        public string WordsText => words.text;

        public DialogueBox(Transform parent, Font titleFont, Font textFont)
        {
            var bottom = new Vector2(0.5f, 0f);
            root = UiKit.Rect(parent, "Dialogue", bottom, new Vector2(0, 40), new Vector2(1500, 300));
            root.pivot = bottom;
            group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = group.blocksRaycasts = false;
            UiKit.Image(root, "Band", UiKit.Band, Color.black.WithAlpha(0.78f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1800, 320));
            var top = new Vector2(0f, 1f);
            speaker = UiKit.Text(root, "Speaker", titleFont, 40, UiKit.Ember, TextAnchor.MiddleLeft, top, new Vector2(150, -34), 4f);
            speaker.rectTransform.pivot = new Vector2(0f, 0.5f);
            speaker.rectTransform.sizeDelta = new Vector2(900, 50);
            rule = UiKit.Image(root, "Rule", UiKit.Rule, UiKit.Ember.WithAlpha(0.45f), top, new Vector2(420, -66), new Vector2(560, 2)).rectTransform;
            rule.pivot = new Vector2(0f, 0.5f);
            words = UiKit.Text(root, "Words", textFont, 36, UiKit.Bone, TextAnchor.UpperLeft, top, new Vector2(150, -82)); // (no letter spacing: it skews wrapped lines)
            words.rectTransform.pivot = top;
            words.rectTransform.sizeDelta = new Vector2(1200, 120);
            words.horizontalOverflow = HorizontalWrapMode.Wrap;
            words.lineSpacing = 1.1f;
            more = UiKit.Image(root, "More", UiKit.SoftDot, UiKit.Ember, new Vector2(1f, 0f), new Vector2(-150, 60), new Vector2(26, 26));
            for (int i = 0; i < MaxChoices; i++)
            {
                var label = UiKit.Text(root, "Choice", textFont, 32, UiKit.Ash, TextAnchor.MiddleLeft, top, new Vector2(190, -200 - i * 42), 0.5f);
                label.rectTransform.pivot = new Vector2(0f, 0.5f);
                label.rectTransform.sizeDelta = new Vector2(900, 40);
                var mark = UiKit.Image(root, "Mark", null, UiKit.Ember, top, new Vector2(170, -200 - i * 42), new Vector2(9, 9));
                mark.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
                choices[i] = (label, mark);
            }
            hints = UiKit.Text(root, "Hints", textFont, 24, UiKit.Bone.WithAlpha(0.6f), TextAnchor.MiddleRight, new Vector2(1f, 0f), new Vector2(-150, 24), 0.5f);
            hints.rectTransform.pivot = new Vector2(1f, 0.5f);
            hints.rectTransform.sizeDelta = new Vector2(800, 30);
        }

        public void Tick(Conversation talk, bool gamepad)
        {
            group.alpha = Mathf.MoveTowards(group.alpha, talk != null ? 1f : 0f, UiKit.DeltaTime * 6f);
            root.gameObject.SetActive(group.alpha > 0.001f);
            if (talk == null)
                return;
            if (speaker.text != talk.Speaker)
            {
                speaker.text = talk.Speaker;
                // The rule runs under the whole name, a little past it (its letter spacing isn't in preferredWidth).
                float width = Mathf.Max(420f, speaker.preferredWidth + (speaker.text.Length - 1) * 4f + 90f);
                rule.sizeDelta = new Vector2(width, 2f);
                rule.anchoredPosition = new Vector2(120f, -66f); // (from its left end: it fades out towards both)
            }
            // The words not yet said are there but clear, so the line is laid out (wrapped) once, not word by word.
            string said = talk.Shown, rest = talk.CurrentLine.Substring(said.Length);
            words.text = rest.Length > 0 ? $"{said}<color=#00000000>{rest}</color>" : said;
            more.enabled = talk.LineDone && !talk.Choosing;
            more.color = UiKit.Ember.WithAlpha(0.5f + 0.4f * Mathf.Sin(UiKit.Now * 4f));
            var rows = talk.Rows;
            for (int i = 0; i < MaxChoices; i++)
            {
                bool on = i < rows.Count;
                choices[i].label.enabled = choices[i].mark.enabled = on;
                if (!on)
                    continue;
                bool chosen = i == talk.Selected;
                choices[i].label.text = rows[i].Label;
                choices[i].label.color = rows[i].Dim ? UiKit.Ash.WithAlpha(0.5f) : chosen ? UiKit.Bone : UiKit.Ash;
                choices[i].mark.enabled = chosen;
            }
            string go = gamepad ? "(A)" : "[E]", leave = gamepad ? "(B)" : "[Esc]";
            hints.text = talk.Choosing ? $"{go} Choose        {leave} Leave" : $"{go} Go on        {leave} Leave";
        }
    }
}
