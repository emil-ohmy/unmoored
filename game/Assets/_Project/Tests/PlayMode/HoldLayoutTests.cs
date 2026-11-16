using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MyGame.Tests
{
    // The Hold, the level (Lab_HoldLayout, from Levels/the_hold_spiral.json, as laid out in the level editor): one way
    // winding in to the Boss's chamber (the Kept tear out of its walls as you pass, the tutorial's messages on its
    // floor), a side way to a grotto (a Warm Stone; the door of eyes, which won't open), a shortcut back to the Mooring
    // post opened only from the antechamber, the arena sealed behind you while the Hold fights. Read from the level's
    // own file, so they follow it as it's changed.
    public class HoldLayoutTests
    {
        Apprentice apprentice;
        ScriptedInput input;

        [Serializable] sealed class Marker { public string code, label; public float e, n; }
        [Serializable] sealed class Level { public float scale = 1f; public Marker[] markers; }
        [Serializable] sealed class Spot { public float[] at; }
        [Serializable] sealed class Spots { public Spot[] eyes, presences; }

        static string LevelFile => Path.Combine(Application.dataPath, "_Project/Levels/the_hold_spiral.json");
        static Level TheLevel => JsonUtility.FromJson<Level>(File.ReadAllText(LevelFile));
        static Spots TheSpots => JsonUtility.FromJson<Spots>(File.ReadAllText(LevelFile.Replace(".json", ".spots.json")));
        static Marker[] Markers(string code) => TheLevel.markers.Where(m => m.code == code).ToArray();
        static Vector2 At(Marker m) => new Vector2(m.e, m.n) * TheLevel.scale;

        // Its paths' points (e, n), straight out of the file (Unity's JSON can't read nested lists).
        static List<List<Vector2>> Paths()
        {
            var text = File.ReadAllText(LevelFile);
            float scale = TheLevel.scale;
            float F(string s) => float.Parse(s, CultureInfo.InvariantCulture);
            return Regex.Matches(text, @"""points""\s*:\s*\[((?:\s*\[[^\]]*\]\s*,?)*)\s*\]").Cast<Match>()
                .Select(m => Regex.Matches(m.Groups[1].Value, @"\[\s*([-\d.eE+]+)\s*,\s*([-\d.eE+]+)\s*\]").Cast<Match>()
                    .Select(p => new Vector2(F(p.Groups[1].Value), F(p.Groups[2].Value)) * scale).ToList())
                .ToList();
        }

        // The way: the longest of them, start to Boss.
        static List<Vector2> TheWay() => Paths().OrderByDescending(p => p.Count).First();

        [UnitySetUp]
        public IEnumerator LoadIt()
        {
            Application.targetFrameRate = -1;
            Time.captureDeltaTime = 1f / 60f;
            yield return SceneManager.LoadSceneAsync("Lab_HoldLayout");
            yield return null;
            apprentice = UnityEngine.Object.FindAnyObjectByType<Apprentice>();
            apprentice.Input = input = new ScriptedInput();
            apprentice.postHitInvulnerability = 1000f; // (struck once, then left alone: the way is what's tested)
            if (!TestContext.CurrentContext.Test.Name.StartsWith("TheOpening"))
                UnityEngine.Object.FindAnyObjectByType<TheOpening>()?.Skip(); // (standing, the lantern at the hip: the way is what's tested)
            yield return new WaitForSeconds(0.3f);
        }

        // The game's opening: lying still in complete darkness (no light but the lantern's, and that out); it kindles, the
        // words come, then the prompt (nothing pressed before it does anything); taken, you rise, the cave's lights come
        // back, and only once you stand is the Hold named. Unharmed throughout; free to walk after.
        [UnityTest]
        public IEnumerator TheOpening_YouLieInTheDark_TakeTheLight_AndRise()
        {
            var opening = UnityEngine.Object.FindAnyObjectByType<TheOpening>();
            var bow = apprentice.GetComponent<BowLantern>();
            var hud = UnityEngine.Object.FindAnyObjectByType<Hud>();
            var entrance = UnityEngine.Object.FindAnyObjectByType<AreaEntrance>();
            Assert.IsTrue(opening.Playing, "it plays as the Hold loads");
            Assert.AreEqual(Apprentice.State.Waking, apprentice.CurrentState, "lying still");
            Assert.IsTrue(apprentice.LanternOnTheFloor, "the lantern on the floor, not at the hip");
            // (the world's lights: not the character screen's stage lamps, 400 m under it, lighting only its portrait)
            IEnumerable<Light> WorldLights() => UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l => l.transform.position.y > -100f);
            int lightsAtAll = WorldLights().Count(l => l.enabled);
            Assert.That(WorldLights().Where(l => l.enabled),
                Is.All.Matches<Light>(l => l == opening.floorLight || l == bow.lanternLight), "no light but the lanterns'");
            Assert.AreEqual(0f, bow.Dimmed, "the hip's out");
            Assert.Less(opening.floorLight.intensity, 0.01f, "and the floor's not yet lit: complete darkness");
            Assert.IsFalse(entrance.Announced, "not named over the dark");

            // Pressing before you're bidden does nothing.
            yield return new WaitForSeconds(1f);
            input.PressInteract();
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(Apprentice.State.Waking, apprentice.CurrentState);
            Assert.IsTrue(apprentice.LanternOnTheFloor, "nothing taken before the word");

            for (float t = 0f; opening.Current != TheOpening.Stage.Bidden; t += Time.deltaTime)
            {
                Assert.Less(t, 15f, "bidden in time");
                yield return null;
            }
            Assert.Greater(opening.floorLight.intensity, 1f, "the lantern lit before you");
            Assert.AreEqual("Apprentice. Take light.", hud.OpeningLine.Text.text);
            Assert.IsTrue(hud.OpeningLine.Showing, "the words in the dark");
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual("Take light", hud.PromptText);
            Assert.Greater(hud.PromptAlpha, 0.9f, "the prompt");
            Assert.IsTrue(apprentice.AwaitingLight);

            input.PressInteract();
            yield return new WaitForSeconds(apprentice.wakeTakeAt + 0.2f);
            Assert.IsFalse(apprentice.LanternOnTheFloor, "taken: at the hip now");
            Assert.IsFalse(opening.floorLantern.activeSelf, "gone from the floor");
            Assert.AreEqual(Apprentice.State.Waking, apprentice.CurrentState, "still rising");
            for (float t = 0f; apprentice.CurrentState != Apprentice.State.Free; t += Time.deltaTime)
            {
                Assert.Less(t, apprentice.wakeDuration + 1f, "up in time");
                yield return null;
            }
            for (float t = 0f; opening.Playing; t += Time.deltaTime)
            {
                Assert.Less(t, 6f, "the lights back in time");
                yield return null;
            }
            Assert.AreEqual(1f, bow.Dimmed, "the hip lantern burning");
            Assert.Greater(WorldLights().Count(l => l.enabled), lightsAtAll + 10,
                "the cave's lights all back");
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(entrance.Announced, "named once you've risen");
            Assert.AreEqual(apprentice.maxHealth, apprentice.Health, "unharmed");
            Vector3 was = apprentice.transform.position;
            input.Move = new Vector2(0f, -1f);
            yield return new WaitForSeconds(0.6f);
            input.Move = Vector2.zero;
            Assert.Greater(Vector3.Distance(was, apprentice.transform.position), 1f, "free to walk");
        }

        [TearDown]
        public void ResetTime() => Time.captureDeltaTime = 0f;

        IEnumerator Walk(Vector2 to, Action each = null, float within = 0.5f)
        {
            Vector3 last = apprentice.transform.position;
            float stuck = 0f;
            for (float t = 0f; t < 40f; t += Time.deltaTime)
            {
                Vector3 d = new Vector3(to.x, 0f, to.y) - new Vector3(apprentice.transform.position.x, 0f, apprentice.transform.position.z);
                if (d.magnitude < within)
                    break;
                input.Move = new Vector2(d.x, d.z).normalized;
                if (apprentice.CurrentState == Apprentice.State.Staggered || apprentice.CurrentState == Apprentice.State.Attacking)
                    input.PressLight(); // (the Kept in the way: strike back)
                each?.Invoke();
                yield return null;
                Vector3 now = apprentice.transform.position;
                stuck = (now - last).magnitude < 0.003f ? stuck + Time.deltaTime : 0f;
                last = now;
                Assert.Less(stuck, 3f, $"stuck at {now} on the way to {to}");
                Assert.IsTrue(apprentice.IsAlive, $"died at {now}");
            }
            input.Move = Vector2.zero;
        }

        IEnumerator Interact()
        {
            yield return null;
            input.PressInteract();
            yield return null;
            yield return null;
        }

        static int Count<T>() where T : UnityEngine.Object => UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None).Length;

        [UnityTest]
        public IEnumerator ItsPieces_AreAllThere_AsTheLevelHasThem()
        {
            int doors = Markers("EYED").Length;
            Assert.AreEqual(Markers("MSG").Length, Count<FloorMessage>(), "a message on the floor for each");
            Assert.AreEqual(Markers("VOICE").Length, Count<VoiceLine>(), "the voice, where the level has it");
            Assert.AreEqual(Markers("KP").Length, Count<Kept>(), "the Kept");
            Assert.AreEqual(Markers("MP").Length, Count<MooringPost>(), "the Mooring post");
            Assert.AreEqual(Markers("WS").Length, UnityEngine.Object.FindObjectsByType<UpgradePiece>(FindObjectsSortMode.None).Count(p => p.kind == UpgradePiece.Kind.WarmStone), "the Warm Stone");
            Assert.AreEqual(Markers("OB").Length, Count<ObolCache>(), "the Obols");
            Assert.AreEqual(Markers("SG").Length, Count<ShortcutGate>(), "the shortcut");
            Assert.AreEqual(Markers("HOLD").Length, Count<TheHoldWall>(), "the Boss");
            Assert.AreEqual(Markers("AG").Length, Count<HoldSeal>(), "its arena's seal");
            foreach (var seal in UnityEngine.Object.FindObjectsByType<HoldSeal>(FindObjectsSortMode.None))
                Assert.IsNotNull(seal.ridge.GetComponentInChildren<Renderer>(true), "its stone in it (to sink and raise)");
            Assert.AreEqual(doors, Count<EyeDoor>(), "the door of eyes");
            Assert.AreEqual(TheSpots.eyes.Length + 5 * doors, Count<WallEye>(), "the eyes in the walls, and the door's");
            Assert.GreaterOrEqual(TheSpots.eyes.Length, Markers("EYE").Length - 1, "every eye drawn found its wall (or all but one)");
            Assert.AreEqual(TheSpots.presences.Length, Count<WallPresence>(), "the faces and hands");
            Assert.IsNotNull(UnityEngine.Object.FindAnyObjectByType<Drips>(), "drips all through it");
            yield return null;
        }

        // You walk the way in, reading each message as you come to it; the Kept come out of the walls as you pass; the
        // Hold keeps its arms out of sight while you go round behind its wall; you come into its chamber, it wakes, and
        // the way in heaves shut behind you.
        [UnityTest]
        public IEnumerator YouWalkTheWayIn_ReadingTheMessages_AndTheHoldWakes_AndSealsYouIn()
        {
            var hud = UnityEngine.Object.FindAnyObjectByType<Hud>();
            var hold = UnityEngine.Object.FindAnyObjectByType<TheHoldWall>();
            var seal = UnityEngine.Object.FindAnyObjectByType<HoldSeal>();
            var way = TheWay();
            var messages = UnityEngine.Object.FindObjectsByType<FloorMessage>(FindObjectsSortMode.None).ToList();
            var voices = UnityEngine.Object.FindObjectsByType<VoiceLine>(FindObjectsSortMode.None).ToList();
            // (the way, with a step aside onto each message and to each voice along it, at the point it's nearest)
            var stops = way.Select(p => (p, (Component)null)).ToList();
            var along = messages.Where(m => way.Min(p => Vector2.Distance(p, Flat(m.transform.position))) < 6f).ToList();
            var voicesAlong = voices.Where(v => way.Min(p => Vector2.Distance(p, Flat(v.transform.position))) < 6f).ToList();
            foreach (var c in along.Cast<Component>().Concat(voicesAlong).OrderByDescending(c => Nearest(way, Flat(c.transform.position))))
                stops.Insert(Nearest(way, Flat(c.transform.position)) + 1, (Flat(c.transform.position), c));
            bool hiddenBehind = false;
            void Watch()
            {
                float d = Vector3.Distance(new Vector3(apprentice.transform.position.x, 0f, apprentice.transform.position.z),
                                           new Vector3(hold.arenaCentre.x, 0f, hold.arenaCentre.z));
                if (d > hold.armsHiddenBeyond + 2f && hold.ArmsHidden)
                    hiddenBehind = true;
            }
            foreach (var (p, c) in stops)
            {
                yield return Walk(p, Watch, c is FloorMessage ? 0.5f : 1.2f);
                if (c is FloorMessage m)
                {
                    yield return null;
                    Assert.AreEqual(m, apprentice.Nearby, $"{m.message}: in reach to read");
                    yield return Interact();
                    Assert.AreEqual(m.message, hud.FoundBanner.Text.text, "its message up");
                }
                else if (c is VoiceLine v)
                {
                    yield return null;
                    Assert.IsTrue(v.Spoken, $"{v.line}: spoken as you came near");
                    Assert.AreEqual(UiKit.Unmarked(v.line), hud.LastSpoken, "its words up");
                    Assert.IsTrue(hud.VoiceBanner.Showing, "as subtitles");
                    StringAssert.Contains("<color=", hud.VoiceBanner.Text.text, "what matters in it in the Info colour");
                }
            }
            Assert.AreEqual(along.Count, along.Count(m => m.Read > 0), "every message on the way read");
            Assert.AreEqual(1, messages.Count, "one message left on the floor, for old times' sake");
            Assert.AreEqual("Try finger.", messages[0].message);
            Assert.AreEqual(voicesAlong.Count, voicesAlong.Count(v => v.Spoken), "the voice heard all along the way");
            Assert.GreaterOrEqual(voicesAlong.Count, 4, "the tutorial in the voice along the way");
            var keptAlong = UnityEngine.Object.FindObjectsByType<Kept>(FindObjectsSortMode.None)
                .Where(k => way.Min(p => Vector2.Distance(p, Flat(k.transform.position))) < 8f).ToList();
            Assert.GreaterOrEqual(keptAlong.Count, 4, "the Kept wait along the way");
            Assert.IsTrue(keptAlong.All(k => k.Emerged || !k.IsAlive), "the Kept torn out of the walls as you passed");
            Assert.IsTrue(hiddenBehind, "its arms folded away while you went round behind its wall");
            Assert.AreEqual(TheHoldWall.Mood.Dormant, hold.State, "asleep till you come in");
            var stone = seal.ridge.GetComponentInChildren<Renderer>(true);
            Assert.IsTrue(!stone.gameObject.activeInHierarchy || stone.bounds.max.y < 0.45f, $"the seal sunk in the floor (its top at {stone.bounds.max.y})");
            Assert.Less(hold.transform.position.z - seal.transform.position.z, 13f, "a short walk from the way in to its wall");
            // Into its chamber: it wakes, and the way in heaves shut behind you; its name only once its face has formed.
            float wokeAt = -1f, namedAt = -1f;
            void Waking()
            {
                if (wokeAt < 0f && hold.State != TheHoldWall.Mood.Dormant)
                    wokeAt = Time.time;
                if (namedAt < 0f && BossFight.Current == (IBoss)hold)
                {
                    namedAt = Time.time;
                    Assert.IsTrue(hold.Framed, "the camera turned to it before its name comes up");
                }
            }
            yield return Walk(Flat(seal.transform.position + seal.transform.forward * 3f), Waking);
            for (float t = 0f; t < 2.5f; t += Time.deltaTime)
            {
                Waking();
                yield return null;
            }
            Assert.Greater(wokeAt, 0f, "it wakes as you come in");
            Assert.Greater(namedAt, 0f, "its name up");
            Assert.GreaterOrEqual(namedAt - wokeAt, hold.namedAfter - 0.05f, "its name not up before its face has formed");
            Assert.IsFalse(hold.ArmsHidden, "its arms there to see");
            Assert.IsTrue(seal.Shut, "the way in sealed");
            Assert.Greater(seal.Raised, 0.99f, "heaved up");
            Assert.Greater(stone.bounds.max.y, 1.4f, "its stone up across the way");
            Vector2 behind = Flat(seal.transform.position) - Flat(seal.transform.forward) * 4f;
            for (float t = 0f; t < 3f; t += Time.deltaTime)
            {
                var d = behind - Flat(apprentice.transform.position);
                input.Move = d.normalized;
                yield return null;
            }
            input.Move = Vector2.zero;
            Assert.Greater(Vector3.Dot(apprentice.transform.position - seal.transform.position, seal.transform.forward), 0f, "held in: no way back out over it");
        }

        // Beaten (struck wherever it's open: the fight itself is the Hold lab's), it lets go: the seal sinks back into the
        // floor, and its mouth is open on the way out: through it, up the tunnel behind, into the cleft where the day falls.
        [UnityTest]
        public IEnumerator TheHold_Beaten_TheSealSinks_AndYouWalkOutThroughItsMouth()
        {
            var hold = UnityEngine.Object.FindAnyObjectByType<TheHoldWall>();
            var seal = UnityEngine.Object.FindAnyObjectByType<HoldSeal>();
            Vector3 stand = hold.arenaCentre + Vector3.back * 3f + Vector3.up * 0.05f;
            apprentice.Teleport(stand);
            apprentice.maxHealth = 100000f; // (whatever it lands on you: the fight itself is the Hold lab's)
            apprentice.RestoreFully();
            for (float t = 0f; t < 90f && hold.IsAlive; t += Time.deltaTime)
            {
                foreach (var part in new[] { hold.handL, hold.handR, hold.face })
                    if (part.Open)
                        part.TakeHit(new Hit { Damage = 200f, Stagger = 0f, From = apprentice.transform.position, Knockback = Vector3.zero });
                if (Vector3.Distance(apprentice.transform.position, stand) > 2f)
                    apprentice.Teleport(stand); // (stood still: the test only strikes)
                yield return null;
            }
            Assert.IsFalse(hold.IsAlive, $"beaten (it {hold.State}, you {(apprentice.IsAlive ? "alive" : "dead")})");
            Assert.IsTrue(BossFall.Framing, "its fall made a moment of (time slowed, the camera on it)");
            for (float t = 0f; t < 8f && hold.State != TheHoldWall.Mood.Gone; t += Time.deltaTime)
                yield return null;
            yield return new WaitForSeconds(3f);
            Assert.IsFalse(seal.Shut, "the way in open again");
            Assert.Less(seal.Raised, 0.01f, "the seal sunk back into the floor");
            float mouthE = hold.transform.position.x, outAt = hold.transform.position.z + 4f;
            Vector3 last = apprentice.transform.position;
            float stuck = 0f;
            for (float t = 0f; t < 20f && apprentice.transform.position.z < outAt; t += Time.deltaTime)
            {
                Vector3 p = apprentice.transform.position;
                input.Move = new Vector2(Mathf.Clamp((mouthE - p.x) * 0.8f, -0.7f, 0.7f), 1f).normalized; // (up the middle)
                stuck = (p - last).magnitude < 0.002f ? stuck + Time.deltaTime : 0f;
                last = p;
                if (stuck > 1f)
                {
                    string what = Physics.SphereCast(p + Vector3.up, 0.3f, Vector3.forward, out var hit, 2f) ? $"{hit.collider.name} at {hit.point}" : "nothing ahead";
                    Assert.Fail($"stuck at {p}: {what}");
                }
                yield return null;
            }
            input.Move = Vector2.zero;
            Assert.GreaterOrEqual(apprentice.transform.position.z, outAt - 0.1f, $"out through its mouth, up into the cleft (got to {apprentice.transform.position})");
        }

        // The side way to the grotto: the Warm Stone (more health, for good), and the door of eyes: it won't open; every
        // eye in it looks at you; it says its piece.
        [UnityTest]
        public IEnumerator TheSideWay_TheWarmStone_AndTheDoorOfEyes_WhichWontOpen()
        {
            var hud = UnityEngine.Object.FindAnyObjectByType<Hud>();
            var stone = UnityEngine.Object.FindObjectsByType<UpgradePiece>(FindObjectsSortMode.None).First(p => p.kind == UpgradePiece.Kind.WarmStone);
            var door = UnityEngine.Object.FindAnyObjectByType<EyeDoor>();
            var way = TheWay();
            var side = Paths().Where(p => p != way).OrderBy(p => Vector2.Distance(p.Last(), Flat(stone.transform.position))).First();
            // (from the way, where the side way leaves it)
            apprentice.Teleport(new Vector3(side[0].x, 0.6f, side[0].y));
            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(door.eyes.Any(e => e.Open), "its eyes shut till you come");
            foreach (var p in side.Skip(1))
                yield return Walk(p, null, 1.0f);
            float health = apprentice.maxHealth;
            yield return Walk(Flat(stone.transform.position) + Vector2.down * 0.6f);
            Assert.AreEqual(stone, apprentice.Nearby, "the Warm Stone in reach");
            yield return Interact();
            yield return new WaitForSeconds(2f);
            Assert.IsTrue(stone.Taken, "taken");
            Assert.Greater(apprentice.maxHealth, health, "more health, for good");
            // The door.
            yield return Walk(Flat(door.InteractPosition));
            yield return new WaitForSeconds(1.5f);
            Assert.GreaterOrEqual(door.eyes.Count(e => e.Open), 4, "its eyes open as you come near");
            Assert.AreEqual(door, apprentice.Nearby, "the door in reach");
            yield return Interact();
            Assert.AreEqual(1, door.Tried, "tried");
            Assert.AreEqual("...", hud.FoundBanner.Text.text, "it says its piece");
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(door.eyes.All(e => e.Open), "every eye on you");
            Assert.Greater(Vector3.Dot(apprentice.transform.position - door.transform.position, door.transform.forward), 0.5f, "still this side of it: it won't open");
        }

        // The shortcut back to the Mooring post: barred from the post's side; opened from the antechamber's, for good.
        [UnityTest]
        public IEnumerator TheShortcut_IsBarred_FromThePost_AndOpens_FromTheAntechamber()
        {
            var gate = UnityEngine.Object.FindAnyObjectByType<ShortcutGate>();
            Vector3 from = gate.openFrom.normalized;
            apprentice.Teleport(gate.transform.position - from * 1.3f + Vector3.up * 0.1f);
            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual(gate, apprentice.Nearby, "the gate in reach (the post's side)");
            Assert.AreEqual("Barred", gate.PromptLabel);
            yield return Interact();
            yield return new WaitForSeconds(1f);
            Assert.IsFalse(gate.IsOpen, "barred from here");
            // (and no way round it)
            Vector3 was = apprentice.transform.position;
            for (float t = 0f; t < 2.5f; t += Time.deltaTime)
            {
                input.Move = new Vector2(from.x, from.z);
                yield return null;
            }
            input.Move = Vector2.zero;
            Assert.Less(Vector3.Dot(apprentice.transform.position - gate.transform.position, from), 0.2f, "it holds the way");
            apprentice.Teleport(gate.transform.position + from * 1.3f + Vector3.up * 0.1f);
            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual("Open", gate.PromptLabel);
            yield return Interact();
            yield return new WaitForSeconds(1.5f);
            Assert.IsTrue(gate.IsOpen, "opened from the antechamber");
            yield return Walk(Flat(gate.transform.position - from * 3f));
        }

        // (SHOTS=dir) The words over the cave, with the UI (as PlayCaptureTests.Capture_Ui): the voice's first line and its
        // controls, low; then a kind of Soured written in the Ledger (its name in the Info colour).
        [UnityTest]
        public IEnumerator Capture_TheVoice_AndTheLedgerLine()
        {
            string outDir = Environment.GetEnvironmentVariable("SHOTS");
            if (string.IsNullOrEmpty(outDir))
                Assert.Ignore("set SHOTS to capture");
            Directory.CreateDirectory(outDir);
            var cam = Camera.main;
            var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            cam.targetTexture = rt;
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
            }
            void Shot(string name)
            {
                var previous = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                RenderTexture.active = previous;
                File.WriteAllBytes(Path.Combine(outDir, $"words-{name}.png"), tex.EncodeToPNG());
                UnityEngine.Object.Destroy(tex);
            }
            try
            {
                var first = UnityEngine.Object.FindObjectsByType<VoiceLine>(FindObjectsSortMode.None)
                    .OrderBy(v => Vector3.Distance(v.transform.position, apprentice.transform.position)).First();
                yield return Walk(Flat(first.transform.position), null, 1.2f);
                yield return new WaitForSeconds(1.6f);
                Shot("1-the-voice");
                yield return new WaitForSeconds(5f);
                apprentice.Equipment.Record(LedgerEntry.TheKept);
                yield return new WaitForSeconds(1.2f);
                Shot("2-written-in-the-ledger");
            }
            finally
            {
                cam.targetTexture = null;
                rt.Release();
            }
        }

        // (SHOTS=dir) The opening, from the game camera (no UI): the dark; the lantern lit; reaching for it; kneeling; risen.
        [UnityTest]
        public IEnumerator TheOpening_Captured()
        {
            string outDir = Environment.GetEnvironmentVariable("SHOTS");
            if (string.IsNullOrEmpty(outDir))
                Assert.Ignore("set SHOTS to capture");
            Directory.CreateDirectory(outDir);
            var opening = UnityEngine.Object.FindAnyObjectByType<TheOpening>();
            var cam = Camera.main;
            void Shot(string name)
            {
                var rt = new RenderTexture(1600, 900, 24);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                File.WriteAllBytes(Path.Combine(outDir, $"opening-{name}.png"), tex.EncodeToPNG());
                cam.targetTexture = null;
                RenderTexture.active = null;
                UnityEngine.Object.Destroy(tex);
                rt.Release();
            }
            yield return new WaitForSeconds(1.5f);
            Shot("1-dark");
            while (opening.Current != TheOpening.Stage.Bidden)
                yield return null;
            yield return new WaitForSeconds(0.5f);
            Shot("2-the-lantern-lit");
            input.PressInteract();
            yield return new WaitForSeconds(0.75f);
            Shot("3-reaching");
            yield return new WaitForSeconds(1.3f);
            Shot("4-kneeling");
            yield return new WaitForSeconds(1.6f);
            Shot("5-risen");
            yield return new WaitForSeconds(3f);
            Shot("6-the-cave-back");
        }

        // (SHOTS=dir) The way, from the game camera: the start, the first message, the post, the far side, behind the
        // Boss's wall, the grotto and its door, the chamber.
        [UnityTest]
        public IEnumerator Capture_TheWay()
        {
            string outDir = Environment.GetEnvironmentVariable("SHOTS");
            if (string.IsNullOrEmpty(outDir))
                Assert.Ignore("set SHOTS to capture");
            Directory.CreateDirectory(outDir);
            var cam = Camera.main;
            void Shot(string name)
            {
                var rt = new RenderTexture(1600, 900, 24);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                File.WriteAllBytes(Path.Combine(outDir, $"level-{name}.png"), tex.EncodeToPNG());
                cam.targetTexture = null;
                RenderTexture.active = null;
                UnityEngine.Object.Destroy(tex);
                rt.Release();
            }
            IEnumerator At(Vector2 p, string name, float settle = 1.2f)
            {
                apprentice.Teleport(new Vector3(p.x, 0.6f, p.y));
                yield return new WaitForSeconds(settle);
                Shot(name);
            }
            var way = TheWay();
            yield return new WaitForSeconds(1.5f);
            Shot("01-start");
            for (int i = 4; i < way.Count - 3; i += 5)
                yield return At(way[i], $"{i + 2:00}-way-{i}");
            var door = UnityEngine.Object.FindAnyObjectByType<EyeDoor>();
            yield return At(Flat(door.transform.position + door.transform.forward * 6f), "60-the-grotto");
            yield return At(Flat(door.InteractPosition), "61-the-door-of-eyes", 2.5f);
            var gate = UnityEngine.Object.FindAnyObjectByType<ShortcutGate>();
            yield return At(Flat(gate.transform.position - gate.openFrom.normalized * 2.5f), "65-the-shortcut-gate");
            yield return At(Flat(gate.transform.position + gate.openFrom.normalized * 2.5f), "66-the-shortcut-gate-behind");
            var hold = UnityEngine.Object.FindAnyObjectByType<TheHoldWall>();
            yield return At(Flat(hold.arenaCentre) + Vector2.down * 11f, "70-the-antechamber");
            yield return At(Flat(hold.arenaCentre) + Vector2.down * 3f, "71-the-hold-wakes", hold.namedAfter + 0.3f);
            var seal = UnityEngine.Object.FindAnyObjectByType<HoldSeal>();
            yield return At(Flat(seal.transform.position + seal.transform.forward * 1.8f), "72-sealed-in", 0.6f);
            yield return At(Flat(hold.arenaCentre) + Vector2.down * 3f, "73-the-fight", 3f);
        }

        static Vector2 Flat(Vector3 v) => new Vector2(v.x, v.z);

        static int Nearest(List<Vector2> way, Vector2 p)
        {
            int best = 0;
            for (int i = 1; i < way.Count; i++)
                if (Vector2.Distance(way[i], p) < Vector2.Distance(way[best], p))
                    best = i;
            return best;
        }
    }
}
