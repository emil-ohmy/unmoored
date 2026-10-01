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
    // The Orchard of Last Autumn's lab (Scenes/Lab_Orchard.unity): its terraces walk up (steps) and drop down, the press
    // house's loft is reached by its stair (and the roof fades inside), the air has its leaves and mist, the night its
    // harvest moon.
    public class OrchardLabTests
    {
        Apprentice apprentice;
        ScriptedInput input;

        [UnitySetUp]
        public IEnumerator LoadOrchard()
        {
            Application.targetFrameRate = -1;
            Time.captureDeltaTime = 1f / 60f;
            yield return SceneManager.LoadSceneAsync("Lab_Orchard");
            yield return null;
            apprentice = UnityEngine.Object.FindAnyObjectByType<Apprentice>();
            apprentice.Input = input = new ScriptedInput();
            yield return new WaitForSeconds(0.3f);
        }

        [TearDown]
        public void ResetTime()
        {
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f;
        }

        IEnumerator Walk(Vector2 move, float seconds)
        {
            input.Move = move;
            yield return new WaitForSeconds(seconds);
            input.Move = Vector2.zero;
            yield return new WaitForSeconds(0.2f);
        }

        // Its night: a warm low moon (not the city's cold high one), the leaves falling, the mist lying low.
        [UnityTest]
        public IEnumerator TheOrchard_HasItsHarvestMoon_LeavesAndMist()
        {
            var moon = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l => l.type == LightType.Directional);
            Assert.Greater(moon.color.r, moon.color.b, "a warm moon");
            Assert.Less(moon.transform.eulerAngles.x, 35f, "and low: long shadows");
            var air = GameObject.Find("Orchard Air");
            Assert.IsNotNull(air);
            yield return new WaitForSeconds(0.5f);
            var leaves = air.transform.Find("Falling Leaves").GetComponent<ParticleSystem>();
            Assert.Greater(leaves.particleCount, 50, "leaves in the air from the start");
            Assert.Greater(air.GetComponentsInChildren<ParticleSystem>().Count(p => p.name == "Mist"), 2, "mist on the terraces");
        }

        // Up the steps from the bottom terrace to the middle, and from the middle to the top; down again by a drop.
        [UnityTest]
        public IEnumerator TheTerraces_WalkUpTheSteps_AndDropDown()
        {
            apprentice.Teleport(new Vector3(-9f, 0.55f, -1.6f));
            yield return Walk(Vector2.up, 1.8f);
            Assert.AreEqual(1.9f, apprentice.transform.position.y, 0.2f, $"up on the middle terrace ({apprentice.transform.position})");
            apprentice.Teleport(new Vector3(3f, 1.95f, 6.8f));
            yield return Walk(Vector2.up, 1.8f);
            Assert.AreEqual(3.3f, apprentice.transform.position.y, 0.2f, $"up on the top terrace ({apprentice.transform.position})");
            apprentice.Teleport(new Vector3(-6f, 3.35f, 10.8f));
            yield return Walk(Vector2.down, 1f);
            Assert.AreEqual(1.9f, apprentice.transform.position.y, 0.2f, "dropped down a terrace (one way)");
            Assert.IsTrue(apprentice.IsAlive && apprentice.CurrentState == Apprentice.State.Free, "no harm in it");
        }

        // The press house: up its stair (stepped onto from the floor beside it) to the loft; inside, its roof fades (and the loft over you,
        // down on the floor).
        [UnityTest]
        public IEnumerator ThePressHouse_UpTheStairToTheLoft_TheRoofFades()
        {
            var house = UnityEngine.Object.FindAnyObjectByType<Storeys>();
            apprentice.Teleport(new Vector3(12.4f, 1.95f, 3.5f)); // (on the floor, beside the stair's foot)
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(0, house.Level, "inside, on the floor");
            Assert.IsFalse(house.Shown(2), "the roof gone");
            Assert.IsFalse(house.Shown(1), "and the loft over you");
            yield return Walk(Vector2.right, 0.9f); // onto the stair from the side
            Assert.Greater(apprentice.transform.position.x, 13.5f, $"onto the stair ({apprentice.transform.position})");
            yield return Walk(Vector2.up, 2.6f);
            Assert.AreEqual(4.3f, apprentice.transform.position.y, 0.25f, $"up in the loft ({apprentice.transform.position})");
            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual(1, house.Level);
            Assert.IsTrue(house.Shown(1));
            Assert.IsFalse(house.Shown(2), "the roof still gone");
        }

        // (SHOTS=dir) The lab round: coming in, along the rows, the press house, its loft, the top terrace, and wide.
        [UnityTest]
        public IEnumerator Capture_Orchard()
        {
            string outDir = Environment.GetEnvironmentVariable("SHOTS");
            if (string.IsNullOrEmpty(outDir))
                Assert.Ignore("set SHOTS to capture");
            Directory.CreateDirectory(outDir);
            Time.captureFramerate = 30;
            var cam = Camera.main;
            var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            cam.targetTexture = rt;
            void Shot(string name, Camera from = null)
            {
                from ??= cam;
                var target = from.targetTexture;
                if (from != cam)
                    from.Render();
                var previous = RenderTexture.active;
                RenderTexture.active = target;
                var tex = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                tex.Apply();
                RenderTexture.active = previous;
                File.WriteAllBytes(Path.Combine(outDir, name + ".png"), tex.EncodeToPNG());
            }
            IEnumerator At(Vector3 p, string name)
            {
                apprentice.Teleport(p, true, true);
                for (int i = 0; i < 45; i++)
                    yield return null;
                Shot(name);
            }
            yield return At(new Vector3(-12f, 0.55f, -3.8f), "orchard-0-coming-in");
            yield return At(new Vector3(1f, 0.55f, -3.5f), "orchard-1-bottom-row");
            yield return At(new Vector3(-6f, 1.95f, 6.5f), "orchard-2-middle-rows");
            yield return At(new Vector3(5f, 1.95f, 5.6f), "orchard-3-press-house");
            yield return At(new Vector3(12f, 1.95f, 5f), "orchard-4-inside");
            yield return At(new Vector3(11f, 4.35f, 8f), "orchard-5-loft");
            yield return At(new Vector3(2f, 3.35f, 13f), "orchard-6-top-terrace");
            // Wide, from the south over the water.
            var wide = new GameObject("Wide").AddComponent<Camera>();
            wide.CopyFrom(cam);
            wide.targetTexture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            wide.transform.SetPositionAndRotation(new Vector3(0f, 22f, -22f), Quaternion.Euler(40f, 0f, 0f));
            yield return null;
            Shot("orchard-7-wide", wide);
        }
    }
}
