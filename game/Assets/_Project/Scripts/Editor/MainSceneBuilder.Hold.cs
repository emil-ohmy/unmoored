using MyGame;
using UnityEditor;
using UnityEngine;

// The Hold's lab (the game's opening, before the Hub; docs/brief.md): a cave whose walls are made of the souls that
// were kept there (hold.py). They breathe (LivingWalls: every "..._Breathe" surface swells and sinks in time with the
// sound of it, ripples running in towards you); faces and hands press out of the stone and sink back (WallPresence);
// eyes in the walls open as you come and follow you (WallEye). Dark, eerie, desolate, dead: your lantern nearly the
// only light, black still water, mist, ash, drips, the Kept's eyes watching from the dark. Its spaces (a lab to play
// in, not the level): the Pool where you wake, the Breathing Passage, the Gallery of the Kept, the Black Water (the
// Mooring islet), and the Way Out: a climb to a crack of grey daylight.
public static partial class MainSceneBuilder
{
    const string HoldLabPath = "Assets/_Project/Scenes/Lab_Hold.unity";

    // The Hold: no moon to speak of, the dark close and black, the colour all but gone out of it (dead, not stopped:
    // not the Hall's grey photograph, but a place the life went out of), the grain heavy, the edges closing in.
    static readonly AreaNight HoldNight = new AreaNight
    {
        volume = "HoldVolume", moon = Hex("#7c8aa0"), moonIntensity = 0.26f, moonAngles = new Vector3(70f, -20f, 0f), shadowStrength = 0.6f,
        sky = Hex("#2a3142"), equator = Hex("#1b1f28"), ground = Hex("#0b0c0f"),
        fog = Hex("#080a0d"), fogStart = 12f, fogEnd = 36f,
        saturation = -32f, contrast = 18f, exposure = 0.35f, vignette = 0.48f, grain = 0.45f, filter = Hex("#e6edf6"),
    };
    static readonly Color HoldDaylight = Hex("#b9c3cf"); // (the grey of the day, far above)

    [MenuItem("Assets/MyGame/Build Lab (The Hold)")]
    static void BuildHoldLabFromMenu()
    {
        if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        BuildHoldLab();
    }

    public static void BuildHoldLabFromCommandLine() => BuildHoldLab();

