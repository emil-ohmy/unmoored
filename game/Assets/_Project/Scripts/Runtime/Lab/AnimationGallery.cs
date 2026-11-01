using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MyGame
{
    // The animation gallery (Lab_Gallery): every rigged creature on its own plinth in a row, nothing to fight, only how
    // they move. One is chosen (the camera on it, from the game's angle, turnable); it plays
    //   Chain     its clips one after another as a fight chains them (idle, walking up, the guard, each attack, back),
    //             with the game's own crossfades, slowly enough to see the joins: a timeline below shows each step and,
    //             at every join, how far the pose jumps there (the bone that turns most, in degrees);
    //   Clips     one clip at a time at its own speed (the motion as made), a one-off held a beat at its end;
    //   Attacks   its attacks as the game times them: wind-up, strike, recovery, each clip stretched to the attack's own
    //             duration (as ClipPlayer plays them in a fight), a breath of idling between;
    //   Everyone  every creature at once, each going through all its clips in turn.
    // The rest idle meanwhile. Slow it down (a half, a quarter, a tenth), pause it, step it a frame (30 a second) at a
    // time, and show the bones (over the body, or the bones alone). The time is the gallery's own: clips are sampled at
    // its playheads, so pausing and stepping are exact.
    public sealed class AnimationGallery : MonoBehaviour
    {
        public enum Mode { Chain, Clips, Attacks, Everyone }

        public Font font, titleFont;
        public Material boneMaterial;
        public float pitch = 48f, fieldOfView = 30f;
        public float breath = 0.8f;                  // (attacks: idling between one and the next)
        public const float Fps = 30f;                 // (the clips' own frame rate)
        public static readonly float[] Speeds = { 1f, 0.5f, 0.25f, 0.1f };

        public GalleryExhibit[] Exhibits { get; private set; }
        public int Selected { get; private set; }
        public int Item { get; private set; }         // the clip (Clips) or the attack (Attacks) chosen
        public Mode Current { get; private set; } = Mode.Chain;
        public int Step { get; private set; }         // (Chain: the step playing now)
        public float hold = 0.7f;                      // (Clips: a one-off held at its end this long)
        public bool Paused { get; private set; }
        public int SpeedIndex { get; private set; }
        public float Speed => Speeds[SpeedIndex];
        public int BonesShown { get; private set; }   // 0 none, 1 over the body, 2 alone
        public float Playhead => heads.Count > Selected ? heads[Selected] : 0f; // (the chosen one's time, seconds)
        public string NowPlaying { get; private set; } = ""; // (the chosen one: the clip sampled now)
        public float NowAt { get; private set; }      // ...and where in it (seconds into the clip, as authored)

        readonly List<float> heads = new List<float>();
        readonly List<LineRenderer> lines = new List<LineRenderer>();
        Camera cam;
        float yaw, zoom = 1f;
        RectTransform timeline, playhead;
        GalleryExhibit timelineOf;
        Mode timelineMode;
        Vector3 focus;
        Text panel, help;

        void Start()
        {
            var found = FindObjectsByType<GalleryExhibit>(FindObjectsSortMode.None);
            System.Array.Sort(found, (a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
            Exhibits = found;
            foreach (var e in Exhibits)
            {
                heads.Add(0f);
                if (e.Animation != null)
                {
                    e.Animation.Stop();
                    e.Animation.playAutomatically = false;
                }
            }
            cam = Camera.main;
            if (cam != null)
                cam.fieldOfView = fieldOfView;
            BuildPanel();
            if (Exhibits.Length > 0)
                focus = Focus(Exhibits[0]);
        }

        // ---- choosing (the controls call these; so do the tests)

        public void Select(int index)
        {
            if (Exhibits.Length == 0)
                return;
            Selected = (index % Exhibits.Length + Exhibits.Length) % Exhibits.Length;
            Item = 0;
            focus = Focus(Exhibits[Selected]); // (straight to it)
            Restart();
        }

        public void SelectItem(int index)
        {
            int count = Items;
            if (count == 0)
                return;
            Item = (index % count + count) % count;
            Restart();
        }

        public void SetMode(Mode mode)
        {
            Current = mode;
            Item = 0;
            for (int i = 0; i < heads.Count; i++)
                heads[i] = 0f;
        }

        public void TogglePause() => Paused = !Paused;
        public float Yaw { get => yaw; set => yaw = value; }   // (the camera turned round the chosen one: 90 its side)
        public float Zoom { get => zoom; set => zoom = value; }
        public void NextSpeed() => SpeedIndex = (SpeedIndex + 1) % Speeds.Length;
        public void SetSpeed(int index) => SpeedIndex = Mathf.Clamp(index, 0, Speeds.Length - 1);
        public void NextBones() => BonesShown = (BonesShown + 1) % 3;

        // A frame on (or back), paused.
        public void StepFrames(int frames)
        {
            Paused = true;
            for (int i = 0; i < heads.Count; i++)
                heads[i] = Mathf.Max(0f, heads[i] + frames / Fps);
        }

        void Restart()
        {
            if (Selected < heads.Count)
                heads[Selected] = 0f;
        }

        GalleryExhibit Chosen => Exhibits.Length > 0 ? Exhibits[Selected] : null;

        // How many there are to choose between, in this mode, for the chosen one.
        public int Items => Chosen == null ? 0 : Current == Mode.Attacks ? Chosen.attacks.Length : Current == Mode.Chain ? 1 : Chosen.Clips.Length;

        // ---- every frame

        void Update()
        {
            ReadInput();
            float dt = Paused ? 0f : Time.deltaTime * Speed;
            for (int i = 0; i < Exhibits.Length; i++)
            {
                heads[i] += dt;
                Pose(i);
            }
            Frame();
            Bones();
            Write();
            Timeline();
        }

        // Each one's pose at its playhead.
        void Pose(int i)
        {
            var e = Exhibits[i];
            if (e.Animation == null)
                return;
            float t = heads[i];
            bool chosen = i == Selected;
            if (Current == Mode.Everyone)
            {
                // Every clip in turn, each at least a second and a half (a short one played over again).
                var clips = e.Clips;
                float total = 0f;
                foreach (var c in clips)
                    total += Hold(e, c);
                float at = total > 0f ? Mathf.Repeat(t, total) : 0f;
                foreach (var c in clips)
                {
                    float hold = Hold(e, c);
                    if (at < hold)
                    {
                        var s = e.State(c);
                        Sample(e, s, Mathf.Repeat(at, s.length), null, 0f, 0f, chosen);
                        break;
                    }
                    at -= hold;
                }
                return;
            }
            if (!chosen)
            {
                var idle = e.State(e.Clips.Length > 0 && System.Array.IndexOf(e.Clips, "Idle") >= 0 ? "Idle" : e.Clips.Length > 0 ? e.Clips[0] : null);
                if (idle != null)
                    Sample(e, idle, Mathf.Repeat(t, idle.length), null, 0f, 0f, false);
                return;
            }
            if (Current == Mode.Chain)
            {
                Chain(e, t);
                return;
            }
            if (Current == Mode.Clips)
            {
                string name = e.Clips.Length > 0 ? e.Clips[Mathf.Clamp(Item, 0, e.Clips.Length - 1)] : null;
                var s = e.State(name);
                if (s == null)
                    return;
                float length = Mathf.Max(0.01f, s.length);
                if (Loops(e, name))
                    Sample(e, s, Mathf.Repeat(t, length), null, 0f, 0f, true);
                else
                    Sample(e, s, Mathf.Min(Mathf.Repeat(t, length + hold), length), null, 0f, 0f, true); // (then held a beat)
                return;
            }
            // Attacks: wind-up, strike, recovery (each clip stretched over its time, as a fight plays it), then idling.
            if (e.attacks.Length == 0)
            {
                var idle = e.State(e.Clips.Length > 0 ? e.Clips[0] : null);
                if (idle != null)
                    Sample(e, idle, Mathf.Repeat(t, idle.length), null, 0f, 0f, true);
                return;
            }
            var a = e.attacks[Mathf.Clamp(Item, 0, e.attacks.Length - 1)];
            var phases = new[] { (a.windup, a.telegraph), (a.strike, a.strikeTime), (a.recover, a.recoverTime), ("Idle", breath) };
            float sum = 0f;
            foreach (var p in phases)
                sum += Mathf.Max(0.01f, p.Item2);
            float u = Mathf.Repeat(t, sum);
            AnimationState previous = null;
            float previousAt = 0f;
            foreach (var (clip, duration) in phases)
            {
                float d = Mathf.Max(0.01f, duration);
                var s = e.State(clip) ?? e.State(e.Clips.Length > 0 ? e.Clips[0] : null);
                if (u < d)
                {
                    if (s == null)
                        return;
                    bool loops = clip == "Idle";
                    float at = loops ? Mathf.Repeat(u, s.length) : u / d * s.length; // (stretched over its time)
                    float fade = previous != null ? Mathf.Clamp01(u / 0.05f) : 1f;    // (ClipPlayer's crossfade)
                    Sample(e, s, at, previous, previousAt, 1f - fade, true);
                    return;
                }
                u -= d;
                previous = s;
                previousAt = s != null ? s.length : 0f;
            }
        }

        // Its chain at `t`: the step playing, its clip where the fight would have it, crossfaded in from the step before.
        void Chain(GalleryExhibit e, float t)
        {
            var steps = e.chain;
            if (steps.Length == 0)
                return;
            float total = ChainLength(e);
            float u = Mathf.Repeat(t, total);
            for (int i = 0; i < steps.Length; i++)
            {
                float d = Mathf.Max(0.01f, steps[i].duration);
                if (u >= d && i < steps.Length - 1)
                {
                    u -= d;
                    continue;
                }
                Step = i;
                var s = e.State(steps[i].clip);
                if (s == null)
                    return;
                AnimationState from = null;
                float fromAt = 0f, fromWeight = 0f;
                if (i > 0 && u < steps[i].fade)
                {
                    from = e.State(steps[i - 1].clip);
                    if (from != null)
                    {
                        fromAt = StepTime(steps[i - 1], steps[i - 1].duration + u, from);
                        fromWeight = 1f - u / steps[i].fade;
                    }
                }
                Sample(e, s, StepTime(steps[i], u, s), from, fromAt, fromWeight, true);
                return;
            }
        }

        public static float ChainLength(GalleryExhibit e)
        {
            float total = 0f;
            foreach (var step in e.chain)
                total += Mathf.Max(0.01f, step.duration);
            return total;
        }

        // Where in its clip a step is, `local` seconds in: a loop at its own speed; a one-off stretched over its time,
        // held on its last frame after (as ClipPlayer's ClampForever).
        static float StepTime(GalleryExhibit.Step step, float local, AnimationState state) =>
            step.loop ? Mathf.Repeat(local, Mathf.Max(0.01f, state.length)) : Mathf.Clamp01(local / Mathf.Max(0.01f, step.duration)) * state.length;

        // Each join in its chain: how far the pose jumps (the step before, at its end, to this one at its start).
        public (float degrees, string bone, float shift)[] Seams(GalleryExhibit e)
        {
            if (seams.TryGetValue(e, out var found))
                return found;
            var steps = e.chain;
            var result = new (float, string, float)[steps.Length];
            for (int i = 1; i < steps.Length; i++)
            {
                var from = e.State(steps[i - 1].clip);
                var to = e.State(steps[i].clip);
                if (from == null || to == null)
                    continue;
                result[i] = e.Jump(steps[i - 1].clip, StepTime(steps[i - 1], steps[i - 1].duration, from), steps[i].clip, 0f);
            }
            seams[e] = result;
            return result;
        }

        readonly Dictionary<GalleryExhibit, (float, string, float)[]> seams = new Dictionary<GalleryExhibit, (float, string, float)[]>();
        readonly Dictionary<(GalleryExhibit, string), bool> loops = new Dictionary<(GalleryExhibit, string), bool>();

        bool Loops(GalleryExhibit e, string clip)
        {
            if (!loops.TryGetValue((e, clip), out bool loop))
                loops[(e, clip)] = loop = e.Loops(clip);
            return loop;
        }

        static float Hold(GalleryExhibit e, string clip)
        {
            var s = e.State(clip);
            if (s == null)
                return 0f;
            return s.length * Mathf.Max(1f, Mathf.Ceil(1.5f / Mathf.Max(0.05f, s.length)));
        }

        // Its pose: `state` at `at` (and, fading out, `from` at `fromAt` with `fromWeight`).
        void Sample(GalleryExhibit e, AnimationState state, float at, AnimationState from, float fromAt, float fromWeight, bool chosen)
        {
            var anim = e.Animation;
            foreach (AnimationState s in anim)
            {
                s.enabled = false;
                s.weight = 0f;
            }
            state.enabled = true;
            state.weight = 1f - fromWeight;
            state.time = at;
            if (from != null && from != state && fromWeight > 0f)
            {
                from.enabled = true;
                from.weight = fromWeight;
                from.time = fromAt;
            }
            anim.Sample();
            state.enabled = false;
            if (from != null)
                from.enabled = false;
            if (chosen)
            {
                NowPlaying = GalleryExhibit.Short(state.name);
                NowAt = at;
            }
        }

        // ---- the camera, on the chosen one, from the game's angle (turned, nearer, further at will)

        static Vector3 Focus(GalleryExhibit e) => e.transform.position + Vector3.up * Mathf.Min(e.height * 0.45f, 4f);

        void Frame()
        {
            if (cam == null || Chosen == null)
                return;
            var e = Chosen;
            focus = Focus(e);
            float distance = Mathf.Max(7f, e.height * 2.3f + e.radius * 1.2f) * zoom;
            var view = Quaternion.Euler(pitch, yaw, 0f);
            cam.transform.SetPositionAndRotation(focus - view * Vector3.forward * distance, view);
        }

        // ---- the bones of the chosen one: lines from each to its parent (over the body, or alone)

        void Bones()
        {
            var e = Chosen;
            var skin = e != null ? e.GetComponentInChildren<SkinnedMeshRenderer>() : null;
            foreach (var r in e != null ? e.GetComponentsInChildren<Renderer>() : new Renderer[0])
                if (!(r is LineRenderer))
                    r.enabled = BonesShown != 2;
            int used = 0;
            if (BonesShown > 0 && skin != null)
            {
                var bones = new HashSet<Transform>(skin.bones);
                foreach (var b in skin.bones)
                {
                    if (b == null || b.parent == null || !bones.Contains(b.parent))
                        continue;
                    var line = Line(used++);
                    line.SetPosition(0, b.parent.position);
                    line.SetPosition(1, b.position);
                    float w = Mathf.Clamp(e.height * 0.02f, 0.04f, 0.14f);
                    line.startWidth = w * 1.6f;
                    line.endWidth = w * 0.6f;
                }
            }
            for (int i = 0; i < lines.Count; i++)
                lines[i].enabled = i < used;
            // (and the rest, whose bodies may have been hidden: shown)
            foreach (var other in Exhibits)
                if (other != e)
                    foreach (var r in other.GetComponentsInChildren<Renderer>())
                        if (!(r is LineRenderer) && !r.enabled)
                            r.enabled = true;
        }

        LineRenderer Line(int index)
        {
            while (lines.Count <= index)
            {
                var go = new GameObject("Bone");
                go.transform.SetParent(transform, false);
                var line = go.AddComponent<LineRenderer>();
                line.positionCount = 2;
                line.useWorldSpace = true;
                line.sharedMaterial = boneMaterial;
                line.startColor = new Color(1f, 0.85f, 0.4f, 1f);
                line.endColor = new Color(1f, 0.5f, 0.2f, 1f);
                line.numCapVertices = 2;
                lines.Add(line);
            }
            return lines[index];
        }

        // ---- the controls

        void ReadInput()
        {
            var k = Keyboard.current;
            var g = Gamepad.current;
            var m = Mouse.current;
            bool Pressed(Key key) => k != null && k[key].wasPressedThisFrame;
            if (Pressed(Key.RightArrow) || Pressed(Key.D) || (g != null && g.dpad.right.wasPressedThisFrame))
                Select(Selected + 1);
            if (Pressed(Key.LeftArrow) || Pressed(Key.A) || (g != null && g.dpad.left.wasPressedThisFrame))
                Select(Selected - 1);
            if (Pressed(Key.DownArrow) || Pressed(Key.S) || (g != null && g.dpad.down.wasPressedThisFrame))
                SelectItem(Item + 1);
            if (Pressed(Key.UpArrow) || Pressed(Key.W) || (g != null && g.dpad.up.wasPressedThisFrame))
                SelectItem(Item - 1);
            if (Pressed(Key.Tab) || (g != null && g.buttonNorth.wasPressedThisFrame))
                SetMode((Mode)(((int)Current + 1) % 4));
            if (Pressed(Key.Space) || (g != null && g.buttonSouth.wasPressedThisFrame))
                TogglePause();
            if (Pressed(Key.F) || (g != null && g.buttonWest.wasPressedThisFrame))
                NextSpeed();
            if (Pressed(Key.B) || (g != null && g.buttonEast.wasPressedThisFrame))
                NextBones();
            if (Pressed(Key.C) || Pressed(Key.Period) || (g != null && g.rightShoulder.wasPressedThisFrame))
                StepFrames(1);
            if (Pressed(Key.Z) || Pressed(Key.Comma) || (g != null && g.leftShoulder.wasPressedThisFrame))
                StepFrames(-1);
            if (Pressed(Key.Home) || (g != null && g.rightStickButton.wasPressedThisFrame))
            {
                yaw = 0f;
                zoom = 1f;
            }
            float dt = Time.unscaledDeltaTime;
            float turn = (k != null && k[Key.Q].isPressed ? -1f : 0f) + (k != null && k[Key.E].isPressed ? 1f : 0f) + (g != null ? g.rightStick.x.ReadValue() : 0f);
            yaw += turn * 90f * dt;
            if (m != null && m.rightButton.isPressed)
                yaw += m.delta.x.ReadValue() * 0.3f;
            float scroll = m != null ? m.scroll.y.ReadValue() : 0f;
            float pull = (g != null ? -g.rightStick.y.ReadValue() : 0f) + (k != null && k[Key.PageDown].isPressed ? 1f : 0f) - (k != null && k[Key.PageUp].isPressed ? 1f : 0f);
            zoom = Mathf.Clamp(zoom * Mathf.Exp(-scroll * 0.0015f + pull * dt), 0.35f, 2.5f);
        }

        // ---- the panel

        void BuildPanel()
        {
            var canvasGo = new GameObject("Gallery Canvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var topLeft = new Vector2(0f, 1f);
            UiKit.Image(canvasGo.transform, "Band", UiKit.Band, Color.black.WithAlpha(0.55f), topLeft, new Vector2(420, -150), new Vector2(900, 300));
            panel = UiKit.Text(canvasGo.transform, "Panel", font, 26, UiKit.Bone.WithAlpha(0.95f), TextAnchor.UpperLeft, topLeft, new Vector2(840, -40));
            var bottom = new Vector2(0.5f, 0f);
            UiKit.Image(canvasGo.transform, "Band", UiKit.Band, Color.black.WithAlpha(0.45f), bottom, new Vector2(0, 50), new Vector2(1800, 70));
            help = UiKit.Text(canvasGo.transform, "Help", font, 21, UiKit.Bone.WithAlpha(0.75f), TextAnchor.MiddleCenter, bottom, new Vector2(0, 50));
            timeline = UiKit.Rect(canvasGo.transform, "Timeline", bottom, new Vector2(0, 150), new Vector2(TimelineWidth, 90));
            help.text = "←/→ creature   ↑/↓ clip or attack   Tab mode   Space pause   C/Z a frame on/back   F speed   B bones   Q/E turn   wheel near/far   Home reset"
                      + "\n(pad: d-pad, Y mode, A pause, RB/LB a frame, X speed, B bones, right stick turn and near/far)";
        }

        const float TimelineWidth = 1760f;

        // The chain's timeline (Chain mode): a bar per step (blue a loop, amber a one-off; its clip named), a mark at
        // every join coloured by how far the pose jumps there (green a little, yellow some, red a lot) and the degrees
        // over it, and where it's playing now.
        void Timeline()
        {
            if (timeline == null)
                return;
            var e = Chosen;
            bool show = Current == Mode.Chain && e != null && e.chain.Length > 0;
            timeline.gameObject.SetActive(show);
            if (!show)
                return;
            float total = ChainLength(e);
            if (timelineOf != e || timelineMode != Current)
            {
                timelineOf = e;
                timelineMode = Current;
                for (int i = timeline.childCount - 1; i >= 0; i--)
                    Destroy(timeline.GetChild(i).gameObject);
                var seamsHere = Seams(e);
                float x = -TimelineWidth / 2f;
                var left = new Vector2(0.5f, 0.5f);
                for (int i = 0; i < e.chain.Length; i++)
                {
                    var step = e.chain[i];
                    float w = Mathf.Max(0.01f, step.duration) / total * TimelineWidth;
                    var bar = UiKit.Image(timeline, "Step", null, (step.loop ? new Color(0.35f, 0.5f, 0.7f) : new Color(0.85f, 0.6f, 0.25f)).WithAlpha(0.85f),
                                          left, new Vector2(x + w / 2f, -12f), new Vector2(Mathf.Max(2f, w - 2f), 26f));
                    var label = UiKit.Text(timeline, "Clip", font, 15, Color.black, TextAnchor.MiddleCenter, left, new Vector2(x + w / 2f, -12f));
                    label.text = w > 50f ? step.clip : "";
                    if (i > 0)
                    {
                        float deg = seamsHere[i].degrees;
                        var colour = deg < 6f ? new Color(0.4f, 0.9f, 0.4f) : deg < 15f ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.3f, 0.25f);
                        UiKit.Image(timeline, "Join", null, colour, left, new Vector2(x, -4f), new Vector2(4f, 46f));
                        var mark = UiKit.Text(timeline, "Degrees", font, 16, colour, TextAnchor.MiddleCenter, left, new Vector2(x, 26f));
                        mark.text = $"{deg:0}°";
                    }
                    x += w;
                }
                playhead = UiKit.Image(timeline, "Playhead", null, Color.white, left, Vector2.zero, new Vector2(3f, 60f)).rectTransform;
            }
            if (playhead != null)
                playhead.anchoredPosition = new Vector2(-TimelineWidth / 2f + Mathf.Repeat(Playhead, total) / total * TimelineWidth, -8f);
        }

        void Write()
        {
            if (panel == null || Chosen == null)
                return;
            var e = Chosen;
            var s = new System.Text.StringBuilder();
            s.Append($"<size=34>{e.title}</size>   ({Selected + 1} of {Exhibits.Length})\n");
            s.Append(Current == Mode.Chain ? "Its clips chained as a fight chains them\n" : Current == Mode.Clips ? "Its clips, as made\n" : Current == Mode.Attacks ? "Its attacks, as a fight times them\n" : "Everyone, every clip in turn\n");
            if (Current == Mode.Chain && e.chain.Length > 0)
            {
                var step = e.chain[Mathf.Clamp(Step, 0, e.chain.Length - 1)];
                var st = e.State(step.clip);
                float stretch = !step.loop && st != null && step.duration > 0f ? st.length / step.duration : 1f;
                var seam = Seams(e)[Mathf.Clamp(Step, 0, e.chain.Length - 1)];
                s.Append($"{Step + 1} of {e.chain.Length}: {step.note}   ({(step.loop ? "looping" : $"once, played ×{stretch:0.00}")}, faded in over {step.fade:0.00}s)");
                s.Append(Step > 0 ? $"   joined: {seam.degrees:0}° ({seam.bone}){(seam.shift > 0.02f ? $", {seam.shift * 100f:0} cm" : "")}\n" : "\n");
            }
            else if (Current == Mode.Attacks && e.attacks.Length > 0)
            {
                var a = e.attacks[Mathf.Clamp(Item, 0, e.attacks.Length - 1)];
                s.Append($"{a.name} ({Item + 1} of {e.attacks.Length}):  {a.windup} {a.telegraph:0.00}s  ▸  {a.strike} {a.strikeTime:0.00}s  ▸  {a.recover} {a.recoverTime:0.00}s\n");
            }
            else if (Current == Mode.Attacks)
                s.Append("(no attacks: it isn't one that fights)\n");
            else if (Current == Mode.Clips)
                s.Append($"clip {Item + 1} of {e.Clips.Length}\n");
            else
                s.Append("\n");
            var state = e.State(NowPlaying);
            float length = state != null ? state.length : 0f;
            s.Append($"now: {NowPlaying}   {NowAt:0.00} / {length:0.00}s   frame {Mathf.RoundToInt(NowAt * Fps)} / {Mathf.RoundToInt(length * Fps)}\n");
            s.Append($"speed {(Speed >= 1f ? "1" : Speed == 0.5f ? "½" : Speed == 0.25f ? "¼" : "1/10")}{(Paused ? "   PAUSED" : "")}{(BonesShown == 1 ? "   bones" : BonesShown == 2 ? "   bones alone" : "")}");
            panel.supportRichText = true;
            panel.text = s.ToString();
        }
    }
}
