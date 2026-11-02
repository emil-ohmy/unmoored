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
    // The Hold as the user drew it (Lab_HoldLayout, from Levels/the_hold.json): you begin in the south passage, go up
    // the long hall (the Kept tear out of its walls) and the corridor north of it to the lantern (a Mooring post); the
    // tutorial's messages on the floor along the way.
    public class HoldLayoutTests
    {
        Apprentice apprentice;
        ScriptedInput input;

        [UnitySetUp]
        public IEnumerator LoadIt()
        {
            Application.targetFrameRate = -1;
            Time.captureDeltaTime = 1f / 60f;
            yield return SceneManager.LoadSceneAsync("Lab_HoldLayout");
            yield return null;
            apprentice = UnityEngine.Object.FindAnyObjectByType<Apprentice>();
            apprentice.Input = input = new ScriptedInput();
            apprentice.postHitInvulnerability = 1000f; // (struck once, then left alone: the walk is what's tested)
            yield return new WaitForSeconds(0.3f);
        }

        [TearDown]
        public void ResetTime() => Time.captureDeltaTime = 0f;

        // The route, in the scene's metres (the drawing's, from where you begin).
        static readonly Vector2[] Route =
        {
            new Vector2(-1.1f, 8.8f),    // (up the south passage)
            new Vector2(-1.1f, 38.8f),   // (into the long hall)
            new Vector2(4.5f, 73.8f),    // (into the corridor at its north-east)
            new Vector2(4.5f, 120f),     // (up it, to the lantern)
        };

        IEnumerator Walk(Vector2 to, Action each = null)
        {
            Vector3 last = apprentice.transform.position;
            float stuck = 0f;
            for (float t = 0f; t < 40f; t += Time.deltaTime)
            {
                Vector3 d = new Vector3(to.x, 0f, to.y) - new Vector3(apprentice.transform.position.x, 0f, apprentice.transform.position.z);
                if (d.magnitude < 0.5f)
                    break;
                input.Move = new Vector2(d.x, d.z).normalized;
                if (apprentice.CurrentState == Apprentice.State.Staggered || apprentice.CurrentState == Apprentice.State.Attacking)
                    input.PressLight(); // (the Kept in the way: strike back)
                each?.Invoke();
                yield return null;
                Vector3 now = apprentice.transform.position;
                stuck = (now - last).magnitude < 0.003f ? stuck + Time.deltaTime : 0f;
                last = now;
                Assert.Less(stuck, 3f, $"stuck at {now} on the way to {to}");
                Assert.IsTrue(apprentice.IsAlive, $"died at {now}");
            }
            input.Move = Vector2.zero;
        }

        [UnityTest]
        public IEnumerator ItsPieces_AreAllThere()
        {
            Assert.AreEqual(6, UnityEngine.Object.FindObjectsByType<WallEye>(FindObjectsSortMode.None).Length, "six eyes");
            Assert.AreEqual(9, UnityEngine.Object.FindObjectsByType<WallPresence>(FindObjectsSortMode.None).Length, "four faces, five hands");
            Assert.AreEqual(3, UnityEngine.Object.FindObjectsByType<Kept>(FindObjectsSortMode.None).Length, "three of the Kept");
            Assert.AreEqual(3, UnityEngine.Object.FindObjectsByType<FloorMessage>(FindObjectsSortMode.None).Length, "three messages");
            Assert.AreEqual(1, UnityEngine.Object.FindObjectsByType<MooringPost>(FindObjectsSortMode.None).Length, "the lantern's post");
            Assert.IsNotNull(UnityEngine.Object.FindAnyObjectByType<Drips>(), "drips all through it");
            yield return null;
        }

        // You walk it, start to lantern; the Kept come out of the walls as you pass; each message read says its piece.
        [UnityTest]
        public IEnumerator YouWalkIt_ReadingTheMessages_AndTheKeptComeOut()
        {
            var hud = UnityEngine.Object.FindAnyObjectByType<Hud>();
            var messages = UnityEngine.Object.FindObjectsByType<FloorMessage>(FindObjectsSortMode.None).OrderBy(m => m.transform.position.z).ToList();
            // (the route, with a step aside onto each message as you come to it)
            var stops = Route.Select(p => (p, (FloorMessage)null)).ToList();
            foreach (var m in messages)
            {
                var at = new Vector2(m.transform.position.x, m.transform.position.z);
                int i = stops.FindIndex(s => s.Item1.y > at.y);
                stops.Insert(i < 0 ? stops.Count : i, (at, m));
            }
            foreach (var (p, m) in stops)
            {
                yield return Walk(p);
                if (m != null)
                {
                    yield return null;
                    Assert.AreEqual(m, apprentice.Nearby, $"{m.message}: in reach to read");
                    input.PressInteract();
                    yield return null;
                    yield return null;
                    Assert.AreEqual(m.message, hud.FoundBanner.Text.text, "its message up");
                }
            }
            Assert.AreEqual(messages.Count, messages.Count(m => m.Read > 0), "every message read on the way");
            Assert.IsTrue(UnityEngine.Object.FindObjectsByType<Kept>(FindObjectsSortMode.None).All(k => k.Emerged || !k.IsAlive), "the Kept torn out of the walls as you passed");
            var post = UnityEngine.Object.FindAnyObjectByType<MooringPost>();
            Assert.Less(Vector3.Distance(post.transform.position, apprentice.transform.position), 8f, "at the lantern");
            yield return null;
            yield return null;
            StringAssert.Contains("Mooring post", hud.Controls(messages.Last().line), "its line");
        }

        // (SHOTS=dir) The way through, from the game camera.
        [UnityTest]
        public IEnumerator Capture_TheWayThrough()
        {
            string outDir = Environment.GetEnvironmentVariable("SHOTS");
            if (string.IsNullOrEmpty(outDir))
                Assert.Ignore("set SHOTS to capture");
            Directory.CreateDirectory(outDir);
            var cam = Camera.main;
            void Shot(string name)
            {
                var rt = new RenderTexture(1600, 900, 24);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                File.WriteAllBytes(Path.Combine(outDir, $"layout-{name}.png"), tex.EncodeToPNG());
                cam.targetTexture = null;
                RenderTexture.active = null;
                UnityEngine.Object.Destroy(tex);
                rt.Release();
            }
            yield return new WaitForSeconds(1.5f);
            Shot("1-we-begin-here");
            var first = UnityEngine.Object.FindObjectsByType<FloorMessage>(FindObjectsSortMode.None).OrderBy(m => m.transform.position.z).First();
            yield return Walk(new Vector2(first.transform.position.x, first.transform.position.z));
            input.PressInteract();
            yield return new WaitForSeconds(0.6f);
            Shot("2-the-first-message");
            yield return Walk(new Vector2(-1.1f, 26f));
            yield return new WaitForSeconds(1.5f);
            Shot("3-the-hall-the-kept");
            yield return Walk(new Vector2(-1.1f, 50f));
            yield return new WaitForSeconds(1f);
            Shot("4-the-hall-north");
            yield return Walk(new Vector2(4.5f, 80f));
            yield return new WaitForSeconds(0.6f);
            Shot("5-the-corridor");
            yield return Walk(new Vector2(4.5f, 120f));
            yield return new WaitForSeconds(0.6f);
            Shot("6-the-lantern");
        }
    }
}
