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
            // The household keeps out of every test but its own kind's (one of each pair, then, on its own).
            foreach (var kind in new[] { typeof(Punctual), typeof(Wound), typeof(Keener) })
            {
                var household = UnityEngine.Object.FindObjectsByType(kind, FindObjectsSortMode.None).Cast<Component>().OrderBy(c => c.transform.position.x).ToArray();
                bool theirs = TestContext.CurrentContext.Test.Name.StartsWith(kind.Name);
                for (int i = theirs ? 1 : 0; i < household.Length; i++)
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

        [UnityTest]
        public IEnumerator TheGearDoor_ShutBehindYou_StruckFromInside_LetsYouOut()
        {
            apprentice.GiveStroke();
            var door = UnityEngine.Object.FindObjectsByType<ClockworkMover>(FindObjectsSortMode.None).First(m => m.name.Contains("Gear Door"));
            var work = Machine("Gear Door");
            yield return WalkTo(T(13.0f, 0f, -9.3f), 0.4f, 10f);
            yield return Stroke(Vector3.right);
            yield return new WaitForSeconds(2.2f);
            yield return WalkTo(T(14.4f, 0f, -12.5f), 0.4f, 10f);
            yield return WalkTo(T(19.4f, 0f, -12.5f));
            yield return new WaitForSeconds(work.runTime + 2.5f);
            Assert.IsTrue(door.Home, "shut behind you");
            yield return WalkTo(T(19.4f, 0f, -9.6f));
            yield return Stroke(Vector3.left);
            Assert.IsTrue(work.Running, "the Stroke at its drive inside: it runs");
            yield return new WaitForSeconds(2.2f);
            Assert.IsTrue(door.Out, "rolled aside");
            yield return WalkTo(T(19.0f, 0f, -12.5f));
            yield return WalkTo(T(13.5f, 0f, -12.5f), 0.4f, 6f);
            Assert.Less(apprentice.transform.position.x - tower.transform.position.x, 14f, "out in the court again");
        }

        // ---- The Punctual

        Punctual ThePunctual => UnityEngine.Object.FindAnyObjectByType<Punctual>();

        // Face to face with it in the great hall, `distance` in front of it.
        Punctual FaceThePunctual(float quartersChance, float distance = 1.8f)
        {
            var p = ThePunctual;
            Assert.IsNotNull(p, "one of the household keeps the orrery");
            p.quartersChance = quartersChance;
            Vector3 at = p.transform.position + p.transform.forward * distance;
            apprentice.Teleport(new Vector3(at.x, p.transform.position.y + 0.05f, at.z));
            apprentice.transform.rotation = Quaternion.LookRotation(-p.transform.forward);
            return p;
        }

        IEnumerator AwaitWindup(Punctual p, SouredAttack attack, float timeout = 8f)
        {
            for (float t = 0f; t < timeout; t += Time.deltaTime)
            {
                if (p.CurrentState == Soured.State.Telegraph && p.CurrentAttack == attack)
                    yield break;
                yield return null;
            }
            Assert.Fail($"it never began {attack.name} ({p.CurrentState})");
        }

        static double Nearest(double t) => System.Math.Round(t / HallClock.Beat);

        [UnityTest]
        public IEnumerator Punctual_TheCount_RaisedOnABeat_BroughtDownTwoBeatsLater_OnTheTick()
        {
            var p = FaceThePunctual(0f);
            yield return AwaitWindup(p, p.primary);
            double raised = HallClock.Now;
            Assert.Less(HallClock.Through, 0.1f, "raised on the beat");
            for (float t = 0f; t < 3f && p.LastBlowOffBeat < 0f; t += Time.deltaTime)
                yield return null;
            double fell = HallClock.Now;
            Assert.GreaterOrEqual(p.LastBlowOffBeat, 0f, "it struck");
            Assert.Less(p.LastBlowOffBeat, 0.05f, "on the tick");
            Assert.AreEqual(2.0, Nearest(fell) - Nearest(raised), "two beats after it was raised");
        }

        [UnityTest]
        public IEnumerator Punctual_TheQuarters_FourBlowsOnFourBeats()
        {
            var p = FaceThePunctual(1f);
            yield return AwaitWindup(p, p.quarters);
            var beats = new System.Collections.Generic.List<double>();
            float worst = 0f;
            int seen = p.QuarterBlows;
            for (float t = 0f; t < 6f && beats.Count < 4; t += Time.deltaTime)
            {
                if (p.QuarterBlows > seen)
                {
                    seen = p.QuarterBlows;
                    beats.Add(Nearest(HallClock.Now));
                    worst = Mathf.Max(worst, p.LastBlowOffBeat);
                    input.Move = new Vector2(0f, -1f); // (backing off: the run goes on, whether or not it lands)
                }
                yield return null;
            }
            input.Move = Vector2.zero;
            Assert.AreEqual(4, beats.Count, "four blows");
            for (int k = 1; k < 4; k++)
                Assert.AreEqual(1.0, beats[k] - beats[k - 1], $"blow {k + 1} a beat after the last");
            Assert.Less(worst, 0.05f, "each on the tick");
        }

        [UnityTest]
        public IEnumerator Punctual_BlockedOnTheBeat_LosesCount_TillABarBegins()
        {
            var p = FaceThePunctual(0f);
            yield return AwaitWindup(p, p.primary);
            float impact = p.primary.ImpactTime;
            for (float t = 0f; t < impact - 0.1f; t += Time.deltaTime)
                yield return null;
            input.BlockHeld = true; // (raised a tenth before the tick: a perfect block)
            for (float t = 0f; t < 0.5f && p.CurrentState != Soured.State.Stunned; t += Time.deltaTime)
                yield return null;
            input.BlockHeld = false;
            Assert.AreEqual(Soured.State.Stunned, p.CurrentState, "blocked on the beat: stunned");
            Assert.IsTrue(p.Lost, "and it's lost count");
            bool struckWhileLost = false;
            for (float t = 0f; t < 8f && p.Lost; t += Time.deltaTime)
            {
                struckWhileLost |= p.CurrentState == Soured.State.Telegraph;
                yield return null;
            }
            Assert.IsFalse(p.Lost, "it finds the beat again");
            Assert.IsTrue(HallClock.BarStarts((long)Nearest(HallClock.Now)), "where a bar begins");
            Assert.IsFalse(struckWhileLost, "nothing while it's lost");
        }

        // (SHOTS=dir) The Punctual: keeping the orrery; the Count raised; the Quarters mid-run; lost count.
        [UnityTest]
        public IEnumerator Punctual_Capture()
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
                var p = ThePunctual;
                apprentice.Teleport(p.transform.position + p.transform.forward * 7f, true, true);
                CameraRig.Instance?.SetZoom(0.8f);
                yield return new WaitForSeconds(0.2f);
                Shot("punctual-0-keeping-the-orrery");
                p = FaceThePunctual(0f, 2.2f);
                yield return AwaitWindup(p, p.primary);
                yield return new WaitForSeconds(1.5f);
                Shot("punctual-1-the-count-raised");
                for (float t = 0f; t < 3f && p.CurrentState != Soured.State.Recover; t += Time.deltaTime)
                    yield return null;
                yield return new WaitForSeconds(0.1f);
                Shot("punctual-2-the-count-down");
                apprentice.RestoreFully();
                p.quartersChance = 1f;
                yield return AwaitWindup(p, p.quarters);
                for (float t = 0f; t < 4f && p.QuarterBlows % 4 != 2; t += Time.deltaTime)
                    yield return null;
                Shot("punctual-3-the-quarters-second-blow");
                apprentice.RestoreFully();
                p.Stun();
                yield return new WaitForSeconds(p.stunDuration + 0.3f);
                Shot("punctual-4-lost-count");
            }
            finally
            {
                cam.targetTexture = null;
                rt.Release();
            }
        }

        // ---- The Wound

        Wound TheWound => UnityEngine.Object.FindAnyObjectByType<Wound>();

        // (bitten while the test waits: kept on its feet)
        void KeepAlive()
        {
            if (apprentice.Health < apprentice.maxHealth * 0.5f)
                apprentice.RestoreFully();
        }

        Wound FaceTheWound(float distance = 4f)
        {
            var w = TheWound;
            Assert.IsNotNull(w, "a hound keeps the court");
            Vector3 at = w.transform.position + w.transform.forward * distance;
            apprentice.Teleport(new Vector3(at.x, w.transform.position.y + 0.05f, at.z));
            apprentice.transform.rotation = Quaternion.LookRotation(-w.transform.forward);
            return w;
        }

        [UnityTest]
        public IEnumerator Wound_RunsDown_Slowing_SitsToWind_ThenRunsAgain()
        {
            var w = FaceTheWound();
            w.runTime = 3f; // (quicker to run down, for the test)
            float fastest = 0f, slowest = 9f;
            for (float t = 0f; t < 10f && !w.Winding; t += Time.deltaTime)
            {
                KeepAlive();
                if (w.CurrentState == Soured.State.Chase)
                {
                    fastest = Mathf.Max(fastest, w.Speed);
                    slowest = Mathf.Min(slowest, w.Speed);
                }
                yield return null;
            }
            Assert.IsTrue(w.Winding, "run down, it sits to wind itself");
            Assert.Greater(fastest, 1f, "wound tight, quick");
            Assert.Less(slowest, 0.5f, "running down, slow");
            for (float t = 0f; t < 4f && w.Winds == 0; t += Time.deltaTime)
            {
                KeepAlive();
                yield return null;
            }
            Assert.AreEqual(1, w.Winds, "wound again");
            Assert.AreEqual(1f, w.Tension, 0.05f);
        }

        [UnityTest]
        public IEnumerator Wound_StruckWhileWinding_StartsAgain()
        {
            var w = FaceTheWound();
            w.runTime = 2f;
            for (float t = 0f; t < 10f && !w.Winding; t += Time.deltaTime)
            {
                KeepAlive();
                yield return null;
            }
            Assert.IsTrue(w.Winding);
            float started = Time.time;
            yield return new WaitForSeconds(1.2f);
            w.TakeHit(new Hit { Damage = 1f, From = apprentice.transform.position, Knockback = Vector3.zero, Stagger = 999f });
            for (float t = 0f; t < 6f && w.Winds == 0; t += Time.deltaTime)
                yield return null;
            Assert.AreEqual(1, w.Winds);
            Assert.Greater(Time.time - started, w.windTime + 1.2f, "struck, it had to start winding over");
        }

        [UnityTest]
        public IEnumerator Wound_TheStroke_KnocksItsKeyLoose_RunsDownAtOnce()
        {
            var w = FaceTheWound(11.5f); // (as soon as it sees you: it closes nine metres in about a second)
            w.immortal = true; // (the charged blow and the wave together can scatter it outright; here it must see its key go)
            apprentice.GiveStroke();
            Assert.Greater(w.Tension, 0.9f);
            // Drawn as it comes (it's quick), let go when it's close.
            apprentice.transform.rotation = Quaternion.LookRotation((w.transform.position - apprentice.transform.position).Flat());
            input.HeavyHeld = true;
            input.PressHeavy();
            bool everCharged = false;
            float nearest = 99f;
            for (float t = 0f; t < 4f && !(apprentice.StrokeCharged && Vector3.Distance(w.transform.position, apprentice.transform.position) < 4.5f); t += Time.deltaTime)
            {
                everCharged |= apprentice.StrokeCharged;
                nearest = Mathf.Min(nearest, Vector3.Distance(w.transform.position, apprentice.transform.position));
                apprentice.transform.rotation = Quaternion.LookRotation((w.transform.position - apprentice.transform.position).Flat());
                yield return null;
            }
            Assert.IsTrue(apprentice.StrokeCharged, $"charged (ever {everCharged}, nearest {nearest:0.0} m, it {w.CurrentState}, the Apprentice {apprentice.LastEvent}, health {apprentice.Health})");
            input.HeavyHeld = false;
            yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(w.KeyLoose || w.Winding || w.Winds > 0, $"the wave reached it (it {w.CurrentState}, health {w.Health}, tension {w.Tension:0.00}, {Vector3.Distance(w.transform.position, apprentice.transform.position):0.0} m; the Apprentice {apprentice.Health}, wave {(apprentice.LastWave != null)})");
            Assert.Less(w.Tension, 0.05f, "its key knocked loose: run down at once");
            for (float t = 0f; t < 4f && !w.Winding; t += Time.deltaTime)
                yield return null;
            Assert.IsTrue(w.Winding, "and it sits to wind");
        }

        // (SHOTS=dir) The Wound: keeping the court; running at you; the pounce; run down, winding.
        [UnityTest]
        public IEnumerator Wound_Capture()
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
                var w = TheWound;
                w.immortal = true;
                w.runTime = 4f;
                Vector3 post = w.transform.position, south = Vector3.back; // (it keeps the court facing south)
                apprentice.Teleport(post + new Vector3(2.5f, 0.05f, -1.5f), true, true);
                CameraRig.Instance?.SetZoom(0.7f);
                yield return new WaitForSeconds(0.1f);
                Shot("wound-0-keeping-the-court");
                apprentice.Teleport(post + south * 8f + Vector3.up * 0.05f, true, true);
                apprentice.transform.rotation = Quaternion.LookRotation(-south);
                for (float t = 0f; t < 3f && w.CurrentState != Soured.State.Chase; t += Time.deltaTime)
                    yield return null;
                yield return new WaitForSeconds(0.25f);
                Shot("wound-1-running-at-you");
                for (float t = 0f; t < 3f && !(w.CurrentState == Soured.State.Strike && w.CurrentAttack == w.pounce); t += Time.deltaTime)
                {
                    KeepAlive();
                    yield return null;
                }
                yield return new WaitForSeconds(0.12f);
                Shot("wound-2-the-pounce");
                for (float t = 0f; t < 8f && !w.Winding; t += Time.deltaTime)
                {
                    KeepAlive();
                    yield return null;
                }
                yield return new WaitForSeconds(0.8f);
                Vector3 front = w.transform.position + Vector3.back * 2.5f;
                apprentice.Teleport(new Vector3(front.x, w.transform.position.y + 0.05f, front.z), true, true);
                yield return new WaitForSeconds(0.1f);
                Shot("wound-3-winding");
            }
            finally
            {
                cam.targetTexture = null;
                rt.Release();
            }
        }

        // ---- The Keener

        Keener TheKeener => UnityEngine.Object.FindAnyObjectByType<Keener>();

        Keener FaceTheKeener(float distance)
        {
            var k = TheKeener;
            Assert.IsNotNull(k, "a mourner keeps the hall");
            Vector3 at = k.transform.position + k.transform.forward * distance;
            apprentice.Teleport(new Vector3(at.x, k.transform.position.y + 0.05f, at.z));
            apprentice.transform.rotation = Quaternion.LookRotation(-k.transform.forward);
            return k;
        }

        // Waits (in game time) until `done`, or fails after `timeout`.
        static IEnumerator Until(Func<bool> done, float timeout, string what)
        {
            for (float t = 0f; t < timeout && !done(); t += Time.deltaTime)
                yield return null;
            Assert.IsTrue(done(), "timed out waiting for " + what);
        }

        static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = b.y = 0f;
            return Vector3.Distance(a, b);
        }

        [UnityTest]
        public IEnumerator Keener_DrawsBreath_ThenTollsThreeRings_HalfASecondApart()
        {
            var k = FaceTheKeener(7f);
            var rang = new System.Collections.Generic.List<float>();
            void Heard(Keener by, Shockwave ring) => rang.Add(Time.time);
            Keener.Rang += Heard;
            try
            {
                yield return Until(() => k.CurrentState == Soured.State.Telegraph, 4f, "the breath");
                float breath = Time.time;
                yield return Until(() => rang.Count >= 3, 4f, "three rings");
                Assert.AreEqual(k.primary.telegraph + k.primary.strike * k.primary.impactAt, rang[0] - breath, 0.1f, "the breath first, then the knell");
                Assert.AreEqual(k.primary.volleyInterval, rang[1] - rang[0], 0.05f);
                Assert.AreEqual(k.primary.volleyInterval, rang[2] - rang[1], 0.05f);
                yield return new WaitForSeconds(1.5f);
                Assert.AreEqual(3, k.Rings, "three, and no more for a while");
            }
            finally { Keener.Rang -= Heard; }
        }

        [UnityTest]
        public IEnumerator Keener_ARing_KnocksYouBack_WhereItsCrestPasses()
        {
            var k = FaceTheKeener(6f);
            yield return Until(() => k.LastRing != null, 4f, "the knell");
            var ring = k.LastRing;
            yield return Until(() => ring == null || ring.Struck, 3f, "the ring reaching you");
            Assert.AreEqual(apprentice.maxHealth - k.ringDamage, apprentice.Health, 0.01f, "struck by the ring");
        }

        [UnityTest]
        public IEnumerator Keener_ARing_DashedThrough_DoesNoHarm()
        {
            var k = FaceTheKeener(6f);
            yield return Until(() => k.LastRing != null, 4f, "the knell");
            var ring = k.LastRing;
            yield return Until(() => ring.Radius >= FlatDistance(apprentice.transform.position, ring.transform.position) - 0.9f, 3f, "the crest arriving");
            input.PressDash(); // (in, through it)
            yield return Until(() => ring == null || ring.Struck, 1f, "the crest passing");
            Assert.AreEqual(apprentice.maxHealth, apprentice.Health, "dashed through: unharmed");
        }

        [UnityTest]
        public IEnumerator Keener_DriftsBack_WhenYouComeNear()
        {
            var k = TheKeener;
            k.firstKnellDelay = 99f; // (only its keeping away, here)
            FaceTheKeener(3f);
            yield return new WaitForSeconds(3f);
            Assert.AreEqual(Soured.State.Chase, k.CurrentState);
            Assert.Greater(FlatDistance(apprentice.transform.position, k.transform.position), 4.8f, "it keeps its distance");
        }

        [UnityTest]
        public IEnumerator Keener_StaggeredWhileDrawingBreath_TollsNothing()
        {
            var k = FaceTheKeener(7f);
            yield return Until(() => k.CurrentState == Soured.State.Telegraph, 4f, "the breath");
            yield return new WaitForSeconds(0.5f);
            k.TakeHit(new Hit { Damage = 1f, From = apprentice.transform.position, Knockback = Vector3.zero, Stagger = 999f });
            Assert.AreEqual(Soured.State.Staggered, k.CurrentState, "fragile: it reels");
            yield return new WaitForSeconds(2f);
            Assert.AreEqual(0, k.Rings, "the knell cut short");
        }

        // A knell's rings roll across its own floor: a wall between muffles one, and one from the floor below passes
        // under you.
        [UnityTest]
        public IEnumerator TheKnell_IsMuffledByAWall_AndKeepsToItsOwnFloor()
        {
            Vector3 at = apprentice.transform.position;
            Shockwave Ring(Vector3 centre)
            {
                var r = Shockwave.Spread(centre, apprentice, 6f, 5.5f, 0.8f, 12f, 5f, Color.white);
                r.muffled = true;
                return r;
            }
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = at + Vector3.right * 1.5f + Vector3.up * 1f;
            wall.transform.localScale = new Vector3(0.3f, 2f, 3f);
            yield return null;
            var walled = Ring(at + Vector3.right * 3f);
            yield return Until(() => walled == null || walled.Struck, 2f, "the walled ring");
            Assert.AreEqual(apprentice.maxHealth, apprentice.Health, "a wall between: muffled");
            UnityEngine.Object.Destroy(wall);
            var below = Ring(at + Vector3.right * 3f + Vector3.down * 3f);
            yield return Until(() => below == null, 2f, "the ring below fading");
            Assert.AreEqual(apprentice.maxHealth, apprentice.Health, "from the floor below: it passes under");
            var open = Ring(at + Vector3.right * 3f);
            yield return Until(() => open == null || open.Struck, 2f, "the open ring");
            Assert.AreEqual(apprentice.maxHealth - 12f, apprentice.Health, 0.01f, "in the open, on its floor: struck");
        }

        // (SHOTS=dir) The Keener: keeping the hall; drawing breath; the knell's rings; spent.
        [UnityTest]
        public IEnumerator Keener_Capture()
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
                var k = TheKeener;
                k.immortal = true;
                Vector3 post = k.transform.position;
                apprentice.Teleport(post + new Vector3(2.2f, 0.05f, 1.2f), true, true);
                apprentice.transform.rotation = Quaternion.LookRotation(Vector3.back);
                CameraRig.Instance?.SetZoom(0.6f);
                k.passive = true;
                yield return new WaitForSeconds(0.1f);
                Shot("keener-0-keeping-the-hall");
                k.passive = false;
                CameraRig.Instance?.SetZoom(1.5f);
                // Up the aisle, turned to face south, you south of it: the camera (looking north) sees it and its rings
                // rolling down at you.
                var body = k.GetComponent<CharacterController>();
                body.enabled = false;
                k.transform.position = new Vector3(T(22f, 0f, -13.5f).x, post.y, T(22f, 0f, -13.5f).z);
                k.transform.rotation = Quaternion.AngleAxis(180f, Vector3.up) * k.transform.rotation;
                body.enabled = true;
                FaceTheKeener(5.5f);
                yield return Until(() => k.CurrentState == Soured.State.Telegraph, 4f, "the breath");
                yield return new WaitForSeconds(k.primary.telegraph * 0.8f);
                Shot("keener-1-drawing-breath");
                yield return Until(() => k.Rings >= 2, 3f, "the rings");
                yield return new WaitForSeconds(0.3f);
                Shot("keener-2-the-knell");
                yield return Until(() => k.CurrentState == Soured.State.Recover, 3f, "spent");
                yield return new WaitForSeconds(0.4f);
                Shot("keener-3-spent");
            }
            finally
            {
                cam.targetTexture = null;
                rt.Release();
            }
        }

        [UnityTest]
        public IEnumerator TheMusic_TheLastHour_StoppedTillWoken_ThenAwakeInStep()
        {
            var music = UnityEngine.Object.FindAnyObjectByType<MusicPlayer>();
            Assert.IsNotNull(music);
            Assert.AreEqual("music_last_hour_1", music.GetComponent<AudioSource>().clip.name, "the Hall as it is: stopped");
            Assert.AreEqual("music_last_hour_2", music.woken.name);
            Assert.AreEqual(music.GetComponent<AudioSource>().clip.length, music.woken.length, 0.01f, "one loop: they cross in step");
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(0f, music.WakeMix);
            music.Wake();
            yield return new WaitForSeconds(music.wakeCrossfade + 0.5f);
            Assert.IsTrue(music.Woken);
            Assert.AreEqual(1f, music.WakeMix, 0.001f, "awake: crossfaded over");
            music.Wake(false);
            yield return new WaitForSeconds(music.wakeCrossfade + 0.5f);
            Assert.AreEqual(0f, music.WakeMix, 0.001f, "and still again (the lab's toggle)");
        }

        [UnityTest]
        public IEnumerator TheMarks_GlowWhenTheStrokeCouldSetTheirMachineRunning()
        {
            var lift = Machine("Gear Lift");
            var marks = UnityEngine.Object.FindObjectsByType<StrokeMark>(FindObjectsSortMode.None).Where(m => m.clockwork == lift).ToArray();
            Assert.AreEqual(1, marks.Length, "the crescent on its deck (where you stand to use the Stroke), and nowhere else");
            yield return null;
            Assert.IsFalse(marks[0].InReach, "without the Stroke");
            apprentice.GiveStroke();
            yield return null;
            Assert.IsFalse(marks[0].InReach, "too far off");
            Assert.Less(marks[0].Glow, 0.6f);
            yield return WalkTo(T(-10.5f, 0f, -21.0f));
            yield return null;
            Assert.IsTrue(marks[0].InReach, "the Stroke, near enough");
            float most = 0f;
            for (float t = 0f; t < 1.2f; t += Time.deltaTime)
            {
                most = Mathf.Max(most, marks[0].Glow);
                yield return null;
            }
            Assert.Greater(most, 0.8f, "it brightens, pulsing");
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
            yield return Stroke(Vector3.left);                          // from on its crescent, whichever way
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
            yield return WalkTo(T(-2.7f, 0f, -0.6f), 0.3f);            // onto its plate, under the bob
            yield return Stroke(Vector3.back);                          // (whichever way: from on the crescent)
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
                yield return At("last-hour-10b-its-drive-inside", T(19.4f, 0.55f, -10.6f), Vector3.left);
                yield return At("last-hour-10c-the-lift-waiting", T(-10.4f, 0.55f, -20.6f), Vector3.back);
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
