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
            Assert.GreaterOrEqual(UnityEngine.Object.FindObjectsByType<Watcher>(FindObjectsSortMode.None).Length, 6, "the Kept's eyes in the dark");
            Assert.AreEqual(25, UnityEngine.Object.FindObjectsByType<WallEye>(FindObjectsSortMode.None).Length, "an eye in every socket of the walls");
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

        Kept[] Group(int group) => UnityEngine.Object.FindObjectsByType<Kept>(FindObjectsSortMode.None).Where(x => x.group == group).ToArray();

        static HitResult Strike(Kept s, Vector3 from) => s.TakeHit(new Hit { Damage = 20f, Stagger = 20f, From = from, Knockback = Vector3.zero });

        // The Kept wait in the walls (unseen, untouchable) till you pass; then they tear free together and come.
        [UnityTest]
        public IEnumerator TheKept_WaitInTheWalls_AndTearFreeAsYouPass()
        {
            var pair = Group(1);
            Assert.AreEqual(2, pair.Length, "a pair in the Passage");
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(pair.All(x => !x.Emerged && !x.Targetable), "waiting in the stone");
            apprentice.Teleport(At(2.4f, 18.4f) + Vector3.up * 0.05f);
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(pair.All(x => x.Emerged), "torn free as you came");
            yield return new WaitForSeconds(2.6f);
            Assert.IsTrue(pair.All(x => x.Targetable), "out, and can be struck");
            Assert.IsTrue(pair.All(x => x.CurrentState != Soured.State.Idle), "coming for you");
            Assert.IsFalse(Group(2).Any(x => x.Emerged), "the Gallery's still in their wall");
        }

        // Stood still, one reaches you: the long arm raised, the swipe.
        [UnityTest]
        public IEnumerator OneOfTheKept_Swipes()
        {
            apprentice.Teleport(At(2.4f, 18.4f) + Vector3.up * 0.05f);
            for (float t = 0f; t < 10f && apprentice.Health >= apprentice.maxHealth; t += Time.deltaTime)
                yield return null;
            Assert.Less(apprentice.Health, apprentice.maxHealth, "clawed");
        }

        // Two blows and it's gone back into the stone; a face presses out of the wall nearby.
        [UnityTest]
        public IEnumerator TwoBlows_TakeItBack_AndAFacePressesOutNearby()
        {
            apprentice.Teleport(At(2.4f, 18.4f) + Vector3.up * 0.05f);
            var shade = Group(1)[0];
            for (float t = 0f; t < 4f && !shade.Targetable; t += Time.deltaTime)
                yield return null;
            var near = UnityEngine.Object.FindObjectsByType<WallPresence>(FindObjectsSortMode.None)
                .OrderBy(p => Vector3.Distance(p.transform.position, shade.transform.position)).First();
            for (float t = 0f; t < 8f && near.Out > 0f; t += Time.deltaTime)
                yield return null; // (back in the wall first: pressed out on its own just now, it wouldn't press again)
            int before = near.Presses;
            Strike(shade, apprentice.transform.position);
            Strike(shade, apprentice.transform.position);
            Assert.IsFalse(shade.IsAlive, "two blows");
            yield return new WaitForSeconds(shade.deathDuration + 0.5f);
            Assert.Greater(near.Presses, before, "a face pressed out of the wall");
            Assert.IsTrue(apprentice.Equipment.Met(LedgerEntry.TheKept), "written in the Ledger");
        }

        // At a Rest they're back in the walls, waiting again.
        [UnityTest]
        public IEnumerator AtARest_TheKeptAreBackInTheWalls()
        {
            apprentice.Teleport(At(2.4f, 18.4f) + Vector3.up * 0.05f);
            yield return new WaitForSeconds(3f);
            Assert.IsTrue(Group(1).All(x => x.Emerged));
            apprentice.Teleport(At(-6f, 53.5f) + Vector3.up * 0.05f); // (resting at the Mooring post, far from them)
            yield return null;
            WorldState.Instance.ReformAllSoured();
            yield return null;
            Assert.IsTrue(Group(1).All(x => !x.Emerged && !x.Targetable), "back in the stone");
        }

        [UnityTest]
        public IEnumerator Drips_FallOutOfTheDark()
        {
            var drips = UnityEngine.Object.FindAnyObjectByType<Drips>();
            Assert.IsNotNull(drips);
            yield return new WaitForSeconds(8f); // (they fall at random: long enough to be sure of a few)
            Assert.Greater(drips.Fallen, 3, "drops falling, near you");
        }

        // ---- The Hold, That Would Not Let Go (its Boss, at the way out)

        TheHoldWall Boss => UnityEngine.Object.FindAnyObjectByType<TheHoldWall>();

        IEnumerator IntoTheChamber()
        {
            apprentice.postHitInvulnerability = 1000f; // (struck once, then left alone: the tests watch it, not you)
            apprentice.Teleport(Boss.arenaCentre + Vector3.back * 3f + Vector3.up * 0.05f);
            for (float t = 0f; t < 8f && Boss.State != TheHoldWall.Mood.Fighting; t += Time.deltaTime)
                yield return null;
            Assert.AreEqual(TheHoldWall.Mood.Fighting, Boss.State, "awake");
        }

        static void Strike(HoldPart part, float damage, Vector3 from) =>
            part.TakeHit(new Hit { Damage = damage, Stagger = 0f, From = from, Knockback = Vector3.zero });

        // Asleep in the wall till you come into its chamber; then it wakes, and the fight's on.
        [UnityTest]
        public IEnumerator TheHold_SleepsTillYouCome_ThenWakes()
        {
            var boss = Boss;
            Assert.IsNotNull(boss, "in the chamber's north wall");
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(TheHoldWall.Mood.Dormant, boss.State, "asleep");
            Assert.IsTrue(boss.wayOut.enabled, "the way out blocked");
            yield return IntoTheChamber();
            Assert.AreEqual(boss, BossFight.Current, "its name plate and bar");
        }

        // A hand slams down where you stand, then lies stunned: struck there, it hurts the whole.
        [UnityTest]
        public IEnumerator TheHold_AHandSlams_LiesStunned_AndStrikingItHurtsTheWhole()
        {
            yield return IntoTheChamber();
            var boss = Boss;
            HoldPart open = null;
            for (float t = 0f; t < 20f && open == null; t += Time.deltaTime)
            {
                open = boss.handL.Open ? boss.handL : boss.handR.Open ? boss.handR : null;
                yield return null;
            }
            Assert.IsNotNull(open, "a hand lying stunned after a slam");
            Assert.Greater(boss.Slams, 0);
            Assert.IsTrue(open.Targetable, "in reach");
            Strike(open, 20f, apprentice.transform.position);
            Assert.Less(boss.HealthFraction, 1f, "it hurt the whole");
        }

        // Wound both hands and they withdraw into the wall; then the face sags down into reach, and takes harm doubly.
        [UnityTest]
        public IEnumerator TheHold_BothHandsWithdrawn_TheFaceSags_AndItsBlowsCountDouble()
        {
            yield return IntoTheChamber();
            var boss = Boss;
            for (float t = 0f; t < 45f && !(boss.Withdrawn(true) && boss.Withdrawn(false)); t += Time.deltaTime)
            {
                if (boss.handL.Open) Strike(boss.handL, boss.handWound, apprentice.transform.position);
                if (boss.handR.Open) Strike(boss.handR, boss.handWound, apprentice.transform.position);
                yield return null;
            }
            Assert.IsTrue(boss.Withdrawn(true) && boss.Withdrawn(false), "both hands back in the wall");
            for (float t = 0f; t < 4f && !boss.Sagged; t += Time.deltaTime)
                yield return null;
            Assert.IsTrue(boss.Sagged, "the face sagged into reach");
            float was = boss.HealthFraction;
            Strike(boss.face, 20f, apprentice.transform.position);
            Assert.AreEqual(was - 40f / boss.maxHealth, boss.HealthFraction, 0.001f, "doubly");
        }

        // Beaten, it lets go: the way out opens, and it's in the Ledger.
        [UnityTest]
        public IEnumerator TheHold_Beaten_LetsGo_AndTheWayOutOpens()
        {
            yield return IntoTheChamber();
            var boss = Boss;
            for (float t = 0f; t < 45f && boss.IsAlive; t += Time.deltaTime)
            {
                foreach (var part in new[] { boss.handL, boss.handR, boss.face })
                    if (part.Open)
                        Strike(part, 200f, apprentice.transform.position);
                yield return null;
            }
            Assert.IsFalse(boss.IsAlive, "beaten");
            Assert.AreEqual("LET GO", boss.Farewell);
            yield return new WaitForSeconds(5.5f);
            Assert.IsFalse(boss.wayOut.enabled, "its mouth open on the way out");
            Assert.AreEqual(TheHoldWall.Mood.Gone, boss.State);
            Assert.IsTrue(apprentice.Equipment.Met(LedgerEntry.TheHold), "written in the Ledger");
        }

        // Beaten, its mouth is open on the way out: you walk out through it, up the tunnel behind, to the daylight.
        [UnityTest]
        public IEnumerator TheHold_Beaten_YouWalkOutThroughItsMouth()
        {
            yield return IntoTheChamber();
            var boss = Boss;
            for (float t = 0f; t < 60f && boss.IsAlive; t += Time.deltaTime)
            {
                foreach (var part in new[] { boss.handL, boss.handR, boss.face })
                    if (part.Open)
                        Strike(part, 200f, apprentice.transform.position);
                if (Vector3.Distance(apprentice.transform.position, boss.arenaCentre + Vector3.back * 3f) > 2f)
                    apprentice.Teleport(boss.arenaCentre + Vector3.back * 3f + Vector3.up * 0.05f); // (stood still: the test only strikes)
                yield return null;
            }
            Assert.IsFalse(boss.IsAlive, "beaten");
            for (float t = 0f; t < 8f && boss.State != TheHoldWall.Mood.Gone; t += Time.deltaTime)
                yield return null;
            var input = (ScriptedInput)apprentice.Input;
            float mouthE = boss.transform.position.x;
            Vector3 last = apprentice.transform.position;
            float stuck = 0f;
            for (float t = 0f; t < 20f && apprentice.transform.position.z < 98f; t += Time.deltaTime)
            {
                Vector3 p = apprentice.transform.position;
                input.Move = new Vector2(Mathf.Clamp((mouthE - p.x) * 0.8f, -0.7f, 0.7f), 1f).normalized; // (up the middle)
                stuck = (p - last).magnitude < 0.002f ? stuck + Time.deltaTime : 0f;
                last = p;
                if (stuck > 1f)
                {
                    var from = p + Vector3.up;
                    string what = Physics.SphereCast(from, 0.3f, Vector3.forward, out var hit, 2f) ? $"{hit.collider.name} at {hit.point}" : "nothing ahead";
                    Assert.Fail($"stuck at {p}: {what}");
                }
                yield return null;
            }
            input.Move = Vector2.zero;
            Assert.GreaterOrEqual(apprentice.transform.position.z, 97.9f, $"out through its mouth, up to the daylight (got to {apprentice.transform.position})");
        }

        // Its hands and its face can be locked on to while they're out of the wall, not only while they're in reach; a
        // hand withdrawn into the wall lets the lock go on to another part.
        [UnityTest]
        public IEnumerator TheHold_ItsHandsAndFace_CanBeLockedOn()
        {
            yield return IntoTheChamber();
            var boss = Boss;
            var input = (ScriptedInput)apprentice.Input;
            input.PressLock();
            yield return null;
            yield return null;
            Assert.IsInstanceOf<HoldPart>(apprentice.LockTarget, "locked on to one of its parts");
            Assert.IsFalse(((HoldPart)apprentice.LockTarget).Open, "(not only one lying stunned)");
            var first = apprentice.LockTarget;
            input.Switch(1);
            yield return null;
            yield return null;
            if (apprentice.LockTarget == first)
            {
                input.Switch(-1);
                yield return null;
                yield return null;
            }
            Assert.IsInstanceOf<HoldPart>(apprentice.LockTarget, "on to the next");
            Assert.AreNotEqual(first, apprentice.LockTarget, "another part");
            // Both hands driven back into the wall: the lock moves on, to the face.
            for (float t = 0f; t < 45f && !(boss.Withdrawn(true) && boss.Withdrawn(false)); t += Time.deltaTime)
            {
                if (boss.handL.Open) Strike(boss.handL, boss.handWound, apprentice.transform.position);
                if (boss.handR.Open) Strike(boss.handR, boss.handWound, apprentice.transform.position);
                yield return null;
            }
            Assert.IsTrue(boss.Withdrawn(true) && boss.Withdrawn(false), "both hands back in the wall");
            Assert.IsFalse(boss.handL.Lockable || boss.handR.Lockable, "nothing to lock on to there");
            Assert.AreEqual(boss.face, apprentice.LockTarget, "the lock gone on to the face");
        }

        // Every swing of its hands (a slam, a sweep) draws a swipe from its fingertips (CLAUDE.md: every blow in reach).
        [UnityTest]
        public IEnumerator TheHold_EverySlamAndSweep_DrawsItsSwipe()
        {
            yield return IntoTheChamber();
            var boss = Boss;
            int slams = boss.Slams, sweeps = boss.Sweeps, drawn = 0, blows = 0;
            for (float t = 0f; t < 40f && (boss.Slams - slams < 2 || boss.Sweeps - sweeps < 1); t += Time.deltaTime)
            {
                int now = boss.Slams + boss.Sweeps - slams - sweeps;
                if (now != blows)
                {
                    blows = now; // (just struck: its swipe still fading behind it)
                    if (boss.SwipeSamples >= 2) drawn++;
                }
                yield return null;
            }
            Assert.GreaterOrEqual(boss.Slams - slams, 2, "it slammed");
            Assert.GreaterOrEqual(boss.Sweeps - sweeps, 1, "it swept");
            Assert.AreEqual(blows, drawn, "a swipe through every one");
        }

        // The fight as it's played: locked on, the oar's light blows on whatever lies in reach, till it lets go; then it's
        // in the Ledger. (Its length: the first Boss is quick to beat.)
        [UnityTest]
        public IEnumerator TheHold_FoughtWithTheOar_IsBeaten_AndWrittenInTheLedger()
        {
            yield return IntoTheChamber();
            var boss = Boss;
            var input = (ScriptedInput)apprentice.Input;
            float began = Time.time, nextSwing = 0f;
            int swings = 0;
            Vector3 waitAt = boss.arenaCentre + Vector3.back * 3f;
            while (boss.IsAlive && Time.time - began < 240f)
            {
                var open = boss.handL.Open ? boss.handL : boss.handR.Open ? boss.handR : boss.face.Open ? boss.face : null;
                Vector3 goal = open != null ? open.transform.position : waitAt;
                Vector3 to = goal - apprentice.transform.position;
                to.y = 0f;
                if (open != null && to.magnitude < 1.8f)
                {
                    input.Move = new Vector2(to.x, to.z).normalized * 0.2f;
                    if (Time.time >= nextSwing)
                    {
                        input.PressLight();
                        swings++;
                        nextSwing = Time.time + 0.4f;
                    }
                }
                else
                    input.Move = to.magnitude > 0.5f ? new Vector2(to.x, to.z).normalized : Vector2.zero;
                yield return null;
            }
            input.Move = Vector2.zero;
            Debug.Log($"[the hold] beaten in {Time.time - began:0.0} s, {swings} swings");
            Assert.IsFalse(boss.IsAlive, $"beaten (at {boss.HealthFraction:P0} after {Time.time - began:0} s)");
            Assert.Less(Time.time - began, 150f, "a quick fight: the first Boss");
            yield return new WaitForSeconds(6.5f);
            Assert.AreEqual(TheHoldWall.Mood.Gone, boss.State);
            Assert.IsTrue(apprentice.Equipment.Met(LedgerEntry.TheHold), "written in the Ledger");
            yield return null;
            Assert.AreEqual("Written in the Ledger: the Hold, That Would Not Let Go", UnityEngine.Object.FindAnyObjectByType<Hud>().LastWritten,
                "said so, after its farewell");
            // Its page, in the Ledger (the Kept's too, if any were scattered on the way: none here).
            var screen = UnityEngine.Object.FindAnyObjectByType<CharacterScreen>();
            screen.Toggle(true);
            while (screen.Current != CharacterScreen.Tab.Ledger)
                screen.Turn(1);
            yield return null;
            CollectionAssert.Contains(screen.LedgerRows, LedgerEntry.TheHold.Name);
            StringAssert.Contains(LedgerEntry.TheHold.Description, screen.LedgerDetail);
            StringAssert.Contains("Beaten.", screen.LedgerDetail);
            Assert.IsNotNull(screen.ShownModel, "it, turning");
            screen.Toggle(false);
        }

        // (SHOTS=dir) The fight from the game camera: waking, the hands, a slam and the stunned hand, the face sagged.
        [UnityTest]
        public IEnumerator Capture_TheHoldFight()
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
                File.WriteAllBytes(Path.Combine(outDir, $"fight-{name}.png"), tex.EncodeToPNG());
                cam.targetTexture = null;
                RenderTexture.active = null;
                UnityEngine.Object.Destroy(tex);
                rt.Release();
            }
            var boss = Boss;
            apprentice.postHitInvulnerability = 1000f;
            apprentice.Teleport(boss.arenaCentre + Vector3.back * 2f + Vector3.up * 0.05f);
            yield return new WaitForSeconds(2f);
            Shot("1-waking");
            yield return new WaitForSeconds(2.5f);
            Shot("2-awake");
            int slams = boss.Slams;
            for (float t = 0f; t < 15f && boss.Slams == slams; t += Time.deltaTime)
                yield return null;
            Shot("3-slam");
            for (float t = 0f; t < 20f && !boss.trails.Any(w => w.Emitting && (w.For("SlamL") || w.For("SlamR")) && w.Samples >= 9); t += Time.deltaTime)
                yield return null;
            Shot("3b-slamming");
            yield return new WaitForSeconds(0.5f);
            Shot("4-stunned");
            int sweeps = boss.Sweeps;
            for (float t = 0f; t < 40f && !boss.trails.Any(w => w.Emitting && (w.For("SweepL") || w.For("SweepR")) && w.Samples >= 12); t += Time.deltaTime)
                yield return null;
            Shot("4b-sweep");
            for (float t = 0f; t < 45f && !(boss.Withdrawn(true) && boss.Withdrawn(false)); t += Time.deltaTime)
            {
                if (boss.handL.Open) Strike(boss.handL, boss.handWound, apprentice.transform.position);
                if (boss.handR.Open) Strike(boss.handR, boss.handWound, apprentice.transform.position);
                yield return null;
            }
            yield return new WaitForSeconds(2.5f);
            Shot("5-sagged");
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
                                               ("9-crack", At(-2.4f, 68.6f, 6f)), ("10-chamber", At(-1f, 78f, 6f)) })
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
