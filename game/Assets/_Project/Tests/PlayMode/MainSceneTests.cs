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
    public class MainSceneTests
    {
        Apprentice apprentice;
        ScriptedInput input;
        Laden[] laden;
        MooringPost post;

        [UnitySetUp]
        public IEnumerator LoadMain()
        {
            Application.targetFrameRate = 60;
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            apprentice = Object.FindAnyObjectByType<Apprentice>();
            laden = Object.FindObjectsByType<Laden>(FindObjectsSortMode.None);
            post = Object.FindAnyObjectByType<MooringPost>();
            apprentice.Input = input = new ScriptedInput();
            yield return new WaitForSeconds(0.5f); // settle onto the ground
        }

        [Test]
        public void Scene_HasTheSliceCast()
        {
            Assert.IsNotNull(apprentice, "no Apprentice");
            Assert.AreEqual(1, laden.Length, "one Laden while the combat is being tuned");
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
            Vector2 expected = Camera.main.WorldToScreenPoint(target.transform.position + Vector3.up * target.barHeight);
            Vector2 actual = RectTransformUtility.WorldToScreenPoint(null, hud.LockBarRect.position); // overlay: screen pixels
            Assert.Less(Vector2.Distance(expected, actual), 4f, $"the bar should float above its target (expected {expected}, got {actual})");

            input.PressLock();
            yield return null;
            yield return null; // the HUD updates in LateUpdate, after this coroutine resumes: give it that frame
            Assert.IsNull(apprentice.LockTarget, "pressing again should release");
            Assert.IsFalse(Object.FindAnyObjectByType<Hud>().LockBarVisible);
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
            for (float t = 0; t < 0.6f; t += Time.unscaledDeltaTime) // includes the hit-stop
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
            apprentice.Teleport(new Vector3(-14f, 1f, -9f), onSafeGround: false); // drop into open water
            yield return new WaitForSeconds(1f);
            Assert.Greater(apprentice.transform.position.y, 0.3f, "should be back on solid ground");
            Assert.Less(apprentice.Health, apprentice.maxHealth, "washing ashore should hurt");
            Assert.IsTrue(apprentice.IsAlive);
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

        [Test]
        public void Hud_UsesTheFellFonts()
        {
            var hud = Object.FindAnyObjectByType<Hud>();
            StringAssert.StartsWith("IMFellEnglishSC", hud.titleFont != null ? hud.titleFont.name : "missing");
            StringAssert.StartsWith("IMFellEnglish-", hud.textFont != null ? hud.textFont.name : "missing");
        }

        [UnityTest]
        public IEnumerator Dying_ShowsTheDeathScreen_WakesBehindBlack_ThenFadesBackIn()
        {
            var hud = Object.FindAnyObjectByType<Hud>();
            apprentice.TakeHit(1000f, Vector3.zero);
            Assert.AreEqual("death_toll", Sfx.Instance.LastPlayed, "falling should toll");
            yield return new WaitForSeconds(2f);
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
            Assert.AreEqual("The Drowned Village", hud.AreaTitle.Text.text);
        }

        // Keep only one Laden active so the other doesn't join in. `frozen` stops it moving, turning and
        // attacking, for tests about how it takes hits rather than how it fights.
        Laden Isolate(Laden keep, bool frozen = false)
        {
            foreach (var other in laden.Where(l => l != keep))
                other.gameObject.SetActive(false);
            if (frozen)
            {
                keep.chaseSpeed = 0f;
                keep.guardSpeed = 0f;
                keep.sweep.reach = 0f;
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
}
