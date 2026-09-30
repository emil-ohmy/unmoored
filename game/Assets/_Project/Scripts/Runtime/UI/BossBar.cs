using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MyGame
{
    // A Boss on screen. When its fight begins, the first time: its full title revealed large across the screen (the
    // name, a rule, the epithet beneath). Then, every fight: a wide framed bar along the bottom, the name above its
    // left end, a notch where each new phase begins (dimming once passed), a pale trail behind the damage. When the
    // fight ends it fades: at once if the Apprentice falls, after draining to nothing if the Boss is beaten.
    public sealed class BossBar
    {
        const float Width = 860f, Height = 12f, Y = 58f;
        static readonly Color Fill = UiKit.Hex(0x8e2a22);

        public bool Visible => group.alpha > 0.01f;
        public float Alpha => group.alpha;
        public string Name => name.text;
        public float FillShown => fill.anchorMax.x;
        public Banner Reveal { get; }

        readonly RectTransform root, fill, trail, frame;
        readonly List<GameObject> caps = new List<GameObject>();
        // A Boss of parts (IBossParts: the Unparted's Hold and Fast): two bars side by side, each named, in place of one.
        readonly (RectTransform frame, RectTransform fill, RectTransform trail, Text label)[] parts = new (RectTransform, RectTransform, RectTransform, Text)[2];
        readonly float[] partTrail = new float[2];
        public string PartName(int i) => parts[i].label.text;
        public bool Parted { get; private set; }
        readonly CanvasGroup group;
        readonly Text name;
        readonly Banner epithet;
        readonly List<(RectTransform mark, Image image, float at)> notches = new List<(RectTransform, Image, float)>();
        readonly HashSet<IBoss> revealed = new HashSet<IBoss>();
        IBoss shown;
        float trailValue, last, holdUntil, endedAt = -1f;

        public BossBar(Transform parent, Font titleFont, Font textFont)
        {
            var bottom = new Vector2(0.5f, 0f);
            root = UiKit.Rect(parent, "Boss Bar", bottom, new Vector2(0, Y), new Vector2(Width + 4, Height + 4));
            group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = group.blocksRaycasts = false;
            // Under it all, a soft dark band so it reads over bright water.
            var band = UiKit.Image(root, "Band", UiKit.Band, Color.black.WithAlpha(0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 16), new Vector2(Width + 260, 110));
            band.transform.SetAsFirstSibling();
            frame = UiKit.Image(root, "Frame", null, UiKit.Bronze.WithAlpha(0.95f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Width + 4, Height + 4)).rectTransform;
            UiKit.Stretch(UiKit.Image(frame, "Back", null, UiKit.Hex(0x0b0a0e, 0.9f), Vector2.zero, Vector2.zero, Vector2.zero).rectTransform, 1f);
            trail = Bar(frame, UiKit.Bone.WithAlpha(0.75f));
            fill = Bar(frame, Fill);
            fill.GetComponent<Image>().sprite = UiKit.Sheen;
            foreach (float x in new[] { -1f, 1f }) // bronze diamonds capping both ends
            {
                var cap = UiKit.Image(root, "Cap", null, UiKit.Bronze, new Vector2(0.5f, 0.5f), new Vector2(x * (Width / 2f + 7f), 0), new Vector2(12, 12));
                cap.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
                caps.Add(cap.gameObject);
            }
            float half = (Width - 40f) / 2f;
            for (int i = 0; i < 2; i++)
            {
                float x = (i == 0 ? -1f : 1f) * (half / 2f + 10f);
                var partFrame = UiKit.Image(root, "Part Frame", null, UiKit.Bronze.WithAlpha(0.95f), new Vector2(0.5f, 0.5f), new Vector2(x, 0), new Vector2(half + 4, Height + 4)).rectTransform;
                UiKit.Stretch(UiKit.Image(partFrame, "Back", null, UiKit.Hex(0x0b0a0e, 0.9f), Vector2.zero, Vector2.zero, Vector2.zero).rectTransform, 1f);
                var partTrailBar = Bar(partFrame, UiKit.Bone.WithAlpha(0.75f));
                var partFill = Bar(partFrame, Fill);
                partFill.GetComponent<Image>().sprite = UiKit.Sheen;
                var label = UiKit.Text(root, "Part Name", textFont, 24, UiKit.Ash, i == 0 ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight,
                                       new Vector2(0.5f, 0.5f), new Vector2(x + (i == 0 ? -half / 2f : half / 2f), -22), 2f);
                label.rectTransform.pivot = new Vector2(i == 0 ? 0f : 1f, 0.5f);
                label.rectTransform.sizeDelta = new Vector2(half, 30);
                partFrame.gameObject.SetActive(false);
                label.gameObject.SetActive(false);
                parts[i] = (partFrame, partFill, partTrailBar, label);
            }
            name = UiKit.Text(root, "Name", titleFont, 32, UiKit.Bone, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(2, 30), 3f);
            name.rectTransform.pivot = new Vector2(0f, 0.5f);
            name.rectTransform.sizeDelta = new Vector2(Width, 44);

            Reveal = new Banner(parent, "Boss Reveal", new Banner.Style
            {
                font = titleFont, size = 84, colour = UiKit.Bone, spacing = 8f, y = 150f, glow = Color.black.WithAlpha(0.7f),
                rule = true, bandAlpha = 0.55f, fadeIn = 1.1f, hold = 2.2f, fadeOut = 1.4f, drift = 0.035f,
            });
            epithet = new Banner(parent, "Boss Epithet", new Banner.Style
            {
                font = textFont, size = 38, colour = UiKit.Ash, spacing = 3f, y = 36f, fadeIn = 1.5f, hold = 1.8f, fadeOut = 1.4f, drift = 0.02f,
            });
        }

        static RectTransform Bar(RectTransform parent, Color colour)
        {
            var bar = UiKit.Image(parent, "Fill", null, colour, Vector2.zero, Vector2.zero, Vector2.zero).rectTransform;
            UiKit.Stretch(bar, 2f);
            bar.pivot = new Vector2(0f, 0.5f);
            return bar;
        }

        public void Tick(IBoss current)
        {
            Reveal.Tick();
            epithet.Tick();
            if (current != null && current != shown)
                Begin(current);
            else if (current != null && endedAt >= 0f)
                endedAt = -1f; // back into the fight before the bar had gone
            else if (current == null && shown != null && endedAt < 0f)
                endedAt = UiKit.Now; // the fight's over: fade (after draining, if the Boss is beaten)
            if (shown == null)
                return;

            float health = shown.IsAlive ? shown.HealthFraction : 0f;
            fill.anchorMax = new Vector2(health, 1f);
            if (health < last)
                holdUntil = UiKit.Now + 0.45f;
            last = health;
            if (UiKit.Now > holdUntil)
                trailValue = Mathf.MoveTowards(trailValue, health, UiKit.DeltaTime * 0.5f);
            trailValue = Mathf.Max(trailValue, health);
            trail.anchorMax = new Vector2(trailValue, 1f);
            foreach (var (mark, image, at) in notches)
                image.color = UiKit.Bone.WithAlpha(health > at ? 0.9f : 0.25f); // passed: its phase has begun
            if (Parted && shown is IBossParts split)
            {
                var list = split.Parts;
                for (int i = 0; i < 2 && i < list.Count; i++)
                {
                    float f = list[i].fraction;
                    parts[i].fill.anchorMax = new Vector2(f, 1f);
                    partTrail[i] = Mathf.Max(f, Mathf.MoveTowards(partTrail[i], f, UiKit.DeltaTime * 0.5f));
                    parts[i].trail.anchorMax = new Vector2(partTrail[i], 1f);
                    parts[i].label.color = UiKit.Ash.WithAlpha(f > 0f ? 1f : 0.4f); // (scattered: its name dims)
                }
            }

            float target = 1f;
            if (endedAt >= 0f)
            {
                // Beaten: let the trail drain to nothing first. Fallen: fade at once.
                float wait = shown.IsAlive ? 0f : 1.6f;
                target = UiKit.Now - endedAt < wait ? 1f : 0f;
            }
            group.alpha = Mathf.MoveTowards(group.alpha, target, UiKit.DeltaTime * (target > 0f ? 1.2f : 0.9f));
            if (target == 0f && group.alpha <= 0f)
                shown = null;
        }

        void Begin(IBoss boss)
        {
            shown = boss;
            endedAt = -1f;
            name.text = boss.Title;
            trailValue = last = boss.HealthFraction;
            Parted = boss is IBossParts split && split.Parts.Count == 2;
            frame.gameObject.SetActive(!Parted);
            foreach (var cap in caps)
                cap.SetActive(!Parted);
            for (int i = 0; i < 2; i++)
            {
                parts[i].frame.gameObject.SetActive(Parted);
                parts[i].label.gameObject.SetActive(Parted);
                if (Parted)
                {
                    var (partName, fraction) = ((IBossParts)boss).Parts[i];
                    parts[i].label.text = partName;
                    partTrail[i] = fraction;
                }
            }
            foreach (var (mark, _, _) in notches)
                Object.Destroy(mark.gameObject);
            notches.Clear();
            foreach (float at in boss.PhaseMarks)
            {
                var image = UiKit.Image(root, "Phase Notch", null, UiKit.Bone, new Vector2(0f, 0.5f), new Vector2(2 + Width * at, 0), new Vector2(3, Height + 12));
                notches.Add((image.rectTransform, image, at));
            }
            if (revealed.Add(boss)) // the first time: the whole of its title, large
            {
                Reveal.Show(boss.Title);
                if (!string.IsNullOrEmpty(boss.Epithet))
                    epithet.Show(boss.Epithet);
            }
        }
    }
}
