using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MyGame.Tests
{
    // The Boss arena (BossArena scene): the Antediluvian Anchoress's phase one, played headless with scripted input,
    // time stepped at a fixed 1/60 s like MainSceneTests.
    public class BossArenaTests
    {
        Apprentice apprentice;
        ScriptedInput input;
        Anchoress boss;

        [UnitySetUp]
        public IEnumerator LoadArena()
        {
            Application.targetFrameRate = -1;
            Time.captureDeltaTime = 1f / 60f;
            yield return SceneManager.LoadSceneAsync("BossArena");
            yield return null;
            apprentice = Object.FindAnyObjectByType<Apprentice>();
            boss = Object.FindAnyObjectByType<Anchoress>();
            apprentice.Input = input = new ScriptedInput();
            yield return new WaitForSeconds(0.5f);
        }

        [TearDown]
        public void ResetTime()
        {
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f;
        }

        [Test]
        public void Scene_HasTheBossAndAWayIn()
        {
            Assert.IsNotNull(boss, "no Anchoress");
            Assert.IsNotNull(Object.FindAnyObjectByType<MooringPost>(), "a Mooring post to wake at");
            Assert.Greater(Flat(boss.transform.position - apprentice.transform.position).magnitude, 12f, "you start a walk away from her");
            Assert.IsTrue(Physics.Raycast(boss.transform.position + Vector3.up, Vector3.down, 2f), "she kneels on the chapel floor");
            Assert.AreEqual(Anchoress.AnchorMode.Held, boss.Anchor);
        }

        // She keeps her vigil, kneeling at the altar, until the Apprentice is well onto the chapel floor.
        [UnityTest]
        public IEnumerator KeepsVigil_UntilYouComeNear()
        {
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(Soured.State.Idle, boss.CurrentState);
            Assert.IsFalse(boss.Awakened);
            input.Move = Vector2.up;
            for (float t = 0; t < 12f && !boss.Awakened; t += Time.deltaTime)
                yield return null;
            input.Move = Vector2.zero;
            Assert.IsTrue(boss.Awakened, "she should wake as you walk up the chapel");
            Assert.Greater(apprentice.transform.position.z, -7.8f, "not before you're on the chapel floor");
            Assert.Greater(Flat(boss.transform.position - apprentice.transform.position).magnitude, 5f, "she senses you from well off");
        }

        // The Anchor Drop: stepping out of the blow's line isn't enough; the ring of water it throws out reaches you.
        [UnityTest]
        public IEnumerator AnchorDrop_RingOfWaterReachesPastTheBlow()
        {
            FaceTheEntrance();
            OnlyTheDrop();
            PlaceApprentice(boss.transform.position + boss.transform.forward * 2.3f);
            yield return WaitFor(() => boss.CurrentState == Soured.State.Telegraph && boss.CurrentAttack == boss.primary, 5f, "the Drop's wind-up");
            yield return WaitFor(() => boss.StateTime >= boss.primary.telegraph * boss.primary.trackUntil + 0.05f, 2f, "her aim set");
            // Out of the blow's arc, but inside the ring's reach.
            PlaceApprentice(boss.transform.position + boss.transform.forward * 2.6f + boss.transform.right * 3f);
            yield return WaitFor(() => boss.LastShockwave != null, 2f, "the ring of water");
            var wave = boss.LastShockwave;
            yield return WaitFor(() => wave == null || wave.Struck, 2f, "the ring reaching you");
            Assert.AreEqual(apprentice.maxHealth - boss.ringDamage, apprentice.Health, 0.01f, "hit by the ring (not the blow)");
        }

        [UnityTest]
        public IEnumerator AnchorDrop_RingCanBeOutrun()
        {
            FaceTheEntrance();
            OnlyTheDrop();
            PlaceApprentice(boss.transform.position + boss.transform.forward * 2.3f);
            yield return WaitFor(() => boss.CurrentState == Soured.State.Telegraph && boss.CurrentAttack == boss.primary, 5f, "the Drop's wind-up");
            yield return WaitFor(() => boss.StateTime >= boss.primary.telegraph * boss.primary.trackUntil + 0.05f, 2f, "her aim set");
            PlaceApprentice(boss.transform.position + boss.transform.forward * 2.6f + boss.transform.right * (boss.ringRadius + 0.8f));
            yield return WaitFor(() => boss.LastShockwave != null, 2f, "the ring of water");
            var wave = boss.LastShockwave;
            yield return WaitFor(() => wave == null, 2f, "the ring fading out");
            Assert.AreEqual(apprentice.maxHealth, apprentice.Health, "beyond its reach, the ring can't touch you");
        }

        // From range she throws the anchor at you; it lies a moment, then she hauls it home through anyone between.
        [UnityTest]
        public IEnumerator CastAndHaul_ThrowsAtRange_ThenHaulsThroughYou()
        {
            FaceTheEntrance();
            boss.firstCastDelay = 0.1f;
            PlaceApprentice(boss.transform.position + boss.transform.forward * 5.8f);
            yield return WaitFor(() => boss.Anchor == Anchoress.AnchorMode.Flying, 6f, "the cast");
            Assert.AreEqual(boss.cast, boss.CurrentAttack);
            Vector3 aimedAt = apprentice.transform.position;
            StepTowardsHer(2.5f); // between her and it
            yield return WaitFor(() => boss.Anchor == Anchoress.AnchorMode.Lying, 2f, "the anchor landing");
            Assert.Less(Flat(boss.AnchorMiddle - aimedAt).magnitude, 1.5f, "it lands where you were");
            Assert.AreEqual(apprentice.maxHealth, apprentice.Health, $"stepping in dodged the landing ({apprentice.LastEvent} at {apprentice.LastEventTime:0.00}, now {Time.time:0.00})");
            yield return WaitFor(() => boss.Anchor == Anchoress.AnchorMode.Hauled, 2f, "the haul");
            yield return WaitFor(() => apprentice.Health < apprentice.maxHealth || boss.Anchor != Anchoress.AnchorMode.Hauled, 2f, "the anchor dragged through you");
            Assert.AreEqual(apprentice.maxHealth - boss.haulDamage, apprentice.Health, 0.01f, "hit by the anchor on its way home");
        }

        // The risk and the reward: perfect-block the haul and the chain snaps taut against the oar, yanking her off
        // balance (stunned, the anchor left lying). Back on her feet, she reels it in before anything else.
        [UnityTest]
        public IEnumerator CastAndHaul_PerfectBlockedHaul_YanksHerOffBalance()
        {
            FaceTheEntrance();
            boss.firstCastDelay = 0.1f;
            PlaceApprentice(boss.transform.position + boss.transform.forward * 5.8f);
            yield return WaitFor(() => boss.Anchor == Anchoress.AnchorMode.Flying, 6f, "the cast");
            StepTowardsHer(2.5f);
            yield return WaitFor(() => boss.Anchor == Anchoress.AnchorMode.Hauled, 3f, "the haul");
            yield return WaitFor(() => Flat(boss.AnchorMiddle - apprentice.transform.position).magnitude < boss.haulHitRadius + 0.8f, 2f, "the anchor close");
            input.BlockHeld = true;
            yield return WaitFor(() => boss.CurrentState == Soured.State.Stunned || boss.Anchor != Anchoress.AnchorMode.Hauled, 1f, "the perfect block");
            Assert.AreEqual(Soured.State.Stunned, boss.CurrentState, $"a perfect-blocked haul stuns her ({apprentice.LastEvent}, {boss.Anchor}, health {apprentice.Health}, {Flat(boss.AnchorMiddle - apprentice.transform.position).magnitude:0.00} m)");
            Assert.AreEqual(Anchoress.AnchorMode.Lying, boss.Anchor, "the anchor stays where it stopped");
            Assert.AreEqual(apprentice.maxHealth, apprentice.Health);
            input.BlockHeld = false;
            yield return WaitFor(() => boss.Anchor == Anchoress.AnchorMode.Reeling, boss.stunDuration + 1f, "reeling it in");
            yield return WaitFor(() => boss.Anchor == Anchoress.AnchorMode.Held, 2f, "the anchor home");
        }

        // The Chain Sweep reaches whoever stands at her side, well past a Laden's reach.
        [UnityTest]
        public IEnumerator ChainSweep_ReachesYourSide()
        {
            FaceTheEntrance();
            boss.castCooldown = boss.firstCastDelay = 1000f;
            PlaceApprentice(boss.transform.position + boss.transform.right * 3.4f);
            yield return WaitFor(() => boss.CurrentState == Soured.State.Telegraph, 3f, "a wind-up");
            Assert.AreEqual(boss.sweep, boss.CurrentAttack, "at her side: the chain comes round");
            yield return WaitFor(() => apprentice.Health < apprentice.maxHealth || boss.CurrentState == Soured.State.Recover, 3f, "the sweep");
            Assert.AreEqual(apprentice.maxHealth - boss.sweep.damage, apprentice.Health, 0.01f, "hit by the anchor on its chain");
        }

        // Her back is open: she's slow to turn, doesn't attack what's behind her, and has no guard there.
        [UnityTest]
        public IEnumerator Guard_FrontalHitsBounceOff_HerBackIsOpen()
        {
            FaceTheEntrance();
            boss.attackRange = 0f; // just guarding, for now
            boss.castCooldown = boss.firstCastDelay = 1000f;
            boss.midRangeSweepChance = 0f;
            boss.flankAngle = 180f;
            PlaceApprentice(boss.transform.position + boss.transform.forward * 2.5f);
            yield return WaitFor(() => boss.Guarding, 3f, "her guard");
            float before = boss.Health;
            input.PressLight();
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(before, boss.Health, "the anchor takes the blow");

            boss.turnSpeed = 0f; // (so the test can stay behind her)
            PlaceApprentice(boss.transform.position - boss.transform.forward * 2.2f);
            yield return new WaitForSeconds(0.3f);
            Assert.AreNotEqual(Soured.State.Telegraph, boss.CurrentState, "she doesn't attack behind her");
            input.PressLight();
            yield return new WaitForSeconds(0.6f);
            Assert.Less(boss.Health, before, "her back is open");
        }

        // A Boss stays beaten: resting (or dying) doesn't bring her back.
        [UnityTest]
        public IEnumerator Defeated_SheStaysDefeated()
        {
            FaceTheEntrance();
            typeof(Soured).GetProperty("Health").SetValue(boss, 1f);
            boss.turnSpeed = 0f;
            PlaceApprentice(boss.transform.position - boss.transform.forward * 2.2f);
            yield return null;
            input.PressLight();
            yield return WaitFor(() => !boss.IsAlive, 2f, "her defeat");
            yield return new WaitForSeconds(2.5f);
            WorldState.Instance.ReformAllSoured();
            yield return null;
            Assert.IsFalse(boss.IsAlive, "a Boss doesn't re-form");
            Assert.AreEqual(1, boss.TimesDefeated);
        }

        // Turned from the altar to face the way in, so there's chapel floor in front of her to stage the fight on.
        void FaceTheEntrance() => boss.transform.Rotate(0f, 180f, 0f, Space.World);

        void OnlyTheDrop()
        {
            boss.sweepChanceInFront = 0f;
            boss.midRangeSweepChance = 0f;
            boss.castCooldown = boss.firstCastDelay = 1000f;
            boss.flankAngle = 180f;
        }

        void StepTowardsHer(float metres)
        {
            Vector3 to = Flat(boss.transform.position - apprentice.transform.position);
            PlaceApprentice(apprentice.transform.position + to.normalized * metres);
        }

        void PlaceApprentice(Vector3 position)
        {
            apprentice.Teleport(new Vector3(position.x, 0.55f, position.z));
            Vector3 to = Flat(boss.transform.position - position);
            if (to.sqrMagnitude > 0.01f)
                apprentice.transform.rotation = Quaternion.LookRotation(to);
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

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
