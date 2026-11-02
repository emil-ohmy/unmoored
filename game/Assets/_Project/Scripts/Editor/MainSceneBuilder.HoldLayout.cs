using System.Collections.Generic;
using System.IO;
using System.Linq;
using MyGame;
using UnityEditor;
using UnityEngine;

// The Hold as the user drew it (the Figma level-design file, exported to Levels/the_hold.json; docs: CLAUDE.md): a
// blockout of their layout, built from the drawing, not hold.py's sculpted cave. Its ground pieces become floors (+0.5)
// and the cave's walls (+4.8: rock, breathing like the Hold's); its markers its set pieces, each on the wall face
// nearest it (eyes, faces and hands pressing out); the Kept torn out of the wall nearest their spot; drips all through
// it; the lantern a Mooring post; its notes the start ("We begin here") and the tutorial's messages on the floor.
public static partial class MainSceneBuilder
{
    const string HoldLayoutLabPath = "Assets/_Project/Scenes/Lab_HoldLayout.unity";
    const string HoldLayoutData = "Assets/_Project/Levels/the_hold.json";

    [System.Serializable] sealed class LevelGround { public string kind; public float e0, e1, n0, n1; }
    [System.Serializable] sealed class LevelMarker { public string code, label; public float e, n; }
    [System.Serializable] sealed class LevelNote { public string text; public float e0, e1, n0, n1; }
    [System.Serializable] sealed class LevelData { public float width, height; public LevelGround[] ground; public LevelMarker[] markers; public LevelNote[] notes; }

    // The tutorial's messages: the genre's own templates, in the game's dry voice (docs/brief.md, Tone and voice).
    static readonly (string match, string message, string line)[] HoldMessages =
    {
        ("dodge", "Try dodging", "{dodge} to dodge, {light} to strike. You'll be doing a great deal of both. Mostly the first."),
        ("heal", "Be wary of dying", "{heal} to heal. You won't have many. You'll want more. Everyone does."),
        ("lantern", "Praise the lantern", "Rest at a Mooring post ({interact}) to light its lantern. Everything you scattered gets back up. That's the deal."),
    };

    [MenuItem("Assets/MyGame/Build Lab (The Hold, as drawn)")]
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
        var scene = BeginScene(new Vector3(0f, -60f, 0f), HoldNight); // (no water to be seen: the dark outside the walls)
        var root = new GameObject("The Hold (as drawn)").transform;

        // Where things are: the drawing's metres, the start (its "We begin here") at the origin.
        var startNote = data.notes.FirstOrDefault(n => n.text.ToLowerInvariant().Contains("begin"));
        Vector2 origin = startNote != null ? Into(data, new Vector2((startNote.e0 + startNote.e1) / 2f, (startNote.n0 + startNote.n1) / 2f), 0f)
                                           : new Vector2(data.width / 2f, -data.height / 2f);
        Vector3 W(Vector2 p, float u) => new Vector3(p.x - origin.x, u, p.y - origin.y);

        // The ground: floors, the cave's walls.
        var floorMat = GeneratedMaterial("M_LayoutFloor", "MyGame/Toon");
        floorMat.SetColor("_BaseColor", Hex("#5e5f64"));
        EditorUtility.SetDirty(floorMat);
        var wallMat = GeneratedMaterial("M_LayoutWall", "MyGame/Toon");
        wallMat.SetColor("_BaseColor", Hex("#4c4553"));
        wallMat.SetFloat("_Breathe", 1f);
        wallMat.SetVector("_EmissionColor", (Vector4)(Hex("#4c4553") * 0.4f));
        EditorUtility.SetDirty(wallMat);
        int k = 0;
        foreach (var g in data.ground)
        {
            float top = LevelTop(g.kind);
            bool wall = top > 3f;
            // (a wall south of every floor beside it: the camera looks over it from the south, so it's kept low, a rim)
            bool southOf = wall && Floors(data).Any(f => g.e0 < f.e1 && g.e1 > f.e0 && Mathf.Abs(g.n1 - f.n0) < 0.6f)
                                && !Floors(data).Any(f => g.e0 < f.e1 && g.e1 > f.e0 && g.n0 > f.n0 && g.n0 < f.n1);
            if (southOf)
                top = 1.2f;
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = (wall ? "Wall " : "Floor ") + g.kind;
            box.transform.SetParent(root, true);
            float lift = 0.004f * k++; // (no two tops in one plane: they'd flicker)
            float bottom = -1.5f;
            box.transform.position = W(new Vector2((g.e0 + g.e1) / 2f, (g.n0 + g.n1) / 2f), (bottom + top + lift) / 2f);
            box.transform.localScale = new Vector3(g.e1 - g.e0, top + lift - bottom, g.n1 - g.n0);
            box.GetComponent<MeshRenderer>().sharedMaterial = wall ? wallMat : floorMat;
        }
        var living = new GameObject("The Living Walls").AddComponent<LivingWalls>();
        living.transform.SetParent(root, true);
        living.inSound = Clips("hold_breath_in");
        living.outSound = Clips("hold_breath_out");

