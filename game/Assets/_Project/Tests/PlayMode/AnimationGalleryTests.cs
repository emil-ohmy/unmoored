using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MyGame.Tests
{
    // The animation gallery (Scenes/Lab_Gallery.unity): every rigged creature there with its clips, those that fight
    // with their attacks as a fight times them; each clip and attack plays; pausing holds it, a step moves it a frame.
    public class AnimationGalleryTests
    {
        AnimationGallery gallery;

        [UnitySetUp]
        public IEnumerator LoadGallery()
        {
            Application.targetFrameRate = -1;
            Time.captureDeltaTime = 1f / 60f;
            yield return SceneManager.LoadSceneAsync("Lab_Gallery");
            yield return null;
            gallery = UnityEngine.Object.FindAnyObjectByType<AnimationGallery>();
            yield return null;
        }

        [TearDown]
        public void ResetTime() => Time.captureDeltaTime = 0f;

        [UnityTest]
        public IEnumerator EveryCreature_IsThere_WithItsClips_AndTheFightersWithTheirAttacks()
        {
            int rigged = Directory.GetFiles(Path.Combine(Application.dataPath, "_Project/Art/Models/Characters"), "*.fbx").Length;
            Assert.AreEqual(rigged, gallery.Exhibits.Length, "every rigged model on a plinth");
            foreach (var e in gallery.Exhibits)
                Assert.Greater(e.Clips.Length, 0, $"{e.title}: its clips");
            foreach (var name in new[] { "Laden", "Keener", "Punctual", "The One Who Waits", "Father Moth" })
            {
                var e = gallery.Exhibits.First(x => x.title == name);
                Assert.Greater(e.attacks.Length, 0, $"{name}: its attacks");
                foreach (var a in e.attacks)
                    Assert.IsNotNull(e.State(a.windup), $"{name}: {a.name}'s wind-up clip ({a.windup})");
            }
            yield return null;
        }

        // Every creature, every clip, then every attack: each sampled a moment, nothing amiss.
        [UnityTest]
        public IEnumerator EveryClipAndAttack_Plays()
        {
            for (int i = 0; i < gallery.Exhibits.Length; i++)
            {
                gallery.Select(i);
                gallery.SetMode(AnimationGallery.Mode.Clips);
                for (int c = 0; c < gallery.Items; c++)
                {
                    gallery.SelectItem(c);
                    yield return null;
                    yield return null;
                    Assert.AreEqual(gallery.Exhibits[i].Clips[c], gallery.NowPlaying, $"{gallery.Exhibits[i].title}");
                }
                gallery.SetMode(AnimationGallery.Mode.Attacks);
                for (int a = 0; a < gallery.Items; a++)
                {
                    gallery.SelectItem(a);
                    yield return null;
                    var attack = gallery.Exhibits[i].attacks[a];
                    Assert.AreEqual(attack.windup, gallery.NowPlaying, $"{gallery.Exhibits[i].title}: {attack.name} begins with its wind-up");
                }
            }
            gallery.SetMode(AnimationGallery.Mode.Everyone);
            yield return new WaitForSeconds(0.5f);
        }

        // Every blow in reach draws a swipe through its strike (the creature's own trail for that attack, as a fight draws
        // it), and only its own trail: the Hour's hammer doesn't smear when the blade cuts. Shots draw none.
        static readonly string[] NotASwing =
        {
            "Thresher: Vent",               // (steam bursting out all round it: its own cloud)
            "Father Moth: Beat",            // (a gust of leaves off his wings: no blow)
            "The One Who Waits: Grind",     // (the gears grinding all round its base: their own sparks)
            "The One Who Waits: Stop",      // (a spell: the slam that follows it, StopSlam, swipes)
            "The Hold, That Would Not Let Go: Breath", // (the cave's breath: a gust, no blow)
            "The Hold, That Would Not Let Go: Bite",   // (its great mouth shutting where you stand: no swing)
        };

        [UnityTest]
        public IEnumerator EveryBlowInReach_DrawsItsSwipe()
        {
            var missing = new System.Collections.Generic.List<string>();
            for (int i = 0; i < gallery.Exhibits.Length; i++)
            {
                var e = gallery.Exhibits[i];
                gallery.Select(i);
                gallery.SetMode(AnimationGallery.Mode.Attacks);
                for (int k = 0; k < e.attacks.Length; k++)
                {
                    var a = e.attacks[k];
                    gallery.SelectItem(k);
                    yield return new WaitForSeconds(a.telegraph + a.strikeTime * 0.5f);
                    yield return null;
                    foreach (var trail in e.Trails)
                        if (a.ranged || !trail.For(a.attack))
                            Assert.AreEqual(0, trail.Samples, $"{e.title}: {a.name} draws a swipe that isn't its own ({trail.tip.parent.name})");
                    if (!a.ranged && !NotASwing.Contains($"{e.title}: {a.name}") && !e.Trails.Any(t => t.For(a.attack) && t.Samples >= 2))
                        missing.Add($"{e.title}: {a.name}");
                }
            }
            Assert.IsEmpty(missing, "blows in reach without a swipe");
        }

        // An attack plays as a fight times it: its wind-up stretched over the telegraph, then the strike.
        [UnityTest]
        public IEnumerator AnAttack_IsTimedAsAFightTimesIt()
        {
            int keener = Array.FindIndex(gallery.Exhibits, e => e.title == "Keener");
            gallery.Select(keener);
            gallery.SetMode(AnimationGallery.Mode.Attacks);
            var attack = gallery.Exhibits[keener].attacks[0];
            yield return new WaitForSeconds(attack.telegraph * 0.5f);
            Assert.AreEqual(attack.windup, gallery.NowPlaying);
            float length = gallery.Exhibits[keener].State(attack.windup).length;
            Assert.AreEqual(length * 0.5f, gallery.NowAt, length * 0.06f, "halfway through its clip at half its telegraph");
            yield return new WaitForSeconds(attack.telegraph * 0.5f + attack.strikeTime * 0.5f);
            Assert.AreEqual(attack.strike, gallery.NowPlaying, "then the strike");
        }

        // Paused, it holds; a step moves it one frame (a thirtieth of a second).
        [UnityTest]
        public IEnumerator Paused_ItHolds_AndAStepIsAFrame()
        {
            gallery.Select(0);
            gallery.SetMode(AnimationGallery.Mode.Clips);
            yield return new WaitForSeconds(0.3f);
            gallery.TogglePause();
            float held = gallery.Playhead;
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(held, gallery.Playhead, 1e-5f, "held");
            gallery.StepFrames(1);
            Assert.AreEqual(held + 1f / AnimationGallery.Fps, gallery.Playhead, 1e-4f, "a frame on");
            gallery.StepFrames(-2);
            Assert.AreEqual(held - 1f / AnimationGallery.Fps, gallery.Playhead, 1e-4f, "two back");
            gallery.TogglePause();
            gallery.SetSpeed(2); // (a quarter)
            float from = gallery.Playhead;
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(from + 0.25f, gallery.Playhead, 0.03f, "a quarter speed");
        }

        // The Laden's chain: its steps as its fight chains them, each played in turn, the joins measured.
        [UnityTest]
        public IEnumerator TheChain_PlaysEachStepInTurn_AndMeasuresTheJoins()
        {
            int laden = Array.FindIndex(gallery.Exhibits, e => e.title == "Laden");
            gallery.Select(laden);
            gallery.SetMode(AnimationGallery.Mode.Chain);
            var e = gallery.Exhibits[laden];
            Assert.Greater(e.chain.Length, 10, "its fight, step by step");
            var seams = gallery.Seams(e);
            float at = 0f;
            for (int i = 0; i < e.chain.Length; i++)
            {
                if (e.chain[i].duration < 0.1f)
                {
                    at += e.chain[i].duration; // (a step of a frame or two: too brief to catch in the middle of)
                    continue;
                }
                float mid = at + e.chain[i].duration * 0.5f;
                while (gallery.Playhead < mid)
                    yield return null;
                Assert.AreEqual(i, gallery.Step, $"step {i + 1}: {e.chain[i].clip}");
                at += e.chain[i].duration;
                if (i > 0)
                    Assert.GreaterOrEqual(seams[i].degrees, 0f);
            }
        }

        // (REPORT=dir) Every creature's joins and stretches, worst first: the seam report (markdown).
        [UnityTest]
        public IEnumerator Report_Seams()
        {
            string outDir = Environment.GetEnvironmentVariable("REPORT");
            if (string.IsNullOrEmpty(outDir))
                Assert.Ignore("set REPORT to write it");
            Directory.CreateDirectory(outDir);
            var md = new System.Text.StringBuilder("# The seam report\n\nEach creature's chain (its clips one after another as a fight chains them): at every join, how far the pose jumps (the bone that turns most, degrees; the furthest any bone shifts); for every one-off, how much the fight's timing speeds it up (×1 as made, ×2 twice as fast) or slows it. The game's crossfades: 0.15 s into a loop, 0.05 s into a one-off.\n");
            foreach (var e in gallery.Exhibits)
            {
                if (e.chain.Length == 0)
                    continue;
                var seams = gallery.Seams(e);
                md.Append($"\n## {e.title}\n\n| # | from → to | fade | jump | bone | shift | played |\n|---|---|---|---|---|---|---|\n");
                for (int i = 0; i < e.chain.Length; i++)
                {
                    var step = e.chain[i];
                    var st = e.State(step.clip);
                    string played = step.loop || st == null ? "loop" : $"×{st.length / Mathf.Max(0.01f, step.duration):0.00} ({st.length:0.00}s in {step.duration:0.00}s)";
                    string from = i > 0 ? e.chain[i - 1].clip + " → " : "";
                    md.Append(i == 0 ? $"| 1 | {step.clip} | | | | | {played} |\n"
                                     : $"| {i + 1} | {from}{step.clip} | {step.fade:0.00}s | **{seams[i].degrees:0}°** | {seams[i].bone} | {seams[i].shift * 100f:0} cm | {played} |\n");
                }
                // Into an attack from a loop at any point in it (the game's are at whatever point the loop is at).
                foreach (var a in e.attacks)
                    foreach (var loop in new[] { "Idle", "Walk", "GuardWalk" })
                    {
                        var st = e.State(loop);
                        if (st == null)
                            continue;
                        float worst = 0f;
                        for (int k = 0; k < 12; k++)
                            worst = Mathf.Max(worst, e.Jump(loop, st.length * k / 12f, a.windup, 0f).degrees);
                        md.Append($"\n- {loop} (anywhere in it) → {a.windup}: up to {worst:0}°");
                    }
                md.Append("\n");
                yield return null;
            }
            File.WriteAllText(Path.Combine(outDir, "seams.md"), md.ToString());
        }

        // (FILM=dir, FILM_CREATURE=title) A creature's chain filmed as the game plays it: every frame at 30 a second, from
        // the game's angle and from its side (the frames as PNGs). Several ("Laden,Keener", or * for all): each in a
        // folder of its own there.
        [UnityTest, Timeout(3600000)] // (a film of every one takes minutes)
        public IEnumerator Film_Chain()
        {
            string outDir = Environment.GetEnvironmentVariable("FILM");
            if (string.IsNullOrEmpty(outDir))
                Assert.Ignore("set FILM to film");
            string asked = Environment.GetEnvironmentVariable("FILM_CREATURE") ?? "Laden";
            var who = asked == "*" ? gallery.Exhibits.Select(e => e.title).ToArray() : asked.Split(',').Select(w => w.Trim()).ToArray();
            Time.captureDeltaTime = 1f / 30f;
            foreach (var title in who)
                yield return Film(who.Length > 1 ? Path.Combine(outDir, title) : outDir, title);
        }

        IEnumerator Film(string outDir, string who)
        {
            Directory.CreateDirectory(outDir);
            int index = Array.FindIndex(gallery.Exhibits, e => e.title == who);
            Assert.GreaterOrEqual(index, 0, who);
            gallery.Select(index);
            gallery.SetMode(AnimationGallery.Mode.Chain);
            var cam = Camera.main;
            var rt = new RenderTexture(640, 640, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            cam.targetTexture = rt;
            gallery.Zoom = 0.72f;
            float total = AnimationGallery.ChainLength(gallery.Exhibits[index]);
            int frames = Mathf.CeilToInt(total * 30f);
            float gamePitch = gallery.pitch;
            var exhibit = gallery.Exhibits[index];
            var log = new System.Text.StringBuilder("# view frame: step clip (seconds into it) [swiping]\n");
            try
            {
                foreach (var (view, yaw, pitch) in new[] { ("game", 0f, gamePitch), ("side", 90f, 10f) })
                {
                    gallery.Yaw = yaw;
                    gallery.pitch = pitch;
                    gallery.SetMode(AnimationGallery.Mode.Chain); // (from its start)
                    yield return null;
                    for (int f = 0; f < frames; f++)
                    {
                        yield return null;
                        var previous = RenderTexture.active;
                        cam.Render();
                        RenderTexture.active = rt;
                        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                        tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
                        tex.Apply();
                        RenderTexture.active = previous;
                        File.WriteAllBytes(Path.Combine(outDir, $"{view}_{f + 1:0000}.png"), tex.EncodeToPNG());
                        UnityEngine.Object.Destroy(tex);
                        log.Append($"{view} {f + 1}: {gallery.Step + 1} {gallery.NowPlaying} ({gallery.NowAt:0.00}s){(exhibit.Trails.Any(t => t.Emitting || t.Samples > 1) ? " swiping" : "")}\n");
                    }
                }
            }
            finally
            {
                cam.targetTexture = null;
                rt.Release();
                gallery.pitch = gamePitch;
                gallery.Yaw = 0f;
                File.WriteAllText(Path.Combine(outDir, "frames.txt"), log.ToString());
            }
        }

        // (SHOTS=dir) The gallery: a few of them, and the bones.
        [UnityTest]
        public IEnumerator Capture_Gallery()
        {
            string outDir = Environment.GetEnvironmentVariable("SHOTS");
            if (string.IsNullOrEmpty(outDir))
                Assert.Ignore("set SHOTS to capture");
            Directory.CreateDirectory(outDir);
            var cam = Camera.main;
            var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            cam.targetTexture = rt;
            void Shot(string name)
            {
                var previous = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
                tex.Apply();
                RenderTexture.active = previous;
                File.WriteAllBytes(Path.Combine(outDir, name + ".png"), tex.EncodeToPNG());
            }
            try
            {
                foreach (var name in new[] { "Laden", "Keener", "Father Moth", "The One Who Waits" })
                {
                    gallery.Select(Array.FindIndex(gallery.Exhibits, e => e.title == name));
                    gallery.SetMode(AnimationGallery.Mode.Attacks);
                    yield return new WaitForSeconds(1.5f);
                    Shot("gallery-" + name.ToLowerInvariant().Replace(' ', '-'));
                }
                gallery.NextBones();
                yield return new WaitForSeconds(0.3f);
                Shot("gallery-bones");
                gallery.NextBones();
                yield return new WaitForSeconds(0.3f);
                Shot("gallery-bones-alone");
            }
            finally { cam.targetTexture = null; }
        }
    }
}
