using System.Collections.Generic;
using System.IO;
using System.Linq;
using MyGame;
using UnityEditor;
using UnityEngine;

// The Hold, the level (the game's first), as the user laid it out in the level editor (Levels/the_hold_spiral.json:
// tools/level-editor.sh; CLAUDE.md): one winding way spiralling in to the Boss's chamber at its middle, a side way to a
// grotto (a Warm Stone, and the door of eyes: shut, for later), a Mooring post on the way and a shortcut back to it from
// the antechamber, the tutorial's messages on the floor. hold_drawn.py grows hold.py's living cave round its floors (the
// walls leaning, folding, breathing, the kept pressing out of them; the Boss's wall, the tunnel out behind its face, a
// door's wall built and flat) and says where it put the set pieces (Levels/the_hold_spiral.spots.json): the eyes in their
// sockets' holes, the faces and hands pressing out, the wall each of the Kept tears out of, the Boss's wall, the way out,
// the door's wall, the way across each gate. The rest stands where its marker is.
public static partial class MainSceneBuilder
{
    const string HoldLayoutLabPath = "Assets/_Project/Scenes/Lab_HoldLayout.unity";
    const string HoldLayoutData = "Assets/_Project/Levels/the_hold_spiral.json";

    // The level (format "level/2", metres: e east, n north; its floors are the cave builder's, not read here).
    [System.Serializable] sealed class LevelMarker { public string code, label, note; public float e, n; }
    [System.Serializable] sealed class LevelData { public string format, name; public float scale = 1f; public LevelMarker[] markers; }
    // Where hold_drawn.py put the set pieces in its cave (its metres: the scene's; e, n, u).
    [System.Serializable] sealed class LevelSpot { public string what; public float[] at, normal; }
    [System.Serializable] sealed class LevelKept { public float[] spot, foot, @out; }
    [System.Serializable] sealed class LevelDoor { public float[] foot, @out; }
    [System.Serializable] sealed class LevelGate { public string code; public float[] spot, a, b; }
    [System.Serializable] sealed class LevelBoss { public float[] wall, chamber, way_out; }
    [System.Serializable] sealed class LevelSpots
    {
        public LevelSpot[] eyes, presences; public LevelKept[] kept; public LevelDoor[] doors; public LevelGate[] gates;
        public LevelBoss boss; public float[] gleams, extent;
    }

    // The door of eyes: its eyes' places on its leaves (x across from the middle, u up) and their scale over the Hold's
    // eye parts (hold_pieces.py's EYE_DOOR_EYES).
    static readonly (float x, float u, float scale)[] EyeDoorEyes =
        { (0f, 2.6f, 3.0f), (-1.2f, 4.25f, 1.8f), (1.25f, 4.3f, 1.7f), (-1.35f, 1.0f, 1.6f), (1.4f, 0.95f, 1.75f) };

    [MenuItem("Assets/MyGame/Build Lab (The Hold, the level)")]
    static void BuildHoldLayoutLabFromMenu()
    {
        if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        BuildHoldLayoutLab();
    }

    public static void BuildHoldLayoutLabFromCommandLine() => BuildHoldLayoutLab();

