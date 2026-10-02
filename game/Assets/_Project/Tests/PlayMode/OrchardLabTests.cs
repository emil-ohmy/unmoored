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
    static class TestVectors
    {
        public static Vector3 Flat(this Vector3 v) => new Vector3(v.x, 0f, v.z);
    }

    // The Orchard of Last Autumn's lab (Scenes/Lab_Orchard.unity): its terraces walk up (steps) and drop down, the press
    // house's loft is reached by its stair (and the roof fades inside), the air has its leaves and mist, the night its
    // harvest moon.
    public class OrchardLabTests
    {
        Apprentice apprentice;
        ScriptedInput input;

        [UnitySetUp]
        public IEnumerator LoadOrchard()
        {
            Application.targetFrameRate = -1;
            Time.captureDeltaTime = 1f / 60f;
            yield return SceneManager.LoadSceneAsync("Lab_Orchard");
            yield return null;
            apprentice = UnityEngine.Object.FindAnyObjectByType<Apprentice>();
            apprentice.Input = input = new ScriptedInput();
            // The Gleaners keep out of every test but their own (their fire would cut into the rest).
            if (!TestContext.CurrentContext.Test.Name.Contains("Gleaner"))
                foreach (var g in UnityEngine.Object.FindObjectsByType<Gleaner>(FindObjectsSortMode.None))
                    g.passive = true;
            yield return new WaitForSeconds(0.3f);
        }

        [TearDown]
        public void ResetTime()
        {
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f;
        }

        IEnumerator Walk(Vector2 move, float seconds)
        {
            input.Move = move;
            yield return new WaitForSeconds(seconds);
            input.Move = Vector2.zero;
            yield return new WaitForSeconds(0.2f);
        }

        // Its night: a warm low moon (not the city's cold high one), the leaves falling, the mist lying low.
        [UnityTest]
        public IEnumerator TheOrchard_HasItsHarvestMoon_LeavesAndMist()
        {
            var moon = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l => l.type == LightType.Directional);
            Assert.Greater(moon.color.r, moon.color.b, "a warm moon");
            Assert.Less(moon.transform.eulerAngles.x, 35f, "and low: long shadows");
            var air = GameObject.Find("Orchard Air");
            Assert.IsNotNull(air);
            yield return new WaitForSeconds(0.5f);
            var leaves = air.transform.Find("Falling Leaves").GetComponent<ParticleSystem>();
            Assert.Greater(leaves.particleCount, 50, "leaves in the air from the start");
            Assert.Greater(air.GetComponentsInChildren<ParticleSystem>().Count(p => p.name == "Mist"), 2, "mist on the terraces");
        }

        // Up the steps from the bottom terrace to the middle, and from the middle to the top; down again by a drop.
        [UnityTest]
        public IEnumerator TheTerraces_WalkUpTheSteps_AndDropDown()
        {
            apprentice.Teleport(new Vector3(-9f, 0.55f, -1.6f));
            yield return Walk(Vector2.up, 1.8f);
            Assert.AreEqual(1.9f, apprentice.transform.position.y, 0.2f, $"up on the middle terrace ({apprentice.transform.position})");
            apprentice.Teleport(new Vector3(3f, 1.95f, 6.8f));
            yield return Walk(Vector2.up, 1.8f);
            Assert.AreEqual(3.3f, apprentice.transform.position.y, 0.2f, $"up on the top terrace ({apprentice.transform.position})");
            apprentice.Teleport(new Vector3(-6f, 3.35f, 10.8f));
            yield return Walk(Vector2.down, 1f);
            Assert.AreEqual(1.9f, apprentice.transform.position.y, 0.2f, "dropped down a terrace (one way)");
            Assert.IsTrue(apprentice.IsAlive && apprentice.CurrentState == Apprentice.State.Free, "no harm in it");
        }

        // The press house: up its stair (stepped onto from the floor beside it) to the loft; inside, its roof fades (and the loft over you,
        // down on the floor).
        [UnityTest]
        public IEnumerator ThePressHouse_UpTheStairToTheLoft_TheRoofFades()
        {
            var house = UnityEngine.Object.FindAnyObjectByType<Storeys>();
            apprentice.Teleport(new Vector3(12.4f, 1.95f, 3.5f)); // (on the floor, beside the stair's foot)
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(0, house.Level, "inside, on the floor");
            Assert.IsFalse(house.Shown(2), "the roof gone");
            Assert.IsFalse(house.Shown(1), "and the loft over you");
            yield return Walk(Vector2.right, 0.9f); // onto the stair from the side
            Assert.Greater(apprentice.transform.position.x, 13.5f, $"onto the stair ({apprentice.transform.position})");
            yield return Walk(Vector2.up, 2.6f);
            Assert.AreEqual(4.3f, apprentice.transform.position.y, 0.25f, $"up in the loft ({apprentice.transform.position})");
            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual(1, house.Level);
            Assert.IsTrue(house.Shown(1));
            Assert.IsFalse(house.Shown(2), "the roof still gone");
        }

        MothFlock Flock(string name) => UnityEngine.Object.FindObjectsByType<MothFlock>(FindObjectsSortMode.None).First(f => f.name == name);

        IEnumerator WaitFor(Func<bool> done, float seconds, string what)
        {
            for (float t = 0f; t < seconds && !done(); t += Time.deltaTime)
                yield return null;
            Assert.IsTrue(done(), $"waited {seconds} s for {what}");
        }

        // A flock circles its lantern while nothing's near; come close and they all come to you, circling, and send one
        // in at a time: it stalls, flaring, then dives, and strikes if you stand there.
        [UnityTest]
        public IEnumerator Moths_CircleTheirLantern_ThenComeAndDive()
        {
            var flock = Flock("Moths (middle terrace)");
            Assert.AreEqual(6, flock.Moths.Count);
            Assert.IsFalse(flock.Chasing, "circling their light");
            Assert.Less(Vector3.Distance(flock.Moths[0].transform.position, flock.lamp.position), 3f);
            Moth dived = null;
            Action<Moth> onDive = m => { if (m.Flock == flock) dived = m; };
            Moth.Dived += onDive;
            try
            {
                float health = apprentice.Health;
                apprentice.Teleport(new Vector3(0.8f, 1.95f, 4.2f));
                yield return WaitFor(() => flock.Chasing, 3f, "the flock coming");
                yield return WaitFor(() => dived != null, 6f, "one diving");
                yield return WaitFor(() => apprentice.Health < health, 2f, "its dive striking");
                Assert.AreEqual(health - dived.diveDamage, apprentice.Health, 0.5f, "a small hurt");
                int diving = flock.Moths.Count(m => m.Diving);
                Assert.LessOrEqual(diving, 1, "one at a time");
            }
            finally { Moth.Dived -= onDive; }
        }

        // One blow scatters a Moth; a heavy one goes on through all it reaches.
        [UnityTest]
        public IEnumerator Moths_OneBlowEach_TheHeavySweepsThroughSeveral()
        {
            var moths = Flock("Moths (middle terrace)").Moths;
            apprentice.Teleport(new Vector3(0.5f, 1.95f, 3.2f));
            apprentice.transform.rotation = Quaternion.LookRotation(Vector3.right);
            for (int i = 0; i < 3; i++)
                moths[i].Pin(apprentice.transform.position + new Vector3(1.3f, 1.0f, (i - 1) * 0.6f));
            for (int i = 4; i < moths.Count; i++)
                moths[i].Pin(apprentice.transform.position + new Vector3(-6f, 3f, i)); // (the rest held out of it: no dive cutting in)
            yield return null;
            input.PressHeavy();
            yield return new WaitForSeconds(1.2f);
            Assert.AreEqual(3, moths.Take(3).Count(m => !m.IsAlive), "all three, in one heavy swing");
            moths[3].Pin(apprentice.transform.position + new Vector3(1.2f, 1.0f, 0f));
            yield return new WaitForSeconds(0.3f);
            input.PressLight();
            yield return new WaitForSeconds(0.6f);
            Assert.IsFalse(moths[3].IsAlive, "and one light blow, one Moth");
        }

        // Fire takes them like paper, and one alight sets the next alight: a flock goes up together.
        [UnityTest]
        public IEnumerator Moths_OneAlight_SetsTheFlockAlight()
        {
            var flock = Flock("Moths (middle terrace)");
            foreach (var m in flock.Moths)
                m.passive = true; // (circling their lantern, close together)
            yield return new WaitForSeconds(0.5f);
            Burning.Set(flock.Moths[0], 1.5f, 6f);
            yield return WaitFor(() => flock.Alive == 0, 6f, "the whole flock gone up");
        }

        // Everything in the Orchard burns harder: the same fire does more here than in the Drowned City.
        [UnityTest]
        public IEnumerator InTheOrchard_EverythingBurnsHarder()
        {
            Assert.Greater(AreaTraits.FireHarm, 1.4f, "the Orchard's dry");
            var post = UnityEngine.Object.FindAnyObjectByType<TesterPost>();
            var laden = (Laden)post.spawner.SpawnAt("Laden", new Vector3(-4f, 0.55f, -5f), Vector3.forward);
            laden.passive = true;
            yield return new WaitForSeconds(0.3f);
            float before = laden.Health;
            laden.TakeBurn(10f, laden.transform.position);
            Assert.AreEqual(before - 10f * AreaTraits.FireHarm, laden.Health, 0.01f);
        }

        // A dive caught on a perfect block breaks against the oar.
        [UnityTest]
        public IEnumerator Moths_ADivePerfectBlocked_Scatters()
        {
            var flock = Flock("Moths (middle terrace)");
            Moth dived = null;
            Action<Moth> onDive = m => { if (m.Flock == flock) dived = m; };
            Moth.Dived += onDive;
            try
            {
                apprentice.Teleport(new Vector3(0.8f, 1.95f, 4.2f));
                yield return WaitFor(() => dived != null, 8f, "one diving");
                var moth = dived;
                var toIt = moth.transform.position - apprentice.transform.position;
                apprentice.transform.rotation = Quaternion.LookRotation(new Vector3(toIt.x, 0f, toIt.z));
                yield return WaitFor(() => !moth.IsAlive || Vector3.Distance(moth.transform.position, apprentice.transform.position + Vector3.up * 0.6f) < 1.7f, 2f, "it close");
                input.BlockHeld = true;
                yield return WaitFor(() => !moth.IsAlive, 1f, "it broken on the block");
                input.BlockHeld = false;
            }
            finally { Moth.Dived -= onDive; }
        }

        // ---- The Watchman (Abner Wick, Who Merits Flame)

        Watchman Watchman() => UnityEngine.Object.FindAnyObjectByType<Watchman>();

        // Stands the Apprentice `distance` from `figure`, out towards the middle of the field, facing it (or away).
        void StandBy(Component figure, float distance, bool facing)
        {
            Vector3 at = figure.transform.position, centre = new Vector3(-2f, at.y, 26f); // (the field's middle)
            Vector3 out_ = centre - at;
            out_.y = 0f;
            out_ = out_.sqrMagnitude < 0.5f ? Vector3.back : out_.normalized;
            apprentice.Teleport(at + out_ * distance + Vector3.up * 0.05f);
            apprentice.transform.rotation = Quaternion.LookRotation(facing ? -out_ : out_);
        }

        // Stands the Apprentice `distance` from him, facing him, with nothing between (no scarecrow in the line of fire).
        void StandClear(Watchman w, float distance)
        {
            Vector3 at = w.transform.position;
            for (int k = 0; k < 24; k++)
            {
                var dir = Quaternion.Euler(0f, k * 15f, 0f) * Vector3.back;
                Vector3 p = at + dir * distance;
                if (p.x < -12.5f || p.x > 8.5f || p.z < 19f || p.z > 33f)
                    continue;
                bool clear = w.field.All(s => !s.IsAlive || DistanceToSegment(s.transform.position, at, p) > 1.3f);
                if (!clear)
                    continue;
                apprentice.Teleport(p + Vector3.up * 0.05f);
                apprentice.transform.rotation = Quaternion.LookRotation(-dir);
                return;
            }
            Assert.Fail("nowhere clear to stand");
        }

        static float DistanceToSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            point.y = a.y = b.y = 0f;
            Vector3 ab = b - a;
            float t = Mathf.Clamp01(Vector3.Dot(point - a, ab) / ab.sqrMagnitude);
            return Vector3.Distance(a + ab * t, point);
        }

        // Found: struck once (no harm), he tears free.
        IEnumerator Reveal(Watchman w)
        {
            StandBy(w, 1.6f, true);
            yield return null;
            input.PressLight();
            yield return WaitFor(() => !w.Hidden, 1f, "him found");
            yield return new WaitForSeconds(1.4f); // (tearing free)
        }

        // Scorched (as by his own fire turned back) and lost again among them: found again.
        IEnumerator ScorchAndFind(Watchman w)
        {
            int round = w.Round;
            w.Scorch(apprentice.transform.position);
            if (round == 1)
            {
                yield return WaitFor(() => w.Hidden, 4f, "him hiding again");
                yield return new WaitForSeconds(3.8f); // (the lanterns back)
                yield return Reveal(w);
            }
            else
                yield return new WaitForSeconds(1.5f);
        }

        // Six scarecrows in the field, all alike: one of them is him, still hidden, his lantern dark; no fight yet.
        [UnityTest]
        public IEnumerator TheWatchman_StandsAmongTheScarecrows_Hidden()
        {
            var w = Watchman();
            Assert.IsNotNull(w);
            Assert.AreEqual(5, w.field.Length);
            Assert.IsTrue(w.Hidden);
            Assert.AreEqual(0, w.Round);
            Assert.IsFalse(w.lantern.enabled, "his lantern dark");
            Assert.IsNull(BossFight.Current);
            foreach (var s in w.field)
            {
                Assert.AreEqual(w.transform.position.y, s.transform.position.y, 0.1f, "in the field with him");
                Assert.AreEqual(w.transform.eulerAngles.y, s.transform.eulerAngles.y, 1f, "facing the same way");
                Assert.Greater(Vector3.Distance(w.transform.position, s.transform.position), 2.5f);
            }
            yield return null;
        }

        // Only one of them turns on its stake to watch you: him.
        [UnityTest]
        public IEnumerator TheWatchman_Hidden_OnlyHeTurnsToWatchYou()
        {
            var w = Watchman();
            var yaws = w.field.Select(s => s.transform.eulerAngles.y).ToArray();
            StandBy(w, 5f, false);
            var a = apprentice.transform.position;
            yield return new WaitForSeconds(4f);
            Vector3 toYou = a - w.transform.position;
            toYou.y = 0f;
            Assert.Less(Vector3.Angle(w.transform.forward, toYou), 10f, "he's watching you");
            for (int i = 0; i < yaws.Length; i++)
                Assert.AreEqual(yaws[i], w.field[i].transform.eulerAngles.y, 0.5f, "the rest stare on as they were");
            Assert.IsTrue(w.Hidden);
        }

        // Strike the wrong one: it catches and goes up in flames, burning you. He stays hidden.
        [UnityTest]
        public IEnumerator AScarecrowStruck_GoesUpInFlames_AndBurnsYou()
        {
            var w = Watchman();
            var s = w.field[0];
            StandBy(s, 1.6f, true);
            yield return null;
            float health = apprentice.Health;
            input.PressLight();
            yield return WaitFor(() => !s.IsAlive, 1f, "it struck");
            yield return WaitFor(() => apprentice.Health < health, 1.5f, "its flames");
            Assert.AreEqual(health - s.burstDamage, apprentice.Health, 0.5f);
            Assert.IsTrue(apprentice.Alight, "and you're alight");
            Assert.IsTrue(w.Hidden, "he's still one of them");
            Assert.IsNull(BossFight.Current);
        }

        // Strike the right one: no harm done, but he's found: he tears free, his lantern lights, the fight is on.
        [UnityTest]
        public IEnumerator TheWatchmanStruck_TearsFree_TheFightBegins()
        {
            var w = Watchman();
            StandBy(w, 1.6f, true);
            yield return null;
            input.PressLight();
            yield return WaitFor(() => !w.Hidden, 1f, "him found");
            Assert.AreEqual(w.maxHealth, w.Health, "the blow that found him did no harm");
            Assert.AreEqual(1, w.Round);
            yield return null;
            yield return null;
            Assert.AreSame(w, BossFight.Current, "his fight");
            Assert.IsTrue(w.lantern.enabled, "his lantern lit");
            yield return new WaitForSeconds(1.4f);
            StandBy(w, 6f, false); // (back turned: he comes)
            yield return new WaitForSeconds(1.2f);
            Assert.Greater(Vector3.Distance(w.transform.position, w.pole.position), 0.3f, "torn free: the stake left standing");
        }

        // He's straw: the oar goes through him. Fire burns him (a little).
        [UnityTest]
        public IEnumerator TheWatchman_TheOarGoesThrough_FireBurns()
        {
            var w = Watchman();
            yield return Reveal(w);
            w.TakeHit(new Hit { Damage = 80f, Heavy = true, From = apprentice.transform.position });
            Assert.AreEqual(w.maxHealth, w.Health, "straw");
            w.TakeBurn(10f, apprentice.transform.position);
            Assert.Less(w.Health, w.maxHealth, "fire");
            Assert.Greater(w.Health, w.maxHealth - 30f, "a little");
        }

        // Faced, he stands still and breathes fire at you; turned back with a perfect block, it scorches a third of
        // him away.
        [UnityTest]
        public IEnumerator TheWatchman_Watched_BreathesFire_TurnedBack_ItScorchesHim()
        {
            var w = Watchman();
            yield return Reveal(w);
            Fireball ball = null;
            Action<Watchman, Fireball> onBreath = (_, f) => ball = f;
            global::MyGame.Watchman.Breathed += onBreath;
            try
            {
                StandClear(w, 7f);
                Vector3 at = w.transform.position;
                yield return WaitFor(() => ball != null, 6f, "his fire");
                Assert.Less(Vector3.Distance(at, w.transform.position), 0.2f, "watched: not a step");
                yield return WaitFor(() => ball == null || Vector3.Distance(ball.transform.position, apprentice.transform.position + Vector3.up * 0.7f) < 1.6f, 3f, "it close");
                input.BlockHeld = true;
                yield return WaitFor(() => ball == null || ball.Deflected, 1f, "it turned back");
                input.BlockHeld = false;
                yield return WaitFor(() => w.Health < w.maxHealth, 2f, "it striking him");
                Assert.AreEqual(w.maxHealth * 2f / 3f, w.Health, 1f, "a third of him gone");
            }
            finally { global::MyGame.Watchman.Breathed -= onBreath; }
        }

        // By the well, before the field's gate: Sodden Sacks to pick up.
        [UnityTest]
        public IEnumerator BytheWell_SoddenSacks()
        {
            var sack = UnityEngine.Object.FindObjectsByType<ProvisionFind>(FindObjectsSortMode.None).FirstOrDefault(f => f.provision == "sodden_sack");
            Assert.IsNotNull(sack, "a Sodden Sack lying by the well");
            apprentice.Teleport(sack.transform.position + Vector3.back * 0.8f);
            yield return new WaitForSeconds(0.3f);
            input.PressInteract();
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(2, apprentice.Equipment.Carried(Provision.SoddenSack), "picked up: two");
        }

        // ---- The Gleaners

        Gleaner MiddleGleaner => UnityEngine.Object.FindObjectsByType<Gleaner>(FindObjectsSortMode.None)
            .Where(g => g.transform.position.y < 2.5f).OrderBy(g => g.transform.position.x).First(); // (the middle terrace's west one)

        // Four in the Orchard: two in the middle rows, two before the field's gate.
        [UnityTest]
        public IEnumerator Gleaners_InTheRows()
        {
            var all = UnityEngine.Object.FindObjectsByType<Gleaner>(FindObjectsSortMode.None);
            Assert.AreEqual(4, all.Length);
            Assert.AreEqual(2, all.Count(g => g.transform.position.y < 2.5f), "two on the middle terrace");
            yield return null;
        }

        // It keeps its distance: close in, and it skips away from the oar.
        [UnityTest]
        public IEnumerator Gleaner_SkipsAwayFromTheOar()
        {
            var g = MiddleGleaner;
            Vector3 at = g.transform.position;
            apprentice.Teleport(at + Vector3.right * 5f);
            apprentice.transform.rotation = Quaternion.LookRotation(Vector3.left);
            yield return WaitFor(() => g.CurrentState != Soured.State.Idle, 3f, "it noticing you");
            int hops = 0;
            Action<Gleaner> onHop = x => { if (x == g) hops++; };
            Gleaner.Hopped += onHop;
            try
            {
                // (not while it's winding up or throwing: it's committed then; wait till it's free to move)
                for (float t = 0f; t < 3f && g.CurrentState != Soured.State.Chase; t += Time.deltaTime)
                    yield return null;
                apprentice.Teleport(g.transform.position + (apprentice.transform.position - g.transform.position).Flat().normalized * 2.2f);
                apprentice.transform.rotation = Quaternion.LookRotation((g.transform.position - apprentice.transform.position).Flat());
                input.PressLight();
                yield return WaitFor(() => hops > 0, 1f, "it skipping away");
                yield return new WaitForSeconds(0.5f);
                Assert.Greater(Vector3.Distance(g.transform.position.Flat(), apprentice.transform.position.Flat()), 3.5f, "out of the oar's reach");
                Assert.AreEqual(g.maxHealth, g.Health, "and unhurt");
            }
            finally { Gleaner.Hopped -= onHop; }
        }

        // Its fire, turned back with a perfect block: it goes up at once.
        [UnityTest]
        public IEnumerator Gleaner_ItsFireTurnedBack_BurnsItUp()
        {
            var g = MiddleGleaner;
            Fireball ball = null;
            Action<Gleaner, Fireball> onFling = (x, f) => { if (x == g && ball == null) ball = f; };
            Gleaner.Flung += onFling;
            try
            {
                apprentice.Teleport(g.transform.position + Vector3.right * 8f);
                apprentice.transform.rotation = Quaternion.LookRotation(Vector3.left);
                yield return WaitFor(() => ball != null, 8f, "its fire");
                yield return Parry(ball, g);
                yield return WaitFor(() => !g.IsAlive, 3f, "it burning up");
                Assert.IsTrue(apprentice.Equipment.Met(LedgerEntry.Gleaners), "written in the Ledger");
            }
            finally { Gleaner.Flung -= onFling; }
        }

        // His fire taken full (not blocked): you're alight.
        [UnityTest]
        public IEnumerator TheWatchman_HisFireTaken_SetsYouAlight()
        {
            var w = Watchman();
            yield return Reveal(w);
            Fireball ball = null;
            Defence outcome = Defence.Dodged;
            Action<Watchman, Fireball> onBreath = (_, f) => { if (ball == null) { ball = f; f.Done = (b, d) => outcome = d; } };
            global::MyGame.Watchman.Breathed += onBreath;
            try
            {
                StandClear(w, 7f);
                yield return WaitFor(() => ball != null, 6f, "his fire");
                yield return WaitFor(() => ball == null, 3f, "it reaching you");
                Assert.AreEqual(Defence.Taken, outcome);
                Assert.IsTrue(apprentice.Alight, "alight");
            }
            finally { global::MyGame.Watchman.Breathed -= onBreath; }
        }

        // Behind you, he comes, and sweeps his reaping hook.
        [UnityTest]
        public IEnumerator TheWatchman_Unwatched_SweepsHisHook()
        {
            var w = Watchman();
            yield return Reveal(w);
            StandBy(w, 2f, false);
            float health = apprentice.Health;
            yield return WaitFor(() => apprentice.Health < health, 4f, "the hook");
            Assert.AreEqual(health - w.primary.damage, apprentice.Health, 0.5f);
        }

        // Scorched once, he flings himself back among them: in the dark they all move, then they all watch you; he's
        // the one smouldering. Found again: round 2, his fire a fan of three (or held back).
        [UnityTest]
        public IEnumerator TheWatchman_ScorchedOnce_HidesAgain_TheyAllWatch()
        {
            var w = Watchman();
            yield return Reveal(w);
            var places = new[] { w.transform.position }.Concat(w.field.Select(s => s.transform.position)).ToArray();
            int lost = 0;
            Action<Watchman> onLost = _ => lost++;
            global::MyGame.Watchman.Lost += onLost;
            try
            {
                w.Scorch(apprentice.transform.position);
                yield return WaitFor(() => lost > 0, 4f, "him lost among them");
                Assert.IsTrue(w.Hidden);
                Assert.AreEqual(2, w.Round);
                Assert.AreSame(w, BossFight.Current, "the fight goes on (the gate stays shut)");
                Assert.IsTrue(w.field.All(s => s.Follows), "they all watch you now");
                Assert.IsTrue(places.Any(p => Vector3.Distance(new Vector3(p.x, 0f, p.z), new Vector3(w.transform.position.x, 0f, w.transform.position.z)) < 0.2f), "in one of their places");
                Assert.Less(apprentice.GetComponent<BowLantern>().Dimmed, 0.5f, "even your lantern burns low");
                yield return new WaitForSeconds(3.8f);
                Assert.AreEqual(1f, apprentice.GetComponent<BowLantern>().Dimmed, 0.01f, "the light back");
                Assert.Greater(Effects.Instance.LiveSmoke, 0, "a thread of smoke off him");
                Assert.IsFalse(w.GetComponentsInChildren<Light>().Any(l => l.enabled), "and no glow: nothing lit gives him away");
                yield return Reveal(w);
                Assert.AreEqual(2, w.Round);
                int balls = 0;
                Action<Watchman, Fireball> onBreath = (_, f) => balls++;
                global::MyGame.Watchman.Breathed += onBreath;
                try
                {
                    StandClear(w, 7f);
                    yield return WaitFor(() => balls > 0, 6f, "his fire");
                    yield return new WaitForSeconds(0.1f);
                    Assert.IsTrue(balls == 1 || balls == 3, $"a fan of three, or one held back ({balls})");
                }
                finally { global::MyGame.Watchman.Breathed -= onBreath; }
            }
            finally { global::MyGame.Watchman.Lost -= onLost; }
        }

        // A perfect block on `ball`, raised just before it arrives, facing him all the while.
        IEnumerator Parry(Fireball ball, Component w)
        {
            bool done = false;
            var outcome = Defence.Dodged;
            var previous = ball.Done;
            ball.Done = (f, d) => { done = true; outcome = d; previous?.Invoke(f, d); };
            while (!done && ball != null && Vector3.Distance(ball.transform.position, apprentice.transform.position + Vector3.up * 0.7f) > 1.5f)
            {
                Vector3 to = w.transform.position - apprentice.transform.position;
                to.y = 0f;
                apprentice.transform.rotation = Quaternion.LookRotation(to);
                yield return null;
            }
            input.BlockHeld = true;
            for (float t = 0f; t < 1f && !done; t += Time.deltaTime)
                yield return null;
            input.BlockHeld = false;
            Assert.IsTrue(done, "it reached you");
            Assert.AreEqual(Defence.Parried, outcome, "perfect-blocked (not over your head, nor past you)");
        }

        // Scorched twice: he tears free for good, alight, never still (watched or not). His fire comes three small
        // balls at a time: knock all three aside (they glance off, never back at him) and he draws his last great
        // breath; turned back, it ends him.
        [UnityTest]
        public IEnumerator TheWatchman_Round3_KnockThreeAside_ThenTurnBackHisLast()
        {
            var w = Watchman();
            yield return Reveal(w);
            yield return ScorchAndFind(w);
            yield return ScorchAndFind(w);
            Assert.AreEqual(3, w.Round);
            Assert.IsTrue(w.Thrashing);
            StandClear(w, 8f);
            var balls = new System.Collections.Generic.List<Fireball>();
            Action<Watchman, Fireball> onBreath = (_, f) => balls.Add(f);
            global::MyGame.Watchman.Breathed += onBreath;
            IBoss beaten = null;
            Action<IBoss> onDefeat = b => beaten = b;
            BossFight.Defeated += onDefeat;
            try
            {
                Vector3 at = w.transform.position;
                yield return new WaitForSeconds(1.0f);
                Assert.Greater(Vector3.Distance(at, w.transform.position), 0.8f, "coming, though you face him");
                StandClear(w, 9f);
                float health = w.Health;
                for (int i = 0; i < 3; i++)
                {
                    yield return WaitFor(() => balls.Count > i, 6f, $"small fire {i + 1}");
                    Assert.IsTrue(balls[i].glances, "a small one: knocked aside");
                    yield return Parry(balls[i], w); // (each one: parried, not missed)
                    Assert.AreEqual(1f - (i + 1) / 3f, w.Breath, 0.01f, "his breath, spent a step");
                    if (i < 2)
                        Assert.IsTrue(w.lantern.enabled);
                }
                yield return null;
                Assert.IsFalse(w.lantern.enabled, "three: his lantern out, a beat");
                yield return new WaitForSeconds(0.5f);
                Assert.AreEqual(health, w.Health, 0.01f, "they glance off: none back at him");
                Assert.AreEqual(3, w.Knocked);
                if (Vector3.Distance(w.transform.position, apprentice.transform.position) < 5f)
                    StandClear(w, 8f);
                yield return WaitFor(() => balls.Count > 3, 6f, "his last great breath");
                var last = balls[3];
                Assert.IsFalse(last.glances, "the great one: to turn back");
                yield return Parry(last, w);
                yield return WaitFor(() => beaten != null, 3f, "it ending him");
                Assert.AreSame(w, beaten, "his farewell");
                Assert.AreEqual("FLAME MERITED", w.Farewell);
                Assert.AreEqual("Abner Wick", w.Title);
                Assert.IsFalse(w.lantern.enabled);
            }
            finally
            {
                global::MyGame.Watchman.Breathed -= onBreath;
                BossFight.Defeated -= onDefeat;
            }
        }

        // Miss one of the three (it hits you) and there's no great breath: the next is three small ones again.
        [UnityTest]
        public IEnumerator TheWatchman_Round3_MissOne_NoLastBreath()
        {
            var w = Watchman();
            yield return Reveal(w);
            yield return ScorchAndFind(w);
            yield return ScorchAndFind(w);
            var balls = new System.Collections.Generic.List<Fireball>();
            Action<Watchman, Fireball> onBreath = (_, f) => balls.Add(f);
            global::MyGame.Watchman.Breathed += onBreath;
            try
            {
                StandClear(w, 9f);
                for (int i = 0; i < 3; i++)
                {
                    yield return WaitFor(() => balls.Count > i, 6f, $"small fire {i + 1}");
                    if (i < 2)
                        yield return Parry(balls[i], w);
                }
                Assert.AreEqual(1f / 3f, w.Breath, 0.01f);
                yield return WaitFor(() => balls[2] == null, 3f, "the third striking you");
                yield return new WaitForSeconds(0.3f);
                Assert.IsFalse(w.FinaleDrawn, "two of three: not enough");
                Assert.AreEqual(1f, w.Breath, 0.01f, "the row broken: his lantern full again");
                for (float t = 0f; t < 8f && balls.Count <= 3; t += Time.deltaTime)
                {
                    if (Vector3.Distance(w.transform.position, apprentice.transform.position) < 5f)
                        StandClear(w, 9f); // (keeping out of his hook's reach)
                    yield return null;
                }
                Assert.Greater(balls.Count, 3, "his fire again");
                Assert.IsTrue(balls[3].glances, "three small ones again");
            }
            finally { global::MyGame.Watchman.Breathed -= onBreath; }
        }

        // Lock-on never picks a scarecrow, nor him while he's still one of them; found, it does.
        [UnityTest]
        public IEnumerator LockOn_NeverAScarecrow_OnlyHimFighting()
        {
            var w = Watchman();
            StandBy(w.field[0], 2f, true);
            yield return null;
            input.PressLock();
            yield return null;
            yield return null;
            Assert.IsNull(apprentice.LockTarget, "a scarecrow: nothing to lock on to");
            StandBy(w, 2f, true);
            yield return null;
            input.PressLock();
            yield return null;
            yield return null;
            Assert.IsNull(apprentice.LockTarget, "nor him, on his stake");
            yield return Reveal(w);
            StandBy(w, 3f, true);
            input.PressLock();
            yield return null;
            yield return null;
            Assert.AreSame(w, apprentice.LockTarget, "found: him");
        }

        // Nothing that follows you crosses into the field: it stops at the edge and gives you up.
        [UnityTest]
        public IEnumerator AnArena_WhatFollowsYouNeverCrossesIn()
        {
            var gate = UnityEngine.Object.FindAnyObjectByType<ArenaGate>();
            Vector3 way = gate.transform.position;
            var post = UnityEngine.Object.FindAnyObjectByType<TesterPost>();
            var laden = (Laden)post.spawner.SpawnAt("Laden", new Vector3(way.x, 3.35f, way.z - 5f), Vector3.forward);
            apprentice.Teleport(new Vector3(way.x, 3.35f, way.z - 2.2f));
            yield return WaitFor(() => laden.CurrentState == Soured.State.Chase, 3f, "it after you");
            float furthest = 0f;
            input.Move = Vector2.up;
            for (float t = 0f; t < 2.5f; t += Time.deltaTime)
            {
                furthest = Mathf.Max(furthest, laden.transform.position.z);
                yield return null;
            }
            input.Move = Vector2.zero;
            Assert.Greater(apprentice.transform.position.z, way.z + 2f, "you're in the field");
            for (float t = 0f; t < 3f; t += Time.deltaTime)
            {
                furthest = Mathf.Max(furthest, laden.transform.position.z);
                yield return null;
            }
            Assert.Less(furthest, way.z + 0.6f, "it never crossed in");
            Assert.AreNotEqual(Soured.State.Chase, laden.CurrentState, "it gave you up");
        }

        // A Rest: he's one of them again, somewhere among them (each spot taken once), the scarecrows whole.
        [UnityTest]
        public IEnumerator TheWatchman_AfterARest_HidesAgainAmongThem()
        {
            var w = Watchman();
            var spots = new[] { w.transform.position }.Concat(w.field.Select(s => s.transform.position)).ToArray();
            w.field[1].Slay(apprentice.transform.position);
            yield return Reveal(w);
            WorldState.Instance.ReformAllSoured();
            yield return null;
            Assert.IsTrue(w.Hidden);
            Assert.AreEqual(0, w.Round);
            Assert.IsFalse(w.lantern.enabled);
            Assert.IsTrue(w.field.All(s => s.IsAlive && !s.Follows), "the scarecrows whole");
            var now = new[] { w }.Cast<Soured>().Concat(w.field).Select(s => s.transform.position).ToArray();
            foreach (var p in spots)
                Assert.AreEqual(1, now.Count(q => Vector3.Distance(new Vector3(p.x, 0f, p.z), new Vector3(q.x, 0f, q.z)) < 0.2f), $"one at {p}");
            Assert.Less(Vector3.Distance(w.transform.position, w.pole.position), 0.3f, "on his stake again");
        }

        // (SHOTS=dir) The Watchman: the field, him creeping, found, faced, the hook, alight.
        [UnityTest]
        public IEnumerator Capture_Watchman()
        {
            string outDir = Environment.GetEnvironmentVariable("SHOTS");
            if (string.IsNullOrEmpty(outDir))
                Assert.Ignore("set SHOTS to capture");
            Directory.CreateDirectory(outDir);
            Time.captureFramerate = 30;
            var cam = Camera.main;
            var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            cam.targetTexture = rt;
            void Shot(string name)
            {
                var previous = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                RenderTexture.active = previous;
                File.WriteAllBytes(Path.Combine(outDir, name + ".png"), tex.EncodeToPNG());
            }
            IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
            var w = Watchman();
            foreach (var m in Flock("Moths (middle terrace)").Moths)
                m.Pin(m.transform.position); // (the middle terrace's: kept to their lantern, out of it)
            apprentice.Teleport(new Vector3(-1.7f, 3.35f, 16.2f), true, true);
            yield return Frames(45);
            Shot("watchman-0-the-gateway");
            apprentice.Teleport(new Vector3(-1.7f, 3.35f, 20f), true, true);
            yield return Frames(45);
            Shot("watchman-0-the-field");
            var s = w.field[0];
            StandBy(s, 1.6f, true);
            yield return Frames(5);
            input.PressLight();
            for (int i = 0; i < 60 && s.IsAlive; i++)
                yield return null;
            yield return Frames(6);
            Shot("watchman-1a-a-scarecrow-catches");
            yield return Frames(9);
            Shot("watchman-1b-and-goes-up");
            yield return Frames(30);
            StandBy(w, 1.6f, true);
            yield return Frames(30);
            input.PressLight();
            yield return Frames(14);
            Shot("watchman-2-found");
            yield return Frames(30);
            Shot("watchman-3-torn-free");
            var gate = UnityEngine.Object.FindAnyObjectByType<ArenaGate>();
            var gateCam = new GameObject("Gate Camera").AddComponent<Camera>();
            gateCam.CopyFrom(cam);
            gateCam.targetTexture = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            gateCam.transform.SetPositionAndRotation(gate.transform.position + new Vector3(0f, 9f, -7f), Quaternion.Euler(50f, 0f, 0f));
            gateCam.Render();
            var previousRt = RenderTexture.active;
            RenderTexture.active = gateCam.targetTexture;
            var gateTex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            gateTex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            gateTex.Apply();
            RenderTexture.active = previousRt;
            File.WriteAllBytes(Path.Combine(outDir, "watchman-3b-the-gate-shut.png"), gateTex.EncodeToPNG());
            UnityEngine.Object.Destroy(gateCam.gameObject);
            // Faced: he draws breath over his lantern, and breathes fire.
            StandClear(w, 5f);
            for (int i = 0; i < 150 && w.CurrentState != Soured.State.Telegraph; i++)
                yield return null;
            yield return Frames(18);
            Shot("watchman-4-drawing-breath");
            for (int i = 0; i < 60 && Fireball.Live == 0; i++)
                yield return null;
            yield return Frames(5);
            Shot("watchman-5-his-fire");
            yield return Frames(30);
            // Scorched: he hides again in the dark; they all watch you; he smoulders.
            w.Scorch(apprentice.transform.position);
            yield return Frames(8);
            Shot("watchman-6-scorched");
            for (int i = 0; i < 120 && !w.Hidden; i++)
                yield return null;
            yield return Frames(4);
            Shot("watchman-7-the-dark");
            yield return Frames(120);
            apprentice.Teleport(new Vector3(-2f, 3.35f, 24f), true, true);
            yield return Frames(45);
            Shot("watchman-8-they-all-watch");
            StandBy(w, 2.2f, true);
            yield return Frames(45);
            Shot("watchman-8b-up-close-smouldering");
            // Round 3: alight, never still.
            StandBy(w, 1.6f, true);
            yield return Frames(3);
            input.PressLight();
            yield return Frames(45);
            w.Scorch(apprentice.transform.position);
            yield return Frames(60);
            StandBy(w, 7f, true);
            yield return Frames(20);
            Shot("watchman-9-alight");
            for (int i = 0; i < 200 && Fireball.Live == 0; i++)
                yield return null;
            yield return Frames(6);
            Shot("watchman-10-three-small");
        }

        // (SHOTS=dir) A Gleaner: in the rows; drawing back to throw (its lantern flaring); its fire.
        [UnityTest]
        public IEnumerator Capture_Gleaner()
        {
            string outDir = Environment.GetEnvironmentVariable("SHOTS");
            if (string.IsNullOrEmpty(outDir))
                Assert.Ignore("set SHOTS to capture");
            Directory.CreateDirectory(outDir);
            Time.captureFramerate = 30;
            var cam = Camera.main;
            var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            cam.targetTexture = rt;
            void Shot(string name)
            {
                var previous = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                RenderTexture.active = previous;
                File.WriteAllBytes(Path.Combine(outDir, name + ".png"), tex.EncodeToPNG());
            }
            var g = MiddleGleaner;
            foreach (var m in Flock("Moths (middle terrace)").Moths)
                m.Pin(m.transform.position);
            apprentice.Teleport(g.transform.position + new Vector3(0.5f, 0.05f, -3.6f), true, true);
            for (int i = 0; i < 20; i++)
                yield return null;
            Shot("gleaner-0-in-the-rows");
            for (int i = 0; i < 200 && g.CurrentState != Soured.State.Telegraph; i++)
                yield return null;
            for (int i = 0; i < 18; i++)
                yield return null;
            Shot("gleaner-1-drawing-back");
            for (int i = 0; i < 60 && Fireball.Live == 0; i++)
                yield return null;
            for (int i = 0; i < 6; i++)
                yield return null;
            Shot("gleaner-2-its-fire");
        }

        // (SHOTS=dir) The Moths: round their lantern, come to you, a stall, a dive; and a flock going up.
        [UnityTest]
        public IEnumerator Capture_Moths()
        {
            string outDir = Environment.GetEnvironmentVariable("SHOTS");
            if (string.IsNullOrEmpty(outDir))
                Assert.Ignore("set SHOTS to capture");
            Directory.CreateDirectory(outDir);
            Time.captureFramerate = 30;
            var cam = Camera.main;
            var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            cam.targetTexture = rt;
            void Shot(string name)
            {
                var previous = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                RenderTexture.active = previous;
                File.WriteAllBytes(Path.Combine(outDir, name + ".png"), tex.EncodeToPNG());
            }
            var flock = Flock("Moths (middle terrace)");
            apprentice.Teleport(new Vector3(-1.5f, 1.95f, 3.4f), true, true);
            for (int i = 0; i < 30; i++)
                yield return null;
            Shot("moths-0-round-their-lantern");
            apprentice.Teleport(new Vector3(0.8f, 1.95f, 4.2f), true, true);
            for (int i = 0; i < 90; i++)
                yield return null;
            Shot("moths-1-come");
            var stalled = flock.Moths.FirstOrDefault(m => m.CurrentState == Soured.State.Telegraph);
            for (int i = 0; i < 150 && stalled == null; i++)
            {
                yield return null;
                stalled = flock.Moths.FirstOrDefault(m => m.CurrentState == Soured.State.Telegraph);
            }
            Shot("moths-2-stall");
            while (stalled != null && stalled.CurrentState == Soured.State.Telegraph)
                yield return null;
            for (int i = 0; i < 3; i++)
                yield return null;
            Shot("moths-3-dive");
            Burning.Set(flock.Moths.First(m => m.IsAlive), 1.5f, 6f);
            for (int i = 0; i < 25; i++)
                yield return null;
            Shot("moths-4-alight");
            for (int i = 0; i < 40; i++)
                yield return null;
            Shot("moths-5-gone-up");
        }

        // (SHOTS=dir) The lab round: coming in, along the rows, the press house, its loft, the top terrace, and wide.
        [UnityTest]
        public IEnumerator Capture_Orchard()
        {
            string outDir = Environment.GetEnvironmentVariable("SHOTS");
            if (string.IsNullOrEmpty(outDir))
                Assert.Ignore("set SHOTS to capture");
            Directory.CreateDirectory(outDir);
            Time.captureFramerate = 30;
            var cam = Camera.main;
            var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            cam.targetTexture = rt;
            void Shot(string name, Camera from = null)
            {
                from ??= cam;
                var target = from.targetTexture;
                if (from != cam)
                    from.Render();
                var previous = RenderTexture.active;
                RenderTexture.active = target;
                var tex = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                tex.Apply();
                RenderTexture.active = previous;
                File.WriteAllBytes(Path.Combine(outDir, name + ".png"), tex.EncodeToPNG());
            }
            IEnumerator At(Vector3 p, string name)
            {
                apprentice.Teleport(p, true, true);
                for (int i = 0; i < 45; i++)
                    yield return null;
                Shot(name);
            }
            yield return At(new Vector3(-12f, 0.55f, -3.8f), "orchard-0-coming-in");
            yield return At(new Vector3(1f, 0.55f, -3.5f), "orchard-1-bottom-row");
            yield return At(new Vector3(-6f, 1.95f, 6.5f), "orchard-2-middle-rows");
            yield return At(new Vector3(5f, 1.95f, 5.6f), "orchard-3-press-house");
            yield return At(new Vector3(12f, 1.95f, 5f), "orchard-4-inside");
            yield return At(new Vector3(11f, 4.35f, 8f), "orchard-5-loft");
            yield return At(new Vector3(2f, 3.35f, 13f), "orchard-6-top-terrace");
            yield return At(new Vector3(-2f, 3.35f, 25f), "orchard-6b-the-unreaped-rows");
            // Wide, from the south over the water.
            var wide = new GameObject("Wide").AddComponent<Camera>();
            wide.CopyFrom(cam);
            wide.targetTexture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            wide.transform.SetPositionAndRotation(new Vector3(0f, 28f, -20f), Quaternion.Euler(45f, 0f, 0f));
            yield return null;
            Shot("orchard-7-wide", wide);
        }
    }
}
