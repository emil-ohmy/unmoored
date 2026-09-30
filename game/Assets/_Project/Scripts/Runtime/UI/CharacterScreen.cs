using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MyGame
{
    // The character screen (I or C; Select on a gamepad): the world pauses behind six pages, left/right between them.
    // The Apprentice (health, stamina, healing, what's carried), The Oar (its step: its name, what it is, how strong,
    // what the next step asks), Abilities (each found: what it is, how it's used, what it's for), The Satchel (the
    // Provisions carried, and which of them go on the belt's four slots: confirm moves one round), Charms (those found,
    // those fitted, the slots, and dark sockets for the rest) and The Lantern, which says as little as it can. Long lists
    // scroll. Closed with the same key, Esc or B. Drawn in real time.
    public sealed class CharacterScreen : MonoBehaviour
    {
        public enum Tab { Apprentice, Oar, Abilities, Satchel, Charms, Lantern }
        public static readonly string[] TabNames = { "The Apprentice", "The Oar", "Abilities", "The Satchel", "Charms", "The Lantern" };
        static int Tabs => TabNames.Length;

        public Font titleFont, textFont;
        public GameObject[] oarModels; // the oar at each step, on its own (oar_display): shown turning on the Oar page

        public static CharacterScreen Instance { get; private set; }
        // Whether Esc belongs to it this frame (open, or just closed by it): the pause menu leaves it alone.
        public static bool Blocks => Instance != null && (Instance.Open || Instance.closedFrame == Time.frameCount);

        public bool Open { get; private set; }
        public Tab Current { get; private set; }
        public int SelectedCharm => charmList.Selected;
        public int SelectedAbility => abilityList.Selected;
        public int SelectedProvision => satchelList.Selected;
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
        public string AbilityHow => abilityKeys.text + "\n" + abilityPad.text + "\n" + string.Join("\n", System.Array.ConvertAll(abilityUses, u => u.text));
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
        RawImage oarView;
        RenderTexture oarTexture;
        Camera oarCamera;
        Transform oarPivot;
        Text oarName, oarDescription, oarNext;
        Image[] pips;
        Image[] slotMarks;
        ScrollRows charmList, abilityList, satchelList;
        readonly List<string> charmRowText = new List<string>(), abilityRowText = new List<string>(), satchelRowText = new List<string>();
        readonly List<Provision> satchel = new List<Provision>();
        Text satchelName, satchelDescription, satchelEffect, satchelBeltLine, satchelNone;
        Graphic[] satchelDetail;
        (Image frame, Image icon)[] satchelBelt;
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
                tabs[i] = UiKit.Text(root, "Tab", titleFont, 32, UiKit.Ash, TextAnchor.MiddleCenter, centre, new Vector2((i - (Tabs - 1) / 2f) * 300f, 400), 2f);
            tabLeft = Diamond(root, UiKit.Ember, 10f);
            tabRight = Diamond(root, UiKit.Ember, 10f);
            UiKit.Image(root, "Rule", UiKit.Rule, UiKit.Bone.WithAlpha(0.55f), centre, new Vector2(0, 360), new Vector2(1600, 3));
            var hintBand = UiKit.Image(root, "Hint Band", UiKit.Band, Color.black.WithAlpha(0.55f), new Vector2(0.5f, 0f), new Vector2(0, 70), new Vector2(1500, 70));
            var hints = UiKit.Text(root, "Hints", textFont, 26, UiKit.Bone.WithAlpha(0.75f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0, 70), 1f);
            hints.text = "Left / Right: page     Up / Down: choose     E / A: on the belt     I / Select, Esc / B: close";

            for (int i = 0; i < Tabs; i++)
                pages[i] = UiKit.Stretch(UiKit.Rect(root, TabNames[i], centre, Vector2.zero, Vector2.zero));
            BuildApprentice(pages[(int)Tab.Apprentice]);
            BuildOar(pages[(int)Tab.Oar]);
            BuildAbilities(pages[(int)Tab.Abilities]);
            BuildSatchel(pages[(int)Tab.Satchel]);
            BuildCharms(pages[(int)Tab.Charms]);
            BuildLantern(pages[(int)Tab.Lantern]);
            root.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (oarTexture != null)
                oarTexture.Release();
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
            BuildOarStage();
            oarView.texture = oarTexture;
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

        // Where the oar turns: far below the world, lit by two lamps of its own (warm and cold, for the metal's glints),
        // seen by a camera that draws only while the page is open.
        void BuildOarStage()
        {
            oarTexture = new RenderTexture(768, 768, 24, RenderTextureFormat.ARGB32) { name = "Oar Page", antiAliasing = 4 };
            var stage = new GameObject("Oar Stage").transform;
            stage.SetParent(transform, false);
            stage.position = new Vector3(0f, -400f, 0f);
            oarPivot = new GameObject("Oar Pivot").transform;
            oarPivot.SetParent(stage, false);
            if (oarModels != null)
                foreach (var model in oarModels)
                    if (model != null)
                    {
                        model.transform.SetParent(oarPivot, false);
                        model.transform.localPosition = Vector3.down * 1.15f; // (its middle at the pivot)
                        model.transform.localRotation = Quaternion.identity;
                        model.SetActive(false);
                    }
            oarCamera = new GameObject("Oar Camera").AddComponent<Camera>();
            oarCamera.transform.SetParent(stage, false);
            oarCamera.transform.localPosition = new Vector3(0f, 0f, -3.9f);
            oarCamera.fieldOfView = 42f;
            oarCamera.nearClipPlane = 0.5f;
            oarCamera.farClipPlane = 8f;
            oarCamera.clearFlags = CameraClearFlags.SolidColor;
            oarCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            oarCamera.targetTexture = oarTexture;
            oarCamera.enabled = false;
            Lamp(new Vector3(1.4f, 1.2f, -1.8f), new Color(1f, 0.85f, 0.65f), 2.6f);
            Lamp(new Vector3(-1.6f, -0.4f, -1.4f), Cold, 2.5f);
            void Lamp(Vector3 at, Color colour, float intensity)
            {
                var lamp = new GameObject("Oar Lamp").AddComponent<Light>();
                lamp.transform.SetParent(stage, false);
                lamp.transform.localPosition = at;
                lamp.type = LightType.Point;
                lamp.range = 6f;
                lamp.color = colour;
                lamp.intensity = intensity;
                lamp.shadows = LightShadows.None;
            }
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

        void BuildSatchel(RectTransform page)
        {
            var centre = new Vector2(0.5f, 0.5f);
            satchelList = new ScrollRows(page, textFont, -470f, 280f, 520f, 9);
            satchelNone = UiKit.Text(page, "None", textFont, 32, UiKit.Ash.WithAlpha(0.7f), TextAnchor.MiddleCenter, centre, new Vector2(0, 40), 1f);
            satchelNone.text = "Empty. The merchant soul would love to help.";
            const float x = 290f;
            Text Line(string name, Font font, int size, Color colour, float y, float height)
            {
                var t = UiKit.Text(page, name, font, size, colour, TextAnchor.UpperLeft, centre, new Vector2(x, y));
                t.horizontalOverflow = HorizontalWrapMode.Wrap;
                t.rectTransform.pivot = new Vector2(0.5f, 1f);
                t.rectTransform.sizeDelta = new Vector2(880, height);
                return t;
            }
            satchelName = Line("Name", titleFont, 48, UiKit.Bone, 300, 70);
            satchelDescription = Line("Description", textFont, 30, UiKit.Ash, 225, 90);
            satchelEffect = Line("Effect", textFont, 30, UiKit.Bone, 130, 130);
            satchelBeltLine = Line("Belt", textFont, 28, UiKit.Ember, -20, 40);
            // The belt, as the d-pad lies: what's on each slot.
            satchelBelt = new (Image, Image)[Provision.BeltSlots];
            var offsets = new[] { new Vector2(0, 58), new Vector2(58, 0), new Vector2(0, -58), new Vector2(-58, 0) };
            for (int i = 0; i < satchelBelt.Length; i++)
            {
                var at = new Vector2(x - 330f, -170f) + offsets[i];
                var frame = UiKit.Image(page, "Slot", UiKit.KeyCap, UiKit.Bronze, centre, at, new Vector2(58, 58));
                frame.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
                var icon = UiKit.Image(page, "Icon", null, Color.white, centre, at, new Vector2(34, 34));
                satchelBelt[i] = (frame, icon);
            }
            satchelDetail = new Graphic[] { satchelName, satchelDescription, satchelEffect, satchelBeltLine };
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
            Sfx.Instance?.UiTick();
            Draw();
        }

        public void ChooseCharm(int by) => Choose(charmList, by);
        public void ChooseProvision(int by) => Choose(satchelList, by);

        // The chosen Provision onto the next belt slot round (up, right, down, left, then off the belt).
        public void MoveOnBelt()
        {
            if (Current != Tab.Satchel || satchel.Count == 0)
                return;
            var p = satchel[satchelList.Selected];
            var e = apprentice.Equipment;
            int at = e.BeltSlotOf(p);
            if (at >= 0)
                e.PutOnBelt(at, null);
            int next = at + 1;
            if (next < Provision.BeltSlots)
                e.PutOnBelt(next, p);
            Sfx.Instance?.UiTick();
            Draw();
        }
        public void ChooseAbility(int by) => Choose(abilityList, by);

        void Choose(ScrollRows list, int by)
        {
            if (!list.Move(by))
                return;
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
                else if (step.y != 0 && Current == Tab.Abilities)
                    ChooseAbility(-step.y);
                else if (step.y != 0 && Current == Tab.Satchel)
                    ChooseProvision(-step.y);
                else if (input.Confirm && Current == Tab.Satchel)
                    MoveOnBelt();
            }
            if (group == null)
                return;
            group.alpha = Mathf.MoveTowards(group.alpha, Open ? 1f : 0f, Time.unscaledDeltaTime * 6f);
            group.gameObject.SetActive(group.alpha > 0.001f || Open);
            if (oarCamera != null)
                oarCamera.enabled = group.alpha > 0.001f && Current == Tab.Oar;
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
            switch (Current)
            {
                case Tab.Apprentice: DrawApprentice(); break;
                case Tab.Oar: DrawOar(); break;
                case Tab.Abilities: DrawAbilities(); break;
                case Tab.Satchel: DrawSatchel(); break;
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
            if (oarModels != null)
                for (int i = 0; i < oarModels.Length; i++)
                    if (oarModels[i] != null && oarModels[i].activeSelf != (i == step))
                        oarModels[i].SetActive(i == step);
            // Leaning, blade up and to the right, turning slowly about its length.
            oarPivot.localRotation = Quaternion.Euler(0f, 0f, -38f) * Quaternion.Euler(0f, UiKit.Now * 40f, 0f);
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
            abilityKeys.text = chosen != null ? $"Keyboard:  {chosen.Keys}" : "";
            abilityPad.text = chosen != null ? $"Gamepad:  {chosen.Pad}" : "";
            for (int i = 0; i < abilityUses.Length; i++)
            {
                bool on = chosen != null && i < chosen.Uses.Length;
                abilityUses[i].enabled = on;
                abilityUses[i].text = on ? "·  " + chosen.Uses[i] : "";
            }
        }

        // Every Provision carried (or on the belt, used up), how many of its cap; the chosen one beside them, and the belt.
        void DrawSatchel()
        {
            var e = apprentice.Equipment;
            satchel.Clear();
            satchelRowText.Clear();
            var rows = new List<ScrollRows.Item>();
            foreach (var p in Provision.All)
                if (e.Carried(p) > 0 || e.BeltSlotOf(p) >= 0)
                {
                    satchel.Add(p);
                    satchelRowText.Add(p.Name);
                    int slot = e.BeltSlotOf(p);
                    rows.Add(new ScrollRows.Item { Label = p.Name, Value = $"{e.Carried(p)} / {p.Cap}" + (slot >= 0 ? $"   {Provision.SlotNames[slot]}" : ""), Dim = e.Carried(p) == 0 });
                }
            satchelList.Set(rows);
            bool any = satchel.Count > 0;
            satchelNone.enabled = !any;
            foreach (var g in satchelDetail)
                g.enabled = any;
            var chosen = any ? satchel[satchelList.Selected] : null;
            satchelName.text = chosen?.Name ?? "";
            satchelDescription.text = chosen?.Description ?? "";
            satchelEffect.text = chosen?.Effect ?? "";
            int at = chosen != null ? e.BeltSlotOf(chosen) : -1;
            satchelBeltLine.text = chosen == null ? "" : at >= 0
                ? $"On the belt: {Provision.SlotNames[at]} (key {at + 1}).   E / A: move it round"
                : "Not on the belt.   E / A: put it on";
            for (int i = 0; i < satchelBelt.Length; i++)
            {
                var p = e.OnBelt(i);
                satchelBelt[i].icon.enabled = p != null && any;
                satchelBelt[i].frame.enabled = any;
                if (p != null)
                {
                    satchelBelt[i].icon.sprite = Hud.IconOf(p);
                    satchelBelt[i].icon.color = Hud.ColourOf(p).WithAlpha(e.Carried(p) > 0 ? 1f : 0.3f);
                }
                satchelBelt[i].frame.color = p != null && p == chosen ? UiKit.Ember : UiKit.Bronze;
            }
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

            public void Set(IReadOnlyList<Item> items)
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
                    band.enabled = on && item == Selected;
                    if (!on)
                        continue;
                    label.text = items[item].Label;
                    label.color = items[item].Dim ? UiKit.Ash.WithAlpha(0.35f) : item == Selected ? UiKit.Bone : UiKit.Ash;
                    value.text = items[item].Value ?? "";
                }
                up.gameObject.SetActive(first > 0);
                down.gameObject.SetActive(first + rows.Length < count);
            }
        }
    }
}
