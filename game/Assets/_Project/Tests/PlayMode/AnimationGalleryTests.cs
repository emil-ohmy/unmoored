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
            gallery.Step(1);
            Assert.AreEqual(held + 1f / AnimationGallery.Fps, gallery.Playhead, 1e-4f, "a frame on");
            gallery.Step(-2);
            Assert.AreEqual(held - 1f / AnimationGallery.Fps, gallery.Playhead, 1e-4f, "two back");
            gallery.TogglePause();
            gallery.SetSpeed(2); // (a quarter)
            float from = gallery.Playhead;
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(from + 0.25f, gallery.Playhead, 0.03f, "a quarter speed");
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
