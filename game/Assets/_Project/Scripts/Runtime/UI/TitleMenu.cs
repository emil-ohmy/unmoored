using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MyGame
{
    // The title screen: the game's name over the dark water, then Continue (the slot played last), Play (the three
    // save slots: continue one, begin a new game in an empty one, or delete one, asked twice), Settings and Quit.
    public sealed class TitleMenu : MonoBehaviour
    {
        public Font titleFont, textFont;
        public string gameScene = "Main";

        enum Page { Main, Slots, Delete, Settings }

        public string PageName => page.ToString();
        public int Selected => selected;
        public IReadOnlyList<ScreenList.Row> Rows => list.Shown;
        public string LoadingScene { get; private set; } // (set as it leaves: tests read it)

        MenuInput input;
        ScreenList list;
        Text title, subtitle;
        Page page;
        int selected, deleting;
        float shownAt;
        readonly List<ScreenList.Row> rows = new List<ScreenList.Row>();
        readonly List<System.Action> actions = new List<System.Action>();

        void Start()
        {
            Pause.Set(false);
            SaveSlots.Current = -1;
            input = new MenuInput();
            var canvasGo = new GameObject("Title Canvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var vignette = UiKit.Image(canvasGo.transform, "Vignette", UiKit.Vignette, Color.black.WithAlpha(0.8f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            UiKit.Stretch(vignette.rectTransform, -4f);
            var centre = new Vector2(0.5f, 0.5f);
            title = UiKit.Text(canvasGo.transform, "Title", titleFont, 104, UiKit.Bone, TextAnchor.MiddleCenter, centre, new Vector2(0, 300), 8f);
            title.rectTransform.sizeDelta = new Vector2(1800, 170);
            title.text = "The Ferryman's Apprentice";
            subtitle = UiKit.Text(canvasGo.transform, "Subtitle", textFont, 34, UiKit.Ash, TextAnchor.MiddleCenter, centre, new Vector2(0, 222), 2f);
            subtitle.rectTransform.sizeDelta = new Vector2(1400, 50);
            subtitle.text = "The dead have stopped crossing.";
            list = new ScreenList(canvasGo.transform, titleFont, textFont, 40f, 110f);
            list.Show(true);
            shownAt = Time.unscaledTime;
            Go(Page.Main, 0);
        }

        void OnDestroy() => input?.Dispose();

        void Update()
        {
            input.Tick();
            float age = Time.unscaledTime - shownAt; // the name fades up out of the dark first
            title.color = UiKit.Bone.WithAlpha(UiKit.Smooth(0.2f, 2.0f, age));
            subtitle.color = UiKit.Ash.WithAlpha(UiKit.Smooth(1.2f, 2.8f, age));
            list.Tick();
            if (LoadingScene != null || age < 1.0f)
                return;
            var step = input.Step;
            if (step.y != 0 && rows.Count > 0)
            {
                selected = (selected - step.y + rows.Count) % rows.Count;
                Sfx.Instance?.UiTick();
                Draw();
            }
            if (page == Page.Settings && step.x != 0 && selected < 3)
            {
                if (selected == 0) Settings.Master += step.x * 0.1f;
                else if (selected == 1) Settings.Music += step.x * 0.1f;
                else Settings.Effects += step.x * 0.1f;
                Sfx.Instance?.UiTick();
                Draw();
            }
            if (input.Confirm && selected < actions.Count)
                actions[selected]();
            else if (input.Delete && page == Page.Slots && selected < SaveSlots.Count && SaveSlots.Exists(selected))
                AskDelete(selected);
            else if (input.Back && page != Page.Main)
                Go(page == Page.Delete ? Page.Slots : Page.Main, page == Page.Delete ? deleting : 0);
        }

        // ---- the pages

        public void Choose(int index) // (tests: as if confirmed on this row)
        {
            selected = index;
            if (index < actions.Count)
                actions[index]();
        }

        void Go(Page to, int select)
        {
            page = to;
            selected = select;
            Draw();
        }

        void Draw()
        {
            rows.Clear();
            actions.Clear();
            string detail = "", hint = "Up / Down: choose     E / A: confirm     Esc / B: back";
            string heading = "";
            switch (page)
            {
                case Page.Main:
                    int recent = SaveSlots.MostRecent();
                    if (recent >= 0)
                        Add("Continue", null, () => Play(recent));
                    Add("Play", null, () => Go(Page.Slots, 0));
                    Add("Settings", null, () => Go(Page.Settings, 0));
                    Add("Quit", null, Quit);
                    hint = "Up / Down: choose     E / A: confirm";
                    break;
                case Page.Slots:
                    heading = "Choose a Journey";
                    for (int i = 0; i < SaveSlots.Count; i++)
                    {
                        int slot = i;
                        var d = SaveSlots.Load(i);
                        Add(Numeral(i) + (d != null ? "   " + d.area : "   An empty journey"), d != null ? Summary(d) : "New game", () => Play(slot));
                    }
                    Add("Back", null, () => Go(Page.Main, 0));
                    hint = "E / A: begin or continue     X / Delete: delete     Esc / B: back";
                    break;
                case Page.Delete:
                    heading = $"Delete Journey {Numeral(deleting)}?";
                    Add("Keep it", null, () => Go(Page.Slots, deleting));
                    Add("Delete it, for good", null, () => { SaveSlots.Delete(deleting); Go(Page.Slots, deleting); });
                    detail = "It can't be brought back.";
                    break;
                default:
                    heading = "Settings";
                    Add("Master volume", PauseMenu.Level(Settings.Master), null);
                    Add("Music", PauseMenu.Level(Settings.Music), null);
                    Add("Effects", PauseMenu.Level(Settings.Effects), null);
                    Add("Back", null, () => Go(Page.Main, 0));
                    hint = "Left / Right: change     Esc / B: back";
                    break;
            }
            list.Set(heading, rows, selected, detail, hint);
        }

        void Add(string label, string value, System.Action action)
        {
            rows.Add(new ScreenList.Row { Label = label, Value = value });
            actions.Add(action ?? (() => { }));
        }

        static string Numeral(int i) => i == 0 ? "I." : i == 1 ? "II." : "III.";

        static string Summary(SaveData d) =>
            $"{SaveSlots.PlayTime(d.playTime)}   ·   {d.obols} Obols" + (d.beaten.Length > 0 ? $"   ·   {d.beaten.Length} beaten" : "");

        void AskDelete(int slot)
        {
            deleting = slot;
            Go(Page.Delete, 0);
            Sfx.Instance?.UiTick();
        }

        void Play(int slot)
        {
            if (SceneFader.Busy)
                return;
            SaveSlots.Current = slot;
            LoadingScene = gameScene;
            Sfx.Instance?.UiTick();
            SceneFader.Load(gameScene); // a fade, a moment's loading, and the village fades in
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
