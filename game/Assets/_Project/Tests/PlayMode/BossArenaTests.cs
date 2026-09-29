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

        // Pressed up against her, you're inside the swing's reach: she shoulders you out past it instead.
        [UnityTest]
        public IEnumerator ChainSweep_HugHer_AndSheShouldersYouAway()
        {
            FaceTheEntrance();
            boss.castCooldown = boss.firstCastDelay = 1000f;
            PlaceApprentice(boss.transform.position + boss.transform.right * 1.3f);
            yield return WaitFor(() => boss.CurrentState == Soured.State.Telegraph, 3f, "a wind-up");
            Assert.AreEqual(boss.sweep, boss.CurrentAttack);
            yield return WaitFor(() => apprentice.Health < apprentice.maxHealth || boss.CurrentState == Soured.State.Recover, 3f, "the shove");
            Assert.AreEqual(apprentice.maxHealth - boss.shoveDamage, apprentice.Health, 0.01f, "shouldered, not swept");
            yield return new WaitForSeconds(0.6f);
            Assert.Greater(Flat(apprentice.transform.position - boss.transform.position).magnitude, 3f, "pushed out past the swing");
            Assert.AreEqual(apprentice.maxHealth - boss.shoveDamage, apprentice.Health, 0.01f, "and the anchor didn't catch you too");
        }

        // Stay close too long (even at her back) and she kneels to pray: the water rises round her, dragging you in and
        // hurting a little every tick, without ever staggering you.
        [UnityTest]
        public IEnumerator Undertow_TooCloseTooLong_TheWaterRises()
        {
            FaceTheEntrance();
            boss.turnSpeed = boss.chaseSpeed = boss.guardSpeed = 0f; // (so the test can stay at her back)
            PlaceApprentice(boss.transform.position - boss.transform.forward * 1.8f);
            yield return WaitFor(() => boss.CurrentState == Soured.State.Telegraph, boss.undertowAfter + 1.5f, "a wind-up");
            Assert.AreEqual(boss.undertow, boss.CurrentAttack, $"too close, too long (got {boss.CurrentAttack.name} at {boss.StateTime}, close {boss.CloseTime:0.00}, angle {Vector3.Angle(boss.transform.forward, apprentice.transform.position - boss.transform.position):0})");
            yield return WaitFor(() => boss.Pool != null, boss.undertow.telegraph + 0.5f, "the water rising");
            yield return new WaitForSeconds(boss.poolSpread + 0.1f);
            float before = Flat(apprentice.transform.position - boss.transform.position).magnitude;
            yield return new WaitForSeconds(1.2f);
            Assert.Greater(boss.Pool.Ticks, 1, "it hurts, tick by tick");
            Assert.AreEqual(apprentice.maxHealth - boss.poolDamage * boss.Pool.Ticks, apprentice.Health, 0.01f);
            Assert.AreNotEqual(Apprentice.State.Staggered, apprentice.CurrentState, "without staggering you");
            Assert.Less(Flat(apprentice.transform.position - boss.transform.position).magnitude, before, "dragged towards her");
            PlaceApprentice(boss.transform.position - boss.transform.forward * (boss.poolRadius + 1f));
            float outside = apprentice.Health;
            yield return new WaitForSeconds(0.8f);
            Assert.AreEqual(outside, apprentice.Health, "out of it, it can't reach you");
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
            boss.phaseTwoAt = 0f; // (straight to the end)
            SetHealth(1f);
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

        // For play-testing, the Tester's Knot is owned in the arena: fitted, every blow lands four times as hard.
        [UnityTest]
        public IEnumerator TestersKnot_FourTimesTheDamage()
        {
            FaceTheEntrance();
            Assert.IsTrue(apprentice.Equipment.Owns(Charm.TestersKnot), "owned here from the start");
            apprentice.Equipment.Toggle(Charm.TestersKnot);
            boss.turnSpeed = 0f;
            PlaceApprentice(boss.transform.position - boss.transform.forward * 2.2f);
            yield return null;
            input.PressLight();
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(boss.maxHealth - apprentice.lightDamage * 4f, boss.Health, 0.01f);
        }

        // ---- Phase two

        // At half health she rises: nothing gets through meanwhile, a ring of water throws you back, and the floor
        // starts to break. Resetting the fight (waking at the Mooring post) mends the floor and her.
        [UnityTest]
        public IEnumerator PhaseTwo_AtHalfHealth_SheRises_AndTheFloorBreaks()
        {
            FaceTheEntrance();
            var floor = Object.FindAnyObjectByType<ChapelFloor>();
            Assert.Greater(floor.Sections, 10, "the floor is laid in sections that can break");
            SetHealth(boss.maxHealth * boss.phaseTwoAt);
            PlaceApprentice(boss.transform.position + boss.transform.forward * 4f);
            yield return WaitFor(() => boss.CurrentState == Soured.State.Busy, 1f, "her rising");
            Assert.IsTrue(boss.PhaseTwo);
            float health = boss.Health;
            Assert.AreEqual(HitResult.Blocked, boss.TakeHit(new Hit { Damage = 50f, From = apprentice.transform.position }));
            Assert.AreEqual(health, boss.Health, "nothing gets through while she rises");
            yield return WaitFor(() => boss.CurrentState != Soured.State.Busy, boss.riseDuration + 0.5f, "her risen");
            Assert.AreEqual(apprentice.maxHealth - boss.riseRingDamage, apprentice.Health, 0.01f, "thrown back by the ring of water");
            Assert.AreEqual(boss.riseBreaks, floor.Broken, "the floor starts to break");
            WorldState.Instance.ReformAllSoured();
            yield return null;
            Assert.AreEqual(0, floor.Broken, "mended when the fight resets");
            Assert.IsFalse(boss.PhaseTwo);
        }

        // Her Drops break the floor where they land.
        [UnityTest]
        public IEnumerator PhaseTwo_DropBreaksTheFloorWhereItLands()
        {
            boss.riseBreaks = 0;
            yield return IntoPhaseTwo();
            OnlyTheDrop();
            boss.yankMin = 100f;
            var floor = Object.FindAnyObjectByType<ChapelFloor>();
            PlaceApprentice(boss.transform.position + boss.transform.forward * 2.3f);
            var before = boss.LastShockwave;
            yield return WaitFor(() => boss.LastShockwave != before && boss.LastShockwave != null, 4f, "the Drop landing");
            Vector3 landed = boss.LastShockwave.transform.position;
            Assert.AreEqual(1, floor.Broken, $"landed at {landed}, she's at {boss.transform.position}");
            yield return new WaitForSeconds(floor.crumbleTime + 0.2f);
            Assert.IsFalse(FloorUnder(landed), "the floor gave way where it landed");
            Assert.Greater(floor.FallingChunks, 0, "chunks of it breaking off");
        }

        // The Hook Yank: caught, you're dragged in to land in front of her, and a quick Drop follows.
        [UnityTest]
        public IEnumerator PhaseTwo_HookYank_DragsYouIn_AndADropFollows()
        {
            boss.riseBreaks = 0;
            yield return IntoPhaseTwo();
            boss.castMin = 100f;
            boss.midRangeSweepChance = 0f;
            yield return new WaitForSeconds(1.1f); // her first yank is ready a moment after she rises
            PlaceApprentice(boss.transform.position + boss.transform.forward * 6f);
            yield return WaitFor(() => boss.CurrentState == Soured.State.Telegraph, 3f, "a wind-up");
            Assert.AreEqual(boss.yank, boss.CurrentAttack, "at range, in phase two: the hook");
            yield return WaitFor(() => boss.Hook == Anchoress.HookMode.Caught, 3f, "the hook catching you");
            yield return new WaitForSeconds(0.6f);
            Assert.LessOrEqual(Flat(boss.transform.position - apprentice.transform.position).magnitude, boss.yankLandDistance + 0.8f, "dragged in front of her");
            yield return WaitFor(() => boss.CurrentState == Soured.State.Telegraph, 2f, "the follow-up");
            Assert.AreEqual(boss.followUpTelegraph, boss.CurrentAttack.telegraph, 0.001f, "a quick Drop follows");
        }

        // Knocked (or fallen) into the water, a Boss doesn't drown: she hauls herself out onto her middle, hurt and open.
        [UnityTest]
        public IEnumerator SheHaulsHerselfOutOfTheWater()
        {
            FaceTheEntrance();
            PlaceApprentice(boss.transform.position + boss.transform.forward * 4f);
            yield return WaitFor(() => boss.Awakened, 1f, "her waking");
            var controller = boss.GetComponent<CharacterController>();
            controller.enabled = false;
            boss.transform.position = new Vector3(7f, 0.5f, -9f); // over the water, off the chapel's near corner
            controller.enabled = true;
            yield return WaitFor(() => boss.Surfaced > 0, 3f, "her falling in");
            yield return null;
            Assert.IsTrue(boss.IsAlive, "a Boss doesn't drown");
            Assert.Less(boss.Health, boss.maxHealth, "it hurt");
            Assert.AreEqual(Soured.State.Staggered, boss.CurrentState, "open while she recovers");
            Assert.IsTrue(FloorUnder(boss.transform.position), "back on the chapel floor");
        }

        IEnumerator IntoPhaseTwo()
        {
            FaceTheEntrance();
            SetHealth(boss.maxHealth * boss.phaseTwoAt);
            PlaceApprentice(boss.transform.position + boss.transform.forward * 5f);
            yield return WaitFor(() => boss.CurrentState == Soured.State.Busy, 1f, "her rising");
            yield return WaitFor(() => boss.CurrentState != Soured.State.Busy, boss.riseDuration + 0.5f, "her risen");
            apprentice.RestoreFully();
        }

        void SetHealth(float health) => typeof(Soured).GetProperty("Health").SetValue(boss, health);

        // Solid floor under this point (characters don't count).
        static bool FloorUnder(Vector3 point)
        {
            foreach (var hit in Physics.RaycastAll(new Vector3(point.x, 3f, point.z), Vector3.down, 3.2f, ~0, QueryTriggerInteraction.Ignore))
                if (!(hit.collider is CharacterController))
                    return true;
            return false;
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
