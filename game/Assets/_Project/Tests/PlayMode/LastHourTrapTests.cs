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
    // The Hall of the Last Hour's traps (Scenes/Lab_LastHour.unity): the great hall's pendulum blades, the Gear Run's
    // rolling gear, the bell gallery's clock jacks, the undercroft's grinding floor. Held harmless while time is
    // stopped; set going a while by the Stroke, and for good with the Hall's time; they hit hard, whoever they catch.
    public class LastHourTrapTests
    {
        Apprentice apprentice;
        ScriptedInput input;
        Transform keep;

        [UnitySetUp]
        public IEnumerator LoadLastHour()
        {
            Application.targetFrameRate = -1;
            Time.captureDeltaTime = 1f / 60f;
            yield return SceneManager.LoadSceneAsync("Lab_LastHour");
            yield return null;
            apprentice = UnityEngine.Object.FindAnyObjectByType<Apprentice>();
            apprentice.Input = input = new ScriptedInput();
            keep = UnityEngine.Object.FindObjectsByType<Storeys>(FindObjectsSortMode.None).First(st => st.name.Contains("keep")).transform;
            // The household keeps out of the way (one Punctual stays for a test of the Soured caught in a trap).
            bool soured = TestContext.CurrentContext.Test.Name.StartsWith("Soured");
            foreach (var kind in new[] { typeof(Punctual), typeof(Wound), typeof(Keener) })
            {
                var household = UnityEngine.Object.FindObjectsByType(kind, FindObjectsSortMode.None).Cast<Component>().OrderBy(c => c.transform.position.x).ToArray();
                for (int i = soured && kind == typeof(Punctual) ? 1 : 0; i < household.Length; i++)
                    UnityEngine.Object.Destroy(household[i].gameObject);
            }
            yield return new WaitForSeconds(0.3f);
        }

        [TearDown]
        public void ResetTime()
        {
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f;
        }

        Vector3 T(float e, float u, float n) => keep.position + new Vector3(e, u, n);

        IEnumerator WalkTo(Vector3 to, float within = 0.4f, float timeout = 8f)
        {
            for (float t = 0f; t < timeout; t += Time.deltaTime)
            {
                Vector3 d = to - apprentice.transform.position;
                d.y = 0f;
                if (d.magnitude <= within)
                {
                    input.Move = Vector2.zero;
                    yield break;
                }
                input.Move = new Vector2(d.x, d.z).normalized;
                yield return null;
            }
            input.Move = Vector2.zero;
            Assert.Fail($"couldn't walk to {to} (stuck at {apprentice.transform.position})");
        }

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

        static IEnumerator Until(Func<bool> done, float timeout, string what)
        {
            for (float t = 0f; t < timeout && !done(); t += Time.deltaTime)
                yield return null;
            Assert.IsTrue(done(), "timed out waiting for " + what);
        }

        PendulumBlade Blade(float n) => UnityEngine.Object.FindObjectsByType<PendulumBlade>(FindObjectsSortMode.None).OrderBy(b => Mathf.Abs(b.transform.position.z - (keep.position.z + n))).First();
        RollingGear Gear => UnityEngine.Object.FindAnyObjectByType<RollingGear>();
        ClockJack[] Jacks => UnityEngine.Object.FindObjectsByType<ClockJack>(FindObjectsSortMode.None).OrderBy(j => j.beat).ToArray();
        GrindingGears Floor => UnityEngine.Object.FindAnyObjectByType<GrindingGears>();

        // The Gear Run's floor (last_hour.py's run_ground).
        float RunGround(float n) => Gear.Ground(keep.position.z + n);

        // Time stopped: every trap held where the clock stopped it, and nothing they do (standing on a jack's strip,
        // under a blade, on the floor gears).
        [UnityTest]
        public IEnumerator TimeStopped_TheTrapsAreHeld_AndHarmless()
        {
            var blades = UnityEngine.Object.FindObjectsByType<PendulumBlade>(FindObjectsSortMode.None);
            Assert.AreEqual(3, blades.Length, "the great hall's three pendulums, armed");
            var held = blades.Select(b => b.transform.rotation).ToArray();
            float gearAt = Gear.N;
            Assert.AreEqual(RollingGear.Phase.Rolling, Gear.Current, "the gear caught rolling");
            Assert.IsTrue(Jacks[0].Raised, "a jack caught with its hammer high");
            float health = apprentice.Health;
            apprentice.Teleport(Jacks[0].strip + Vector3.up * 0.05f);
            yield return new WaitForSeconds(2.5f);
            apprentice.Teleport(T(22f, 0.55f, -12f));
            yield return new WaitForSeconds(1.5f);
            apprentice.Teleport(T(1.4f, 0.55f, -5f));
            Vector3 on = apprentice.transform.position;
            yield return new WaitForSeconds(2f);
            Assert.Less(Vector3.Distance(on, apprentice.transform.position), 0.05f, "the floor gears still");
            Assert.AreEqual(health, apprentice.Health, 0.01f, "no harm");
            for (int i = 0; i < blades.Length; i++)
                Assert.Less(Quaternion.Angle(held[i], blades[i].transform.rotation), 0.01f, "a blade held mid-swing");
            Assert.AreEqual(gearAt, Gear.N, 0.001f, "the gear held mid-run");
            Assert.IsTrue(Jacks.All(j => j.Strikes == 0), "no jack strikes");
        }

        // A pendulum set swinging: standing in its way at the bottom of its swing, you're cut down (a third of you) and
        // thrown along the swing.
        [UnityTest]
        public IEnumerator ThePendulumBlade_Swinging_CutsYouDown_AndThrowsYouAlongTheSwing()
        {
            var blade = Blade(-12f);
            blade.clockwork.Set();
            apprentice.Teleport(T(22f, 0.55f, -12f));
            float health = apprentice.Health;
            yield return Until(() => blade.Blows.OnApprentice >= 1, 5f, "the blade");
            Assert.LessOrEqual(apprentice.Health, health - 30f, "cut down: a third of you");
            float struckAt = apprentice.transform.position.x;
            yield return new WaitForSeconds(0.5f);
            Assert.Greater(Mathf.Abs(apprentice.transform.position.x - struckAt), 1.2f, "thrown along the swing (east or west)");
            Assert.Greater(blade.Swishes, 0, "heard through the bottom of its swing");
        }

        // The Gear Run's gear, caught mid-run, set rolling by the Stroke at it (from the alcove beside it, out of its
        // way): down past you into the buffers, drawn back up, held at the head as its clockwork runs down.
        [UnityTest]
        public IEnumerator TheGearRun_TheStroke_SetsTheGearRolling_IntoTheBuffers_ThenDrawnBackUp()
        {
            var gear = Gear;
            apprentice.GiveStroke();
            apprentice.Teleport(T(12.9f, RunGround(-2.4f) + 0.05f, -2.4f));
            yield return new WaitForSeconds(0.3f);
            float health = apprentice.Health;
            Vector3 at = gear.strikePoint.position - apprentice.transform.position;
            yield return Stroke(at);
            Assert.IsTrue(gear.clockwork.Running, "set going");
            yield return Until(() => gear.Crashes >= 1, 4f, "the crash into the buffers");
            Assert.AreEqual(health, apprentice.Health, 0.01f, "it rolled past the alcove");
            yield return Until(() => gear.Current == RollingGear.Phase.Held, 9f, "drawn back up to the head");
            yield return new WaitForSeconds(3.5f);
            Assert.IsFalse(gear.clockwork.Running, "its clockwork run down");
            Assert.AreEqual(RollingGear.Phase.Held, gear.Current, "held at the head");
            Assert.AreEqual(gear.headN, gear.N, 0.01f);
        }

        // Rolling, it flattens what's in the lane below it.
        [UnityTest]
        public IEnumerator TheGearRun_Rolling_FlattensWhatsInTheLane()
        {
            var gear = Gear;
            apprentice.Teleport(T(11.2f, RunGround(-3.8f) + 0.05f, -3.8f));
            yield return new WaitForSeconds(0.2f);
            float health = apprentice.Health;
            gear.clockwork.Set();
            yield return Until(() => gear.Blows.OnApprentice >= 1, 3f, "flattened");
            Assert.LessOrEqual(apprentice.Health, health - 35f);
        }

        // With the gear held at its head: in through the curtain's arch, up the run behind it, round it through the
        // last alcove onto the landing, and in at the keep's door to the works.
        [UnityTest]
        public IEnumerator TheGearRun_UpItRoundTheHeldGear_IntoTheKeepsWorks()
        {
            var gear = Gear;
            gear.clockwork.Set();
            yield return Until(() => gear.Current == RollingGear.Phase.Held, 12f, "the gear at the head");
            yield return Until(() => !gear.clockwork.Running && gear.clockwork.Run <= 0f, 6f, "stopped");
            apprentice.Teleport(T(13.7f, 0.55f, -10f));
            yield return new WaitForSeconds(0.3f);
            yield return WalkTo(T(13.7f, 0f, -6.0f));                   // through the arch, into the bay
            yield return WalkTo(T(11.2f, 0f, -5.2f));                   // the run's foot
            yield return WalkTo(T(11.2f, 0f, 1.0f), 0.4f, 8f);          // up the lane, behind the gear
            yield return WalkTo(T(12.9f, 0f, 2.0f));                    // into the last alcove
            yield return WalkTo(T(12.95f, 0f, 3.9f));                   // up it, onto the landing
            yield return WalkTo(T(13.5f, 0f, 4.6f));                    // (round the gear's house)
            yield return WalkTo(T(13.5f, 0f, 5.8f));
            yield return WalkTo(T(9.6f, 0f, 5.8f));                     // round behind the held gear
            Assert.AreEqual(4.5f, apprentice.transform.position.y, 0.15f, "on the landing, at the works' height");
            yield return WalkTo(T(8.7f, 0f, 4.9f));
            yield return WalkTo(T(6.0f, 0f, 4.9f));                     // in at the keep's door
            Assert.AreEqual(4.5f, apprentice.transform.position.y, 0.15f, "in the works");
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(1, keep.GetComponent<Storeys>().Level, "the works' storey");
        }

        // The jacks, time going: each strikes once a bar on its own beat (the first on the bar's first, the second on
        // its third); what's on its strip as the hammer falls is struck; beside it, nothing.
        [UnityTest]
        public IEnumerator TheClockJacks_TimeGoing_StrikeOnTheirBeats_WhatsOnTheStripIsStruck()
        {
            var jacks = Jacks;
            jacks[0].clockwork.RunForever();
            yield return Until(() => jacks[0].Strikes >= 1, 5f, "falling into step (a blow)");
            apprentice.Teleport(jacks[0].strip + Vector3.up * 0.05f);
            float health = apprentice.Health;
            yield return Until(() => jacks[0].Strikes >= 2, 5f, "the first jack's blow, in step");
            float beat = Mathf.Repeat((float)HallClock.Now, ClockJack.Round);
            Assert.That(beat < 0.1f || beat > ClockJack.Round - 0.1f, $"on the bar's first beat (at {beat:0.00})");
            Assert.GreaterOrEqual(jacks[0].Blows.OnApprentice, 1, "struck");
            Assert.LessOrEqual(apprentice.Health, health - 30f);
            yield return new WaitForSeconds(1f);
            apprentice.Teleport(T(-4.0f, 8.55f, 5.3f)); // (between the strips)
            int hits = jacks[0].Blows.OnApprentice + jacks[1].Blows.OnApprentice;
            int struck = jacks[1].Strikes;
            yield return Until(() => jacks[1].Strikes > struck, 4.5f, "the second jack's blow");
            float second = Mathf.Repeat((float)HallClock.Now - 2f, ClockJack.Round);
            Assert.That(second < 0.1f || second > ClockJack.Round - 0.1f, $"on the bar's third beat (at {second:0.00})");
            yield return new WaitForSeconds(4.5f);
            Assert.AreEqual(hits, jacks[0].Blows.OnApprentice + jacks[1].Blows.OnApprentice, "beside the strips: untouched");
        }

        // Stopped, the jacks are held; the Stroke at their plate sets them striking a while.
        [UnityTest]
        public IEnumerator TheClockJacks_TheStroke_AtTheirPlate_SetsThemStriking()
        {
            var jacks = Jacks;
            apprentice.GiveStroke();
            yield return new WaitForSeconds(2f);
            Assert.IsTrue(jacks.All(j => j.Strikes == 0), "held");
            apprentice.Teleport(jacks[0].clockwork.strikePoint.position + Vector3.up * 0.05f);
            yield return new WaitForSeconds(0.3f);
            yield return Stroke(Vector3.back);
            Assert.IsTrue(jacks[0].clockwork.Running);
            yield return Until(() => jacks.Sum(j => j.Strikes) >= 2, 6f, "both striking");
        }

        // The keep running: whoever stands on a floor gear is carried round with it, and into the mesh: crushed.
        [UnityTest]
        public IEnumerator TheGrindingFloor_CarriesYouRound_IntoTheMesh()
        {
            var floor = Floor;
            floor.clockwork.RunForever();
            apprentice.Teleport(T(1.4f, 0.55f, -5f));
            float health = apprentice.Health;
            yield return Until(() => floor.Blows.OnApprentice >= 1, 10f, "the mesh");
            Assert.Greater(floor.Carried, 60, "carried round to it");
            Assert.LessOrEqual(apprentice.Health, health - 30f, "crushed");
        }

        // A Soured lured into a trap is caught just as you'd be: a Punctual standing in a blade's way is cut down.
        [UnityTest]
        public IEnumerator Soured_InABladesWay_IsCutDownToo()
        {
            var punctual = UnityEngine.Object.FindAnyObjectByType<Punctual>();
            Assert.IsNotNull(punctual);
            var controller = punctual.GetComponent<CharacterController>();
            controller.enabled = false;
            punctual.transform.position = T(22f, 0.55f, -12f);
            controller.enabled = true;
            apprentice.Teleport(T(-10.5f, 0.55f, -17f)); // (far off: it keeps still)
            var blade = Blade(-12f);
            blade.clockwork.Set();
            float health = punctual.Health;
            yield return Until(() => blade.Blows.OnSoured >= 1, 5f, "the blade, on the Punctual");
            Assert.Less(punctual.Health, health, "cut");
        }

        // The Hall's time going for good: every trap keeps going on its own (the blades swinging, the gear let go again
        // and again, the jacks striking).
        [UnityTest]
        public IEnumerator TheHallsTime_Going_EveryTrapGoesForGood()
        {
            foreach (var c in UnityEngine.Object.FindObjectsByType<Clockwork>(FindObjectsSortMode.None))
                c.RunForever();
            apprentice.Teleport(T(-10.5f, 0.55f, -17f));
            float spun = 0f;
            Quaternion was = Floor.gears[0].part.rotation;
            for (float t = 0f; t < 16f; t += Time.deltaTime)
            {
                spun += Quaternion.Angle(was, Floor.gears[0].part.rotation);
                was = Floor.gears[0].part.rotation;
                yield return null;
            }
            foreach (var b in UnityEngine.Object.FindObjectsByType<PendulumBlade>(FindObjectsSortMode.None))
                Assert.GreaterOrEqual(b.Swishes, 6, "a blade swinging on");
            Assert.GreaterOrEqual(Gear.Crashes, 1, "the gear down into the buffers");
            Assert.GreaterOrEqual(Gear.Releases, 1, "...and let go again");
            Assert.IsTrue(Jacks.All(j => j.Strikes >= 3), "the jacks striking every bar");
            Assert.Greater(spun, 300f, "the floor gears turning");
        }

        // (SHOTS=dir) The traps, held and going: the blades in the great hall, the Gear Run, the jacks, the floor gears.
        [UnityTest]
        public IEnumerator Capture_Traps()
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
            IEnumerator At(string name, Vector3 at, Vector3 facing, float zoom = 1f, float wait = 1.2f)
            {
                apprentice.Teleport(at, true, true);
                apprentice.transform.rotation = Quaternion.LookRotation(facing);
                CameraRig.Instance?.SetZoom(zoom);
                yield return new WaitForSeconds(wait);
                Shot(name);
            }
            try
            {
                apprentice.GiveStroke();
                yield return At("traps-1-the-blades-held", T(22f, 0.55f, -16f), Vector3.forward);
                yield return At("traps-2-the-gear-run-from-the-court", T(13.7f, 0.55f, -10.5f), Vector3.forward, 1.6f);
                yield return At("traps-3-the-gear-caught-mid-run", T(11.2f, RunGround(-4f) + 0.05f, -4f), Vector3.forward);
                yield return At("traps-4-the-run-from-its-head", T(12.9f, RunGround(3.2f) + 0.05f, 3.2f), Vector3.back);
                yield return At("traps-5-the-jacks-held", T(-1.6f, 8.55f, 5.2f), Vector3.left);
                yield return At("traps-6-the-floor-gears", T(3.2f, 0.55f, -1.0f), Vector3.left);
                foreach (var c in UnityEngine.Object.FindObjectsByType<Clockwork>(FindObjectsSortMode.None))
                    c.RunForever();
                HallTime.Instance?.RunAtOnce();
                yield return At("traps-7-the-blades-swinging", T(22f, 0.55f, -16f), Vector3.forward, 1f, 1.9f);
                yield return Until(() => Gear.Current == RollingGear.Phase.Rolling && Gear.Speed > 4f, 20f, "the gear rolling");
                apprentice.Teleport(T(12.9f, RunGround(-2.4f) + 0.05f, -2.4f), true, true);
                yield return new WaitForSeconds(0.25f);
                Shot("traps-8-the-gear-rolling-past-an-alcove");
                apprentice.Teleport(T(-1.6f, 8.55f, 5.2f), true, true);
                apprentice.transform.rotation = Quaternion.LookRotation(Vector3.left);
                yield return new WaitForSeconds(1.5f); // (the storeys above faded out)
                yield return Until(() => Jacks[0].Cycle > 2.5f, 6f, "held high");
                Shot("traps-9a-a-jack-wound-up");
                yield return Until(() => Jacks[0].Cycle < 0.3f, 6f, "the blow");
                Shot("traps-9b-a-jack-striking");
                yield return At("traps-10-the-floor-grinding", T(3.2f, 0.55f, -1.0f), Vector3.left);
            }
            finally { cam.targetTexture = null; }
        }
    }
}
