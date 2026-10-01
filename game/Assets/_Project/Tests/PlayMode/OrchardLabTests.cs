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

        // ---- The Watchman, of the Unreaped Rows

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

        // Found: struck once (no harm), he tears free; his Moths held out of it (pinned far off), so he's alone.
        IEnumerator Reveal(Watchman w)
        {
            StandBy(w, 1.6f, true);
            yield return null;
            input.PressLight();
            yield return WaitFor(() => !w.Hidden, 1f, "him found");
            yield return new WaitForSeconds(1.4f); // (tearing free)
            foreach (var f in w.flocks)
                if (f.gameObject.activeSelf)
                    foreach (var m in f.Moths)
                        m.Pin(new Vector3(-14f, 6f, 16f));
        }

        // The Unreaped Rows: in through the gateway from the orchard; found, he shuts it behind you (the gates swing to,
        // and hold), and his own music plays; beaten, it opens again.
        [UnityTest]
        public IEnumerator TheField_FoundHeShutsTheGate_BeatenItOpens()
        {
            var w = Watchman();
            var gate = UnityEngine.Object.FindAnyObjectByType<ArenaGate>();
            Assert.IsNotNull(gate);
            Assert.IsFalse(gate.Shut);
            Assert.AreEqual(2, w.Music.Length, "his music, both halves");
            Assert.IsTrue(w.Music.All(m => m != null));
            // In from the orchard's top terrace, through the gateway, while it's open.
            Vector3 way = gate.transform.position;
            apprentice.Teleport(new Vector3(way.x, 3.35f, way.z - 2f));
            yield return Walk(Vector2.up, 1.6f);
            Assert.Greater(apprentice.transform.position.z, way.z + 0.8f, $"through the gateway ({apprentice.transform.position})");
            yield return Reveal(w);
            Assert.IsTrue(gate.Shut, "shut behind you");
            Assert.AreEqual(1f, gate.Closed, 0.01f, "swung to");
            // Try to leave: the way's held.
            apprentice.Teleport(new Vector3(way.x, 3.35f, way.z + 1.5f));
            yield return Walk(Vector2.down, 1.6f);
            Assert.Greater(apprentice.transform.position.z, way.z, $"held in ({apprentice.transform.position})");
            w.TakeHit(new Hit { Damage = w.maxHealth * 2f, From = apprentice.transform.position });
            yield return new WaitForSeconds(2f);
            Assert.IsFalse(gate.Shut, "beaten: open again");
            Assert.AreEqual(0f, gate.Closed, 0.01f);
        }

        // Six scarecrows in the top rows, all alike: one of them is him, still hidden, his lantern dark; no fight yet.
        [UnityTest]
        public IEnumerator TheWatchman_StandsAmongTheScarecrows_Hidden()
        {
            var w = Watchman();
            Assert.IsNotNull(w);
            Assert.AreEqual(5, w.field.Length);
            Assert.IsTrue(w.Hidden);
            Assert.IsFalse(w.lantern.enabled, "his lantern dark");
            Assert.IsNull(BossFight.Current);
            foreach (var s in w.field)
            {
                Assert.AreEqual(w.transform.position.y, s.transform.position.y, 0.1f, "on the top terrace with him");
                Assert.AreEqual(w.transform.eulerAngles.y, s.transform.eulerAngles.y, 1f, "facing the same way");
                Assert.Greater(Vector3.Distance(w.transform.position, s.transform.position), 2.5f);
            }
            yield return null;
        }

        // Watched (in front of the Apprentice), he never stirs; behind them, he creeps nearer, turning to them.
        [UnityTest]
        public IEnumerator TheWatchman_Hidden_CreepsOnlyWhenBehindYou()
        {
            var w = Watchman();
            int crept = 0;
            Action<Watchman> onCreep = _ => crept++;
            global::MyGame.Watchman.Crept += onCreep;
            try
            {
                StandBy(w, 5f, true);
                Vector3 start = w.transform.position;
                yield return new WaitForSeconds(4f);
                Assert.AreEqual(0, crept, "watched: still");
                Assert.Less(Vector3.Distance(start, w.transform.position), 0.05f);
                StandBy(w, 5f, false);
                yield return WaitFor(() => crept > 0, 4f, "him creeping, behind you");
                Assert.Less(Vector3.Distance(w.transform.position, apprentice.transform.position), 4.95f, "nearer");
                Assert.IsTrue(w.Hidden, "and still one of them");
                Assert.IsTrue(w.field.All(s => s.IsAlive && Vector3.Distance(s.transform.position, s.pole.position) < 0.3f), "the rest unmoved");
            }
            finally { global::MyGame.Watchman.Crept -= onCreep; }
        }

        // Strike the wrong one: it bursts into straw, and the Moths nesting in it come at you. He stays hidden.
        [UnityTest]
        public IEnumerator AScarecrowStruck_Bursts_AndMothsComeOut()
        {
            var w = Watchman();
            var s = w.field[0];
            StandBy(s, 1.6f, true);
            yield return null;
            input.PressLight();
            yield return WaitFor(() => !s.IsAlive, 1f, "it burst");
            Assert.IsTrue(s.nest.gameObject.activeSelf, "its Moths out");
            yield return WaitFor(() => s.nest.Chasing, 2f, "them coming");
            Assert.IsTrue(w.Hidden, "he's still one of them");
            Assert.IsNull(BossFight.Current);
        }

        // Strike the right one: no harm done, but he's found: he tears free of his stake, his lantern flares, his Moths
        // come to it, and his name plate and bar come up.
        [UnityTest]
        public IEnumerator TheWatchmanStruck_TearsFree_TheFightBegins()
        {
            var w = Watchman();
            StandBy(w, 1.6f, true);
            yield return null;
            input.PressLight();
            yield return WaitFor(() => !w.Hidden, 1f, "him found");
            Assert.AreEqual(w.maxHealth, w.Health, "the blow that found him did no harm");
            yield return null;
            yield return null;
            Assert.AreSame(w, BossFight.Current, "his fight");
            Assert.AreEqual("The Watchman", w.Title);
            Assert.IsTrue(w.lantern.enabled, "his lantern lit");
            Assert.IsTrue(w.flocks[0].gameObject.activeSelf && !w.flocks[1].gameObject.activeSelf, "one flock to it");
            yield return new WaitForSeconds(1.4f);
            StandBy(w, 6f, false); // (back turned: he comes)
            yield return new WaitForSeconds(1.2f);
            Assert.Greater(Vector3.Distance(w.transform.position, w.pole.position), 0.3f, "torn free: the stake left standing");
        }

        // Faced, he's stock still; turn away and he comes, in lurches.
        [UnityTest]
        public IEnumerator TheWatchman_Watched_StandsStill_Unwatched_Comes()
        {
            var w = Watchman();
            yield return Reveal(w);
            StandBy(w, 6f, true);
            Vector3 at = w.transform.position;
            yield return new WaitForSeconds(2f);
            Assert.IsTrue(w.Frozen, "watched");
            Assert.Less(Vector3.Distance(at, w.transform.position), 0.05f, "not a step");
            apprentice.transform.rotation = Quaternion.LookRotation(apprentice.transform.position - w.transform.position);
            float before = Vector3.Distance(w.transform.position, apprentice.transform.position);
            yield return new WaitForSeconds(1.5f);
            Assert.IsFalse(w.Frozen);
            Assert.Less(Vector3.Distance(w.transform.position, apprentice.transform.position), before - 1f, "on you, while your back's turned");
        }

        // Behind you, he sweeps his reaping hook.
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

        // Alight, he can't hold still: watched or not, he comes.
        [UnityTest]
        public IEnumerator TheWatchman_Alight_Thrashes_EvenWatched()
        {
            var w = Watchman();
            yield return Reveal(w);
            StandBy(w, 6f, true);
            yield return new WaitForSeconds(0.5f);
            Vector3 at = w.transform.position;
            Burning.Set(w, 4f, 2f);
            yield return new WaitForSeconds(1.5f);
            Assert.IsTrue(w.Thrashing);
            Assert.Greater(Vector3.Distance(at, w.transform.position), 1f, "moving, though you're looking at him");
            Assert.Less(w.Health, w.maxHealth, "the fire eating him");
        }

        // Half way, his second flock comes; beaten, his farewell, and his lantern's Moths fall with him.
        [UnityTest]
        public IEnumerator TheWatchman_AtHalf_ASecondFlock_Beaten_HisMothsFall()
        {
            var w = Watchman();
            yield return Reveal(w);
            foreach (var m in w.flocks[0].Moths)
                m.Pin(apprentice.transform.position + Vector3.up * 2f + Vector3.right * 3f);
            w.TakeHit(new Hit { Damage = w.maxHealth * 0.55f, From = apprentice.transform.position });
            yield return WaitFor(() => w.flocks[1].gameObject.activeSelf, 1f, "the second flock");
            Assert.AreEqual(2, w.Phase);
            foreach (var m in w.flocks[1].Moths)
                m.Pin(apprentice.transform.position + Vector3.up * 2f + Vector3.left * 3f);
            IBoss beaten = null;
            Action<IBoss> onDefeat = b => beaten = b;
            BossFight.Defeated += onDefeat;
            try
            {
                yield return new WaitForSeconds(1.2f);
                w.TakeHit(new Hit { Damage = w.maxHealth, From = apprentice.transform.position });
                yield return null;
                Assert.AreSame(w, beaten, "his farewell");
                Assert.AreEqual("THE WATCH IS ENDED", w.Farewell);
                Assert.IsTrue(w.flocks.SelectMany(f => f.Moths).All(m => !m.IsAlive), "his Moths fall with him");
                Assert.IsFalse(w.lantern.enabled);
            }
            finally { BossFight.Defeated -= onDefeat; }
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
            Assert.IsFalse(w.lantern.enabled);
            Assert.IsTrue(w.field.All(s => s.IsAlive && !s.nest.gameObject.activeSelf), "the scarecrows whole");
            Assert.IsTrue(w.flocks.All(f => !f.gameObject.activeSelf));
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
            yield return Frames(12);
            Shot("watchman-1-a-scarecrow-bursts");
            foreach (var m in s.nest.Moths)
                m.Slay(apprentice.transform.position);
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
            foreach (var m in w.flocks[0].Moths)
                m.Pin(new Vector3(-14f, 6f, 16f));
            StandBy(w, 2.2f, false);
            for (int i = 0; i < 150 && w.CurrentState != Soured.State.Telegraph; i++)
                yield return null;
            yield return Frames(10);
            Shot("watchman-4-the-hook-drawn");
            apprentice.transform.rotation = Quaternion.LookRotation(w.transform.position - apprentice.transform.position);
            yield return Frames(20);
            Shot("watchman-5-faced-stock-still");
            Burning.Set(w, 4f, 2f);
            yield return Frames(30);
            Shot("watchman-6-alight");
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
