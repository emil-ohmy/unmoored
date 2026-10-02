using UnityEngine;
using UnityEngine.UI;

namespace MyGame
{
    // Draws whichever Menu is open (the merchant's wares, Equipment at a post, the Tester's Post): a framed panel on the
    // right, leaving the Apprentice in view. Title, rows (the chosen one marked), the chosen row's description, a short
    // notice after confirming, and the keys for the device in use. More rows than fit: it scrolls (the chosen one kept
    // in view, a caret above or below when there are more that way).
    public sealed class MenuPanel
    {
        const int MaxRows = 8; // (shown at once)
        const float Width = 600f, RowHeight = 46f;

        readonly RectTransform root;
        readonly CanvasGroup group;
        readonly Text title, slots, description, notice, hints;
        readonly (RectTransform rect, Image highlight, Image marker, Text label, Text detail)[] rows;
        readonly RectTransform back;
        readonly Image up, down;
        readonly System.Collections.Generic.List<string> shown = new System.Collections.Generic.List<string>();
        Menu shownMenu;
        string shownTitle;
        int first;

        public int First => first;                                    // (tests) the first row in view
        public System.Collections.Generic.IReadOnlyList<string> ShownLabels => shown; // (tests) the rows in view

        public bool Visible => group.alpha > 0.01f;
        public string TitleText => title.text;

        public MenuPanel(Transform parent, Font titleFont, Font textFont)
        {
            var right = new Vector2(1f, 0.5f);
            root = UiKit.Rect(parent, "Menu", right, new Vector2(-90, 20), new Vector2(Width, 520));
            group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = group.blocksRaycasts = false;
            var frame = UiKit.Image(root, "Frame", null, UiKit.Bronze.WithAlpha(0.9f), Vector2.zero, Vector2.zero, Vector2.zero);
            UiKit.Stretch(frame.rectTransform);
            back = UiKit.Image(frame.rectTransform, "Back", null, UiKit.Hex(0x0b0a0e, 0.9f), Vector2.zero, Vector2.zero, Vector2.zero).rectTransform;
            UiKit.Stretch(back, 1f);

            var top = new Vector2(0f, 1f);
            title = Left(UiKit.Text(root, "Title", titleFont, 40, UiKit.Bone, TextAnchor.MiddleLeft, top, new Vector2(34, -40)));
            UiKit.Image(root, "Rule", UiKit.Rule, UiKit.Bone.WithAlpha(0.5f), new Vector2(0.5f, 1f), new Vector2(0, -72), new Vector2(Width - 40, 3));
            slots = Left(UiKit.Text(root, "Slots", textFont, 24, UiKit.Ash, TextAnchor.MiddleLeft, top, new Vector2(34, -96)));

            rows = new (RectTransform, Image, Image, Text, Text)[MaxRows];
            for (int i = 0; i < MaxRows; i++)
            {
                var row = UiKit.Rect(root, "Row", top, new Vector2(20, -120 - i * RowHeight), new Vector2(Width - 40, RowHeight - 4));
                row.pivot = top;
                var highlight = UiKit.Image(row, "Highlight", UiKit.Band, UiKit.Ember.WithAlpha(0.16f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Width + 80, RowHeight + 10));
                var marker = UiKit.Image(row, "Marker", null, UiKit.Ember, new Vector2(0f, 0.5f), new Vector2(6, 0), new Vector2(9, 9));
                marker.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
                var label = Left(UiKit.Text(row, "Label", textFont, 30, UiKit.Bone, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(26, 0)));
                var detail = UiKit.Text(row, "Detail", textFont, 26, UiKit.Ash, TextAnchor.MiddleRight, new Vector2(1f, 0.5f), new Vector2(-10, 0));
                detail.rectTransform.pivot = new Vector2(1f, 0.5f);
                detail.rectTransform.sizeDelta = new Vector2(260, 40);
                rows[i] = (row, highlight, marker, label, detail);
            }

            up = Caret(root, true);
            down = Caret(root, false);

            description = Left(UiKit.Text(root, "Description", textFont, 26, UiKit.Ash, TextAnchor.UpperLeft, top, new Vector2(34, -0)));
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            description.rectTransform.pivot = top;
            description.rectTransform.sizeDelta = new Vector2(Width - 68, 110);
            notice = Left(UiKit.Text(root, "Notice", textFont, 26, UiKit.Ember, TextAnchor.MiddleLeft, top, Vector2.zero));
            hints = Left(UiKit.Text(root, "Hints", textFont, 24, UiKit.Bone.WithAlpha(0.8f), TextAnchor.MiddleLeft, top, Vector2.zero));
        }

