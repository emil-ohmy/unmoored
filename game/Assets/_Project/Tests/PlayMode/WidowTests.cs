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
    // The Widow Hale, the Hall of the Last Hour's miniboss (docs/brief.md), at the central engine in Lab_LastHour's
    // belfry: held in the stillness (blows pass through her), hurt only in a pocket of time round a part set going; she
    // goes to stop it; her knell and her veil; she keeps the engine's plate until she's beaten.
    public class WidowTests
    {
        Apprentice apprentice;
        ScriptedInput input;
        Widow widow;

        [UnitySetUp]
        public IEnumerator LoadTheBelfry()
        {
            Application.targetFrameRate = -1;
            Time.captureDeltaTime = 1f / 60f;
            yield return SceneManager.LoadSceneAsync("Lab_LastHour");
            yield return null;
            apprentice = UnityEngine.Object.FindAnyObjectByType<Apprentice>();
            apprentice.Input = input = new ScriptedInput();
            foreach (var kind in new[] { typeof(Punctual), typeof(Wound), typeof(Keener) })
                foreach (var c in UnityEngine.Object.FindObjectsByType(kind, FindObjectsSortMode.None).Cast<Component>())
                    UnityEngine.Object.Destroy(c.gameObject); // (her alone)
            widow = UnityEngine.Object.FindAnyObjectByType<Widow>();
            Assert.IsNotNull(widow, "the Widow, at the engine");
            yield return new WaitForSeconds(0.3f);
        }

        [TearDown]
        public void ResetTime()
        {
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f;
        }

        static IEnumerator Until(Func<bool> done, float timeout, string what)
        {
            for (float t = 0f; t < timeout && !done(); t += Time.deltaTime)
                yield return null;
            Assert.IsTrue(done(), "timed out waiting for " + what);
        }

        HitResult Strike(float damage = 20f, float stagger = 20f) =>
            widow.TakeHit(new Hit { Damage = damage, Stagger = stagger, From = apprentice.transform.position, Knockback = Vector3.zero });

        // Somewhere out of her way: the belfry's east side, over the broken floor from her.
        void Away() => apprentice.Teleport(CentralEngine.Instance.transform.position + new Vector3(6.2f, 0.05f, 4f));

        IEnumerator Stroke(Vector3 towards)
        {
            apprentice.transform.rotation = Quaternion.LookRotation(new Vector3(towards.x, 0f, towards.z));
            input.HeavyHeld = true;
            input.PressHeavy();
            for (float t = 0f; t < 2.5f && !apprentice.StrokeCharged; t += Time.deltaTime)
                yield return null;
            Assert.IsTrue(apprentice.StrokeCharged, "charged");
            input.HeavyHeld = false;
            yield return new WaitForSeconds(0.6f);
        }

        // Time stopped: a blow passes through her (no harm), as through a photograph.
        [UnityTest]
        public IEnumerator Widow_HeldInTheStillness_ABlowPassesThroughHer()
        {
            apprentice.Teleport(widow.transform.position + widow.transform.forward * 1.6f);
            yield return null;
            Assert.IsFalse(widow.InTime);
            float was = widow.Health;
            Strike(60f);
            Assert.AreEqual(was, widow.Health, 0.01f, "no harm");
            Assert.AreEqual(1, widow.Passed, "it passed through her");
            Assert.AreNotEqual(Soured.State.Staggered, widow.CurrentState, "nor a stagger");
        }

        // One of the engine's parts set going: time comes back round it (a pocket: colour, and she can be touched
        // there); she hurries to it, lays her hands on it, and it stops.
        [UnityTest]
        public IEnumerator Widow_APartSetGoing_IsAPocketOfTime_SheGoesToStopIt()
        {
            Away();
            var part = widow.parts[1]; // (the winding drum)
            part.pocket.clockwork.Set();
            yield return new WaitForSeconds(0.8f);
            Assert.IsTrue(TimePocket.InAny(part.pocket.Centre), "a pocket of time round it");
            Assert.IsFalse(TimePocket.InAny(part.pocket.Centre + Vector3.right * 6f), "only round it");
            Assert.GreaterOrEqual(Shader.GetGlobalFloat("_HallPocketCount"), 1f, "the colour back there");
            yield return Until(() => widow.Tending == part, 8f, "her hands on it");
            Assert.IsTrue(part.pocket.clockwork.Running, "still going, as she begins");
            yield return Until(() => !part.pocket.clockwork.Running, widow.tendTime + 1f, "stopped");
            Assert.AreEqual(1, widow.Stopped);
            yield return new WaitForSeconds(2f);
            Assert.IsFalse(TimePocket.InAny(part.pocket.Centre), "its pocket gone");
        }

        // In the pocket she can be hurt; struck there enough, she reels, her hands off it (it runs on).
        [UnityTest]
        public IEnumerator Widow_InThePocket_SheCanBeHurt_AndReels_HerHoldBroken()
        {
            Away();
            var part = widow.parts[0]; // (the escapement)
            part.pocket.clockwork.Set();
            yield return Until(() => widow.Tending == part, 8f, "her hands on it");
            Assert.IsTrue(widow.InTime, "in time, there");
            float was = widow.Health;
            Assert.AreEqual(HitResult.Landed, Strike());
            Assert.Less(widow.Health, was, "it touched her");
            Strike();
            Strike();
            Assert.AreEqual(Soured.State.Staggered, widow.CurrentState, "she reels");
            yield return null;
            Assert.IsNull(widow.Tending, "her hold broken");
            Assert.IsTrue(part.pocket.clockwork.Running, "it runs on");
        }

        // Her knell: a breath, then four rings as one band.
        [UnityTest]
        public IEnumerator Widow_TheKnell_FourRings()
        {
            apprentice.Teleport(widow.transform.position + widow.transform.forward * 7f);
            apprentice.postHitInvulnerability = 1000f;
            yield return Until(() => widow.Rings >= 4, 8f, "four rings");
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(4, widow.Rings, "four, one knell");
        }

        // Her veil, cast low along the floor: caught, you're slowed a while.
        [UnityTest]
        public IEnumerator Widow_TheVeil_CaughtYoureSlowed()
        {
            apprentice.Teleport(widow.transform.position + widow.transform.forward * 2.4f);
            yield return Until(() => apprentice.Slowed, 6f, "caught in the veil");
            Assert.Less(apprentice.Health, apprentice.maxHealth, "and it hurt");
        }

        // She keeps the engine: its plate won't take the Stroke. Beaten (farewell, the Ledger), it's free.
        [UnityTest]
        public IEnumerator Widow_KeepsTheEngine_TillBeaten_ThenItsFree()
        {
            var engine = CentralEngine.Instance;
            widow.Held = true; // (keeping still for this)
            apprentice.GiveStroke();
            apprentice.Teleport(engine.transform.position + Vector3.up * 0.05f);
            yield return new WaitForSeconds(0.3f);
            yield return Stroke(Vector3.back);
            yield return new WaitForSeconds(0.5f);
            Assert.Greater(engine.clockwork.Refusals, 0, "held fast");
            Assert.IsFalse(engine.Set, "not set going");
            Assert.IsFalse(HallTime.Instance.Running);
            HallTime.Instance.RunAtOnce(); // (time everywhere: she can be touched anywhere)
            for (int i = 0; i < 30 && widow.IsAlive; i++)
                Strike(60f, 0f);
            Assert.IsFalse(widow.IsAlive, "beaten");
            Assert.IsTrue(apprentice.Equipment.Met(LedgerEntry.WidowHale), "written in the Ledger");
            Assert.IsFalse(engine.clockwork.Locked(), "the plate free");
        }

        // (SHOTS=dir) The belfry: her vigil; a part set going, its pocket of time, her hands on it; the veil cast.
        [UnityTest]
        public IEnumerator Capture_TheWidow()
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
                File.WriteAllBytes(Path.Combine(outDir, $"widow-{name}.png"), tex.EncodeToPNG());
                cam.targetTexture = null;
                RenderTexture.active = null;
                UnityEngine.Object.Destroy(tex);
                rt.Release();
            }
            apprentice.Teleport(widow.transform.position + widow.transform.forward * 5f);
            apprentice.postHitInvulnerability = 1000f;
            widow.Held = true;
            yield return new WaitForSeconds(1.5f);
            Shot("1-vigil");
            var part = widow.parts[1];
            apprentice.Teleport(part.tendAt.position + new Vector3(1.6f, 0.05f, 1.2f)); // (by the part: the camera with it)
            yield return new WaitForSeconds(0.5f);
            widow.Held = false;
            part.pocket.clockwork.Set();
            yield return new WaitForSeconds(1.0f);
            Shot("2-pocket");
            yield return Until(() => widow.Tending != null, 8f, "tending");
            yield return new WaitForSeconds(0.6f);
            Shot("3-tending");
            yield return new WaitForSeconds(3f);
            apprentice.Teleport(widow.transform.position + widow.transform.forward * 2.4f);
            yield return Until(() => widow.CurrentAttack == widow.veil && widow.CurrentState == Soured.State.Strike, 8f, "the veil");
            yield return new WaitForSeconds(0.25f);
            Shot("4-veil");
        }
    }
}
