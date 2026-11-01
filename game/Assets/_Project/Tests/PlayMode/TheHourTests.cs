using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MyGame.Tests
{
    // The One Who Waits, the Hour (Scenes/Lab_OneWhoWaits.unity): limp and brittle with the Hall's time stopped, the real
    // fight with it running (its quarters, its rewind, Stop You, the floor's hands); beaten either way, and which way kept.
    public class TheHourTests
    {
        Apprentice apprentice;
        ScriptedInput input;
        TheHour hour;

        [UnitySetUp]
        public IEnumerator LoadTheHour()
        {
            Application.targetFrameRate = -1;
            Time.captureDeltaTime = 1f / 60f;
            yield return SceneManager.LoadSceneAsync("Lab_OneWhoWaits");
            yield return null;
            apprentice = UnityEngine.Object.FindAnyObjectByType<Apprentice>();
            apprentice.Input = input = new ScriptedInput();
            hour = UnityEngine.Object.FindAnyObjectByType<TheHour>();
            yield return new WaitForSeconds(0.3f);
        }

        [TearDown]
        public void ResetTime()
        {
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f;
        }

        // In front of it (south), `distance` from its middle, on its floor.
        Vector3 Before(float distance) => hour.middle + new Vector3(0f, 0.05f, -distance);

        static IEnumerator Until(Func<bool> done, float timeout, string what)
        {
            for (float t = 0f; t < timeout && !done(); t += Time.deltaTime)
                yield return null;
            Assert.IsTrue(done(), "timed out waiting for " + what);
        }

        // A blow of the oar on it, as the Apprentice lands one.
        HitResult Strike(float damage = 14f) => hour.TakeHit(new Hit { Damage = damage, Stagger = 0f, From = apprentice.transform.position, Knockback = Vector3.zero });

        void Run() => HallTime.Instance.RunAtOnce();

        // Time stopped: it hangs limp, wakes only to loll; brittle (a blow does two and a half times its harm), no
        // quarters, no rewind, its machines still; beaten, the hour never comes.
        [UnityTest]
        public IEnumerator Stopped_ItHangsLimp_Brittle_NoQuarters_AndTheHourNeverComes()
        {
            Assert.IsTrue(hour.Limp, "the Hall's time stopped: limp");
            apprentice.Teleport(Before(6f));
            yield return Until(() => hour.Woken, 2f, "woken (as much as it wakes)");
            float was = hour.Health;
            Assert.AreEqual(HitResult.Landed, Strike());
            Assert.AreEqual(was - 35f, hour.Health, 0.01f, "brittle: 14 as 35");
            bool? running = null;
            TheHour.Beaten += (h, r) => running = r;
            for (int i = 0; i < 60 && hour.IsAlive; i++)
            {
                Strike();
                yield return new WaitForSeconds(0.15f);
            }
            Assert.IsFalse(hour.IsAlive, "struck down in under sixty blows");
            Assert.AreEqual(1, hour.Phase, "no quarters struck");
            Assert.AreEqual(0, hour.Rewinds, "no rewinding");
            Assert.IsFalse(hour.HandsTurning, "its hands still");
            Assert.AreEqual(false, running, "beaten with time stopped");
            Assert.AreEqual("TIME OUT", hour.Farewell);
            Assert.IsTrue(apprentice.Equipment.Met(LedgerEntry.OneWhoWaits), "written in the Ledger (beaten either way)");
        }

        // Limp, its only blow: a slow twitch of an arm (a long tell), weak.
        [UnityTest]
        public IEnumerator Stopped_ItsTwitch_IsSlowAndWeak()
        {
            apprentice.Teleport(Before(3.5f));
            float health = apprentice.Health;
            yield return Until(() => hour.CurrentState == Soured.State.Telegraph, 6f, "a twitch drawn");
            Assert.GreaterOrEqual(hour.CurrentAttack.telegraph, 1.5f, "slow: a long tell");
            yield return Until(() => apprentice.Health < health, 4f, "the twitch");
            Assert.LessOrEqual(health - apprentice.Health, 8.01f, "weak");
        }

        // Time running: it wakes for real; brought to each mark it strikes the quarter (untouchable a moment) and the
        // next begins: the floor's hands turning from the second, the pendulums swinging from the third.
        [UnityTest]
        public IEnumerator Running_TheQuarters_EachAddsItsMachine()
        {
            Run();
            hour.rewindBurst = hour.lastHourBurst = 9999f; // (not this test's)
            apprentice.Teleport(Before(8.2f));
            yield return Until(() => hour.Woken, 2f, "woken");
            Assert.IsFalse(hour.Limp);
            Assert.IsFalse(hour.HandsTurning, "the first quarter: its hands still");
            int struck = 0;
            TheHour.QuarterStruck += (h, q) => struck = q;
            while (hour.Health > hour.maxHealth * 0.75f + 1f)
                Strike(60f);
            Strike(60f); // (to its mark: no further)
            Assert.AreEqual(hour.maxHealth * 0.75f, hour.Health, 0.5f, "held at the quarter's mark");
            yield return null;
            Assert.IsTrue(hour.Chiming, "striking the quarter");
            float marked = hour.Health;
            Strike(60f);
            Assert.AreEqual(marked, hour.Health, 0.01f, "untouchable meanwhile");
            yield return Until(() => struck == 2, hour.chimeTime + 1f, "the second quarter");
            yield return new WaitForSeconds(0.2f);
            Assert.IsTrue(hour.HandsTurning, "the floor's hands turning");
            foreach (var p in hour.pendulums)
                Assert.IsFalse(p.Running, "the pendulums still held");
            while (hour.Health > hour.maxHealth * 0.5f + 1f && !hour.Chiming)
                Strike(60f);
            yield return Until(() => struck == 3, hour.chimeTime + 1f, "the third quarter");
            yield return new WaitForSeconds(0.2f);
            foreach (var p in hour.pendulums)
                Assert.IsTrue(p.Running, "the pendulums swinging");
        }

        // Too much at once: it winds itself back, mending the burst; strike it as it winds, and it's broken off.
        [UnityTest]
        public IEnumerator Running_Rewind_MendsABurst_UnlessStruckWhileItWinds()
        {
            Run();
            hour.BeginAt(1);
            apprentice.Teleport(Before(8.2f));
            yield return null;
            for (int i = 0; i < 7; i++)
                Strike(15f); // (105 at once)
            float hurt = hour.Health;
            yield return Until(() => hour.Rewinding, 1f, "winding back");
            yield return Until(() => !hour.Rewinding, hour.rewindTime + 0.5f, "wound");
            Assert.AreEqual(1, hour.Rewinds);
            Assert.AreEqual(hour.maxHealth, hour.Health, 1f, "mended: as if it never happened");
            yield return new WaitForSeconds(hour.rewindRest + 0.2f);
            for (int i = 0; i < 7; i++)
                Strike(15f);
            yield return Until(() => hour.Rewinding, 1f, "winding back again");
            yield return new WaitForSeconds(0.3f);
            Strike(14f);
            Assert.IsFalse(hour.Rewinding, "broken off");
            Assert.AreEqual(1, hour.RewindsBroken);
            Assert.Less(hour.Health, hour.maxHealth - 100f, "the burst kept");
            Assert.AreEqual(Soured.State.Staggered, hour.CurrentState, "and reeling");
        }

        // From the third quarter: it stops you in time (held still, drained grey), and brings the hammer down on you.
        [UnityTest]
        public IEnumerator Running_StopYou_HoldsYouStill_ThenTheHammer()
        {
            Run();
            hour.BeginAt(3);
            hour.minuteLength = hour.hourLength = 0f; // (its floor's hands: not this test's)
            apprentice.Teleport(Before(8.2f));        // (beyond its blows; within its reach to stop you)
            float health = apprentice.Health;
            yield return Until(() => hour.Stops >= 1, 12f, "stopped");
            Assert.IsTrue(apprentice.HeldInTime, "held in time");
            Assert.AreEqual(1f, Shader.GetGlobalFloat("_HallHeld"), 0.001f, "drained grey");
            Vector3 held = apprentice.transform.position;
            input.Move = Vector2.right;
            yield return new WaitForSeconds(0.3f);
            Assert.Less(Vector3.Distance(held, apprentice.transform.position), 0.05f, "still, whatever's pressed");
            input.Move = Vector2.zero;
            yield return Until(() => apprentice.Health < health, 1.5f, "the hammer, on you");
            Assert.LessOrEqual(apprentice.Health, health - 30f);
            Assert.IsFalse(apprentice.HeldInTime, "struck back into time");
            Assert.AreEqual(0f, Shader.GetGlobalFloat("_HallHeld"), 0.001f);
        }

        // From the second quarter its floor's hands sweep round, low: standing on the floor you're caught.
        [UnityTest]
        public IEnumerator Running_TheFloorsHands_SweepYou()
        {
            Run();
            hour.BeginAt(2);
            apprentice.Teleport(Before(8.0f));
            yield return Until(() => hour.HandBlows.OnApprentice >= 1, 14f, "a hand's sweep");
        }

        // Beaten with time running: the hour strikes at last (the way on).
        [UnityTest]
        public IEnumerator Running_Beaten_TheHourStrikesAtLast()
        {
            Run();
            hour.BeginAt(4);
            hour.rewindBurst = hour.lastHourBurst = 9999f;
            apprentice.Teleport(Before(8.2f));
            yield return null;
            bool? running = null;
            TheHour.Beaten += (h, r) => running = r;
            for (int i = 0; i < 40 && hour.IsAlive; i++)
            {
                Strike(30f);
                yield return new WaitForSeconds(0.1f);
            }
            Assert.IsFalse(hour.IsAlive);
            Assert.AreEqual(true, running, "beaten with time running");
            Assert.IsTrue(hour.BeatenRunning);
            Assert.AreEqual("TIME'S UP", hour.Farewell);
            Assert.IsTrue(apprentice.Equipment.Met(LedgerEntry.OneWhoWaits), "written in the Ledger");
        }

        // (SHOTS=dir) The Hour: limp in its chamber; awake, poised; the hammer raised; Stop You; the hands sweeping.
        [UnityTest]
        public IEnumerator Capture_TheHour()
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
                apprentice.Teleport(hour.middle + new Vector3(0f, 0.05f, -15.6f), true, true);
                yield return new WaitForSeconds(1.2f);
                Shot("hour-1-from-the-landing");
                apprentice.Teleport(Before(6f), true, true);
                yield return new WaitForSeconds(2.5f);
                Shot("hour-2-limp");
                Run();
                hour.BeginAt(1);
                hour.rewindBurst = hour.lastHourBurst = 9999f;
                apprentice.Teleport(Before(5.2f), true, true);
                yield return new WaitForSeconds(2.5f);
                Shot("hour-3-awake");
                yield return Until(() => hour.CurrentState == Soured.State.Telegraph, 8f, "a blow drawn");
                yield return new WaitForSeconds(hour.CurrentAttack.telegraph * 0.9f);
                Shot("hour-4-" + hour.CurrentAttack.name.ToLowerInvariant() + "-drawn");
                yield return new WaitForSeconds(0.6f);
                Shot("hour-5-" + hour.CurrentAttack.name.ToLowerInvariant() + "-landed");
                hour.BeginAt(3);
                hour.minuteLength = hour.hourLength = 0f;
                apprentice.Teleport(Before(8.2f), true, true);
                yield return Until(() => hour.Stops >= 1, 14f, "stopped");
                yield return new WaitForSeconds(0.2f);
                Shot("hour-6-stopped-in-time");
                hour.minuteLength = 9f;
                hour.hourLength = 6f;
                yield return new WaitForSeconds(3f);
                Shot("hour-7-the-third-quarter");
            }
            finally { cam.targetTexture = null; }
        }
    }
}
