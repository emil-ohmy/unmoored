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
            Assert.AreEqual(2, laden.Length, "expected two Laden");
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

            var other = laden[1];
            other.gameObject.SetActive(true);
            yield return null;
            PlaceApprentice(other.transform.position + other.transform.forward * 5f, other.transform.position);
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(Soured.State.Chase, other.CurrentState, "5 m in front of it, it should see the Apprentice");
        }

        [UnityTest]
        public IEnumerator Dash_IsInvulnerableDuringItsIFrames_ButNotAfter()
        {
            input.Move = Vector2.left;
            input.PressDash();
            yield return new WaitForSeconds((apprentice.iFrameStart + apprentice.iFrameEnd) / 2f);
            Assert.AreEqual(Apprentice.State.Dashing, apprentice.CurrentState);
            Assert.IsFalse(apprentice.TakeHit(20f, Vector3.zero), "a hit mid-dash should pass through");
            Assert.AreEqual(apprentice.maxHealth, apprentice.Health);
            Assert.IsTrue(Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Any(r => r.name == "Dash Ghost" && r.gameObject.activeInHierarchy),
                "the i-frames should leave ghost images");

            input.Move = Vector2.zero;
            yield return new WaitForSeconds(apprentice.dashDuration + apprentice.dashRecovery);
            Assert.IsTrue(apprentice.TakeHit(20f, Vector3.zero), "after the dash a hit should land");
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
            var behind = laden[1].transform.position - laden[1].transform.forward * 2f;
            laden[1].TakeHit(new Hit { Damage = 1000f, From = behind });
            Assert.IsFalse(laden[1].IsAlive, "setup: Laden should be scattered");
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
            Assert.IsTrue(laden[1].IsAlive, "resting should re-form scattered Soured");

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