    static void BuildHoldLab()
    {
        var scene = BeginScene(new Vector3(0f, 0f, 30f), HoldNight);
        var root = new GameObject("The Hold").transform;
        var hold = Place("Environment/hold_hold", Vector3.zero, 0f);
        hold.transform.SetParent(root, true);
        Vector3 At(float e, float n, float u = 0.5f) => new Vector3(e, u, n);

        // The walls breathe (every "..._Breathe" surface, in time with the sound of it), ripples running in towards you.
        var living = new GameObject("The Living Walls").AddComponent<LivingWalls>();
        living.transform.SetParent(root, true);
        living.inSound = Clips("hold_breath_in");
        living.outSound = Clips("hold_breath_out");

        // Eyes in the walls (they open as you come and follow you), and the kept pressing out of the stone.
        foreach (var (at, normal) in HoldEyeSpots)
            WallEyeAt(root, at, normal);
        int kind = 0;
        foreach (var (at, normal) in HoldPresenceSpots)
            PresenceAt(root, at, normal, PresenceKinds[kind++ % PresenceKinds.Length]);

        // The Kept: waiting in the walls at their places, torn free as you pass (in twos and threes).
        foreach (var (e, n, ne, nn, group) in HoldKeptSpots)
        {
            Vector3 wall = At(e, n), outward = new Vector3(ne, 0f, nn).normalized;
            var kept = Kept("The Kept", wall + outward * 1.1f, wall + outward * 0.2f, group); // (its back to the wall's face, drawn 0.9 m out: kept.py's Emerge)
            kept.transform.SetParent(root, true);
        }

        // A few dead lanterns, dropped along the way.
        foreach (var (e, n, yaw, lying) in new[] { (0.8f, 1.0f, 30f, true), (1.2f, 17f, 200f, false), (6.5f, 34f, 110f, true) })
        {
            var lantern = Place("Environment/hold_lantern_dead", At(e, n, 0.5f + (lying ? 0.085f : 0f)), yaw);
            lantern.transform.SetParent(root, true);
            if (lying) // (on its side)
                lantern.transform.rotation = Quaternion.AngleAxis(84f, Quaternion.Euler(0f, yaw, 0f) * Vector3.right) * lantern.transform.rotation;
        }

        // Light: next to none but your lantern. The way out: grey day pouring through the crack high in the north wall.
        var day = PointLight(root, HoldCrack - HoldCrackOut * 1.2f + Vector3.up * 1.8f, 14f, 5f, HoldDaylight); // (just inside the crack)
        day.shadows = LightShadows.Soft;
        LightShaft(root, HoldCrack + HoldCrackOut * 0.6f, 3.5f, 0.9f, brightness: 3f);
        var beyond = PointLight(root, HoldCrack - HoldCrackOut * 4.5f + Vector3.up * 2f, 10f, 4f, HoldDaylight); // (the day itself, past it)
        beyond.shadows = LightShadows.None;
        // Barely: a cold gleam high in the Gallery and the Passage (their walls' shapes, no more), and off the black lake.
        foreach (var (e, n, u, range, power) in new[] { (4f, 37f, 6f, 16f, 1.8f), (2f, 18f, 5f, 12f, 1.2f), (0f, 2f, 5f, 12f, 0.9f) })
            PointLight(root, At(e, n, u), range, power, Hex("#66708a")).shadows = LightShadows.None;
        var lake = PointLight(root, At(-6f, 56f, 4f), 16f, 0.7f, Hex("#5f6f86"));
        lake.shadows = LightShadows.None;

        // The air: mist on the black water, ash drifting down out of the dark, drips.
        HoldMist(root, At(-2f, 40f, 0.15f), new Vector3(40f, 0.3f, 80f));
        HoldAsh(root, At(-2f, 40f, 9f), new Vector3(36f, 1f, 80f));
        var drips = new GameObject("Drips").AddComponent<Drips>();
        drips.transform.SetParent(root, true);
        drips.transform.position = At(-2f, 40f, 0f);
        drips.size = new Vector3(40f, 0f, 84f);
        drips.everySecond = 0.4f;
        drips.from = 7f;
        drips.dripSound = Clips("hold_drip");

        // Eyes in the dark of the pools and the lake (the Kept, loose out there), watching, shrinking back from the lantern.
        foreach (var (e, n, u) in new[] { (-5f, 2f, 0.7f), (5f, 4f, 0.9f), (-3f, 52f, 0.6f), (-11f, 58f, 0.8f), (-8f, 61f, 0.7f), (8f, 40f, 0.9f) })
            KeptEyes(root, At(e, n, u));

        // Sounds: the rock groaning far off, now and then; the day outside the crack.
        var groans = new GameObject("The Rock Settling").AddComponent<AmbientOneShots>();
        groans.transform.SetParent(root, true);
        groans.transform.position = At(-2f, 40f, 6f);
        groans.clips = Clips("hold_creak");
        groans.every = new Vector2(12f, 26f);
        groans.spread = 30f;
        groans.volume = 0.5f;
        var wind = new GameObject("The Day Outside (the crack)", typeof(AudioSource)).GetComponent<AudioSource>();
        wind.transform.SetParent(root, true);
        wind.transform.position = HoldCrack + Vector3.up * 1.5f;
        wind.clip = Clips("hold_hatch") is var hatchClips && hatchClips.Length > 0 ? hatchClips[0] : null;
        wind.loop = true;
        wind.playOnAwake = true;
        wind.spatialBlend = 1f;
        wind.rolloffMode = AudioRolloffMode.Linear;
        wind.minDistance = 3f;
        wind.maxDistance = 22f;
        wind.volume = 0.45f;
        wind.dopplerLevel = 0f;

        // The lab's posts on the lake's islet; where you wake (the islet in the Pool).
        MooringPostAt(HoldMooring, 180f);
        var tester = TesterPostAt(HoldMooring + new Vector3(1.5f, 0f, -1.2f), 225f);
        tester.spawner = LabTemplates();
        var start = new GameObject("Start Point").transform;
        start.position = At(0f, 0f);
        var entrance = new GameObject("Area Entrance").AddComponent<AreaEntrance>();
        entrance.transform.position = start.position + Vector3.up;
        entrance.areaName = "The Hold";
        var apprentice = Apprentice(start.position);
        var bow = apprentice.GetComponent<BowLantern>();
        bow.minRange = 7.5f;      // (down here, even your first light reaches a little further: the dark is all there is)
        bow.minIntensity = 3f;
        apprentice.GetComponent<Equipment>().startingCharms = new[] { Charm.TestersKnot.Id };
        var readout = new GameObject("Lab Readout").AddComponent<LabReadout>();
        readout.font = AssetDatabase.LoadAssetAtPath<Font>(Fonts + "IMFellEnglish-Regular.ttf");
        tester.readout = readout;

        FinishScene(scene, HoldLabPath, start, apprentice, "music_hold");
    }

