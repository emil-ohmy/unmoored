using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MyGame.Tests
{
    // The Drowned City's lab (Scenes/Lab_DrownedCity.unity): the Tester's Post spawns any Soured and gives the Apprentice anything; the training
    // Laden never fights back and never dies; the readout counts.
    public class LabTests
    {
        Apprentice apprentice;
        ScriptedInput input;
        TesterPost post;
        LabReadout readout;
        Laden dummy;

        [UnitySetUp]
        public IEnumerator LoadLab()
        {
            Application.targetFrameRate = -1;
            Time.captureDeltaTime = 1f / 60f;
            yield return SceneManager.LoadSceneAsync("Lab_DrownedCity");
            yield return null;
            apprentice = Object.FindAnyObjectByType<Apprentice>();
            apprentice.Input = input = new ScriptedInput();
            post = Object.FindObjectsByType<TesterPost>(FindObjectsSortMode.None).OrderBy(p => p.transform.position.x).First(); // (the west one)
            readout = Object.FindAnyObjectByType<LabReadout>();
            dummy = Object.FindObjectsByType<Laden>(FindObjectsSortMode.None).First(l => l.immortal);
            // (the lab's Drowned spit at anyone in sight of their water: only a test about them lets them)
            if (!NUnit.Framework.TestContext.CurrentContext.Test.Name.Contains("Drowned"))
                foreach (var d in Object.FindObjectsByType<Drowned>(FindObjectsSortMode.None))
                    d.passive = true;
            yield return new WaitForSeconds(0.3f);
        }

        [TearDown]
        public void ResetTime()
        {
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f;
        }

        IEnumerator OpenTheMenu()
        {
            apprentice.Teleport(post.InteractPosition + Vector3.forward * 1f);
            yield return new WaitForSeconds(0.2f);
            yield return null;
            Assert.AreEqual(post, apprentice.Nearby);
            Assert.AreEqual("Test", Object.FindAnyObjectByType<Hud>().PromptText);
            input.PressInteract();
            yield return null;
            Assert.IsInstanceOf<TesterMenu>(apprentice.OpenMenu);
        }

        TesterMenu Menu => (TesterMenu)apprentice.OpenMenu;

        [UnityTest]
        public IEnumerator TheLab_HasItsPosts_AndATrainingLaden()
        {
            Assert.IsNotNull(post, "the Tester's Post");
            Assert.IsNotNull(Object.FindAnyObjectByType<MooringPost>(), "a Mooring post (Rest, fit Charms)");
            Assert.IsTrue(dummy.passive && dummy.immortal);
            Assert.AreEqual(0, Object.FindObjectsByType<LadenArcher>(FindObjectsSortMode.None).Length, "the kinds are templates, switched off");
            Assert.IsTrue(apprentice.Equipment.Owns(Charm.TestersKnot));
            yield return null;
            StringAssert.Contains("LAB", readout.Text);
        }

        [UnityTest]
        public IEnumerator TestersPost_SpawnsSoured_InFrontOfYou_AndClearsThem()
        {
            yield return OpenTheMenu();
            Assert.AreEqual("Spawn", Menu.PageName);
            Menu.Do("How many");
            Menu.Do("Laden");
            yield return null;
            var spawned = post.spawner.Spawned.ToArray();
            Assert.AreEqual(2, spawned.Length, "two, as asked");
            foreach (var s in spawned)
            {
                Assert.IsTrue(s.gameObject.activeInHierarchy);
                Vector3 to = s.transform.position - apprentice.transform.position;
                Assert.Greater(Vector3.Dot(to.normalized, apprentice.transform.forward), 0.5f, "in front of you");
                Assert.Greater(s.transform.position.y, 0.3f, "on the quay, not in the water");
                Assert.Greater(Vector3.Dot(s.transform.forward, -to.normalized), 0.7f, "facing you");
                Assert.IsNotNull(s.droppedLantern);
                Assert.AreNotSame(post.spawner.kinds[0].template.GetComponent<Soured>().droppedLantern, s.droppedLantern, "a lantern of its own");
            }
            Assert.AreNotSame(spawned[0].droppedLantern, spawned[1].droppedLantern);
            foreach (var s in spawned)
                Assert.Greater(Vector3.Distance(s.transform.position, apprentice.transform.position), 5f, "set down a little way off");
            apprentice.OpenMenu.Close();
            yield return new WaitForSeconds(1.5f);
            Assert.IsTrue(spawned.Any(s => s.CurrentState != Soured.State.Idle), "aggressive: they come for you");

            // Opening a menu again: they're set back where they were set down, and leave you be meanwhile.
            var spots = spawned.Select(s => s.transform.position).ToArray();
            yield return new WaitForSeconds(1f);
            yield return OpenTheMenu();
            yield return null;
            for (int i = 0; i < spawned.Length; i++)
            {
                Assert.AreEqual(Soured.State.Idle, spawned[i].CurrentState, "reset");
                Assert.AreEqual(spawned[i].maxHealth, spawned[i].Health);
            }
            yield return new WaitForSeconds(2f);
            Assert.IsTrue(spawned.All(s => s.CurrentState == Soured.State.Idle), "and they leave you be in the menu");
            Assert.AreEqual(apprentice.maxHealth, apprentice.Health);

            Menu.Do("Clear them all");
            yield return null;
            Assert.IsTrue(spawned.All(s => s == null), "cleared");

            Menu.Do("They start"); // idle
            Menu.Do("How many");   // (2 -> 3)
            Menu.Do("How many");   // (3 -> 5)
            Menu.Do("How many");   // (5 -> 1)
            Menu.Do("Laden archer");
            apprentice.OpenMenu.Close();
            var archer = post.spawner.Spawned.Single();
            Assert.IsInstanceOf<LadenArcher>(archer);
            yield return new WaitForSeconds(2f);
            Assert.AreEqual(Soured.State.Idle, archer.CurrentState, "idle: it never notices you");
            Assert.AreEqual(apprentice.maxHealth, apprentice.Health);
        }

        [UnityTest]
        public IEnumerator TestersPost_GivesAnything_AndResets()
        {
            yield return OpenTheMenu();
            Menu.Do("Give the Apprentice...");
            Assert.AreEqual("Give", Menu.PageName);
            Menu.Do(Charm.HespersWick.Name);
            Menu.Do(Charm.EelSkin.Name);
            Menu.Do("A Charm slot");
            Menu.Do("The Hook");
            Menu.Do("500 Obols");
            Menu.Do("Strength...");
            Assert.AreEqual("Strength", Menu.PageName);
            Menu.Do("A Healing charge");
            Menu.Do("A Ferry Nail");
            Menu.Do("A Warm Stone");
            Menu.Do("A Held Breath");
            Menu.Do("The lantern: a level");
            Assert.AreEqual(1, apprentice.Equipment.FerryNails);
            Assert.AreEqual(1, apprentice.Equipment.WarmStones);
            Assert.AreEqual(1, apprentice.Equipment.HeldBreaths);
            Assert.AreEqual(2, Lantern.Level);
            Assert.IsTrue(apprentice.Equipment.Owns(Charm.HespersWick) && apprentice.Equipment.Owns(Charm.EelSkin));
            Assert.AreEqual(2, apprentice.Equipment.charmSlots);
            Assert.IsTrue(apprentice.HasHook);
            Assert.AreEqual(500, apprentice.Obols);
            Assert.AreEqual(4, apprentice.maxHealingCharges);
            Menu.Do("Back");
            Menu.Do("Back");
            Menu.Do("Reset the Apprentice");
            Assert.AreEqual(0, apprentice.Equipment.WarmStones + apprentice.Equipment.HeldBreaths + apprentice.Equipment.FerryNails);
            Assert.AreEqual(1, Lantern.Level);
            Assert.IsFalse(apprentice.Equipment.Owns(Charm.HespersWick));
            Assert.IsTrue(apprentice.Equipment.Owns(Charm.TestersKnot), "the Tester's Knot stays");
            Assert.AreEqual(1, apprentice.Equipment.charmSlots);
            Assert.IsFalse(apprentice.HasHook);
            Assert.AreEqual(0, apprentice.Obols);
            Assert.AreEqual(3, apprentice.maxHealingCharges);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TrainingLaden_NeverDies_NeverFightsBack_AndTheReadoutCounts()
        {
            Vector3 front = dummy.transform.position + dummy.transform.forward * 1.6f;
            apprentice.Teleport(new Vector3(front.x, apprentice.transform.position.y, front.z));
            apprentice.transform.rotation = Quaternion.LookRotation(-dummy.transform.forward);
            yield return new WaitForSeconds(0.2f);
            for (int i = 0; i < 20; i++)
                dummy.TakeHit(new Hit { Damage = 50f, From = apprentice.transform.position, Knockback = Vector3.zero });
            Assert.IsTrue(dummy.IsAlive, "it never dies");
            Assert.AreEqual(20, readout.Landed);
            Assert.AreEqual(50f, readout.LastDamage, 0.01f);
            yield return null;
            Assert.AreEqual(1000f, readout.RecentDamage, 0.01f);
            StringAssert.Contains("Training Laden", readout.Text);

            input.PressLight(); // and a real swing counts too
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(21, readout.Landed);
            yield return new WaitForSeconds(3f);
            Assert.AreEqual(Soured.State.Idle, dummy.CurrentState, "it never fights back");
            Assert.AreEqual(apprentice.maxHealth, apprentice.Health);
        }

        // ---- The Clinger

        Clinger[] SpawnClingers()
        {
            var spawner = post.spawner;
            int kind = System.Array.FindIndex(spawner.kinds, k => k.name == "Clinger");
            Assert.GreaterOrEqual(kind, 0, "the lab can set down Clingers");
            spawner.Count = 2; // (a pair)
            spawner.Spawn(kind, apprentice);
            return spawner.Spawned.OfType<Clinger>().ToArray();
        }

        [UnityTest]
        public IEnumerator Clingers_ComeInPairs_CircleYou_ThenOneLeaps()
        {
            var pair = SpawnClingers();
            Assert.AreEqual(2, pair.Length, "a pair");
            yield return null;
            yield return null;
            Assert.AreSame(pair[1], pair[0].partner, "each the other's other half");
            Assert.AreSame(pair[0], pair[1].partner);
            bool leapt = false, together = false;
            float nearest = 99f;
            for (float t = 0f; t < 8f && !leapt; t += Time.deltaTime)
            {
                foreach (var c in pair)
                    if (c.CurrentState == Soured.State.Telegraph)
                        leapt = true;
                together |= pair.All(c => c.CurrentState == Soured.State.Telegraph);
                if (pair.All(c => c.CurrentState == Soured.State.Chase))
                    nearest = Mathf.Min(nearest, pair.Min(c => Vector3.Distance(c.transform.position, apprentice.transform.position)));
                yield return null;
            }
            Assert.IsTrue(leapt, "one crouches to leap");
            Assert.IsFalse(together, "one at a time: the other watches");
            Assert.Greater(nearest, 1.2f, "they circle at a distance, they don't walk into you");
        }

        [UnityTest]
        public IEnumerator Clinger_ClingsToYourBack_DrainsYou_AndADashThrowsItOff()
        {
            var pair = SpawnClingers();
            pair[1].gameObject.SetActive(false); // (one, for a clean look)
            var clinger = pair[0];
            for (float t = 0f; t < 10f && apprentice.ClungBy == 0; t += Time.deltaTime)
                yield return null; // standing still: it catches you
            Assert.AreEqual(1, apprentice.ClungBy, "it caught hold");
            Assert.IsTrue(clinger.Clinging);
            yield return new WaitForSeconds(0.5f); // (the grab's own jolt passes)
            Vector3 back = apprentice.transform.position - apprentice.transform.forward * clinger.backOffset;
            Assert.Less(Vector3.Distance(new Vector3(clinger.transform.position.x, 0f, clinger.transform.position.z), new Vector3(back.x, 0f, back.z)), 0.1f, "on your back");
            float stamina = apprentice.Stamina, health = apprentice.Health;
            yield return new WaitForSeconds(1f);
            Assert.Less(apprentice.Stamina, stamina - 15f, "your stamina drains away");
            Assert.Less(apprentice.Health, health - 1.5f, "and your health a little");
            yield return new WaitForSeconds(4f);
            Assert.AreEqual(0f, apprentice.Stamina, 0.01f, "none left");
            Assert.Greater(clinger.Whispers, 0, "it whispers");

            input.Move = Vector2.right;
            input.PressDash(); // even spent, a dash throws it off
            yield return null;
            input.Move = Vector2.zero;
            Assert.AreEqual(Apprentice.State.Dashing, apprentice.CurrentState);
            Assert.AreEqual(0, apprentice.ClungBy);
            Assert.IsTrue(clinger.BeingThrown, "flung off");
            yield return new WaitForSeconds(clinger.throwTime + 0.1f);
            Assert.AreEqual(Soured.State.Staggered, clinger.CurrentState, "it lands staggered: your window");
            Assert.Greater(Vector3.Distance(clinger.transform.position, apprentice.transform.position), 1.5f);
            Assert.Greater(clinger.transform.position.y, 0.3f, "on the quay");
        }

        [UnityTest]
        public IEnumerator Clinger_PerfectBlocked_IsKnockedDown()
        {
            var pair = SpawnClingers();
            pair[1].gameObject.SetActive(false);
            var clinger = pair[0];
            for (float t = 0f; t < 10f && clinger.CurrentState != Soured.State.Telegraph; t += Time.deltaTime)
                yield return null;
            Assert.AreEqual(Soured.State.Telegraph, clinger.CurrentState);
            // Raise the block facing it just before it lands (the perfect window).
            float landsIn = clinger.leap.telegraph + clinger.leap.strike * clinger.leap.impactAt - clinger.StateTime;
            yield return new WaitForSeconds(landsIn - 0.12f);
            Vector3 to = clinger.transform.position - apprentice.transform.position;
            to.y = 0f;
            apprentice.transform.rotation = Quaternion.LookRotation(to);
            input.BlockHeld = true;
            yield return new WaitForSeconds(0.3f);
            input.BlockHeld = false;
            Assert.AreEqual(Soured.State.Stunned, clinger.CurrentState, "knocked down, open to a riposte");
            Assert.AreEqual(0, apprentice.ClungBy);
        }

        [UnityTest]
        public IEnumerator Clinger_ItsOtherHalfScattered_Keens_AndTurnsFrantic()
        {
            var pair = SpawnClingers();
            yield return null;
            yield return null;
            pair[0].TakeHit(new Hit { Damage = 1000f, From = apprentice.transform.position });
            yield return new WaitForSeconds(0.2f);
            Assert.IsFalse(pair[0].IsAlive);
            Assert.IsTrue(pair[1].Frantic, "the other keens, and turns frantic");
            Assert.IsFalse(pair[0].Frantic);
        }

        // (a probe: which Clinger sounds actually play in a fight, and at what volume)
        [UnityTest]
        public IEnumerator Clinger_Sounds_Play()
        {
            var played = new System.Collections.Generic.List<string>();
            void OnPlayed(AudioClip clip, AudioSource source)
            {
                if (clip.name.StartsWith("clinger") || clip.name.StartsWith("dash"))
                    played.Add($"{clip.name} vol {source.volume:0.00} at {Vector3.Distance(source.transform.position, apprentice.transform.position):0.0} m");
            }
            Sfx.Played += OnPlayed;
            try
            {
                var pair = SpawnClingers();
                for (float t = 0f; t < 12f && apprentice.ClungBy < 2; t += Time.deltaTime)
                    yield return null;
                yield return new WaitForSeconds(3f);
                input.Move = Vector2.left;
                input.PressDash();
                yield return new WaitForSeconds(0.6f);
                input.Move = Vector2.zero;
                pair[0].TakeHit(new Hit { Damage = 1000f, From = apprentice.transform.position });
                yield return new WaitForSeconds(0.5f);
            }
            finally
            {
                Sfx.Played -= OnPlayed;
            }
            Debug.Log("[probe] Clinger sounds:\n" + string.Join("\n", played));
            foreach (var kind in new[] { "clinger_skitter", "clinger_hiss", "clinger_grab", "clinger_whisper", "clinger_throw", "clinger_keen" })
                Assert.IsTrue(played.Any(p => p.StartsWith(kind)), $"{kind} played");
        }

        // Dying in the lab: everything set down is back where it was set down, whole and calm, when you wake.
        [UnityTest]
        public IEnumerator Dying_ResetsEverySoured()
        {
            var pair = SpawnClingers();
            for (float t = 0f; t < 12f && apprentice.ClungBy == 0; t += Time.deltaTime)
                yield return null;
            Assert.IsTrue(apprentice.TakeTickDamage(10000f));
            Assert.IsFalse(apprentice.IsAlive);
            yield return new WaitForSeconds(apprentice.respawnDelay + 0.2f);
            Assert.IsTrue(apprentice.IsAlive, "woken");
            yield return new WaitForSeconds(1.5f);
            foreach (var c in pair)
            {
                Assert.IsFalse(c.Clinging);
                Assert.AreEqual(c.maxHealth, c.Health);
                Assert.AreEqual(Soured.State.Idle, c.CurrentState, "reset: calm, where it was set down");
            }
            Assert.AreEqual(0, apprentice.ClungBy);
            // Step off the spot: they come again.
            input.Move = Vector2.right;
            yield return new WaitForSeconds(0.6f);
            input.Move = Vector2.zero;
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(pair.Any(c => c.CurrentState != Soured.State.Idle), "then they come for you");
        }

        // Different kinds, one after another, each on a spot of its own; and the ready-made formations.
        [UnityTest]
        public IEnumerator TestersPost_MixesKinds_AndFormations()
        {
            yield return OpenTheMenu();
            Menu.Do("Laden");
            Menu.Do("Clinger");
            Menu.Do("Laden archer");
            Menu.Do("Clinger");
            var all = post.spawner.Spawned.ToArray();
            Assert.AreEqual(4, all.Length);
            for (int i = 0; i < all.Length; i++)
                for (int j = i + 1; j < all.Length; j++)
                    Assert.Greater(Vector3.Distance(all[i].transform.position, all[j].transform.position), 1.5f, "each on a spot of its own");
            Assert.AreEqual(2, all.OfType<Clinger>().Count(), "Clingers one at a time");
            Menu.Do("Clear them all");
            Menu.Do("Formations...");
            Assert.AreEqual("Formations", Menu.PageName);
            Menu.Do("Everything");
            var formed = post.spawner.Spawned.ToArray();
            Assert.AreEqual(4, formed.Length);
            Assert.AreEqual(1, formed.OfType<LadenArcher>().Count());
            Assert.AreEqual(2, formed.OfType<Clinger>().Count());
            Assert.AreEqual(1, formed.Count(s => s.GetType() == typeof(Laden)));
            apprentice.OpenMenu.Close();
            yield return new WaitForSeconds(0.2f);
            var clingers = formed.OfType<Clinger>().ToArray();
            Assert.AreSame(clingers[1], clingers[0].partner, "the two Clingers pair up");
        }

        // ---- The heights

        static float FlatDistance(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        IEnumerator WalkTowards(Vector3 point, float timeout, float within = 0.4f)
        {
            for (float t = 0f; t < timeout && FlatDistance(apprentice.transform.position, point) > within; t += Time.deltaTime)
            {
                Vector3 to = point - apprentice.transform.position;
                input.Move = new Vector2(to.x, to.z).normalized;
                yield return null;
            }
            input.Move = Vector2.zero;
        }

        // The pieces face the way they should: the tower's door south, the stair along its east wall, the walkway's door west.
        [Test]
        public void Tower_IsBuiltTheRightWayRound()
        {
            var tower = Object.FindAnyObjectByType<Storeys>().transform.position;
            Assert.IsTrue(Physics.Raycast(tower + new Vector3(0f, 0.8f, -6f), Vector3.forward, out var door, 12f));
            Assert.Greater(door.point.z, tower.z, "through the south door, into the tower");
            Assert.IsTrue(Physics.Raycast(tower + new Vector3(-2f, 0.8f, -6f), Vector3.forward, out var wall, 12f));
            Assert.Less(wall.point.z, tower.z - 2.9f, "beside it, the south wall");
            Assert.IsTrue(Physics.Raycast(tower + new Vector3(2.6f, 1.9f, 0f), Vector3.down, out var step, 3f));
            Assert.Greater(step.point.y, tower.y + 0.5f, "a step of the stair, along the east wall");
            Assert.IsTrue(Physics.Raycast(tower + new Vector3(-6f, 3.4f, 0f), Vector3.right, out var inside, 12f));
            Assert.Greater(inside.point.x, tower.x - 3f, "from the walkway through the first floor's west door");
        }

        [UnityTest]
        public IEnumerator Heights_UpTheStairs_AlongTheWalkway_IntoTheTower_TheStoreysAboveFade()
        {
            var storeys = Object.FindAnyObjectByType<Storeys>();
            apprentice.Teleport(new Vector3(21.2f, 0.55f, 0.5f));
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(-1, storeys.Level, "outside, the tower is whole");
            yield return WalkTowards(new Vector3(29.5f, 0f, 0.5f), 6f);
            Assert.Greater(apprentice.transform.position.y, 2.7f, "up the stair, on the walkway");
            // The railings hold: push north off the walkway.
            input.Move = Vector2.up;
            yield return new WaitForSeconds(1f);
            input.Move = Vector2.zero;
            Assert.Greater(apprentice.transform.position.y, 2.7f, "the railing holds");
            Assert.Less(apprentice.transform.position.z, 2f);
            yield return WalkTowards(new Vector3(34.5f, 0f, 0.5f), 6f);
            Assert.Greater(apprentice.transform.position.y, 2.7f, "through the door, on the first floor");
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(1, storeys.Level);
            Assert.IsTrue(storeys.Shown(1));
            Assert.IsFalse(storeys.Shown(2), "the roof above is hidden");
            // Down on the ground floor: both storeys above gone.
            apprentice.Teleport(storeys.transform.position + new Vector3(-1f, 0.05f, 0f));
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(0, storeys.Level);
            Assert.IsFalse(storeys.Shown(1));
            Assert.IsFalse(storeys.Shown(2));
            Assert.Greater(Shader.GetGlobalVector("_CutawayCentre").w, 0f, "the cutaway follows the Apprentice");
            Assert.AreEqual(1f, storeys.storeys[0].parts[0].GetComponentInChildren<Renderer>().sharedMaterial.GetFloat("_Cutaway"), "architecture opens up");
            Assert.AreEqual(0f, apprentice.GetComponentInChildren<Renderer>().sharedMaterial.GetFloat("_Cutaway"), "characters never do");
        }

        [UnityTest]
        public IEnumerator Heights_FromTheRoof_TheHookAcross_ThenDropsDown()
        {
            var storeys = Object.FindAnyObjectByType<Storeys>();
            Vector3 roof = storeys.transform.position + new Vector3(2.2f, 4.85f, -0.9f);
            apprentice.GiveHook();
            apprentice.Teleport(roof);
            apprentice.transform.rotation = Quaternion.LookRotation(Vector3.right);
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(storeys.Shown(0) && storeys.Shown(1) && storeys.Shown(2));
            // The chained gap holds you on the roof.
            input.Move = Vector2.right;
            yield return new WaitForSeconds(1f);
            input.Move = Vector2.zero;
            Assert.Greater(apprentice.transform.position.y, 5f, "the chain holds");
            input.HookHeld = true;
            yield return new WaitForSeconds(0.2f);
            Assert.IsNotNull(apprentice.HookTarget, "the high platform's post, across");
            input.HookHeld = false;
            yield return null;
            yield return null;
            Assert.AreEqual(Apprentice.State.Hooking, apprentice.CurrentState);
            for (float t = 0f; t < 3f && apprentice.CurrentState != Apprentice.State.Free; t += Time.deltaTime)
                yield return null;
            yield return new WaitForSeconds(0.1f);
            Assert.Less(FlatDistance(apprentice.transform.position, new Vector3(44.8f, 0f, 0.5f)), 0.6f, "pulled over the railing onto the platform");
            Assert.Greater(apprentice.transform.position.y, 3.1f);
            float health = apprentice.Health;
            // Off its open (east) side onto the low platform, and off that to the ground: no harm.
            yield return WalkTowards(new Vector3(49.5f, 0f, 0.5f), 4f);
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(1.9f, apprentice.transform.position.y, 0.2f, "dropped onto the low platform");
            yield return WalkTowards(new Vector3(53f, 0f, 0.5f), 4f);
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(0.5f, apprentice.transform.position.y, 0.2f, "and down to the ground");
            Assert.AreEqual(health, apprentice.Health, "no falling damage");
            Assert.AreNotEqual(Apprentice.State.Sinking, apprentice.CurrentState);
        }

        // ---- The Unparted (the miniboss)

        Unparted SpawnTheUnparted()
        {
            int f = System.Array.FindIndex(LabSpawner.Formations, x => x.name.StartsWith("The Unparted"));
            Assert.GreaterOrEqual(f, 0, "the lab can set down the Unparted");
            post.spawner.SpawnFormation(f, apprentice);
            return Object.FindObjectsByType<Unparted>(FindObjectsSortMode.None).Single();
        }

        [UnityTest]
        public IEnumerator Unparted_FastRidesHold_IsShieldedThere_LeapsDown_AndClimbsBackUp()
        {
            var u = SpawnTheUnparted();
            var hud = Object.FindAnyObjectByType<Hud>();
            yield return null;
            yield return null;
            Assert.IsTrue(u.fast.Mounted, "Fast rides Hold's back");
            Assert.Less(Vector3.Distance(u.fast.transform.position, u.fast.mount.position), 0.7f, "on Hold's back (its seat, or hopping onto it)");
            Assert.AreEqual(HitResult.Blocked, u.fast.TakeHit(new Hit { Damage = 50f, From = apprentice.transform.position }), "Hold's shoulders take the blow");
            for (float t = 0f; t < 6f && BossFight.Current == null; t += Time.deltaTime)
                yield return null;
            Assert.AreSame(u, BossFight.Current, "the fight: its name plate and bars");
            yield return null;
            Assert.AreEqual("The Unparted", hud.Boss.Name);
            Assert.IsTrue(hud.Boss.Parted, "two bars");
            Assert.AreEqual("Hold", hud.Boss.PartName(0));
            Assert.AreEqual("Fast", hud.Boss.PartName(1));
            // Keep clear of Hold's blows (it wouldn't be a fair look otherwise): it hits hard.
            apprentice.postHitInvulnerability = 1000f;
            apprentice.TakeHit(0f, Vector3.zero);
            for (float t = 0f; t < 10f && u.fast.Mounted; t += Time.deltaTime)
                yield return null;
            Assert.IsFalse(u.fast.Mounted, "it leaps down at you");
            yield return new WaitForSeconds(0.1f);
            Assert.AreEqual(HitResult.Landed, u.fast.TakeHit(new Hit { Damage = 1f, From = apprentice.transform.position }), "down, it can be hit");
            apprentice.postHitInvulnerability = 0.5f;
            // Let it catch hold (or not), throw it off, and it goes back up.
            for (float t = 0f; t < 8f && !u.fast.Clinging && u.fast.Mounted == false && !u.fast.CurrentState.Equals(Soured.State.Recover); t += Time.deltaTime)
                yield return null;
            input.Move = Vector2.left;
            input.PressDash();
            yield return null;
            input.Move = Vector2.zero;
            apprentice.postHitInvulnerability = 1000f;
            apprentice.TakeHit(0f, Vector3.zero);
            for (float t = 0f; t < 12f && !u.fast.Mounted; t += Time.deltaTime)
                yield return null;
            Assert.IsTrue(u.fast.Mounted, "and climbs back up");
        }

        [UnityTest]
        public IEnumerator Unparted_TheHookPullsFastOffHoldsBack()
        {
            var u = SpawnTheUnparted();
            yield return null;
            yield return null;
            Assert.IsTrue(u.fast.Mounted);
            Assert.AreEqual(HookResult.Yanked, u.hold.OnHooked(apprentice), "hooking Hold pulls Fast off");
            Assert.IsFalse(u.fast.Mounted);
            Assert.AreEqual(Soured.State.Yanked, u.fast.CurrentState);
        }

        [UnityTest]
        public IEnumerator Unparted_FastScatteredFirst_HoldHowls_AndGoesBerserk()
        {
            var u = SpawnTheUnparted();
            yield return null;
            yield return null;
            u.hold.OnHooked(apprentice); // (Fast off Hold's back, to be struck)
            yield return new WaitForSeconds(0.8f);
            u.fast.TakeHit(new Hit { Damage = 1000f, From = apprentice.transform.position });
            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(u.fast.IsAlive);
            Assert.IsTrue(u.hold.Berserk, "Hold howls, and goes berserk");
            Assert.AreEqual(Soured.State.Busy, u.hold.CurrentState, "(the howl)");
            Assert.AreEqual(2, u.Phase);
        }

        [UnityTest]
        public IEnumerator Unparted_HoldScatteredFirst_FastKeens_AndWeepsWhereItFell()
        {
            var u = SpawnTheUnparted();
            yield return null;
            yield return null;
            apprentice.postHitInvulnerability = 1000f;
            apprentice.TakeHit(0f, Vector3.zero);
            u.hold.TakeHit(new Hit { Damage = 10000f, From = apprentice.transform.position });
            yield return new WaitForSeconds(0.2f);
            Assert.IsFalse(u.hold.IsAlive);
            Assert.IsTrue(u.fast.Frantic, "Fast keens, and turns frantic");
            Assert.IsTrue(u.fast.Mourning);
            Assert.IsFalse(u.fast.Mounted, "off the back of what's gone");
            bool wept = false;
            for (float t = 0f; t < 8f && !wept; t += Time.deltaTime)
            {
                wept |= u.fast.Weeping;
                yield return null;
            }
            Assert.IsTrue(wept, "and weeps where Hold fell");
        }

        [UnityTest]
        public IEnumerator Unparted_BothScattered_TillDeathDidTheyPart()
        {
            var u = SpawnTheUnparted();
            IBoss farewell = null;
            string sound = "";
            void OnDefeated(IBoss b)
            {
                farewell = b;
                sound = Sfx.Instance.LastPlayed; // (the HUD's own handler, first: the bell)
            }
            BossFight.Defeated += OnDefeated;
            try
            {
                yield return null;
                yield return null;
                u.hold.OnHooked(apprentice);
                yield return new WaitForSeconds(0.8f);
                int obols = apprentice.Obols;
                int fallen = BossFall.Fallen;
                u.fast.TakeHit(new Hit { Damage = 1000f, From = apprentice.transform.position });
                Assert.AreEqual(fallen, BossFall.Fallen, "the first of them: no moment yet");
                u.hold.TakeHit(new Hit { Damage = 10000f, From = apprentice.transform.position });
                Assert.AreEqual(fallen + 1, BossFall.Fallen, "the last of them: its fall made a moment of");
                yield return new WaitForSeconds(0.5f);
                Assert.AreSame(u, farewell, "its farewell, once both are scattered");
                Assert.AreEqual("boss_farewell", sound, "a great bell as its farewell comes up");
                Assert.AreEqual(2, u.Music.Length, "its own music, a track per phase");
                Assert.AreEqual(obols + u.hold.obols + u.fast.obols, apprentice.Obols);
                yield return new WaitForSeconds(0.2f);
                var hud = Object.FindAnyObjectByType<Hud>();
                StringAssert.Contains("TILL DEATH", hud.Farewell.Text.text);
            }
            finally
            {
                BossFight.Defeated -= OnDefeated;
            }
        }

        // Left behind (the fight broken off, Hold wandered off from where Fast was set down): Fast goes back to Hold and
        // climbs up, it doesn't walk off to where it first stood.
        [UnityTest]
        public IEnumerator Unparted_FastBelongsWithHold_NotWhereItWasSetDown()
        {
            var u = SpawnTheUnparted();
            apprentice.postHitInvulnerability = 1000f;
            apprentice.TakeHit(0f, Vector3.zero);
            for (float t = 0f; t < 10f && u.fast.Mounted; t += Time.deltaTime)
                yield return null;
            Assert.IsFalse(u.fast.Mounted, "(it leapt down)");
            // Hold, dragged far from where they were set down; then the Apprentice goes away.
            Vector3 far = u.hold.transform.position + Vector3.right * 18f;
            u.hold.SetSpawn(far, u.hold.transform.rotation);
            u.hold.Reform();
            apprentice.Teleport(new Vector3(-6f, 0.55f, -2f));
            for (float t = 0f; t < 15f && !u.fast.Mounted; t += Time.deltaTime)
            {
                if (u.fast.Clinging)
                {
                    input.Move = Vector2.left;
                    input.PressDash();
                }
                yield return null;
            }
            input.Move = Vector2.zero;
            Assert.IsTrue(u.fast.Mounted, "back up on Hold");
            Assert.Less(Vector3.Distance(u.fast.transform.position, u.fast.mount.position), 0.7f, "on Hold's back (its seat, or hopping onto it)");
        }

        // ---- Finding the way (heights, stop 2)

        Soured SetDown(string kind, Vector3 at)
        {
            var s = post.spawner.SpawnAt(kind, at, apprentice.transform.position - at);
            s.aggroRadius = 30f;
            s.leashRadius = 40f;
            return s;
        }

        [UnityTest]
        public IEnumerator Pathfinding_ALadenClimbsTheStair_UpToTheWalkway()
        {
            apprentice.postHitInvulnerability = 1000f;
            apprentice.TakeHit(0f, Vector3.zero);
            apprentice.Teleport(new Vector3(29.5f, 2.95f, 0.5f));
            var laden = SetDown("Laden", new Vector3(19f, 0.55f, -3f));
            float highest = 0f;
            for (float t = 0f; t < 20f && highest < 2.7f; t += Time.deltaTime)
            {
                highest = Mathf.Max(highest, laden.transform.position.y);
                yield return null;
            }
            Assert.Greater(highest, 2.7f, "up the stair, onto the walkway after you");
        }

        [UnityTest]
        public IEnumerator Pathfinding_UpTheTowersStair_AndNoBlowThroughTheFloor()
        {
            var tower = Object.FindAnyObjectByType<Storeys>().transform.position;
            apprentice.postHitInvulnerability = 1000f;
            apprentice.TakeHit(0f, Vector3.zero);
            apprentice.Teleport(tower + new Vector3(-1.5f, 2.45f, 1.2f)); // the first floor
            var laden = SetDown("Laden", tower + new Vector3(-1.5f, 0.1f, 1.2f)); // right under the Apprentice
            for (float t = 0f; t < 1.5f; t += Time.deltaTime)
            {
                Assert.AreNotEqual(Soured.State.Telegraph, laden.CurrentState, "no blow up through the floor");
                yield return null;
            }
            // Then it finds the stair up (along the east wall) and comes across the first floor to you.
            float highest = 0f;
            for (float t = 0f; t < 20f && highest < tower.y + 2.2f; t += Time.deltaTime)
            {
                highest = Mathf.Max(highest, laden.transform.position.y);
                yield return null;
            }
            Assert.Greater(highest, tower.y + 2.2f, $"it finds the stair up (it got to {laden.transform.position}, {laden.CurrentState})");
            for (float t = 0f; t < 6f && FlatDistance(laden.transform.position, apprentice.transform.position) > 2.5f; t += Time.deltaTime)
                yield return null;
            Assert.Less(FlatDistance(laden.transform.position, apprentice.transform.position), 2.5f, "and across the first floor to you");
        }

        // ---- the Stroke (hold the heavy attack, let go: a wave)

        // Holds the heavy attack (with the Stroke: drawn back, and charged) for `seconds`, then lets go.
        IEnumerator Stroke(float seconds = 1.5f)
        {
            input.HeavyHeld = true;
            input.PressHeavy();
            yield return new WaitForSeconds(seconds);
            input.HeavyHeld = false;
        }

        // Faces the Apprentice towards `at`.
        void Face(Vector3 at)
        {
            Vector3 to = at - apprentice.transform.position;
            to.y = 0f;
            apprentice.transform.rotation = Quaternion.LookRotation(to);
        }

        // A tap is still just the heavy attack, with the Stroke or without; held, without it, it's the heavy too.
        [UnityTest]
        public IEnumerator TheStroke_ATapIsStillTheHeavy_AndWithoutItHoldingIsToo()
        {
            input.PressHeavy();
            yield return null;
            Assert.AreEqual(Apprentice.State.Attacking, apprentice.CurrentState);
            input.HeavyHeld = true; // (held, without the Stroke)
            yield return new WaitForSeconds(1.3f);
            input.HeavyHeld = false;
            Assert.AreEqual(Apprentice.State.Free, apprentice.CurrentState, "just the heavy");
            Assert.IsNull(apprentice.LastWave);
            apprentice.GiveStroke();
            yield return new WaitForSeconds(0.6f);
            input.PressHeavy(); // a tap
            for (float t = 0f; t < 1.2f; t += Time.deltaTime)
            {
                Assert.AreNotEqual(Apprentice.State.Stroke, apprentice.CurrentState);
                yield return null;
            }
            Assert.IsNull(apprentice.LastWave, "a tap: no wave");
        }

        // Held: the heavy's windup turns into the Stroke drawn back, rooted, the blade's sheen brightening; let go, it
        // sweeps and the wave rolls out ahead, about 4 m, then it's spent. Its stamina is the heavy's and more.
        [UnityTest]
        public IEnumerator TheStroke_Held_DrawsBack_LetGo_TheWaveRollsOut()
        {
            apprentice.GiveStroke();
            yield return new WaitForSeconds(1f);
            Vector3 start = apprentice.transform.position;
            float stamina = apprentice.Stamina;
            input.HeavyHeld = true;
            input.PressHeavy();
            for (float t = 0f; t < 0.6f && !apprentice.DrawingStroke; t += Time.deltaTime)
                yield return null;
            Assert.IsTrue(apprentice.DrawingStroke, "drawn back");
            yield return new WaitForSeconds(apprentice.strokeCharge + 0.2f);
            Assert.IsTrue(apprentice.DrawingStroke, "held as long as it's held");
            Assert.Greater(apprentice.StrokeDrawn, 0.9f);
            Assert.Greater(apprentice.GetComponent<OarLook>().Sheen, 1.4f, "the blade brightening");
            Assert.Less(Vector3.Distance(start, apprentice.transform.position), 0.3f, "rooted");
            input.HeavyHeld = false;
            for (float t = 0f; t < 0.5f && apprentice.LastWave == null; t += Time.deltaTime)
                yield return null;
            var wave = apprentice.LastWave;
            Assert.IsNotNull(wave, "the wave");
            Assert.AreEqual(apprentice.strokeStaminaCost, stamina - apprentice.Stamina, 1f, "its stamina, all of it");
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(apprentice.strokeReach, wave != null ? wave.Radius : apprentice.strokeReach, 0.6f, "about four metres out");
            yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(wave == null, "spent, and gone");
            Assert.AreEqual(Apprentice.State.Free, apprentice.CurrentState);
        }

        // Drawn long enough, it's charged: a ring and a flash on the blade (its sheen flaring, then shimmering while held).
        [UnityTest]
        public IEnumerator TheStroke_Charged_TheBladeFlashes()
        {
            apprentice.GiveStroke();
            yield return new WaitForSeconds(1f);
            input.HeavyHeld = true;
            input.PressHeavy();
            for (float t = 0f; t < 0.6f && !apprentice.DrawingStroke; t += Time.deltaTime)
                yield return null;
            yield return new WaitForSeconds(apprentice.strokeCharge * 0.6f);
            Assert.IsFalse(apprentice.StrokeCharged, "not yet");
            for (float t = 0f; t < 1f && !apprentice.StrokeCharged; t += Time.deltaTime)
                yield return null;
            Assert.IsTrue(apprentice.StrokeCharged, "charged");
            yield return null;
            Assert.Greater(apprentice.GetComponent<OarLook>().Sheen, 3.5f, "the blade flashing");
            yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(apprentice.DrawingStroke && apprentice.StrokeCharged, "held, charged");
            input.HeavyHeld = false;
            yield return new WaitForSeconds(0.4f);
            Assert.IsNotNull(apprentice.LastWave);
        }

        // Let go before it's charged: just the heavy, coming down at once (no wave, no more stamina).
        [UnityTest]
        public IEnumerator TheStroke_LetGoTooSoon_IsJustTheHeavy()
        {
            apprentice.GiveStroke();
            yield return new WaitForSeconds(1f);
            float stamina = apprentice.Stamina;
            yield return Stroke(apprentice.strokeHold + 0.2f);
            yield return null;
            Assert.AreEqual(Apprentice.State.Attacking, apprentice.CurrentState, "the heavy");
            Assert.AreEqual(apprentice.heavyStaminaCost, stamina - apprentice.Stamina, 1f, "a heavy's stamina");
            yield return new WaitForSeconds(1f);
            Assert.IsNull(apprentice.LastWave, "no wave");
        }

        // The blade itself, sweeping through a Soured in reach, strikes harder than a heavy (and the wave behind it too).
        [UnityTest]
        public IEnumerator TheStroke_TheBladeConnects_HarderThanAHeavy()
        {
            apprentice.GiveStroke();
            var laden = SetDown("Laden", apprentice.transform.position + apprentice.transform.forward * 1.6f);
            laden.passive = true;
            yield return new WaitForSeconds(0.5f);
            laden.transform.rotation = Quaternion.LookRotation(laden.transform.position - apprentice.transform.position); // (its back to you: no guard)
            Face(laden.transform.position);
            float health = laden.Health;
            yield return Stroke();
            yield return new WaitForSeconds(0.4f);
            float dealt = health - laden.Health;
            Assert.Greater(dealt, apprentice.heavyDamage * apprentice.DamageFactor + 1f, "more than a heavy");
            Assert.AreEqual((apprentice.strokeBladeDamage + apprentice.strokeDamage) * apprentice.DamageFactor, dealt, 1f, "the blade, and the wave");
        }

        // A dash lets the draw go, unspent.
        [UnityTest]
        public IEnumerator TheStroke_ADashLetsTheDrawGo()
        {
            apprentice.GiveStroke();
            yield return new WaitForSeconds(1f);
            input.HeavyHeld = true;
            input.PressHeavy();
            yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(apprentice.DrawingStroke);
            float stamina = apprentice.Stamina;
            input.PressDash();
            yield return null;
            yield return null;
            Assert.AreEqual(Apprentice.State.Dashing, apprentice.CurrentState);
            input.HeavyHeld = false;
            yield return new WaitForSeconds(0.6f);
            Assert.IsNull(apprentice.LastWave, "no wave");
            Assert.Less(stamina - apprentice.Stamina, apprentice.DashCost + 0.5f, "only the dash spent");
        }

        // The wave goes through a Laden's guard (water, not an edge): it's shoved back and staggered, with little harm.
        [UnityTest]
        public IEnumerator TheStroke_ThroughALadensGuard_ShovesAndStaggersIt_LittleHarm()
        {
            apprentice.GiveStroke();
            var laden = SetDown("Laden", apprentice.transform.position + apprentice.transform.forward * 2.6f);
            laden.passive = true;
            laden.transform.rotation = Quaternion.LookRotation(apprentice.transform.position - laden.transform.position); // (its heap to you)
            yield return new WaitForSeconds(0.5f);
            Face(laden.transform.position);
            Vector3 was = laden.transform.position;
            float health = laden.Health;
            yield return Stroke();
            for (float t = 0f; t < 0.6f && laden.CurrentState != Soured.State.Staggered; t += Time.deltaTime)
                yield return null;
            Assert.AreEqual(Soured.State.Staggered, laden.CurrentState, "staggered, its guard no help");
            yield return new WaitForSeconds(0.4f);
            Assert.Greater(Vector3.Distance(was, laden.transform.position), 1.2f, "shoved back");
            Assert.That(health - laden.Health, Is.InRange(0.1f, 12f), "little harm");
        }

        // Shoved off the quay's edge, a Soured goes into the water (and under).
        [UnityTest]
        public IEnumerator TheStroke_ShovesASouredOffTheEdge_IntoTheWater()
        {
            apprentice.GiveStroke();
            // The lab's quays end a little past z 5.5 (a raised lip), water beyond.
            apprentice.Teleport(new Vector3(5f, 0.6f, 2.2f));
            var laden = SetDown("Laden", new Vector3(5f, 0.6f, 4.7f));
            laden.passive = true;
            yield return new WaitForSeconds(0.5f);
            Face(laden.transform.position);
            yield return Stroke();
            for (float t = 0f; t < 3f && !laden.Drowned; t += Time.deltaTime)
                yield return null;
            Assert.IsTrue(laden.Drowned, "into the water");
        }

        // A Clinger on your back is thrown off as the oar sweeps round (and one leaping at you is knocked out of the air).
        [UnityTest]
        public IEnumerator TheStroke_ThrowsOffAClinger()
        {
            apprentice.GiveStroke();
            var pair = SpawnClingers();
            pair[1].gameObject.SetActive(false);
            var clinger = pair[0];
            for (float t = 0f; t < 10f && apprentice.ClungBy == 0; t += Time.deltaTime)
                yield return null;
            Assert.AreEqual(1, apprentice.ClungBy, "it caught hold");
            yield return new WaitForSeconds(0.5f); // (the grab's own jolt passes)
            yield return Stroke();
            for (float t = 0f; t < 0.5f && apprentice.ClungBy > 0; t += Time.deltaTime)
                yield return null;
            Assert.AreEqual(0, apprentice.ClungBy, "thrown off");
            Assert.IsTrue(clinger.BeingThrown || clinger.CurrentState == Soured.State.Staggered);
        }

        [UnityTest]
        public IEnumerator Pathfinding_NoSouredWalksOffALedge()
        {
            apprentice.postHitInvulnerability = 1000f;
            apprentice.TakeHit(0f, Vector3.zero);
            apprentice.Teleport(new Vector3(53f, 0.55f, 3.5f)); // on the ground, beside the low platform
            var laden = SetDown("Laden", new Vector3(49.5f, 1.95f, 0.5f)); // on the low platform (1.4 m up)
            var clinger = SetDown("Clinger", new Vector3(50.5f, 1.95f, -0.5f));
            yield return new WaitForSeconds(6f);
            Assert.Greater(laden.transform.position.y, 1.7f, "the Laden keeps to the platform");
            Assert.Greater(clinger.transform.position.y, 1.7f, "and so does the Clinger");
            Assert.AreEqual(apprentice.maxHealth, apprentice.Health, "and neither strikes down at you");
        }

        [UnityTest]
        public IEnumerator Pathfinding_ArchersAimUpAtAWalkway()
        {
            apprentice.Teleport(new Vector3(27.5f, 2.95f, 0.5f));
            var archer = (LadenArcher)SetDown("Laden archer", new Vector3(18.5f, 0.55f, 0.5f));
            archer.volleyChance = 0f;
            Defence? result = null;
            float rise = 0f;
            void OnResolved(Arrow a, Defence d) => result = d;
            Arrow.Resolved += OnResolved;
            try
            {
                for (float t = 0f; t < 10f && result == null; t += Time.deltaTime)
                {
                    foreach (var a in Object.FindObjectsByType<Arrow>(FindObjectsSortMode.None))
                        rise = Mathf.Max(rise, a.Velocity.y);
                    yield return null;
                }
            }
            finally
            {
                Arrow.Resolved -= OnResolved;
            }
            Assert.Greater(rise, 0.5f, "its arrows fly up at the walkway");
            Assert.AreEqual(Defence.Taken, result, "and hit you up there");
        }

        // A Laden set down by the west post follows you all the way east to the tower; and there's a post by the heights.
        [UnityTest]
        public IEnumerator Lab_SouredFollowYouAllTheWayToTheHeights()
        {
            var posts = Object.FindObjectsByType<TesterPost>(FindObjectsSortMode.None);
            Assert.AreEqual(2, posts.Length, "a second Tester's Post");
            Assert.IsTrue(posts.Any(p => p.transform.position.x > 18f && p.transform.position.x < 24f), "by the heights");
            apprentice.postHitInvulnerability = 1000f;
            apprentice.TakeHit(0f, Vector3.zero);
            apprentice.Teleport(new Vector3(-1f, 0.55f, -1f));
            apprentice.transform.rotation = Quaternion.LookRotation(Vector3.right);
            yield return null;
            post.spawner.Spawn(System.Array.FindIndex(post.spawner.kinds, k => k.name == "Laden"), apprentice);
            var laden = post.spawner.Spawned.Last();
            yield return new WaitForSeconds(1.5f);
            Assert.AreNotEqual(Soured.State.Idle, laden.CurrentState, "(it's seen you)");
            var tower = Object.FindAnyObjectByType<Storeys>().transform.position;
            yield return WalkTowards(new Vector3(tower.x, 0f, tower.z - 5f), 12f);
            yield return WalkTowards(new Vector3(tower.x, 0f, tower.z), 3f); // in through the south door
            float nearest = 99f;
            for (float t = 0f; t < 25f && nearest > 2.5f; t += Time.deltaTime)
            {
                nearest = Mathf.Min(nearest, FlatDistance(laden.transform.position, apprentice.transform.position));
                yield return null;
            }
            Assert.Less(nearest, 2.5f, $"it followed you all the way into the tower (it got to {laden.transform.position}, {laden.CurrentState}; you're at {apprentice.transform.position})");
        }


        // The oar is strengthened a step at a time, with Ferry Nails at a Mooring post (1, 2, 3, then 4 of them): no
        // step skipped however many are carried. Each step hits harder, and looks it.
        [UnityTest]
        public IEnumerator TheOar_IsStrengthenedInOrder_AtAMooringPost()
        {
            var e = apprentice.Equipment;
            var look = apprentice.GetComponent<OarLook>();
            Assert.AreEqual(0, e.OarStep);
            Assert.AreEqual("The Apprentice's Oar", e.Oar.Name);
            Assert.IsFalse(e.Strengthen(), "nothing to strengthen it with");
            var materials = apprentice.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterials;
            for (int step = 0; step <= Oar.Last; step++)
                Assert.Greater(look.SlotsOf(step), 0, $"the model carries the oar at step {step}");
            Assert.IsTrue(materials.Any(m => m.name.StartsWith("Oar4Gold_Metal")), "the Ferryman's is gold");
            void AssertShows(int step)
            {
                Assert.AreEqual(step, look.ShownStep);
                for (int i = 0; i < materials.Length; i++)
                    if (OarLook.StepOf(materials[i]) >= 0)
                        Assert.AreEqual(OarLook.StepOf(materials[i]) == step, look.Showing(i), $"{materials[i].name} at step {step}");
            }
            AssertShows(0);
            float plain = apprentice.DamageFactor;

            for (int i = 0; i < 4; i++)
                apprentice.AddFerryNail();
            var mooring = Object.FindAnyObjectByType<MooringPost>();
            apprentice.Teleport(mooring.RespawnPoint);
            yield return new WaitForSeconds(0.2f);
            input.PressInteract();
            yield return null;
            Assert.IsInstanceOf<PostMenu>(apprentice.OpenMenu, "Resting opens the post's menu: the oar's row first");
            Assert.AreEqual(0, apprentice.OpenMenu.Selected);
            StringAssert.Contains("Strengthen", apprentice.OpenMenu.Rows[0].Label);
            // Strengthening is a moment of its own: the menu closes, the oar is held up high, and it changes at the top.
            string found = null;
            apprentice.Found += (title, hint) => found = title;
            input.PressInteract();
            yield return null;
            Assert.AreEqual(Apprentice.State.Ceremony, apprentice.CurrentState, "held up, as a key item is");
            Assert.IsNull(apprentice.OpenMenu, "the post's menu closes for it");
            Assert.AreEqual(0, e.OarStep, "not yet: it changes in the flash at the top");
            yield return new WaitForSeconds(apprentice.ceremonyDuration * 0.5f);
            Assert.AreEqual(1, e.OarStep, "one step (one nail)");
            Assert.AreEqual(3, e.FerryNails);
            AssertShows(1);
            Assert.AreEqual("The Oarhand's Oar", found, "and its name");
            yield return new WaitForSeconds(apprentice.ceremonyDuration * 0.5f + 0.2f);
            Assert.AreEqual(Apprentice.State.Free, apprentice.CurrentState);

            IEnumerator StrengthenAtThePost()
            {
                input.PressInteract(); // (Rest again)
                yield return null;
                Assert.IsInstanceOf<PostMenu>(apprentice.OpenMenu);
                input.PressInteract();
                yield return null;
                yield return new WaitForSeconds(apprentice.ceremonyDuration + 0.2f);
            }
            yield return StrengthenAtThePost();
            Assert.AreEqual(2, e.OarStep, "the next (two nails)");
            Assert.AreEqual(1, e.FerryNails);
            yield return StrengthenAtThePost();
            Assert.AreEqual(2, e.OarStep, "three nails for the next: one isn't enough");
            Assert.IsInstanceOf<PostMenu>(apprentice.OpenMenu, "(the menu stays, and says so)");
            Assert.AreEqual("The Boatman's Oar", e.Oar.Name);
            Assert.AreEqual(plain * Oar.Steps[2].Strength, apprentice.DamageFactor, 0.001f, "it hits harder");
            AssertShows(2);
            input.PressDash();
            yield return null;

            for (int i = 0; i < 7; i++)
                apprentice.AddFerryNail();
            Assert.IsTrue(e.Strengthen() && e.Strengthen() && !e.Strengthen(), "the last two steps, then no more");
            Assert.AreEqual(Oar.Last, e.OarStep);
            Assert.AreEqual("The Ferryman's Oar", e.Oar.Name);
            Assert.AreEqual(1, e.FerryNails);
            AssertShows(Oar.Last);
        }

        // The pieces that make the Apprentice stronger for good lie about the heights: taken with a stoop, each adds
        // what it's for; resetting the Apprentice puts them back.
        [UnityTest]
        public IEnumerator UpgradePieces_AreFound_AndMakeTheApprenticeStronger()
        {
            var pieces = Object.FindObjectsByType<UpgradePiece>(FindObjectsSortMode.None);
            Assert.AreEqual(4, pieces.Count(p => p.kind == UpgradePiece.Kind.FerryNail), "three about the heights, one in the row");
            Assert.AreEqual(2, pieces.Count(p => p.kind == UpgradePiece.Kind.WarmStone));
            Assert.AreEqual(2, pieces.Count(p => p.kind == UpgradePiece.Kind.HeldBreath));
            float health = apprentice.maxHealth, stamina = apprentice.maxStamina;
            var hud = Object.FindAnyObjectByType<Hud>();
            float healthBar = hud.HealthBarWidth, staminaBar = hud.StaminaBarWidth;
            foreach (var kind in new[] { UpgradePiece.Kind.FerryNail, UpgradePiece.Kind.WarmStone, UpgradePiece.Kind.HeldBreath })
            {
                var piece = pieces.First(p => p.kind == kind);
                apprentice.Teleport(piece.transform.position + Vector3.right * 0.6f);
                yield return new WaitForSeconds(0.2f);
                yield return null;
                Assert.AreEqual(piece, apprentice.Nearby, $"the {kind} is right there");
                input.PressInteract();
                yield return null;
                Assert.AreEqual(Apprentice.State.PickingUp, apprentice.CurrentState, "it stoops to pick it up");
                yield return new WaitForSeconds(0.8f);
                Assert.IsTrue(piece.Taken && !piece.gameObject.activeSelf);
            }
            Assert.AreEqual(1, apprentice.Equipment.FerryNails);
            Assert.AreEqual(health + apprentice.healthPerStone, apprentice.maxHealth, "a Warm Stone: more health");
            Assert.AreEqual(stamina + apprentice.staminaPerBreath, apprentice.maxStamina, "a Held Breath: more stamina");
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(healthBar * apprentice.maxHealth / health, hud.HealthBarWidth, 1f, "the health bar grows with it");
            Assert.AreEqual(staminaBar * apprentice.maxStamina / stamina, hud.StaminaBarWidth, 1f, "and the stamina bar");

            yield return OpenTheMenu();
            Menu.Do("Reset the Apprentice");
            Assert.AreEqual(health, apprentice.maxHealth);
            Assert.AreEqual(stamina, apprentice.maxStamina);
            Assert.IsTrue(Object.FindObjectsByType<UpgradePiece>(FindObjectsSortMode.None).Length == pieces.Length, "back where they were");
        }

        // Every kind of find lies in a row on the quay; taken, they come back when the Apprentice is reset.
        [UnityTest]
        public IEnumerator EveryKindOfFind_LiesInARow_AndComesBack()
        {
            yield return null;
            var finds = Object.FindAnyObjectByType<LabFinds>();
            Assert.IsNotNull(finds);
            foreach (UpgradePiece.Kind kind in System.Enum.GetValues(typeof(UpgradePiece.Kind)))
                Assert.IsTrue(Object.FindObjectsByType<UpgradePiece>(FindObjectsSortMode.None).Any(p => p.kind == kind), $"a {kind}");
            foreach (var p in Provision.All)
                Assert.IsTrue(Object.FindObjectsByType<ProvisionFind>(FindObjectsSortMode.None).Any(f => f.Kind == p), p.Name);
            Assert.IsNotNull(Object.FindAnyObjectByType<ChargePiece>(), "an Ember");
            Assert.IsNotNull(Object.FindAnyObjectByType<ObolCache>(), "an Obol Cache");
            Assert.IsNotNull(Object.FindAnyObjectByType<Keepsake>(), "a keepsake");
            var hook = Object.FindAnyObjectByType<FerrymansHook>();
            Assert.IsTrue(hook != null && hook.Available, "and the Hook, to take");

            // Take the Hook and the Ember, then reset: both are back.
            apprentice.Teleport(hook.transform.position + Vector3.right * 0.6f);
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(hook, apprentice.Nearby);
            input.PressInteract();
            yield return new WaitForSeconds(apprentice.ceremonyDuration + 0.3f);
            Assert.IsTrue(apprentice.HasHook, "the Hook, held up");
            var ember = Object.FindAnyObjectByType<ChargePiece>();
            apprentice.Teleport(ember.transform.position + Vector3.right * 0.6f);
            yield return new WaitForSeconds(0.3f);
            input.PressInteract();
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(ember.Taken);
            yield return OpenTheMenu();
            Menu.Do("Reset the Apprentice");
            yield return null;
            Assert.IsFalse(apprentice.HasHook);
            Assert.IsFalse(ember.Taken, "the Ember's back");
            var again = Object.FindObjectsByType<FerrymansHook>(FindObjectsSortMode.None).FirstOrDefault(h => h.Available);
            Assert.IsNotNull(again, "and the Hook");
        }

        // Nothing is reached through a floor: under the Held Breath, on the storey below, it's neither in reach nor
        // shining (its storey, lamps and all, is hidden). And thin posts never open up for the camera, as walls do.
        [UnityTest]
        public IEnumerator ThingsOnAnotherStorey_AreOutOfReach_AndThinPostsStayWhole()
        {
            var breath = Object.FindObjectsByType<UpgradePiece>(FindObjectsSortMode.None)
                .Where(p => p.kind == UpgradePiece.Kind.HeldBreath).OrderByDescending(p => p.transform.position.y).First(); // (the tower's, upstairs)
            var below = breath.transform.position;
            below.y = Object.FindAnyObjectByType<Storeys>().transform.position.y + 0.08f; // the ground floor's flagstones
            apprentice.Teleport(below);
            yield return new WaitForSeconds(0.5f);
            Assert.AreNotEqual(breath, apprentice.Nearby, "not through the ceiling");
            Assert.IsFalse(breath.GetComponentInChildren<Light>().enabled, "and its glow is hidden with its storey");
            apprentice.Teleport(breath.transform.position + Vector3.right * 0.6f);
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(breath, apprentice.Nearby, "on its own floor, it's there");

            foreach (var post in Object.FindObjectsByType<MooringPost>(FindObjectsSortMode.None))
                foreach (var r in post.GetComponentsInChildren<Renderer>())
                    foreach (var m in r.sharedMaterials)
                        if (m != null && m.HasProperty("_Cutaway"))
                            Assert.AreEqual(0f, m.GetFloat("_Cutaway"), $"{m.name}: a post isn't cut away");
        }

        // The Satchel: each Provision up to its cap. The belt: four slots; the first of a kind goes in the first empty
        // one; putting one in another's slot swaps them; left/right pass the empty slots by.
        [Test]
        public void TheSatchel_HoldsUpToACap_AndFillsTheBelt()
        {
            var e = apprentice.Equipment;
            Assert.AreEqual(Provision.PitchPot.Cap, e.AddProvision(Provision.PitchPot, 99), "no more than it takes");
            Assert.AreEqual(0, e.AddProvision(Provision.PitchPot), "full");
            e.AddProvision(Provision.WarmBroth);
            CollectionAssert.AreEqual(new[] { Provision.PitchPot, Provision.WarmBroth, null, null }, e.Belt);
            Assert.AreEqual(Provision.PitchPot, e.Chosen);
            e.SetBeltSlot(3, Provision.PitchPot);
            CollectionAssert.AreEqual(new[] { null, Provision.WarmBroth, null, Provision.PitchPot }, e.Belt, "moved (into an empty slot)");
            e.SetBeltSlot(1, Provision.PitchPot);
            CollectionAssert.AreEqual(new[] { null, Provision.PitchPot, null, Provision.WarmBroth }, e.Belt, "swapped");
            e.ChooseOnBelt(1);
            Assert.AreEqual(3, e.BeltChosen, "past the empty slot");
            e.ChooseOnBelt(1);
            Assert.AreEqual(1, e.BeltChosen, "round again, past the empty first");
            e.SetBeltSlot(1, null);
            Assert.AreEqual(Provision.WarmBroth, e.Chosen, "an emptied slot passes the choice on");
            Assert.IsTrue(e.SpendProvision(Provision.WarmBroth));
            Assert.IsFalse(e.SpendProvision(Provision.WarmBroth), "none left");
            Assert.AreEqual(Provision.WarmBroth, e.Chosen, "an empty one stays in its slot");
        }

        IEnumerator UseFromTheBelt(Provision p)
        {
            apprentice.Equipment.AddProvision(p);
            for (int i = 0; i < Provision.BeltSlots && apprentice.Equipment.Chosen != p; i++)
            {
                input.StepBelt(1); // (right along the belt)
                yield return null;
            }
            Assert.AreEqual(p, apprentice.Equipment.Chosen);
            input.PressBeltUse();
            yield return null;
            Assert.AreEqual(Apprentice.State.Provision, apprentice.CurrentState, $"{p.Name}: used");
        }

        // A Pitch Pot, thrown at a Laden behind its heap: fire goes round a guard. It burns, and burns on a while.
        [UnityTest]
        public IEnumerator APitchPot_BurnsASouredThroughItsGuard()
        {
            float burnt = 0f;
            System.Action<Soured, HitResult, float> count = (s, r, d) => { if (s == dummy) burnt += d; };
            Soured.Struck += count;
            try
            {
                apprentice.Teleport(dummy.transform.position - dummy.transform.forward * 4f);
                apprentice.transform.rotation = Quaternion.LookRotation(dummy.transform.position - apprentice.transform.position);
                yield return new WaitForSeconds(0.2f);
                PitchPot pot = null;
                apprentice.Threw += p => pot = p;
                yield return UseFromTheBelt(Provision.PitchPot);
                yield return new WaitForSeconds(1.2f);
                Assert.IsNotNull(pot, "thrown");
                Assert.IsTrue(pot.Burst, "and burst");
                Assert.IsTrue(dummy.GetComponent<Burning>() != null && dummy.GetComponent<Burning>().Alight, "the Laden's alight");
                float soon = burnt;
                Assert.Greater(soon, 0f, "and hurt, heap or no heap");
                yield return new WaitForSeconds(2f);
                Assert.Greater(burnt, soon, "and burning on");
                Assert.Greater(Effects.Instance.LiveFlames, 0, "flames to see");
            }
            finally { Soured.Struck -= count; }
        }

        // Ember Tar: for a while the oar hits harder, and what it hits catches fire.
        [UnityTest]
        public IEnumerator EmberTar_MakesTheOarBurn()
        {
            float plain = apprentice.DamageFactor;
            yield return UseFromTheBelt(Provision.EmberTar);
            yield return new WaitForSeconds(apprentice.drinkTime + 0.1f);
            Assert.IsTrue(apprentice.Tarred);
            Assert.AreEqual(plain * apprentice.tarDamage, apprentice.DamageFactor, 0.001f);
            apprentice.Teleport(dummy.transform.position + dummy.transform.forward * 1.2f);
            apprentice.transform.rotation = Quaternion.LookRotation(dummy.transform.position - apprentice.transform.position);
            yield return new WaitForSeconds(0.2f);
            input.PressLight();
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(dummy.GetComponent<Burning>() != null && dummy.GetComponent<Burning>().Alight, "it caught");
        }

        // Warm Broth: health mended slowly, a little at a time.
        [UnityTest]
        public IEnumerator WarmBroth_MendsSlowly()
        {
            apprentice.TakeTickDamage(60f);
            float hurt = apprentice.Health;
            yield return UseFromTheBelt(Provision.WarmBroth);
            yield return new WaitForSeconds(apprentice.drinkTime + 1f);
            Assert.Less(apprentice.Health, hurt + apprentice.brothAmount * 0.5f, "not all at once");
            Assert.Greater(apprentice.Health, hurt, "but mending");
            yield return new WaitForSeconds(apprentice.brothDuration);
            Assert.AreEqual(hurt + apprentice.brothAmount, apprentice.Health, 1f, "all of it, in the end");
        }

        // The Way Back: to black, and back at the last Mooring post Rested at.
        [UnityTest]
        public IEnumerator TheWayBack_TakesYouToThePost()
        {
            var mooring = Object.FindAnyObjectByType<MooringPost>();
            apprentice.Teleport(mooring.RespawnPoint);
            yield return new WaitForSeconds(0.2f);
            input.PressInteract();
            yield return null;
            input.PressDash(); // (stand up)
            yield return new WaitForSeconds(0.3f);
            apprentice.Teleport(new Vector3(30f, apprentice.transform.position.y, 0f));
            yield return new WaitForSeconds(0.2f);
            yield return UseFromTheBelt(Provision.WayBack);
            yield return new WaitForSeconds(apprentice.wayBackTime * 0.8f);
            Assert.Greater(apprentice.Dip, 0.5f, "going dark");
            yield return new WaitForSeconds(apprentice.wayBackTime * 0.2f + 0.2f);
            Assert.Less(Vector3.Distance(apprentice.transform.position, mooring.RespawnPoint), 1f, "at the post");
            Assert.AreEqual(Apprentice.State.Free, apprentice.CurrentState);
            Assert.AreEqual(0, apprentice.Equipment.Carried(Provision.WayBack), "used up");
        }

        // The Ledger: scatter a kind of Soured and it's written in (a quiet line says so), with how many times; the
        // page shows it turning, what it is and what's known. A Boss beaten is written in too.
        [UnityTest]
        public IEnumerator TheLedger_WritesInWhatYouScatter()
        {
            var e = apprentice.Equipment;
            var hud = Object.FindAnyObjectByType<Hud>();
            Assert.IsFalse(e.Met(LedgerEntry.Laden));
            var laden = post.spawner.SpawnAt("Laden", apprentice.transform.position + Vector3.forward * 4f, Vector3.forward);
            laden.passive = true;
            yield return null;
            laden.Slay(apprentice.transform.position);
            yield return null;
            Assert.IsTrue(e.Met(LedgerEntry.Laden));
            Assert.AreEqual(1, e.Scattered(LedgerEntry.Laden));
            Assert.AreEqual("Written in the Ledger: the Laden", hud.LastWritten);
            var another = post.spawner.SpawnAt("Laden", apprentice.transform.position + Vector3.forward * 4f, Vector3.forward);
            another.passive = true;
            yield return null;
            another.Slay(apprentice.transform.position);
            Assert.AreEqual(2, e.Scattered(LedgerEntry.Laden), "counted, not written in twice");
            Assert.IsFalse(e.Met(LedgerEntry.Clinger));

            var screen = Object.FindAnyObjectByType<CharacterScreen>();
            screen.Toggle(true);
            while (screen.Current != CharacterScreen.Tab.Ledger)
                screen.Turn(1);
            yield return null;
            CollectionAssert.AreEqual(new[] { "The Laden" }, screen.LedgerRows);
            StringAssert.Contains(LedgerEntry.Laden.Description, screen.LedgerDetail);
            StringAssert.Contains("Scattered 2 times.", screen.LedgerDetail);
            StringAssert.Contains("The Laden", screen.ShownModel.name, "it, turning");
            screen.Toggle(false);
        }

        // The Tester's Post has more rows than fit: its menu scrolls, the chosen one kept in view.
        [UnityTest]
        public IEnumerator TestersPost_ItsMenuScrolls()
        {
            yield return OpenTheMenu();
            var hud = Object.FindAnyObjectByType<Hud>();
            yield return null;
            yield return null;
            var rows = Menu.Rows;
            Assert.Greater(rows.Count, 8, "more rows than fit");
            Assert.AreEqual(0, hud.Menu.First, "from the top");
            Assert.AreEqual(rows[0].Label, hud.Menu.ShownLabels[0]);
            input.Move = Vector2.up; // (up from the first: round to the last)
            yield return null;
            input.Move = Vector2.zero;
            yield return null;
            yield return null;
            Assert.AreEqual(rows.Count - 1, Menu.Selected);
            Assert.Greater(hud.Menu.First, 0, "scrolled down");
            Assert.AreEqual(rows[rows.Count - 1].Label, hud.Menu.ShownLabels[hud.Menu.ShownLabels.Count - 1], "the last row in view");
            Assert.LessOrEqual(hud.Menu.ShownLabels.Count, 8);
        }

        // A dash steered hands straight on to a run: it never stops (full speed, easing down to a run's), the run's
        // there the moment it ends, and it goes as far as it always did. Unsteered (a backstep) it comes to rest and
        // settles onto the oar; another dash waits out its recovery.
        [UnityTest]
        public IEnumerator ADash_HandsStraightOnToARun_OrSettlesToAStop()
        {
            Vector3 clear = Vector3.zero;
            foreach (var d in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
                if (!Physics.SphereCast(apprentice.transform.position + Vector3.up, 0.4f, d, out _, 9f))
                { clear = d; break; }
            Assert.AreNotEqual(Vector3.zero, clear, "room to dash");
            var stick = new Vector2(clear.x, clear.z);
            input.Move = stick;
            yield return new WaitForSeconds(0.3f);
            Vector3 from = apprentice.transform.position;
            input.PressDash();
            yield return null;
            Assert.AreEqual(Apprentice.State.Dashing, apprentice.CurrentState);
            float slowest = float.MaxValue, freeAt = -1f, t = 0f;
            Vector3 last = apprentice.transform.position;
            for (; t < apprentice.dashDuration + 0.15f; t += Time.deltaTime)
            {
                yield return null;
                Vector3 p = apprentice.transform.position;
                slowest = Mathf.Min(slowest, (p - last).magnitude / Time.deltaTime);
                last = p;
                if (freeAt < 0f && apprentice.CurrentState == Apprentice.State.Free)
                    freeAt = t;
            }
            Assert.Greater(slowest, apprentice.moveSpeed * 0.9f, "never stopping: down to a run's speed, no slower");
            Assert.GreaterOrEqual(freeAt, 0f, "running again");
            Assert.Less(freeAt, apprentice.dashDuration + 0.04f, "the moment the dash ends: no standing about");
            input.Move = Vector2.zero;
            yield return new WaitForSeconds(0.5f);

            // Unsteered: straight on the way it faces, coming to rest.
            input.PressDash();
            yield return null;
            Assert.AreEqual(Apprentice.State.Dashing, apprentice.CurrentState);
            from = apprentice.transform.position;
            yield return new WaitForSeconds(apprentice.dashDuration + 0.02f);
            Assert.AreEqual(apprentice.dashDistance, Vector3.Distance(from, apprentice.transform.position), 0.35f, "as far as a dash goes");
            Vector3 stopped = apprentice.transform.position;
            input.PressDash(); // (another, at once: it waits out the recovery)
            yield return null;
            yield return null;
            Assert.Less(Vector3.Distance(stopped, apprentice.transform.position), 0.02f, "at rest");
            Assert.AreEqual(Apprentice.State.Dashing, apprentice.CurrentState);
            Assert.Greater(apprentice.StateTime, apprentice.dashDuration, "still the first: another waits out its recovery");
        }

        // Every kind of Soured, and every Boss and miniboss, has its page in the Ledger: all but the few that are part of
        // another (Hold and Fast are the Unparted) or have no soul (a scarecrow).
        [Test]
        public void EveryKindOfSoured_AndEveryBoss_HasItsPage()
        {
            var notTheirOwn = new[] { typeof(Soured), typeof(Scarecrow), typeof(UnpartedHold), typeof(HoldPart) }; // (Soured: only ever a kind of it)
            var types = typeof(Soured).Assembly.GetTypes().Where(t => !t.IsAbstract && (typeof(Soured).IsAssignableFrom(t) || typeof(IBoss).IsAssignableFrom(t)));
            var missing = new System.Collections.Generic.List<string>();
            foreach (var t in types)
            {
                if (notTheirOwn.Contains(t))
                    continue;
                var made = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(t); // (only its kind is asked)
                var entry = made is IBoss boss ? LedgerEntry.For(boss) : LedgerEntry.For((Soured)made);
                if (entry == null)
                    missing.Add(t.Name);
            }
            Assert.IsEmpty(missing, "kinds with no page in the Ledger");
            Assert.AreEqual(LedgerEntry.All.Length, LedgerEntry.All.Select(e => e.Id).Distinct().Count(), "each its own id");
        }

        // (the lab) The Tester's Post writes the whole Ledger in, so every page can be seen.
        [UnityTest]
        public IEnumerator TestersPost_WritesInTheWholeLedger()
        {
            yield return OpenTheMenu();
            Menu.Do("Give the Apprentice...");
            Menu.Do("The whole Ledger");
            var e = apprentice.Equipment;
            Assert.IsTrue(LedgerEntry.All.All(entry => e.Met(entry)));
            apprentice.OpenMenu.Close();
            yield return null;
            var screen = Object.FindAnyObjectByType<CharacterScreen>();
            screen.Toggle(true);
            while (screen.Current != CharacterScreen.Tab.Ledger)
                screen.Turn(1);
            yield return null;
            Assert.AreEqual(LedgerEntry.All.Length, screen.LedgerRows.Count);
            for (int i = 0; i < LedgerEntry.All.Length; i++)
            {
                Assert.IsNotNull(screen.ShownModel, $"{LedgerEntry.All[i].Name}: its model");
                StringAssert.Contains(LedgerEntry.All[i].Name, screen.ShownModel.name);
                screen.ChooseEntry(1);
            }
            screen.Toggle(false);
        }

        // The Satchel page: pick a belt slot (left/right), confirm, choose what goes in it (up/down), confirm; back
        // leaves the slot as it was. The one in question turns beside it.
        [UnityTest]
        public IEnumerator TheSatchelPage_FillsABeltSlot()
        {
            var screen = Object.FindAnyObjectByType<CharacterScreen>();
            yield return null;
            var e = apprentice.Equipment;
            e.AddProvision(Provision.PitchPot, 2);
            e.AddProvision(Provision.WarmBroth, 1);
            screen.Toggle(true);
            while (screen.Current != CharacterScreen.Tab.Satchel)
                screen.Turn(1);
            yield return null;
            Assert.AreEqual(0, screen.SatchelSlot);
            StringAssert.Contains("Pitch Pot", screen.ShownModel.name, "the pot in the first slot, turning");
            screen.MoveSlot(1);
            screen.MoveSlot(1);
            Assert.AreEqual(2, screen.SatchelSlot);
            Assert.IsNull(screen.ShownModel, "an empty slot: nothing to show");
            screen.SatchelConfirm();
            Assert.IsTrue(screen.SatchelPicking, "choosing what goes in it");
            Assert.AreEqual("(empty)", screen.SatchelRows[0]);
            Assert.AreEqual(Provision.PitchPot.Name, screen.SatchelRows[1]);
            screen.ChooseProvision(1);
            StringAssert.Contains("burning pitch", screen.SatchelDetail, "the pot, as it's chosen");
            screen.SatchelConfirm();
            Assert.IsFalse(screen.SatchelPicking);
            CollectionAssert.AreEqual(new[] { null, Provision.WarmBroth, Provision.PitchPot, null }, e.Belt, "the pot moved into slot 3 (it was empty)");
            screen.SatchelConfirm();
            screen.ChooseProvision(-1);
            Assert.IsTrue(screen.SatchelBack(), "back: nothing changed");
            CollectionAssert.AreEqual(new[] { null, Provision.WarmBroth, Provision.PitchPot, null }, e.Belt);
            screen.MoveSlot(-1);
            screen.SatchelConfirm();
            screen.ChooseProvision(-1); // (up from the broth: the pot)
            screen.SatchelConfirm();
            CollectionAssert.AreEqual(new[] { null, Provision.PitchPot, Provision.WarmBroth, null }, e.Belt, "put in the broth's slot: they swap");
            InputDevice.Set(true);
            yield return null;
            StringAssert.Contains("LB / RB", screen.Hints, "the gamepad's buttons");
            InputDevice.Set(false);
            yield return null;
            StringAssert.Contains("Q / R", screen.Hints, "or the keyboard's");
            screen.Toggle(false);
            yield return null;
            var hud = Object.FindAnyObjectByType<Hud>();
            StringAssert.Contains("Pitch Pot 2", hud.BeltText(1), "the belt shows it, and how many");
        }

        Drowned SouthDrowned => Object.FindObjectsByType<Drowned>(FindObjectsSortMode.None).First(d => d.name == "Drowned (south edge)");
        static readonly Vector3 AtTheEdge = new Vector3(9f, 0.55f, -4.6f); // (the quay's south edge, over it)

        IEnumerator WaitFor(System.Func<bool> done, float seconds, string what)
        {
            for (float t = 0f; t < seconds && !done(); t += Time.deltaTime)
                yield return null;
            Assert.IsTrue(done(), $"waited {seconds} s for {what}");
        }

        // The Tester's Post sets a Drowned down in the water nearest you, never on the quay.
        [UnityTest]
        public IEnumerator TestersPost_SetsADrownedInTheWater()
        {
            yield return OpenTheMenu();
            Menu.Do("Drowned");
            var set = post.spawner.Spawned.OfType<Drowned>().Single();
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(Drowned.IsOpenWater(set.transform.position, 0.3f), $"in the water ({set.transform.position})");
        }

        // A Drowned waits under the water, out of reach (no lock-on, no blows); when it sees you, it rises and spits:
        // the spit hurts a little, and builds poison.
        [UnityTest]
        public IEnumerator TheDrowned_RisesAndSpitsPoison()
        {
            var drowned = SouthDrowned;
            Assert.IsTrue(drowned.Submerged && !drowned.Targetable, "under, and out of reach");
            Spit spat = null;
            Defence? what = null;
            System.Action<Drowned, Spit> onSpat = (d, sp) => { if (d == drowned) spat = sp; };
            System.Action<Spit, Defence> onResolved = (sp, r) => { if (sp == spat) what = r; };
            Drowned.Spat += onSpat;
            Spit.Resolved += onResolved;
            try
            {
                float health = apprentice.Health;
                apprentice.Teleport(new Vector3(9f, 0.55f, -1.5f));
                yield return WaitFor(() => drowned.CurrentState == Soured.State.Telegraph, 4f, "it rising to spit");
                yield return WaitFor(() => !drowned.Submerged, 1f, "it up out of the water");
                Assert.IsTrue(drowned.Targetable, "up, it can be reached");
                yield return WaitFor(() => spat != null, 2f, "the spit");
                yield return WaitFor(() => what != null, 2f, "it reaching you");
                Assert.AreEqual(Defence.Taken, what);
                Assert.Less(apprentice.Health, health, "it hurts a little");
                Assert.Greater(apprentice.Poison, 30f, "and builds poison");
                yield return null;
                yield return null; // (the HUD draws after gameplay)
                Assert.IsTrue(Object.FindAnyObjectByType<Hud>().PoisonShown, "the meter shows");
                yield return WaitFor(() => drowned.Submerged, 4f, "and it sinks back");
            }
            finally
            {
                Drowned.Spat -= onSpat;
                Spit.Resolved -= onResolved;
            }
        }

        // Poison builds up, and ebbs if you keep clear; full, you're poisoned, losing health, until a Bitter Root (or a
        // Rest) cures it.
        [UnityTest]
        public IEnumerator Poison_BuildsUp_Poisons_AndABitterRootCuresIt()
        {
            apprentice.AddPoison(40f);
            yield return new WaitForSeconds(apprentice.poisonDecayDelay + 1.5f);
            Assert.Less(apprentice.Poison, 40f, "it ebbs, kept clear");
            Assert.IsFalse(apprentice.Poisoned);
            apprentice.AddPoison(70f);
            apprentice.AddPoison(70f);
            Assert.IsTrue(apprentice.Poisoned, "full: poisoned");
            Assert.AreEqual("Poisoned", Object.FindAnyObjectByType<Hud>().LastWarning, "said, in red");
            float health = apprentice.Health;
            yield return new WaitForSeconds(2f);
            Assert.AreEqual(health - apprentice.poisonPerSecond * 2f, apprentice.Health, 1f, "losing health");
            yield return UseFromTheBelt(Provision.BitterRoot);
            yield return new WaitForSeconds(apprentice.drinkTime + 0.1f);
            Assert.IsFalse(apprentice.Poisoned, "cured");
            Assert.AreEqual(0f, apprentice.Poison, "and the build-up gone");
            apprentice.AddPoison(200f);
            apprentice.RestoreFully();
            Assert.IsFalse(apprentice.Poisoned, "a Rest cures it too");
        }

        // Alight: health and stamina burn away (no breath back while it lasts); a Sodden Sack puts it out at once, and
        // so does a Rest. Your own pitch burns you too.
        [UnityTest]
        public IEnumerator Alight_BurnsHealthAndStamina_ASoddenSackPutsItOut()
        {
            yield return null;
            float health = apprentice.Health, stamina = apprentice.Stamina;
            var hud = Object.FindAnyObjectByType<Hud>();
            apprentice.SetAlight();
            Assert.IsTrue(apprentice.Alight);
            Assert.AreEqual("Alight", hud.LastWarning, "said, in red (a warning, not a find)");
            yield return new WaitForSeconds(2f);
            Assert.AreEqual(health - apprentice.alightHealthPerSecond * 2f, apprentice.Health, 1f, "burning");
            Assert.Less(apprentice.Stamina, stamina - apprentice.alightStaminaPerSecond * 1.5f, "and out of breath");
            yield return UseFromTheBelt(Provision.SoddenSack);
            yield return new WaitForSeconds(apprentice.drinkTime + 0.1f);
            Assert.IsFalse(apprentice.Alight, "smothered");
            float after = apprentice.Health;
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(after, apprentice.Health, 0.01f, "and no more burning");
            apprentice.SetAlight();
            apprentice.RestoreFully();
            Assert.IsFalse(apprentice.Alight, "a Rest puts it out too");
            // More fire, longer: but never past the most.
            apprentice.SetAlight();
            Assert.AreEqual(apprentice.alightTime, apprentice.AlightLeft, 0.05f, "long enough to put it out");
            for (int i = 0; i < 5; i++)
                apprentice.SetAlight();
            Assert.AreEqual(apprentice.alightMost, apprentice.AlightLeft, 0.05f, "more fire, longer: never past the most");
            apprentice.Douse();
            // A Pitch Pot at your own feet.
            PitchPot.Throw(null, apprentice.transform.position + Vector3.up * 1.2f + apprentice.transform.forward * 0.3f, apprentice.transform.position + apprentice.transform.forward * 0.4f);
            yield return new WaitForSeconds(1.2f);
            Assert.IsTrue(apprentice.Alight, "your own pitch burns you too");
        }

        // A perfect block turns its spit back, and a Drowned struck by its own spit is slain outright.
        [UnityTest]
        public IEnumerator TheDrowned_SpitTurnedBack_SlaysIt()
        {
            var drowned = SouthDrowned;
            Spit spat = null;
            System.Action<Drowned, Spit> onSpat = (d, sp) => { if (d == drowned) spat = sp; };
            Drowned.Spat += onSpat;
            try
            {
                apprentice.Teleport(new Vector3(9f, 0.55f, -1.5f));
                var toIt = drowned.transform.position - apprentice.transform.position;
                apprentice.transform.rotation = Quaternion.LookRotation(new Vector3(toIt.x, 0f, toIt.z));
                yield return WaitFor(() => spat != null, 6f, "the spit");
                // Raised on time: just before it arrives.
                yield return WaitFor(() => spat == null || Vector3.Distance(spat.transform.position, apprentice.transform.position + Vector3.up * 0.6f) < 1.5f, 2f, "it close");
                input.BlockHeld = true;
                yield return WaitFor(() => !drowned.IsAlive, 2f, "it struck by its own spit");
                input.BlockHeld = false;
                Assert.AreEqual(0f, apprentice.Poison, "nothing got through");
            }
            finally { Drowned.Spat -= onSpat; }
        }

        // Out of its sight (behind something), it comes to the shore nearest you and looks for you there: open to the oar.
        [UnityTest]
        public IEnumerator TheDrowned_YouHide_ItComesAshore()
        {
            var drowned = SouthDrowned;
            var cover = GameObject.CreatePrimitive(PrimitiveType.Cube); // (a wall between it and you)
            cover.transform.position = new Vector3(9f, 1.2f, -4.6f);
            cover.transform.localScale = new Vector3(4f, 1.4f, 0.3f);
            apprentice.Teleport(new Vector3(9f, 0.55f, -3.2f));
            yield return WaitFor(() => drowned.Ashore && !drowned.Submerged, 6f, "it coming ashore, looking");
            Assert.IsTrue(drowned.Targetable);
            Object.Destroy(cover);
            apprentice.Teleport(new Vector3(drowned.transform.position.x, 0.55f, -4.9f));
            apprentice.transform.rotation = Quaternion.LookRotation(Vector3.back);
            yield return null;
            input.PressLight();
            yield return new WaitForSeconds(0.5f);
            Assert.Less(drowned.Health, drowned.maxHealth, "whacked while it's up");
        }

        // Weak to the Hook: caught while it's up, it's hauled out onto the quay, stranded and reeling; then it crawls
        // back to the water and slips in.
        [UnityTest]
        public IEnumerator TheDrowned_Hooked_IsStrandedOnTheQuay()
        {
            var drowned = SouthDrowned;
            apprentice.GiveHook();
            apprentice.Teleport(AtTheEdge);
            apprentice.transform.rotation = Quaternion.LookRotation(Vector3.back);
            yield return WaitFor(() => drowned.CurrentState == Soured.State.Telegraph && !drowned.Submerged, 5f, "it rising");
            input.HookHeld = true;
            yield return null;
            yield return null;
            Assert.AreEqual(drowned, apprentice.HookFoe, "the Hook aims at it, up");
            input.HookHeld = false;
            yield return WaitFor(() => drowned.Stranded, 1f, "hauled out");
            yield return WaitFor(() => drowned.CurrentState == Soured.State.Staggered, 2f, "landed, reeling");
            Assert.Greater(drowned.transform.position.y, 0.3f, "up on the quay");
            Assert.Greater(drowned.transform.position.z, apprentice.transform.position.z, "beyond you, away from the water");
            yield return WaitFor(() => !drowned.Stranded, 10f, "it crawling back in");
            yield return WaitFor(() => drowned.Submerged, 3f, "under again");
        }

        // A long list keeps its chosen row in view, moving as little as it can.
        [Test]
        public void ScrollRows_KeepTheChosenOneInView()
        {
            Assert.AreEqual(0, CharacterScreen.ScrollRows.Window(0, 3, 12, 5), "in view already");
            Assert.AreEqual(2, CharacterScreen.ScrollRows.Window(0, 6, 12, 5), "down past the bottom: one row at a time");
            Assert.AreEqual(4, CharacterScreen.ScrollRows.Window(6, 4, 12, 5), "up past the top");
            Assert.AreEqual(7, CharacterScreen.ScrollRows.Window(0, 11, 12, 5), "wrapped round to the last");
            Assert.AreEqual(0, CharacterScreen.ScrollRows.Window(4, 1, 3, 5), "a short list never scrolls");
        }

        // The character screen: I / C / Select pauses the world behind four pages, left and right between them.
        [UnityTest]
        public IEnumerator TheCharacterScreen_Pauses_AndShowsItsPages()
        {
            var screen = Object.FindAnyObjectByType<CharacterScreen>();
            Assert.IsNotNull(screen);
            yield return null;
            apprentice.AddWarmStone();
            apprentice.AddFerryNail();
            screen.Toggle(true);
            Assert.IsTrue(screen.Open && Pause.IsPaused, "the world stops");
            Assert.IsTrue(CharacterScreen.Blocks, "Esc belongs to it, not the pause menu");
            yield return null;
            Assert.AreEqual(CharacterScreen.Tab.Apprentice, screen.Current);
            Assert.AreEqual($"{Mathf.RoundToInt(apprentice.maxHealth)}", screen.StatValue(0));

            screen.Turn(1);
            Assert.AreEqual(CharacterScreen.Tab.Oar, screen.Current);
            Assert.AreEqual("The Apprentice's Oar", screen.OarName);
            StringAssert.Contains("???", screen.OarNext, "the next step, unnamed");
            StringAssert.Contains("1 Ferry Nail", screen.OarNext);

            screen.Turn(1);
            Assert.AreEqual(CharacterScreen.Tab.Abilities, screen.Current);
            Assert.AreEqual(0, screen.AbilityRows.Count, "none found yet");
            apprentice.GiveHook();
            yield return null;
            Assert.AreEqual("The Hook", screen.AbilityRows[0]);
            Assert.AreEqual("The Hook", screen.AbilityName);
            InputDevice.Set(false);
            yield return null;
            StringAssert.Contains("Hold F", screen.AbilityHow, "how it's used, on the keyboard");
            InputDevice.Set(true);
            yield return null;
            StringAssert.Contains("Hold RB", screen.AbilityHow, "and on a gamepad: only the controls in use");
            StringAssert.Contains("hook post", screen.AbilityHow, "and what for");

            screen.Turn(1);
            Assert.AreEqual(CharacterScreen.Tab.Satchel, screen.Current);
            Assert.AreEqual(1, screen.SatchelRows.Count, "nothing carried yet (only \"empty\")");

            screen.Turn(1);
            Assert.AreEqual(CharacterScreen.Tab.Charms, screen.Current);
            Assert.AreEqual(Charm.TestersKnot.Name, screen.CharmRows[0]);
            Assert.AreEqual(Charm.All.Length, screen.CharmRows.Count, "those found, then a socket for each still to find");
            Assert.AreEqual(Charm.TestersKnot.Description, screen.CharmDetail);
            screen.ChooseCharm(1);
            StringAssert.Contains("Somewhere", screen.CharmDetail);

            screen.Turn(1);
            Assert.AreEqual(CharacterScreen.Tab.Ledger, screen.Current);
            Assert.AreEqual(0, screen.LedgerRows.Count, "no one met yet");

            screen.Turn(1);
            Assert.AreEqual(CharacterScreen.Tab.Lantern, screen.Current);
            Assert.AreEqual(Lantern.Lines[0], screen.LanternLine);
            Lantern.DevLevels = Lantern.Levels - 1;
            yield return null;
            Assert.AreEqual("It's ready.", screen.LanternLine);
            Lantern.DevLevels = 0;
            screen.Turn(1);
            Assert.AreEqual(CharacterScreen.Tab.Apprentice, screen.Current, "round again");

            screen.Toggle(false);
            Assert.IsFalse(Pause.IsPaused);
            // Not over a menu in the world.
            yield return OpenTheMenu();
            screen.Toggle(true);
            Assert.IsFalse(screen.Open, "not while a post's menu is open");
        }

        // ---- The Vault

        // An open stretch of the quay at the west end, nothing overhead (the walkway by the tower would catch the leap).
        static readonly Vector3 VaultFrom = new Vector3(-1f, 0.55f, 0f);

        void ReadyToVault(Vector3 at, Vector3 facing)
        {
            apprentice.GiveVault();
            apprentice.Teleport(at);
            apprentice.transform.rotation = Quaternion.LookRotation(facing);
        }

        IEnumerator VaultEnds(float timeout = 3f)
        {
            for (float t = 0f; t < timeout && apprentice.CurrentState == Apprentice.State.Vaulting; t += Time.deltaTime)
                yield return null;
        }

        static IEnumerator Until(System.Func<bool> done, float timeout, string what)
        {
            for (float t = 0f; t < timeout && !done(); t += Time.deltaTime)
                yield return null;
            Assert.IsTrue(done(), "timed out waiting for " + what);
        }

        static float Flat(Vector3 a, Vector3 b)
        {
            a.y = b.y = 0f;
            return Vector3.Distance(a, b);
        }

        [UnityTest]
        public IEnumerator TheVault_WithoutIt_RTDoesNothing()
        {
            apprentice.Teleport(VaultFrom);
            yield return new WaitForSeconds(0.2f);
            input.PressVault();
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(Apprentice.State.Free, apprentice.CurrentState);
        }

        // Always the same distance, where you face; up high enough to clear a 1.5 m wall; and stamina spent.
        [UnityTest]
        public IEnumerator TheVault_LeapsItsDistance_WhereYouFace()
        {
            ReadyToVault(VaultFrom, Vector3.right);
            yield return new WaitForSeconds(0.2f);
            Vector3 from = apprentice.transform.position;
            float stamina = apprentice.Stamina, top = from.y;
            input.PressVault();
            yield return null;
            Assert.AreEqual(Apprentice.State.Vaulting, apprentice.CurrentState);
            for (float t = 0f; t < 3f && apprentice.CurrentState == Apprentice.State.Vaulting; t += Time.deltaTime)
            {
                top = Mathf.Max(top, apprentice.transform.position.y);
                yield return null;
            }
            float skid = apprentice.vaultDistance / apprentice.vaultFlight * apprentice.vaultSkid / 10f; // (the landing's skid)
            Assert.AreEqual(apprentice.vaultDistance + skid, apprentice.transform.position.x - from.x, 0.35f, "its distance (and a skid), east");
            Assert.AreEqual(from.z, apprentice.transform.position.z, 0.2f);
            Assert.Greater(top - from.y, 1.6f, "up over the oar");
            Assert.AreEqual(stamina - apprentice.vaultStaminaCost, apprentice.Stamina, 6f, "it costs");
        }

        // Over a wall of 1.4 m; not over one of 2.4 m (it comes down this side of it).
        [UnityTest]
        public IEnumerator TheVault_OverALowWall_NotATallOne()
        {
            GameObject Wall(float height)
            {
                var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
                w.transform.position = VaultFrom + Vector3.right * 1.8f + Vector3.up * (height / 2f - 0.05f);
                w.transform.localScale = new Vector3(0.3f, height, 3f);
                return w;
            }
            var low = Wall(1.4f);
            ReadyToVault(VaultFrom, Vector3.right);
            yield return new WaitForSeconds(0.2f);
            input.PressVault();
            yield return null;
            yield return VaultEnds();
            Assert.Greater(apprentice.transform.position.x, low.transform.position.x + 0.5f, "over the low wall");
            Object.Destroy(low);
            var tall = Wall(2.4f);
            ReadyToVault(VaultFrom, Vector3.right);
            yield return new WaitForSeconds(0.6f);
            input.PressVault();
            yield return null;
            yield return VaultEnds();
            Assert.Less(apprentice.transform.position.x, tall.transform.position.x, "not over the tall one");
        }

        // Misjudged: off the quay's edge and short of anything, into the water; washed ashore.
        [UnityTest]
        public IEnumerator TheVault_FallsShort_IntoTheWater_WashedAshore()
        {
            ReadyToVault(new Vector3(26f, 0.55f, -4.4f), Vector3.back); // (the quay's south edge, open water beyond)
            yield return new WaitForSeconds(0.3f);
            float health = apprentice.Health;
            input.PressVault();
            yield return Until(() => apprentice.CurrentState == Apprentice.State.Sinking, 3f, "into the water");
            yield return Until(() => apprentice.CurrentState == Apprentice.State.Free, 4f, "washed ashore");
            Assert.Less(apprentice.Health, health, "a little hurt");
            Assert.Greater(apprentice.transform.position.z, -5.5f, "on the quay again");
        }

        // In the air, what's low passes under: a ring across the floor.
        [UnityTest]
        public IEnumerator TheVault_ARingPassesUnder()
        {
            ReadyToVault(VaultFrom, Vector3.right);
            yield return new WaitForSeconds(0.2f);
            input.PressVault();
            yield return Until(() => apprentice.Airborne, 1f, "off the ground");
            Vector3 at = apprentice.transform.position;
            var ring = Shockwave.Spread(new Vector3(at.x - 1f, VaultFrom.y - 0.05f, at.z), apprentice, 3f, 20f, 0.8f, 12f, 5f, Color.white);
            yield return Until(() => ring == null || ring.Struck, 0.5f, "the ring passing");
            Assert.AreEqual(apprentice.maxHealth, apprentice.Health, "it passed under");
            yield return VaultEnds();
            Assert.IsFalse(apprentice.Airborne);
        }

        // RT again in the air doesn't queue another vault; down, it doesn't go again by itself.
        [UnityTest]
        public IEnumerator TheVault_NotAgainTillItsDown()
        {
            ReadyToVault(VaultFrom, Vector3.right);
            yield return new WaitForSeconds(0.2f);
            Vector3 from = apprentice.transform.position;
            input.PressVault();
            yield return new WaitForSeconds(0.4f);
            input.PressVault();
            yield return VaultEnds();
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(Apprentice.State.Free, apprentice.CurrentState);
            Assert.Less(apprentice.transform.position.x - from.x, apprentice.vaultDistance + 1f, "one leap, not two");
        }

        // Planted on the run, the run carries on through the plant (no dead stop); the camera stays on the ground
        // under you (it doesn't leap with you); the shadow under you shows, and goes when you're down.
        [UnityTest]
        public IEnumerator TheVault_OnTheRun_NoDeadStop_TheCameraStaysLow_AShadowUnderYou()
        {
            ReadyToVault(VaultFrom, Vector3.right);
            input.Move = Vector2.right;
            yield return new WaitForSeconds(0.5f);
            Vector3 before = apprentice.transform.position;
            input.PressVault();
            yield return null;
            yield return null;
            Assert.AreEqual(Apprentice.State.Vaulting, apprentice.CurrentState);
            yield return new WaitForSeconds(0.08f); // (still planting)
            Assert.Greater(apprentice.transform.position.x - before.x, 0.4f, "still going through the plant");
            var rig = CameraRig.Instance;
            float camera = rig.transform.position.y;
            float highest = camera;
            bool shadow = false;
            while (apprentice.CurrentState == Apprentice.State.Vaulting)
            {
                highest = Mathf.Max(highest, rig.transform.position.y);
                shadow |= apprentice.VaultShadowShown;
                yield return null;
            }
            input.Move = Vector2.zero;
            Assert.Less(highest - camera, 0.15f, "the camera didn't leap with you");
            Assert.IsTrue(shadow, "a shadow under you in the air");
            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(apprentice.VaultShadowShown, "and gone when you're down");
        }

        // Down, straight on: free to move (and to act) the moment it lands, no hold; what was pressed just before
        // landing happens then.
        [UnityTest]
        public IEnumerator TheVault_LandsStraightIntoMoving_NoHold()
        {
            ReadyToVault(VaultFrom, Vector3.right);
            yield return new WaitForSeconds(0.2f);
            bool landed = false;
            apprentice.Landed += () => landed = true;
            input.PressVault();
            yield return Until(() => apprentice.Airborne, 1f, "off the ground");
            yield return Until(() => apprentice.transform.position.y < VaultFrom.y + 0.6f && apprentice.CurrentState == Apprentice.State.Vaulting
                                     && apprentice.StateTime > 0.4f, 1f, "coming down");
            input.PressLight(); // (just before it lands)
            yield return Until(() => landed, 1f, "down");
            yield return null;
            Assert.AreEqual(Apprentice.State.Attacking, apprentice.CurrentState, "straight into the swing pressed as it came down");
        }

        // Locked on to a Soured within reach, the Vault carries you to its feet, wherever you were facing.
        [UnityTest]
        public IEnumerator TheVault_LockedOn_CarriesYouToItsFeet()
        {
            ReadyToVault(VaultFrom, Vector3.right);
            var laden = SetDown("Laden", VaultFrom + new Vector3(2.5f, 0f, -2.8f));
            laden.passive = true;
            yield return new WaitForSeconds(0.4f);
            input.PressLock();
            yield return Until(() => apprentice.LockTarget == laden, 1f, "locked on");
            input.PressVault();
            yield return null;
            yield return VaultEnds();
            Assert.Less(Flat(apprentice.transform.position, laden.transform.position), laden.radius + 1.1f, "at its feet");
        }
    }
}
