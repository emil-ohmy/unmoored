using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MyGame
{
    // The character screen (I or C; Select on a gamepad): the world pauses behind six pages, LB / RB (Q / R) between
    // them (left/right are for moving within a page).
    // The Apprentice (health, stamina, healing, what's carried), The Oar (its step: its name, what it is, how strong,
    // what the next step asks), Abilities (each found: what it is, how it's used, what it's for), The Satchel (the
    // Provisions carried, and the belt: pick a slot, then what goes in it), Charms (those found,
    // those fitted, the slots, and dark sockets for the rest) and The Lantern, which says as little as it can. Long lists
    // scroll. Closed with the same key, Esc or B. Drawn in real time.
    public sealed class CharacterScreen : MonoBehaviour
    {
        public enum Tab { Apprentice, Oar, Abilities, Satchel, Charms, Ledger, Lantern }
        public static readonly string[] TabNames = { "The Apprentice", "The Oar", "Abilities", "The Satchel", "Charms", "The Ledger", "The Lantern" };
        static int Tabs => TabNames.Length;
        static float TabSpacing => Mathf.Min(300f, 1640f / (Tabs - 1)); // (as many as fit across)

        public Font titleFont, textFont;
        public GameObject[] oarModels;       // the oar at each step, on its own (oar_display): shown turning on the Oar page
        public GameObject[] provisionModels; // each Provision (Provision.All's order): shown turning on the Satchel page
        public GameObject[] ledgerModels;    // each Ledger entry's own (LedgerEntry.All's order): shown turning on the Ledger page

        public static CharacterScreen Instance { get; private set; }
        // Whether Esc belongs to it this frame (open, or just closed by it): the pause menu leaves it alone.
        public static bool Blocks => Instance != null && (Instance.Open || Instance.closedFrame == Time.frameCount);

        public bool Open { get; private set; }
        public Tab Current { get; private set; }
        public int SelectedCharm => charmList.Selected;
        public int SelectedAbility => abilityList.Selected;
        public int SelectedProvision => satchelList.Selected;
        public int SelectedEntry => ledgerList.Selected;
        public IReadOnlyList<string> LedgerRows => ledgerRowText;
        public string LedgerDetail => ledgerName.text + "\n" + ledgerKind.text + "\n" + ledgerDescription.text + "\n" + ledgerKnown.text + "\n" + ledgerCount.text;
        public IReadOnlyList<string> SatchelRows => satchelRowText;
        public string SatchelDetail => satchelEffect.text;

        // What each page says (tests, and the capture).
        public string OarName => oarName.text;
        public string OarNext => oarNext.text;
        public string LanternLine => lanternLine.text;
        public string CharmDetail => charmDetail.text;
        public IReadOnlyList<string> CharmRows => charmRowText;
        public IReadOnlyList<string> AbilityRows => abilityRowText;
        public string AbilityName => abilityName.text;
        public string AbilityHow => abilityKeys.text + "\n" + string.Join("\n", System.Array.ConvertAll(abilityUses, u => u.text));
        public string Hints => hints.text;
        public int SatchelSlot => satchelSlot;          // the belt slot picked (the Satchel page)
        public bool SatchelPicking => satchelPicking;   // choosing what goes in it
        public string StatValue(int row) => stats[row].value.text;

        static readonly Color Cold = new Color(0.78f, 0.88f, 1f);

        MenuInput input;
        Apprentice apprentice;
        PauseMenu pause;
        CanvasGroup group;
        Image shade;
        int closedFrame = -1;

        readonly Text[] tabs = new Text[TabNames.Length];
        Image tabLeft, tabRight;
        readonly RectTransform[] pages = new RectTransform[TabNames.Length];

        (Text label, Text value, Text note)[] stats;
        Image oarGlow;
        RawImage oarView, satchelView;
        ModelStage stage;
        Image satchelGlow;
        Text oarName, oarDescription, oarNext;
        Image[] pips;
        Image[] slotMarks;
        ScrollRows charmList, abilityList, satchelList, ledgerList;
        readonly List<string> ledgerRowText = new List<string>();
        readonly List<LedgerEntry> ledgerShown = new List<LedgerEntry>();
        Text ledgerName, ledgerKind, ledgerDescription, ledgerKnownHeading, ledgerKnown, ledgerCount, ledgerNone;
        RawImage ledgerView;
        Image ledgerGlow;
        Graphic[] ledgerDetail;
        readonly List<string> charmRowText = new List<string>(), abilityRowText = new List<string>(), satchelRowText = new List<string>();
        readonly List<Provision> satchel = new List<Provision>();
        Text satchelName, satchelDescription, satchelEffect, satchelBeltLine, satchelNone, hints;
        int satchelSlot;
        bool satchelPicking;
        (Image frame, Image icon, Text count, Text empty)[] beltSlots;
        Graphic[] satchelDetail;
        (Image frame, Image icon)[] satchelBelt;
        public GameObject ShownModel => stage?.Shown; // (tests)
        Text abilityName, abilityDescription, abilityHowHeading, abilityKeys, abilityPad, abilityNone;
        Text[] abilityUses;
        Text charmDetail;
        Image lanternGlow, lanternCore;
        Image[] flames;
        Image lastRing;
        Text lanternLine;

        void Awake() => Instance = this;

        void Start()
        {
            apprentice = FindAnyObjectByType<Apprentice>();
            pause = FindAnyObjectByType<PauseMenu>();
            input = new MenuInput();
            var canvasGo = new GameObject("Character Canvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 49; // over the HUD, under the pause menu
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var centre = new Vector2(0.5f, 0.5f);
            shade = UiKit.Image(canvasGo.transform, "Shade", null, Color.black.WithAlpha(0.86f), centre, Vector2.zero, Vector2.zero);
            UiKit.Stretch(shade.rectTransform, -4f);
            var root = UiKit.Stretch(UiKit.Rect(canvasGo.transform, "Character Screen", centre, Vector2.zero, Vector2.zero));
            group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = group.blocksRaycasts = false;
            shade.transform.SetParent(root, true);
            UiKit.Image(root, "Backdrop", UiKit.Band, Color.black.WithAlpha(0.55f), centre, new Vector2(0, 20), new Vector2(1900, 980));

            // The tabs, across the top.
            for (int i = 0; i < Tabs; i++)
                tabs[i] = UiKit.Text(root, "Tab", titleFont, 30, UiKit.Ash, TextAnchor.MiddleCenter, centre, new Vector2((i - (Tabs - 1) / 2f) * TabSpacing, 400), 2f);
            tabLeft = Diamond(root, UiKit.Ember, 10f);
            tabRight = Diamond(root, UiKit.Ember, 10f);
            UiKit.Image(root, "Rule", UiKit.Rule, UiKit.Bone.WithAlpha(0.55f), centre, new Vector2(0, 360), new Vector2(1600, 3));
            var hintBand = UiKit.Image(root, "Hint Band", UiKit.Band, Color.black.WithAlpha(0.55f), new Vector2(0.5f, 0f), new Vector2(0, 70), new Vector2(1500, 70));
            hints = UiKit.Text(root, "Hints", textFont, 26, UiKit.Bone.WithAlpha(0.75f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0, 70), 1f);

            for (int i = 0; i < Tabs; i++)
                pages[i] = UiKit.Stretch(UiKit.Rect(root, TabNames[i], centre, Vector2.zero, Vector2.zero));
            BuildApprentice(pages[(int)Tab.Apprentice]);
            BuildOar(pages[(int)Tab.Oar]);
            BuildAbilities(pages[(int)Tab.Abilities]);
            BuildSatchel(pages[(int)Tab.Satchel]);
            BuildCharms(pages[(int)Tab.Charms]);
            BuildLedger(pages[(int)Tab.Ledger]);
            BuildLantern(pages[(int)Tab.Lantern]);
            root.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            stage?.Release();
            input?.Dispose();
            if (Open)
                Pause.Set(false);
            if (Instance == this)
                Instance = null;
        }

        // ---- the pages

        void BuildApprentice(RectTransform page)
        {
            var centre = new Vector2(0.5f, 0.5f);
            string[] labels = { "Health", "Stamina", "Healing", "Ferry Nails", "Obols" };
            stats = new (Text, Text, Text)[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                float y = 250 - i * 78;
                var label = UiKit.Text(page, "Label", textFont, 38, UiKit.Bone, TextAnchor.MiddleLeft, centre, new Vector2(-560, y), 1f);
                label.rectTransform.pivot = new Vector2(0f, 0.5f);
                label.rectTransform.sizeDelta = new Vector2(420, 60);
                label.text = labels[i];
                var value = UiKit.Text(page, "Value", textFont, 38, UiKit.Bone, TextAnchor.MiddleRight, centre, new Vector2(20, y), 1f);
                value.rectTransform.pivot = new Vector2(1f, 0.5f);
                value.rectTransform.sizeDelta = new Vector2(300, 60);
                var note = UiKit.Text(page, "Note", textFont, 28, UiKit.Ash, TextAnchor.MiddleLeft, centre, new Vector2(80, y), 1f);
                note.rectTransform.pivot = new Vector2(0f, 0.5f);
                note.rectTransform.sizeDelta = new Vector2(620, 60);
                stats[i] = (label, value, note);
                UiKit.Image(page, "Rule", UiKit.Rule, UiKit.Bone.WithAlpha(0.14f), centre, new Vector2(40, y - 39), new Vector2(1250, 2));
            }
            var aside = UiKit.Text(page, "Aside", textFont, 28, UiKit.Ash.WithAlpha(0.7f), TextAnchor.MiddleCenter, centre, new Vector2(0, -210), 1f);
            aside.text = "What the river hasn't taken yet.";
        }

        void BuildOar(RectTransform page)
        {
            var centre = new Vector2(0.5f, 0.5f);
            // The oar itself, turning slowly on the left (its own camera and lights, far below the world), a glow
            // behind it; what it is on the right.
            oarGlow = UiKit.Image(page, "Glow", UiKit.SoftDot, Cold.WithAlpha(0f), centre, new Vector2(-440, 20), new Vector2(620, 620));
            oarView = UiKit.Rect(page, "Oar", centre, new Vector2(-440, 20), new Vector2(640, 640)).gameObject.AddComponent<RawImage>();
            oarView.raycastTarget = false;
            stage = new ModelStage(transform, Cold);
            if (oarModels != null)
                foreach (var m in oarModels)
                    stage.Add(m, 2.3f);
            if (provisionModels != null)
                foreach (var m in provisionModels)
                    stage.Add(m, 1.8f);
            if (ledgerModels != null)
                for (int i = 0; i < ledgerModels.Length && i < LedgerEntry.All.Length; i++)
                {
                    if (LedgerEntry.All[i].Standing)
                        stage.Add(ledgerModels[i], 2.7f, byHeight: true); // (all the same height, whatever they carry)
                    else
                        stage.Add(ledgerModels[i], 2.9f);
                }
            oarView.texture = stage.Texture;
            const float x = 260f;
            oarName = UiKit.Text(page, "Name", titleFont, 54, UiKit.Bone, TextAnchor.MiddleCenter, centre, new Vector2(x, 200), 3f);
            oarDescription = UiKit.Text(page, "Description", textFont, 32, UiKit.Ash, TextAnchor.UpperCenter, centre, new Vector2(x, 140));
            oarDescription.horizontalOverflow = HorizontalWrapMode.Wrap;
            oarDescription.rectTransform.pivot = new Vector2(0.5f, 1f);
            oarDescription.rectTransform.sizeDelta = new Vector2(860, 170);
            var strength = UiKit.Text(page, "Strength", textFont, 30, UiKit.Bone, TextAnchor.MiddleRight, centre, new Vector2(x - 120, -70), 1f);
            strength.rectTransform.pivot = new Vector2(1f, 0.5f);
            strength.rectTransform.sizeDelta = new Vector2(300, 50);
            strength.text = "Strength";
            pips = new Image[Oar.Steps.Length];
            for (int i = 0; i < pips.Length; i++)
            {
                pips[i] = Diamond(page, UiKit.Ember, 18f);
                pips[i].rectTransform.anchoredPosition = new Vector2(x - 70 + i * 50, -70);
            }
            oarNext = UiKit.Text(page, "Next", textFont, 30, UiKit.Ash, TextAnchor.MiddleCenter, centre, new Vector2(x, -140), 1f);
            var how = UiKit.Text(page, "How", textFont, 26, UiKit.Ash.WithAlpha(0.65f), TextAnchor.MiddleCenter, centre, new Vector2(x, -200), 1f);
            how.text = "Strengthened at a Mooring post, a step at a time. No step is skipped.";
        }

        void BuildCharms(RectTransform page)
        {
            var centre = new Vector2(0.5f, 0.5f);
            var slots = UiKit.Text(page, "Slots", textFont, 32, UiKit.Bone, TextAnchor.MiddleRight, centre, new Vector2(-40, 290), 1f);
            slots.rectTransform.pivot = new Vector2(1f, 0.5f);
            slots.rectTransform.sizeDelta = new Vector2(300, 50);
            slots.text = "Slots";
            slotMarks = new Image[6];
            for (int i = 0; i < slotMarks.Length; i++)
            {
                slotMarks[i] = Diamond(page, UiKit.Ember, 20f);
                slotMarks[i].rectTransform.anchoredPosition = new Vector2(10 + i * 50, 290);
            }
            charmList = new ScrollRows(page, textFont, 0f, 210f, 1000f, 7);
            charmDetail = UiKit.Text(page, "Detail", textFont, 30, UiKit.Ash, TextAnchor.UpperCenter, centre, new Vector2(0, -262));
            charmDetail.horizontalOverflow = HorizontalWrapMode.Wrap;
            charmDetail.rectTransform.pivot = new Vector2(0.5f, 1f);
            charmDetail.rectTransform.sizeDelta = new Vector2(1100, 90);
        }

        void BuildAbilities(RectTransform page)
        {
            var centre = new Vector2(0.5f, 0.5f);
            abilityList = new ScrollRows(page, textFont, -470f, 280f, 520f, 9);
            abilityNone = UiKit.Text(page, "None", textFont, 32, UiKit.Ash.WithAlpha(0.7f), TextAnchor.MiddleCenter, centre, new Vector2(0, 40), 1f);
            abilityNone.text = "Nothing yet. The oar will have to do.";
            const float x = 290f;
            Text Line(string name, Font font, int size, Color colour, float y, float height, TextAnchor anchor = TextAnchor.UpperLeft)
            {
                var t = UiKit.Text(page, name, font, size, colour, anchor, centre, new Vector2(x, y));
                t.horizontalOverflow = HorizontalWrapMode.Wrap;
                t.rectTransform.pivot = new Vector2(0.5f, 1f);
                t.rectTransform.sizeDelta = new Vector2(880, height);
                return t;
            }
            abilityName = Line("Name", titleFont, 48, UiKit.Bone, 300, 70);
            abilityDescription = Line("Description", textFont, 30, UiKit.Ash, 225, 110);
            abilityHowHeading = Line("How", titleFont, 30, UiKit.Ember, 105, 44);
            abilityHowHeading.text = "How";
            abilityKeys = Line("Keys", textFont, 30, UiKit.Bone, 62, 40);
            abilityPad = Line("Pad", textFont, 30, UiKit.Bone, 24, 40);
            var forHeading = Line("For", titleFont, 30, UiKit.Ember, -38, 44);
            forHeading.text = "For";
            abilityUses = new Text[6];
            for (int i = 0; i < abilityUses.Length; i++)
                abilityUses[i] = Line("Use", textFont, 28, UiKit.Ash, -80 - i * 40, 40);
            abilityDetail = new Graphic[] { abilityName, abilityDescription, abilityHowHeading, abilityKeys, abilityPad, forHeading };
        }

        Graphic[] abilityDetail;

        // The belt across the top: four slots, left/right to pick one, confirm to choose what goes in it from the list
        // below (the chosen one turning beside it, what it is on the right).
        void BuildSatchel(RectTransform page)
        {
            var centre = new Vector2(0.5f, 0.5f);
            var beltLabel = UiKit.Text(page, "Belt Label", titleFont, 28, UiKit.Ash, TextAnchor.MiddleCenter, centre, new Vector2(0, 318), 2f);
            beltLabel.text = "The Belt";
            beltSlots = new (Image, Image, Text, Text)[Provision.BeltSlots];
            for (int i = 0; i < beltSlots.Length; i++)
            {
                var at = new Vector2((i - 1.5f) * 150f, 230f);
                UiKit.Image(page, "Back", null, UiKit.Hex(0x0b0a0e, 0.75f), centre, at, new Vector2(64, 64)).rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
                var frame = UiKit.Image(page, "Slot", UiKit.KeyCap, UiKit.Bronze, centre, at, new Vector2(96, 96));
                frame.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
                var icon = UiKit.Image(page, "Icon", null, Color.white, centre, at + new Vector2(0, 2), new Vector2(50, 50));
                var count = UiKit.Text(page, "Count", textFont, 26, UiKit.Bone, TextAnchor.LowerRight, centre, at + new Vector2(38, -30));
                count.rectTransform.pivot = new Vector2(1f, 0f);
                count.rectTransform.sizeDelta = new Vector2(50, 34);
                var empty = UiKit.Text(page, "Empty", textFont, 22, UiKit.Ash.WithAlpha(0.5f), TextAnchor.MiddleCenter, centre, at);
                empty.rectTransform.sizeDelta = new Vector2(100, 30);
                empty.text = "empty";
                beltSlots[i] = (frame, icon, count, empty);
            }
            UiKit.Image(page, "Rule", UiKit.Rule, UiKit.Bone.WithAlpha(0.25f), centre, new Vector2(0, 150), new Vector2(1300, 2));
            satchelList = new ScrollRows(page, textFont, -560f, 100f, 520f, 6);
            satchelNone = UiKit.Text(page, "None", textFont, 30, UiKit.Ash.WithAlpha(0.7f), TextAnchor.MiddleCenter, centre, new Vector2(0, -40), 1f);
            satchelNone.text = "The Satchel's empty. The merchant soul would love to help.";
            satchelGlow = UiKit.Image(page, "Glow", UiKit.SoftDot, UiKit.Ember.WithAlpha(0.12f), centre, new Vector2(10, -70), new Vector2(380, 380));
            satchelView = UiKit.Rect(page, "Model", centre, new Vector2(10, -70), new Vector2(380, 380)).gameObject.AddComponent<RawImage>();
            satchelView.raycastTarget = false;
            satchelView.texture = stage.Texture;
            const float x = 520f;
            Text Line(string name, Font font, int size, Color colour, float y, float height)
            {
                var t = UiKit.Text(page, name, font, size, colour, TextAnchor.UpperLeft, centre, new Vector2(x, y));
                t.horizontalOverflow = HorizontalWrapMode.Wrap;
                t.rectTransform.pivot = new Vector2(0.5f, 1f);
                t.rectTransform.sizeDelta = new Vector2(560, height);
                return t;
            }
            satchelName = Line("Name", titleFont, 42, UiKit.Bone, 118, 60);
            satchelDescription = Line("Description", textFont, 27, UiKit.Ash, 58, 90);
            satchelEffect = Line("Effect", textFont, 27, UiKit.Bone, -40, 140);
            satchelBeltLine = Line("Belt", textFont, 26, UiKit.Ember, -190, 60);
            satchelDetail = new Graphic[] { satchelName, satchelDescription, satchelEffect, satchelView, satchelGlow };
        }

        // Every kind of Soured met (and Boss beaten), a row each; the chosen one turning in the middle, what it is
        // and what's known of it on the right.
        void BuildLedger(RectTransform page)
        {
            var centre = new Vector2(0.5f, 0.5f);
            ledgerList = new ScrollRows(page, textFont, -620f, 280f, 520f, 9);
            ledgerNone = UiKit.Text(page, "None", textFont, 32, UiKit.Ash.WithAlpha(0.7f), TextAnchor.MiddleCenter, centre, new Vector2(0, 40), 1f);
            ledgerNone.text = "No one's written in yet. That won't last.";
            ledgerGlow = UiKit.Image(page, "Glow", UiKit.SoftDot, Cold.WithAlpha(0.08f), centre, new Vector2(-60, 10), new Vector2(600, 600));
            ledgerView = UiKit.Rect(page, "Model", centre, new Vector2(-60, 10), new Vector2(680, 680)).gameObject.AddComponent<RawImage>();
            ledgerView.raycastTarget = false;
            ledgerView.texture = stage.Texture;
            const float x = 560f;
            Text Line(string name, Font font, int size, Color colour, float y, float height)
            {
                var t = UiKit.Text(page, name, font, size, colour, TextAnchor.UpperLeft, centre, new Vector2(x, y));
                t.horizontalOverflow = HorizontalWrapMode.Wrap;
                t.rectTransform.pivot = new Vector2(0.5f, 1f);
                t.rectTransform.sizeDelta = new Vector2(600, height);
                return t;
            }
            ledgerName = Line("Name", titleFont, 36, UiKit.Bone, 300, 100);
            ledgerKind = Line("Kind", textFont, 26, UiKit.Ember, 192, 40);
            ledgerDescription = Line("Description", textFont, 27, UiKit.Ash, 140, 170);
            ledgerKnownHeading = Line("Known", titleFont, 28, UiKit.Ember, -45, 40);
            ledgerKnownHeading.text = "Known";
            ledgerKnown = Line("Known Text", textFont, 27, UiKit.Bone, -85, 150);
            ledgerCount = Line("Count", textFont, 26, UiKit.Ash.WithAlpha(0.75f), -250, 40);
            ledgerDetail = new Graphic[] { ledgerName, ledgerKind, ledgerDescription, ledgerKnownHeading, ledgerKnown, ledgerCount, ledgerView, ledgerGlow };
        }

        void BuildLantern(RectTransform page)
        {
            var centre = new Vector2(0.5f, 0.5f);
            lanternGlow = UiKit.Image(page, "Glow", UiKit.SoftDot, Cold, centre, new Vector2(0, 110), new Vector2(420, 420));
            lanternCore = UiKit.Image(page, "Core", UiKit.Flame, Cold, centre, new Vector2(0, 110), new Vector2(64, 96));
            flames = new Image[Lantern.Levels];
            for (int i = 0; i < flames.Length; i++)
            {
                bool last = i == flames.Length - 1;
                flames[i] = UiKit.Image(page, "Flame", UiKit.Flame, Cold, centre, new Vector2(-180 + i * 90, -130), last ? new Vector2(40, 60) : new Vector2(28, 42));
            }
            lastRing = UiKit.Image(page, "Ring", UiKit.Ring, Cold, centre, new Vector2(180, -130), new Vector2(84, 84));
            lanternLine = UiKit.Text(page, "Line", textFont, 34, UiKit.Bone.WithAlpha(0.85f), TextAnchor.MiddleCenter, centre, new Vector2(0, -240), 1.5f);
        }

        static Image Diamond(Transform parent, Color colour, float size)
        {
            var image = UiKit.Image(parent, "Mark", null, colour, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size) * 0.7071f);
            image.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            return image;
        }

        // ---- opening, closing, turning pages

        bool CanOpen => apprentice != null && apprentice.IsAlive && apprentice.OpenMenu == null && !Pause.IsPaused
                        && (pause == null || !pause.Open) && !SceneFader.Busy;

        public void Toggle(bool open)
        {
            if (open == Open || open && !CanOpen)
                return;
            Open = open;
            Pause.Set(open);
            Sfx.Instance?.UiTick();
            if (!open)
                closedFrame = Time.frameCount;
            else
                Draw();
        }

        public void Turn(int by)
        {
            Current = (Tab)(((int)Current + by + Tabs) % Tabs);
            satchelPicking = false;
            Sfx.Instance?.UiTick();
            Draw();
        }

        public void ChooseCharm(int by) => Choose(charmList, by);
        public void ChooseProvision(int by) => Choose(satchelList, by);
        public void ChooseAbility(int by) => Choose(abilityList, by);
        public void ChooseEntry(int by) => Choose(ledgerList, by);

        void Choose(ScrollRows list, int by)
        {
            if (!list.Move(by))
                return;
            Sfx.Instance?.UiTick();
            Draw();
        }

        // ---- the Satchel page: a belt slot picked, then what goes in it

        // The slot to the left or right (on the belt), or up/down the list (choosing what goes in it).
        public void MoveSlot(int by)
        {
            if (satchelPicking)
                return;
            satchelSlot = (satchelSlot + by + Provision.BeltSlots) % Provision.BeltSlots;
            Sfx.Instance?.UiTick();
            Draw();
        }

        // Confirm: on the belt, choose what goes in the slot (the list, starting at what's in it); in the list, put the
        // chosen one in the slot (swapping with its own slot, if it had one; "empty" takes the slot's out).
        public void SatchelConfirm()
        {
            var e = apprentice.Equipment;
            if (!satchelPicking)
            {
                satchelPicking = true;
                DrawSatchel();
                var inSlot = e.Belt[satchelSlot];
                int row = inSlot != null ? satchel.IndexOf(inSlot) : 0;
                satchelList.Select(Mathf.Max(0, row));
            }
            else
            {
                var chosen = satchel[satchelList.Selected];
                e.SetBeltSlot(satchelSlot, chosen);
                satchelPicking = false;
            }
            Sfx.Instance?.UiTick();
            Draw();
        }

        // Back: from the list to the belt (nothing changed); from the belt, false (the screen closes).
        public bool SatchelBack()
        {
            if (!satchelPicking)
                return false;
            satchelPicking = false;
            Sfx.Instance?.UiTick();
            Draw();
            return true;
        }

        void Update()
        {
            InputDevice.Track();
            input.Tick();
            if (!Open)
            {
                if (input.CharacterPressed)
                    Toggle(true);
            }
            else if (input.Back && Current == Tab.Satchel && SatchelBack())
            {
            }
            else if (input.CharacterPressed || input.Back || input.PausePressed)
                Toggle(false);
            else
            {
                var step = input.Step;
                int page = input.PageStep;
                if (page != 0)
                    Turn(page);
                else if (Current == Tab.Satchel)
                {
                    if (step.x != 0)
                        MoveSlot(step.x);
                    else if (step.y != 0 && satchelPicking)
                        ChooseProvision(-step.y);
                    else if (input.Confirm && apprentice.Equipment != null)
                        SatchelConfirm();
                }
                else if (step.y != 0 && Current == Tab.Charms)
                    ChooseCharm(-step.y);
                else if (step.y != 0 && Current == Tab.Abilities)
                    ChooseAbility(-step.y);
                else if (step.y != 0 && Current == Tab.Ledger)
                    ChooseEntry(-step.y);
            }
            if (group == null)
                return;
            group.alpha = Mathf.MoveTowards(group.alpha, Open ? 1f : 0f, Time.unscaledDeltaTime * 6f);
            group.gameObject.SetActive(group.alpha > 0.001f || Open);
            stage?.Draw(group.alpha > 0.001f && (Current == Tab.Oar || Current == Tab.Satchel || Current == Tab.Ledger));
            if (Open)
                Draw();
        }

        // ---- drawing

        void Draw()
        {
            if (apprentice == null || group == null)
                return;
            for (int i = 0; i < Tabs; i++)
            {
                bool on = i == (int)Current;
                tabs[i].text = TabNames[i];
                tabs[i].color = on ? UiKit.Bone : UiKit.Ash.WithAlpha(0.6f);
                pages[i].gameObject.SetActive(on);
            }
            var chosen = tabs[(int)Current].rectTransform;
            float half = tabs[(int)Current].preferredWidth / 2f + 30f;
            tabLeft.rectTransform.anchoredPosition = chosen.anchoredPosition + new Vector2(-half, 0);
            tabRight.rectTransform.anchoredPosition = chosen.anchoredPosition + new Vector2(half, 0);
            hints.text = HintLine();
            switch (Current)
            {
                case Tab.Apprentice: DrawApprentice(); break;
                case Tab.Oar: DrawOar(); break;
                case Tab.Abilities: DrawAbilities(); break;
                case Tab.Satchel: DrawSatchel(); break;
                case Tab.Charms: DrawCharms(); break;
                case Tab.Ledger: DrawLedger(); break;
                default: DrawLantern(); break;
            }
        }

        // The keys for what can be done on this page, for the controls in use.
        string HintLine()
        {
            bool pad = InputDevice.Gamepad;
            string pages = pad ? "LB / RB: page" : "Q / R: page";
            string close = pad ? "B: close" : "Esc: close";
            string gap = "     ";
            switch (Current)
            {
                case Tab.Satchel:
                    return satchelPicking
                        ? $"Up / Down: choose{gap}{(pad ? "A" : "E")}: put it in the slot{gap}{(pad ? "B" : "Esc")}: back"
                        : $"{pages}{gap}Left / Right: a slot{gap}{(pad ? "A" : "E")}: choose what goes in it{gap}{close}";
                case Tab.Abilities:
                case Tab.Charms:
                case Tab.Ledger:
                    return $"{pages}{gap}Up / Down: choose{gap}{close}";
                default:
                    return $"{pages}{gap}{close}";
            }
        }

        static string Count(int n, string one, string many) => n == 1 ? $"1 {one}" : $"{n} {many}";

        void DrawApprentice()
        {
            var e = apprentice.Equipment;
            Stat(0, $"{Mathf.RoundToInt(apprentice.maxHealth)}", e.WarmStones > 0 ? $"{Count(e.WarmStones, "Warm Stone", "Warm Stones")} found" : "");
            Stat(1, $"{Mathf.RoundToInt(apprentice.maxStamina)}", e.HeldBreaths > 0 ? $"{Count(e.HeldBreaths, "Held Breath", "Held Breaths")} found" : "");
            Stat(2, Count(apprentice.maxHealingCharges, "Ember", "Embers"), "each a heal, rekindled at a post");
            Stat(3, $"{e.FerryNails}", e.FerryNails > 0 ? "for the oar, at a Mooring post" : "");
            Stat(4, $"{apprentice.Obols}", "carried: left where you fall");
        }

        void Stat(int row, string value, string note)
        {
            stats[row].value.text = value;
            stats[row].note.text = note;
        }

        void DrawOar()
        {
            var e = apprentice.Equipment;
            int step = e.OarStep;
            // Leaning, blade up and to the right, turning slowly about its length.
            stage.Show(oarModels != null && step < oarModels.Length ? oarModels[step] : null, Quaternion.Euler(0f, 0f, -38f));
            float breathe = 0.5f + 0.5f * Mathf.Sin(UiKit.Now * 1.6f);
            oarGlow.color = (step == Oar.Last ? new Color(1f, 0.8f, 0.4f) : Cold).WithAlpha((step == Oar.Last ? 0.3f : 0.08f + 0.03f * step) + 0.06f * breathe);
            oarName.text = e.Oar.Name;
            oarDescription.text = e.Oar.Description;
            for (int i = 0; i < pips.Length; i++)
                pips[i].color = i <= step ? UiKit.Ember : UiKit.Ash.WithAlpha(0.22f);
            var next = e.NextOar;
            oarNext.text = next == null
                ? "It will grow no stronger."
                : $"Next: ???     ·     {Count(next.Cost, "Ferry Nail", "Ferry Nails")} (you carry {e.FerryNails})";
            oarNext.color = next != null && e.CanStrengthen ? UiKit.Bone : UiKit.Ash;
        }

        // The Abilities found (how many there'll be isn't said: no sockets for the rest), the chosen one beside them.
        void DrawAbilities()
        {
            abilityRowText.Clear();
            var found = new List<Ability>();
            foreach (var a in Ability.All)
                if (a.FoundBy(apprentice))
                {
                    found.Add(a);
                    abilityRowText.Add(a.Name);
                }
            var rows = new List<ScrollRows.Item>();
            foreach (var name in abilityRowText)
                rows.Add(new ScrollRows.Item { Label = name });
            abilityList.Set(rows);
            bool any = found.Count > 0;
            abilityNone.enabled = !any;
            foreach (var g in abilityDetail)
                g.enabled = any;
            var chosen = any ? found[abilityList.Selected] : null;
            abilityName.text = chosen?.Name ?? "";
            abilityDescription.text = chosen?.Description ?? "";
            abilityKeys.text = chosen != null ? InputDevice.Pick(chosen.Keys, chosen.Pad) : "";
            abilityPad.text = "";
            for (int i = 0; i < abilityUses.Length; i++)
            {
                bool on = chosen != null && i < chosen.Uses.Length;
                abilityUses[i].enabled = on;
                abilityUses[i].text = on ? "·  " + chosen.Uses[i] : "";
            }
        }

        // The belt's four slots (the picked one lit); below, while choosing, every Provision carried (and "empty"); the
        // one in question turning, and what it is.
        void DrawSatchel()
        {
            var e = apprentice.Equipment;
            satchel.Clear();
            satchelRowText.Clear();
            var rows = new List<ScrollRows.Item>();
            satchel.Add(null);
            satchelRowText.Add("(empty)");
            rows.Add(new ScrollRows.Item { Label = "(empty)", Dim = true });
            foreach (var p in Provision.All)
                if (e.Carried(p) > 0 || e.OnBelt(p))
                {
                    satchel.Add(p);
                    satchelRowText.Add(p.Name);
                    int slot = e.BeltSlotOf(p);
                    rows.Add(new ScrollRows.Item { Label = p.Name, Value = $"{e.Carried(p)}/{p.Cap}" + (slot >= 0 ? $"  slot {slot + 1}" : ""), Dim = e.Carried(p) == 0 });
                }
            satchelList.Set(rows, satchelPicking);
            bool any = satchel.Count > 1;
            satchelNone.enabled = !any;
            for (int i = 0; i < beltSlots.Length; i++)
            {
                var p = e.Belt[i];
                var (frame, icon, count, empty) = beltSlots[i];
                bool picked = i == satchelSlot;
                icon.enabled = p != null;
                empty.enabled = p == null;
                count.text = p != null ? $"{e.Carried(p)}" : "";
                if (p != null)
                {
                    icon.sprite = Hud.IconOf(p);
                    icon.color = Hud.ColourOf(p).WithAlpha(e.Carried(p) > 0 ? 1f : 0.3f);
                }
                float pulse = 0.5f + 0.5f * Mathf.Sin(UiKit.Now * 5f);
                frame.color = picked ? Color.Lerp(UiKit.Ember, Color.white, satchelPicking ? 0.15f : 0.3f * pulse) : UiKit.Bronze.WithAlpha(p != null ? 1f : 0.45f);
                frame.rectTransform.localScale = Vector3.one * (picked ? 1.12f : 1f);
            }
            // What's in question: the list's choice while choosing, else what's in the picked slot.
            var shown = satchelPicking ? satchel[satchelList.Selected] : e.Belt[satchelSlot];
            foreach (var g in satchelDetail)
                g.enabled = shown != null;
            satchelName.text = shown?.Name ?? "";
            satchelDescription.text = shown?.Description ?? "";
            satchelEffect.text = shown?.Effect ?? "";
            satchelBeltLine.text = satchelPicking
                ? $"For slot {satchelSlot + 1}."
                : e.Belt[satchelSlot] == null ? $"Slot {satchelSlot + 1} is empty." : $"Slot {satchelSlot + 1}.";
            int kind = shown != null ? System.Array.IndexOf(Provision.All, shown) : -1;
            if (Current == Tab.Satchel)
                stage.Show(provisionModels != null && kind >= 0 && kind < provisionModels.Length ? provisionModels[kind] : null, Quaternion.Euler(28f, 0f, 0f));
        }

        // Every kind met so far, in the order they're met on the way (how many there are isn't said), the chosen one
        // turning beside them.
        void DrawLedger()
        {
            var e = apprentice.Equipment;
            ledgerShown.Clear();
            ledgerRowText.Clear();
            var rows = new List<ScrollRows.Item>();
            foreach (var entry in LedgerEntry.All)
                if (e.Met(entry))
                {
                    ledgerShown.Add(entry);
                    ledgerRowText.Add(entry.Name);
                    rows.Add(new ScrollRows.Item { Label = ShortName(entry) }); // (what it is: on the right)
                }
            ledgerList.Set(rows);
            bool any = ledgerShown.Count > 0;
            ledgerNone.enabled = !any;
            foreach (var g in ledgerDetail)
                g.enabled = any;
            var chosen = any ? ledgerShown[ledgerList.Selected] : null;
            ledgerName.text = chosen?.Name ?? "";
            ledgerKind.text = chosen?.KindLine ?? "";
            ledgerDescription.text = chosen?.Description ?? "";
            ledgerKnown.text = chosen?.Known ?? "";
            int n = e.Scattered(chosen);
            ledgerCount.text = chosen == null ? "" : chosen.Kind == LedgerEntry.Rank.Soured
                ? (n == 1 ? "Scattered once." : $"Scattered {n} times.")
                : (n == 1 ? "Beaten." : $"Beaten {n} times.");
            int kind = chosen != null ? System.Array.IndexOf(LedgerEntry.All, chosen) : -1;
            // Standing ones face you, swaying a little to and fro (their faces, not their backs); a Moth turns, seen
            // a little from above.
            // The Unparted three-quarters on, a little from above: Fast, on Hold's back, peers over its shoulder.
            bool standing = chosen != null && chosen.Standing;
            bool two = chosen == LedgerEntry.Unparted;
            if (Current == Tab.Ledger)
                stage.Show(ledgerModels != null && kind >= 0 && kind < ledgerModels.Length ? ledgerModels[kind] : null,
                    Quaternion.Euler(two ? 18f : standing ? 6f : 14f, 0f, 0f), facing: standing, turnedBy: two ? 55f : 0f);
        }

        // Its row's name: the title, without the rest of a Boss's (the full name's on the right).
        static string ShortName(LedgerEntry entry)
        {
            int comma = entry.Name.IndexOf(',');
            return comma > 0 ? entry.Name.Substring(0, comma) : entry.Name;
        }

        // The charms found (no dev charm unless it's owned), then a dark socket for each still to find.
        void DrawCharms()
        {
            var e = apprentice.Equipment;
            for (int i = 0; i < slotMarks.Length; i++)
            {
                slotMarks[i].gameObject.SetActive(i < e.charmSlots);
                slotMarks[i].color = i < e.Fitted.Count ? UiKit.Ember : UiKit.Ash.WithAlpha(0.3f);
            }
            var found = new List<Charm>(e.Owned);
            int unfound = 0;
            foreach (var c in Charm.All)
                if (!e.Owns(c) && c != Charm.TestersKnot)
                    unfound++;
            charmRowText.Clear();
            foreach (var c in found)
                charmRowText.Add(c.Name);
            for (int i = 0; i < unfound; i++)
                charmRowText.Add("Not yet found");
            var rows = new List<ScrollRows.Item>();
            for (int i = 0; i < charmRowText.Count; i++)
            {
                bool isFound = i < found.Count;
                rows.Add(new ScrollRows.Item { Label = charmRowText[i], Value = isFound && e.IsFitted(found[i]) ? "fitted" : "", Dim = !isFound });
            }
            charmList.Set(rows);
            if (charmRowText.Count == 0)
                charmDetail.text = "Nothing yet.";
            else if (SelectedCharm < found.Count)
                charmDetail.text = found[SelectedCharm].Description;
            else
                charmDetail.text = "Somewhere down here. Probably behind something unpleasant.";
        }

        void DrawLantern()
        {
            int level = Lantern.Level;
            float breathe = 0.5f + 0.5f * Mathf.Sin(UiKit.Now * 1.1f);
            float strength = level / (float)Lantern.Levels;
            lanternGlow.color = Cold.WithAlpha(0.18f + 0.3f * strength + 0.06f * breathe);
            lanternGlow.rectTransform.sizeDelta = Vector2.one * (300f + 200f * strength + 12f * breathe);
            lanternCore.color = Color.Lerp(Cold, Color.white, strength).WithAlpha(0.8f + 0.2f * breathe);
            for (int i = 0; i < flames.Length; i++)
            {
                float alpha = i < level ? 0.95f : i == level ? 0.08f + 0.3f * Lantern.TowardNext : 0.08f;
                flames[i].color = Cold.WithAlpha(alpha);
            }
            lastRing.color = Cold.WithAlpha(level >= Lantern.Levels ? 0.7f + 0.3f * breathe : 0.12f);
            lanternLine.text = Lantern.Line;
        }
        // Where a model turns for a page (the oar, a Provision): far below the world, lit by two lamps of its own (warm
        // and cold, for the metal's glints), seen by a camera that draws only while such a page is open. Each model is
        // fitted to the frame (its longest side made `size` metres, its middle at the pivot) when it's added.
        public sealed class ModelStage
        {
            readonly Transform pivot;
            readonly Camera camera;
            readonly List<GameObject> models = new List<GameObject>();
            Quaternion lean = Quaternion.identity;
            bool facing;
            float turned;

            public RenderTexture Texture { get; }
            public GameObject Shown { get; private set; }

            public ModelStage(Transform parent, Color cold)
            {
                Texture = new RenderTexture(768, 768, 24, RenderTextureFormat.ARGB32) { name = "Model Stage", antiAliasing = 4 };
                var root = new GameObject("Model Stage").transform;
                root.SetParent(parent, false);
                root.position = new Vector3(0f, -400f, 0f);
                pivot = new GameObject("Pivot").transform;
                pivot.SetParent(root, false);
                camera = new GameObject("Stage Camera").AddComponent<Camera>();
                camera.transform.SetParent(root, false);
                camera.transform.localPosition = new Vector3(0f, 0f, -3.9f);
                camera.fieldOfView = 42f;
                camera.nearClipPlane = 0.5f;
                camera.farClipPlane = 8f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                camera.targetTexture = Texture;
                camera.enabled = false;
                Lamp(root, new Vector3(1.4f, 1.2f, -1.8f), new Color(1f, 0.85f, 0.65f), 2.6f);
                Lamp(root, new Vector3(-1.6f, -0.4f, -1.4f), cold, 2.5f);
            }

            static void Lamp(Transform root, Vector3 at, Color colour, float intensity)
            {
                var lamp = new GameObject("Stage Lamp").AddComponent<Light>();
                lamp.transform.SetParent(root, false);
                lamp.transform.localPosition = at;
                lamp.type = LightType.Point;
                lamp.range = 6f;
                lamp.color = colour;
                lamp.intensity = intensity;
                lamp.shadows = LightShadows.None;
            }

            // Onto the stage, fitted: its longest side (or its height) `size` metres, its middle on the pivot. Hidden until shown.
            public void Add(GameObject model, float size, bool byHeight = false)
            {
                if (model == null)
                    return;
                var holder = new GameObject(model.name + " (fitted)").transform;
                holder.SetParent(pivot, false);
                model.transform.SetParent(holder, false);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;
                model.SetActive(true);
                var bounds = new Bounds(holder.position, Vector3.zero);
                bool first = true;
                foreach (var r in model.GetComponentsInChildren<Renderer>())
                {
                    var b = r is SkinnedMeshRenderer skinned ? Posed(skinned) : r.bounds; // (as it stands, not its rest pose)
                    if (first)
                        bounds = b;
                    else
                        bounds.Encapsulate(b);
                    first = false;
                }
                float longest = byHeight ? bounds.size.y : Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
                model.transform.localPosition = -holder.InverseTransformPoint(bounds.center); // (its middle at the pivot)
                holder.localScale = Vector3.one * (longest > 0.001f ? size / longest : 1f);
                model.SetActive(false);
                models.Add(model);
            }

            // Where a skinned mesh is, in the pose it's in now (its own bounds are its rest pose's, or a guess).
            static Bounds Posed(SkinnedMeshRenderer skinned)
            {
                var mesh = new Mesh();
                skinned.BakeMesh(mesh, true);
                var t = skinned.transform;
                var verts = mesh.vertices;
                Object.Destroy(mesh);
                if (verts.Length == 0)
                    return skinned.bounds;
                var b = new Bounds(t.position + t.rotation * verts[0], Vector3.zero);
                foreach (var v in verts)
                    b.Encapsulate(t.position + t.rotation * v);
                return b;
            }

            // The one to show (null: none), leaning so, turning slowly about its own up.
            // (`facing`: towards the camera, swaying a little either way, instead of turning right round)
            public void Show(GameObject model, Quaternion leanedSo, bool facing = false, float turnedBy = 0f)
            {
                lean = leanedSo;
                this.facing = facing;
                turned = turnedBy;
                if (Shown == model)
                    return;
                Shown = model;
                foreach (var m in models)
                    m.SetActive(m == model);
            }

            public void Draw(bool on)
            {
                camera.enabled = on && Shown != null;
                float yaw = facing ? 180f + turned + 38f * Mathf.Sin(UiKit.Now * 0.55f) : UiKit.Now * 40f; // (a figure's front is its +z: away from the camera)
                pivot.localRotation = lean * Quaternion.Euler(0f, yaw, 0f);
            }

            public void Release() => Texture.Release();
        }

        // A list of rows that scrolls: as many as fit are shown, the chosen one kept in view, and a caret above or below
        // when there are more that way. For lists whose length isn't known yet (Abilities, Charms).
        public sealed class ScrollRows
        {
            public struct Item
            {
                public string Label, Value;
                public bool Dim;
            }

            const float RowHeight = 58f;
            readonly (Image band, Text label, Text value)[] rows;
            readonly Image up, down;
            int first, count;

            public int Selected { get; private set; }
            public int First => first;

            public ScrollRows(Transform page, Font font, float x, float top, float width, int visible)
            {
                var centre = new Vector2(0.5f, 0.5f);
                rows = new (Image, Text, Text)[visible];
                for (int i = 0; i < visible; i++)
                {
                    float y = top - i * RowHeight;
                    var band = UiKit.Image(page, "Band", UiKit.Band, UiKit.Ember.WithAlpha(0.14f), centre, new Vector2(x, y), new Vector2(width, RowHeight + 6));
                    var label = UiKit.Text(page, "Label", font, 34, UiKit.Bone, TextAnchor.MiddleLeft, centre, new Vector2(x - width / 2f + 60f, y), 1f);
                    label.rectTransform.pivot = new Vector2(0f, 0.5f);
                    label.rectTransform.sizeDelta = new Vector2(width - 120f, RowHeight);
                    var value = UiKit.Text(page, "Value", font, 30, UiKit.Ember, TextAnchor.MiddleRight, centre, new Vector2(x + width / 2f - 60f, y), 1f);
                    value.rectTransform.pivot = new Vector2(1f, 0.5f);
                    value.rectTransform.sizeDelta = new Vector2(300, RowHeight);
                    rows[i] = (band, label, value);
                }
                up = Caret(page, new Vector2(x, top + RowHeight * 0.62f), true);
                down = Caret(page, new Vector2(x, top - (visible - 0.38f) * RowHeight), false);
            }

            // A small chevron (two strokes).
            static Image Caret(Transform page, Vector2 at, bool pointsUp)
            {
                var root = UiKit.Image(page, "Caret", null, Color.clear, new Vector2(0.5f, 0.5f), at, new Vector2(30, 16));
                foreach (float side in new[] { -1f, 1f })
                {
                    var stroke = UiKit.Image(root.rectTransform, "Stroke", null, UiKit.Ember.WithAlpha(0.8f), new Vector2(0.5f, 0.5f), new Vector2(side * 6f, 0f), new Vector2(16, 3));
                    stroke.rectTransform.localRotation = Quaternion.Euler(0, 0, (pointsUp ? -30f : 30f) * side);
                }
                return root;
            }

            // Moves the choice (wrapping round); false if there's nothing to move to.
            public bool Move(int by)
            {
                if (count < 2)
                    return false;
                Selected = (Selected + by + count) % count;
                first = Window(first, Selected, count, rows.Length);
                return true;
            }

            // The first row shown, so the chosen one is in view (moving the window as little as it can).
            public static int Window(int first, int selected, int count, int visible)
            {
                if (selected < first)
                    first = selected;
                if (selected >= first + visible)
                    first = selected - visible + 1;
                return Mathf.Clamp(first, 0, Mathf.Max(0, count - visible));
            }

            // Chooses a row (the window follows).
            public void Select(int item)
            {
                Selected = Mathf.Clamp(item, 0, Mathf.Max(0, count - 1));
                first = Window(first, Selected, count, rows.Length);
            }

            // (`active` false: shown, but nothing chosen: no band, all dim, as a list waiting its turn)
            public void Set(IReadOnlyList<Item> items, bool active = true)
            {
                count = items.Count;
                Selected = Mathf.Clamp(Selected, 0, Mathf.Max(0, count - 1));
                first = Window(first, Selected, count, rows.Length);
                for (int i = 0; i < rows.Length; i++)
                {
                    var (band, label, value) = rows[i];
                    int item = first + i;
                    bool on = item < count;
                    label.gameObject.SetActive(on);
                    value.gameObject.SetActive(on);
                    band.enabled = on && item == Selected && active;
                    if (!on)
                        continue;
                    label.text = items[item].Label;
                    label.color = items[item].Dim ? UiKit.Ash.WithAlpha(0.35f) : item == Selected && active ? UiKit.Bone : UiKit.Ash.WithAlpha(active ? 1f : 0.7f);
                    value.text = items[item].Value ?? "";
                }
                up.gameObject.SetActive(first > 0);
                down.gameObject.SetActive(first + rows.Length < count);
            }
        }
    }
}
