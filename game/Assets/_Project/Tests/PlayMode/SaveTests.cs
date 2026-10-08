using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MyGame.Tests
{
    // Saving (three slots, kept as you go), the title screen, and the pause menu. Save files go to a scratch folder.
    public class SaveTests
    {
        string folder;

        [SetUp]
        public void PointSavesAway()
        {
            Application.targetFrameRate = -1;
            Time.captureDeltaTime = 1f / 60f;
            folder = Path.Combine(Application.temporaryCachePath, "save-tests");
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
            SaveSlots.Folder = folder;
        }

        [TearDown]
        public void CleanUp()
        {
            SaveSlots.Current = -1;
            SaveSlots.Folder = Application.persistentDataPath;
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
            Pause.Set(false);
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f;
        }

        // A journey is kept as you go: quit and come back, and the world is as you left it; you wake at the Mooring
        // post you last Rested at, your Shade waiting where you fell.
        [UnityTest]
        public IEnumerator AJourney_IsKept_AsYouLeftIt()
        {
            SaveSlots.Current = 0;
            yield return SceneManager.LoadSceneAsync("Main");
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(SaveSlots.Exists(0), "a new game takes its slot from the start");
            var apprentice = Object.FindAnyObjectByType<Apprentice>();
            apprentice.Input = new ScriptedInput();
            foreach (var s in Object.FindObjectsByType<Soured>(FindObjectsSortMode.None).Where(s => !(s is Anchoress)))
                s.gameObject.SetActive(false);

            // Things that last: a Charm fitted, a gate opened, a Healing charge piece taken, a post Rested at, a Boss.
            apprentice.Equipment.Own(Charm.EelSkin);
            apprentice.Equipment.Toggle(Charm.EelSkin);
            var gate = Object.FindAnyObjectByType<ShortcutGate>();
            apprentice.Teleport(gate.transform.position - gate.openFrom * -1.3f);
            gate.Interact(apprentice);
            Assert.IsTrue(gate.IsOpen);
            var piece = Object.FindAnyObjectByType<ChargePiece>();
            apprentice.Teleport(piece.transform.position + Vector3.right * 0.6f);
            piece.Interact(apprentice);
            yield return new WaitForSeconds(1f);
            var boss = Object.FindAnyObjectByType<Anchoress>();
            boss.TakeHit(new Hit { Damage = 100000f, From = boss.transform.position - boss.transform.forward * 2f });
            var chapelPost = MooringPost.All.OrderBy(p => Vector3.Distance(p.transform.position, boss.transform.position)).First();
            apprentice.Teleport(chapelPost.RespawnPoint);
            yield return null;
            chapelPost.Rest(apprentice);
            apprentice.OpenMenu?.Close();
            yield return new WaitForSeconds(6f); // (her death runs its course; the hook comes to rest)
            // Then die carrying Obols, up the boardwalk from the post (not where you'll wake): the Shade keeps them.
            apprentice.Teleport(new Vector3(-20.2f, 0.55f, -33f));
            yield return new WaitForSeconds(0.2f);
            apprentice.GainObols(77);
            int carried = apprentice.Obols; // (her fare, and these)
            apprentice.TakeTickDamage(10000f);
            yield return new WaitForSeconds(apprentice.respawnDelay + 0.5f);
            Assert.IsNotNull(Shade.Current);
            SaveSystem.Instance.SaveNow();
            int maxCharges = apprentice.maxHealingCharges;

            // Come back to it.
            yield return SceneManager.LoadSceneAsync("Main");
            yield return new WaitForSeconds(0.3f);
            apprentice = Object.FindAnyObjectByType<Apprentice>();
            chapelPost = MooringPost.All.First(p => p.SaveId == chapelPost.SaveId);
            Assert.Less(Vector3.Distance(apprentice.transform.position, chapelPost.RespawnPoint), 1f, "woken at the post last Rested at");
            Assert.IsTrue(chapelPost.IsLit);
            Assert.IsTrue(Object.FindAnyObjectByType<ShortcutGate>().IsOpen, "the shortcut stays open");
            Assert.IsTrue(Object.FindObjectsByType<ChargePiece>(FindObjectsInactive.Include, FindObjectsSortMode.None).All(c => c.Taken), "taken stays taken");
            Assert.AreEqual(maxCharges, apprentice.maxHealingCharges);
            Assert.IsTrue(apprentice.Equipment.IsFitted(Charm.EelSkin), "Equipment as it was");
            boss = Object.FindAnyObjectByType<Anchoress>(FindObjectsInactive.Include);
            Assert.IsFalse(boss.IsAlive, "a Boss beaten stays beaten");
            Assert.IsNotNull(boss.FreedHook, "and his hook still lies on his mark, not yet taken");
            Assert.IsTrue(boss.FreedHook.Landed);
            Assert.IsFalse(apprentice.HasHook);
            Assert.IsNotNull(Shade.Current, "the Shade waiting where you fell");
            Assert.AreEqual(carried, Shade.Current.Obols, "holding what you carried");
            Assert.AreEqual(0, apprentice.Obols);
        }

        // The oar's step, the Ferry Nails carried, the Warm Stones and Held Breaths found, and the Stroke are kept.
        [UnityTest]
        public IEnumerator TheOar_WhatsFound_AndTheSatchel_AreKept()
        {
            SaveSlots.Current = 0;
            yield return SceneManager.LoadSceneAsync("Main");
            yield return new WaitForSeconds(0.3f);
            var apprentice = Object.FindAnyObjectByType<Apprentice>();
            apprentice.Input = new ScriptedInput();
            for (int i = 0; i < 4; i++)
                apprentice.AddFerryNail();
            Assert.IsTrue(apprentice.Equipment.Strengthen() && apprentice.Equipment.Strengthen());
            apprentice.AddWarmStone();
            apprentice.AddHeldBreath();
            apprentice.AddHeldBreath();
            apprentice.GiveStroke();
            apprentice.Equipment.AddProvision(Provision.PitchPot, 3);
            apprentice.Equipment.AddProvision(Provision.WarmBroth, 1);
            apprentice.Equipment.ChooseOnBelt(1);
            apprentice.Equipment.Record(LedgerEntry.Laden);
            apprentice.Equipment.Record(LedgerEntry.Laden);
            apprentice.Equipment.Record(LedgerEntry.Anchoress);
            float health = apprentice.maxHealth, stamina = apprentice.maxStamina;
            SaveSystem.Instance.SaveNow();

            yield return SceneManager.LoadSceneAsync("Main");
            yield return new WaitForSeconds(0.3f);
            apprentice = Object.FindAnyObjectByType<Apprentice>();
            var e = apprentice.Equipment;
            Assert.AreEqual(2, e.OarStep);
            Assert.AreEqual(1, e.FerryNails);
            Assert.AreEqual(1, e.WarmStones);
            Assert.AreEqual(2, e.HeldBreaths);
            Assert.AreEqual(health, apprentice.maxHealth);
            Assert.AreEqual(stamina, apprentice.maxStamina);
            Assert.AreEqual(2, apprentice.GetComponent<OarLook>().ShownStep, "and it looks it");
            Assert.IsTrue(apprentice.HasStroke, "the Stroke had");
            Assert.Greater(apprentice.GetComponent<OarLook>().Sheen, 0.1f, "the Drowned Moon's sheen on the blade");
            Assert.Less(apprentice.GetComponent<OarLook>().Sheen, 1f, "(settled: it had it long ago)");
            Assert.AreEqual(3, e.Carried(Provision.PitchPot), "the Satchel as it was");
            Assert.AreEqual(1, e.Carried(Provision.WarmBroth));
            CollectionAssert.AreEqual(new[] { Provision.PitchPot, Provision.WarmBroth, null, null }, e.Belt, "and the belt");
            Assert.AreEqual(Provision.WarmBroth, e.Chosen, "the same one chosen");
            Assert.AreEqual(2, e.Scattered(LedgerEntry.Laden), "the Ledger as it was");
            Assert.AreEqual(1, e.Scattered(LedgerEntry.Anchoress));
            Assert.IsFalse(e.Met(LedgerEntry.Clinger));
        }

        // A Lingerer's business is kept: the keepsake carried (and taken from where it lay), then where she stands with
        // it: given, her fare paid, waiting by the boat.
        [UnityTest]
        public IEnumerator ALingerersBusiness_IsKept()
        {
            SaveSlots.Current = 0;
            yield return SceneManager.LoadSceneAsync("Main");
            yield return new WaitForSeconds(0.3f);
            var apprentice = Object.FindAnyObjectByType<Apprentice>();
            apprentice.Input = new ScriptedInput();
            var hesper = Object.FindAnyObjectByType<Lingerer>();
            var keepsake = Object.FindAnyObjectByType<Keepsake>();
            hesper.Interact(apprentice);
            apprentice.OpenMenu.Close();
            apprentice.Teleport(keepsake.transform.position + Vector3.right * 0.6f);
            yield return null;
            keepsake.Interact(apprentice);
            yield return new WaitForSeconds(1f);
            SaveSystem.Instance.SaveNow();

            yield return SceneManager.LoadSceneAsync("Main");
            yield return new WaitForSeconds(0.3f);
            apprentice = Object.FindAnyObjectByType<Apprentice>();
            hesper = Object.FindAnyObjectByType<Lingerer>();
            Assert.AreEqual(Lingerer.Business.Waiting, hesper.State, "met");
            Assert.IsTrue(apprentice.Equipment.Carries(keepsake.id), "still carried");
            Assert.IsTrue(Object.FindObjectsByType<Keepsake>(FindObjectsInactive.Include, FindObjectsSortMode.None).All(k => k.Taken && !k.gameObject.activeSelf),
                "not lying there again");

            apprentice.GainObols(100);
            hesper.Interact(apprentice);
            var talk = (Conversation)apprentice.OpenMenu;
            talk.Skip();
            talk.Choose(0);
            apprentice.OpenMenu.Close();
            yield return null;
            Assert.AreEqual(Lingerer.Business.Paid, hesper.State);
            SaveSystem.Instance.SaveNow();

            yield return SceneManager.LoadSceneAsync("Main");
            yield return new WaitForSeconds(0.3f);
            apprentice = Object.FindAnyObjectByType<Apprentice>();
            hesper = Object.FindAnyObjectByType<Lingerer>();
            Assert.AreEqual(Lingerer.Business.Paid, hesper.State);
            Assert.IsFalse(apprentice.Equipment.Carries(keepsake.id), "given");
            Assert.IsTrue(hesper.heldKeepsake.activeSelf, "she holds it");
            Assert.Less(Vector3.Distance(hesper.figure.position, hesper.waitAt.position), 0.1f, "waiting by the boat");
            Assert.AreEqual(0, apprentice.Obols);
        }

        // Her Crossing is kept: she's gone on, his lantern stays on the Far shore, her Wick is yours.
        [UnityTest]
        public IEnumerator ACrossing_IsKept()
        {
            SaveSlots.Current = 0;
            yield return SceneManager.LoadSceneAsync("Main");
            yield return new WaitForSeconds(0.3f);
            var apprentice = Object.FindAnyObjectByType<Apprentice>();
            var input = new ScriptedInput();
            apprentice.Input = input;
            var hesper = Object.FindAnyObjectByType<Lingerer>();
            var boat = Object.FindAnyObjectByType<Boat>();
            var far = boat.docks.First(d => d.farShore);
            hesper.Restore(Lingerer.Business.Paid);
            apprentice.Teleport(boat.InteractPosition);
            yield return null;
            boat.Interact(apprentice);
            yield return new WaitForSeconds(1.6f);
            boat.PlaceAt(far.mooring.position, Vector3.back);
            yield return null;
            yield return null;
            apprentice.OpenMenu?.Close(); // (her talk on the water)
            yield return null;
            apprentice.StepAshore(far);
            yield return new WaitForSeconds(apprentice.hopTime + 0.3f);
            apprentice.OpenMenu?.Close(); // (her goodbye)
            yield return new WaitForSeconds(3.5f);
            Assert.AreEqual(Lingerer.Business.Crossed, hesper.State);
            string home = boat.home.name;
            SaveSystem.Instance.SaveNow();

            yield return SceneManager.LoadSceneAsync("Main");
            yield return new WaitForSeconds(0.3f);
            apprentice = Object.FindAnyObjectByType<Apprentice>();
            hesper = Object.FindAnyObjectByType<Lingerer>();
            Assert.AreEqual(Lingerer.Business.Crossed, hesper.State);
            Assert.IsFalse(hesper.figure.gameObject.activeSelf, "gone on");
            Assert.IsTrue(hesper.leftBehind.activeSelf, "his lantern still on the Far shore");
            Assert.IsTrue(apprentice.Equipment.Owns(Charm.HespersWick));
            Assert.AreEqual(home, Object.FindAnyObjectByType<Boat>().MooredAt.name, "the boat at home");
        }

        // Playing a scene straight from the editor (no slot chosen) keeps nothing.
        [UnityTest]
        public IEnumerator PlayingWithoutASlot_SavesNothing()
        {
            yield return SceneManager.LoadSceneAsync("Main");
            yield return new WaitForSeconds(3f);
            Object.FindAnyObjectByType<Apprentice>().GainObols(10);
            yield return new WaitForSeconds(3f);
            for (int i = 0; i < SaveSlots.Count; i++)
                Assert.IsFalse(SaveSlots.Exists(i));
        }

        // The title screen: Play, the three slots (all empty the first time), begin a new game in one; back at the title,
        // Continue leads the menu. A slot can be deleted (asked twice).
        [UnityTest]
        public IEnumerator TitleScreen_Slots_NewGame_Continue_Delete()
        {
            yield return SceneManager.LoadSceneAsync("Title");
            yield return null;
            var title = Object.FindAnyObjectByType<TitleMenu>();
            yield return null;
            Assert.AreEqual("Play", title.Rows[0].Label, "no journeys yet: no Continue");
            title.Choose(0);
            yield return null;
            Assert.AreEqual("Slots", title.PageName);
            Assert.AreEqual(SaveSlots.Count + 1, title.Rows.Count, "three slots and Back");
            StringAssert.Contains("empty", title.Rows[1].Label);
            title.Choose(1);
            Assert.IsTrue(SceneFader.Busy, "a fade, and a moment's loading");
            for (float t = 0; t < 6f && SceneFader.Busy; t += Time.deltaTime)
                yield return null;
            Assert.IsFalse(SceneFader.Busy, "faded back in");
            Assert.AreEqual("Main", SceneManager.GetActiveScene().name, "a new game in slot II");
            Assert.AreEqual(1, SaveSlots.Current);
            Assert.IsTrue(SaveSlots.Exists(1));

            yield return SceneManager.LoadSceneAsync("Title");
            yield return null;
            title = Object.FindAnyObjectByType<TitleMenu>();
            yield return null;
            Assert.AreEqual("Continue", title.Rows[0].Label);
            Assert.AreEqual(-1, SaveSlots.Current, "at the title, no slot is being played");
            SaveSlots.Delete(1);
            Assert.IsFalse(SaveSlots.Exists(1));
        }

        // The pause menu stops the world: time, and the Apprentice (who ignores the controls meanwhile).
        [UnityTest]
        public IEnumerator Pause_StopsTheWorld()
        {
            yield return SceneManager.LoadSceneAsync("Main");
            yield return new WaitForSeconds(0.3f);
            var apprentice = Object.FindAnyObjectByType<Apprentice>();
            var input = new ScriptedInput();
            apprentice.Input = input;
            var pause = Object.FindAnyObjectByType<PauseMenu>();
            pause.Toggle(true);
            Assert.IsTrue(Pause.IsPaused);
            Assert.AreEqual(0f, Time.timeScale);
            Assert.AreEqual("Paused", pause.PageName == "Main" ? "Paused" : pause.PageName);
            Vector3 at = apprentice.transform.position;
            input.Move = Vector2.up;
            for (int f = 0; f < 30; f++)
                yield return null;
            Assert.Less(Vector3.Distance(at, apprentice.transform.position), 0.01f, "nothing moves while paused");
            pause.Toggle(false);
            for (int f = 0; f < 30; f++)
                yield return null;
            Assert.AreEqual(1f, Time.timeScale);
            Assert.Greater(Vector3.Distance(at, apprentice.transform.position), 0.5f, "and on again");
        }

        // Esc with a Mooring post's menu open leaves the menu; it doesn't pause on top of it.
        [UnityTest]
        public IEnumerator Escape_LeavesAMenuInTheWorld_RatherThanPausing()
        {
            yield return SceneManager.LoadSceneAsync("Main");
            yield return new WaitForSeconds(0.3f);
            var apprentice = Object.FindAnyObjectByType<Apprentice>();
            apprentice.Input = new ScriptedInput();
            var post = MooringPost.All.OrderBy(p => Vector3.Distance(p.transform.position, apprentice.transform.position)).First();
            post.Rest(apprentice); // (the Tester's Knot is owned: its menu opens)
            yield return null;
            Assert.IsNotNull(apprentice.OpenMenu);
            var pause = Object.FindAnyObjectByType<PauseMenu>();
            pause.PausePressed();
            yield return null;
            Assert.IsNull(apprentice.OpenMenu, "the menu left");
            Assert.IsFalse(pause.Open, "and no pause on top of it");
            pause.PausePressed();
            Assert.IsTrue(pause.Open, "the next press pauses");
            pause.Toggle(false);
        }

        [Test]
        public void Settings_AreKept()
        {
            float music = Settings.Music;
            Settings.Music = 0.3f;
            Assert.AreEqual(0.3f, Settings.Music, 0.001f);
            Settings.Music = 1.7f;
            Assert.AreEqual(1f, Settings.Music, 0.001f, "within 0..1");
            Settings.Music = music;
        }
    }
}