    // (hold.py's: where things are in the cave)
    static readonly Vector3 HoldCrack = new Vector3(-2.06f, 6f, 70.31f);   // the crack's foot (1.5 m wide, 4 m tall), facing...
    static readonly Vector3 HoldCrackOut = new Vector3(-0.33f, 0f, -0.94f); // ...out of the north wall, over the top ledge
    static readonly Vector3 HoldMooring = new Vector3(-6f, 0.5f, 54.3f);
    static readonly (Vector3 at, Vector3 normal)[] HoldEyeSpots = // (holes in the wall: each must have its socket)
    {
        (new Vector3(-2.04f, 1.93f, 10.4f), new Vector3(0.88f, -0.03f, -0.47f).normalized),
        (new Vector3(2.49f, 1.95f, 10.29f), new Vector3(-0.93f, 0.0f, -0.36f).normalized),
        (new Vector3(-1.61f, 2.9f, 12.74f), new Vector3(0.94f, -0.08f, -0.33f).normalized),
        (new Vector3(4.35f, 2.02f, 16.52f), new Vector3(-0.97f, -0.01f, 0.22f).normalized),
        (new Vector3(4.64f, 2.93f, 18.3f), new Vector3(-0.98f, 0.0f, 0.2f).normalized),
        (new Vector3(0.05f, 2.04f, 20.99f), new Vector3(0.99f, 0.0f, 0.15f).normalized),
        (new Vector3(10.26f, 2.94f, 30.37f), new Vector3(-1.0f, -0.03f, 0.03f).normalized),
        (new Vector3(-2.33f, 2.57f, 30.48f), new Vector3(1.0f, -0.03f, 0.06f).normalized),
        (new Vector3(9.61f, 1.74f, 34.7f), new Vector3(-1.0f, -0.01f, 0.0f).normalized),
        (new Vector3(9.68f, 1.75f, 37.09f), new Vector3(-1.0f, -0.03f, 0.09f).normalized),
        (new Vector3(10.09f, 1.13f, 40.05f), new Vector3(-1.0f, 0.0f, 0.06f).normalized),
        (new Vector3(-2.31f, 2.03f, 40.99f), new Vector3(1.0f, -0.01f, 0.0f).normalized),
        (new Vector3(-2.26f, 2.31f, 43.98f), new Vector3(0.99f, -0.02f, -0.14f).normalized),
        (new Vector3(-0.38f, 1.41f, 46.16f), new Vector3(0.32f, 0.0f, -0.95f).normalized),
        (new Vector3(6.81f, 2.93f, 56.68f), new Vector3(-1.0f, -0.04f, 0.09f).normalized),
        (new Vector3(6.55f, 2.28f, 59.98f), new Vector3(-0.98f, -0.02f, -0.18f).normalized),
        (new Vector3(3.52f, 1.4f, 66.41f), new Vector3(-0.77f, 0.02f, -0.64f).normalized),
        (new Vector3(1.6f, 1.72f, 68.31f), new Vector3(-0.61f, 0.01f, -0.79f).normalized),
        (new Vector3(-19.77f, 1.64f, 59.03f), new Vector3(0.99f, -0.03f, -0.16f).normalized),
        (new Vector3(-17.94f, 1.97f, 65.36f), new Vector3(0.9f, -0.01f, -0.45f).normalized),
        (new Vector3(-16.83f, 2.61f, 66.79f), new Vector3(0.75f, -0.03f, -0.66f).normalized),
    };
    static readonly (Vector3 at, Vector3 normal)[] HoldPresenceSpots =
    {
        (new Vector3(9.71f, 1.25f, 32.9f), new Vector3(-1.0f, -0.01f, -0.08f).normalized),
        (new Vector3(-2.29f, 1.85f, 33.48f), new Vector3(1.0f, 0.0f, 0.0f).normalized),
        (new Vector3(-2.28f, 2.51f, 35.59f), new Vector3(1.0f, -0.02f, 0.0f).normalized),
        (new Vector3(-2.3f, 1.65f, 37.69f), new Vector3(1.0f, 0.0f, 0.0f).normalized),
        (new Vector3(10.21f, 1.59f, 43.02f), new Vector3(-0.98f, 0.0f, 0.2f).normalized),
        (new Vector3(9.27f, 2.01f, 45.73f), new Vector3(-0.62f, -0.01f, -0.78f).normalized),
        (new Vector3(4.87f, 1.81f, 46.32f), new Vector3(0.01f, -0.03f, -1.0f).normalized),
        (new Vector3(-0.45f, 2.5f, 14.85f), new Vector3(0.89f, -0.05f, -0.45f).normalized),
        (new Vector3(0.27f, 1.79f, 16.83f), new Vector3(0.99f, 0.01f, -0.1f).normalized),
        (new Vector3(0.37f, 2.44f, 19.22f), new Vector3(0.99f, 0.0f, 0.11f).normalized),
        (new Vector3(4.55f, 1.92f, 20.08f), new Vector3(-0.97f, 0.01f, -0.23f).normalized),
        (new Vector3(0.36f, 2.04f, 24.23f), new Vector3(0.95f, 0.01f, -0.3f).normalized),
    };
    static readonly string[] PresenceKinds = { "face_1", "hand_1", "face_2", "face_3", "hand_2" };

