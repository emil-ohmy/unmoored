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
            post = Object.FindAnyObjectByType<TesterPost>();
            readout = Object.FindAnyObjectByType<LabReadout>();
            dummy = Object.FindObjectsByType<Laden>(FindObjectsSortMode.None).First(l => l.immortal);
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
            Menu.Do("A Healing charge");
            Assert.IsTrue(apprentice.Equipment.Owns(Charm.HespersWick) && apprentice.Equipment.Owns(Charm.EelSkin));
            Assert.AreEqual(2, apprentice.Equipment.charmSlots);
            Assert.IsTrue(apprentice.HasHook);
            Assert.AreEqual(500, apprentice.Obols);
            Assert.AreEqual(4, apprentice.maxHealingCharges);
            Menu.Do("Back");
            Menu.Do("Reset the Apprentice");
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
            Assert.Less(Vector3.Distance(u.fast.transform.position, u.hold.transform.position), 1.5f);
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
                u.fast.TakeHit(new Hit { Damage = 1000f, From = apprentice.transform.position });
                u.hold.TakeHit(new Hit { Damage = 10000f, From = apprentice.transform.position });
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
            Assert.Less(Vector3.Distance(u.fast.transform.position, u.hold.transform.position), 1.5f);
        }
    }
}
