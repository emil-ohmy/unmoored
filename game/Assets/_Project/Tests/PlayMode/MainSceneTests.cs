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
        public IEnumerator Apprentice_StandsOnTheQuay()
        {
            yield return new WaitForSeconds(0.5f);
            Assert.That(apprentice.transform.position.y, Is.InRange(0.4f, 0.65f), "not standing on the quay top");
            Assert.AreEqual(Apprentice.State.Free, apprentice.CurrentState);
        }

        [UnityTest]
        public IEnumerator Apprentice_MovesWithInput()
        {
            Vector3 start = apprentice.transform.position;
            input.Move = Vector2.left;
            yield return new WaitForSeconds(0.5f);
            input.Move = Vector2.zero;
            Assert.Greater(Vector3.Distance(start, apprentice.transform.position), 2f, "barely moved in half a second");
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

            // In front of it, facing it: the swing bounces off the heap.
            PlaceApprentice(target.transform.position + target.transform.forward * 1.6f, target.transform.position);
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