    // Where the Kept wait in the walls (the wall's foot, its outward normal), and their groups (each comes out together).
    static readonly (float e, float n, float ne, float nn, int group)[] HoldKeptSpots =
    {
        (0.27f, 16.83f, 0.99f, -0.10f, 1), (4.55f, 20.08f, -0.97f, -0.23f, 1),                                  // the Passage: a pair
        (-2.29f, 33.48f, 1f, 0f, 2), (9.71f, 32.90f, -1f, -0.08f, 2), (-2.30f, 37.69f, 1f, 0f, 2),            // the Gallery: three
        (6.81f, 56.68f, -1f, 0.09f, 3), (6.55f, 59.98f, -0.98f, -0.18f, 3),                                     // the lake's east ledge: a pair
    };

    // One of the Kept (the Hold's): standing `outOfWall` once torn free, waiting `inWall` till then; turned out of the wall.
    static Kept Kept(string name, Vector3 outOfWall, Vector3 inWall, int group)
    {
        Vector3 facing = new Vector3(outOfWall.x - inWall.x, 0f, outOfWall.z - inWall.z);
        var go = Place("Characters/kept", outOfWall, facing.sqrMagnitude > 0.001f ? Quaternion.LookRotation(facing).eulerAngles.y : 0f);
        go.name = name;
        var controller = go.AddComponent<CharacterController>();
        controller.center = new Vector3(0f, 0.6f, 0f);
        controller.height = 1.2f;
        controller.radius = 0.35f;
        controller.minMoveDistance = 0f;
        var s = go.AddComponent<Kept>();
        s.inWall = inWall;
        s.group = group;
        s.maxHealth = 34f;          // (two blows of the oar)
        s.radius = 0.4f;
        s.staggerCapacity = 30f;
        s.staggerDuration = 0.7f;
        s.aggroRadius = 12f;
        s.viewHalfAngle = 180f;
        s.noticeCloseRadius = 4f;
        s.chaseSpeed = 1.6f;        // (a shuffle: you can walk away from one)
        s.turnSpeed = 220f;
        s.leashRadius = 14f;
        s.giveUpRange = 16f;
        s.obols = 6;
        s.eyeHeight = 1.0f;
        s.barHeight = 1.5f;
        s.deathDuration = 1.5f;
        s.dissolveDuration = 0.8f;
        s.fareMotes = 10;
        s.attackRange = 1.5f;
        s.primary = new SouredAttack
        {
            name = "Swipe", windupClip = "Windup", strikeClip = "Swipe", recoverClip = "Recover",
            telegraph = 0.9f, strike = 0.35f, recover = 0.8f, impactAt = 0.45f,
            lunge = 0.4f, maxLunge = 1.0f, trackUntil = 0.7f, damage = 10f, reach = 1.6f, arc = 70f, knockback = 3f,
            windupSound = Clips("kept_windup"), swingSound = Clips("kept_swipe"),
        };
        s.emergeSound = Clips("kept_emerge");
        s.stepSound = Clips("kept_step");
        s.hurtSound = Clips("kept_hurt");
        s.deathSound = Clips("kept_death");
        Swoosh(go, "HandR", "KeptClaw", SouredSwoosh, 0.14f, "KeptPalm"); // (its claws' swipe)
        return s;
    }

