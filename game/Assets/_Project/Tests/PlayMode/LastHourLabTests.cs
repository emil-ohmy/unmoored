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
    // The Hall of the Last Hour's lab (Scenes/Lab_LastHour.unity): its architecture. The keep is climbed floor by floor
    // on foot from the landing (the stairs wind round it), the storeys above the Apprentice are hidden while they're
    // inside, the ledges and the clock's hands outside the face room can be walked, and time has stopped in it (the dust
    // hangs still).
    public class LastHourLabTests
    {
        Apprentice apprentice;
        ScriptedInput input;
        Storeys tower;

        [UnitySetUp]
        public IEnumerator LoadLastHour()
        {
            Application.targetFrameRate = -1;
            Time.captureDeltaTime = 1f / 60f;
            yield return SceneManager.LoadSceneAsync("Lab_LastHour");
            yield return null;
            apprentice = UnityEngine.Object.FindAnyObjectByType<Apprentice>();
            apprentice.Input = input = new ScriptedInput();
            tower = UnityEngine.Object.FindObjectsByType<Storeys>(FindObjectsSortMode.None).First(st => st.name.Contains("keep"));
            yield return new WaitForSeconds(0.3f);
        }

        [TearDown]
        public void ResetTime()
        {
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f;
        }

        // (the tower's middle at the water; a point in it is (east, up, north) from it)
        Vector3 T(float e, float u, float n) => tower.transform.position + new Vector3(e, u, n);

        // Walks towards `to` (straight at it, on the level) until within `within`, or fails after `timeout`.
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

        // From the court, in at the keep's door, up every stair to the belfry, on foot: each storey's floor reached,
        // and while on it the storeys above hidden.
        [UnityTest]
        public IEnumerator TheTower_ClimbedOnFoot_FromTheLandingToTheBelfry()
        {
            float[] floor = { 0.5f, 4.5f, 8.5f, 12.5f, 16.5f };
            // In at the door, across the undercroft to the foot of its stair (the east wall), and up.
            yield return WalkTo(T(0f, 0f, -6.5f));
            Assert.AreEqual(0, tower.Level, "inside, on the undercroft's floor");
            yield return WalkTo(T(6.5f, 0f, -4.4f));
            yield return WalkTo(T(6.5f, 0f, 2.4f), 0.4f, 6f);
            Assert.AreEqual(floor[1], apprentice.transform.position.y, 0.15f, "up on the works");
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(1, tower.Level);
            Assert.IsFalse(tower.Shown(2), "the gallery above hidden");
            // Across the works to its stair (the north wall, rising west), and up.
            yield return WalkTo(T(4.4f, 0f, 3.8f));
            yield return WalkTo(T(4.4f, 0f, 6.5f));
            yield return WalkTo(T(-2.4f, 0f, 6.5f), 0.4f, 6f);
            Assert.AreEqual(floor[2], apprentice.transform.position.y, 0.15f, "up on the gallery");
            // Round the gallery to its stair (the west wall, rising south), and up.
            yield return WalkTo(T(-5.5f, 0f, 6.0f));
            yield return WalkTo(T(-6.5f, 0f, 4.4f));
            yield return WalkTo(T(-6.5f, 0f, -2.4f), 0.4f, 6f);
            Assert.AreEqual(floor[3], apprentice.transform.position.y, 0.15f, "up in the face room");
            // Across the face room to its stair (the east wall, rising north), and up to the belfry.
            yield return WalkTo(T(-4.5f, 0f, -4.5f));
            yield return WalkTo(T(4.5f, 0f, -4.5f));
            yield return WalkTo(T(6.5f, 0f, -4.4f));
            yield return WalkTo(T(6.5f, 0f, 2.4f), 0.4f, 6f);
            Assert.AreEqual(floor[4], apprentice.transform.position.y, 0.15f, "up in the belfry");
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(4, tower.Level);
        }

        // The Stroke: hold the heavy attack till it's charged, let go (facing `towards`, from where they stand).
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

        Clockwork Machine(string name) => UnityEngine.Object.FindObjectsByType<Clockwork>(FindObjectsSortMode.None).First(c => c.name.Contains(name));

        // The great hall's gear door: shut, no way in; the Stroke at its drive gear and it rolls aside a while, then back.
        [UnityTest]
        public IEnumerator TheGearDoor_ShutTillStruck_RollsAsideAWhile_AndBack()
        {
            apprentice.GiveStroke();
            var door = UnityEngine.Object.FindObjectsByType<ClockworkMover>(FindObjectsSortMode.None).First(m => m.name.Contains("Gear Door"));
            var work = Machine("Gear Door");
            yield return WalkTo(T(14.4f, 0f, -12.5f), 0.4f, 10f);
            input.Move = new Vector2(1f, 0f);
            yield return new WaitForSeconds(1f);
            input.Move = Vector2.zero;
            Assert.Less(apprentice.transform.position.x - tower.transform.position.x, 15.4f, "the door shut: no way in");
            yield return WalkTo(T(13.0f, 0f, -9.3f));
            yield return Stroke(Vector3.right);
            Assert.IsTrue(work.Running, "the Stroke at its drive: it runs");
            yield return new WaitForSeconds(2.2f);
            Assert.IsTrue(door.Out, "rolled aside");
            yield return new WaitForSeconds(work.runTime + 2.5f);
            Assert.IsFalse(work.Running);
            Assert.IsTrue(door.Home, "rolled back: shut again");
        }

        // The gear lift: onto its deck, the Stroke at its drive (beyond the column), carried up to the west wall's walk;
        // off onto it; the lift sinks back without you.
        [UnityTest]
        public IEnumerator TheGearLift_RideItUpToTheWallWalk()
        {
            apprentice.GiveStroke();
            var lift = UnityEngine.Object.FindObjectsByType<ClockworkMover>(FindObjectsSortMode.None).First(m => m.carries);
            yield return WalkTo(T(-11.8f, 0f, -21.4f), 0.3f);
            yield return WalkTo(T(-11.8f, 0f, -23.2f), 0.25f);        // onto the deck
            yield return Stroke(Vector3.right);                         // at its drive, beyond the column
            Assert.IsTrue(Machine("Gear Lift").Running);
            yield return new WaitForSeconds(3.0f);
            Assert.AreEqual(5.0f, apprentice.transform.position.y, 0.2f, "carried up to the walk's height");
            yield return WalkTo(T(-13.6f, 0f, -23.2f), 0.3f);         // off onto the walk
            yield return new WaitForSeconds(8f);
            Assert.IsTrue(lift.Home, "the lift back down");
            Assert.AreEqual(5.0f, apprentice.transform.position.y, 0.2f, "and you still up on the wall");
        }

        // The pendulum: the Stroke at its bob (in the undercroft) and the keep's clockwork runs: the pendulum swings, the
        // wheels turn, the hanging dust moves; then it all stops, and hangs still again.
        [UnityTest]
        public IEnumerator ThePendulum_Struck_TheKeepRunsAWhile()
        {
            apprentice.GiveStroke();
            var work = Machine("Keep's Clockwork");
            var pendulum = work.swingers[0].part;
            var wheel = work.spinners[0].part;
            Quaternion swingFrom = pendulum.rotation, wheelFrom = wheel.rotation;
            yield return WalkTo(T(0f, 0f, -6.5f));
            yield return WalkTo(T(-2.7f, 0f, -3.4f));
            yield return Stroke(Vector3.forward);
            Assert.IsTrue(work.Running, "set running");
            float swung = 0f;
            for (float t = 0f; t < 3f; t += Time.deltaTime)
            {
                swung = Mathf.Max(swung, Quaternion.Angle(pendulum.rotation, swingFrom));
                yield return null;
            }
            Assert.Greater(swung, 12f, "the pendulum swinging (out past hanging straight)");
            Assert.Greater(Quaternion.Angle(wheel.rotation, wheelFrom), 10f, "the great wheel turning");
            yield return new WaitForSeconds(work.runTime + work.spinDown + 0.5f);
            Assert.IsFalse(work.Running);
            Assert.AreEqual(0f, work.Run, 0.001f, "stopped");
            Quaternion held = pendulum.rotation;
            yield return new WaitForSeconds(1f);
            Assert.Less(Quaternion.Angle(pendulum.rotation, held), 0.01f, "held where it stopped");
        }

        // Across the court to the great hall's door (the Stroke opens it), in, and down its aisle (under the pendulums)
        // to the orrery; its roof hidden while you're in it.
        [UnityTest]
        public IEnumerator TheGreatHall_FromTheCourt_DownTheAisleToTheOrrery()
        {
            apprentice.GiveStroke();
            var hall = UnityEngine.Object.FindObjectsByType<Storeys>(FindObjectsSortMode.None).First(st => st != tower);
            yield return WalkTo(T(13.0f, 0f, -9.3f), 0.4f, 10f);
            yield return Stroke(Vector3.right);
            yield return new WaitForSeconds(1.6f);
            yield return WalkTo(T(14.4f, 0f, -12.5f), 0.4f, 10f);
            yield return WalkTo(T(19f, 0f, -12.5f));
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(0, hall.Level, "inside the hall");
            Assert.IsFalse(hall.Shown(1), "its roof hidden");
            yield return WalkTo(T(22f, 0f, -12.5f));
            yield return WalkTo(T(22f, 0f, -3.0f), 0.4f, 8f);
            Assert.AreEqual(0.5f, apprentice.transform.position.y, 0.15f, "on its floor, the length of it");
        }

        // Out through the gate's passage (the west wall's) onto the quay in the river.
        [UnityTest]
        public IEnumerator TheGatehouse_OutOntoTheQuay()
        {
            yield return WalkTo(T(-12.5f, 0f, -17f));
            yield return WalkTo(T(-16.5f, 0f, -17f));
            yield return WalkTo(T(-21.5f, 0f, -17f));
            Assert.AreEqual(0.5f, apprentice.transform.position.y, 0.15f, "on the quay");
        }

        // Out of the face room's west door onto the ledge, along the hour hand, over the dial's middle, along the minute
        // hand to the east ledge, and in again by the east door.
        [UnityTest]
        public IEnumerator TheFace_TheLedgesAndTheHands_CanBeWalked()
        {
            apprentice.Teleport(T(-6f, 12.6f, -6.2f));
            yield return new WaitForSeconds(0.3f);
            yield return WalkTo(T(-6f, 0f, -8.7f));      // out of the west door, onto the ledge
            yield return WalkTo(T(-2.9f, 0f, -8.45f));   // along it to the hour hand
            yield return WalkTo(T(0f, 0f, -8.45f));      // along the hand to the dial's middle
            yield return WalkTo(T(3.1f, 0f, -8.45f));    // the minute hand
            yield return WalkTo(T(6f, 0f, -8.7f));       // the east ledge
            yield return WalkTo(T(6f, 0f, -6.2f));       // in by the east door
            Assert.AreEqual(12.5f, apprentice.transform.position.y, 0.15f, "in the face room again, never having fallen");
        }

        // Time has stopped here: the dust hangs in every storey (and stays where it hangs).
        [UnityTest]
        public IEnumerator TimeStopped_TheDustHangsStill()
        {
            var still = UnityEngine.Object.FindObjectsByType<StillAir>(FindObjectsSortMode.None);
            Assert.GreaterOrEqual(still.Length, 5, "in every storey");
            Assert.IsTrue(still.Where(s => s.motes > 0).All(s => s.Count > 50), "dust in each");
            Assert.IsTrue(still.Any(s => s.motes == 0 && s.drops.Length > 20 && s.Count == s.drops.Length), "the fountain's water, held in the air");
            var ps = still.First(s => s.motes > 0).GetComponent<ParticleSystem>();
            var before = new ParticleSystem.Particle[ps.particleCount];
            ps.GetParticles(before);
            yield return new WaitForSeconds(2f);
            var after = new ParticleSystem.Particle[ps.particleCount];
            ps.GetParticles(after);
            Assert.AreEqual(before.Length, after.Length, "none gone");
            Assert.AreEqual(before[0].position, after[0].position, "none moved");
        }

        // (SHOTS=dir) The tower: from the landing, and floor by floor inside, the face's ledge and hands, and from afar.
        [UnityTest]
        public IEnumerator Capture_LastHour()
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
            IEnumerator At(string name, Vector3 at, Vector3 facing, float zoom = 1f)
            {
                apprentice.Teleport(at, true, true);
                apprentice.transform.rotation = Quaternion.LookRotation(facing);
                CameraRig.Instance?.SetZoom(zoom);
                yield return new WaitForSeconds(1.2f);
                Shot(name);
            }
            try
            {
                yield return At("last-hour-0-the-court", T(0f, 0.55f, -20f), Vector3.forward);
                yield return At("last-hour-1-from-afar", T(4f, 0.55f, -18f), Vector3.forward, 2.2f);
                yield return At("last-hour-1b-the-gate-from-the-quay", T(-21.5f, 0.55f, -17f), Vector3.right);
                yield return At("last-hour-1c-the-great-hall", T(22f, 0.55f, -18f), Vector3.forward);
                yield return At("last-hour-1d-the-orrery", T(22f, 0.55f, -5f), Vector3.forward);
                yield return At("last-hour-2-the-undercroft", T(0f, 0.55f, -4.5f), Vector3.forward);
                yield return At("last-hour-3-the-wheel", T(-3f, 0.55f, 1.2f), Vector3.forward);
                yield return At("last-hour-4-the-works", T(0f, 4.55f, -3.5f), Vector3.forward);
                yield return At("last-hour-5-the-gallery", T(-2.5f, 8.55f, -5.8f), Vector3.forward);
                yield return At("last-hour-6-the-face-room", T(2.5f, 12.55f, -3.5f), Vector3.forward);
                yield return At("last-hour-7-the-ledge-and-the-hands", T(-5.5f, 12.55f, -8.7f), Vector3.right);
                yield return At("last-hour-8-the-belfry", T(0f, 16.55f, -3f), Vector3.forward);
                // The machines: the gear door rolled aside, the lift up at the walk, the pendulum swinging.
                apprentice.GiveStroke();
                yield return At("last-hour-9-the-gear-door-shut", T(13.0f, 0.55f, -9.3f), Vector3.right);
                yield return Stroke(Vector3.right);
                yield return new WaitForSeconds(1.8f);
                Shot("last-hour-10-the-gear-door-rolled-aside");
                yield return At("last-hour-11-on-the-lift", T(-11.8f, 0.55f, -23.2f), Vector3.right);
                yield return Stroke(Vector3.right);
                yield return new WaitForSeconds(2.8f);
                Shot("last-hour-12-the-lift-up");
                yield return At("last-hour-13-by-the-pendulum", T(-2.7f, 0.55f, -3.4f), Vector3.forward);
                yield return Stroke(Vector3.forward);
                yield return new WaitForSeconds(1.6f);
                Shot("last-hour-14-the-pendulum-swinging");
            }
            finally { cam.targetTexture = null; }
        }
    }
}