        // Its markers.
        int kind = 0, kept = 0;
        var keptSpots = data.markers.Where(m => m.code == "KP").OrderBy(m => m.n).ToList(); // (south first: the first met)
        foreach (var m in data.markers)
        {
            var at = new Vector2(m.e, m.n);
            switch (m.code)
            {
                case "EYE":
                case "FACE":
                case "HAND":
                {
                    // On the wall face nearest it, looking out into the cave.
                    var (face, normal) = WallFace(data, at);
                    float up = m.code == "EYE" ? 1.9f + 0.5f * Mathf.Repeat(m.e * 0.37f + m.n * 0.11f, 1f) : 1.3f + 0.6f * Mathf.Repeat(m.e * 0.23f + m.n * 0.29f, 1f);
                    Vector3 wallAt = W(face, 0.5f + up) - new Vector3(normal.x, 0f, normal.y) * 0.02f;
                    var n3 = new Vector3(normal.x, 0f, normal.y);
                    if (m.code == "EYE")
                        WallEyeAt(root, wallAt + n3 * 0.06f, n3);
                    else
                        PresenceAt(root, wallAt, n3, m.code == "FACE" ? new[] { "face_1", "face_2", "face_3" }[kind++ % 3] : new[] { "hand_1", "hand_2" }[kind++ % 2]);
                    break;
                }
                case "KP":
                {
                    // Torn out of the wall nearest its spot; the first met on their own, the rest together.
                    var (face, outward) = NearestWall(data, at);
                    int group = keptSpots.IndexOf(m) == 0 ? 1 : 2;
                    var o3 = new Vector3(outward.x, 0f, outward.y);
                    var one = Kept("The Kept", W(face, 0.5f) + o3 * 1.1f, W(face, 0.5f) + o3 * 0.2f, group);
                    one.transform.SetParent(root, true);
                    one.wakeWithin = Mathf.Max(one.wakeWithin, (face - at).magnitude + 4f); // (it tears free as you pass the spot drawn)
                    kept++;
                    break;
                }
                case "LT":
                {
                    // The lantern: a Mooring post (Rest, and its lantern is lit).
                    var spot = Into(data, at, 1.2f);
                    MooringPostAt(W(spot, 0.5f), 270f);
                    break;
                }
            }
        }

        // Drips all through it (the markers say so: all around), the rock settling far off, ash in the dark.
        float minE = data.ground.Min(g => g.e0), maxE = data.ground.Max(g => g.e1), minN = data.ground.Min(g => g.n0), maxN = data.ground.Max(g => g.n1);
        Vector3 middle = W(new Vector2((minE + maxE) / 2f, (minN + maxN) / 2f), 0.5f);
        var drips = new GameObject("Drips").AddComponent<Drips>();
        drips.transform.SetParent(root, true);
        drips.transform.position = middle;
        drips.size = new Vector3(maxE - minE, 0f, maxN - minN);
        drips.everySecond = 0.4f;
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

        // A cold gleam here and there along it (the walls' shapes, no more): every 14 m or so over the floor.
        foreach (var g in data.ground.Where(g => LevelTop(g.kind) < 3f))
            for (float n = g.n0 + 5f; n < g.n1; n += 10f)
                PointLight(root, W(new Vector2((g.e0 + g.e1) / 2f, n), 5f), 14f, 2.4f, Hex("#6f7a96")).shadows = LightShadows.None;

        // Where the drawing ends (its far end, open): the way on, grey daylight beyond, and nothing to fall off into.
        var end = data.ground.Where(g => LevelTop(g.kind) < 3f).OrderByDescending(g => g.n1).First();
        Vector3 wayOn = W(new Vector2((end.e0 + end.e1) / 2f, end.n1), 0.5f);
        PointLight(root, wayOn + new Vector3(0f, 3f, 1.5f), 12f, 3f, HoldDaylight).shadows = LightShadows.Soft;
        LightShaft(root, wayOn + Vector3.back * 1.5f, 4f, 1.1f, brightness: 2.5f);
        var stop = new GameObject("The Way On (not drawn yet)").AddComponent<BoxCollider>();
        stop.transform.SetParent(root, true);
        stop.transform.position = wayOn + new Vector3(0f, 1.5f, 0.3f);
        stop.size = new Vector3(end.e1 - end.e0, 3f, 0.6f);

