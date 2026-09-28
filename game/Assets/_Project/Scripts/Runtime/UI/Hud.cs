using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace MyGame
{
    // The HUD, built in code: framed health (with a trail of the damage just taken) and stamina bars, Healing
    // charges as small flames, the Rest prompt with the key or button of the device in use, and the lock-on
    // target's health bar floating above it. Also the screen messages: the area title card, "Lantern Lit" when a
    // Mooring post is lit for the first time, a warm flash on every Rest, and the death screen.
    // No combat text: hits, blocks and dodges read from the world (sparks, flashes, sounds), not from words.
    public sealed class Hud : MonoBehaviour
    {
        public Apprentice apprentice;
        public Font titleFont, textFont;
        public string deathPhrase = "You Sank";

        public bool LockBarVisible => lockBar != null && lockBar.gameObject.activeSelf;
        public RectTransform LockBarRect => lockBar;
        public Banner AreaTitle { get; private set; }
        public Banner LanternBanner { get; private set; }
        public DeathScreen Death { get; private set; }
        public float PromptAlpha => promptGroup != null ? promptGroup.alpha : 0f;
        public bool GamepadGlyphs => usingGamepad;
        public int ObolsShown => Mathf.RoundToInt(obolsShown);
        public MenuPanel Menu { get; private set; }
        public string PromptText => promptLabel != null ? promptLabel.text : "";

        static readonly Color HealthRed = UiKit.Hex(0x8e2a22), StaminaMoss = UiKit.Hex(0x6f8a55);
        static readonly Color FlameOuter = UiKit.Hex(0xff9a45), FlameCore = UiKit.Hex(0xffe3a8), FlameSpent = UiKit.Hex(0x2a2629, 0.9f);

        RectTransform canvasRect, hpFill, hpTrail, staminaFill, lockBar, lockFill, lockTrail;
        CanvasGroup hudGroup, promptGroup;
        Image restFlash, keyCap, buttonRing;
        Text glyph, promptLabel, obolText, obolGain;
        float obolsShown, gainAt = -100f, healedAt = -100f;
        Image hpFillImage;
        int gainAmount;
        (RectTransform flame, Image outer, Image core, Image glow)[] charges;
        float hpTrailValue = 1f, hpLast = 1f, hpTrailHold, lockTrailValue, lockLast, lockTrailHold, restFlashAt = -100f;
        Soured barTarget;
        bool usingGamepad;

        void Start()
        {
            if (apprentice == null)
                apprentice = FindAnyObjectByType<Apprentice>();
            var canvasGo = new GameObject("HUD Canvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasRect = (RectTransform)canvasGo.transform;

            var hudRoot = UiKit.Stretch(UiKit.Rect(canvasRect, "HUD", Vector2.zero, Vector2.zero, Vector2.zero));
            hudGroup = hudRoot.gameObject.AddComponent<CanvasGroup>();
            BuildLockBar(hudRoot);
            hpFill = Bar(hudRoot, "Health", new Vector2(56, -46), 380, 14, HealthRed, out hpTrail);
            hpFillImage = hpFill.GetComponent<Image>();
            staminaFill = Bar(hudRoot, "Stamina", new Vector2(56, -70), 300, 8, StaminaMoss, out _);
            BuildCharges(hudRoot);
            BuildPrompt(hudRoot);
            BuildObols(hudRoot);

            restFlash = UiKit.Image(canvasRect, "Rest Flash", UiKit.SoftDot, UiKit.Ember.WithAlpha(0f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            UiKit.Stretch(restFlash.rectTransform, -600f);
            AreaTitle = new Banner(canvasRect, "Area Title", new Banner.Style
            {
                font = titleFont, size = 76, colour = UiKit.Bone, spacing = 7f, y = 190f, glow = Color.black.WithAlpha(0.6f),
                rule = true, fadeIn = 1.2f, hold = 2.6f, fadeOut = 1.6f, drift = 0.03f,
            });
            LanternBanner = new Banner(canvasRect, "Lantern Lit", new Banner.Style
            {
                font = titleFont, size = 68, colour = UiKit.Ember, spacing = 6f, y = 40f, glow = UiKit.Ember.WithAlpha(0.22f),
                bandAlpha = 0.6f, fadeIn = 0.5f, hold = 1.8f, fadeOut = 1.1f, drift = 0.025f,
            });
            Menu = new MenuPanel(canvasRect, titleFont, textFont);
            Death = new DeathScreen(canvasRect, titleFont, deathPhrase);

            usingGamepad = Gamepad.current != null;
            apprentice.Died += OnDied;
            apprentice.Woke += OnWoke;
            apprentice.ObolsChanged += OnObolsChanged;
            apprentice.Healed += OnHealed;
            obolsShown = apprentice.Obols;
            MooringPost.Rested += OnRested;
            AreaEntrance.Entered += OnAreaEntered;
        }

        void OnDestroy()
        {
            if (apprentice != null)
            {
                apprentice.Died -= OnDied;
                apprentice.Woke -= OnWoke;
                apprentice.ObolsChanged -= OnObolsChanged;
                apprentice.Healed -= OnHealed;
            }
            MooringPost.Rested -= OnRested;
            AreaEntrance.Entered -= OnAreaEntered;
        }

        void OnHealed() => healedAt = UiKit.Now;

        void OnObolsChanged(int delta)
        {
            if (delta <= 0)
                return;
            gainAmount = UiKit.Now - gainAt < 1.5f ? gainAmount + delta : delta; // gains close together add up
            gainAt = UiKit.Now;
        }

        void OnDied()
        {
            AreaTitle.Dismiss();
            LanternBanner.Dismiss();
            Death.OnDied(apprentice.respawnDelay);
        }

        void OnWoke() => Death.OnWoke();
        void OnAreaEntered(string area)
        {
            LanternBanner.Dismiss();
            AreaTitle.Show(area);
        }

        void OnRested(MooringPost post, bool firstLight)
        {
            restFlashAt = UiKit.Now;
            if (firstLight)
                LanternBanner.Show("Lantern Lit");
        }

        // LateUpdate: after all gameplay has run this frame, so the HUD never shows last frame's state.
        void LateUpdate()
        {
            if (apprentice == null)
                return;
            float now = UiKit.Now;
            float health = apprentice.Health / apprentice.maxHealth;
            hpFill.anchorMax = new Vector2(health, 1f);
            hpFillImage.color = Color.Lerp(HealthRed, UiKit.Ember, 0.8f * (1f - UiKit.Smooth(0.05f, 0.7f, now - healedAt))); // flares warm on a heal
            hpTrail.anchorMax = new Vector2(Trail(health, ref hpLast, ref hpTrailValue, ref hpTrailHold), 1f);
            staminaFill.anchorMax = new Vector2(apprentice.Stamina / apprentice.maxStamina, 1f);
            UpdateCharges(now);
            UpdatePrompt();
            UpdateObols();
            UpdateLockBar();

            Menu.Tick(apprentice.OpenMenu, usingGamepad);
            Death.Tick();
            hudGroup.alpha = Death.HudAlpha;
            AreaTitle.Tick();
            LanternBanner.Tick();
            float flash = now - restFlashAt;
            restFlash.color = UiKit.Ember.WithAlpha(0.09f * UiKit.Smooth(0f, 0.12f, flash) * (1f - UiKit.Smooth(0.12f, 1.4f, flash)));
            restFlash.enabled = restFlash.color.a > 0.001f;
        }

        // A pale trail behind a health fill: holds a moment after a hit, then drains down to the new value.
        static float Trail(float value, ref float last, ref float trail, ref float holdUntil)
        {
            if (value < last)
                holdUntil = UiKit.Now + 0.45f;
            last = value;
            if (UiKit.Now > holdUntil)
                trail = Mathf.MoveTowards(trail, value, UiKit.DeltaTime * 0.8f);
            return trail = Mathf.Max(trail, value);
        }

        void BuildCharges(Transform root)
        {
            charges = new (RectTransform, Image, Image, Image)[apprentice.maxHealingCharges];
            for (int i = 0; i < charges.Length; i++)
            {
                var slot = UiKit.Rect(root, "Charge", new Vector2(0, 1), new Vector2(66 + i * 30, -104), new Vector2(20, 30));
                var glow = UiKit.Image(slot, "Glow", UiKit.SoftDot, FlameOuter.WithAlpha(0.3f), new Vector2(0.5f, 0.35f), Vector2.zero, new Vector2(54, 54));
                var flame = UiKit.Rect(slot, "Flame", new Vector2(0.5f, 0f), Vector2.zero, new Vector2(20, 30));
                var outer = UiKit.Image(flame, "Outer", UiKit.Flame, FlameOuter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20, 30));
                var core = UiKit.Image(flame, "Core", UiKit.Flame, FlameCore, new Vector2(0.5f, 0f), new Vector2(0, 2), new Vector2(10, 15));
                charges[i] = (flame, outer, core, glow);
            }
        }

        void UpdateCharges(float now)
        {
            for (int i = 0; i < charges.Length; i++)
            {
                var c = charges[i];
                bool lit = i < apprentice.HealingCharges;
                c.outer.color = lit ? FlameOuter : FlameSpent;
                c.core.enabled = c.glow.enabled = lit;
                // Lit flames flicker, each out of step with the others.
                float flicker = lit ? 0.07f * Mathf.Sin(now * 9f + i * 1.7f) * Mathf.Sin(now * 5.3f + i * 2.9f) : 0f;
                c.flame.localScale = new Vector3(1f - flicker * 0.5f, 1f + flicker, 1f);
                if (lit)
                    c.glow.color = FlameOuter.WithAlpha(0.26f + flicker);
            }
        }

        // Bottom right: the count of Obols carried, ticking towards the true value, and "+40" rising when some come in.
        void BuildObols(Transform root)
        {
            var corner = new Vector2(1f, 0f);
            UiKit.Image(root, "Obol", UiKit.Coin, UiKit.Hex(0xc9bfa6), corner, new Vector2(-56, 54), new Vector2(30, 30));
            obolText = UiKit.Text(root, "Obols", textFont, 36, UiKit.Bone, TextAnchor.MiddleRight, corner, new Vector2(-84, 54));
            obolText.rectTransform.pivot = new Vector2(1f, 0.5f);
            obolText.rectTransform.sizeDelta = new Vector2(300, 50);
            obolGain = UiKit.Text(root, "Obol Gain", textFont, 28, UiKit.Ember, TextAnchor.MiddleRight, corner, new Vector2(-84, 96));
            obolGain.rectTransform.pivot = new Vector2(1f, 0.5f);
            obolGain.rectTransform.sizeDelta = new Vector2(300, 40);
        }

        void UpdateObols()
        {
            float target = apprentice.Obols;
            float rate = Mathf.Max(Mathf.Abs(target - obolsShown) * 2.5f, 40f);
            obolsShown = Mathf.MoveTowards(obolsShown, target, rate * UiKit.DeltaTime);
            obolText.text = Mathf.RoundToInt(obolsShown).ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
            float age = UiKit.Now - gainAt;
            obolGain.text = "+" + gainAmount;
            obolGain.color = UiKit.Ember.WithAlpha(UiKit.Smooth(0f, 0.1f, age) * (1f - UiKit.Smooth(1.2f, 2f, age)));
            obolGain.rectTransform.anchoredPosition = new Vector2(-84, 90 + 10f * Mathf.Clamp01(age / 2f));
        }

        void BuildPrompt(Transform root)
        {
            var prompt = UiKit.Rect(root, "Prompt", new Vector2(0.5f, 0f), new Vector2(0, 118), new Vector2(300, 60));
            promptGroup = prompt.gameObject.AddComponent<CanvasGroup>();
            promptGroup.alpha = 0f;
            UiKit.Image(prompt, "Band", UiKit.Band, Color.black.WithAlpha(0.55f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420, 80));
            keyCap = UiKit.Image(prompt, "Key", UiKit.KeyCap, UiKit.Bone.WithAlpha(0.85f), new Vector2(0.5f, 0.5f), new Vector2(-40, 0), new Vector2(42, 42));
            buttonRing = UiKit.Image(prompt, "Button", UiKit.Ring, UiKit.Bone.WithAlpha(0.85f), new Vector2(0.5f, 0.5f), new Vector2(-40, 0), new Vector2(42, 42));
            glyph = UiKit.Text(prompt, "Glyph", textFont, 24, UiKit.Bone, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(-40, 1));
            glyph.rectTransform.sizeDelta = new Vector2(42, 42);
            var label = UiKit.Text(prompt, "Label", textFont, 32, UiKit.Bone, TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f), new Vector2(-10, 0));
            label.rectTransform.pivot = new Vector2(0f, 0.5f);
            label.rectTransform.sizeDelta = new Vector2(200, 50);
            label.text = "Rest";
            promptLabel = label;
        }

        void UpdatePrompt()
        {
            TrackDevice();
            keyCap.enabled = !usingGamepad;
            buttonRing.enabled = usingGamepad;
            glyph.text = usingGamepad ? "A" : "E";
            if (apprentice.Nearby != null)
                promptLabel.text = apprentice.Nearby.PromptLabel;
            float target = apprentice.Nearby != null && apprentice.IsAlive ? 1f : 0f;
            promptGroup.alpha = Mathf.MoveTowards(promptGroup.alpha, target, UiKit.DeltaTime * 5f);
        }

        // Show the keys of whichever device was touched last.
        void TrackDevice()
        {
            var pad = Gamepad.current;
            if (pad != null && (pad.leftStick.ReadValue().sqrMagnitude > 0.25f || AnyButtonPressed(pad)))
                usingGamepad = true;
            else if ((Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                     || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame))
                usingGamepad = false;
        }

        static bool AnyButtonPressed(Gamepad pad)
        {
            foreach (var control in pad.allControls)
                if (control is ButtonControl button && button.wasPressedThisFrame)
                    return true;
            return false;
        }

        void BuildLockBar(Transform root)
        {
            var frame = UiKit.Image(root, "Lock Bar", null, UiKit.Bronze.WithAlpha(0.9f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(128, 12));
            lockBar = frame.rectTransform;
            UiKit.Stretch(UiKit.Image(lockBar, "Back", null, UiKit.Hex(0x0b0a0e, 0.9f), Vector2.zero, Vector2.zero, Vector2.zero).rectTransform, 1f);
            lockTrail = Fill(lockBar, UiKit.Bone.WithAlpha(0.8f));
            lockFill = Fill(lockBar, HealthRed);
            lockFill.GetComponent<Image>().sprite = UiKit.Sheen;
            var marker = UiKit.Image(lockBar, "Lock Marker", null, UiKit.Bone, new Vector2(0.5f, 0.5f), new Vector2(0, 17), new Vector2(9, 9));
            marker.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            lockBar.gameObject.SetActive(false);
        }

        static RectTransform Fill(RectTransform parent, Color colour)
        {
            var fill = UiKit.Image(parent, "Fill", null, colour, Vector2.zero, Vector2.zero, Vector2.zero).rectTransform;
            UiKit.Stretch(fill, 2f);
            fill.pivot = new Vector2(0, 0.5f);
            return fill;
        }

        void UpdateLockBar()
        {
            var target = apprentice.LockTarget;
            var cam = Camera.main;
            bool show = target != null && target.IsAlive && cam != null;
            Vector3 screen = show ? cam.WorldToScreenPoint(target.transform.position + Vector3.up * target.barHeight) : Vector3.zero;
            show &= screen.z > 0f;
            lockBar.gameObject.SetActive(show);
            if (!show)
                return;

            float health = target.HealthFraction;
            if (target != barTarget)
            {
                barTarget = target;
                lockTrailValue = lockLast = health;
            }
            lockFill.anchorMax = new Vector2(health, 1f);
            lockTrail.anchorMax = new Vector2(Trail(health, ref lockLast, ref lockTrailValue, ref lockTrailHold), 1f);

            var canvas = canvasRect.GetComponent<Canvas>(); // overlay canvases convert without a camera; camera-space ones need it
            var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, uiCamera, out var local);
            lockBar.anchoredPosition = local;
        }

        // A framed bar at the top left: a thin bronze frame with a diamond at its left end, a dark back, a pale
        // trail (optional use) and the fill. Returns the fill; its anchorMax.x is the value.
        static RectTransform Bar(Transform parent, string name, Vector2 position, float width, float height, Color colour, out RectTransform trail)
        {
            var top = new Vector2(0, 1);
            var frame = UiKit.Image(parent, name, null, UiKit.Bronze.WithAlpha(0.9f), top, position, new Vector2(width + 4, height + 4)).rectTransform;
            frame.pivot = top;
            UiKit.Stretch(UiKit.Image(frame, "Back", null, UiKit.Hex(0x0b0a0e, 0.85f), Vector2.zero, Vector2.zero, Vector2.zero).rectTransform, 1f);
            trail = Fill(frame, UiKit.Bone.WithAlpha(0.75f));
            trail.anchorMax = new Vector2(0f, 1f);
            var fill = Fill(frame, colour);
            fill.GetComponent<Image>().sprite = UiKit.Sheen;
            var cap = UiKit.Image(parent, name + " Cap", null, UiKit.Bronze, top, position + new Vector2(-6, -(height + 4) / 2f), new Vector2(10, 10));
            cap.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            cap.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            return fill;
        }
    }
}
