using UnityEngine;
using UnityEngine.UI;

namespace MyGame
{
    // When the Apprentice falls: the world dims, a dark band draws across the screen and the death phrase glows up
    // in red, swelling slowly; then everything sinks to black just before the Apprentice wakes at the Mooring post,
    // and the world fades back in. Runs on game time (not unscaled) so the blackout stays in step with the respawn.
    public sealed class DeathScreen
    {
        public float BlackoutAlpha => blackout.color.a;
        public float MessageAlpha => group.alpha;
        public string Message => message.text;
        public float HudAlpha { get; private set; } = 1f; // the HUD fades out while dead and returns after waking

        readonly Image blackout, glow;
        readonly RectTransform messageRoot;
        readonly CanvasGroup group;
        readonly Text message;
        float diedAt = -100f, wokeAt = -100f, respawnDelay = 3f;
        bool dead;

        public DeathScreen(Transform parent, Font font, string phrase)
        {
            var centre = new Vector2(0.5f, 0.5f);
            blackout = UiKit.Image(parent, "Blackout", null, Color.black.WithAlpha(0f), centre, Vector2.zero, Vector2.zero);
            UiKit.Stretch(blackout.rectTransform, -4f);
            messageRoot = UiKit.Rect(parent, "Death Message", centre, new Vector2(0, 20), new Vector2(1920, 400));
            group = messageRoot.gameObject.AddComponent<CanvasGroup>();
            group.interactable = group.blocksRaycasts = false;
            UiKit.Image(messageRoot, "Band", UiKit.Band, Color.black.WithAlpha(0.85f), centre, Vector2.zero, new Vector2(2400, 320));
            glow = UiKit.Image(messageRoot, "Glow", UiKit.SoftDot, UiKit.Blood.WithAlpha(0.35f), centre, Vector2.zero, new Vector2(1300, 320));
            message = UiKit.Text(messageRoot, "Text", font, 136, UiKit.Blood, TextAnchor.MiddleCenter, centre, Vector2.zero, 12f);
            message.text = phrase;
            Apply(0f, 0f);
        }

        public void OnDied(float delayUntilWaking)
        {
            dead = true;
            diedAt = Time.time;
            respawnDelay = delayUntilWaking;
        }

        public void OnWoke()
        {
            dead = false;
            wokeAt = Time.time;
        }

        public void Tick()
        {
            if (dead)
            {
                float s = Time.time - diedAt, d = respawnDelay;
                // The world dims as the lantern goes out; the phrase comes once the Apprentice lies still.
                float dim = 0.55f * UiKit.Smooth(0.4f, 2.0f, s);
                Apply(Mathf.Lerp(dim, 1f, UiKit.Smooth(d - 0.8f, d - 0.1f, s)), UiKit.Smooth(1.8f, 2.7f, s));
                float grow = 1f - Mathf.Pow(1f - Mathf.Clamp01(s / d), 2f); // ease out
                messageRoot.localScale = Vector3.one * (0.94f + 0.08f * grow);
                glow.color = UiKit.Blood.WithAlpha(0.3f + 0.06f * Mathf.Sin(s * 2.4f));
                HudAlpha = 1f - UiKit.Smooth(0f, 0.4f, s);
            }
            else
            {
                float r = Time.time - wokeAt;
                Apply(1f - UiKit.Smooth(0.25f, 1.4f, r), 0f);
                HudAlpha = UiKit.Smooth(0.6f, 1.4f, r);
            }
        }

        void Apply(float black, float text)
        {
            blackout.color = Color.black.WithAlpha(black);
            blackout.enabled = black > 0.001f;
            group.alpha = text;
            messageRoot.gameObject.SetActive(text > 0.001f);
        }
    }
}
