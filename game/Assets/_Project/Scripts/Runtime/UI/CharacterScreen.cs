using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MyGame
{
    // The character screen (I or C; Select on a gamepad): the world pauses behind four pages, left/right between them.
    // The Apprentice (health, stamina, healing, what's carried), The Oar (its step: its name, what it is, how strong,
    // what the next step asks), Charms (those found, those fitted, the slots, and dark sockets for the rest) and The
    // Lantern, which says as little as it can. Closed with the same key, Esc or B. Drawn in real time.
    public sealed class CharacterScreen : MonoBehaviour
    {
        public enum Tab { Apprentice, Oar, Charms, Lantern }
        public static readonly string[] TabNames = { "The Apprentice", "The Oar", "Charms", "The Lantern" };

        public Font titleFont, textFont;

        public static CharacterScreen Instance { get; private set; }
        // Whether Esc belongs to it this frame (open, or just closed by it): the pause menu leaves it alone.
        public static bool Blocks => Instance != null && (Instance.Open || Instance.closedFrame == Time.frameCount);

        public bool Open { get; private set; }
        public Tab Current { get; private set; }
        public int SelectedCharm { get; private set; }

        // What each page says (tests, and the capture).
        public string OarName => oarName.text;
        public string OarNext => oarNext.text;
        public string LanternLine => lanternLine.text;
        public string CharmDetail => charmDetail.text;
        public IReadOnlyList<string> CharmRows => charmRowText;
        public string StatValue(int row) => stats[row].value.text;

        static readonly Color Cold = new Color(0.78f, 0.88f, 1f);
        const float CharmRowHeight = 58f;
        const int CharmRows_ = 8;

        MenuInput input;
        Apprentice apprentice;
        PauseMenu pause;
        CanvasGroup group;
        Image shade;
        int closedFrame = -1;

        readonly Text[] tabs = new Text[4];
        Image tabLeft, tabRight;
        readonly RectTransform[] pages = new RectTransform[4];

        (Text label, Text value, Text note)[] stats;
        Image oar, oarGlow;
        readonly List<(Image image, int from, int to)> oarTrims = new List<(Image, int, int)>();
        Text oarName, oarDescription, oarNext;
        Image[] pips;
        Image[] slotMarks;
        (Image band, Text label, Text value)[] charmRows;
        readonly List<string> charmRowText = new List<string>();
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
            for (int i = 0; i < 4; i++)
                tabs[i] = UiKit.Text(root, "Tab", titleFont, 40, UiKit.Ash, TextAnchor.MiddleCenter, centre, new Vector2(-600 + i * 400, 400), 3f);
            tabLeft = Diamond(root, UiKit.Ember, 10f);
            tabRight = Diamond(root, UiKit.Ember, 10f);
            UiKit.Image(root, "Rule", UiKit.Rule, UiKit.Bone.WithAlpha(0.55f), centre, new Vector2(0, 360), new Vector2(1600, 3));
            var hintBand = UiKit.Image(root, "Hint Band", UiKit.Band, Color.black.WithAlpha(0.55f), new Vector2(0.5f, 0f), new Vector2(0, 70), new Vector2(1500, 70));
            var hints = UiKit.Text(root, "Hints", textFont, 26, UiKit.Bone.WithAlpha(0.75f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0, 70), 1f);
            hints.text = "Left / Right: page     Up / Down: choose     I / Select, Esc / B: close";

            for (int i = 0; i < 4; i++)
                pages[i] = UiKit.Stretch(UiKit.Rect(root, TabNames[i], centre, Vector2.zero, Vector2.zero));
            BuildApprentice(pages[0]);
            BuildOar(pages[1]);
            BuildCharms(pages[2]);
            BuildLantern(pages[3]);
            root.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
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
            oarGlow = UiKit.Image(page, "Glow", UiKit.SoftDot, Cold.WithAlpha(0f), centre, new Vector2(280, 215), new Vector2(360, 200));
            oar = UiKit.Image(page, "Oar", UiKit.OarShape, Color.white, centre, new Vector2(0, 215), new Vector2(840, 105));
            // Its trims, as the model's (OarLook): along the sprite (u 0 the butt, 1 the blade's tip), their height a
            // share of the oar's (the shaft is 0.18 of it, the blade 0.67).
            void Trim(float u, float width, float height, uint colour, int from, int to, Sprite sprite = null, float v = 0.5f)
            {
                var image = UiKit.Image(oar.rectTransform, "Trim", sprite, UiKit.Hex(colour), new Vector2(u, 0.5f), Vector2.zero, Vector2.zero);
                var rt = image.rectTransform;
                rt.anchorMin = new Vector2(u - width / 2f, v - height / 2f);
                rt.anchorMax = new Vector2(u + width / 2f, v + height / 2f);
                rt.sizeDelta = Vector2.zero;
                oarTrims.Add((image, from, to));
            }
            Trim(0.63f, 0.012f, 0.26f, 0xb8352b, 0, 3);                   // the Apprentice's red band
            foreach (float u in new[] { 0.14f, 0.5f, 0.57f })
                Trim(u, 0.014f, 0.24f, 0x2a221c, 1, 3);                    // tar
            for (int i = 0; i < 4; i++)
                Trim(0.25f + i * 0.012f, 0.005f, 0.22f, 0xa38b62, 1, 3);   // twine
            Trim(0.012f, 0.02f, 0.24f, 0x3a3a42, 2, 4);                    // the iron ferrule
            Trim(0.981f, 0.012f, 0.5f, 0x3a3a42, 2, 4);                    // the iron-shod tip
            Trim(0.815f, 0.27f, 0.05f, 0x3a3a42, 2, 4, null, 0.81f);        // (and its edges, along the blade)
            Trim(0.815f, 0.27f, 0.05f, 0x3a3a42, 2, 4, null, 0.19f);
            for (int i = 0; i < 8; i++)
                Trim(0.33f + i * 0.02f, 0.003f, 0.2f, 0xe2d6bc, 3, 4);     // a notch for every crossing
            Trim(0.83f, 0.055f, 0.44f, 0x9fc4ff, 4, 4, UiKit.Ring);        // his mark
            oarName = UiKit.Text(page, "Name", titleFont, 54, UiKit.Bone, TextAnchor.MiddleCenter, centre, new Vector2(0, 100), 3f);
            oarDescription = UiKit.Text(page, "Description", textFont, 32, UiKit.Ash, TextAnchor.UpperCenter, centre, new Vector2(0, 40));
            oarDescription.horizontalOverflow = HorizontalWrapMode.Wrap;
            oarDescription.rectTransform.pivot = new Vector2(0.5f, 1f);
            oarDescription.rectTransform.sizeDelta = new Vector2(1000, 130);
            var strength = UiKit.Text(page, "Strength", textFont, 30, UiKit.Bone, TextAnchor.MiddleRight, centre, new Vector2(-120, -120), 1f);
            strength.rectTransform.pivot = new Vector2(1f, 0.5f);
            strength.rectTransform.sizeDelta = new Vector2(300, 50);
            strength.text = "Strength";
            pips = new Image[Oar.Steps.Length];
            for (int i = 0; i < pips.Length; i++)
            {
                pips[i] = Diamond(page, UiKit.Ember, 18f);
                pips[i].rectTransform.anchoredPosition = new Vector2(-70 + i * 50, -120);
            }
            oarNext = UiKit.Text(page, "Next", textFont, 30, UiKit.Ash, TextAnchor.MiddleCenter, centre, new Vector2(0, -190), 1f);
            var how = UiKit.Text(page, "How", textFont, 26, UiKit.Ash.WithAlpha(0.65f), TextAnchor.MiddleCenter, centre, new Vector2(0, -250), 1f);
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
            charmRows = new (Image, Text, Text)[CharmRows_];
            for (int i = 0; i < CharmRows_; i++)
            {
                float y = 210 - i * CharmRowHeight;
                var band = UiKit.Image(page, "Band", UiKit.Band, UiKit.Ember.WithAlpha(0.14f), centre, new Vector2(0, y), new Vector2(1000, CharmRowHeight + 6));
                var label = UiKit.Text(page, "Label", textFont, 34, UiKit.Bone, TextAnchor.MiddleLeft, centre, new Vector2(-420, y), 1f);
                label.rectTransform.pivot = new Vector2(0f, 0.5f);
                label.rectTransform.sizeDelta = new Vector2(600, CharmRowHeight);
                var value = UiKit.Text(page, "Value", textFont, 30, UiKit.Ember, TextAnchor.MiddleRight, centre, new Vector2(420, y), 1f);
                value.rectTransform.pivot = new Vector2(1f, 0.5f);
                value.rectTransform.sizeDelta = new Vector2(300, CharmRowHeight);
                charmRows[i] = (band, label, value);
            }
            charmDetail = UiKit.Text(page, "Detail", textFont, 30, UiKit.Ash, TextAnchor.UpperCenter, centre, new Vector2(0, -262));
            charmDetail.horizontalOverflow = HorizontalWrapMode.Wrap;
            charmDetail.rectTransform.pivot = new Vector2(0.5f, 1f);
            charmDetail.rectTransform.sizeDelta = new Vector2(1100, 90);
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
            Current = (Tab)(((int)Current + by + 4) % 4);
            Sfx.Instance?.UiTick();
            Draw();
        }

        public void ChooseCharm(int by)
        {
            int count = CharmCount;
            if (count == 0)
                return;
            SelectedCharm = (SelectedCharm + by + count) % count;
            Sfx.Instance?.UiTick();
            Draw();
        }

        void Update()
        {
            input.Tick();
            if (!Open)
            {
                if (input.CharacterPressed)
                    Toggle(true);
            }
            else if (input.CharacterPressed || input.Back || input.PausePressed)
                Toggle(false);
            else
            {
                var step = input.Step;
                if (step.x != 0)
                    Turn(step.x);
                else if (step.y != 0 && Current == Tab.Charms)
                    ChooseCharm(-step.y);
            }
            if (group == null)
                return;
            group.alpha = Mathf.MoveTowards(group.alpha, Open ? 1f : 0f, Time.unscaledDeltaTime * 6f);
            group.gameObject.SetActive(group.alpha > 0.001f || Open);
            if (Open)
                Draw();
        }

        // ---- drawing

        void Draw()
        {
            if (apprentice == null || group == null)
                return;
            for (int i = 0; i < 4; i++)
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
            switch (Current)
            {
                case Tab.Apprentice: DrawApprentice(); break;
                case Tab.Oar: DrawOar(); break;
                case Tab.Charms: DrawCharms(); break;
                default: DrawLantern(); break;
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
            oar.color = OarLook.Wood[step];
            foreach (var (image, from, to) in oarTrims)
                image.enabled = step >= from && step <= to;
            float breathe = 0.5f + 0.5f * Mathf.Sin(UiKit.Now * 1.6f);
            oarGlow.color = Cold.WithAlpha(step == Oar.Last ? 0.35f + 0.15f * breathe : 0f);
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

        // The charms found (no dev charm unless it's owned), then a dark socket for each still to find.
        int CharmCount => charmRowText.Count;

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
            SelectedCharm = Mathf.Clamp(SelectedCharm, 0, Mathf.Max(0, charmRowText.Count - 1));
            for (int i = 0; i < charmRows.Length; i++)
            {
                var (band, label, value) = charmRows[i];
                bool on = i < charmRowText.Count;
                label.gameObject.SetActive(on);
                value.gameObject.SetActive(on);
                band.enabled = on && i == SelectedCharm;
                if (!on)
                    continue;
                bool isFound = i < found.Count;
                label.text = charmRowText[i];
                label.color = !isFound ? UiKit.Ash.WithAlpha(0.35f) : i == SelectedCharm ? UiKit.Bone : UiKit.Ash;
                value.text = isFound && e.IsFitted(found[i]) ? "fitted" : "";
            }
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
    }
}
