using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MyGame
{
    // Building blocks for the code-built UI: element helpers and a few soft shapes drawn into textures at runtime
    // (no image assets to keep in sync): a glow dot, a feathered band, a fading rule, a flame, a ring and a key cap.
    public static class UiKit
    {
        public static readonly Color Bone = Hex(0xe8dcc0);
        public static readonly Color Ash = Hex(0xa89f8a);
        public static readonly Color Ember = Hex(0xffb866);
        public static readonly Color Blood = Hex(0x9e2f27);
        // Information: what matters in a line (a name written in the Ledger, a key to press, a thing a tutorial
        // points at), pale gold, as a Zelda game colours the words that matter. In text: *marked like this*.
        public static readonly Color Info = Hex(0xe8c170);
        const string InfoTag = "<color=#E8C170>";

        // A line's *marked* words in the Info colour (rich text).
        public static string Marked(string text) =>
            string.IsNullOrEmpty(text) ? text : System.Text.RegularExpressions.Regex.Replace(text, @"\*([^*]+)\*", InfoTag + "$1</color>");

        // The same line, its marks taken out (what it says, plainly).
        public static string Unmarked(string text) => string.IsNullOrEmpty(text) ? text : text.Replace("*", "");
        public static readonly Color Bronze = Hex(0x5a4c3c);

        // The UI's clock: real time, so hit-stop doesn't freeze it, except while a recording steps the game at a fixed
        // frame rate (Time.captureDeltaTime), when real time runs apart from the frames and game time is what's seen.
        public static float Now => Time.captureDeltaTime > 0f ? Time.time : Time.unscaledTime;
        public static float DeltaTime => Time.captureDeltaTime > 0f ? Time.deltaTime : Time.unscaledDeltaTime;

        public static Color Hex(uint rgb, float alpha = 1f) =>
            new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, alpha);

        public static Color WithAlpha(this Color c, float a) => new Color(c.r, c.g, c.b, a);

        public static float Smooth(float from, float to, float t) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(from, to, t)); // HLSL smoothstep

        public static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        // Stretches to fill its parent, inset by `inset` pixels on every side.
        public static RectTransform Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        public static Image Image(Transform parent, string name, Sprite sprite, Color colour, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var image = Rect(parent, name, anchor, position, size).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = colour;
            image.raycastTarget = false;
            return image;
        }

        public static Text Text(Transform parent, string name, Font font, int size, Color colour, TextAnchor alignment,
                                Vector2 anchor, Vector2 position, float spacing = 0f)
        {
            var text = Rect(parent, name, anchor, position, new Vector2(1600, size * 1.6f)).gameObject.AddComponent<Text>();
            text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.color = colour;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            if (spacing != 0f)
                text.gameObject.AddComponent<LetterSpacing>().spacing = spacing;
            return text;
        }

        // A soft round glow, brightest in the middle.
        public static Sprite SoftDot => Cached("SoftDot", 128, 128, (u, v) =>
        {
            float d = Mathf.Clamp01(Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f);
            return Sq(1f - Smooth(0f, 1f, d));
        });

        // Clear in the middle, darkening towards the edges and corners.
        public static Sprite Vignette => Cached("Vignette", 128, 72, (u, v) =>
        {
            float d = new Vector2((u - 0.5f) * 1.6f, (v - 0.5f) * 1.2f).magnitude;
            return Smooth(0.3f, 0.95f, d);
        });

        // A horizontal band that feathers out at its top and bottom and fades away towards both ends.
        public static Sprite Band => Cached("Band", 256, 64, (u, v) =>
            (1f - Smooth(0.3f, 1f, Mathf.Abs(2f * v - 1f))) * (1f - Smooth(0.45f, 1f, Mathf.Abs(2f * u - 1f))));

        // A thin line, strongest in the middle, fading out at both ends.
        public static Sprite Rule => Cached("Rule", 256, 8, (u, v) =>
            Mathf.Pow(1f - Mathf.Abs(2f * u - 1f), 1.6f) * (1f - Smooth(0.4f, 1f, Mathf.Abs(2f * v - 1f))));

        // A teardrop flame: round at the bottom, drawn up into a point.
        public static Sprite Flame => Cached("Flame", 64, 96, (u, v) =>
        {
            const float cx = 0.5f, cy = 0.34f, r = 0.3f, tip = 0.98f;
            float x = Mathf.Abs(u - cx) * 64f / 96f; // keep the base round in the 2:3 texture
            float dist;
            if (v <= cy)
                dist = Mathf.Sqrt(x * x + (v - cy) * (v - cy)) - r;
            else
            {
                float s = Mathf.Clamp01((v - cy) / (tip - cy));
                dist = x - r * Mathf.Pow(1f - s, 1.35f) * (1f + 0.35f * s) * 64f / 96f;
            }
            return 1f - Smooth(-0.012f, 0.012f, dist);
        });

        // An outlined circle (a gamepad button).
        public static Sprite Ring => Cached("Ring", 96, 96, (u, v) =>
        {
            float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f));
            return 1f - Smooth(0.025f, 0.045f, Mathf.Abs(d - 0.44f));
        });

        // An outlined rounded square (a key).
        public static Sprite KeyCap => Cached("KeyCap", 96, 96, (u, v) =>
        {
            float qx = Mathf.Abs(u - 0.5f) - 0.3f, qy = Mathf.Abs(v - 0.5f) - 0.3f;
            float d = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - 0.14f;
            return 1f - Smooth(0.02f, 0.04f, Mathf.Abs(d));
        });

        // An Obol: a worn disc with a raised rim and a stamped centre (shading in the grey channel, for tinting).
        public static Sprite Coin => Cached("Coin", 64, 64,
            (u, v) => 1f - Smooth(0.44f, 0.47f, Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f))),
            (u, v) =>
            {
                float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f));
                float rim = Smooth(0.34f, 0.37f, d);
                float stamp = 1f - Smooth(0.12f, 0.15f, d);
                return 0.55f + 0.35f * rim + 0.25f * stamp + 0.15f * (v - 0.5f);
            });

        // Provision icons (the belt, the Satchel): a round-bellied pot with a neck, a flask with a cork, a squat tin, a
        // knotted loop of cord. Shading in the grey channel (lit from above), for tinting.
        public static Sprite PotIcon => Cached("PotIcon", 64, 64, (u, v) =>
        {
            float belly = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.4f)) - 0.3f;
            float neck = Mathf.Max(Mathf.Abs(u - 0.5f) - 0.1f, Mathf.Abs(v - 0.75f) - 0.12f);
            float rim = Mathf.Max(Mathf.Abs(u - 0.5f) - 0.15f, Mathf.Abs(v - 0.86f) - 0.04f);
            return 1f - Smooth(-0.01f, 0.01f, Mathf.Min(belly, Mathf.Min(neck, rim)));
        }, (u, v) => Mathf.Lerp(0.6f, 1f, v));

        public static Sprite FlaskIcon => Cached("FlaskIcon", 64, 64, (u, v) =>
        {
            float body = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.38f)) - 0.27f;
            float neck = Mathf.Max(Mathf.Abs(u - 0.5f) - 0.07f, Mathf.Abs(v - 0.7f) - 0.12f);
            float cork = Mathf.Max(Mathf.Abs(u - 0.5f) - 0.09f, Mathf.Abs(v - 0.86f) - 0.06f);
            return 1f - Smooth(-0.01f, 0.01f, Mathf.Min(body, Mathf.Min(neck, cork)));
        }, (u, v) => v > 0.8f ? 0.55f : Mathf.Lerp(0.7f, 1f, v));

        public static Sprite TinIcon => Cached("TinIcon", 64, 64, (u, v) =>
        {
            float qx = Mathf.Abs(u - 0.5f) - 0.28f, qy = Mathf.Abs(v - 0.42f) - 0.18f;
            float d = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - 0.04f;
            return 1f - Smooth(-0.01f, 0.01f, d);
        }, (u, v) => v > 0.55f ? 1f : 0.65f);

        public static Sprite CordIcon => Cached("CordIcon", 64, 64, (u, v) =>
        {
            float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f));
            float loop = Mathf.Abs(d - 0.3f) - 0.05f;
            float knot = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.2f)) - 0.1f;
            float tail = Mathf.Max(Mathf.Abs(u - 0.62f - (0.2f - v) * 0.5f) - 0.035f, Mathf.Abs(v - 0.1f) - 0.1f);
            return 1f - Smooth(-0.01f, 0.01f, Mathf.Min(loop, Mathf.Min(knot, tail)));
        });

        // A gnarled root, forked at the foot, a twist of twine round its neck.
        public static Sprite RootIcon => Cached("RootIcon", 64, 64, (u, v) =>
        {
            float stem = Mathf.Abs(u - 0.5f - 0.06f * Mathf.Sin(v * 9f)) - (0.07f + 0.05f * v);
            stem = Mathf.Max(stem, Mathf.Max(0.2f - v, v - 0.88f));
            float forkL = Mathf.Abs(u - 0.5f + (0.3f - v) * 0.9f) - 0.04f, forkR = Mathf.Abs(u - 0.5f - (0.3f - v) * 0.7f) - 0.035f;
            float fork = Mathf.Max(Mathf.Min(forkL, forkR), Mathf.Max(v - 0.32f, 0.06f - v));
            return 1f - Smooth(-0.01f, 0.01f, Mathf.Min(stem, fork));
        }, (u, v) => Mathf.Abs(v - 0.7f) < 0.04f ? 0.55f : Mathf.Lerp(0.65f, 1f, v));

        // A sack, slumped and tied at the neck, a drop falling from it.
        public static Sprite SackIcon => Cached("SackIcon", 64, 64, (u, v) =>
        {
            float body = Vector2.Distance(new Vector2(u, v * 1.25f), new Vector2(0.5f, 0.45f)) - 0.3f;
            float neck = Mathf.Max(Mathf.Abs(u - 0.5f) - 0.07f, Mathf.Max(0.66f - v, v - 0.8f));
            float ears = Mathf.Max(Mathf.Abs(Mathf.Abs(u - 0.5f) - 0.09f) - 0.05f, Mathf.Max(0.78f - v, v - 0.9f));
            float drop = Vector2.Distance(new Vector2(u, v), new Vector2(0.78f, 0.12f)) - 0.05f;
            return 1f - Smooth(-0.01f, 0.01f, Mathf.Min(Mathf.Min(body, neck), Mathf.Min(ears, drop)));
        }, (u, v) => Mathf.Abs(v - 0.66f) < 0.03f ? 0.5f : Mathf.Lerp(0.6f, 1f, v));

        // Lighter at the top than the bottom: gives a bar's fill a little body.
        public static Sprite Sheen => Cached("Sheen", 4, 32, (u, v) => 1f, (u, v) => Mathf.Lerp(0.62f, 1f, Mathf.Pow(v, 0.7f)));

        static float Sq(float x) => x * x;

        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        static Sprite Cached(string name, int width, int height, Func<float, float, float> alpha, Func<float, float, float> grey = null)
        {
            if (cache.TryGetValue(name, out var sprite) && sprite != null)
                return sprite;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "UI_" + name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / width, v = (y + 0.5f) / height;
                    byte g = (byte)(255 * Mathf.Clamp01(grey?.Invoke(u, v) ?? 1f));
                    pixels[y * width + x] = new Color32(g, g, g, (byte)(255 * Mathf.Clamp01(alpha(u, v))));
                }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            cache[name] = sprite;
            return sprite;
        }
    }

    // Letter spacing for a single-line legacy Text: spreads the glyph quads apart by `spacing` pixels, keeping the
    // line centred (or left/right aligned) where it was. Spaces have no quads, so they're skipped when matching.
    [RequireComponent(typeof(Text))]
    public sealed class LetterSpacing : BaseMeshEffect
    {
        public float spacing;
        static readonly List<UIVertex> stream = new List<UIVertex>();

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || spacing == 0f)
                return;
            var text = GetComponent<Text>();
            string s = text.text;
            stream.Clear();
            vh.GetUIVertexStream(stream);
            int quads = stream.Count / 6;
            if (quads == 0)
                return;
            // Character index of each quad: all characters if every one has a quad, otherwise the visible ones.
            var slots = new List<int>(quads);
            for (int i = 0; i < s.Length && slots.Count < quads; i++)
                if (quads >= s.Length || !char.IsWhiteSpace(s[i]))
                    slots.Add(i);
            float total = (s.Length - 1) * spacing;
            float shift = text.alignment switch
            {
                TextAnchor.UpperLeft or TextAnchor.MiddleLeft or TextAnchor.LowerLeft => 0f,
                TextAnchor.UpperRight or TextAnchor.MiddleRight or TextAnchor.LowerRight => -total,
                _ => -total * 0.5f,
            };
            for (int q = 0; q < quads && q < slots.Count; q++)
            {
                float dx = slots[q] * spacing + shift;
                for (int k = 0; k < 6; k++)
                {
                    var vertex = stream[q * 6 + k];
                    vertex.position.x += dx;
                    stream[q * 6 + k] = vertex;
                }
            }
            vh.Clear();
            vh.AddUIVertexTriangleStream(stream);
        }
    }
}