    // An eye in the living wall at `at`, looking out along `normal`: its socket, its ball and its lids (hold.py's eye_*),
    // turned so their front faces out (a static model's front is its back: see CLAUDE.md).
    static void WallEyeAt(Transform parent, Vector3 at, Vector3 normal)
    {
        // Turned up towards the camera (it looks down from above: an eye looking straight out is hidden under its own
        // upper lip), and big enough to read from there.
        normal = (new Vector3(normal.x, 0f, normal.z).normalized * Mathf.Cos(35f * Mathf.Deg2Rad) + Vector3.up * Mathf.Sin(35f * Mathf.Deg2Rad)).normalized;
        var eye = new GameObject("Eye in the Wall").AddComponent<WallEye>();
        eye.transform.SetParent(parent, true);
        eye.transform.SetPositionAndRotation(at, Quaternion.LookRotation(normal, Vector3.up));
        Transform Part(string variant)
        {
            var go = Place("Environment/hold_" + variant, at, 0f);
            go.transform.rotation = Quaternion.LookRotation(-normal, Vector3.up);
            go.transform.SetParent(eye.transform, true);
            foreach (var c in go.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(c); // (it's in the wall: the wall's collider is what's there)
            go.AddComponent<NavMeshIgnore>();
            return go.transform;
        }
        Part("eye_socket");
        eye.ball = Part("eye_ball");
        eye.lidTop = Part("eye_lid_top");
        eye.lidBottom = Part("eye_lid_bottom");
        eye.open = 68f;        // (hold.py's: the top lid's front edge rises 68°, the bottom's drops 52°)
        eye.openBottom = 52f;
        eye.transform.localScale = Vector3.one * 1.6f;
        eye.openSound = Clips("hold_eye_open");
        eye.closeSound = Clips("hold_eye_close");
    }

    // A face or a hand of the kept, in the living wall at `at`, pressing out along `normal` now and then.
    static void PresenceAt(Transform parent, Vector3 at, Vector3 normal, string variant)
    {
        var presence = new GameObject("The Kept (" + variant + ")").AddComponent<WallPresence>();
        presence.transform.SetParent(parent, true);
        presence.transform.SetPositionAndRotation(at, Quaternion.LookRotation(normal, Vector3.up));
        var body = Place("Environment/hold_" + variant, at, 0f);
        body.transform.rotation = Quaternion.LookRotation(-normal, Vector3.up);
        body.transform.SetParent(presence.transform, true);
        foreach (var c in body.GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(c);
        body.AddComponent<NavMeshIgnore>();
        presence.body = body.transform;
        presence.pressSound = Clips("hold_stone_press");
    }

    // A shaft of grey daylight falling from a crack `tall` above `floor`: crossed soft planes (bright where they come
    // through, fading as they fall), a dim pool of light where it lands, and dust turning slowly in it.
    static void LightShaft(Transform parent, Vector3 floor, float tall, float radius, float brightness = 1f)
    {
        var go = new GameObject("Light Shaft", typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, true);
        go.transform.position = floor;
        var mesh = new Mesh { name = "Light Shaft" };
        var verts = new System.Collections.Generic.List<Vector3>();
        var colours = new System.Collections.Generic.List<Color>();
        var uvs = new System.Collections.Generic.List<Vector2>();
        var tris = new System.Collections.Generic.List<int>();
        const int planes = 6, rows = 8; // (many faint planes: a soft round column from any side, no edges to see)
        for (int p = 0; p < planes; p++)
        {
            float a = Mathf.PI * p / planes;
            Vector3 side = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            int first = verts.Count;
            for (int r = 0; r <= rows; r++)
            {
                float v = (float)r / rows;                         // 0 at the floor, 1 at the crack
                float width = radius * Mathf.Lerp(1.35f, 0.55f, v); // (spreading as it falls)
                // Faint, strongest a little above the floor, gone well before it reaches up to the camera.
                // Faint, fading to nothing at both ends (no edges where it meets the floor, gone before the camera); a
                // short bright one (from an opening) strongest high up, where it comes through.
                float peak = brightness > 1f ? 0.7f : 0.35f;
                float alpha = brightness * 0.042f * Mathf.Sin(Mathf.PI * Mathf.Pow(v, Mathf.Log(0.5f) / Mathf.Log(peak)));
                Color c = new Color(HoldDaylight.r, HoldDaylight.g, HoldDaylight.b, alpha);
                verts.Add(-side * width + Vector3.up * tall * v);
                verts.Add(side * width + Vector3.up * tall * v);
                colours.Add(c);
                colours.Add(c);
                uvs.Add(new Vector2(0f, v));
                uvs.Add(new Vector2(1f, v));
            }
            for (int r = 0; r < rows; r++)
            {
                int i = first + r * 2;
                tris.AddRange(new[] { i, i + 2, i + 1, i + 1, i + 2, i + 3 });
            }
        }
        mesh.SetVertices(verts);
        mesh.SetColors(colours);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = ParticleMaterial("M_LightShaft", ShaftTexture("T_LightShaft"));
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        go.AddComponent<NavMeshIgnore>();

        var pool = new GameObject("Light Shaft (its pool)").AddComponent<Light>();
        pool.transform.SetParent(go.transform, false);
        pool.transform.localPosition = Vector3.up * Mathf.Min(tall, 7f);
        pool.transform.rotation = Quaternion.LookRotation(Vector3.down);
        pool.type = LightType.Spot;
        pool.spotAngle = Mathf.Clamp(Mathf.Atan2(radius * 2.2f, Mathf.Min(tall, 7f)) * Mathf.Rad2Deg * 2f, 10f, 60f);
        pool.innerSpotAngle = pool.spotAngle * 0.4f;
        pool.range = Mathf.Min(tall, 7f) + 1.5f;
        pool.intensity = 3.2f * Mathf.Sqrt(brightness);
        pool.color = HoldDaylight;
        pool.shadows = LightShadows.None;

        var dust = new GameObject("Light Shaft (dust)").AddComponent<Motes>();
        dust.transform.SetParent(go.transform, false);
        dust.transform.localPosition = Vector3.up * 1.2f;
        dust.radius = radius * 0.9f;
        dust.perSecond = 1.4f;
        dust.colour = new Color(0.85f, 0.88f, 0.92f, 0.5f);
    }

    // A 32x8 alpha texture: soft at both edges across, full down its length (a beam's cross-section).
    static Texture2D ShaftTexture(string name)
    {
        string path = $"{Generated}/{name}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null)
            return existing;
        const int w = 32, h = 8;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = name };
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f) / w * 2f - 1f;
                float a = Mathf.Clamp01(1f - u * u);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        tex.Apply();
        AssetDatabase.CreateAsset(tex, path);
        return tex;
    }

