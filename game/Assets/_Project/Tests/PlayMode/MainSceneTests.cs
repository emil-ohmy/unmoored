using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MyGame.Tests
{
    // Plays the Main scene headless with scripted input and checks the rules from docs/brief.md.
    // Any Debug.LogError or exception during a test fails it.
    // Time is stepped at a fixed 1/60 s per frame (Time.captureDeltaTime): deterministic, and headless Unity renders
    // those frames faster than real time, so the suite runs several times faster than the seconds it simulates.
    // Long measurements are [Category("Slow")]: tools/unity.sh test skips them, test-all runs everything.
    public class MainSceneTests
    {
        Apprentice apprentice;
        ScriptedInput input;
        Laden[] laden;
        LadenArcher archer;
        MooringPost post;

        [UnitySetUp]
        public IEnumerator LoadMain()
        {
            Application.targetFrameRate = -1; // as fast as it can render
            Time.captureDeltaTime = 1f / 60f;
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            apprentice = Object.FindAnyObjectByType<Apprentice>();
            // laden[0] is the quay's (nearest the middle); the archer is the yard's (the one met alone).
            laden = Object.FindObjectsByType<Laden>(FindObjectsSortMode.None).Where(l => !(l is Anchoress)) // (she's a Laden too)
                .OrderBy(l => l.transform.position.magnitude).ToArray();
            archer = Object.FindObjectsByType<LadenArcher>(FindObjectsSortMode.None).OrderByDescending(a => a.transform.position.z).First();
            // The landing's Mooring post (where the Apprentice starts; the other stands before the chapel).
            post = Object.FindObjectsByType<MooringPost>(FindObjectsSortMode.None)
                .OrderBy(p => Vector3.Distance(p.transform.position, apprentice.transform.position)).First();
            apprentice.Input = input = new ScriptedInput();
            yield return new WaitForSeconds(0.5f); // settle onto the ground
        }

        [TearDown]
        public void ResetTime()
        {
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f;
        }

        [Test]
        public void Scene_HasTheSliceCast()
        {
            Assert.IsNotNull(apprentice, "no Apprentice");
            Assert.AreEqual(2, laden.Length, "a Laden on the quay, and one at the crossroads");
            Assert.AreEqual(2, Object.FindObjectsByType<LadenArcher>(FindObjectsSortMode.None).Length, "an archer in the yard, and one at the crossroads");
            Assert.IsNotNull(Object.FindAnyObjectByType<ShortcutGate>(), "the shortcut gate home");
            Assert.IsNotNull(post, "no Mooring post");
            Assert.IsFalse(post.IsLit, "the Mooring post should start dark");
        }

        // The code's front (transform.forward) must be the model's visual front: the Apprentice's mask and the
        // Laden's heap. (The heap is what blocks, so a mismatch would make the block rule look backwards.)
        [Test]
        public void Models_FaceTheirTransformForward()
        {
            Assert.Greater(FrontDot(apprentice.gameObject, "Mask"), 0.5f, "the Apprentice's mask isn't on its forward side");
            foreach (var l in laden)
                Assert.Greater(FrontDot(l.gameObject, "HeapWood"), 0.5f, "the Laden's heap isn't on its forward side");
        }

        static float FrontDot(GameObject character, string material)
        {
            var skinned = character.GetComponentInChildren<SkinnedMeshRenderer>();
            var materials = skinned.sharedMaterials;
            int index = System.Array.FindIndex(materials, m => m.name.StartsWith(material));
            Assert.GreaterOrEqual(index, 0, $"no {material} material on {character.name}");
            // Bake the posed mesh: a skinned mesh's raw vertices are in bind-pose space, not the renderer's.
            var posed = new Mesh();
            skinned.BakeMesh(posed, true);
            var vertices = posed.vertices;
            var triangles = posed.GetTriangles(index);
            var centre = triangles.Aggregate(Vector3.zero, (sum, i) => sum + vertices[i]) / triangles.Length;
            Vector3 toPart = skinned.transform.position + skinned.transform.rotation * centre - character.transform.position;
            toPart.y = 0f;
            return Vector3.Dot(toPart.normalized, character.transform.forward);
        }

        [UnityTest]
        public IEnumerator Apprentice_StartsOnTheLanding_ByTheMooringPost()
        {
            yield return new WaitForSeconds(0.5f);
            Assert.That(apprentice.transform.position.y, Is.InRange(0.35f, 0.65f), "not standing on the landing");
            Assert.IsNotNull(MooringPost.Nearest(apprentice.transform.position), "the start should be in reach of the Mooring post");
            Assert.AreEqual(Apprentice.State.Free, apprentice.CurrentState);
        }

        [UnityTest]
        public IEnumerator Apprentice_MovesWithInput()
        {
            Vector3 start = apprentice.transform.position;
            input.Move = Vector2.up; // along the boardwalk towards the quay
            yield return new WaitForSeconds(0.5f);
            input.Move = Vector2.zero;
            Assert.Greater(Vector3.Distance(start, apprentice.transform.position), 2f, "barely moved in half a second");
        }

        [UnityTest]
        public IEnumerator TheWayIn_IsQuiet_UntilTheApprenticeReachesTheQuay()
        {
            Assert.IsTrue(laden.All(l => l.CurrentState == Soured.State.Idle), "nothing should be alert at the start");
            // Walk the whole boardwalk to the quay's edge.
            for (float t = 0; t < 6f && apprentice.transform.position.z < -5.5f; t += Time.deltaTime)
            {
                input.Move = Vector2.up;
                yield return null;
            }
            input.Move = Vector2.zero;
            Assert.Greater(apprentice.transform.position.z, -5.5f, "the boardwalk should lead to the quay");
            Assert.AreEqual(apprentice.maxHealth, apprentice.Health, "the way in should be safe");
            Assert.IsTrue(laden.All(l => l.CurrentState == Soured.State.Idle), "the Laden should only notice up close");

            var nearest = laden.OrderBy(l => Vector3.Distance(l.transform.position, apprentice.transform.position)).First();
            for (float t = 0; t < 3f && nearest.CurrentState == Soured.State.Idle; t += Time.deltaTime)
            {
                Vector3 to = nearest.transform.position - apprentice.transform.position;
                input.Move = new Vector2(to.x, to.z).normalized;
                yield return null;
            }
            input.Move = Vector2.zero;
            Assert.AreNotEqual(Soured.State.Idle, nearest.CurrentState, "walking up to a Laden should wake it");
            Assert.Less(Vector3.Distance(nearest.transform.position, apprentice.transform.position), nearest.aggroRadius + 0.3f);
        }

        [UnityTest]
        public IEnumerator LockOn_TargetsTheNearbySoured_FacesIt_AndShowsItsHealth()
        {
            var target = Isolate(laden[0], frozen: true);
            Vector3 side = Quaternion.Euler(0f, 90f, 0f) * target.transform.forward;
            PlaceApprentice(target.transform.position + side * 3f, target.transform.position + side * 10f); // facing away
            input.PressLock();
            yield return null;
            Assert.AreEqual(target, apprentice.LockTarget, "lock-on should pick the Laden nearby");
            yield return new WaitForSeconds(0.3f);
            Vector3 to = target.transform.position - apprentice.transform.position;
            to.y = 0f;
            Assert.Greater(Vector3.Dot(apprentice.transform.forward, to.normalized), 0.95f, "locked on, the Apprentice should face its target");
            var hud = Object.FindAnyObjectByType<Hud>();
            Assert.IsTrue(hud.LockBarVisible, "the target's health bar should show");
            Assert.IsTrue(hud.LockMarkerVisible, "and the lock-on marker over it");
            Vector2 expected = Camera.main.WorldToScreenPoint(target.transform.position + Vector3.up * target.barHeight);
            Vector2 actual = RectTransformUtility.WorldToScreenPoint(null, hud.LockBarRect.position); // overlay: screen pixels
            Assert.Less(Vector2.Distance(expected, actual), 4f, $"the bar should float above its target (expected {expected}, got {actual})");

            input.PressLock();
            yield return null;
            yield return null; // the HUD updates in LateUpdate, after this coroutine resumes: give it that frame
            Assert.IsNull(apprentice.LockTarget, "pressing again should release");
            Assert.IsFalse(Object.FindAnyObjectByType<Hud>().LockBarVisible);
            Assert.IsFalse(Object.FindAnyObjectByType<Hud>().LockMarkerVisible);
        }

        [UnityTest]
        public IEnumerator LockOn_AimsAttacksAtTheTarget_AndReleasesWhenItScatters()
        {
            var target = Isolate(laden[0], frozen: true);
            PlaceApprentice(target.transform.position - target.transform.forward * 1.8f, target.transform.position - target.transform.forward * 9f);
            input.PressLock();
            yield return null;
            input.Move = new Vector2(1f, 0f); // stick pointing somewhere else entirely
            float before = target.Health;
            input.PressLight();
            yield return new WaitForSeconds(0.35f);
            input.Move = Vector2.zero;
            Assert.Less(target.Health, before, "a locked-on swing should turn to the target and land");

            target.TakeHit(new Hit { Damage = 1000f, From = apprentice.transform.position });
            yield return null;
            Assert.IsNull(apprentice.LockTarget, "a scattered target (with none other near) should release the lock");
        }

        [UnityTest]
        public IEnumerator LockOn_SwitchesBetweenTargets()
        {
            Object.Instantiate(laden[0], new Vector3(-1.4f, 0.55f, 3.6f), laden[0].transform.rotation); // a second target
            yield return null;
            PlaceApprentice(new Vector3(1.4f, 0.55f, 1.9f), new Vector3(1.4f, 0.55f, 5f)); // between the two Laden
            input.PressLock();
            yield return null;
            var first = apprentice.LockTarget;
            Assert.IsNotNull(first);
            input.Switch(1);
            yield return null;
            if (apprentice.LockTarget == first)
            {
                input.Switch(-1);
                yield return null;
            }
            Assert.IsNotNull(apprentice.LockTarget);
            Assert.AreNotEqual(first, apprentice.LockTarget, "switching should move the lock to the other Laden");
        }

        [UnityTest]
        public IEnumerator Sounds_PlayForSwingDashAndBlock_AndTorchesCrackle()
        {
            var sfx = Object.FindAnyObjectByType<Sfx>();
            Assert.IsTrue(sfx.oarSwing.Length > 0 && sfx.dash.Length > 0 && sfx.blockClang.Length > 0, "sounds should be wired up");
            input.PressLight();
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual("oar_swing", sfx.LastPlayed);
            yield return new WaitForSeconds(0.5f);
            input.PressDash();
            yield return new WaitForSeconds(0.05f);
            Assert.AreEqual("dash", sfx.LastPlayed);
            yield return new WaitForSeconds(apprentice.dashDuration + apprentice.dashRecovery); // let the dash finish

            var target = Isolate(laden[0], frozen: true);
            PlaceApprentice(target.transform.position + target.transform.forward * 1.6f, target.transform.position);
            yield return new WaitForSeconds(0.6f);
            input.PressLight();
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual("block_clang", sfx.LastPlayed, "a blocked hit should clang");

            var torches = Object.FindObjectsByType<TorchFlame>(FindObjectsSortMode.None);
            Assert.IsTrue(torches.Length > 0 && torches.All(t => t.GetComponent<AudioSource>().clip != null), "every torch should have its crackle");
            Assert.AreEqual(1, Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length, "exactly one listener");
            var music = Object.FindAnyObjectByType<MusicPlayer>().GetComponent<AudioSource>();
            Assert.IsNotNull(music.clip, "the music should be wired up");
            Assert.IsTrue(music.loop && music.spatialBlend == 0f, "music should loop and not be positional");
        }

        [UnityTest]
        public IEnumerator Laden_SeesAhead_ButCanBeSneakedUpOnFromBehind()
        {
            var target = Isolate(laden[0], frozen: true);
            PlaceApprentice(target.transform.position - target.transform.forward * 2.2f, target.transform.position);
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(Soured.State.Idle, target.CurrentState, "2.2 m behind it, it shouldn't notice");
            float before = target.Health;
            input.PressLight();
            yield return new WaitForSeconds(0.35f);
            Assert.Less(target.Health, before, "a sneak attack from behind should land");
            Assert.AreNotEqual(Soured.State.Idle, target.CurrentState, "being hit should wake it");

            var other = Object.Instantiate(target, new Vector3(-0.8f, 0.55f, 3.4f), target.transform.rotation);
            yield return null;
            PlaceApprentice(other.transform.position + other.transform.forward * 5f, other.transform.position);
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(Soured.State.Chase, other.CurrentState, "5 m in front of it, it should see the Apprentice");
        }

        float windupStart; // Time.time when the current attack's wind-up began

        // Seconds since the wind-up began (the Laden's own state timer restarts at every state change).
        float SinceWindup => Time.time - windupStart;

        // Wait for the Laden (facing the Apprentice, 1.8 m away) to start the chosen attack; returns when the wind-up begins.
        IEnumerator AwaitWindup(Laden l, bool sweep)
        {
            l.sweepChanceInFront = sweep ? 1f : 0f;
            for (float t = 0; t < 3f && l.CurrentState != Soured.State.Telegraph; t += Time.deltaTime)
                yield return null;
            Assert.AreEqual(Soured.State.Telegraph, l.CurrentState, "the Laden never started its attack");
            Assert.AreEqual(sweep ? "Sweep" : "Slam", l.CurrentAttack.name);
            windupStart = Time.time - l.StateTime;
        }

        // Stage a fight on open ground in the middle of the quay (away from walls and water), Laden facing +X.
        void FaceOff(Laden l, float distance = 1.8f)
        {
            l.SetSpawn(new Vector3(-1f, 0.55f, 0.5f), Quaternion.LookRotation(Vector3.right));
            l.Reform();
            PlaceApprentice(l.transform.position + l.transform.forward * distance, l.transform.position);
            apprentice.RestoreFully();
        }

        [UnityTest]
        public IEnumerator PerfectBlock_StunsTheLaden_AndARiposteHitsDouble()
        {
            var l = laden[0];
            FaceOff(l);
            yield return AwaitWindup(l, sweep: false);
            while (SinceWindup < l.CurrentAttack.ImpactTime - 0.1f)
                yield return null;
            input.BlockHeld = true; // raised 0.1 s before the blow: inside the 0.2 s perfect window
            for (float t = 0; t < 0.5f && l.CurrentState != Soured.State.Stunned; t += Time.deltaTime)
                yield return null;
            Assert.AreEqual(Soured.State.Stunned, l.CurrentState, "a perfect block should stun the Laden");
            Assert.AreEqual(apprentice.maxHealth, apprentice.Health, "a perfect block takes no damage");
            input.BlockHeld = false;
            yield return new WaitForSeconds(0.25f);
            float before = l.Health;
            input.PressLight();
            yield return new WaitForSeconds(0.3f);
            Assert.That(before - l.Health, Is.EqualTo(apprentice.lightDamage * l.riposteMultiplier).Within(0.01f), "a riposte should do double damage");
        }

        [UnityTest]
        public IEnumerator NormalBlock_TakesChipDamageAndStamina_AndMashingGivesNoPerfectBlock()
        {
            var l = laden[0];
            FaceOff(l);
            input.BlockHeld = true; // held from long before: a normal block
            yield return AwaitWindup(l, sweep: false);
            for (float t = 0; t < 1.5f && apprentice.Health >= apprentice.maxHealth; t += Time.deltaTime)
                yield return null;
            float expected = apprentice.maxHealth - l.primary.damage * apprentice.blockChip;
            Assert.That(apprentice.Health, Is.EqualTo(expected).Within(0.01f), "a normal block lets 30% through");
            Assert.Less(apprentice.Stamina, apprentice.maxStamina, "blocking costs stamina");
            Assert.AreNotEqual(Soured.State.Stunned, l.CurrentState);

            // Mashing: lower and re-raise right before the blow. Too soon after lowering: no perfect window.
            input.BlockHeld = false;
            yield return new WaitForSeconds(1.5f);
            FaceOff(l);
            yield return AwaitWindup(l, sweep: false);
            while (SinceWindup < l.CurrentAttack.ImpactTime - 0.3f)
                yield return null;
            input.BlockHeld = true;
            yield return null;
            input.BlockHeld = false;
            yield return null;
            input.BlockHeld = true; // re-raised within 0.4 s of lowering
            for (float t = 0; t < 0.6f && apprentice.Health >= apprentice.maxHealth; t += Time.deltaTime)
                yield return null;
            input.BlockHeld = false;
            Assert.AreNotEqual(Soured.State.Stunned, l.CurrentState, "mashing block shouldn't earn a perfect block");
        }

        [UnityTest]
        public IEnumerator Laden_SweepsAnyoneAtItsFlank()
        {
            var l = laden[0];
            l.Reform();
            l.chaseSpeed = l.guardSpeed = 0f;
            Vector3 side = Quaternion.Euler(0f, 90f, 0f) * l.transform.forward;
            PlaceApprentice(l.transform.position - l.transform.forward * 1.0f + side * 1.6f, l.transform.position); // behind-ish, at its side
            l.TakeHit(new Hit { Damage = 1f, From = apprentice.transform.position }); // wake it
            for (float t = 0; t < 2f && l.CurrentState != Soured.State.Telegraph; t += Time.deltaTime)
                yield return null;
            Assert.AreEqual("Sweep", l.CurrentAttack?.name, "at its flank it should sweep, not slam");
            for (float t = 0; t < 1.5f && apprentice.Health >= apprentice.maxHealth; t += Time.deltaTime)
                yield return null;
            Assert.Less(apprentice.Health, apprentice.maxHealth, "the sweep should catch an Apprentice at its flank");
        }

        // The play-test: for each attack and defence, try every press time around the blow (1/30 s steps) and log
        // which work. The minimums are there so tuning can't quietly make a defence (near) impossible.
        [UnityTest]
        [Category("Slow")]
        [Timeout(900000)]
        public IEnumerator Measure_DodgeAndBlockWindows()
        {
            Time.captureFramerate = 60; // deterministic steps, faster than real time
            var l = laden[0];
            var report = new System.Text.StringBuilder("[Timing] press time relative to the blow (ms): o = safe, x = hit\n");
            var widths = new System.Collections.Generic.Dictionary<string, float>();
            foreach (bool sweep in new[] { false, true })
            foreach (string defence in new[] { "dash sideways", "dash back", "perfect block" })
            {
                var line = new System.Text.StringBuilder();
                float latestSafe = float.NaN, earliestSafe = float.NaN;
                for (int step = -15; step <= 4; step++)
                {
                    float offset = step / 30f;
                    FaceOff(l);
                    yield return AwaitWindup(l, sweep);
                    float due = l.CurrentAttack.ImpactTime + offset;
                    Vector3 to = l.transform.position - apprentice.transform.position;
                    to.y = 0f;
                    Vector2 away = -new Vector2(to.x, to.z).normalized, across = new Vector2(-to.z, to.x).normalized;
                    while (SinceWindup < due)
                        yield return null;
                    if (defence == "perfect block")
                        input.BlockHeld = true;
                    else
                    {
                        input.Move = defence == "dash back" ? away : across;
                        input.PressDash();
                    }
                    yield return null;
                    input.Move = Vector2.zero;
                    for (float t = 0; t < 1.2f && l.CurrentState != Soured.State.Recover && l.CurrentState != Soured.State.Stunned; t += Time.deltaTime)
                        yield return null;
                    input.BlockHeld = false;
                    bool safe = defence == "perfect block" ? l.CurrentState == Soured.State.Stunned : apprentice.Health >= apprentice.maxHealth;
                    line.Append(safe ? 'o' : 'x');
                    if (safe)
                    {
                        latestSafe = offset;
                        if (float.IsNaN(earliestSafe))
                            earliestSafe = offset;
                    }
                    yield return new WaitForSeconds(0.5f); // lets the block spam cooldown pass
                }
                string key = $"{(sweep ? "sweep" : "slam")} / {defence}";
                widths[key] = float.IsNaN(latestSafe) ? 0f : (latestSafe - earliestSafe) * 1000f;
                report.Append("  ").Append(key.PadRight(26)).Append(' ').Append(line.ToString())
                      .Append("   (-500 .. +133)  safe from ").Append((earliestSafe * 1000f).ToString("0"))
                      .Append(" to ").Append((latestSafe * 1000f).ToString("0")).Append('\n');
            }
            Time.captureFramerate = 0;
            Debug.Log(report.ToString());
            Assert.Greater(widths["slam / dash sideways"], 120f, "dodging the slam sideways must be comfortably possible");
            Assert.Less(widths["slam / dash sideways"], 400f, "dodging the slam sideways should need timing, not just a reaction to the wind-up");
            Assert.Greater(widths["sweep / dash sideways"], 120f, "dashing through the sweep must be possible");
            Assert.Greater(widths["sweep / dash back"], 120f, "backing out of the sweep must be comfortably possible");
            Assert.Greater(widths["slam / perfect block"], 100f, "the slam's perfect block window is too tight");
            Assert.Greater(widths["sweep / perfect block"], 100f, "the sweep's perfect block window is too tight");
        }

        [UnityTest]
        public IEnumerator DefeatedLaden_FallsDissolves_AndItsFareStreamsIntoTheLantern()
        {
            var l = laden[0];
            PlaceApprentice(l.transform.position - l.transform.forward * 4f, l.transform.position);
            l.TakeHit(new Hit { Damage = 1000f, From = apprentice.transform.position });
            Assert.AreEqual(Soured.State.Dying, l.CurrentState);
            Assert.IsFalse(l.IsAlive, "a dying Soured is already out of the fight (no lock, no hits)");
            int mostMotes = 0;
            for (float t = 0; t < 3f && l.CurrentState != Soured.State.Scattered; t += Time.deltaTime)
            {
                mostMotes = Mathf.Max(mostMotes, Effects.Instance.LiveFare);
                yield return null;
            }
            Assert.AreEqual(Soured.State.Scattered, l.CurrentState, "after dissolving it should be scattered");
            Assert.Greater(mostMotes, 10, "its fare should fly out as motes");
            for (float t = 0; t < 4f && Effects.Instance.LiveFare > 0; t += Time.deltaTime)
                yield return null;
            Assert.AreEqual(0, Effects.Instance.LiveFare, "every mote should reach the Apprentice's lantern");
        }

        [UnityTest]
        public IEnumerator Dash_IsInvulnerableDuringItsIFrames_ButNotAfter()
        {
            input.Move = Vector2.left;
            input.PressDash();
            yield return new WaitForSeconds((apprentice.iFrameStart + apprentice.iFrameEnd) / 2f);
            Assert.AreEqual(Apprentice.State.Dashing, apprentice.CurrentState);
            Assert.AreEqual(Defence.Dodged, apprentice.TakeHit(20f, Vector3.zero), "a hit mid-dash should pass through");
            Assert.AreEqual(apprentice.maxHealth, apprentice.Health);
            Assert.IsTrue(Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Any(r => r.name == "Dash Ghost" && r.gameObject.activeInHierarchy),
                "the i-frames should leave ghost images");

            input.Move = Vector2.zero;
            yield return new WaitForSeconds(apprentice.dashDuration + apprentice.dashRecovery);
            Assert.AreEqual(Defence.Taken, apprentice.TakeHit(20f, Vector3.zero), "after the dash a hit should land");
            Assert.Less(apprentice.Health, apprentice.maxHealth);
        }

        [UnityTest]
        public IEnumerator Laden_BlocksFromTheFront_ButNotFromBehind()
        {
            var target = Isolate(laden[0], frozen: true);
            yield return null;

            // In front of it, facing it, once its guard is up: the swing bounces off the heap.
            PlaceApprentice(target.transform.position + target.transform.forward * 1.6f, target.transform.position);
            yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(target.Guarding, "an alert Laden should have its guard up");
            float before = target.Health;
            input.PressLight();
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(before, target.Health, "a frontal hit should be blocked by the heap");
            Assert.AreEqual(Apprentice.State.Staggered, apprentice.CurrentState, "a blocked hit should make the Apprentice recoil");

            yield return new WaitForSeconds(0.6f);
            PlaceApprentice(target.transform.position - target.transform.forward * 1.6f, target.transform.position);
            input.PressLight();
            yield return new WaitForSeconds(0.35f);
            Assert.Less(target.Health, before, "a hit from behind should land");
        }

        [UnityTest]
        public IEnumerator Laden_IsOpenWhileUnaware_AndABlockedHitSparksAndShovesBack()
        {
            var target = Isolate(laden[0], frozen: true);
            target.aggroRadius = 0f; // it hasn't noticed the Apprentice
            target.noticeCloseRadius = 0f;
            PlaceApprentice(target.transform.position + target.transform.forward * 1.6f, target.transform.position);
            yield return new WaitForSeconds(0.3f);
            float before = target.Health;
            input.PressLight();
            yield return new WaitForSeconds(0.3f);
            Assert.Less(target.Health, before, "an unaware Laden's lowered heap shouldn't block");

            target.aggroRadius = 20f; // now it notices and raises its guard
            yield return new WaitForSeconds(0.9f);
            Assert.IsTrue(target.Guarding);
            PlaceApprentice(target.transform.position + target.transform.forward * 1.6f, target.transform.position);
            float distanceBefore = Vector3.Distance(apprentice.transform.position, target.transform.position);
            before = target.Health;
            input.PressLight();
            bool sparked = false;
            for (float t = 0; t < 0.6f; t += Time.captureDeltaTime) // steps, not game time: includes the hit-stop
            {
                sparked |= Effects.Instance.LiveBlockSparks > 0;
                yield return null;
            }
            Assert.AreEqual(before, target.Health, "the raised guard should block");
            Assert.IsTrue(sparked, "a blocked hit should throw sparks");
            Assert.Greater(Vector3.Distance(apprentice.transform.position, target.transform.position), distanceBefore + 0.5f,
                "a blocked hit should shove the Apprentice back");
        }

        [UnityTest]
        public IEnumerator Laden_WalksOpenFromAfar_AndGuardsUpClose()
        {
            var target = Isolate(laden[0], frozen: true);
            PlaceApprentice(target.transform.position + target.transform.forward * 5f, target.transform.position);
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(Soured.State.Chase, target.CurrentState, "it should have noticed the Apprentice");
            Assert.IsFalse(target.Guarding, "approaching from afar, the heap should be low (open)");

            PlaceApprentice(target.transform.position + target.transform.forward * 2.6f, target.transform.position);
            yield return null;
            Assert.IsFalse(target.Guarding, "the guard takes a moment to come up");
            yield return new WaitForSeconds(target.guardRaiseTime + 0.1f);
            Assert.IsTrue(target.Guarding, "up close it should raise its guard");

            PlaceApprentice(target.transform.position + target.transform.forward * 5f, target.transform.position);
            yield return null;
            yield return null;
            Assert.IsFalse(target.Guarding, "backing off should drop the guard");
        }

        [UnityTest]
        public IEnumerator Laden_StaggersWhenItsMeterFills()
        {
            var target = Isolate(laden[0], frozen: true);
            yield return null;
            bool staggered = false;
            for (int i = 0; i < 12 && !staggered; i++)
            {
                PlaceApprentice(target.transform.position - target.transform.forward * 1.6f, target.transform.position);
                input.PressHeavy();
                for (float t = 0; t < 1.2f && !staggered; t += Time.deltaTime)
                {
                    staggered = target.CurrentState == Soured.State.Staggered;
                    yield return null;
                }
            }
            Assert.IsTrue(staggered, "heavy hits from behind never staggered the Laden");
        }

        [UnityTest]
        public IEnumerator Laden_AttacksAnApprenticeStandingInFront()
        {
            var target = Isolate(laden[0]);
            PlaceApprentice(target.transform.position + target.transform.forward * 1.8f, target.transform.position);
            for (float t = 0; t < 4f && apprentice.Health >= apprentice.maxHealth; t += Time.deltaTime)
                yield return null;
            Assert.Less(apprentice.Health, apprentice.maxHealth, "the Laden never landed its slam");
        }

        [UnityTest]
        public IEnumerator Resting_HealsRefillsAndReformsTheSoured()
        {
            var behind = laden[0].transform.position - laden[0].transform.forward * 2f;
            laden[0].TakeHit(new Hit { Damage = 1000f, From = behind });
            Assert.IsFalse(laden[0].IsAlive, "setup: Laden should be scattered");
            input.PressHeal();
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(apprentice.maxHealingCharges - 1, apprentice.HealingCharges, "healing should use a charge");

            apprentice.Teleport(post.RespawnPoint);
            yield return new WaitForSeconds(0.2f);
            Assert.IsNotNull(apprentice.NearbyPost, "the respawn point should be in reach of the post");
            input.PressInteract();
            yield return null;
            yield return null;
            Assert.IsTrue(post.IsLit, "resting should light the lantern");
            Assert.AreEqual(apprentice.maxHealingCharges, apprentice.HealingCharges);
            Assert.AreEqual(apprentice.maxHealth, apprentice.Health);
            Assert.IsTrue(laden[0].IsAlive, "resting should re-form scattered Soured");

            var lantern = apprentice.GetComponent<BowLantern>();
            float dim = lantern.lanternLight.range;
            yield return new WaitForSeconds(1f);
            Assert.Greater(lantern.lanternLight.range, dim + 1f, "lighting a Mooring post should make the bow lantern grow");
        }

        [UnityTest]
        public IEnumerator FallingInTheWater_WashesAshoreWithDamage()
        {
            apprentice.Teleport(new Vector3(-12f, 1f, -15f), onSafeGround: false); // drop into open water
            yield return new WaitForSeconds(1f);
            Assert.Greater(apprentice.transform.position.y, 0.3f, "should be back on solid ground");
            Assert.AreEqual(apprentice.maxHealth * (1f - apprentice.waterDamage), apprentice.Health, 0.01f, "a third of your health");
            Assert.IsTrue(apprentice.IsAlive);
        }

        // Going under is smooth: a splash, you sink while the screen dips to black, you're moved behind it, and it
        // fades back in on you on solid ground.
        [UnityTest]
        public IEnumerator FallingInTheWater_DipsToBlack_WashesAshoreBehindIt()
        {
            var hud = Object.FindAnyObjectByType<Hud>();
            apprentice.Teleport(new Vector3(-12f, 1f, -15f), onSafeGround: false);
            yield return WaitFor(() => apprentice.CurrentState == Apprentice.State.Sinking, 1f, "going under");
            Vector3 under = apprentice.transform.position;
            yield return new WaitForSeconds(apprentice.sinkTime * 0.9f);
            yield return null;
            Assert.Greater(hud.DipAlpha, 0.9f, "dipped to black");
            Assert.Less(Vector2.Distance(new Vector2(under.x, under.z), new Vector2(apprentice.transform.position.x, apprentice.transform.position.z)), 0.1f, "still where it fell, sinking");
            yield return WaitFor(() => apprentice.CurrentState != Apprentice.State.Sinking, 1f, "washing ashore");
            Assert.Greater(apprentice.transform.position.y, 0.3f, "moved behind the black, onto solid ground");
            yield return new WaitForSeconds(0.9f);
            yield return null;
            Assert.AreEqual(0f, hud.DipAlpha, 0.01f, "and back");
        }

        // A Soured left behind (you outran it) walks back to its post and takes up its watch again.
        [UnityTest]
        public IEnumerator Soured_LeftBehind_WalksBackToItsPost()
        {
            var target = laden[0];
            Isolate(target);
            Vector3 home = target.transform.position;
            Quaternion facing = target.transform.rotation;
            PlaceApprentice(home + target.transform.forward * 3.5f, home); // in front of it (it starts with its back turned)
            yield return WaitFor(() => target.CurrentState == Soured.State.Chase, 2f, "it noticing you");
            target.attackRange = 0f; // (only following)
            yield return new WaitForSeconds(1.2f);
            Assert.Greater(Vector3.Distance(target.transform.position, home), 0.5f, "drawn from its post");
            apprentice.Teleport(home + target.transform.forward * 22f);
            yield return WaitFor(() => target.CurrentState == Soured.State.Returning, 2f, "it giving up");
            yield return WaitFor(() => target.CurrentState == Soured.State.Idle, 12f, "it back at its post");
            Assert.Less(Vector3.Distance(target.transform.position, home), 0.5f);
            Assert.Less(Quaternion.Angle(target.transform.rotation, facing), 3f, "facing the way it was");
        }

        // Locked on to something that leaves the screen, the lock lets go after a moment: the camera comes back.
        [UnityTest]
        public IEnumerator LockOn_LetsGo_WhenItsTargetLeavesTheScreen()
        {
            var target = laden[0];
            Isolate(target);
            target.aggroRadius = 0f; // (calm)
            PlaceApprentice(target.transform.position + Vector3.back * 4f, target.transform.position);
            yield return null;
            input.PressLock();
            yield return null;
            Assert.AreEqual(target, apprentice.LockTarget);
            apprentice.Teleport(target.transform.position + Vector3.back * 11f); // in range, but off the top of the screen
            yield return new WaitForSeconds(apprentice.lockOffScreenTime + 0.5f);
            Assert.IsNull(apprentice.LockTarget, "off the screen: let go");
        }

        // Low on health, the water finishes you.
        [UnityTest]
        public IEnumerator FallingInTheWater_WhenLow_Drowns()
        {
            apprentice.TakeTickDamage(apprentice.maxHealth * (1f - apprentice.waterDamage) + 1f);
            apprentice.Teleport(new Vector3(-12f, 1f, -15f), onSafeGround: false);
            yield return new WaitForSeconds(1f);
            Assert.IsFalse(apprentice.IsAlive, "the water should have finished them");
        }

        [UnityTest]
        public IEnumerator FallingOffTheBoardwalk_WashesAshoreNearby_NotAtTheStart()
        {
            // Walk halfway up the boardwalk, then step off its side.
            for (float t = 0; t < 4f && apprentice.transform.position.z < -15f; t += Time.deltaTime)
            {
                input.Move = Vector2.up;
                yield return null;
            }
            Vector3 edge = apprentice.transform.position;
            for (float t = 0; t < 1.5f && apprentice.transform.position.y > 0f; t += Time.deltaTime)
            {
                input.Move = Vector2.right;
                yield return null;
            }
            input.Move = Vector2.zero;
            yield return new WaitForSeconds(0.8f);
            Assert.Less(Vector3.Distance(apprentice.transform.position, edge), 4f,
                $"should wash up near where it fell in, not back at the start (fell near {edge}, washed up at {apprentice.transform.position})");
        }

        [UnityTest]
        public IEnumerator Dying_WakesAtTheStart_WithEverythingRestored()
        {
            apprentice.TakeHit(1000f, Vector3.zero);
            Assert.AreEqual(Apprentice.State.Dead, apprentice.CurrentState);
            yield return new WaitForSeconds(apprentice.respawnDelay + 0.5f);
            Assert.AreEqual(Apprentice.State.Free, apprentice.CurrentState);
            Assert.AreEqual(apprentice.maxHealth, apprentice.Health);
            Assert.Less(Vector3.Distance(apprentice.transform.position, WorldState.Instance.RespawnPosition), 1f);
        }

        // Swings leave a smear (a cel-style swoosh) through their active part, and it's gone a moment later; the
        // Soured's melee blows too (a shot doesn't).
        [UnityTest]
        public IEnumerator Swings_LeaveASwoosh_ThatFadesQuickly()
        {
            var trail = apprentice.GetComponent<SwingTrail>();
            Assert.IsNotNull(trail, "the Apprentice's oar should have a swoosh");
            int most = 0;
            input.PressLight();
            for (float t = 0; t < 0.4f; t += Time.deltaTime)
            {
                most = Mathf.Max(most, trail.Samples);
                yield return null;
            }
            Assert.Greater(most, 3, "a light swing should draw a smear");
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(0, trail.Samples, "and it fades a moment later");

            var target = Isolate(laden[0]);
            var ladenTrail = target.GetComponent<SwingTrail>();
            Assert.IsNotNull(ladenTrail);
            PlaceApprentice(target.transform.position + target.transform.forward * 1.8f, target.transform.position);
            most = 0;
            for (float t = 0; t < 4f && target.CurrentState != Soured.State.Recover; t += Time.deltaTime)
            {
                most = Mathf.Max(most, ladenTrail.Samples);
                yield return null;
            }
            Assert.Greater(most, 3, "the Laden's blow should draw its smear too");
        }

        [UnityTest]
        public IEnumerator HittingASoured_Thuds_AndTakingAHit_Grunts()
        {
            var target = Isolate(laden[0], frozen: true);
            PlaceApprentice(target.transform.position - target.transform.forward * 1.6f, target.transform.position);
            input.PressLight();
            for (float t = 0; t < 1f && target.Health >= target.maxHealth; t += Time.deltaTime)
                yield return null;
            Assert.Less(target.Health, target.maxHealth, "setup: the swing should land");
            Assert.AreEqual("oar_hit", Sfx.Instance.LastPlayed, "a landed hit should sound");
            yield return new WaitForSeconds(1f);
            apprentice.TakeHit(10f, Vector3.zero);
            Assert.AreEqual("apprentice_hurt", Sfx.Instance.LastPlayed, "taking a hit should sound");
        }

        // The feedback of an attack you avoid: its tell as it winds up, the whoosh of the swing, and (for the slam)
        // the crash of the heap on the stones, all without a hit.
        [UnityTest]
        public IEnumerator LadenAttack_SoundsItsTell_Swing_AndImpact_EvenWhenDodged()
        {
            var target = Isolate(laden[0]);
            var played = new System.Collections.Generic.List<string>();
            void OnPlayed(AudioClip clip, AudioSource source) => played.Add(clip.name);
            Sfx.Played += OnPlayed;
            try
            {
                PlaceApprentice(target.transform.position + target.transform.forward * 1.8f, target.transform.position);
                for (float t = 0; t < 4f && target.CurrentState != Soured.State.Telegraph; t += Time.deltaTime)
                    yield return null;
                Assert.AreEqual(Soured.State.Telegraph, target.CurrentState, "setup: the Laden should attack");
                var attack = target.CurrentAttack;
                string tell = attack.name == "Slam" ? "laden_slam_windup" : "laden_sweep_windup";
                Assert.Contains(tell, played, "the wind-up should sound its tell");

                // Dash late, sideways towards the middle of the quay: a clean dodge.
                for (float since = target.StateTime; since < attack.ImpactTime - 0.2f; since += Time.deltaTime)
                    yield return null;
                Vector3 to = target.transform.position - apprentice.transform.position;
                Vector3 side = Vector3.Cross(Vector3.up, to).normalized;
                if (Vector3.Dot(side, -apprentice.transform.position) < 0f)
                    side = -side;
                input.Move = new Vector2(side.x, side.z);
                input.PressDash();
                for (float t = 0; t < 2f && target.CurrentState != Soured.State.Recover; t += Time.deltaTime)
                    yield return null;
                input.Move = Vector2.zero;

                Assert.AreEqual(apprentice.maxHealth, apprentice.Health, "setup: the dash should have dodged it");
                Assert.IsTrue(played.Any(n => n.StartsWith("laden_swing")), "the swing should whoosh, hit or miss");
                if (attack.name == "Slam")
                    Assert.IsTrue(played.Any(n => n.StartsWith("laden_slam_impact")), "the slam should crash down, hit or miss");
                else
                    Assert.IsFalse(played.Any(n => n.StartsWith("laden_slam_impact")), "a sweep has no ground impact");
            }
            finally
            {
                Sfx.Played -= OnPlayed;
            }
        }

        [UnityTest]
        public IEnumerator InterruptedWindup_CutsItsTell()
        {
            var target = Isolate(laden[0]);
            AudioSource tellVoice = null;
            void OnPlayed(AudioClip clip, AudioSource source) { if (clip.name.Contains("windup")) tellVoice = source; }
            Sfx.Played += OnPlayed;
            try
            {
                PlaceApprentice(target.transform.position + target.transform.forward * 1.8f, target.transform.position);
                for (float t = 0; t < 4f && target.CurrentState != Soured.State.Telegraph; t += Time.deltaTime)
                    yield return null;
                Assert.IsNotNull(tellVoice, "setup: the wind-up should sound");
                if (!tellVoice.isPlaying)
                    Assert.Ignore("no audio device: can't tell whether the tell was cut");
                target.Stun(); // e.g. knocked out of it
                yield return null;
                Assert.IsFalse(tellVoice.isPlaying, "no tell for a blow that won't come");
            }
            finally
            {
                Sfx.Played -= OnPlayed;
            }
        }

        [UnityTest]
        public IEnumerator DefeatedSoured_DropsItsLantern_WhereItFell_UntilItReforms()
        {
            var target = Isolate(laden[0], frozen: true);
            var dropped = target.droppedLantern;
            Assert.IsNotNull(dropped, "the Laden should carry a lantern to drop");
            Vector3 fell = target.transform.position;
            target.TakeHit(new Hit { Damage = 1000f, From = fell - target.transform.forward * 2f });
            yield return new WaitForSeconds(2.5f);
            Assert.AreEqual(Soured.State.Scattered, target.CurrentState);
            Assert.IsTrue(dropped.gameObject.activeSelf && dropped.Landed, "the lantern should lie where the Laden fell");
            Vector3 p = dropped.transform.position;
            Assert.Less(Vector2.Distance(new Vector2(p.x, p.z), new Vector2(fell.x, fell.z)), 2.5f);
            Assert.AreEqual(fell.y, p.y, 0.15f, "on the quay, not floating or sunk");
            var light = dropped.carried;
            Assert.IsTrue(light.enabled && light.transform.IsChildOf(dropped.transform), "its light should go with it");
            Assert.Less(Vector3.Distance(light.transform.position, p), 0.8f); // just above it (plus the flicker's wobble)

            target.Reform();
            yield return null;
            Assert.IsFalse(dropped.gameObject.activeSelf, "re-formed, it has its lantern back");
            Assert.IsTrue(light.enabled && light.transform.IsChildOf(target.transform));
        }

        [UnityTest]
        public IEnumerator DefeatingASoured_EarnsItsFare()
        {
            var target = Isolate(laden[0], frozen: true);
            target.TakeHit(new Hit { Damage = 1000f, From = target.transform.position - target.transform.forward * 2f });
            Assert.AreEqual(target.obols, apprentice.Obols, "its Obols should be the Apprentice's");
            yield return new WaitForSeconds(2f);
            yield return null;
            Assert.AreEqual(target.obols, Object.FindAnyObjectByType<Hud>().ObolsShown, "the counter should have ticked up");
        }

        [UnityTest]
        public IEnumerator Dying_LeavesAShadeWithTheObols_TouchingItReturnsThem()
        {
            Vector3 fell = new Vector3(2.4f, 0.55f, -13f); // out on the boardwalk, away from the Mooring post
            apprentice.Teleport(fell);
            apprentice.GainObols(120);
            yield return new WaitForSeconds(0.2f);
            apprentice.TakeHit(1000f, Vector3.zero);
            Assert.AreEqual(0, apprentice.Obols, "dying drops every Obol");
            Assert.IsNotNull(Shade.Current, "a Shade should stay where the Apprentice fell");
            Assert.AreEqual(120, Shade.Current.Obols);
            Assert.Less(Vector2.Distance(new Vector2(Shade.Current.transform.position.x, Shade.Current.transform.position.z), new Vector2(fell.x, fell.z)), 0.5f);

            yield return new WaitForSeconds(apprentice.respawnDelay + 0.5f);
            Assert.IsTrue(apprentice.IsAlive);
            Assert.IsNotNull(Shade.Current, "the Shade waits while the Apprentice is elsewhere");
            var shade = Shade.Current;
            apprentice.Teleport(fell);
            yield return null;
            yield return null;
            Assert.AreEqual(120, apprentice.Obols, "touching the Shade returns its Obols");
            Assert.IsTrue(shade.Claimed);
            Assert.IsNull(Shade.Current);
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(shade == null, "the Shade should fade away once claimed");
        }

        [UnityTest]
        public IEnumerator DyingAgain_BeforeReachingTheShade_LosesItsObols()
        {
            apprentice.Teleport(new Vector3(2.4f, 0.55f, -13f));
            apprentice.GainObols(120);
            yield return new WaitForSeconds(0.2f);
            apprentice.TakeHit(1000f, Vector3.zero);
            var first = Shade.Current;
            yield return new WaitForSeconds(apprentice.respawnDelay + 0.5f);
            apprentice.Teleport(new Vector3(2.4f, 0.55f, -18f)); // somewhere else on the boardwalk
            yield return new WaitForSeconds(0.2f);
            apprentice.TakeHit(1000f, Vector3.zero);
            Assert.IsNull(Shade.Current, "carrying nothing, the second death leaves no Shade");
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(first == null, "the first Shade, and its Obols, are gone");
            Assert.AreEqual(0, apprentice.Obols);
        }

        [UnityTest]
        public IEnumerator Merchant_SellsTheCharm_ForObols_WhileTheApprenticeStandsStill()
        {
            var merchant = Object.FindAnyObjectByType<Merchant>();
            Assert.IsNotNull(merchant, "the merchant soul should wait on the landing");
            var hud = Object.FindAnyObjectByType<Hud>();
            Vector3 towardsPost = post.RespawnPoint - merchant.transform.position;
            towardsPost.y = 0f;
            apprentice.Teleport(merchant.transform.position + towardsPost.normalized * 1.1f); // on the landing, in front of it
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(merchant, apprentice.NearbyMerchant);
            Assert.AreEqual("Trade", hud.PromptText);

            input.PressInteract();
            yield return null;
            var shop = apprentice.OpenMenu as ShopMenu;
            Assert.IsNotNull(shop, "Interact should open the merchant's wares");
            Assert.AreEqual("Eel Skin", shop.Rows[0].Label);
            Vector3 standing = apprentice.transform.position;
            input.Move = Vector2.right;
            input.PressInteract();
            yield return new WaitForSeconds(0.3f);
            input.Move = Vector2.zero;
            Assert.Less(Vector3.Distance(standing, apprentice.transform.position), 0.05f, "browsing, the Apprentice stands still");
            Assert.IsFalse(apprentice.Equipment.Owns(Charm.EelSkin), "no Obols, no sale");
            Assert.AreEqual("Not enough Obols.", shop.Notice);

            apprentice.GainObols(150);
            input.PressInteract();
            yield return null;
            Assert.IsTrue(apprentice.Equipment.Owns(Charm.EelSkin));
            Assert.AreEqual(150 - Charm.EelSkin.Price, apprentice.Obols);
            Assert.IsFalse(apprentice.Equipment.IsFitted(Charm.EelSkin), "bought, not yet fitted");
            yield return null;
            Assert.IsTrue(hud.Menu.Visible);

            float stamina = apprentice.Stamina;
            input.PressDash();
            yield return null;
            Assert.IsNull(apprentice.OpenMenu, "Dash leaves the menu");
            Assert.AreEqual(Apprentice.State.Free, apprentice.CurrentState, "leaving shouldn't also dash");
            Assert.AreEqual(stamina, apprentice.Stamina);
        }

        [UnityTest]
        public IEnumerator EelSkin_FittedAtAPost_HalvesTheDashCost()
        {
            Assert.AreEqual(apprentice.dashStaminaCost, apprentice.DashCost);
            apprentice.Equipment.Own(Charm.EelSkin);
            apprentice.Teleport(post.RespawnPoint);
            yield return new WaitForSeconds(0.2f);
            input.PressInteract();
            yield return null;
            Assert.IsInstanceOf<PostMenu>(apprentice.OpenMenu, "Resting with Equipment to fit should open the post's menu");
            // Down to the Eel Skin (the oar's row first, then the dev Tester's Knot, then it).
            for (int i = 0; i < 6 && (apprentice.OpenMenu.Selected == 0 || apprentice.Equipment.Owned[apprentice.OpenMenu.Selected - 1] != Charm.EelSkin); i++)
            {
                input.Move = Vector2.zero;
                yield return null;
                input.Move = Vector2.down;
                yield return null;
            }
            input.Move = Vector2.zero;
            input.PressInteract();
            yield return null;
            Assert.IsTrue(apprentice.Equipment.IsFitted(Charm.EelSkin));
            input.PressDash();
            yield return null;
            Assert.IsNull(apprentice.OpenMenu, "Dash stands up");
            Assert.AreEqual(apprentice.dashStaminaCost * 0.5f, apprentice.DashCost);

            yield return new WaitForSeconds(0.3f);
            float before = apprentice.Stamina;
            input.Move = Vector2.up;
            input.PressDash();
            yield return new WaitForSeconds(0.1f);
            input.Move = Vector2.zero;
            Assert.AreEqual(Apprentice.State.Dashing, apprentice.CurrentState);
            Assert.AreEqual(before - apprentice.dashStaminaCost * 0.5f, apprentice.Stamina, 0.01f, "the dash should cost half");
        }

        // A heavy blocked by the heap must be seen to land: the block happens when the blade comes down (not while
        // it's still overhead), and only within the oar's real reach.
        [UnityTest]
        public IEnumerator HeavyAttack_BlockedByTheHeap_LandsBeforeItBounces()
        {
            var target = Isolate(laden[0], frozen: true);
            target.attackRange = 0f;
            target.sweepChanceInFront = 0f;
            PlaceApprentice(target.transform.position + target.transform.forward * 2.6f, target.transform.position);
            for (float t = 0; t < 2f && !target.Guarding; t += Time.deltaTime)
                yield return null;
            Assert.IsTrue(target.Guarding, "setup: the Laden should raise its heap");

            input.PressHeavy();
            float pressed = Time.time;
            for (float t = 0; t < 1.5f && apprentice.CurrentState != Apprentice.State.Staggered; t += Time.deltaTime)
                yield return null;
            Assert.AreEqual(Apprentice.State.Staggered, apprentice.CurrentState, "the heavy should bounce off the heap");
            float contact = apprentice.heavyWindup + apprentice.heavyActive * apprentice.heavyContact;
            Assert.GreaterOrEqual(Time.time - pressed, contact - 0.02f, "blocked while the oar was still overhead");
            Assert.AreEqual("block_clang", Sfx.Instance.LastPlayed);

            // From further than the oar reaches (even with the lunge), it's a miss, not a bounce.
            yield return new WaitForSeconds(1f);
            PlaceApprentice(target.transform.position + target.transform.forward * 4.2f, target.transform.position);
            yield return new WaitForSeconds(0.3f);
            input.PressHeavy();
            for (float t = 0; t < 1.2f; t += Time.deltaTime)
            {
                Assert.AreNotEqual(Apprentice.State.Staggered, apprentice.CurrentState, "a heavy from 4.2 m can't reach the heap");
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Healing_GathersWarmLight_ThenBursts()
        {
            var glow = apprentice.GetComponent<HealGlow>();
            Assert.IsNotNull(glow);
            apprentice.TakeHit(apprentice.maxHealth * 0.5f, Vector3.zero); // (hurt, not dead: whatever the health's tuned to)
            yield return new WaitForSeconds(1f);
            float hurt = apprentice.Health;
            input.PressHeal();
            yield return null;
            Assert.AreEqual("heal", Sfx.Instance.LastPlayed);
            yield return new WaitForSeconds(0.35f);
            Assert.Greater(glow.LiveEmbers, 10, "embers should gather while healing");
            Assert.Greater(glow.GlowIntensity, 0.5f, "a warm light should swell");
            yield return new WaitForSeconds(0.5f);
            Assert.Greater(apprentice.Health, hurt, "setup: the heal should have taken effect");
            yield return new WaitForSeconds(1.5f);
            Assert.Less(glow.GlowIntensity, 0.05f, "and fade once it's done");
        }

        // Knocked off the quay, a Soured drowns: it counts as a kill, and it doesn't fall (and drag a locked-on
        // camera) forever.
        [UnityTest]
        public IEnumerator SouredInTheWater_Drowns_AsAKill_AndReleasesTheLock()
        {
            var target = Isolate(laden[0], frozen: true);
            PlaceApprentice(target.transform.position - target.transform.forward * 2f, target.transform.position);
            input.PressLock();
            yield return null;
            Assert.AreEqual(target, apprentice.LockTarget, "setup: locked on");
            var played = new System.Collections.Generic.List<string>();
            void OnPlayed(AudioClip clip, AudioSource source) => played.Add(clip.name);
            Sfx.Played += OnPlayed;
            var controller = target.GetComponent<CharacterController>();
            controller.enabled = false;
            target.transform.position = new Vector3(-12f, 0.6f, -15f); // over open water
            controller.enabled = true;
            for (float t = 0; t < 2f && target.IsAlive; t += Time.deltaTime)
                yield return null;
            Assert.IsTrue(target.Drowned, "in the water, it should drown");
            Sfx.Played -= OnPlayed;
            Assert.IsTrue(played.Any(n => n.StartsWith("splash")), "it should splash going in");
            Assert.AreEqual(target.obols, apprentice.Obols, "a drowned Soured still pays its fare");
            yield return null;
            Assert.IsNull(apprentice.LockTarget, "the lock lets go of it");
            yield return new WaitForSeconds(2.5f);
            Assert.AreEqual(Soured.State.Scattered, target.CurrentState);
            Assert.Greater(target.transform.position.y, -3f, "it sinks a little, it doesn't fall forever");

            target.Reform();
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(target.Drowned);
            Assert.IsTrue(target.IsAlive);
            Assert.Greater(target.transform.position.y, 0.3f, "re-formed on the quay");
        }

        // Chasing the Apprentice across water, a Laden keeps to the quay: it follows the edge round to the boardwalk
        // instead of walking into the water.
        [UnityTest]
        public IEnumerator Laden_ChasingAcrossWater_KeepsToSolidGround()
        {
            var target = Isolate(laden[0]);
            target.aggroRadius = 12f;
            target.attackRange = 0f;
            target.sweepChanceInFront = 0f;
            var controller = target.GetComponent<CharacterController>();
            controller.enabled = false;
            target.transform.position = new Vector3(-5f, 0.5f, -3.5f); // the quay's south-west corner
            controller.enabled = true;
            PlaceApprentice(new Vector3(2.4f, 0.55f, -9.5f), target.transform.position); // on the boardwalk: water between them
            Vector3 to = apprentice.transform.position - target.transform.position;
            to.y = 0f;
            target.transform.rotation = Quaternion.LookRotation(to);
            float closest = float.MaxValue;
            for (float t = 0; t < 8f; t += Time.deltaTime)
            {
                Assert.IsFalse(target.Drowned, "it walked into the water");
                Assert.Greater(target.transform.position.y, 0.2f, $"it left solid ground at {target.transform.position}");
                closest = Mathf.Min(closest, Vector3.Distance(target.transform.position, apprentice.transform.position));
                yield return null;
            }
            Assert.AreNotEqual(Soured.State.Idle, target.CurrentState, "setup: it should have given chase");
            Assert.Less(closest, 5f, "it should find the way round, along the edge and onto the boardwalk");
        }

        // ---- The loop and its shortcut

        // Walk the whole loop, landing -> quay -> yard -> crossroads -> loopback, open the gate from the far side and
        // step back onto the landing: every piece connects, and nowhere along it do you fall in.
        [UnityTest]
        public IEnumerator TheLoop_IsWalkableAllTheWayRound_AndTheGateOpensHome()
        {
            foreach (var s in Object.FindObjectsByType<Soured>(FindObjectsSortMode.None))
                s.gameObject.SetActive(false); // just the way itself
            var gate = Object.FindAnyObjectByType<ShortcutGate>();
            var route = new[]
            {
                new Vector2(2.4f, -6f), new Vector2(2.2f, -2.5f), new Vector2(-6.8f, -2f), new Vector2(-17.5f, -2f),
                new Vector2(-24.5f, -1.5f), new Vector2(-25.5f, -4f), new Vector2(-25.5f, -19.8f), new Vector2(-23.5f, -24.5f),
                new Vector2(-15.5f, -25.6f), new Vector2(-1.3f, -25.6f),
            };
            foreach (var point in route)
                yield return WalkTo(point, 12f);
            Assert.IsFalse(gate.IsOpen);
            Assert.AreEqual(gate, apprentice.Nearby, "at the gate, from the crossroads side");
            Assert.AreEqual("Open", apprentice.Nearby.PromptLabel);
            input.PressInteract();
            yield return new WaitForSeconds(1.2f);
            Assert.IsTrue(gate.IsOpen, "it opens from this side");
            yield return WalkTo(new Vector2(2.2f, -25.6f), 5f);
            Assert.AreEqual(apprentice.maxHealth, apprentice.Health, "the whole way round without falling in");
        }

        [UnityTest]
        public IEnumerator ShortcutGate_IsBarredFromTheLandingSide_AndStaysOpenOnceOpened()
        {
            var gate = Object.FindAnyObjectByType<ShortcutGate>();
            apprentice.Teleport(new Vector3(1.3f, 0.55f, -25.6f)); // the landing side
            yield return new WaitForSeconds(0.2f);
            yield return null;
            Assert.AreEqual(gate, apprentice.Nearby, "the gate, not the merchant or the post");
            Assert.AreEqual("Barred", apprentice.Nearby.PromptLabel);
            input.PressInteract();
            yield return new WaitForSeconds(1.2f);
            Assert.IsFalse(gate.IsOpen, "it can't be opened from the landing side");
            Assert.AreEqual("gate_rattle", Sfx.Instance.LastPlayed);
            float before = apprentice.transform.position.x;
            for (float t = 0; t < 0.8f; t += Time.deltaTime)
            {
                input.Move = Vector2.left;
                yield return null;
            }
            input.Move = Vector2.zero;
            Assert.Greater(apprentice.transform.position.x, -0.9f, "barred: no walking through it");

            apprentice.Teleport(new Vector3(-1.4f, 0.55f, -25.6f)); // the far side
            yield return new WaitForSeconds(0.2f);
            input.PressInteract();
            yield return new WaitForSeconds(1.2f);
            Assert.IsTrue(gate.IsOpen);
            Assert.Greater(gate.Swing, 90f, "swung open");
            apprentice.TakeHit(1000f, Vector3.zero);
            yield return new WaitForSeconds(apprentice.respawnDelay + 1f);
            Assert.IsTrue(apprentice.IsAlive);
            Assert.IsTrue(gate.IsOpen, "the way home stays open after dying");
        }

        IEnumerator WalkTo(Vector2 point, float timeout)
        {
            for (float t = 0; t < timeout; t += Time.deltaTime)
            {
                Vector2 here = new Vector2(apprentice.transform.position.x, apprentice.transform.position.z);
                Vector2 to = point - here;
                if (to.magnitude < 0.4f)
                {
                    input.Move = Vector2.zero;
                    yield break;
                }
                Assert.Greater(apprentice.transform.position.y, 0.2f, $"fell off on the way to {point} (at {here})");
                Assert.AreEqual(apprentice.maxHealth, apprentice.Health, $"washed ashore on the way to {point}");
                input.Move = to.normalized * Mathf.Clamp01(to.magnitude + 0.3f);
                yield return null;
            }
            Assert.Fail($"couldn't reach {point}: stuck at {apprentice.transform.position}");
        }

        // ---- The Hook

        // ---- The Hook in combat

        // A light Soured (the archer) caught on the Hook is yanked over to land in front of you, and reels.
        [UnityTest]
        public IEnumerator Hook_YanksALightSoured_OverToYou()
        {
            Isolate(archer);
            archer.aggroRadius = 0f; // (calm: it won't shoot)
            apprentice.GiveHook();
            PlaceApprentice(archer.transform.position + archer.transform.forward * 7f, archer.transform.position);
            yield return new WaitForSeconds(0.2f);
            float stamina = apprentice.Stamina;
            input.HookHeld = true;
            yield return null;
            yield return null;
            Assert.AreEqual(archer, apprentice.HookFoe, "a Soured in reach: the Hook aims at it (over any post)");
            Assert.IsNull(apprentice.HookTarget);
            input.HookHeld = false;
            yield return WaitFor(() => archer.CurrentState == Soured.State.Staggered, 2f, "it yanked over, reeling");
            Assert.Less(FlatDistance(archer.transform.position, apprentice.transform.position), 2.2f, "in front of you");
            Assert.Less(apprentice.Stamina, stamina, "the throw costs stamina");
        }

        // A Laden comes too, heap and all (a little slower on the chain), and takes a moment to find its feet.
        [UnityTest]
        public IEnumerator Hook_YanksALaden_Too()
        {
            var target = Isolate(laden[0]);
            target.attackRange = 0f; // (only following)
            apprentice.GiveHook();
            PlaceApprentice(target.transform.position + target.transform.forward * 5f, target.transform.position);
            yield return WaitFor(() => target.CurrentState == Soured.State.Chase, 3f, "it noticing you");
            input.HookHeld = true;
            yield return null;
            yield return null;
            Assert.AreEqual(target, apprentice.HookFoe);
            input.HookHeld = false;
            yield return WaitFor(() => target.Yanked, 1f, "it pulled");
            yield return WaitFor(() => target.CurrentState == Soured.State.Staggered, 2f, "it landed, reeling");
            Assert.Less(FlatDistance(target.transform.position, apprentice.transform.position), 2.4f, "in front of you");
        }

        [UnityTest]
        public IEnumerator WithoutTheHook_NothingIsAimed_OrPulled()
        {
            foreach (var s in Object.FindObjectsByType<Soured>(FindObjectsSortMode.None))
                s.gameObject.SetActive(false);
            PlaceApprentice(new Vector3(-29f, 0.55f, 3.4f), new Vector3(-29f, 0.55f, 9.5f)); // the yard's hook post, facing the islet
            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(apprentice.HasHook);
            input.HookHeld = true;
            yield return new WaitForSeconds(0.3f);
            Assert.IsNull(apprentice.HookTarget, "no aiming without the Hook (and no camera lean)");
            input.HookHeld = false;
            yield return new WaitForSeconds(0.5f);
            Assert.AreNotEqual(Apprentice.State.Hooking, apprentice.CurrentState);
        }

        // Hook posts never glow; their iron ring catches the moonlight in a tiny glint now and then while you're near
        // (standing still too), and not at all from far off.
        [UnityTest]
        public IEnumerator HookPosts_GlintNowAndThen_WhileYoureNear()
        {
            var post = Object.FindObjectsByType<HookPost>(FindObjectsSortMode.None).OrderBy(p => FlatDistance(p.transform.position, new Vector3(1.55f, 0f, -17.3f))).First();
            int far = post.Glints;
            yield return new WaitForSeconds(6f); // on the landing: too far away
            Assert.AreEqual(far, post.Glints, "no glints from far off");
            apprentice.Teleport(new Vector3(2.4f, 0.55f, -18.6f)); // on the boardwalk beside it, its ring in view
            int before = post.Glints;
            yield return new WaitForSeconds(12f); // standing still
            Assert.Greater(post.Glints, before, "near it, even standing still, its ring should catch the moonlight");
            Assert.LessOrEqual(post.Glints - before, 9, "now and then, not all the time");
        }

        [UnityTest]
        public IEnumerator Hook_ReachesTheObolCache_SeenOnTheWayIn()
        {
            foreach (var s in Object.FindObjectsByType<Soured>(FindObjectsSortMode.None))
                s.gameObject.SetActive(false);
            apprentice.GrantHook();
            Vector3 deck = new Vector3(-3.4f, 0.55f, -19.0f);
            PlaceApprentice(new Vector3(2.4f, 0.55f, -18.2f), deck);
            yield return new WaitForSeconds(0.3f);
            input.HookHeld = true;
            yield return new WaitForSeconds(0.2f);
            Assert.IsNotNull(apprentice.HookTarget, "the deck's post, across the water");
            input.HookHeld = false;
            for (float t = 0; t < 2f && (apprentice.CurrentState == Apprentice.State.Hooking || t < 0.1f); t += Time.deltaTime)
                yield return null;
            Assert.Less(FlatDistance(apprentice.transform.position, deck), 0.6f, "on the deck");
            yield return new WaitForSeconds(0.3f);
            int before = apprentice.Obols;
            Assert.IsInstanceOf<ObolCache>(apprentice.Nearby);
            input.PressInteract();
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(before + 120, apprentice.Obols);
        }

        // Aiming from inside the yard at the yard's own post would be pointless        // Aiming from inside the yard at the yard's own post would be pointless (and swing the camera about).
        [UnityTest]
        public IEnumerator Hook_NeverAimsAtAPostOnTheGroundYouStandOn()
        {
            foreach (var s in Object.FindObjectsByType<Soured>(FindObjectsSortMode.None))
                s.gameObject.SetActive(false);
            apprentice.GrantHook();
            PlaceApprentice(new Vector3(-29f, 0.55f, -1.5f), new Vector3(-29f, 0.55f, 5f)); // mid-yard, facing its hook post
            input.HookHeld = true;
            yield return new WaitForSeconds(0.3f);
            Assert.IsNull(apprentice.HookTarget, "the yard's own post (and the islet beyond reach) mustn't be aimed at");
            input.HookHeld = false;
            yield return new WaitForSeconds(0.3f);
            Assert.AreNotEqual(Apprentice.State.Hooking, apprentice.CurrentState);
        }

        // The way to her: from the crossroads, south along a boardwalk to the second Mooring post, then west into the
        // drowned chapel, on foot all the way (no gaps, no water).
        [UnityTest]
        public IEnumerator TheWayToTheChapel_IsWalkable_PastASecondMooringPost()
        {
            foreach (var s in Object.FindObjectsByType<Soured>(FindObjectsSortMode.None))
                s.gameObject.SetActive(false);
            var chapelPost = Object.FindObjectsByType<MooringPost>(FindObjectsSortMode.None).First(p => p != post);
            apprentice.Teleport(new Vector3(-20.2f, 0.55f, -28.2f)); // the crossroads' south side
            yield return new WaitForSeconds(0.2f);
            yield return WalkTo(new Vector2(chapelPost.RespawnPoint.x, chapelPost.RespawnPoint.z), 6f);
            yield return null;
            Assert.AreEqual(chapelPost, apprentice.Nearby, "the second Mooring post, before her");
            var boss = Object.FindAnyObjectByType<Anchoress>(FindObjectsInactive.Include);
            Vector3 mark = boss.rewardSpot.position;
            yield return WalkTo(new Vector2(mark.x + 4f, mark.z), 6f);
            Assert.Less(Vector3.Distance(apprentice.transform.position, mark), 4.6f, "in the chapel");
        }

        // The Hook is hers to give: in the drowned chapel, south of the crossroads past a second Mooring post. Beaten,
        // she lets it go; it comes to rest on his mark, and taking it is the key-item ceremony. For good.
        [UnityTest]
        public IEnumerator TheHook_TheAnchoressLetsItGo_TakeItFromHisMark()
        {
            var boss = Object.FindAnyObjectByType<Anchoress>();
            Assert.IsNotNull(boss, "the Anchoress, in the village");
            Assert.AreEqual(2, Object.FindObjectsByType<MooringPost>(FindObjectsSortMode.None).Length, "a second Mooring post, before her");
            Assert.IsNull(Object.FindAnyObjectByType<AbilityRelic>(), "no relic: she gives the Hook");
            Assert.IsFalse(apprentice.HasHook);
            boss.TakeHit(new Hit { Damage = 100000f, From = boss.transform.position - boss.transform.forward * 2f });
            yield return WaitFor(() => boss.FreedHook != null && boss.FreedHook.Landed, 10f, "his hook, come to rest");
            var hook = boss.FreedHook;
            Assert.Less(Vector3.Distance(hook.transform.position, boss.rewardSpot.position), 0.1f, "on his mark");
            apprentice.Teleport(hook.transform.position + Vector3.right * 0.9f);
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(hook, apprentice.Nearby);
            Assert.AreEqual("Take", apprentice.Nearby.PromptLabel);
            Assert.IsFalse(apprentice.HookShown, "not carried before it's found");
            input.PressInteract();
            yield return null;
            yield return null;
            Assert.IsTrue(apprentice.HasHook);
            Assert.IsTrue(hook.Taken);
            Assert.AreEqual(Apprentice.State.Ceremony, apprentice.CurrentState, "a key item is held up");
            Assert.IsTrue(apprentice.Invulnerable, "untouchable while it's held up");
            Assert.IsTrue(apprentice.HookShown, "the Hook in the Apprentice's hands");
            Assert.AreEqual("key_item", Sfx.Instance.LastPlayed);
            var hud = Object.FindAnyObjectByType<Hud>();
            yield return new WaitForSeconds(1.2f);
            Assert.IsTrue(hud.FoundBanner.Showing, "then its banner");
            Assert.AreEqual("The Hook", hud.FoundBanner.Text.text);
            yield return new WaitForSeconds(1.4f);
            Assert.AreEqual(Apprentice.State.Free, apprentice.CurrentState, "and back to it");

            apprentice.TakeHit(1000f, Vector3.zero);
            yield return new WaitForSeconds(apprentice.respawnDelay + 0.5f);
            Assert.IsTrue(apprentice.HasHook, "an Ability is for good: dying doesn't take it");
            Assert.IsFalse(boss.IsAlive, "and she stays beaten");
        }

        static IEnumerator WaitFor(System.Func<bool> condition, float timeout, string what)
        {
            for (float t = 0; t < timeout; t += Time.deltaTime)
            {
                if (condition())
                    yield break;
                yield return null;
            }
            Assert.Fail($"timed out waiting for {what}");
        }

        [UnityTest]
        public IEnumerator Hook_PullsAcrossTheWater_ToTheIslet_TakesTheCharge_AndBack()
        {
            foreach (var s in Object.FindObjectsByType<Soured>(FindObjectsSortMode.None))
                s.gameObject.SetActive(false);
            apprentice.GrantHook();
            var hud = Object.FindAnyObjectByType<Hud>();
            Vector3 yard = new Vector3(-29f, 0.55f, 3.4f), islet = new Vector3(-29f, 0.55f, 9.5f);
            PlaceApprentice(yard, islet);
            yield return new WaitForSeconds(0.3f);
            yield return null;
            Assert.IsNotNull(apprentice.HookTarget, "with the Hook, the islet's post in reach ahead is ringed");
            Assert.IsTrue(hud.HookMarkerVisible);
            float dim = hud.HookMarkerAlpha;
            input.HookHeld = true;
            yield return new WaitForSeconds(0.2f);
            yield return null;
            Assert.IsNotNull(apprentice.HookTarget, "aiming at it");
            Assert.Greater(hud.HookMarkerAlpha, dim + 0.3f, "and the ring brightens while aiming");
            Assert.IsTrue(apprentice.HookShown, "carried at the hip (whirled while aiming)");
            input.HookHeld = false; // let go: throw
            yield return null;
            yield return null;
            Assert.AreEqual(Apprentice.State.Hooking, apprentice.CurrentState);
            Assert.IsFalse(apprentice.HookShown, "out on the tether, not at the hip");
            for (float t = 0; t < 2f && apprentice.CurrentState == Apprentice.State.Hooking; t += Time.deltaTime)
            {
                Assert.Greater(apprentice.transform.position.y, 0.2f, "pulled over the water, not through it");
                yield return null;
            }
            Assert.Less(FlatDistance(apprentice.transform.position, islet), 0.6f, "set down on the islet");
            Assert.AreEqual(apprentice.maxHealth, apprentice.Health, "no washing ashore on the way");

            yield return new WaitForSeconds(0.3f);
            Assert.IsInstanceOf<ChargePiece>(apprentice.Nearby, "the charge piece is right there");
            int before = apprentice.maxHealingCharges;
            input.PressInteract();
            yield return null;
            Assert.AreEqual(Apprentice.State.PickingUp, apprentice.CurrentState, "it stoops to pick it up");
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(before + 1, apprentice.maxHealingCharges, "one more Healing charge, for good");
            Assert.AreEqual(apprentice.maxHealingCharges, apprentice.HealingCharges);

            // And back: face the yard (the post you stand by isn't a target) and pull.
            input.HookHeld = true;
            input.Move = Vector2.down * 0.3f; // steer the aim back towards the yard (slowly: aiming)
            yield return new WaitForSeconds(0.2f);
            Assert.IsNotNull(apprentice.HookTarget, "the yard's post, back across");
            input.Move = Vector2.zero;
            input.HookHeld = false;
            for (float t = 0; t < 2.5f && FlatDistance(apprentice.transform.position, yard) > 0.6f; t += Time.deltaTime)
                yield return null;
            Assert.Less(FlatDistance(apprentice.transform.position, yard), 0.6f, "back in the yard");
        }

        // ---- Old Hesper, a Lingerer

        // Speak with her: her lines one at a time, typed out as she says them (Interact finishes one, then goes on),
        // in a band at the bottom of the screen; Dash leaves. The second time, she's waiting: his light's still out there.
        [UnityTest]
        public IEnumerator Hesper_TellsOfHerSon_OneLineAtATime()
        {
            var hesper = Object.FindAnyObjectByType<Lingerer>();
            Assert.IsNotNull(hesper, "Old Hesper sits on the landing");
            var hud = Object.FindAnyObjectByType<Hud>();
            Assert.Greater(Vector3.Distance(apprentice.transform.position, hesper.InteractPosition), hesper.InteractRadius,
                "not in the way where you start");
            apprentice.Teleport(hesper.InteractPosition + Vector3.right * 0.9f);
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(hesper, apprentice.Nearby);
            Assert.AreEqual("Speak", hud.PromptText);

            input.PressInteract();
            yield return null;
            var talk = apprentice.OpenMenu as Conversation;
            Assert.IsNotNull(talk, "Interact speaks with her");
            Assert.AreEqual(Lingerer.Business.Waiting, hesper.State);
            Assert.AreEqual("Oh. You're not him.", talk.CurrentLine);
            yield return new WaitForSeconds(0.1f);
            yield return null;
            Assert.IsTrue(hud.Dialogue.Visible);
            Assert.AreEqual("Old Hesper, who keeps a light", hud.Dialogue.SpeakerText);
            Assert.Less(hud.PromptAlpha, 0.9f, "no Speak prompt over her words");
            Assert.IsFalse(talk.LineDone, "the words come out as she says them");
            input.PressInteract();
            yield return null;
            Assert.IsTrue(talk.LineDone, "Interact finishes the line");
            Assert.AreEqual(0, talk.Line);
            input.PressInteract();
            yield return null;
            Assert.AreEqual(1, talk.Line, "then goes on");
            StringAssert.Contains("my boy", talk.CurrentLine);
            input.PressDash();
            yield return null;
            Assert.IsNull(apprentice.OpenMenu, "Dash leaves");
            Assert.AreEqual(Apprentice.State.Free, apprentice.CurrentState);

            yield return new WaitForSeconds(0.2f);
            input.PressInteract();
            yield return null;
            talk = apprentice.OpenMenu as Conversation;
            Assert.IsNotNull(talk);
            StringAssert.Contains("You're back", talk.CurrentLine, "an aside first, a different one each visit");
            talk.Skip();
            StringAssert.Contains("hook", talk.CurrentLine, "without the Hook, she tells how the Ferryman crossed");
            talk.Close();
        }

        // The errand: his boat-lantern, on the islet north of the yard (the Hook's), carried back to her. She holds
        // it, lit; then asks after her fare (optional, and asked again later); paid, she goes to wait by the boat.
        [UnityTest]
        public IEnumerator Hesper_HisLantern_Brought_ThenHerFare()
        {
            foreach (var s in Object.FindObjectsByType<Soured>(FindObjectsSortMode.None))
                s.gameObject.SetActive(false);
            var hesper = Object.FindAnyObjectByType<Lingerer>();
            var keepsake = Object.FindAnyObjectByType<Keepsake>();
            Assert.IsNotNull(keepsake, "his lantern is out there");
            Assert.Greater(keepsake.transform.position.z, 8f, "on the islet north of the yard");
            Assert.IsFalse(hesper.heldKeepsake.activeSelf, "her hands are empty");
            hesper.Interact(apprentice); // (met)
            apprentice.OpenMenu.Close();

            apprentice.Teleport(keepsake.transform.position + Vector3.right * 0.6f);
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(keepsake, apprentice.Nearby);
            input.PressInteract();
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(apprentice.Equipment.Carries(keepsake.id), "carried");
            Assert.IsFalse(keepsake.gameObject.activeSelf);

            apprentice.Teleport(hesper.InteractPosition + Vector3.right * 0.9f);
            yield return new WaitForSeconds(0.3f);
            input.PressInteract();
            yield return null;
            var talk = apprentice.OpenMenu as Conversation;
            Assert.AreEqual("That's it. That's his.", talk.CurrentLine);
            Assert.AreEqual(Lingerer.Business.Given, hesper.State);
            Assert.IsFalse(apprentice.Equipment.Carries(keepsake.id), "given");
            Assert.IsTrue(hesper.heldKeepsake.activeSelf, "she holds it, lit");
            talk.Skip();
            Assert.IsTrue(talk.Choosing, "then the fare");
            StringAssert.StartsWith("Pay her fare (100 Obols)", talk.Rows[0].Label);
            Assert.IsTrue(talk.Rows[0].Dim, "too few Obols");
            talk.Choose(1); // Not yet
            yield return null;
            StringAssert.Contains("Take your time", (apprentice.OpenMenu as Conversation).CurrentLine);
            apprentice.OpenMenu.Close();
            yield return null;
            Assert.IsNull(apprentice.OpenMenu);
            Assert.AreEqual(Lingerer.Business.Given, hesper.State, "it can wait");

            apprentice.GainObols(130);
            input.PressInteract();
            yield return null;
            talk = apprentice.OpenMenu as Conversation;
            StringAssert.Contains("nothing left but oil", talk.CurrentLine, "asked again");
            talk.Skip();
            Assert.IsFalse(talk.Rows[0].Dim);
            talk.Choose(0);
            yield return null;
            Assert.AreEqual(30, apprentice.Obols);
            Assert.AreEqual(Lingerer.Business.Paid, hesper.State);
            talk = apprentice.OpenMenu as Conversation;
            Assert.AreEqual("Thank you, child.", talk.CurrentLine);
            talk.Close();
            Vector3 was = hesper.figure.position;
            yield return new WaitForSeconds(Lingerer.DriftOut * 0.6f);
            Assert.IsTrue(hesper.Drifting, "she thins away first");
            Assert.IsFalse(hesper.Available, "(no talking to her while she goes)");
            Assert.Less(FlatDistance(hesper.figure.position, was), 0.1f, "still where she sat");
            var block = new MaterialPropertyBlock();
            hesper.GetComponentInChildren<Renderer>().GetPropertyBlock(block);
            Assert.Greater(block.GetFloat("_Dissolve"), 0.3f, "dissolving");
            yield return new WaitForSeconds(Lingerer.DriftOut + Lingerer.DriftPause + Lingerer.DriftIn);
            Assert.IsFalse(hesper.Drifting);
            Assert.Less(FlatDistance(hesper.figure.position, hesper.waitAt.position), 0.1f, "and gathers again by the boat");

            apprentice.Teleport(hesper.InteractPosition + Vector3.left * 0.9f);
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(hesper, apprentice.Nearby);
            input.PressInteract();
            yield return null;
            StringAssert.Contains("Whenever you are", (apprentice.OpenMenu as Conversation).CurrentLine);
        }

        // ---- The boat

        Dock FarDock(Boat boat) => boat.docks.First(d => d.farShore);

        IEnumerator TakeTheOars(Boat boat)
        {
            apprentice.Teleport(boat.InteractPosition);
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(boat, apprentice.Nearby, "the boat, moored: Row");
            input.PressInteract();
            yield return new WaitForSeconds(apprentice.hopTime + 0.1f);
            Assert.AreEqual(Apprentice.State.Rowing, apprentice.CurrentState);
        }

        // Rows towards `target` (steering straight at it) until within `within`, or gives up after `timeout`.
        IEnumerator RowTo(Boat boat, Vector3 target, float within, float timeout)
        {
            for (float t = 0f; t < timeout; t += Time.deltaTime)
            {
                Vector3 to = target - boat.transform.position;
                to.y = 0f;
                if (to.magnitude < within)
                    break;
                input.Move = new Vector2(to.x, to.z).normalized;
                yield return null;
            }
            input.Move = Vector2.zero;
        }

        [UnityTest]
        public IEnumerator Boat_RowsFromTheLanding_ToTheFarShore_AndBack()
        {
            var boat = Object.FindAnyObjectByType<Boat>();
            Assert.IsNotNull(boat, "the Ferryman's boat, at the landing");
            var hud = Object.FindAnyObjectByType<Hud>();
            Assert.AreEqual(boat.home, boat.MooredAt);
            Assert.Greater(Vector3.Dot(boat.bowLantern.transform.position - boat.transform.position, boat.Bow), 1f, "the bow pole is at its bow");
            apprentice.Teleport(boat.InteractPosition);
            yield return new WaitForSeconds(0.3f);
            yield return null;
            Assert.AreEqual("Row", hud.PromptText);
            input.PressInteract();
            yield return new WaitForSeconds(apprentice.hopTime + 0.1f);
            Assert.AreEqual(Apprentice.State.Rowing, apprentice.CurrentState, "into the boat");
            Assert.IsTrue(boat.Rowing);
            Assert.Less(FlatDistance(apprentice.transform.position, boat.seat.position), 0.1f, "sat in it");
            Assert.IsTrue(apprentice.LanternOnTheBow, "the bow lantern hung on the bow");
            Assert.IsTrue(boat.bowLantern.activeSelf);
            var lantern = apprentice.GetComponent<BowLantern>().lanternLight;
            Assert.Less(Vector3.Distance(lantern.transform.position, boat.bowLanternGlass.position), 0.1f, "its light with it");

            var far = FarDock(boat);
            float lowest = 10f;
            for (float t = 0f; t < 30f && boat.DockInReach != far; t += Time.deltaTime)
            {
                Vector3 to = far.mooring.position - boat.transform.position;
                input.Move = new Vector2(to.x, to.z).normalized;
                lowest = Mathf.Min(lowest, apprentice.transform.position.y);
                yield return null;
            }
            input.Move = Vector2.zero;
            Assert.AreEqual(far, boat.DockInReach, "rowed out to the Far shore");
            Assert.Greater(lowest, apprentice.waterLevel, "sat in the boat all the way, not in the water");
            Assert.AreEqual(apprentice.maxHealth, apprentice.Health);
            Assert.Greater(boat.Strokes, 4, "stroke after stroke");
            yield return null;
            Assert.AreEqual("Step ashore", hud.PromptText);
            input.PressInteract();
            yield return new WaitForSeconds(apprentice.hopTime + 1f);
            Assert.AreEqual(Apprentice.State.Free, apprentice.CurrentState, "ashore");
            Assert.Less(FlatDistance(apprentice.transform.position, far.ashore.position), 0.3f, "on the Far shore's jetty");
            Assert.AreEqual(far, boat.MooredAt, "the boat tied up there");
            Assert.Less(FlatDistance(boat.transform.position, far.mooring.position), 0.1f);
            Assert.IsFalse(apprentice.LanternOnTheBow);
            Assert.IsFalse(boat.bowLantern.activeSelf, "the lantern back at the hip");

            // And home again.
            yield return TakeTheOars(boat);
            for (float t = 0f; t < 30f && boat.DockInReach != boat.home; t += Time.deltaTime)
            {
                Vector3 to = boat.home.mooring.position - boat.transform.position;
                input.Move = new Vector2(to.x, to.z).normalized;
                yield return null;
            }
            input.Move = Vector2.zero;
            Assert.AreEqual(boat.home, boat.DockInReach, "back at the landing");
            input.PressInteract();
            yield return new WaitForSeconds(apprentice.hopTime + 1f);
            Assert.AreEqual(Apprentice.State.Free, apprentice.CurrentState);
            Assert.Less(FlatDistance(apprentice.transform.position, boat.home.ashore.position), 0.3f);
        }

        // It glides on, turns better under way, slides along what it meets, and the lagoon's edge turns it back.
        [UnityTest]
        public IEnumerator Boat_HasWeight_MeetsTheWorld_AndStaysInTheLagoon()
        {
            var boat = Object.FindAnyObjectByType<Boat>();
            yield return TakeTheOars(boat);
            boat.PlaceAt(new Vector3(8f, 0f, -32f), Vector3.back);
            input.Move = Vector2.down;
            yield return new WaitForSeconds(0.3f);
            float early = boat.Speed;
            yield return new WaitForSeconds(2f);
            Assert.Greater(boat.Speed, early + 1f, "it gathers way slowly");
            input.Move = Vector2.zero;
            float rowing = boat.Speed;
            yield return new WaitForSeconds(0.5f);
            Assert.Greater(boat.Speed, rowing * 0.5f, "and glides on when you stop");

            // Into the landing (from the east, just south of the mooring): it stops at the planks.
            boat.PlaceAt(new Vector3(6.2f, 0f, -26.8f), Vector3.left);
            input.Move = Vector2.left;
            yield return new WaitForSeconds(3f);
            input.Move = Vector2.zero;
            Assert.Greater(boat.transform.position.x, 4.3f, "not through the landing");

            // Out past the lagoon's edge: the water turns it back.
            boat.PlaceAt(new Vector3(boat.lagoon.xMax - 2f, 0f, -30f), Vector3.right);
            input.Move = Vector2.right;
            yield return new WaitForSeconds(6f);
            input.Move = Vector2.zero;
            Assert.Less(boat.transform.position.x, boat.lagoon.xMax + 1.5f, "kept to the lagoon");

            // Nothing on the water hurts.
            apprentice.TakeHit(50f, Vector3.zero);
            Assert.AreEqual(apprentice.maxHealth, apprentice.Health);
        }

        // Waking at a Mooring post, the boat is home (nobody's left stranded).
        [UnityTest]
        public IEnumerator Boat_IsHomeAgain_WhenYouWake()
        {
            var boat = Object.FindAnyObjectByType<Boat>();
            var far = FarDock(boat);
            yield return TakeTheOars(boat);
            boat.PlaceAt(far.mooring.position, Vector3.back);
            yield return null;
            input.PressInteract();
            yield return new WaitForSeconds(apprentice.hopTime + 1f);
            Assert.AreEqual(far, boat.MooredAt);
            apprentice.TakeHit(10000f, Vector3.zero);
            yield return new WaitForSeconds(apprentice.respawnDelay + 0.5f);
            Assert.AreEqual(boat.home, boat.MooredAt, "the boat's back at the landing");
            Assert.Less(FlatDistance(boat.transform.position, boat.home.mooring.position), 0.1f);
        }

        // Her Crossing: the fare paid, take the oars and she comes aboard. Nearing the Far shore the oar rests and she
        // talks as the boat drifts in (the music low); put in, her goodbye, then she goes on into the light, leaving his
        // lantern on the jetty and her Wick for you.
        [UnityTest]
        public IEnumerator Hesper_HerCrossing_ToTheFarShore()
        {
            foreach (var s in Object.FindObjectsByType<Soured>(FindObjectsSortMode.None))
                s.gameObject.SetActive(false);
            var hesper = Object.FindAnyObjectByType<Lingerer>();
            var boat = Object.FindAnyObjectByType<Boat>();
            var far = FarDock(boat);
            hesper.Restore(Lingerer.Business.Paid);
            Assert.IsFalse(hesper.boatFigure.activeSelf, "not in the boat yet");
            yield return TakeTheOars(boat);
            yield return new WaitForSeconds(1.6f);
            Assert.IsTrue(hesper.Aboard, "she comes aboard");
            Assert.IsTrue(hesper.boatFigure.activeSelf);
            Assert.IsFalse(hesper.figure.gameObject.activeSelf, "gone from the landing");
            Assert.IsFalse(hesper.Available);

            boat.PlaceAt(far.mooring.position + Vector3.forward * (hesper.talkDistance + 3f), Vector3.back);
            yield return RowTo(boat, far.mooring.position, hesper.talkDistance - 1f, 10f);
            yield return null;
            var talk = apprentice.OpenMenu as Conversation;
            Assert.IsNotNull(talk, "she talks, out on the water");
            Assert.AreEqual(Apprentice.State.Rowing, apprentice.CurrentState);
            StringAssert.Contains("colder out here", talk.CurrentLine);
            Assert.IsTrue(boat.Coasting, "the oar rests; the boat drifts in");
            Assert.IsTrue(MusicPlayer.Hushed);
            float before = FlatDistance(boat.transform.position, far.mooring.position);
            yield return new WaitForSeconds(1.5f);
            Assert.Less(FlatDistance(boat.transform.position, far.mooring.position), before - 0.5f, "drifting in");
            talk.Skip();
            StringAssert.Contains("rest of us", talk.CurrentLine);
            talk.Close();
            yield return null;
            Assert.IsFalse(boat.Coasting, "the oars again");

            yield return RowTo(boat, far.mooring.position, 1.5f, 15f);
            Assert.AreEqual(far, boat.DockInReach);
            input.PressInteract();
            yield return new WaitForSeconds(apprentice.hopTime + 0.3f);
            talk = apprentice.OpenMenu as Conversation;
            Assert.IsNotNull(talk, "her goodbye");
            StringAssert.Contains("far enough", talk.CurrentLine);
            Assert.IsFalse(apprentice.Equipment.Owns(Charm.HespersWick));
            talk.Close();
            yield return null;
            Assert.IsTrue(hesper.Departing, "she goes on");
            Assert.AreEqual(Lingerer.Business.Crossed, hesper.State);
            yield return new WaitForSeconds(3.5f);
            Assert.IsFalse(hesper.boatFigure.activeSelf, "gone into the light");
            Assert.IsTrue(hesper.leftBehind.activeSelf, "his lantern left on the jetty");
            Assert.IsTrue(apprentice.Equipment.Owns(Charm.HespersWick), "her parting gift");
            Assert.IsFalse(MusicPlayer.Hushed);
            Assert.IsFalse(hesper.Available, "nobody left to speak to");
        }

        // Hesper's Wick: the bow lantern's light reaches further.
        [UnityTest]
        public IEnumerator HespersWick_LightsFurther()
        {
            var lantern = apprentice.GetComponent<BowLantern>();
            yield return null;
            float range = lantern.Range;
            apprentice.Equipment.Own(Charm.HespersWick);
            if (apprentice.Equipment.IsFitted(Charm.TestersKnot))
                apprentice.Equipment.Toggle(Charm.TestersKnot); // (one slot)
            apprentice.Equipment.Toggle(Charm.HespersWick);
            Assert.IsTrue(apprentice.Equipment.IsFitted(Charm.HespersWick));
            yield return null;
            Assert.Greater(lantern.Range, range * 1.4f);
        }

        // ---- The Laden archer        // ---- The Laden archer

        [UnityTest]
        public IEnumerator Archer_ShootsFromRange_AndTheArrowHurts()
        {
            var target = Isolate(archer);
            target.retreatSpeed = 0f;
            target.volleyChance = 0f;
            PlaceApprentice(target.transform.position + Flat(target.transform.forward) * 7f, target.transform.position);
            for (float t = 0; t < 5f && apprentice.Health >= apprentice.maxHealth; t += Time.deltaTime)
                yield return null;
            Assert.Greater(target.ShotsFired, 0, "it should shoot from 7 m");
            Assert.Less(apprentice.Health, apprentice.maxHealth, "standing still, the arrow should hit");
        }

        // A perfect block deflects an arrow back (no chip, no stamina), and it stuns its archer; a normal block stops
        // it at the usual cost; a dash's i-frames let it pass.
        [UnityTest]
        public IEnumerator Archer_Arrows_PerfectBlockDeflects_BlockCosts_DashPasses()
        {
            var target = Isolate(archer);
            target.retreatSpeed = 0f;
            target.volleyChance = 0f;
            Vector3 spot = target.transform.position + Flat(target.transform.forward) * 7f;
            var outcomes = new System.Collections.Generic.List<Defence>();
            void OnResolved(Arrow arrow, Defence defence) => outcomes.Add(defence);
            Arrow.Resolved += OnResolved;
            try
            {
                // 1. Perfect block: raise it just before the arrow arrives.
                PlaceApprentice(spot, target.transform.position);
                float health = apprentice.Health, stamina = apprentice.Stamina;
                yield return WaitForArrowWithin(1.3f, 5f);
                input.BlockHeld = true;
                for (float t = 0; t < 0.5f && outcomes.Count < 1; t += Time.deltaTime)
                    yield return null;
                input.BlockHeld = false;
                Assert.AreEqual(Defence.Parried, outcomes.LastOrDefault(), "a block raised as it arrives should be perfect");
                Assert.AreEqual(health, apprentice.Health, "a perfect block takes nothing");
                Assert.AreEqual(stamina, apprentice.Stamina, 0.01f, "and costs nothing");
                for (float t = 0; t < 1.5f && target.CurrentState != Soured.State.Stunned; t += Time.deltaTime)
                    yield return null;
                Assert.AreEqual(Soured.State.Stunned, target.CurrentState, "the deflected arrow should fly back and stun its archer");
                Assert.Less(target.Health, target.maxHealth, "and hurt it");
                yield return new WaitForSeconds(target.stunDuration);

                // 2. Normal block: held up well before the arrow comes.
                yield return new WaitForSeconds(0.5f);
                input.BlockHeld = true;
                yield return new WaitForSeconds(0.5f);
                health = apprentice.Health;
                stamina = apprentice.Stamina;
                for (float t = 0; t < 5f && outcomes.Count < 2; t += Time.deltaTime)
                    yield return null;
                input.BlockHeld = false;
                Assert.AreEqual(Defence.Blocked, outcomes.LastOrDefault());
                Assert.Less(apprentice.Stamina, stamina, "a normal block costs stamina");
                Assert.Less(apprentice.Health, health, "and lets a little through");

                // 3. Dash straight back along its line: the arrow is faster, so it catches up inside the i-frames.
                yield return new WaitForSeconds(0.8f);
                apprentice.RestoreFully();
                PlaceApprentice(spot, target.transform.position);
                yield return WaitForArrowWithin(1.1f, 5f);
                Vector3 away = Flat(apprentice.transform.position - target.transform.position);
                input.Move = new Vector2(away.x, away.z);
                input.PressDash();
                yield return new WaitForSeconds(0.5f);
                input.Move = Vector2.zero;
                Assert.AreEqual(Defence.Dodged, outcomes.LastOrDefault(), "the i-frames should let it pass");
                Assert.AreEqual(apprentice.maxHealth, apprentice.Health);
            }
            finally
            {
                Arrow.Resolved -= OnResolved;
                input.BlockHeld = false;
            }
        }

        // The volley: a longer draw with three arrows fanned on the bow (the tell), then three loosed in one strike.
        [UnityTest]
        public IEnumerator Archer_Volley_ShowsThreeArrows_ThenLoosesThem()
        {
            var target = Isolate(archer);
            target.retreatSpeed = 0f;
            target.volleyChance = 1f;
            PlaceApprentice(target.transform.position + Flat(target.transform.forward) * 7f, target.transform.position);
            for (float t = 0; t < 4f && target.CurrentState != Soured.State.Telegraph; t += Time.deltaTime)
                yield return null;
            Assert.AreEqual("Volley", target.CurrentAttack.name);
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(3, target.NockedCount, "three arrows on the bow: a volley is coming");
            for (float t = 0; t < 2f && target.CurrentState != Soured.State.Recover; t += Time.deltaTime)
                yield return null;
            Assert.AreEqual(3, target.ShotsFired, "three arrows in quick succession");
            Assert.Less(apprentice.Health, apprentice.maxHealth, "standing still, the volley should find you");
        }

        [UnityTest]
        public IEnumerator Archer_BacksAway_WhenCrowded_ButNotIntoTheWater()
        {
            var target = Isolate(archer);
            PlaceApprentice(target.transform.position + Flat(target.transform.forward) * 3f, target.transform.position);
            float start = Vector3.Distance(target.transform.position, apprentice.transform.position);
            float furthest = start;
            for (float t = 0; t < 5f; t += Time.deltaTime)
            {
                Assert.IsFalse(target.Drowned, "backing away, it walked into the water");
                furthest = Mathf.Max(furthest, Vector3.Distance(target.transform.position, apprentice.transform.position));
                yield return null;
            }
            Assert.Greater(furthest, start + 0.8f, "crowded, it should back away to shooting distance");
            Assert.Greater(target.transform.position.y, 0.2f);
        }

        IEnumerator WaitForArrowWithin(float distance, float timeout)
        {
            for (float t = 0; t < timeout; t += Time.deltaTime)
            {
                foreach (var arrow in Object.FindObjectsByType<Arrow>(FindObjectsSortMode.None))
                {
                    Vector3 to = apprentice.transform.position - arrow.transform.position;
                    to.y = 0f;
                    if (!arrow.Stuck && to.magnitude < distance)
                        yield break;
                }
                yield return null;
            }
            Assert.Fail("no arrow came");
        }

        // The horizontal direction of v (normalised).
        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.normalized;
        }

        static float FlatDistance(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;

        [Test]
        public void Hud_UsesTheFellFonts()
        {
            var hud = Object.FindAnyObjectByType<Hud>();
            StringAssert.StartsWith("IMFellEnglishSC", hud.titleFont != null ? hud.titleFont.name : "missing");
            StringAssert.StartsWith("IMFellEnglish-", hud.textFont != null ? hud.textFont.name : "missing");
        }

        // The fall: time slows, the camera closes in, the bow lantern gutters out (its light drifting away as motes),
        // and the Shade rises out of the body once it lies still. Waking, all of it is undone.
        [UnityTest]
        public IEnumerator Dying_TheLanternGoesOut_AndTheShadeRisesFromTheBody()
        {
            var lantern = apprentice.GetComponent<BowLantern>();
            apprentice.GainObols(50);
            apprentice.TakeHit(1000f, Vector3.zero);
            yield return null;
            Assert.IsTrue(CameraRig.Instance.Mourning, "the camera closes in on the fallen");
            yield return new WaitForSeconds(0.1f);
            Assert.Less(Time.timeScale, 0.9f, "time slows as they fall");
            yield return new WaitForSeconds(apprentice.snuffDelay + apprentice.snuffDuration * 0.5f);
            Assert.Greater(Effects.Instance.LiveWisps, 0, "the lantern's light drifting away");
            Assert.AreEqual(0f, Shade.Current.GetComponent<MeshRenderer>().GetPropertyBlock_Alpha(), 0.01f, "the Shade hasn't risen yet");
            yield return new WaitForSeconds(apprentice.snuffDuration * 0.5f + 0.1f);
            Assert.Less(lantern.Lit, 0.05f, "the bow lantern has gone out");
            yield return new WaitForSeconds(apprentice.deathDuration - apprentice.snuffDelay - apprentice.snuffDuration + 1.2f);
            Assert.Greater(Shade.Current.GetComponent<MeshRenderer>().GetPropertyBlock_Alpha(), 0.1f, "the Shade has risen from the body");
            yield return new WaitForSeconds(apprentice.respawnDelay);
            Assert.IsTrue(apprentice.IsAlive);
            Assert.IsFalse(CameraRig.Instance.Mourning);
            Assert.AreEqual(1f, lantern.Lit, "lit again at the Mooring post");
        }

        [UnityTest]
        public IEnumerator Dying_ShowsTheDeathScreen_WakesBehindBlack_ThenFadesBackIn()
        {
            var hud = Object.FindAnyObjectByType<Hud>();
            apprentice.TakeHit(1000f, Vector3.zero);
            Assert.AreEqual("death_toll", Sfx.Instance.LastPlayed, "falling should toll");
            yield return new WaitForSeconds(2.8f); // (the fall and the lantern going out come first)
            yield return null;
            Assert.Greater(hud.Death.MessageAlpha, 0.9f, "the death phrase should be up");
            Assert.AreEqual(hud.deathPhrase, hud.Death.Message);

            float blackBeforeWaking = 0f;
            for (float t = 0; t < 5f && !apprentice.IsAlive; t += Time.deltaTime)
            {
                blackBeforeWaking = hud.Death.BlackoutAlpha;
                yield return null;
            }
            Assert.IsTrue(apprentice.IsAlive, "should have woken");
            Assert.Greater(blackBeforeWaking, 0.95f, "the jump to the Mooring post should happen behind a black screen");
            yield return new WaitForSeconds(2f);
            yield return null;
            Assert.Less(hud.Death.BlackoutAlpha, 0.01f, "the world should have faded back in");
            Assert.AreEqual(0f, hud.Death.MessageAlpha);
            Assert.Greater(hud.Death.HudAlpha, 0.99f, "the HUD should be back");
        }

        [UnityTest]
        public IEnumerator FirstRest_ShowsLanternLit_OnlyTheFirstTime()
        {
            var hud = Object.FindAnyObjectByType<Hud>();
            apprentice.Teleport(post.RespawnPoint);
            yield return new WaitForSeconds(0.5f);
            Assert.Greater(hud.PromptAlpha, 0.9f, "the Rest prompt should show by the post");
            input.PressInteract();
            yield return null;
            yield return null;
            Assert.IsTrue(hud.LanternBanner.Showing, "lighting a Mooring post should be announced");
            Assert.AreEqual("Lantern Lit", hud.LanternBanner.Text.text);
            Assert.AreEqual("lantern_lit", Sfx.Instance.LastPlayed);
            yield return new WaitForSeconds(4f);
            Assert.IsFalse(hud.LanternBanner.Showing);
            input.PressInteract();
            yield return null;
            yield return null;
            Assert.IsFalse(hud.LanternBanner.Showing, "resting at a lantern already lit isn't news");
        }

        [UnityTest]
        public IEnumerator WalkingTowardsTheQuay_ShowsTheAreaTitle()
        {
            var hud = Object.FindAnyObjectByType<Hud>();
            Assert.IsFalse(hud.AreaTitle.Showing, "no title on the landing");
            for (float t = 0; t < 5f && apprentice.transform.position.z < -9.5f; t += Time.deltaTime)
            {
                input.Move = Vector2.up;
                yield return null;
            }
            input.Move = Vector2.zero;
            yield return null;
            Assert.IsTrue(hud.AreaTitle.Showing, "the area's name should come up on the way in");
            Assert.AreEqual("The Drowned City", hud.AreaTitle.Text.text);
        }

        // Keep only one Laden active so the other doesn't join in. `frozen` stops it moving, turning and
        // attacking, for tests about how it takes hits rather than how it fights.
        // Keep only this Soured active (the others would join in).
        T Isolate<T>(T keep, bool frozen = false) where T : Soured
        {
            foreach (var other in Object.FindObjectsByType<Soured>(FindObjectsSortMode.None).Where(s => s != keep))
                other.gameObject.SetActive(false);
            if (frozen && keep is Laden l)
            {
                l.guardSpeed = 0f;
                l.sweep.reach = 0f;
            }
            if (frozen)
            {
                keep.chaseSpeed = 0f;
                keep.turnSpeed = 0f;
                keep.attackRange = 0f;
            }
            return keep;
        }

        void PlaceApprentice(Vector3 position, Vector3 lookAt)
        {
            apprentice.Teleport(new Vector3(position.x, 0.55f, position.z));
            Vector3 to = lookAt - position;
            to.y = 0f;
            apprentice.transform.rotation = Quaternion.LookRotation(to);
        }
    }

    static class ShadeTestExtensions
    {
        // The Shade's current tint alpha (it's drawn through a property block).
        public static float GetPropertyBlock_Alpha(this MeshRenderer renderer)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            return block.GetColor("_Color").a;
        }
    }
}
