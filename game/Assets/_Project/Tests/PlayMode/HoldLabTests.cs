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
            Assert.GreaterOrEqual(UnityEngine.Object.FindObjectsByType<Watcher>(FindObjectsSortMode.None).Length, 6, "the Shades' eyes in the dark");
            Assert.AreEqual(21, UnityEngine.Object.FindObjectsByType<WallEye>(FindObjectsSortMode.None).Length, "an eye in every socket of the walls");
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
            apprentice.Teleport((there.z < 30f ? At(-6f, 53.5f) : At(0f, 0f)) + Vector3.up * 0.05f); // (far off: the lake's islet, or back where you woke)
            yield return new WaitForSeconds(12f);
            Assert.IsTrue(eyes.Watching, "back, once the light has passed");
        }

        // The walls breathe: in and out, again and again (the shader's breath moving between -1 and 1).
        [UnityTest]
        public IEnumerator TheWalls_Breathe()
        {
            var walls = UnityEngine.Object.FindAnyObjectByType<LivingWalls>();
            Assert.IsNotNull(walls);
            float low = 1f, high = -1f;
            for (float t = 0f; t < 9f; t += Time.deltaTime)
            {
                float b = Shader.GetGlobalFloat("_WallBreath");
                low = Mathf.Min(low, b);
                high = Mathf.Max(high, b);
                yield return null;
            }
            Assert.GreaterOrEqual(walls.Breaths, 1, "a breath taken");
            Assert.Less(low, -0.8f, "let go");
            Assert.Greater(high, 0.8f, "drawn in");
        }

        // An eye in the wall: shut till you come, then open, turning to follow you; shut again once you've gone.
        [UnityTest]
        public IEnumerator AnEye_OpensAsYouCome_FollowsYou_AndShutsBehindYou()
        {
            var eye = UnityEngine.Object.FindObjectsByType<WallEye>(FindObjectsSortMode.None)
                .OrderBy(w => Vector3.Distance(w.transform.position, At(0.4f, 12f))).First();
            Assert.IsFalse(eye.Open, "shut, while you're far");
            Vector3 front = eye.transform.position + Flat(eye.transform.forward) * 2.6f;
            apprentice.Teleport(new Vector3(front.x, 0.55f, front.z));
            yield return new WaitForSeconds(1.5f);
            Assert.IsTrue(eye.Open, "open, as you come");
            Quaternion looking = eye.ball.localRotation;
            Vector3 side = Vector3.Cross(Vector3.up, Flat(eye.transform.forward)) * 1.5f;
            apprentice.Teleport(new Vector3(front.x, 0.55f, front.z) + side);
            yield return new WaitForSeconds(1f);
            Assert.Greater(Quaternion.Angle(looking, eye.ball.localRotation), 8f, "it followed you");
            apprentice.Teleport(At(-6f, 53.5f) + Vector3.up * 0.05f);
            yield return new WaitForSeconds(2f);
            Assert.Less(eye.Openness, 0.05f, "shut behind you");
        }

        // A face or a hand of the kept presses out of the wall while you're near.
        [UnityTest]
        public IEnumerator TheKept_PressOutOfTheWalls()
        {
            var kept = UnityEngine.Object.FindObjectsByType<WallPresence>(FindObjectsSortMode.None)
                .OrderBy(w => Vector3.Distance(w.transform.position, At(4f, 35f))).First();
            Vector3 front = kept.transform.position + Flat(kept.transform.forward) * 3f;
            apprentice.Teleport(new Vector3(front.x, 0.55f, front.z));
            float most = 0f;
            for (float t = 0f; t < 24f && most < 0.9f; t += Time.deltaTime)
            {
                most = Mathf.Max(most, kept.Out);
                yield return null;
            }
            Assert.Greater(kept.Presses, 0, "pressed out");
            Assert.Greater(most, 0.9f, "all the way");
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z).normalized;

        [UnityTest]
        public IEnumerator Drips_FallOutOfTheDark()
        {
            var drips = UnityEngine.Object.FindAnyObjectByType<Drips>();
            Assert.IsNotNull(drips);
            yield return new WaitForSeconds(4f);
            Assert.Greater(drips.Fallen, 3, "drops falling, near you");
        }

        // (SHOTS=dir) An eye in the wall up close, from in front: shut, then open (to see its parts are the right way round).
        [UnityTest]
        public IEnumerator Capture_AnEye()
        {
            string outDir = Environment.GetEnvironmentVariable("SHOTS");
            if (string.IsNullOrEmpty(outDir))
                Assert.Ignore("set SHOTS to capture");
            Directory.CreateDirectory(outDir);
            var eye = UnityEngine.Object.FindObjectsByType<WallEye>(FindObjectsSortMode.None)
                .OrderBy(w => Vector3.Distance(w.transform.position, At(0.4f, 12f))).First();
            var cam = new GameObject("Close").AddComponent<Camera>();
            cam.fieldOfView = 40f;
            cam.transform.position = eye.transform.position + eye.transform.forward * 1.3f + Vector3.up * 0.15f;
            cam.transform.LookAt(eye.transform.position);
            var lamp = new GameObject("Lamp").AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.range = 4f;
            lamp.intensity = 3f;
            lamp.transform.position = cam.transform.position + Vector3.up * 0.4f;
            foreach (var (name, near) in new[] { ("shut", false), ("open", true) })
            {
                Vector3 front = eye.transform.position + Flat(eye.transform.forward) * (near ? 2.6f : 14f);
                apprentice.Teleport(near ? new Vector3(front.x, 0.55f, front.z) : At(-6f, 53.5f));
                yield return new WaitForSeconds(2f);
                var rt = new RenderTexture(800, 600, 24);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(800, 600, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 800, 600), 0, 0);
                File.WriteAllBytes(Path.Combine(outDir, $"eye-{name}.png"), tex.EncodeToPNG());
                cam.targetTexture = null;
                RenderTexture.active = null;
                UnityEngine.Object.Destroy(tex);
                rt.Release();
                Debug.Log($"[Hold] eye {name}: openness {eye.Openness:0.00}");
            }
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
            foreach (var (name, at) in new[] { ("1-pool", At(0f, 0f)), ("2-passage", At(0.4f, 12f)), ("3-passage", At(2.6f, 19f)), ("4-gallery", At(4f, 35f)),
                                               ("5-gallery", At(4f, 42f)), ("6-lake", At(5f, 58f)), ("7-mooring", At(-6f, 53.5f)), ("8-ramp", At(-8.4f, 69.9f, 4.1f)),
                                               ("9-crack", At(-2.4f, 68.6f, 6f)) })
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
