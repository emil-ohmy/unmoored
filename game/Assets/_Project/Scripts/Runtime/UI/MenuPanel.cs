using UnityEngine;
using UnityEngine.UI;

namespace MyGame
{
    // Draws whichever Menu is open (the merchant's wares, Equipment at a post): a framed panel on the right, leaving
    // the Apprentice in view. Title, rows (the chosen one marked), the chosen row's description, a short notice
    // after confirming, and the keys for the device in use.
    public sealed class MenuPanel
    {
        const int MaxRows = 12; // (the Tester's Post's Spawn page: every kind, and its tools)
        const float Width = 600f, RowHeight = 46f;

        readonly RectTransform root;
        readonly CanvasGroup group;
        readonly Text title, slots, description, notice, hints;
        readonly (RectTransform rect, Image highlight, Image marker, Text label, Text detail)[] rows;
        readonly RectTransform back;

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

            description = Left(UiKit.Text(root, "Description", textFont, 26, UiKit.Ash, TextAnchor.UpperLeft, top, new Vector2(34, -0)));
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            description.rectTransform.pivot = top;
            description.rectTransform.sizeDelta = new Vector2(Width - 68, 110);
            notice = Left(UiKit.Text(root, "Notice", textFont, 26, UiKit.Ember, TextAnchor.MiddleLeft, top, Vector2.zero));
            hints = Left(UiKit.Text(root, "Hints", textFont, 24, UiKit.Bone.WithAlpha(0.8f), TextAnchor.MiddleLeft, top, Vector2.zero));
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
            for (int i = 0; i < MaxRows; i++)
            {
                var r = rows[i];
                bool used = i < list.Count;
                r.rect.gameObject.SetActive(used);
                if (!used)
                    continue;
                r.rect.anchoredPosition = new Vector2(20, y - i * RowHeight);
                bool chosen = i == menu.Selected;
                r.highlight.enabled = r.marker.enabled = chosen;
                float a = list[i].Dim && !chosen ? 0.55f : 1f;
                r.label.text = list[i].Label;
                r.label.color = UiKit.Bone.WithAlpha(a);
                r.detail.text = list[i].Detail;
                r.detail.color = UiKit.Ash.WithAlpha(a);
            }
            float below = y - Mathf.Max(1, list.Count) * RowHeight - 16f;
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
