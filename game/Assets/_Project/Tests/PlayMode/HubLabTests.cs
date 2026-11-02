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
    // The Hub's lab (Lab_Hub): the Ferryman's island, a quiet refuge. You come up out of the Hold through the trapdoor
    // in his hut; out of his door, a path down to the jetty and its landing (the boat, the Mooring post).
    public class HubLabTests
    {
        Apprentice apprentice;
        ScriptedInput input;

        [UnitySetUp]
        public IEnumerator LoadTheHub()
        {
            Application.targetFrameRate = -1;
            Time.captureDeltaTime = 1f / 60f;
            yield return SceneManager.LoadSceneAsync("Lab_Hub");
            yield return null;
            apprentice = UnityEngine.Object.FindAnyObjectByType<Apprentice>();
            apprentice.Input = input = new ScriptedInput();
            yield return new WaitForSeconds(0.3f);
        }

        [TearDown]
        public void ResetTime() => Time.captureDeltaTime = 0f;

        static Storeys Hut => UnityEngine.Object.FindObjectsByType<Storeys>(FindObjectsSortMode.None).First(s => s.name == "The Ferryman's Hut");

        // Walked through `points` (e, n) in turn; fails where it's stuck.
        IEnumerator Walk(params Vector2[] points)
        {
            foreach (var p in points)
            {
                Vector3 last = apprentice.transform.position;
                float stuck = 0f;
                for (float t = 0f; t < 30f; t += Time.deltaTime)
                {
                    Vector3 to = new Vector3(p.x, 0f, p.y) - new Vector3(apprentice.transform.position.x, 0f, apprentice.transform.position.z);
                    if (to.magnitude < 0.35f)
                        break;
                    input.Move = new Vector2(to.x, to.z).normalized;
                    yield return null;
                    Vector3 now = apprentice.transform.position;
                    stuck = (now - last).magnitude < 0.003f ? stuck + Time.deltaTime : 0f;
                    last = now;
                    Assert.Less(stuck, 1f, $"stuck at {now} on the way to {p}");
                    Assert.IsTrue(apprentice.IsAlive && apprentice.CurrentState != Apprentice.State.Sinking, $"in the water at {now}");
                }
            }
            input.Move = Vector2.zero;
        }

        static readonly Vector2[] OutAndDown =
        {
            new Vector2(0.3f, 3.9f), new Vector2(0f, 2.2f),      // (out of his door)
            new Vector2(0.2f, -4f), new Vector2(0.45f, -9f),     // (down the path to the jetty's head)
            new Vector2(0.45f, -22.5f), new Vector2(0.4f, -24.8f) // (down the jetty, onto the landing)
        };

        // You come up in his hut, beside the open trapdoor: inside (its roof faded), the shaft covered over.
        [UnityTest]
        public IEnumerator YouComeUp_InHisHut_BesideTheTrapdoor()
        {
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(0, Hut.Level, "inside his hut");
            Assert.IsFalse(Hut.Shown(1), "its roof faded");
            Assert.IsNotNull(GameObject.Find("Over the Shaft (unseen)"), "nobody falls down the shaft");
            Vector3 hatch = GameObject.Find("Over the Shaft (unseen)").transform.position;
            Assert.Less(Vector3.Distance(new Vector3(hatch.x, 0f, hatch.z), new Vector3(apprentice.transform.position.x, 0f, apprentice.transform.position.z)), 1.6f,
                "beside the trapdoor");
        }

        // Out of his door, down the path and the jetty, onto the landing: the boat, the Mooring post.
        [UnityTest]
        public IEnumerator YouWalkOutOfHisDoor_DownToTheLanding()
        {
            yield return Walk(OutAndDown[0], OutAndDown[1]);
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(-1, Hut.Level, "out of the hut");
            Assert.IsTrue(Hut.Shown(1), "its roof back");
            yield return Walk(OutAndDown.Skip(2).ToArray());
            var post = UnityEngine.Object.FindAnyObjectByType<MooringPost>();
            Assert.Less(Vector3.Distance(post.transform.position, apprentice.transform.position), 3f, "by the Mooring post");
            var boat = UnityEngine.Object.FindAnyObjectByType<Boat>();
            Assert.Less(Vector3.Distance(boat.transform.position, apprentice.transform.position), 4f, "his boat, waiting");
        }

        // (SHOTS=dir) The Hub from the game camera: in his hut, out of his door, the path, the jetty, the landing.
        [UnityTest]
        public IEnumerator Capture_TheHub()
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
                File.WriteAllBytes(Path.Combine(outDir, $"hub-{name}.png"), tex.EncodeToPNG());
                cam.targetTexture = null;
                RenderTexture.active = null;
                UnityEngine.Object.Destroy(tex);
                rt.Release();
            }
            yield return new WaitForSeconds(1.5f);
            Shot("1-in-his-hut");
            yield return Walk(OutAndDown[0], OutAndDown[1]);
            yield return new WaitForSeconds(1f);
            Shot("2-out-of-his-door");
            yield return Walk(new Vector2(0.2f, -3f));
            yield return new WaitForSeconds(0.6f);
            Shot("3-the-path");
            yield return Walk(new Vector2(0.45f, -9f));
            yield return new WaitForSeconds(0.6f);
            Shot("4-the-jetty-head");
            yield return Walk(new Vector2(0.45f, -16f));
            yield return new WaitForSeconds(0.6f);
            Shot("5-along-the-jetty");
            yield return Walk(new Vector2(0.45f, -22.5f), new Vector2(0.4f, -24.8f));
            yield return new WaitForSeconds(0.8f);
            Shot("6-the-landing");
        }
    }
}