        // The tutorial's messages on the floor, where its notes are.
        foreach (var note in data.notes)
        {
            string lower = note.text.ToLowerInvariant();
            var text = HoldMessages.FirstOrDefault(t => lower.Contains(t.match));
            if (text.message == null)
                continue;
            var spot = Into(data, new Vector2(note.e0 + 1.5f, note.n1 - 1.5f), 0.8f);
            FloorMessageAt(root, W(spot, 0.5f), text.message, text.line);
        }

        // Where you begin; the lab's post beside it.
        var start = new GameObject("Start Point").transform;
        start.position = W(Vector2.zero + origin, 0.5f);
        var entrance = new GameObject("Area Entrance").AddComponent<AreaEntrance>();
        entrance.transform.position = start.position + Vector3.up;
        entrance.areaName = "The Hold";
        var tester = TesterPostAt(start.position + new Vector3(-3.2f, 0f, 1f), 120f);
        tester.spawner = LabTemplates();
        var apprentice = Apprentice(start.position);
        var bow = apprentice.GetComponent<BowLantern>();
        bow.minRange = 7.5f;
        bow.minIntensity = 3f;
        apprentice.GetComponent<Equipment>().startingCharms = new[] { Charm.TestersKnot.Id };
        var readout = new GameObject("Lab Readout").AddComponent<LabReadout>();
        readout.font = AssetDatabase.LoadAssetAtPath<Font>(Fonts + "IMFellEnglish-Regular.ttf");
        tester.readout = readout;
        Debug.Log($"[MainSceneBuilder] The Hold as drawn: {data.ground.Length} ground pieces, {data.markers.Length} markers ({kept} Kept), {data.notes.Length} notes");

        FinishScene(scene, HoldLayoutLabPath, start, apprentice, "music_hold");
    }

    // The height a ground piece's top stands at, from its name ("Ground +2.4 ...": 2.4).
    static float LevelTop(string kind)
    {
        var m = System.Text.RegularExpressions.Regex.Match(kind ?? "", @"\+(\d+(\.\d+)?)");
        return m.Success ? float.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : 0.5f;
    }

    static IEnumerable<LevelGround> Floors(LevelData d) => d.ground.Where(g => LevelTop(g.kind) < 3f);

    static bool OnFloor(LevelData d, Vector2 p) => Floors(d).Any(g => p.x > g.e0 && p.x < g.e1 && p.y > g.n0 && p.y < g.n1);

    // The point on the floor nearest p, `inset` in from its edge.
    static Vector2 Into(LevelData d, Vector2 p, float inset)
    {
        if (OnFloor(d, p))
            return p;
        Vector2 best = p;
        float bestD = float.MaxValue;
        foreach (var g in Floors(d))
        {
            var c = new Vector2(Mathf.Clamp(p.x, g.e0 + inset, g.e1 - inset), Mathf.Clamp(p.y, g.n0 + inset, g.n1 - inset));
            float dist = (c - p).sqrMagnitude;
            if (dist < bestD) { bestD = dist; best = c; }
        }
        return best;
    }

    // For something in a wall: the face of the wall nearest it (where the floor begins) and the way that face looks.
    static (Vector2 face, Vector2 normal) WallFace(LevelData d, Vector2 p)
    {
        Vector2 edge = Into(d, p, 0f);
        Vector2 normal = edge - p;
        if (normal.sqrMagnitude < 0.0001f) // (drawn on the floor: the wall nearest it instead)
        {
            var (face, outward) = NearestWall(d, p);
            return (face, outward);
        }
        // Straight off the face (the larger axis): not at a slant into a corner.
        normal = Mathf.Abs(normal.x) > Mathf.Abs(normal.y) ? new Vector2(Mathf.Sign(normal.x), 0f) : new Vector2(0f, Mathf.Sign(normal.y));
        return (edge, normal);
    }

    // For something on the floor: the nearest wall along the four ways, its face, and the way out of it (back to p).
    static (Vector2 face, Vector2 outward) NearestWall(LevelData d, Vector2 p)
    {
        Vector2 bestFace = p, bestOut = Vector2.right;
        float bestD = float.MaxValue;
        foreach (var dir in new[] { Vector2.right, Vector2.left, Vector2.up, Vector2.down })
        {
            float t = 0f;
            while (t < 60f && OnFloor(d, p + dir * (t + 0.1f)))
                t += 0.1f;
            if (t < bestD) { bestD = t; bestFace = p + dir * t; bestOut = -dir; }
        }
        return (bestFace, bestOut);
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
