using UnityEngine;
using UnityEngine.UI;

namespace MyGame
{
    // A centred message that fades in, holds and fades out, drifting very slightly larger as it shows: the area
    // title card and "Lantern Lit". Optionally sits on a dark feathered band, with a soft glow and a rule beneath.
    public sealed class Banner
    {
        public readonly Text Text;
        public float Alpha => group.alpha;
        public bool Showing => UiKit.Now - shownAt < fadeIn + hold + fadeOut;

        readonly RectTransform root;
        readonly CanvasGroup group;
        readonly float fadeIn, hold, fadeOut, drift;
        float shownAt = -100f;

        public struct Style
        {
            public Font font;
            public int size;
            public Color colour;
            public float spacing, y;
            public Color glow;      // alpha 0: none
            public float bandAlpha; // 0: no band
            public bool rule;
            public float fadeIn, hold, fadeOut, drift;
        }

        public Banner(Transform parent, string name, Style s)
        {
            root = UiKit.Rect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0, s.y), new Vector2(1600, 300));
            group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = group.blocksRaycasts = false;
            var centre = new Vector2(0.5f, 0.5f);
            if (s.bandAlpha > 0f)
                UiKit.Image(root, "Band", UiKit.Band, Color.black.WithAlpha(s.bandAlpha), centre, Vector2.zero, new Vector2(1700, s.size * 2.6f));
            if (s.glow.a > 0f)
                UiKit.Image(root, "Glow", UiKit.SoftDot, s.glow, centre, Vector2.zero, new Vector2(s.size * 11f, s.size * 2.8f));
            Text = UiKit.Text(root, "Text", s.font, s.size, s.colour, TextAnchor.MiddleCenter, centre, Vector2.zero, s.spacing);
            if (s.rule)
                UiKit.Image(root, "Rule", UiKit.Rule, s.colour.WithAlpha(0.7f), centre, new Vector2(0, -s.size * 0.72f), new Vector2(s.size * 12f, 3));
            fadeIn = s.fadeIn;
            hold = s.hold;
            fadeOut = s.fadeOut;
            drift = s.drift;
            root.gameObject.SetActive(false);
        }

        public void Show(string text)
        {
            Text.text = text;
            shownAt = UiKit.Now;
            root.gameObject.SetActive(true);
        }

        // Start fading out now (another message is taking the screen).
        public void Dismiss()
        {
            float now = UiKit.Now, age = now - shownAt;
            if (Showing && age < fadeIn + hold)
                shownAt = now - (fadeIn + hold + fadeOut * (1f - Alpha)); // continue from the current alpha
        }

        public void Tick()
        {
            if (!root.gameObject.activeSelf)
                return;
            float age = UiKit.Now - shownAt, total = fadeIn + hold + fadeOut;
            group.alpha = UiKit.Smooth(0f, fadeIn, age) * (1f - UiKit.Smooth(fadeIn + hold, total, age));
            root.localScale = Vector3.one * (1f + drift * Mathf.Clamp01(age / total));
            if (age >= total)
                root.gameObject.SetActive(false);
        }
    }
}