    static void BuildHoldLayoutLab()
    {
        var data = JsonUtility.FromJson<LevelData>(File.ReadAllText(HoldLayoutData));
        var spots = JsonUtility.FromJson<LevelSpots>(File.ReadAllText(HoldLayoutData.Replace(".json", ".spots.json")));
        var scene = BeginScene(new Vector3(0f, -60f, 0f), HoldNight); // (no water to be seen: the dark outside the walls)
        var root = new GameObject("The Hold").transform;
        float scale = data.scale > 0f ? data.scale : 1f;
        const float Floor = 0.5f;
        Vector3 At(LevelMarker m) => new Vector3(m.e * scale, Floor, m.n * scale);
        Vector3 V(float[] a) => new Vector3(a[0], a[2], a[1]);          // (e, n, u: the scene's x, z, y)
        Vector3 Flat(float[] a) => new Vector3(a[0], Floor, a[1]);     // (e, n on the floor)
        IEnumerable<LevelMarker> Markers(string code) => data.markers.Where(m => m.code == code);

        // The cave, grown round the level's floors (hold_drawn.py): its living walls breathe.
        var cave = Place("Environment/hold_drawn", Vector3.zero, 0f);
        cave.transform.SetParent(root, true);
        var living = new GameObject("The Living Walls").AddComponent<LivingWalls>();
        living.transform.SetParent(root, true);
        living.inSound = Clips("hold_breath_in");
        living.outSound = Clips("hold_breath_out");

        // Eyes in their sockets' holes, faces and hands pressing out, where the cave put them.
        foreach (var eye in spots.eyes)
            WallEyeAt(root, V(eye.at), V(eye.normal));
        int kind = 0;
        foreach (var p in spots.presences)
            PresenceAt(root, V(p.at), V(p.normal), p.what == "face" ? new[] { "face_1", "face_2", "face_3" }[kind++ % 3] : new[] { "hand_1", "hand_2" }[kind++ % 2]);

        // The Kept: each torn out of the wall nearest its marker as you pass it; those close together come out together.
        var group = new int[spots.kept.Length];
        int groups = 0;
        for (int i = 0; i < spots.kept.Length; i++)
        {
            if (group[i] != 0)
                continue;
            group[i] = ++groups;
            for (int j = i + 1; j < spots.kept.Length; j++)
                if (group[j] == 0 && new Vector2(spots.kept[i].spot[0] - spots.kept[j].spot[0], spots.kept[i].spot[1] - spots.kept[j].spot[1]).magnitude < 7f)
                    group[j] = group[i];
        }
        for (int i = 0; i < spots.kept.Length; i++)
        {
            var k = spots.kept[i];
            var foot = new Vector3(k.foot[0], k.foot[2], k.foot[1]);
            var outward = new Vector3(k.@out[0], 0f, k.@out[1]).normalized;
            var one = Kept("The Kept", foot + outward * 1.1f, foot + outward * 0.2f, group[i]);
            one.transform.SetParent(root, true);
            one.wakeWithin = Mathf.Max(one.wakeWithin, new Vector2(k.foot[0] - k.spot[0], k.foot[1] - k.spot[1]).magnitude + 3f);
        }

        // Mooring posts (Rest, and its lantern is lit); the finds.
        foreach (var m in Markers("MP"))
            MooringPostAt(At(m), 180f).transform.SetParent(root, true);
        foreach (var m in Markers("WS"))
            UpgradePieceAt(UpgradePiece.Kind.WarmStone, At(m), 0f).transform.SetParent(root, true);
        foreach (var m in Markers("OB"))
        {
            var cache = Place("Environment/obol_cache", At(m), 30f);
            cache.name = "Obol Cache";
            cache.transform.SetParent(root, true);
            cache.AddComponent<ObolCache>();
            var glow = cache.AddComponent<PickupGlow>();
            glow.tier = PickupGlow.Tier.Item;
            glow.colour = LanternLight;
            glow.height = 0.7f;
        }

        // The tutorial's messages on the floor: the genre's template, then the dry line ("Try dodging / {dodge} to...").
        foreach (var m in Markers("MSG"))
        {
            string label = m.label ?? "";
            int cut = label.IndexOf(" / ", System.StringComparison.Ordinal);
            FloorMessageAt(root, At(m), cut < 0 ? label : label.Substring(0, cut), cut < 0 ? "" : label.Substring(cut + 3));
        }

        // The Boss in its chamber's north wall; the arena sealed behind you while it fights; the way out behind its face.
        TheHoldWall boss = null;
        if (spots.boss != null && spots.boss.wall != null && spots.boss.wall.Length == 3)
        {
            Vector3 wall = new Vector3(spots.boss.wall[0], Floor, spots.boss.wall[1]);
            boss = TheHoldBoss(root, wall);
            boss.armsHiddenBeyond = spots.boss.chamber[2] + 3f;
            Vector3 arena = boss.arenaCentre;
            PointLight(root, arena + new Vector3(0f, 4f, 0f), 16f, 5f, Hex("#66708a")).shadows = LightShadows.None;
            PointLight(root, wall + new Vector3(0f, 2.5f, -5f), 14f, 6f, Hex("#7d88a3")).shadows = LightShadows.None;
            // The way out: through its mouth once it's let go, up the tunnel to the cleft, the grey day falling into it.
            Vector3 opening = V(spots.boss.way_out);
            var day = PointLight(root, opening + new Vector3(0f, 3f, 0f), 9f, 4f, HoldDaylight);
            day.shadows = LightShadows.Soft;
            LightShaft(root, opening, 4f, 1.0f, brightness: 3f);
            var wind = new GameObject("The Day Outside (the way out)", typeof(AudioSource)).GetComponent<AudioSource>();
            wind.transform.SetParent(root, true);
            wind.transform.position = opening + Vector3.up * 1.5f;
            wind.clip = Clips("hold_hatch") is var hatch && hatch.Length > 0 ? hatch[0] : null;
            wind.loop = true;
            wind.playOnAwake = true;
            wind.spatialBlend = 1f;
            wind.rolloffMode = AudioRolloffMode.Linear;
            wind.minDistance = 3f;
            wind.maxDistance = 18f;
            wind.volume = 0.4f;
            wind.dopplerLevel = 0f;
        }
        foreach (var g in spots.gates)
        {
            Vector3 a = Flat(g.a), b = Flat(g.b), mid = (a + b) / 2f, across = b - a;
            float width = across.magnitude;
            Vector3 dir = across / width, through = Vector3.Cross(dir, Vector3.up); // (along the way)
            Vector3 towardsBoss = boss != null ? boss.arenaCentre - mid : Vector3.forward;
            if (Vector3.Dot(through, towardsBoss) < 0f)
                through = -through;
            if (g.code == "AG" && boss != null)
                HoldSealAt(root, cave.transform, mid, through, width, boss);
            else if (g.code == "SG")
                HoldShortcutAt(root, mid, dir, through, width);
        }

        // The door of eyes, in its built wall.
        foreach (var d in spots.doors)
            EyeDoorAt(root, V(d.foot), new Vector3(d.@out[0], 0f, d.@out[1]).normalized);

        // Drips all through it, the rock settling far off, ash in the dark.
        float minE = spots.extent[0], maxE = spots.extent[1], minN = spots.extent[2], maxN = spots.extent[3];
        Vector3 middle = new Vector3((minE + maxE) / 2f, Floor, (minN + maxN) / 2f);
        var drips = new GameObject("Drips").AddComponent<Drips>();
        drips.transform.SetParent(root, true);
        drips.transform.position = middle;
        drips.size = new Vector3(maxE - minE, 0f, maxN - minN);
        drips.everySecond = 0.5f;
        drips.from = 6f;
        drips.dripSound = Clips("hold_drip");
        HoldAsh(root, middle + Vector3.up * 8.5f, new Vector3(maxE - minE, 1f, maxN - minN));
        var groans = new GameObject("The Rock Settling").AddComponent<AmbientOneShots>();
        groans.transform.SetParent(root, true);
        groans.transform.position = middle + Vector3.up * 6f;
        groans.clips = Clips("hold_creak");
        groans.every = new Vector2(12f, 26f);
        groans.spread = 30f;
        groans.volume = 0.5f;

        // A cold gleam here and there along the way (the walls' shapes, no more): every 12 m or so.
        for (int i = 0; i + 1 < spots.gleams.Length; i += 2)
            PointLight(root, new Vector3(spots.gleams[i], 3.6f, spots.gleams[i + 1]), 11f, 14f, Hex("#7d879e")).shadows = LightShadows.None;

        // Where you begin; the lab's post beside it.
        var startMarker = Markers("START").FirstOrDefault() ?? new LevelMarker();
        var start = new GameObject("Start Point").transform;
        start.position = At(startMarker);
        var entrance = new GameObject("Area Entrance").AddComponent<AreaEntrance>();
        entrance.transform.position = start.position + Vector3.up;
        entrance.areaName = "The Hold";
        var tester = TesterPostAt(start.position + new Vector3(1.8f, 0f, 0.8f), 200f);
        tester.spawner = LabTemplates();
        var apprentice = Apprentice(start.position);
        var bow = apprentice.GetComponent<BowLantern>();
        bow.minRange = 7.5f;
        bow.minIntensity = 3f;
        apprentice.GetComponent<Equipment>().startingCharms = new[] { Charm.TestersKnot.Id };
        var readout = new GameObject("Lab Readout").AddComponent<LabReadout>();
        readout.font = AssetDatabase.LoadAssetAtPath<Font>(Fonts + "IMFellEnglish-Regular.ttf");
        tester.readout = readout;
        Debug.Log($"[MainSceneBuilder] The Hold, the level ({data.name}): {data.markers.Length} markers, {spots.kept.Length} Kept in {groups} groups, " +
                  $"{spots.eyes.Length} eyes, {spots.presences.Length} presences, {spots.gates.Length} gates, {spots.doors.Length} doors");

        FinishScene(scene, HoldLayoutLabPath, start, apprentice, "music_hold");
    }

