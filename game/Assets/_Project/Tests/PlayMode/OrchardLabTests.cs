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

        MothFlock Flock(string name) => UnityEngine.Object.FindObjectsByType<MothFlock>(FindObjectsSortMode.None).First(f => f.name == name);

        IEnumerator WaitFor(Func<bool> done, float seconds, string what)
        {
            for (float t = 0f; t < seconds && !done(); t += Time.deltaTime)
                yield return null;
            Assert.IsTrue(done(), $"waited {seconds} s for {what}");
        }

        // A flock circles its lantern while nothing's near; come close and they all come to you, circling, and send one
        // in at a time: it stalls, flaring, then dives, and strikes if you stand there.
        [UnityTest]
        public IEnumerator Moths_CircleTheirLantern_ThenComeAndDive()
        {
            var flock = Flock("Moths (middle terrace)");
            Assert.AreEqual(6, flock.Moths.Count);
            Assert.IsFalse(flock.Chasing, "circling their light");
            Assert.Less(Vector3.Distance(flock.Moths[0].transform.position, flock.lamp.position), 3f);
            Moth dived = null;
            Action<Moth> onDive = m => { if (m.Flock == flock) dived = m; };
            Moth.Dived += onDive;
            try
            {
                float health = apprentice.Health;
                apprentice.Teleport(new Vector3(0.8f, 1.95f, 4.2f));
                yield return WaitFor(() => flock.Chasing, 3f, "the flock coming");
                yield return WaitFor(() => dived != null, 6f, "one diving");
                yield return WaitFor(() => apprentice.Health < health, 2f, "its dive striking");
                Assert.AreEqual(health - dived.diveDamage, apprentice.Health, 0.5f, "a small hurt");
                int diving = flock.Moths.Count(m => m.Diving);
                Assert.LessOrEqual(diving, 1, "one at a time");
            }
            finally { Moth.Dived -= onDive; }
        }

        // One blow scatters a Moth; a heavy one goes on through all it reaches.
        [UnityTest]
        public IEnumerator Moths_OneBlowEach_TheHeavySweepsThroughSeveral()
        {
            var moths = Flock("Moths (top terrace)").Moths;
            apprentice.Teleport(new Vector3(-2f, 3.35f, 13f));
            apprentice.transform.rotation = Quaternion.LookRotation(Vector3.right);
            for (int i = 0; i < 3; i++)
                moths[i].Pin(apprentice.transform.position + new Vector3(1.3f, 1.0f, (i - 1) * 0.6f));
            yield return null;
            input.PressHeavy();
            yield return new WaitForSeconds(1.2f);
            Assert.AreEqual(3, moths.Take(3).Count(m => !m.IsAlive), "all three, in one heavy swing");
            moths[3].Pin(apprentice.transform.position + new Vector3(1.2f, 1.0f, 0f));
            yield return new WaitForSeconds(0.3f);
            input.PressLight();
            yield return new WaitForSeconds(0.6f);
            Assert.IsFalse(moths[3].IsAlive, "and one light blow, one Moth");
        }

        // Fire takes them like paper, and one alight sets the next alight: a flock goes up together.
        [UnityTest]
        public IEnumerator Moths_OneAlight_SetsTheFlockAlight()
        {
            var flock = Flock("Moths (top terrace)");
            foreach (var m in flock.Moths)
                m.passive = true; // (circling their lantern, close together)
            yield return new WaitForSeconds(0.5f);
            Burning.Set(flock.Moths[0], 1.5f, 6f);
            yield return WaitFor(() => flock.Alive == 0, 6f, "the whole flock gone up");
        }

        // Everything in the Orchard burns harder: the same fire does more here than in the Drowned City.
        [UnityTest]
        public IEnumerator InTheOrchard_EverythingBurnsHarder()
        {
            Assert.Greater(AreaTraits.FireHarm, 1.4f, "the Orchard's dry");
            var post = UnityEngine.Object.FindAnyObjectByType<TesterPost>();
            var laden = (Laden)post.spawner.SpawnAt("Laden", new Vector3(-4f, 0.55f, -5f), Vector3.forward);
            laden.passive = true;
            yield return new WaitForSeconds(0.3f);
            float before = laden.Health;
            laden.TakeBurn(10f, laden.transform.position);
            Assert.AreEqual(before - 10f * AreaTraits.FireHarm, laden.Health, 0.01f);
        }

        // A dive caught on a perfect block breaks against the oar.
        [UnityTest]
        public IEnumerator Moths_ADivePerfectBlocked_Scatters()
        {
            var flock = Flock("Moths (middle terrace)");
            Moth dived = null;
            Action<Moth> onDive = m => { if (m.Flock == flock) dived = m; };
            Moth.Dived += onDive;
            try
            {
                apprentice.Teleport(new Vector3(0.8f, 1.95f, 4.2f));
                yield return WaitFor(() => dived != null, 8f, "one diving");
                var moth = dived;
                var toIt = moth.transform.position - apprentice.transform.position;
                apprentice.transform.rotation = Quaternion.LookRotation(new Vector3(toIt.x, 0f, toIt.z));
                yield return WaitFor(() => !moth.IsAlive || Vector3.Distance(moth.transform.position, apprentice.transform.position + Vector3.up * 0.6f) < 1.7f, 2f, "it close");
                input.BlockHeld = true;
                yield return WaitFor(() => !moth.IsAlive, 1f, "it broken on the block");
                input.BlockHeld = false;
            }
            finally { Moth.Dived -= onDive; }
        }

        // (SHOTS=dir) The Moths: round their lantern, come to you, a stall, a dive; and a flock going up.
        [UnityTest]
        public IEnumerator Capture_Moths()
        {
            string outDir = Environment.GetEnvironmentVariable("SHOTS");
            if (string.IsNullOrEmpty(outDir))
                Assert.Ignore("set SHOTS to capture");
            Directory.CreateDirectory(outDir);
            Time.captureFramerate = 30;
            var cam = Camera.main;
            var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            cam.targetTexture = rt;
            void Shot(string name)
            {
                var previous = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                RenderTexture.active = previous;
                File.WriteAllBytes(Path.Combine(outDir, name + ".png"), tex.EncodeToPNG());
            }
            var flock = Flock("Moths (middle terrace)");
            apprentice.Teleport(new Vector3(-1.5f, 1.95f, 3.4f), true, true);
            for (int i = 0; i < 30; i++)
                yield return null;
            Shot("moths-0-round-their-lantern");
            apprentice.Teleport(new Vector3(0.8f, 1.95f, 4.2f), true, true);
            for (int i = 0; i < 90; i++)
                yield return null;
            Shot("moths-1-come");
            var stalled = flock.Moths.FirstOrDefault(m => m.CurrentState == Soured.State.Telegraph);
            for (int i = 0; i < 150 && stalled == null; i++)
            {
                yield return null;
                stalled = flock.Moths.FirstOrDefault(m => m.CurrentState == Soured.State.Telegraph);
            }
            Shot("moths-2-stall");
            while (stalled != null && stalled.CurrentState == Soured.State.Telegraph)
                yield return null;
            for (int i = 0; i < 3; i++)
                yield return null;
            Shot("moths-3-dive");
            Burning.Set(flock.Moths.First(m => m.IsAlive), 1.5f, 6f);
            for (int i = 0; i < 25; i++)
                yield return null;
            Shot("moths-4-alight");
            for (int i = 0; i < 40; i++)
                yield return null;
            Shot("moths-5-gone-up");
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
