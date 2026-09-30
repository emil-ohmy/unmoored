using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MyGame.Tests
{
    // The lab (Scenes/Lab.unity): the Tester's Post spawns any Soured and gives the Apprentice anything; the training
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
            yield return SceneManager.LoadSceneAsync("Lab");
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
            StringAssert.Contains("THE LAB", readout.Text);
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
            apprentice.OpenMenu.Close();
            yield return new WaitForSeconds(1.5f);
            Assert.IsTrue(spawned.Any(s => s.CurrentState != Soured.State.Idle), "aggressive: they come for you");

            yield return OpenTheMenu();
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
    }
}
