using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MyGame
{
    // A full-screen list, for the title screen and the pause menu: a heading and its rule, a centred column of rows
    // (the chosen one bright, an ember diamond either side of it, a soft band behind), an optional value at each row's
    // right (a volume, a key), a line of detail under the list, and the keys at the bottom. Drawn in real time (it
    // runs while the game is paused).
    public sealed class ScreenList
    {
        public const int MaxRows = 12;
        const float RowHeight = 54f, Width = 820f;

        public struct Row
        {
            public string Label, Value;
            public bool Dim;
        }

        public bool Visible => group.alpha > 0.01f;
        public string Heading => heading.text;
        public IReadOnlyList<Row> Shown => shown;

        readonly RectTransform root, list, backdrop;
        readonly CanvasGroup group;
        readonly Text heading, detail, hints;
        readonly (RectTransform rect, Image band, Image left, Image right, Text label, Text value)[] rows;
        readonly List<Row> shown = new List<Row>();
        bool open;

        public ScreenList(Transform parent, Font titleFont, Font textFont, float headingSize = 64f, float top = 250f)
        {
            var centre = new Vector2(0.5f, 0.5f);
            root = UiKit.Stretch(UiKit.Rect(parent, "Screen List", centre, Vector2.zero, Vector2.zero));
            group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = group.blocksRaycasts = false;
            // A soft dark band behind the list (sized to it as it's drawn), so it reads over the world.
            backdrop = UiKit.Image(root, "Backdrop", UiKit.Band, Color.black.WithAlpha(0.6f), centre, Vector2.zero, Vector2.zero).rectTransform;
            var hintBand = UiKit.Image(root, "Hint Band", UiKit.Band, Color.black.WithAlpha(0.55f), new Vector2(0.5f, 0f), new Vector2(0, 70), new Vector2(1500, 70));
            heading = UiKit.Text(root, "Heading", titleFont, (int)headingSize, UiKit.Bone, TextAnchor.MiddleCenter, centre, new Vector2(0, top), 6f);
            heading.rectTransform.sizeDelta = new Vector2(1600, headingSize * 1.6f);
            UiKit.Image(root, "Rule", UiKit.Rule, UiKit.Bone.WithAlpha(0.6f), centre, new Vector2(0, top - headingSize * 0.75f), new Vector2(headingSize * 10f, 3));
            list = UiKit.Rect(root, "Rows", centre, new Vector2(0, top - headingSize * 0.75f - 40f), new Vector2(Width, MaxRows * RowHeight));
            list.pivot = new Vector2(0.5f, 1f);
            rows = new (RectTransform, Image, Image, Image, Text, Text)[MaxRows];
            for (int i = 0; i < MaxRows; i++)
            {
                var row = UiKit.Rect(list, "Row", new Vector2(0.5f, 1f), new Vector2(0, -i * RowHeight), new Vector2(Width, RowHeight));
                row.pivot = new Vector2(0.5f, 1f);
                var band = UiKit.Image(row, "Band", UiKit.Band, UiKit.Ember.WithAlpha(0.14f), centre, Vector2.zero, new Vector2(Width + 160, RowHeight + 8));
                var left = UiKit.Image(row, "Mark", null, UiKit.Ember, centre, Vector2.zero, new Vector2(9, 9));
                var right = UiKit.Image(row, "Mark", null, UiKit.Ember, centre, Vector2.zero, new Vector2(9, 9));
                left.rectTransform.localRotation = right.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
                var label = UiKit.Text(row, "Label", textFont, 34, UiKit.Bone, TextAnchor.MiddleCenter, centre, Vector2.zero, 1f);
                label.rectTransform.sizeDelta = new Vector2(Width, RowHeight);
                var value = UiKit.Text(row, "Value", textFont, 34, UiKit.Ash, TextAnchor.MiddleRight, new Vector2(1f, 0.5f), new Vector2(-10, 0));
                value.rectTransform.pivot = new Vector2(1f, 0.5f);
                value.rectTransform.sizeDelta = new Vector2(420, RowHeight);
                rows[i] = (row, band, left, right, label, value);
            }
            detail = UiKit.Text(root, "Detail", textFont, 28, UiKit.Ash, TextAnchor.UpperCenter, centre, Vector2.zero, 1f);
            detail.rectTransform.sizeDelta = new Vector2(1200, 80);
            hints = UiKit.Text(root, "Hints", textFont, 26, UiKit.Bone.WithAlpha(0.75f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0, 70), 1f);
            hints.rectTransform.sizeDelta = new Vector2(1600, 40);
        }

        public void Show(bool on) => open = on;

        // Draws a page: its heading, its rows (one chosen), a detail line, the keys. Values present: labels sit left.
        public void Set(string title, IReadOnlyList<Row> page, int selected, string detailText, string hintText)
        {
            heading.text = title;
            shown.Clear();
            shown.AddRange(page);
            bool values = false;
            foreach (var r in page)
                values |= !string.IsNullOrEmpty(r.Value);
            for (int i = 0; i < MaxRows; i++)
            {
                var (rect, band, left, right, label, value) = rows[i];
                bool on = i < page.Count;
                rect.gameObject.SetActive(on);
                if (!on)
                    continue;
                bool chosen = i == selected;
                label.text = page[i].Label;
                label.alignment = values ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
                label.rectTransform.anchoredPosition = new Vector2(values ? 40f : 0f, 0f);
                label.color = (page[i].Dim ? UiKit.Ash.WithAlpha(0.55f) : chosen ? UiKit.Bone : UiKit.Ash);
                value.text = page[i].Value ?? "";
                value.color = chosen ? UiKit.Bone : UiKit.Ash;
                band.enabled = left.enabled = right.enabled = chosen;
                float half = values ? Width / 2f - 8f : Mathf.Min(Width / 2f, label.preferredWidth / 2f + 34f);
                left.rectTransform.anchoredPosition = new Vector2(-half, 0f);
                right.rectTransform.anchoredPosition = new Vector2(half, 0f);
            }
            detail.text = detailText ?? "";
            detail.rectTransform.anchoredPosition = new Vector2(0, list.anchoredPosition.y - page.Count * RowHeight - 44f);
            float top = heading.rectTransform.anchoredPosition.y + heading.fontSize * 0.6f, bottom = detail.rectTransform.anchoredPosition.y - (string.IsNullOrEmpty(detail.text) ? 0f : 40f);
            backdrop.anchoredPosition = new Vector2(0, (top + bottom) * 0.5f);
            backdrop.sizeDelta = new Vector2(Width + 420, top - bottom + 120);
            hints.text = hintText ?? "";
        }

        public void Tick()
        {
            group.alpha = Mathf.MoveTowards(group.alpha, open ? 1f : 0f, Time.unscaledDeltaTime * 6f);
            root.gameObject.SetActive(group.alpha > 0.001f || open);
        }
    }
}