    // Mist lying low on the black water, barely moving.
    static void HoldMist(Transform parent, Vector3 centre, Vector3 size)
    {
        var go = new GameObject("Mist");
        go.transform.SetParent(parent, false);
        go.transform.position = centre;
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.startLifetime = 24f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(5f, 9f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new Color(0.62f, 0.66f, 0.72f, 0.05f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 160;
        var emission = ps.emission;
        emission.rateOverTime = 6f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = size;
        var drift = ps.velocityOverLifetime;
        drift.enabled = true;
        drift.space = ParticleSystemSimulationSpace.World;
        drift.x = new ParticleSystem.MinMaxCurve(-0.04f, 0.04f);
        drift.y = new ParticleSystem.MinMaxCurve(0f, 0f);
        drift.z = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
        var spin = ps.rotationOverLifetime;
        spin.enabled = true;
        spin.z = new ParticleSystem.MinMaxCurve(-0.03f, 0.03f);
        var fade = ps.colorOverLifetime;
        fade.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
        fade.color = g;
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = LitParticleMaterial("M_Mist", RadialTexture("T_SoftDot", ring: false), 1f);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
        go.AddComponent<NavMeshIgnore>();
    }

    // Something drifting down out of the dark, very slowly: ash, or dust (a few at a time), catching your light.
    static void HoldAsh(Transform parent, Vector3 top, Vector3 size)
    {
        var go = new GameObject("Ash");
        go.transform.SetParent(parent, false);
        go.transform.position = top;
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.startLifetime = 26f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
        main.startColor = new Color(0.78f, 0.78f, 0.8f, 0.7f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 900;
        var emission = ps.emission;
        emission.rateOverTime = 30f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = size;
        var drift = ps.velocityOverLifetime;
        drift.enabled = true;
        drift.space = ParticleSystemSimulationSpace.World;
        drift.x = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);
        drift.y = new ParticleSystem.MinMaxCurve(-0.42f, -0.28f);
        drift.z = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.12f;
        noise.frequency = 0.25f;
        var fade = ps.colorOverLifetime;
        fade.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(1f, 0.85f), new GradientAlphaKey(0f, 1f) });
        fade.color = g;
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = LitParticleMaterial("M_Ash", RadialTexture("T_SoftDot", ring: false), 0.15f); // (lit: seen where the light is)
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        go.AddComponent<NavMeshIgnore>();
    }

    // A pair of pale eyes in the dark (one of the Kept, loose, only watching).
    static void KeptEyes(Transform parent, Vector3 at)
    {
        var go = new GameObject("Eyes in the Dark").AddComponent<Watcher>();
        go.transform.SetParent(parent, true);
        go.transform.position = at;
        var eye = GeneratedMaterial("M_KeptEye", "MyGame/Toon");
        eye.SetColor("_BaseColor", new Color(0.86f, 0.88f, 0.84f));
        eye.EnableKeyword("_EMISSION");
        eye.SetColor("_EmissionColor", new Color(0.86f, 0.88f, 0.84f) * 2.2f);
        EditorUtility.SetDirty(eye);
        Transform Eye(string name)
        {
            var e = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            e.name = name;
            Object.DestroyImmediate(e.GetComponent<Collider>());
            e.transform.SetParent(go.transform, false);
            var r = e.GetComponent<MeshRenderer>();
            r.sharedMaterial = eye;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return e.transform;
        }
        go.eyeL = Eye("Eye (left)");
        go.eyeR = Eye("Eye (right)");
        go.whisperSound = Clips("hold_whisper");
    }
}
