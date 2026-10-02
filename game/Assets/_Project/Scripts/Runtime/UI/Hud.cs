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
        public DialogueBox Dialogue { get; private set; }
        public BossBar Boss { get; private set; }
        public Banner Farewell { get; private set; }     // a Boss beaten: its joke ("ANCHOR DOWN")...
        public Banner FarewellLine { get; private set; } // ...and the sincere line beneath ("She let go.")
        public string PromptText => promptLabel != null ? promptLabel.text : "";

        static readonly Color HealthRed = UiKit.Hex(0x8e2a22), StaminaMoss = UiKit.Hex(0x6f8a55);
        static readonly Color FlameOuter = UiKit.Hex(0xff9a45), FlameCore = UiKit.Hex(0xffe3a8), FlameSpent = UiKit.Hex(0x2a2629, 0.9f);

        RectTransform canvasRect, hpFill, hpTrail, staminaFill, lockBar, lockFill, lockTrail, hpFrame, staminaFrame, poisonFill, poisonFrame;
        Image poisonFillImage;
        Text poisonLabel;
        static readonly Color PoisonGreen = new Color(0.52f, 0.78f, 0.26f);
        public bool PoisonShown => poisonFrame.gameObject.activeSelf;
        const float HealthWidth = 380f, StaminaWidth = 300f, LongestBar = 2.2f; // (at the start; a bar grows with its most, to 2.2 times)

        bool barsSized;
        public float HealthBarWidth => hpFrame.sizeDelta.x - 4f;
        public float StaminaBarWidth => staminaFrame.sizeDelta.x - 4f;
        CanvasGroup hudGroup, promptGroup, canvasGroup;
        Image restFlash, keyCap, buttonRing, ceremonyDim, dip;
        public float DipAlpha => dip != null ? dip.color.a : 0f;
        Text glyph, promptLabel, obolText, obolGain, obolNote;
        bool obolsExplained;
        float obolNoteAt = -100f;
        public float ObolNoteAlpha => obolNote != null ? obolNote.color.a : 0f;
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
        static bool usingGamepad => InputDevice.Gamepad; // (the controls last touched)

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
            canvasGroup = canvasGo.AddComponent<CanvasGroup>();
            canvasGroup.interactable = canvasGroup.blocksRaycasts = false;

            var hudRoot = UiKit.Stretch(UiKit.Rect(canvasRect, "HUD", Vector2.zero, Vector2.zero, Vector2.zero));
            hudGroup = hudRoot.gameObject.AddComponent<CanvasGroup>();
            BuildLockBar(hudRoot);
            hpFill = Bar(hudRoot, "Health", new Vector2(56, -46), 380, 14, HealthRed, out hpTrail);
            hpFillImage = hpFill.GetComponent<Image>();
            staminaFill = Bar(hudRoot, "Stamina", new Vector2(56, -70), 300, 8, StaminaMoss, out _);
            BuildPoison(hudRoot);
            hpFrame = (RectTransform)hpFill.parent;
            staminaFrame = (RectTransform)staminaFill.parent;
            BuildCharges(hudRoot);
            BuildPrompt(hudRoot);
            BuildObols(hudRoot);
            BuildBelt(hudRoot);

            // Washing ashore: a quick dip to black and back.
            dip = UiKit.Image(canvasRect, "Dip", null, Color.black.WithAlpha(0f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            UiKit.Stretch(dip.rectTransform, -4f);
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
                font = textFont, size = 38, colour = UiKit.Bone, spacing = 3f, y = -52f, fadeIn = 1.6f, hold = 2.6f, fadeOut = 2.0f, drift = 0.02f,
                glow = Color.black.WithAlpha(0.8f), bandAlpha = 0.5f, // (a dark band and glow of its own: it reads over anything)
            });
            Menu = new MenuPanel(canvasRect, titleFont, textFont);
            Dialogue = new DialogueBox(canvasRect, titleFont, textFont);
            Death = new DeathScreen(canvasRect, titleFont, deathPhrase);

            apprentice.Died += OnDied;
            apprentice.Woke += OnWoke;
            apprentice.ObolsChanged += OnObolsChanged;
            apprentice.Healed += OnHealed;
            apprentice.Found += OnFound;
            if (apprentice.Equipment != null)
                apprentice.Equipment.Written += OnWritten;
            obolsShown = apprentice.Obols;
            MooringPost.Rested += OnRested;
            AreaEntrance.Entered += OnAreaEntered;
            BossFight.Defeated += OnBossDefeated;
        }

        // The joke first (up to its beat, then the rest), then the sincere line quietly beneath.
        void OnBossDefeated(IBoss boss)
        {
            Sfx.Instance?.BossFarewell(); // a great bell, as its farewell comes up
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
                if (apprentice.Equipment != null)
                    apprentice.Equipment.Written -= OnWritten;
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

        // A kind of Soured met (scattered) for the first time: a quiet line (a Boss's is its farewell).
        void OnWritten(LedgerEntry entry)
        {
            if (entry.Kind != LedgerEntry.Rank.Soured)
                return;
            string name = entry.Name.StartsWith("The ") ? "the " + entry.Name.Substring(4) : entry.Name;
            LastWritten = $"Written in the Ledger: {name}";
            foundHint.Show(LastWritten);
        }

        public string LastWritten { get; private set; } // (tests)

        void OnObolsChanged(int delta)
        {
            if (delta <= 0)
                return;
            gainAmount = UiKit.Now - gainAt < 1.5f ? gainAmount + delta : delta; // gains close together add up
            gainAt = UiKit.Now;
        }

        // Now and then the game's own voice (docs/brief.md, Tone and voice): never the first time, and rarely.
        static readonly string[] DeathVariants = { "You Sank (Again)", "Glub.", "Buoyancy Check Failed" };
        const float VariantChance = 0.15f;
        int deaths, lanternsLit;

        void OnDied()
        {
            AreaTitle.Dismiss();
            LanternBanner.Dismiss();
            deaths++;
            Death.SetPhrase(deaths > 1 && Random.value < VariantChance ? DeathVariants[Random.Range(0, DeathVariants.Length)] : deathPhrase);
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
                LanternBanner.Show(++lanternsLit > 1 && Random.value < VariantChance * 0.6f ? "Lantern Lit (It's a Bonfire)" : "Lantern Lit");
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
            // Poison: its build-up under the bars (only while there's any); poisoned, it pulses, and health goes sickly.
            bool poisoned = apprentice.Poisoned;
            poisonFrame.gameObject.SetActive(apprentice.Poison > 0.01f);
            poisonLabel.text = poisoned ? "Poisoned" : "Poison";
            poisonLabel.color = (poisoned ? new Color(0.8f, 1f, 0.5f) : PoisonGreen).WithAlpha(0.9f);
            poisonFill.anchorMax = new Vector2(apprentice.PoisonFraction, 1f);
            float pulse = poisoned ? 0.5f + 0.5f * Mathf.Sin(now * 6f) : 0f;
            poisonFillImage.color = Color.Lerp(PoisonGreen, new Color(0.8f, 1f, 0.5f), pulse * 0.6f);
            if (poisoned)
                hpFillImage.color = Color.Lerp(hpFillImage.color, new Color(0.45f, 0.62f, 0.2f), 0.55f);
            hpTrail.anchorMax = new Vector2(Trail(health, ref hpLast, ref hpTrailValue, ref hpTrailHold), 1f);
            staminaFill.anchorMax = new Vector2(apprentice.Stamina / apprentice.maxStamina, 1f);
            Grow(hpFrame, HealthWidth * apprentice.maxHealth / apprentice.BaseHealth);
            Grow(staminaFrame, StaminaWidth * apprentice.maxStamina / apprentice.BaseStamina);
            barsSized = true; // (from the first frame on, they grow; before it, a loaded journey's are simply that long)
            UpdateBelt(now);
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
            var talk = apprentice.OpenMenu as Conversation; // (a soul speaking has its own look)
            Menu.Tick(talk == null ? apprentice.OpenMenu : null, usingGamepad);
            Dialogue.Tick(talk, usingGamepad);
            Death.Tick();
            hudGroup.alpha = Death.HudAlpha * (Pause.IsPaused ? 0.2f : 1f); // (dimmed under the pause menu)
            // Paused (the pause menu, the character screen), its messages go quiet too: nothing shows through a menu.
            canvasGroup.alpha = Pause.IsPaused ? 0f : Mathf.MoveTowards(canvasGroup.alpha, 1f, UiKit.DeltaTime * 6f); // (gone at once; back softly)
            AreaTitle.Tick();
            LanternBanner.Tick();
            FoundBanner.Tick();
            UpdateFarewell();
            foundHint.Tick();
            dip.color = Color.black.WithAlpha(apprentice.Dip);
            dip.enabled = dip.color.a > 0.001f;
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
            obolNote = UiKit.Text(root, "Obol Note", textFont, 26, UiKit.Ash, TextAnchor.MiddleRight, corner, new Vector2(-56, 20));
            obolNote.rectTransform.pivot = new Vector2(1f, 0.5f);
            obolNote.rectTransform.sizeDelta = new Vector2(420, 34);
            obolNote.text = "Obols (souls, basically)";
            obolNote.color = UiKit.Ash.WithAlpha(0f);
        }

        void UpdateObols()
        {
            float target = apprentice.Obols;
            float rate = Mathf.Max(Mathf.Abs(target - obolsShown) * 2.5f, 40f);
            obolsShown = Mathf.MoveTowards(obolsShown, target, rate * UiKit.DeltaTime);
            obolText.text = Mathf.RoundToInt(obolsShown).ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
            float age = UiKit.Now - gainAt;
            obolGain.text = "+" + gainAmount;
            // The first Obols ever: what they are, in the game's own voice.
            if (!obolsExplained && gainAt > 0f && apprentice.Obols > 0)
            {
                obolsExplained = true;
                obolNoteAt = UiKit.Now + 0.4f;
            }
            float noteAge = UiKit.Now - obolNoteAt;
            obolNote.color = UiKit.Ash.WithAlpha(UiKit.Smooth(0f, 0.5f, noteAge) * (1f - UiKit.Smooth(3.5f, 4.5f, noteAge)));
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
            float target = apprentice.Nearby != null && apprentice.IsAlive && apprentice.OpenMenu == null ? 1f : 0f; // (not over a menu or a talk)
            promptGroup.alpha = Mathf.MoveTowards(promptGroup.alpha, target, UiKit.DeltaTime * 5f);
        }

        // Show the keys of whichever device was touched last.
        static void TrackDevice() => InputDevice.Track();

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
            Vector3? mark = apprentice.CurrentState == Apprentice.State.Free ? apprentice.HookMark : null;
            var cam = Camera.main;
            Vector3 screen = mark.HasValue && cam != null ? cam.WorldToScreenPoint(mark.Value) : Vector3.zero;
            bool show = mark.HasValue && screen.z > 0f;
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
                && !(target is IBoss boss && boss == BossFight.Current) // a Boss has its own bar along the bottom
                && !(BossFight.Current is IBossParts parted && parted.Owns(target)); // (and so do its parts)
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
            bool boss = show && (target is IBoss b && b == BossFight.Current || BossFight.Current is IBossParts parted && parted.Owns(target));
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
        // A bar is as long as its most: a Warm Stone or a Held Breath found lengthens it (quickly, not at once).
        void Grow(RectTransform frame, float width)
        {
            float longest = (frame.name == "Health" ? HealthWidth : StaminaWidth) * LongestBar;
            float target = Mathf.Min(width, longest) + 4f;
            var size = frame.sizeDelta;
            size.x = !barsSized || Mathf.Abs(size.x - target) < 0.5f ? target : Mathf.Lerp(size.x, target, 1f - Mathf.Exp(-8f * UiKit.DeltaTime));
            frame.sizeDelta = size;
        }

        // ---- the belt: up to four Provisions in a row, bottom left; the chosen one bigger and bright, a caret over it
        // (up uses it), its name above; left/right choose along it

        (RectTransform slot, Image frame, Image icon, Text count)[] belt;
        CanvasGroup beltGroup;
        Text beltName;
        Image beltCaret;
        float beltUsedAt = -10f;
        public string BeltText(int slot) => apprentice.Equipment.Belt[slot] is Provision p ? $"{p.Name} {belt[slot].count.text}" : "";
        public string BeltChosenName => beltName.text;

        void BuildBelt(Transform root)
        {
            var strip = UiKit.Rect(root, "Belt", new Vector2(0f, 0f), new Vector2(60, 70), new Vector2(10, 10));
            strip.pivot = new Vector2(0f, 0.5f);
            beltGroup = strip.gameObject.AddComponent<CanvasGroup>();
            beltGroup.alpha = 0f;
            belt = new (RectTransform, Image, Image, Text)[Provision.BeltSlots];
            for (int i = 0; i < belt.Length; i++)
            {
                var slot = UiKit.Rect(strip, "Slot", new Vector2(0f, 0.5f), new Vector2(34 + i * 78, 0), new Vector2(64, 64));
                UiKit.Image(slot, "Back", null, UiKit.Hex(0x0b0a0e, 0.7f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44, 44))
                    .rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
                var frame = UiKit.Image(slot, "Frame", UiKit.KeyCap, UiKit.Bronze, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64, 64));
                frame.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
                var icon = UiKit.Image(slot, "Icon", null, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(36, 36));
                var count = UiKit.Text(slot, "Count", textFont, 22, UiKit.Bone, TextAnchor.LowerRight, new Vector2(0.5f, 0.5f), new Vector2(26, -20));
                count.rectTransform.sizeDelta = new Vector2(40, 30);
                count.rectTransform.pivot = new Vector2(1f, 0f);
                belt[i] = (slot, frame, icon, count);
            }
            beltCaret = UiKit.Image(strip, "Caret", null, Color.clear, new Vector2(0f, 0.5f), new Vector2(34, 60), new Vector2(30, 14));
            foreach (float side in new[] { -1f, 1f })
            {
                var stroke = UiKit.Image(beltCaret.rectTransform, "Stroke", null, UiKit.Ember, new Vector2(0.5f, 0.5f), new Vector2(side * 6f, 0f), new Vector2(16, 3));
                stroke.rectTransform.localRotation = Quaternion.Euler(0, 0, -30f * side);
            }
            beltName = UiKit.Text(strip, "Chosen", textFont, 26, UiKit.Bone, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(0, 94), 1f);
            beltName.rectTransform.pivot = new Vector2(0f, 0.5f);
            beltName.rectTransform.sizeDelta = new Vector2(400, 36);
        }

        public static Sprite IconOf(Provision p) =>
            p == Provision.PitchPot ? UiKit.PotIcon : p == Provision.EmberTar ? UiKit.TinIcon : p == Provision.WayBack ? UiKit.CordIcon
            : p == Provision.BitterRoot ? UiKit.RootIcon : p == Provision.SoddenSack ? UiKit.SackIcon : UiKit.FlaskIcon;

        public static Color ColourOf(Provision p) =>
            p == Provision.PitchPot ? UiKit.Hex(0xc07a4e) : p == Provision.EmberTar ? UiKit.Hex(0xff8a3a) : p == Provision.WayBack ? UiKit.Hex(0xe0d4b6)
            : p == Provision.BitterRoot ? UiKit.Hex(0xb9a36a) : p == Provision.SoddenSack ? UiKit.Hex(0x9fb4c2) : UiKit.Hex(0xffc47a);

        void UpdateBelt(float now)
        {
            var e = apprentice.Equipment;
            if (apprentice.CurrentState == Apprentice.State.Provision && apprentice.StateTime < 0.05f)
                beltUsedAt = now;
            float flash = 1f - UiKit.Smooth(0f, 0.4f, now - beltUsedAt);
            for (int i = 0; i < belt.Length; i++)
            {
                var (slot, frame, icon, count) = belt[i];
                var p = e.Belt[i];
                icon.enabled = p != null;
                if (p == null)
                {
                    count.text = "";
                    frame.color = UiKit.Bronze.WithAlpha(0.35f); // (an empty slot: left/right pass it by)
                    slot.localScale = Vector3.one * 0.8f;
                    continue;
                }
                bool chosen = i == e.BeltChosen, empty = e.Carried(p) <= 0;
                icon.sprite = IconOf(p);
                icon.color = ColourOf(p).WithAlpha(empty ? 0.25f : chosen ? 1f : 0.6f);
                count.text = $"{e.Carried(p)}";
                count.color = empty ? UiKit.Ash.WithAlpha(0.5f) : chosen ? UiKit.Bone : UiKit.Ash;
                frame.color = chosen ? Color.Lerp(UiKit.Ember.WithAlpha(0.85f), Color.white, flash * 0.6f) : UiKit.Bronze.WithAlpha(0.7f);
                float scale = chosen ? 1.15f + 0.12f * flash : 0.85f;
                slot.localScale = Vector3.one * Mathf.Lerp(slot.localScale.x, scale, 1f - Mathf.Exp(-14f * UiKit.DeltaTime));
            }
            var c = e.Chosen;
            beltCaret.enabled = c != null;
            foreach (Transform stroke in beltCaret.transform)
                stroke.gameObject.SetActive(c != null);
            if (c != null)
                beltCaret.rectTransform.anchoredPosition = new Vector2(34 + e.BeltChosen * 78, 60 + 4f * Mathf.Sin(now * 3f));
            beltName.text = c != null ? c.Name : "";
            beltName.color = c != null && e.Carried(c) > 0 ? UiKit.Bone.WithAlpha(0.85f) : UiKit.Ash.WithAlpha(0.6f);
            beltGroup.alpha = Mathf.MoveTowards(beltGroup.alpha, e.BeltEmpty ? 0f : 1f, UiKit.DeltaTime * 4f);
        }

        // The poison build-up: in the middle of the screen, just under the Apprentice (who's always there), where it
        // can't be missed; only while there's any. Its word beside it.
        void BuildPoison(Transform root)
        {
            var centre = new Vector2(0.5f, 0.5f);
            poisonFrame = UiKit.Image(root, "Poison", null, UiKit.Bronze.WithAlpha(0.9f), centre, new Vector2(0, -110), new Vector2(164, 10)).rectTransform;
            UiKit.Stretch(UiKit.Image(poisonFrame, "Back", null, UiKit.Hex(0x0b0a0e, 0.85f), Vector2.zero, Vector2.zero, Vector2.zero).rectTransform, 1f);
            poisonFill = Fill(poisonFrame, PoisonGreen);
            poisonFillImage = poisonFill.GetComponent<Image>();
            poisonFillImage.sprite = UiKit.Sheen;
            // Its word to its left, level with it (not over it).
            poisonLabel = UiKit.Text(poisonFrame, "Label", textFont, 22, PoisonGreen, TextAnchor.MiddleRight, new Vector2(0f, 0.5f), new Vector2(-10, 1), 1f);
            poisonLabel.rectTransform.pivot = new Vector2(1f, 0.5f);
            poisonLabel.rectTransform.sizeDelta = new Vector2(200, 28);
            poisonFrame.gameObject.SetActive(false);
        }

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
