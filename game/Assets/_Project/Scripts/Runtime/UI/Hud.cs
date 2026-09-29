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
        public bool LockMarkerVisible => lockMarker != null && lockMarker.gameObject.activeSelf;
        public RectTransform LockBarRect => lockBar;
        public Banner AreaTitle { get; private set; }
        public Banner LanternBanner { get; private set; }
        public DeathScreen Death { get; private set; }
        public float PromptAlpha => promptGroup != null ? promptGroup.alpha : 0f;
        public bool GamepadGlyphs => usingGamepad;
        public int ObolsShown => Mathf.RoundToInt(obolsShown);
        public Banner FoundBanner { get; private set; }
        public bool HookMarkerVisible => hookMarker != null && hookMarker.gameObject.activeSelf;
        public float HookMarkerAlpha => hookMarkerImage != null ? hookMarkerImage.color.a : 0f;
        const int MaxChargeSlots = 8;
        public MenuPanel Menu { get; private set; }
        public BossBar Boss { get; private set; }
        public Banner Farewell { get; private set; }     // a Boss beaten: its joke ("ANCHOR DOWN...ed")...
        public Banner FarewellLine { get; private set; } // ...and the sincere line beneath ("She let go.")
        public string PromptText => promptLabel != null ? promptLabel.text : "";

        static readonly Color HealthRed = UiKit.Hex(0x8e2a22), StaminaMoss = UiKit.Hex(0x6f8a55);
        static readonly Color FlameOuter = UiKit.Hex(0xff9a45), FlameCore = UiKit.Hex(0xffe3a8), FlameSpent = UiKit.Hex(0x2a2629, 0.9f);

        RectTransform canvasRect, hpFill, hpTrail, staminaFill, lockBar, lockFill, lockTrail;
        CanvasGroup hudGroup, promptGroup;
        Image restFlash, keyCap, buttonRing, ceremonyDim;
        Text glyph, promptLabel, obolText, obolGain;
        RectTransform hookMarker, lockMarker;
        Image lockMarkerGlow;
        Soured markerTarget;
        float lockedAt = -100f;
        Image hookMarkerImage, hookMarkerGlow;
        Banner foundHint;
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

            // A key item held up: the world dims around the moment.
            ceremonyDim = UiKit.Image(canvasRect, "Ceremony Dim", UiKit.Vignette, Color.black.WithAlpha(0f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            UiKit.Stretch(ceremonyDim.rectTransform); // darkest at the edges: the Apprentice, in the middle, stays lit
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
            FoundBanner = new Banner(canvasRect, "Found", new Banner.Style
            {
                font = titleFont, size = 60, colour = UiKit.Bone, spacing = 5f, y = -150f, glow = UiKit.Hex(0x9fc4ff, 0.18f),
                bandAlpha = 0.6f, fadeIn = 0.5f, hold = 3.0f, fadeOut = 1.0f, drift = 0.02f,
            });
            foundHint = new Banner(canvasRect, "Found Hint", new Banner.Style
            {
                font = textFont, size = 30, colour = UiKit.Ash, y = -210f, fadeIn = 0.7f, hold = 2.8f, fadeOut = 1.0f,
            });
            BuildHookMarker(hudRoot);
            Boss = new BossBar(canvasRect, titleFont, textFont);
            Farewell = new Banner(canvasRect, "Boss Farewell", new Banner.Style
            {
                font = titleFont, size = 80, colour = UiKit.Bone, spacing = 8f, y = 60f, glow = UiKit.Hex(0x9fc4ff, 0.22f),
                bandAlpha = 0.6f, rule = true, fadeIn = 1.0f, hold = 3.8f, fadeOut = 2.0f, drift = 0.03f,
            });
            FarewellLine = new Banner(canvasRect, "Boss Farewell Line", new Banner.Style
            {
                font = textFont, size = 38, colour = UiKit.Ash, spacing = 3f, y = -20f, fadeIn = 1.6f, hold = 2.6f, fadeOut = 2.0f, drift = 0.02f,
            });
            Menu = new MenuPanel(canvasRect, titleFont, textFont);
            Death = new DeathScreen(canvasRect, titleFont, deathPhrase);

            usingGamepad = Gamepad.current != null;
            apprentice.Died += OnDied;
            apprentice.Woke += OnWoke;
            apprentice.ObolsChanged += OnObolsChanged;
            apprentice.Healed += OnHealed;
            apprentice.Found += OnFound;
            obolsShown = apprentice.Obols;
            MooringPost.Rested += OnRested;
            AreaEntrance.Entered += OnAreaEntered;
            BossFight.Defeated += OnBossDefeated;
        }

        // The joke first (up to its beat, then the rest), then the sincere line quietly beneath.
        void OnBossDefeated(IBoss boss)
        {
            string[] parts = (boss.Farewell ?? "").Split('|');
            Farewell.Show(parts[0]);
            farewellRest = parts.Length > 1 ? string.Concat(parts) : null;
            farewellLine = boss.FarewellLine;
            farewellAt = UiKit.Now;
        }

        string farewellRest, farewellLine;
        float farewellAt = -100f;

        void UpdateFarewell()
        {
            float age = UiKit.Now - farewellAt;
            if (farewellRest != null && age >= 1.3f)
            {
                Farewell.Text.text = farewellRest; // ...ed
                farewellRest = null;
            }
            if (!string.IsNullOrEmpty(farewellLine) && age >= 2.3f)
            {
                FarewellLine.Show(farewellLine);
                farewellLine = null;
            }
            Farewell.Tick();
            FarewellLine.Tick();
        }

        void OnDestroy()
        {
            if (apprentice != null)
            {
                apprentice.Died -= OnDied;
                apprentice.Woke -= OnWoke;
                apprentice.ObolsChanged -= OnObolsChanged;
                apprentice.Healed -= OnHealed;
                apprentice.Found -= OnFound;
            }
            MooringPost.Rested -= OnRested;
            AreaEntrance.Entered -= OnAreaEntered;
            BossFight.Defeated -= OnBossDefeated;
        }

        void OnHealed() => healedAt = UiKit.Now;

        // Something gained for good: its name, and a hint (with the key of the device in use).
        void OnFound(string title, string hint)
        {
            AreaTitle.Dismiss();
            LanternBanner.Dismiss();
            FoundBanner.Show(title);
            if (!string.IsNullOrEmpty(hint))
                foundHint.Show(hint.Replace("{hook}", usingGamepad ? "RB" : "F"));
        }

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
            UpdateHookMarker();

            if (BossFight.Current != null)
            {
                AreaTitle.Dismiss(); // the Boss's name takes the screen
                LanternBanner.Dismiss();
            }
            Boss.Tick(BossFight.Current);
            Menu.Tick(apprentice.OpenMenu, usingGamepad);
            Death.Tick();
            hudGroup.alpha = Death.HudAlpha;
            AreaTitle.Tick();
            LanternBanner.Tick();
            FoundBanner.Tick();
            UpdateFarewell();
            foundHint.Tick();
            float dimTarget = apprentice.CurrentState == Apprentice.State.Ceremony ? 0.85f : 0f;
            ceremonyDim.color = Color.black.WithAlpha(Mathf.MoveTowards(ceremonyDim.color.a, dimTarget, UiKit.DeltaTime * 1.2f));
            ceremonyDim.enabled = ceremonyDim.color.a > 0.001f;
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
            charges = new (RectTransform, Image, Image, Image)[MaxChargeSlots]; // shown up to the current maximum
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
                c.flame.parent.gameObject.SetActive(i < apprentice.maxHealingCharges);
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

        // With the Hook: a pale blue ring on the post it would pull to, dim; bright and pulsing while aiming.
        void BuildHookMarker(Transform root)
        {
            var ring = UiKit.Image(root, "Hook Marker", UiKit.Ring, UiKit.Hex(0x9fc4ff, 0.9f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46, 46));
            hookMarker = ring.rectTransform;
            hookMarkerImage = ring;
            hookMarkerGlow = UiKit.Image(hookMarker, "Glow", UiKit.SoftDot, UiKit.Hex(0x9fc4ff, 0.25f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90, 90));
            hookMarker.gameObject.SetActive(false);
        }

        void UpdateHookMarker()
        {
            var post = apprentice.CurrentState == Apprentice.State.Free ? apprentice.HookTarget : null;
            var cam = Camera.main;
            Vector3 screen = post != null && cam != null ? cam.WorldToScreenPoint(post.RingPosition) : Vector3.zero;
            bool show = post != null && screen.z > 0f;
            hookMarker.gameObject.SetActive(show);
            if (!show)
                return;
            var canvas = canvasRect.GetComponent<Canvas>();
            var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, uiCamera, out var local);
            // Off the screen (a post beyond the top, say): pinned to the edge in its direction, smaller.
            Vector2 half = canvasRect.rect.size * 0.5f - new Vector2(50f, 50f);
            bool offScreen = Mathf.Abs(local.x) > half.x || Mathf.Abs(local.y) > half.y;
            if (offScreen)
                local = new Vector2(Mathf.Clamp(local.x, -half.x, half.x), Mathf.Clamp(local.y, -half.y, half.y));
            hookMarker.anchoredPosition = local;
            bool aiming = apprentice.AimingHook;
            hookMarkerImage.color = UiKit.Hex(0x9fc4ff, aiming ? 0.95f : 0.35f);
            hookMarkerGlow.color = UiKit.Hex(0x9fc4ff, aiming ? 0.35f : 0.08f);
            hookMarker.localScale = Vector3.one * ((aiming ? 1.1f + 0.08f * Mathf.Sin(UiKit.Now * 6f) : 0.9f) * (offScreen ? 0.7f : 1f));
        }

        void BuildLockBar(Transform root)
        {
            var frame = UiKit.Image(root, "Lock Bar", null, UiKit.Bronze.WithAlpha(0.9f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(128, 12));
            lockBar = frame.rectTransform;
            UiKit.Stretch(UiKit.Image(lockBar, "Back", null, UiKit.Hex(0x0b0a0e, 0.9f), Vector2.zero, Vector2.zero, Vector2.zero).rectTransform, 1f);
            lockTrail = Fill(lockBar, UiKit.Bone.WithAlpha(0.8f));
            lockFill = Fill(lockBar, HealthRed);
            lockFill.GetComponent<Image>().sprite = UiKit.Sheen;
            lockBar.gameObject.SetActive(false);
            // The lock-on marker, on its own (a Boss has no floating bar, but you still see who you're locked on to):
            // a bone-white diamond over the target, in a soft glow, popping in as you lock and breathing after.
            lockMarker = UiKit.Rect(root, "Lock Marker", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40, 40));
            lockMarkerGlow = UiKit.Image(lockMarker, "Glow", UiKit.SoftDot, UiKit.Bone.WithAlpha(0.25f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44, 44));
            var diamond = UiKit.Image(lockMarker, "Diamond", null, UiKit.Bone, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(13, 13));
            diamond.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            var hollow = UiKit.Image(lockMarker, "Frame", UiKit.Ring, UiKit.Bone.WithAlpha(0.7f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30, 30));
            hollow.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            lockMarker.gameObject.SetActive(false);
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
            UpdateLockMarker();
            var target = apprentice.LockTarget;
            var cam = Camera.main;
            bool show = target != null && target.IsAlive && cam != null
                && !(target is IBoss boss && boss == BossFight.Current); // a Boss has its own bar along the bottom
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

        void UpdateLockMarker()
        {
            var target = apprentice.LockTarget;
            var cam = Camera.main;
            bool show = target != null && target.IsAlive && cam != null && apprentice.IsAlive;
            bool boss = show && target is IBoss b && b == BossFight.Current;
            // Over the target: above its floating bar, or (a Boss, whose bar is along the bottom) where the bar would be.
            Vector3 screen = show ? cam.WorldToScreenPoint(target.transform.position + Vector3.up * (target.barHeight + (boss ? -0.4f : 0.35f))) : Vector3.zero;
            show &= screen.z > 0f;
            lockMarker.gameObject.SetActive(show);
            if (!show)
            {
                markerTarget = null;
                return;
            }
            if (target != markerTarget)
            {
                markerTarget = target;
                lockedAt = UiKit.Now; // a new lock: it pops in
            }
            var canvas = canvasRect.GetComponent<Canvas>();
            var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, uiCamera, out var local);
            lockMarker.anchoredPosition = local;
            float age = UiKit.Now - lockedAt;
            float pop = 1f + 0.6f * (1f - UiKit.Smooth(0f, 0.25f, age));
            float breathe = 1f + 0.06f * Mathf.Sin(UiKit.Now * 4f);
            lockMarker.localScale = Vector3.one * pop * breathe;
            lockMarkerGlow.color = UiKit.Bone.WithAlpha(0.18f + 0.1f * Mathf.Sin(UiKit.Now * 4f) + 0.4f * (1f - UiKit.Smooth(0f, 0.3f, age)));
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