        // A small chevron (two strokes): more rows that way.
        static Image Caret(RectTransform root, bool pointsUp)
        {
            var caret = UiKit.Image(root, "Caret", null, Color.clear, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(30, 16));
            foreach (float side in new[] { -1f, 1f })
            {
                var stroke = UiKit.Image(caret.rectTransform, "Stroke", null, UiKit.Ember.WithAlpha(0.8f), new Vector2(0.5f, 0.5f), new Vector2(side * 6f, 0f), new Vector2(16, 3));
                stroke.rectTransform.localRotation = Quaternion.Euler(0, 0, (pointsUp ? -30f : 30f) * side);
            }
            return caret;
        }

        static Text Left(Text text)
        {
            text.rectTransform.pivot = new Vector2(0f, 0.5f);
            text.rectTransform.sizeDelta = new Vector2(Width - 60, text.fontSize * 1.5f);
            return text;
        }

        public void Tick(Menu menu, bool gamepad)
        {
            float target = menu != null ? 1f : 0f;
            group.alpha = Mathf.MoveTowards(group.alpha, target, UiKit.DeltaTime * 7f);
            root.gameObject.SetActive(group.alpha > 0.001f);
            if (menu == null)
                return;

            title.text = menu.Title;
            var post = menu as PostMenu;
            slots.text = post != null ? post.SlotsText : "";
            float y = post != null ? -120f : -96f;
            var list = menu.Rows;
            // Scrolled so the chosen row's in view (back to the top for a new menu, or a new page of it).
            if (menu != shownMenu || menu.Title != shownTitle)
            {
                shownMenu = menu;
                shownTitle = menu.Title;
                first = 0;
            }
            first = CharacterScreen.ScrollRows.Window(first, menu.Selected, list.Count, MaxRows);
            shown.Clear();
            for (int i = 0; i < MaxRows; i++)
            {
                var r = rows[i];
                int item = first + i;
                bool used = item < list.Count;
                r.rect.gameObject.SetActive(used);
                if (!used)
                    continue;
                r.rect.anchoredPosition = new Vector2(20, y - i * RowHeight);
                bool chosen = item == menu.Selected;
                r.highlight.enabled = r.marker.enabled = chosen;
                float a = list[item].Dim && !chosen ? 0.55f : 1f;
                r.label.text = list[item].Label;
                r.label.color = UiKit.Bone.WithAlpha(a);
                r.detail.text = list[item].Detail;
                r.detail.color = UiKit.Ash.WithAlpha(a);
                shown.Add(list[item].Label);
            }
            int inView = Mathf.Min(list.Count, MaxRows);
            up.gameObject.SetActive(first > 0);
            down.gameObject.SetActive(first + MaxRows < list.Count);
            up.rectTransform.anchoredPosition = new Vector2(0, y + 10f);
            down.rectTransform.anchoredPosition = new Vector2(0, y - inView * RowHeight - 6f);
            float below = y - Mathf.Max(1, inView) * RowHeight - 16f - (down.gameObject.activeSelf ? 12f : 0f);
            description.text = menu.Description;
            description.rectTransform.anchoredPosition = new Vector2(34, below);
            float age = UiKit.Now - menu.NoticeAt;
            notice.text = menu.Notice;
            notice.color = UiKit.Ember.WithAlpha(1f - UiKit.Smooth(1.6f, 2.2f, age));
            notice.rectTransform.anchoredPosition = new Vector2(34, below - 116f);
            string confirm = gamepad ? "(A)" : "[E]", leave = gamepad ? "(B)" : "[Esc]";
            hints.text = $"{confirm} {menu.ConfirmLabel}        {leave} {menu.LeaveLabel}";
            hints.rectTransform.anchoredPosition = new Vector2(34, below - 160f);
            root.sizeDelta = new Vector2(Width, -(below - 190f));
        }
    }
}
