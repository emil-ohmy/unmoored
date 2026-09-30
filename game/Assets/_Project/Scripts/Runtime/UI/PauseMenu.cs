using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MyGame
{
    // Esc (Start on a gamepad) pauses: the world stops and darkens behind a list. Resume; Controls (every action's key
    // and button); Settings (master, music and effects volume: left/right); Quit to Title and Quit Game (saving first,
    // when a slot is being played). Back (Esc / B) goes up a page, and from the top resumes.
    public sealed class PauseMenu : MonoBehaviour
    {
        public Font titleFont, textFont;
        public string titleScene = "Title";

        enum Page { Main, Controls, Settings }

        public bool Open { get; private set; }
        public string PageName => page.ToString();
        public int Selected => selected;
        public IReadOnlyList<ScreenList.Row> Rows => list.Shown;

        MenuInput input;
        ScreenList list;
        Apprentice apprentice;
        Image shade;
        Page page;
        int selected;
        readonly List<ScreenList.Row> rows = new List<ScreenList.Row>();

        // (keyboard, gamepad) for each action, as GameInput binds them.
        public static readonly (string action, string keys, string pad)[] Controls =
        {
            ("Move / Row", "W A S D", "Left stick"),
            ("Dash", "Space", "B"),
            ("Light attack", "J / Left click", "X"),
            ("Heavy attack", "K / Right click", "Y / RT"),
            ("Block (on time: perfect)", "L", "LT"),
            ("Heal", "Q", "LB"),
            ("Interact / Rest / Row", "E", "A"),
            ("Lock on / switch target", "Tab / Middle click, Scroll", "R3, Right stick"),
            ("Hook: hold to aim, let go", "F", "RB"),
            ("The belt: choose / use", "Left, Right / Up arrow", "D-pad left, right / up"),
            ("The Apprentice (character)", "I / C", "Select"),
            ("Pause", "Esc", "Start"),
        };

        void Start()
        {
            apprentice = FindAnyObjectByType<Apprentice>();
            input = new MenuInput();
            var canvasGo = new GameObject("Pause Canvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50; // over the HUD
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            shade = UiKit.Image(canvasGo.transform, "Shade", null, Color.black.WithAlpha(0f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            UiKit.Stretch(shade.rectTransform, -4f);
            list = new ScreenList(canvasGo.transform, titleFont, textFont);
        }

        void OnDestroy()
        {
            input?.Dispose();
            if (Open)
                Pause.Set(false);
        }

        void Update()
        {
            InputDevice.Track();
            input.Tick();
            if (!Open)
            {
                if (input.PausePressed && !CharacterScreen.Blocks)
                    PausePressed();
            }
            else
                Navigate();
            shade.color = Color.black.WithAlpha(Mathf.MoveTowards(shade.color.a, Open ? 0.7f : 0f, Time.unscaledDeltaTime * 4f));
            shade.enabled = shade.color.a > 0.001f;
            list.Tick();
        }

        // Esc / Start: pauses, unless a menu in the world is open (a Mooring post's, the merchant's): then it leaves that.
        public void PausePressed()
        {
            if (apprentice != null && apprentice.OpenMenu != null)
            {
                apprentice.OpenMenu.Close();
                Sfx.Instance?.UiTick();
            }
            else if (!SceneFader.Busy)
                Toggle(true);
        }

        public void Toggle(bool open)
        {
            if (open == Open)
                return;
            Open = open;
            Pause.Set(open);
            page = Page.Main;
            selected = 0;
            list.Show(open);
            if (open)
                Draw();
        }

        void Navigate()
        {
            if (input.PausePressed && page == Page.Main || input.Back && page == Page.Main)
            {
                Toggle(false);
                return;
            }
            if (input.Back)
            {
                Go(Page.Main, page == Page.Controls ? 1 : 2);
                return;
            }
            var step = input.Step;
            if (step.y != 0 && Count > 0)
            {
                selected = (selected - step.y + Count) % Count;
                Sfx.Instance?.UiTick();
            }
            if (page == Page.Settings && step.x != 0 && selected < 3)
            {
                Nudge(selected, step.x * 0.1f);
                Sfx.Instance?.UiTick();
            }
            if (input.Confirm)
                Choose();
            Draw();
        }

        int Count => page == Page.Main ? 5 : page == Page.Controls ? 1 : 4;

        void Choose()
        {
            switch (page)
            {
                case Page.Main:
                    switch (selected)
                    {
                        case 0: Toggle(false); return;
                        case 1: Go(Page.Controls, 0); return;
                        case 2: Go(Page.Settings, 0); return;
                        case 3: QuitToTitle(); return;
                        case 4: QuitGame(); return;
                    }
                    break;
                case Page.Controls:
                    Go(Page.Main, 1);
                    break;
                case Page.Settings:
                    if (selected == 3)
                        Go(Page.Main, 2);
                    break;
            }
        }

        void Go(Page to, int select)
        {
            page = to;
            selected = select;
            Sfx.Instance?.UiTick();
            Draw();
        }

        static void Nudge(int which, float by)
        {
            if (which == 0) Settings.Master += by;
            else if (which == 1) Settings.Music += by;
            else Settings.Effects += by;
        }

        public static string Level(float v) => $"«   {Mathf.RoundToInt(v * 10f)}   »";

        void Draw()
        {
            rows.Clear();
            bool pad = InputDevice.Gamepad; // (only the controls in use)
            string detail = "", hint = pad ? "Up / Down: choose     A: confirm     B: back" : "Up / Down: choose     E: confirm     Esc: back";
            switch (page)
            {
                case Page.Main:
                    foreach (var label in new[] { "Resume", "Controls", "Settings", "Quit to Title", "Quit Game" })
                        rows.Add(new ScreenList.Row { Label = label });
                    detail = SaveSlots.Current >= 0 ? "Your journey is kept as you go." : "(Playing from the editor: nothing is saved.)";
                    list.Set("Paused", rows, selected, detail, hint);
                    return;
                case Page.Controls:
                    foreach (var (action, keys, button) in Controls)
                        rows.Add(new ScreenList.Row { Label = action, Value = pad ? button : keys });
                    list.Set(pad ? "Controls (gamepad)" : "Controls (keyboard)", rows, -1, "", pad ? "B: back" : "Esc: back");
                    return;
                default:
                    rows.Add(new ScreenList.Row { Label = "Master volume", Value = Level(Settings.Master) });
                    rows.Add(new ScreenList.Row { Label = "Music", Value = Level(Settings.Music) });
                    rows.Add(new ScreenList.Row { Label = "Effects", Value = Level(Settings.Effects) });
                    rows.Add(new ScreenList.Row { Label = "Back" });
                    list.Set("Settings", rows, selected, "", pad ? "Left / Right: change     B: back" : "Left / Right: change     Esc: back");
                    return;
            }
        }

        void QuitToTitle()
        {
            SaveSystem.Instance?.SaveNow();
            Toggle(false);
            SaveSlots.Current = -1;
            SceneFader.Load(titleScene);
        }

        void QuitGame()
        {
            SaveSystem.Instance?.SaveNow();
            Toggle(false);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
