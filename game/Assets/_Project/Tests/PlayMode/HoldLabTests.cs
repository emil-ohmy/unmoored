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
    // The Hold's lab (Lab_Hold): the game's opening, its atmosphere: you wake on the stone, eyes watch from the dark and
    // shrink back from your lantern (and come back once you've passed), drips fall out of the dark.
    public class HoldLabTests
    {
        Apprentice apprentice;

        [UnitySetUp]
        public IEnumerator LoadTheHold()
        {
            Application.targetFrameRate = -1;
            Time.captureDeltaTime = 1f / 60f;
            yield return SceneManager.LoadSceneAsync("Lab_Hold");
            yield return null;
            apprentice = UnityEngine.Object.FindAnyObjectByType<Apprentice>();
            apprentice.Input = new ScriptedInput();
            yield return new WaitForSeconds(0.3f);
        }

        [TearDown]
        public void ResetTime() => Time.captureDeltaTime = 0f;

        static Vector3 At(float e, float n, float u = 0.5f) => new Vector3(e, u, n);

        [UnityTest]
        public IEnumerator YouWake_OnTheStone_InTheDark()
        {
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(0.5f, apprentice.transform.position.y, 0.15f, "stood on the stone, not sunk");
            Assert.Less(Vector3.Distance(apprentice.transform.position, At(0f, 0f)), 0.5f);
            Assert.IsTrue(apprentice.IsAlive);
            Assert.Greater(UnityEngine.Object.FindObjectsByType<Watcher>(FindObjectsSortMode.None).Length, 6, "eyes in the dark");
        }

        // A pair of eyes: watching; your lantern comes near and they're gone; you go on, and they're back.
        [UnityTest]
        public IEnumerator TheEyes_ShrinkBackFromTheLantern_AndComeBackOnceItsPassed()
        {
            var eyes = UnityEngine.Object.FindObjectsByType<Watcher>(FindObjectsSortMode.None)
                .OrderBy(w => Vector3.Distance(w.transform.position, apprentice.transform.position)).First();
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(eyes.Watching, "watching");
            Vector3 there = eyes.transform.position;
            apprentice.Teleport(new Vector3(there.x, 0.55f, there.z) + Vector3.back * 1.5f);
            yield return new WaitForSeconds(1.5f);
            Assert.IsFalse(eyes.Watching, "gone from the light");
            apprentice.Teleport((there.z < 20f ? At(-1f, 34.5f) : At(0f, 0f)) + Vector3.up * 0.05f); // (far off: the islet, or back where you woke)
            yield return new WaitForSeconds(12f);
            Assert.IsTrue(eyes.Watching, "back, once the light has passed");
        }

        [UnityTest]
        public IEnumerator Drips_FallOutOfTheDark()
        {
            var drips = UnityEngine.Object.FindAnyObjectByType<Drips>();
            Assert.IsNotNull(drips);
            yield return new WaitForSeconds(4f);
            Assert.Greater(drips.Fallen, 3, "drops falling, near you");
        }

        // (SHOTS=dir) The route from the game camera: where you wake, the Boardwalk, the Foundations, the Pens, the
        // Mooring islet, the Basin's edge, the Climb.
        [UnityTest]
        public IEnumerator Capture_TheHold()
        {
            string outDir = Environment.GetEnvironmentVariable("SHOTS");
            if (string.IsNullOrEmpty(outDir))
                Assert.Ignore("set SHOTS to capture");
            Directory.CreateDirectory(outDir);
            var cam = Camera.main;
            foreach (var (name, at) in new[] { ("0-eyes", At(0f, 4.5f)), ("1-waking", At(0f, 0f)), ("2-boardwalk", At(0f, 8f)), ("3-foundations", At(8f, 21f)),
                                               ("4-pens", At(6f, 29f)), ("5-mooring", At(-1f, 34f)), ("6-basin", At(-6.4f, 43.7f, 0.3f)),
                                               ("7-climb", At(-8.5f, 56f, 0.3f)), ("8-hatch", At(-9f, 60.5f, 10f)) })
            {
                apprentice.Teleport(at + Vector3.up * 0.05f);
                yield return new WaitForSeconds(2.5f);
                var seen = UnityEngine.Object.FindObjectsByType<Watcher>(FindObjectsSortMode.None)
                    .Select(w => (w, v: cam.WorldToViewportPoint(w.transform.position)))
                    .Where(x => x.v.z > 0f && x.v.x > 0f && x.v.x < 1f && x.v.y > 0f && x.v.y < 1f)
                    .Select(x => $"{x.w.transform.position.x:0},{x.w.transform.position.z:0}{(x.w.Watching ? "" : " (gone)")}");
                Debug.Log($"[Hold] {name}: eyes in view: {string.Join("; ", seen)}");
                var rt = new RenderTexture(1600, 900, 24);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                File.WriteAllBytes(Path.Combine(outDir, $"hold-{name}.png"), tex.EncodeToPNG());
                cam.targetTexture = null;
                RenderTexture.active = null;
                UnityEngine.Object.Destroy(tex);
                rt.Release();
            }
        }
    }
}