    // The arena's seal: the cave's own ridge of the living stone across the way in (hold_drawn.py's HoldSeal), sunk in the
    // floor till the Hold wakes with you inside.
    static void HoldSealAt(Transform parent, Transform cave, Vector3 mid, Vector3 into, float width, TheHoldWall boss)
    {
        var seal = new GameObject("The Seal (the arena's)").AddComponent<HoldSeal>();
        seal.transform.SetParent(parent, true);
        seal.transform.SetPositionAndRotation(mid, Quaternion.LookRotation(into, Vector3.up));
        seal.hold = boss;
        seal.wallSize = new Vector3(width + 1.2f, 3f, 1.6f);
        var ridge = new GameObject("Ridge").transform;
        ridge.SetParent(seal.transform, false);
        var stone = cave.Find("HoldSeal");
        if (stone != null)
        {
            stone.SetParent(ridge, true);
            foreach (var c in stone.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(c); // (its unseen wall is what holds the way)
            stone.gameObject.AddComponent<NavMeshIgnore>();
        }
        seal.ridge = ridge;
        seal.riseSound = Clips("hold_stone_press");
        seal.sinkSound = Clips("hold_creak");
    }

    // A shortcut: a barred gate across the way, opened only from the side nearer the Boss (the way you fought through),
    // the rock either side closed to it (the way is wider than the gate).
    static void HoldShortcutAt(Transform parent, Vector3 mid, Vector3 across, Vector3 through, float width)
    {
        float yaw = Mathf.Atan2(across.x, across.z) * Mathf.Rad2Deg - 90f;
        var go = Place("Environment/shortcut_gate", mid, yaw);
        go.name = "Shortcut Gate";
        go.transform.SetParent(parent, true);
        go.AddComponent<ShortcutGate>().openFrom = through;
        const float GateHalf = 1.08f;
        foreach (float side in new[] { -1f, 1f })
        {
            float reach = width / 2f + 0.8f - GateHalf;
            var block = new GameObject("Shortcut Gate (the rock beside it)").AddComponent<BoxCollider>();
            block.transform.SetParent(go.transform, true);
            block.transform.SetPositionAndRotation(mid + across * side * (GateHalf + reach / 2f) + Vector3.up * 1.5f, Quaternion.LookRotation(through, Vector3.up));
            block.size = new Vector3(reach, 3f, 0.6f);
        }
    }

    // The door of eyes (hold_pieces' eye_door) at its wall's foot, out of the wall along `outward`: its great eyes set in
    // its leaves (the Hold's eye parts, many times over), shut till you come near.
    static EyeDoor EyeDoorAt(Transform parent, Vector3 foot, Vector3 outward)
    {
        foot += outward * 0.5f; // (its built wall, 0.45 m thick, stood out in front of the living one: not sunk in it)
        var door = new GameObject("The Door of Eyes").AddComponent<EyeDoor>();
        door.transform.SetParent(parent, true);
        door.transform.SetPositionAndRotation(foot, Quaternion.LookRotation(outward, Vector3.up));
        var model = Place("Environment/hold_pieces_eye_door", foot, 0f);
        model.transform.rotation = Quaternion.LookRotation(outward, Vector3.up); // (its face, game north as built, turned out of the wall)
        model.transform.SetParent(door.transform, true);
        Vector3 side = Vector3.Cross(Vector3.up, outward);
        var eyes = new List<WallEye>();
        foreach (var (x, u, s) in EyeDoorEyes)
        {
            var eye = WallEyeAt(door.transform, foot + side * x + Vector3.up * u - outward * 0.04f, outward, s, 14f);
            eye.openWithin = 9f;
            eye.shutBeyond = 13f;
            eyes.Add(eye);
        }
        door.eyes = eyes.ToArray();
        door.triedSound = Clips("hold_eye_open");
        PointLight(door.transform, foot + outward * 2.4f + Vector3.up * 2.6f, 8f, 10f, Hex("#8590a8")).shadows = LightShadows.None;
        return door;
    }

    // A message on the floor: a faint warm scrawl you stand on and read (FloorMessage).
    static void FloorMessageAt(Transform parent, Vector3 at, string message, string line)
    {
        var go = new GameObject("Message: " + message);
        go.transform.SetParent(parent, true);
        go.transform.position = at;
        var msg = go.AddComponent<FloorMessage>();
        msg.message = message;
        msg.line = line;
        var scrawl = GameObject.CreatePrimitive(PrimitiveType.Quad);
        scrawl.name = "Scrawl";
        Object.DestroyImmediate(scrawl.GetComponent<Collider>());
        scrawl.transform.SetParent(go.transform, false);
        scrawl.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        scrawl.transform.localRotation = Quaternion.Euler(90f, Mathf.Repeat(at.x * 41f + at.z * 13f, 30f) - 15f, 0f);
        scrawl.transform.localScale = new Vector3(1.6f, 0.8f, 1f);
        var mat = ParticleMaterial("M_FloorMessage", ScrawlTexture("T_Scrawl"));
        scrawl.GetComponent<MeshRenderer>().sharedMaterial = mat;
        scrawl.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        msg.glow = scrawl.GetComponent<MeshRenderer>();
        var light = PointLight(go.transform, at + Vector3.up * 0.4f, 2.2f, 0.8f, Hex("#ff9a52"));
        Flicker(light, 0.15f, 1.5f, 0.01f);
        go.AddComponent<NavMeshIgnore>();
    }

    // A warm scrawl, as if written on the stone with a fingertip of light: a few lines of loops and strokes.
    static Texture2D ScrawlTexture(string name)
    {
        string path = $"{Generated}/{name}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null)
            return existing;
        const int w = 256, h = 128;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color[w * h];
        var rng = new System.Random(11);
        void Dot(float x, float y, float r, float a)
        {
            for (int j = (int)(y - r - 1); j <= (int)(y + r + 1); j++)
                for (int i = (int)(x - r - 1); i <= (int)(x + r + 1); i++)
                {
                    if (i < 0 || j < 0 || i >= w || j >= h) continue;
                    float d = Mathf.Sqrt((i - x) * (i - x) + (j - y) * (j - y));
                    float v = Mathf.Clamp01(1f - d / r) * a;
                    var c = px[j * w + i];
                    px[j * w + i] = new Color(1f, 0.62f, 0.3f, Mathf.Max(c.a, v));
                }
        }
        for (int row = 0; row < 3; row++)
        {
            float y0 = 30f + row * 34f, x = 22f + (float)rng.NextDouble() * 10f;
            float end = w - 22f - (row == 2 ? 70f : (float)rng.NextDouble() * 20f);
            while (x < end)
            {
                // A "letter": a short squiggle, then a gap now and then (words).
                float len = 6f + (float)rng.NextDouble() * 10f, phase = (float)rng.NextDouble() * 6f, amp = 4f + (float)rng.NextDouble() * 7f;
                for (float t = 0f; t < len; t += 0.5f)
                    Dot(x + t, y0 + Mathf.Sin(phase + t * 0.7f) * amp, 1.6f, 1f);
                x += len + 2f + (rng.NextDouble() < 0.25 ? 10f : 0f);
            }
        }
        // A soft halo under it.
        for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
            {
                float dx = (i - w / 2f) / (w / 2f), dy = (j - h / 2f) / (h / 2f);
                float halo = Mathf.Clamp01(1f - (dx * dx + dy * dy)) * 0.18f;
                var c = px[j * w + i];
                px[j * w + i] = new Color(1f, 0.62f, 0.3f, Mathf.Max(c.a, halo));
            }
        tex.SetPixels(px);
        tex.Apply();
        AssetDatabase.CreateAsset(tex, path);
        return tex;
    }
}
