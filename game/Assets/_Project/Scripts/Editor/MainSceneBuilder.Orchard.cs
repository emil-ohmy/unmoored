using MyGame;
using UnityEditor;
using UnityEngine;

// The Orchard of Last Autumn's lab (Scenes/Lab_Orchard.unity): its architecture and its feeling, first. Three terraces
// stepping up a hillside out of the water (each 1.4 m above the last: drops you can take, one way; dry-stone steps
// up), rows of apple and pear trees, the press house (a loft over its floor), an orchard wall with a gateway along the
// top, the things left where they were the night the harvest didn't come, lanterns on crooks with moths round them,
// leaves always drifting down, mist lying low in the rows, and the harvest moon (OrchardNight). The Tester's Post
// sets the Drowned City's Soured down here until the Orchard has its own.
public static partial class MainSceneBuilder
{
    const string OrchardLabPath = "Assets/_Project/Scenes/Lab_Orchard.unity";

    // The terraces' tops (each model's origin is at the water, the centre of its south face).
    const float GroundTop = 0.5f, MiddleTop = 1.9f, UpperTop = 3.3f;
    const float GroundZ = -8f, MiddleZ = 2f, UpperZ = 10f, OrchardBackZ = 18f, OrchardHalfWidth = 16f;

    [MenuItem("Assets/MyGame/Build Lab (Orchard)")]
    static void BuildOrchardLabFromMenu()
    {
        if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        BuildOrchardLab();
    }

    public static void BuildOrchardLabFromCommandLine() => BuildOrchardLab();

    static void BuildOrchardLab()
    {
        var scene = BeginScene(new Vector3(0f, 0f, 4f), OrchardNight);
        // The harvest moon that can't set while Father Moth keeps its reflection (it sets once the Stroke is taken).
        GameObject.Find("Moon").AddComponent<SettingMoon>();
        Terraces();
        OrchardRows();
        PressHouse(new Vector3(10.5f, MiddleTop, 6.1f));
        Mill(new Vector3(OrchardHalfWidth, 0f, GroundZ + 5f)); // (off the bottom terrace's east end)
        Afloat();
        // The boat's jetty off the bottom terrace (near the way in), its bell; the boat moored along the jetty's end.
        // Across the water, the tailrace and the landing at its head (a bell there too).
        const float jettyX = -7.5f;
        Place("Environment/jetty_walk", new Vector3(jettyX, GroundTop - 0.03f, GroundZ), 0f);
        Place("Environment/jetty_landing", new Vector3(jettyX, GroundTop - 0.03f, GroundZ - Segment), 0f);
        float mooredZ = GroundZ - Segment - 1.5f;
        var boat = BoatAt(new Vector3(jettyX + 2.55f, 0f, mooredZ), 180f, new Vector3(jettyX + 1.15f, GroundTop, mooredZ));
        var landing = Tailrace(new Vector3(2f, 0f, GroundZ - 20f));
        boat.docks = new[] { boat.home, landing };
        DockBellAt(boat.home, boat, new Vector3(jettyX - 1.15f, GroundTop - 0.03f, GroundZ - Segment - 0.45f), 90f);
        DockBellAt(landing, boat, landing.ashore.position + new Vector3(-1.8f, -0.02f, 0.2f), 90f);
        boat.lagoon = new Rect(-OrchardHalfWidth, GroundZ - 26f, OrchardHalfWidth * 2f, 25.4f);
        UpperTerrace();
        UnreapedRows(); // the field over the orchard wall: the Orchard's miniboss, among its scarecrows
        Glasshouse();   // beyond the field's far gate: the Gardener's (Father Moth's) arena
        GleanersInTheRows();
        Thresher("Thresher", new Vector3(13.5f, GroundTop, -6.2f), -90f); // (the bottom terrace's long rows: room to run, trees to run into)
        OrchardLanterns();
        OrchardAir();

        // The posts at the bottom terrace's west end, where you come in.
        MooringPostAt(new Vector3(-13.5f, GroundTop, -6.2f), 0f);
        var tester = TesterPostAt(new Vector3(-11.2f, GroundTop, -6.6f), 15f);
        tester.spawner = LabTemplates();
        // The rows' own Soured (the Gleaners, the Moths' flocks) start asleep: woken at the post.
        var residents = new System.Collections.Generic.List<GameObject>();
        foreach (var name in new[] { "Gleaners", "Moths (middle terrace)", "Moths (top terrace)" })
            if (GameObject.Find(name) is GameObject resident)
                residents.Add(resident);
        tester.residents = residents.ToArray();
        var start = new GameObject("Start Point").transform;
        start.position = new Vector3(-12f, GroundTop, -3.8f);
        var entrance = new GameObject("Area Entrance").AddComponent<AreaEntrance>();
        entrance.transform.position = start.position + Vector3.up;
        entrance.areaName = "The Orchard of Last Autumn (Lab)";
        var apprentice = Apprentice(start.position);
        apprentice.GetComponent<Equipment>().startingCharms = new[] { Charm.TestersKnot.Id };
        var readout = new GameObject("Lab Readout").AddComponent<LabReadout>();
        readout.font = AssetDatabase.LoadAssetAtPath<Font>(Fonts + "IMFellEnglish-Regular.ttf");
        tester.readout = readout;

        // Dry as old leaves: everything here burns harder, and longer.
        var traits = new GameObject("Area Traits").AddComponent<AreaTraits>();
        traits.fireHarm = 1.75f;
        traits.burnLength = 1.5f;

        FinishScene(scene, OrchardLabPath, start, apprentice, "music_orchard"); // Last Autumn
    }

    // Three terraces up out of the water, steps between them, and walls along the high ends (too tall to drop from).
    static void Terraces()
    {
        Place("Environment/terrace_ground", new Vector3(0f, 0f, GroundZ), 0f);
        Place("Environment/terrace_middle", new Vector3(0f, 0f, MiddleZ), 0f);
        Place("Environment/terrace_upper", new Vector3(0f, 0f, UpperZ), 0f);
        var ramps = new GameObject("Step Ramps").transform;
        void Steps(float x, float fromTop, float faceZ)
        {
            var foot = new Vector3(x, fromTop, faceZ - 7 * 0.333f);
            Place("Environment/orchard_steps", foot, 0f);
            Ramp(ramps, foot, new Vector3(x, fromTop + 1.4f, faceZ), 2f);
        }
        Steps(-9f, GroundTop, MiddleZ);
        Steps(3f, MiddleTop, UpperZ);
        // The middle and upper terraces' ends are too tall above the water to step off: walled.
        foreach (float x in new[] { -OrchardHalfWidth + 0.3f, OrchardHalfWidth - 0.3f })
        {
            for (float z = MiddleZ + 0.4f; z < OrchardBackZ - 0.4f; z += 4f)
                Place("Environment/orchard_wall_long", new Vector3(x, z < UpperZ ? MiddleTop : UpperTop, z), -90f);
            Place("Environment/orchard_wall_end", new Vector3(x, MiddleTop, MiddleZ + 0.3f), 0f);
        }
    }

    // A tree, with only its trunk solid (the crown overhead stops nothing: arrows, the Hook). It doesn't open up for the
    // camera as buildings do: a crown over the Apprentice hides little, and they can be seen through the leaves' gaps.
    static GameObject OrchardTree(string variant, Vector3 at, float yaw)
    {
        var go = Place("Environment/orchard_tree_" + variant, at, yaw);
        foreach (var c in go.GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(c);
        var trunk = go.AddComponent<CapsuleCollider>();
        trunk.center = new Vector3(0f, variant == "flooded" ? 0.5f : 1.0f, 0f);
        trunk.radius = 0.22f;
        trunk.height = 2.2f;
        return go;
    }

    static GameObject OrchardProp(string variant, Vector3 at, float yaw) => Place("Environment/orchard_props_" + variant, at, yaw);

    // The rows: apple trees on the bottom and middle terraces (in lines, a few old), the flooded ones standing in the
    // water below; baskets and crates under them, fallen fruit in the leaves.
    static void OrchardRows()
    {
        var rng = new System.Random(7);
        string Apple() => rng.NextDouble() < 0.25 ? "apple_old" : "apple";
        float Yaw() => (float)rng.NextDouble() * 360f;
        // (the bottom row thinned to the west: the east of the terrace is the Thresher's, open ground to run with a
        // couple of trees left to run into)
        foreach (float x in new[] { -6f, -1.5f, 5f })
            OrchardTree(Apple(), new Vector3(x, GroundTop, -2.2f), Yaw());
        foreach (float x in new[] { -3.5f, 5.5f, 12.5f })
            OrchardTree("flooded", new Vector3(x, 0f, GroundZ - 1.8f), Yaw());
        foreach (float x in new[] { -13f, -5f, -1f })
            OrchardTree(Apple(), new Vector3(x, MiddleTop, 4.6f), Yaw());
        foreach (float x in new[] { -11f, -7f, -3f, 1f })
            OrchardTree(x == -7f ? "pear" : Apple(), new Vector3(x, MiddleTop, 8.2f), Yaw());

        // Baskets and crates waiting under them.
        OrchardProp("basket", new Vector3(-6.8f, GroundTop, -3.1f), -40f);
        OrchardProp("basket", new Vector3(-4.2f, MiddleTop, 3.5f), 60f);
        OrchardProp("crate", new Vector3(-12.2f, MiddleTop, 3.4f), 3f);
        foreach (var p in new[] { new Vector3(-1.2f, GroundTop, -1.4f), new Vector3(-12.6f, MiddleTop, 5.6f),
                                  new Vector3(-7.4f, MiddleTop, 9.1f), new Vector3(0.6f, MiddleTop, 7.4f) })
            OrchardProp("apples", p, Yaw());
    }

    // The press house: its floor, its loft and its roof each fade with the storeys above the Apprentice inside; the
    // press and its barrels on the floor; its stair's ramp (for the Soured's ways).
    static void PressHouse(Vector3 at)
    {
        var root = new GameObject("Press House");
        root.transform.position = at;
        var storeys = root.AddComponent<Storeys>();
        storeys.footprint = new Vector2(4.5f, 3.5f);
        GameObject Part(string model)
        {
            var go = Place("Environment/" + model, at, 0f);
            go.transform.SetParent(root.transform, true);
            return go;
        }
        var ground = Part("press_house_ground");
        var loft = Part("press_house_loft");
        var roof = Part("press_house_roof");
        storeys.storeys = new[]
        {
            new Storeys.Storey { floor = at.y, parts = new[] { ground } },
            new Storeys.Storey { floor = at.y + 2.4f, parts = new[] { loft } },
            new Storeys.Storey { floor = at.y + 99f, parts = new[] { roof } }, // (never stood on: gone whenever you're inside)
        };
        void Inside(GameObject prop, GameObject storey) => prop.transform.SetParent(storey.transform, true);
        Inside(OrchardProp("press", at + new Vector3(-2.2f, 0f, -1.9f), 0f), ground);
        Inside(Place("Environment/barrel", at + new Vector3(-3.6f, 0f, 2.6f), 0f), ground);
        Inside(Place("Environment/barrel", at + new Vector3(-2.8f, 0f, 2.8f), 40f), ground);
        Inside(OrchardProp("crate", at + new Vector3(0.6f, 0f, 2.6f), 5f), ground);
        Inside(OrchardProp("basket", at + new Vector3(-0.4f, 2.4f, 2.6f), 30f), loft);
        Inside(OrchardProp("crate", at + new Vector3(1.6f, 2.4f, 2.7f), -10f), loft);
        var light = PointLight(root.transform, at + new Vector3(-1f, 1.9f, 0.5f), 6f, 1.8f, LanternLight); // (a lamp left lit inside)
        Flicker(light, 0.1f, 4f, 0.01f);
        light.transform.SetParent(ground.transform, true);
        Ramp(new GameObject("Press House Stair Ramp").transform, at + new Vector3(3.68f, 0f, -3.0f), at + new Vector3(3.68f, 2.4f, 1.0f), 1.0f);
    }

    // The top terrace: the old trees, the well, the hives, and the orchard wall along its back, its gateway into the
    // Unreaped Rows (the Watchman's field; the Gardener's glasshouse will be beyond it).
    static void UpperTerrace()
    {
        OrchardTree("apple_old", new Vector3(-12f, UpperTop, 13f), 40f);
        OrchardTree("apple", new Vector3(-4.5f, UpperTop, 15f), 200f);
        OrchardTree("pear", new Vector3(10f, UpperTop, 15.2f), 100f);
        OrchardProp("well", new Vector3(5.5f, UpperTop, 13.6f), 15f);
        ProvisionAt("sodden_sack", 2, new Vector3(4.3f, UpperTop, 12.5f), 30f); // (soaked at the well: before the Watchman's gate)
        foreach (float x in new[] { 12.2f, 13.4f, 14.6f })
            OrchardProp("skep", new Vector3(x, UpperTop, 16.6f), x * 20f);
        OrchardProp("basket", new Vector3(-3.4f, UpperTop, 14.2f), 0f);
        OrchardProp("apples", new Vector3(9.4f, UpperTop, 14.1f), 0f);
        float wallZ = OrchardBackZ - 0.35f;
        for (float x = -OrchardHalfWidth + 0.3f; x < OrchardHalfWidth - 0.4f; x += 4f)
            Place(Mathf.Abs(x - FieldGateWallX) < 0.5f ? "Environment/orchard_wall_gate" : "Environment/orchard_wall_long", new Vector3(x, UpperTop, wallZ), 0f);
    }

    // The Unreaped Rows: the field above the orchard, through the gateway in its wall, never harvested: dead wheat
    // standing in rows, a few stooks where someone began and stopped, walled round. The Watchman's arena: six
    // scarecrows in the rows, all alike, all looking at the gate you come in by; one of them is him (a different one
    // each time he re-forms). Find him and the gates swing shut behind you until the fight is over.
    const float FieldX = -2f, FieldZ = 18f, FieldHalfWidth = 12f, FieldDepth = 16f;
    const float FieldGateWallX = -3.7f; // (the orchard wall's gate segment: its gateway from 0.9 to 3.1 m along it)
    static readonly Vector3[] WatchmanSpots =
    {
        new Vector3(-6.5f, UpperTop, 22.5f), new Vector3(2.5f, UpperTop, 22.0f), new Vector3(-10f, UpperTop, 27.5f),
        new Vector3(-2f, UpperTop, 26.5f), new Vector3(5.5f, UpperTop, 28.5f), new Vector3(-5f, UpperTop, 31.5f),
    };

    static void UnreapedRows()
    {
        var root = new GameObject("The Unreaped Rows").transform;
        GameObject Put(string model, Vector3 at, float yaw)
        {
            var go = Place("Environment/" + model, at, yaw);
            go.transform.SetParent(root, true);
            return go;
        }
        Put("unreaped_field_ground", new Vector3(FieldX, 0f, FieldZ), 0f);
        // Walled round, over the water: west, east and north.
        float west = FieldX - FieldHalfWidth + 0.25f, east = FieldX + FieldHalfWidth - 0.25f, north = FieldZ + FieldDepth - 0.25f;
        for (float z = FieldZ; z < north - 0.5f; z += 4f)
        {
            Put("orchard_wall_long", new Vector3(west, UpperTop, z), -90f);
            Put("orchard_wall_long", new Vector3(east, UpperTop, z), -90f);
        }
        for (float x = FieldX - FieldHalfWidth; x < FieldX + FieldHalfWidth - 0.5f; x += 4f)
            Put(Mathf.Abs(x - (GlassX - 2f)) < 0.5f ? "orchard_wall_gate" : "orchard_wall_long", new Vector3(x, UpperTop, north), 0f); // (the far gateway: to the Glasshouse)
        Put("orchard_wall_end", new Vector3(west, UpperTop, north), 0f);
        Put("orchard_wall_end", new Vector3(east, UpperTop, north), 0f);

        // The stooks, and the wheat in its rows (none in the scarecrows' places, nor in the way in from the gate).
        var stooks = new[] { new Vector3(7.5f, UpperTop, 20.6f), new Vector3(-11.6f, UpperTop, 21.2f), new Vector3(7.2f, UpperTop, 30.0f), new Vector3(-11.4f, UpperTop, 30.2f) };
        var rng = new System.Random(11);
        foreach (var at in stooks)
            Put("unreaped_field_stook", at, (float)rng.NextDouble() * 360f);
        float gateX = FieldGateWallX + 2f;
        for (float x = FieldX - FieldHalfWidth + 1.2f; x < FieldX + FieldHalfWidth - 1f; x += 2.2f)
            for (float z = FieldZ + 1.6f; z < FieldZ + FieldDepth - 1.1f; z += 1.45f)
            {
                var at = new Vector3(x + (float)(rng.NextDouble() - 0.5) * 0.5f, UpperTop, z + (float)(rng.NextDouble() - 0.5) * 0.4f);
                bool clear = System.Array.TrueForAll(WatchmanSpots, p => Flat(p - at).magnitude > 1.4f)
                          && System.Array.TrueForAll(stooks, p => Flat(p - at).magnitude > 1.4f)
                          && !(Mathf.Abs(at.x - gateX) < 1.6f && at.z < FieldZ + 3.5f);
                if (!clear || rng.NextDouble() < 0.28)
                    continue;
                var clump = Put("unreaped_field_wheat", at, (float)rng.NextDouble() * 360f);
                foreach (var c in clump.GetComponentsInChildren<Collider>())
                    Object.DestroyImmediate(c); // (nothing solid in it)
            }

        // A lantern just inside the gate, and two left burning low in the far corners (the rest is the moon's: and his).
        var lights = new System.Collections.Generic.List<Light>();
        foreach (var (at, yaw, range, intensity) in new[] {
                     (new Vector3(gateX - 2.6f, UpperTop, FieldZ + 1.0f), 20f, 6f, 1.8f),
                     (new Vector3(FieldX - FieldHalfWidth + 1.4f, UpperTop, FieldZ + FieldDepth - 1.4f), 135f, 8f, 1.4f),
                     (new Vector3(FieldX + FieldHalfWidth - 1.4f, UpperTop, FieldZ + FieldDepth - 1.4f), -135f, 8f, 1.4f) })
        {
            var lantern = Put("orchard_props_lantern", at, yaw);
            Vector3 lamp = GlowCentre(lantern, "OrchardLantern_Glow");
            var light = PointLight(lantern.transform, lamp, range, intensity, LanternLight);
            Flicker(light, 0.08f, 3f, 0.01f);
            lights.Add(light);
        }

        var watchman = TheWatchmansField(root, lights.ToArray());
        var zone = new GameObject("Arena Zone (the Unreaped Rows)").AddComponent<ArenaZone>(); // (nothing follows you in)
        zone.transform.SetParent(root, true);
        zone.transform.position = new Vector3(FieldX, UpperTop + 1f, FieldZ + FieldDepth / 2f);
        zone.size = new Vector3(FieldHalfWidth * 2f, 6f, FieldDepth);

        // The gateway's two plank gates: open into the field; swung shut behind you once he's found.
        var gate = new GameObject("Field Gate").AddComponent<ArenaGate>();
        gate.transform.SetParent(root, true);
        gate.transform.position = new Vector3(gateX, UpperTop, OrchardBackZ - 0.35f);
        gate.boss = watchman;
        Transform Leaf(float hingeX, float openYaw)
        {
            var leaf = Put("orchard_wall_leaf", new Vector3(hingeX, UpperTop, OrchardBackZ - 0.35f), openYaw);
            foreach (var c in leaf.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(c); // (the gate's wall holds the way while they're shut)
            return leaf.transform;
        }
        gate.leaves = new[] { Leaf(gateX - 1.06f, -90f), Leaf(gateX + 1.06f, 270f) };
        gate.shutYaw = new[] { 0f, 180f };
        gate.openYaw = new[] { -90f, 270f };
        gate.shutSound = Clips("field_gate_slam");
        gate.openSound = Clips("gate_open");

        // The far gateway (on to the Glasshouse) shut too, while he's fought: its leaves open into the field.
        var far = new GameObject("Field Gate (far)").AddComponent<ArenaGate>();
        far.transform.SetParent(root, true);
        far.transform.position = new Vector3(GlassX, UpperTop, north);
        far.boss = watchman;
        Transform FarLeaf(float hingeX, float openYaw)
        {
            var leaf = Put("orchard_wall_leaf", new Vector3(hingeX, UpperTop, north), openYaw);
            foreach (var c in leaf.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(c);
            return leaf.transform;
        }
        far.leaves = new[] { FarLeaf(GlassX - 1.06f, 90f), FarLeaf(GlassX + 1.06f, 90f) };
        far.shutYaw = new[] { 0f, 180f };
        far.openYaw = new[] { 90f, 90f };
        far.shutSound = Clips("field_gate_slam");
        far.openSound = Clips("gate_open");
    }

    static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

    // The Watchman and the scarecrows he stands among, each on its stake: struck, a scarecrow goes up in flames; he's
    // straw too, and only fire hurts him (his own, turned back, most of all).
    static Watchman TheWatchmansField(Transform rows, Light[] fieldLights)
    {
        var root = new GameObject("The Watchman's Field").transform;
        root.SetParent(rows, true);
        const float yaw = 180f; // (towards the gate: their faces to you as you come in)
        Transform Pole(Vector3 at)
        {
            var pole = OrchardProp("pole", at, 0f);
            foreach (var c in pole.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(c); // (inside the figure on it: the figure is what's solid)
            pole.transform.SetParent(root, true);
            return pole.transform;
        }
        T Figure<T>(string name, Vector3 at) where T : Soured
        {
            var go = Place("Characters/watchman", at, yaw);
            go.name = name;
            go.transform.SetParent(root, true);
            var controller = go.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 1.0f, 0f);
            controller.height = 2.0f;
            controller.radius = 0.4f;
            controller.minMoveDistance = 0f;
            var s = go.AddComponent<T>();
            s.radius = 0.45f;
            s.knockbackResistance = 1f; // (on a stake; and, found, he's rooted where he stands)
            s.eyeHeight = 1.8f;
            s.barHeight = 2.4f;
            return s;
        }

        var field = new Scarecrow[WatchmanSpots.Length - 1];
        for (int i = 0; i < field.Length; i++)
        {
            Vector3 at = WatchmanSpots[i + 1];
            var s = field[i] = Figure<Scarecrow>($"Scarecrow {i + 1}", at);
            s.maxHealth = 1f;
            s.staggerCapacity = 999f;
            s.obols = 0;
            s.fareMotes = 0;          // (no soul in it: straw)
            s.deathDuration = 1.0f;     // (catching, then going up)
            s.dissolveDuration = 0.5f;
            s.fireWeakness = 2f;
            s.pole = Pole(at);
            s.burstSound = Clips("scarecrow_burst");
        }

        var w = Figure<Watchman>("The Watchman", WatchmanSpots[0]);
        w.field = field;
        w.pole = Pole(WatchmanSpots[0]);
        w.maxHealth = 300f;
        w.staggerCapacity = 120f;
        w.staggerDuration = 1.0f;
        w.aggroRadius = 14f;
        w.viewHalfAngle = 180f;       // (found, he knows where you are)
        w.noticeCloseRadius = 14f;
        w.attackRange = 2.4f;
        w.chaseSpeed = 3f;
        w.turnSpeed = 540f;
        w.leashRadius = 22f;
        w.giveUpRange = 22f;
        w.obols = 220;
        w.deathDuration = 1.0f;
        w.fireWeakness = 0.6f;        // (the Orchard's dryness makes it about even: pitch and tar burn him a little)
        w.fieldLights = fieldLights;
        w.primary = new SouredAttack
        {
            name = "Sweep", windupClip = "Windup", strikeClip = "Swing", recoverClip = "Recover",
            telegraph = 0.6f, strike = 0.3f, recover = 0.9f, impactAt = 0.4f,
            lunge = 0.6f, maxLunge = 1.8f, trackUntil = 0.8f, damage = 24f, reach = 2.6f, arc = 110f, knockback = 8f,
        };
        w.primary.windupSound = Clips("watchman_windup");
        w.primary.swingSound = Clips("laden_swing");
        w.primary.swingPitch = 1.1f;
        // His fire: breathed over his lantern (the tell: he leans back, the lantern to his face, its flame roaring up).
        SouredAttack Fire(string name, string windup, float telegraph, float speed, float trackUntil) => new SouredAttack
        {
            name = name, windupClip = windup, strikeClip = "Spew", recoverClip = "Recover",
            telegraph = telegraph, strike = 0.4f, recover = 0.9f, impactAt = 0.3f,
            lunge = 0f, maxLunge = 0f, trackUntil = trackUntil, damage = 18f, reach = 15f, arc = 180f, knockback = 3f,
            ranged = true, projectileSpeed = speed,
            windupSound = Clips("watchman_breath"), releaseSound = Clips("fireball_whoosh"),
        };
        w.breath = Fire("Breath", "Breathe", 1.0f, 7f, 0.8f);
        w.fan = Fire("Fan", "Breathe", 1.0f, 8f, 0.8f);
        w.feint = Fire("Feint", "Hold", 1.9f, 9f, 0.92f);   // (held: let go late, and quicker)
        w.flurry = Fire("Flurry", "Breathe", 0.8f, 9f, 0.85f); // (three small, a beat apart: each to knock aside)
        w.flurry.strike = 2.0f;       // (long enough for all three)
        w.flurry.impactAt = 0.05f;
        w.flurry.volley = 3;
        w.flurry.volleyInterval = 0.8f;
        w.flurry.volleyTrack = 20f;
        w.finale = Fire("Finale", "Hold", 1.7f, 7f, 0.9f); // (his last great breath: the lantern blazing)
        w.fireMaterial = FireballMaterial();
        w.creakSound = Clips("watchman_creak");
        w.revealSound = Clips("watchman_reveal");
        w.strawSound = Clips("straw_puff");
        w.gutterSound = Clips("fireball_gutter");
        w.scorchSound = Clips("watchman_scorch");
        w.chimeSound = Clips("watchman_chime"); // (in order: one, two, three, rising)
        w.sourSound = Clips("watchman_sour");
        w.music = new[] // (found: his own; at half, the same music, worse)
        {
            AssetDatabase.LoadAssetAtPath<AudioClip>(Audio + "Music/music_watchman_1.wav"),
            AssetDatabase.LoadAssetAtPath<AudioClip>(Audio + "Music/music_watchman_2.wav"),
        };
        w.lantern = PointLight(Bone(w.gameObject, "ArmL"), GlowCentre(w.gameObject, "WatchmanLanternGlass"), 7.5f, w.lanternIntensity, LanternLight);
        w.lantern.enabled = false;
        Swoosh(w.gameObject, "ArmR", "HookIron", SouredSwoosh, 0.16f, "OrchardWood"); // (the hook's sweep)
        return w;
    }

    // The Glasshouse: beyond the Unreaped Rows' far gateway, on a terrace of its own, the Gardener's ruined palm house:
    // iron ribs and broken panes on a brick plinth, mossy flagstones, two shallow water channels down its sides, raised
    // beds of dead plants against the side walls, the dead orange tree in its tub against the north wall, four lamps
    // hung on chains; the middle left open (his floor). Its roof fades when you're inside (Storeys). Its doors swing shut behind you while his fight is on (ArenaGate); nothing follows
    // you in (ArenaZone).
    // The mill (the Stroke's first use for getting about; art/models/environment/mill.py): a stone yard off the
    // bottom terrace's east end, its mill-race (shallows, a race: a wave let go down it runs its length and breaks on
    // the wheel), the wheel in the river, the store's sluice gate chained to it (two turns of the wheel: open). `o` is
    // the yard's origin (the middle of its west edge, at the water); a point in the yard is (east, north, up) from it.
    static void Mill(Vector3 o)
    {
        Vector3 W(float e, float n, float u) => o + new Vector3(e, u, n);
        var root = new GameObject("The Mill").transform;
        Place("Environment/mill_yard", o, 0f).transform.SetParent(root, true);

        // The race's water: wade in (shallows); let the Stroke go down it (a mill-race).
        var water = GameObject.CreatePrimitive(PrimitiveType.Quad);
        water.name = "Mill-race Water";
        Object.DestroyImmediate(water.GetComponent<Collider>()); // (you wade: its bed holds you)
        water.transform.SetParent(root, true);
        water.transform.SetPositionAndRotation(W(5.6f, -3.3f, 0.85f), Quaternion.Euler(90f, 0f, 0f));
        water.transform.localScale = new Vector3(8.75f, 1.02f, 1f);
        var r = water.GetComponent<MeshRenderer>();
        r.sharedMaterial = ChannelWater();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        water.AddComponent<Shallows>().size = new Vector3(8.75f, 0.8f, 1.05f);
        var race = water.AddComponent<MillRace>();
        race.head = new GameObject("Race Head").transform;
        race.head.SetParent(root, true);
        race.head.position = W(1.25f, -3.3f, 0.85f);
        race.mouth = new GameObject("Race Mouth").transform;
        race.mouth.SetParent(root, true);
        race.mouth.position = W(10.25f, -3.3f, 0.85f);
        race.width = 1f;
        race.surgeSound = Clips("mill_race_surge");

        // The wheel, half in the river below the spout (nothing to stand on: no colliders).
        var wheelModel = Place("Environment/mill_wheel", W(10.9f, -3.3f, 0.2f), 0f);
        wheelModel.transform.SetParent(root, true);
        foreach (var c in wheelModel.GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(c);
        var wheel = new GameObject("Water Wheel").AddComponent<WaterWheel>();
        wheel.transform.SetParent(root, true);
        wheel.transform.position = W(10.9f, -3.3f, 0.2f);
        wheel.wheel = wheelModel.transform;
        wheel.axis = Vector3.forward;
        wheel.turnSound = Clips("mill_wheel_turn");
        wheel.creakSound = Clips("mill_wheel_creak");
        race.wheel = wheel;

        // The store's sluice gate, and its chain: from the axle, over the post's pulley and the gantry's, to the gate.
        var board = Place("Environment/mill_gate", W(8.1f, -1.15f, 0.5f), 0f);
        board.transform.SetParent(root, true);
        var sluice = new GameObject("Sluice Gate").AddComponent<SluiceGate>();
        sluice.transform.SetParent(root, true);
        sluice.transform.position = W(8.1f, -1.15f, 0.5f);
        sluice.board = board.transform;
        sluice.travel = 1.85f;
        sluice.stages = 2;
        sluice.ringHeight = 2.05f;
        sluice.riseSound = Clips("sluice_gate_rise");
        sluice.clunkSound = Clips("sluice_gate_clunk");
        wheel.gate = sluice;
        var chain = new GameObject("Sluice Chain").AddComponent<LineRenderer>();
        chain.transform.SetParent(root, true);
        chain.useWorldSpace = true;
        chain.positionCount = 4;
        chain.SetPositions(new[] { W(10.9f, -2.68f, 0.2f), W(10.9f, -2.55f, 1.9f), W(8.1f, -0.8f, 4.1f), W(8.1f, -1.15f, 0.5f + 2.05f) });
        chain.widthMultiplier = 0.05f;
        chain.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var iron = GeneratedMaterial("M_MillChain", "MyGame/Toon");
        iron.SetColor("_BaseColor", Hex("#2c2a30"));
        iron.SetFloat("_OutlineWidth", 0f);
        EditorUtility.SetDirty(iron);
        chain.sharedMaterial = iron;
        sluice.chain = chain;

        // In the store: what the miller left (an Obol cache).
        var cache = Place("Environment/obol_cache", W(8.6f, 1.6f, 0.52f), 200f);
        cache.name = "Obol Cache (the mill store)";
        cache.transform.SetParent(root, true);
        cache.AddComponent<ObolCache>();
        var glow = cache.AddComponent<PickupGlow>();
        glow.tier = PickupGlow.Tier.Item;
        glow.colour = LanternLight;
        glow.height = 0.7f;

        // Loose on the yard: crates to shove (off the edge, they float).
        foreach (var (e, n, yaw) in new[] { (3.6f, 0.6f, 10f), (4.4f, 1.4f, -25f), (2.6f, 3.2f, 40f) })
            Loose(OrchardProp("crate", W(e, n, 0.52f), yaw), 0.45f, 1.3f).transform.SetParent(root, true);
    }

    // The tailrace (the Stroke on the water; art/models/environment/tailrace.py): out in the river south of the bottom
    // terrace, a walled channel running north from a small landing, the water welling from under the landing and
    // running hard down it: too fast to row against (Current), climbed a surge at a time. A Drowned waits in the pool by
    // the landing; on the landing, an Obol cache. `o` is the middle of the landing's back wall, at the water; a point is
    // (east, north, up) from it. Returns the landing's dock.
    static Dock Tailrace(Vector3 o)
    {
        Vector3 W(float e, float n, float u) => o + new Vector3(e, u, n);
        var root = new GameObject("The Tailrace").transform;
        Place("Environment/tailrace", o, 0f).transform.SetParent(root, true);
        var current = new GameObject("Current").AddComponent<Current>();
        current.transform.SetParent(root, true);
        current.transform.SetPositionAndRotation(W(0f, 10.25f, 0f), Quaternion.identity); // (running north, out of it)
        current.size = new Vector2(6f, 8.5f);
        current.speed = 4f; // (the boat's best is 3.6: rowing alone, it loses ground)
        var rush = current.gameObject.AddComponent<AudioSource>();
        rush.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Audio + "current_rush.wav");
        rush.loop = true;
        rush.playOnAwake = false; // (Current starts it, from somewhere in the loop)
        rush.spatialBlend = 1f;
        rush.rolloffMode = AudioRolloffMode.Linear;
        rush.minDistance = 3f;
        rush.maxDistance = 20f;
        rush.volume = 0.6f;
        rush.dopplerLevel = 0f;

        var cache = Place("Environment/obol_cache", W(-1.6f, 1.4f, 0.52f), 160f);
        cache.name = "Obol Cache (the tailrace landing)";
        cache.transform.SetParent(root, true);
        cache.AddComponent<ObolCache>();
        var glow = cache.AddComponent<PickupGlow>();
        glow.tier = PickupGlow.Tier.Item;
        glow.colour = LanternLight;
        glow.height = 0.7f;

        Drowned("Drowned (the tailrace)", W(1.7f, 5.2f, 0f), 0f).transform.SetParent(root, true);
        var dock = DockAt("the tailrace landing", W(-0.6f, 5.0f, 0f), 0f, W(-0.6f, 3.0f, 0.52f), false);
        dock.transform.SetParent(root, true);
        return dock;
    }

    // A dock's bell (ring it: the boat comes in to this dock).
    static DockBell DockBellAt(Dock dock, Boat boat, Vector3 at, float yaw)
    {
        var post = Place("Environment/dock_bell", at, yaw);
        post.name = $"Dock Bell ({dock.displayName})";
        var bell = post.AddComponent<DockBell>();
        bell.dock = dock;
        bell.boat = boat;
        bell.ringSound = Clips("dock_bell");
        return bell;
    }

    // Something the Stroke's wave can shove (it floats, off the edge).
    static Shovable Loose(GameObject thing, float radius, float heaviness)
    {
        var s = thing.AddComponent<Shovable>();
        s.radius = radius;
        s.heaviness = heaviness;
        s.scrapeSound = Clips("crate_scrape");
        return s;
    }

    // Barrels and a crate adrift off the bottom terrace (the Stroke's wave from the edge sends them on their way).
    static void Afloat()
    {
        var root = new GameObject("Afloat").transform;
        foreach (var (x, z, yaw) in new[] { (7.4f, -10.8f, 0f), (9.6f, -11.8f, 50f), (-1.2f, -11.2f, 120f), (20.5f, -9.8f, 200f) })
        {
            var barrel = Place("Environment/barrel", new Vector3(x, 0f, z), yaw);
            barrel.name = "Barrel (afloat)";
            barrel.transform.SetParent(root, true);
            Loose(barrel, 0.36f, 1f).draft = 0.45f;
        }
        var crate = OrchardProp("crate", new Vector3(14.5f, 0f, -11f), 15f);
        crate.name = "Crate (afloat)";
        crate.transform.SetParent(root, true);
        Loose(crate, 0.45f, 1.2f).draft = 0.3f;
    }

    // Still, shallow water in a stone channel (the Glasshouse's channels, the mill-race): the river's own shader.
    static Material ChannelWater()
    {
        var channel = GeneratedMaterial("M_ChannelWater", "MyGame/Water");
        // (bluer than the river, against the harvest moon's orange, which would turn it olive; a strong cold sheen and
        // glints the ripples break up, the lamps caught in it, a pale edge along the kerbs: water, at a glance)
        channel.SetColor("_DeepColor", Hex("#0f3446"));
        channel.SetColor("_ShallowColor", Hex("#2f6f8c"));
        channel.SetColor("_FoamColor", Hex("#b8ccd2"));
        channel.SetColor("_ShadowTint", Hex("#9aa6d8"));
        channel.SetColor("_SkyColor", Hex("#33507a"));
        channel.SetColor("_GlintColor", Hex("#9fb6d6"));
        channel.SetFloat("_GlintThreshold", 0.978f);
        channel.SetVector("_SheenDir", new Vector4(0f, 0.8f, 0.6f, 0f));
        channel.SetFloat("_FoamDistance", 0.07f);
        channel.SetFloat("_DepthFade", 0.45f);
        channel.SetFloat("_WaveHeight", 0.01f);
        channel.SetFloat("_WaveLength", 1.2f);
        channel.SetFloat("_WaveSpeed", 0.5f);
        channel.SetFloat("_RippleScale", 2.6f);
        channel.SetFloat("_RippleSpeed", 0.6f);
        channel.SetFloat("_RippleStrength", 0.45f);
        channel.SetFloat("_LampReflection", 0.7f);
        channel.SetFloat("_LampSpecular", 3.5f);
        EditorUtility.SetDirty(channel);
        return channel;
    }

    const float GlassX = 0f, GlassGroundZ = 34f, GlassHouseZ = 38.2f;
    const float GlassWidth = 24f, GlassDepth = 18f, GlassWall = 3.6f, GlassRidge = 4.0f;
    const float GlassChannel = 8.6f, GlassChannelFrom = 1.48f, GlassChannelTo = 16.52f; // (the channels' middles, ±, and their ends, in from the doors)

    static void Glasshouse()
    {
        Vector3 door = new Vector3(GlassX, UpperTop, GlassHouseZ);
        var root = new GameObject("The Glasshouse").transform;
        root.position = door + new Vector3(0f, 0f, GlassDepth / 2f); // (the house's middle: the Storeys' footprint is round it)
        GameObject Put(string model, Vector3 at, float yaw)
        {
            var go = Place("Environment/" + model, at, yaw);
            go.transform.SetParent(root, true);
            return go;
        }
        Put("glasshouse_ground", new Vector3(GlassX, 0f, GlassGroundZ), 0f);
        var house = Put("glasshouse_house", door, 0f);
        var roof = Put("glasshouse_roof", door, 0f);
        foreach (var c in roof.GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(c); // (overhead: nothing stands on it, and nothing should stop at it: the Hook, flung up at him)
        var storeys = root.gameObject.AddComponent<Storeys>();
        storeys.footprint = new Vector2(GlassWidth / 2f, GlassDepth / 2f);
        storeys.storeys = new[]
        {
            new Storeys.Storey { floor = UpperTop, parts = new[] { house } },
            new Storeys.Storey { floor = UpperTop + 99f, parts = new[] { roof } }, // (never stood on: gone whenever you're inside)
        };
        // The channels' water: the river's own shader, shallow and still; wade in (it puts out flames).
        var channel = ChannelWater();
        float length = GlassChannelTo - GlassChannelFrom;
        foreach (float e in new[] { -GlassChannel, GlassChannel })
        {
            var water = GameObject.CreatePrimitive(PrimitiveType.Quad);
            water.name = "Channel Water";
            Object.DestroyImmediate(water.GetComponent<Collider>()); // (you wade: the bed below holds you)
            water.transform.SetParent(root, true);
            water.transform.SetPositionAndRotation(door + new Vector3(e, -0.04f, (GlassChannelFrom + GlassChannelTo) / 2f), Quaternion.Euler(90f, 0f, 0f));
            water.transform.localScale = new Vector3(1.1f, length + 0.06f, 1f);
            var r = water.GetComponent<MeshRenderer>();
            r.sharedMaterial = channel;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var shallows = water.AddComponent<Shallows>();
            shallows.size = new Vector3(1.2f, 0.8f, length + 0.1f);
        }

        // The dead orange tree against the north wall (its tub solid; its branches overhead, nothing).
        var tree = Put("glasshouse_tree", door + new Vector3(0f, 0f, GlassDepth - 1.6f), 25f);
        foreach (var c in tree.GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(c);
        var tub = tree.AddComponent<CapsuleCollider>();
        tub.center = new Vector3(0f, 0.6f, 0f);
        tub.radius = 0.95f;
        tub.height = 1.2f;
        // Four lamps hung on chains from the vault, lit (in his last phase, he tears them down).
        var lampModels = new System.Collections.Generic.List<Transform>();
        var lampLights = new System.Collections.Generic.List<Light>();
        foreach (var (e, n) in new[] { (-6f, 4.5f), (6f, 4.5f), (-6f, 13.5f), (6f, 13.5f) })
        {
            float th = Mathf.Acos(1f - 2f * n / GlassDepth);
            float hang = GlassWall + Mathf.Sin(th) * GlassRidge - 0.1f;
            var lamp = Put("glasshouse_lamp", door + new Vector3(e, hang, n), 0f);
            foreach (var c in lamp.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(c); // (overhead: nothing to walk into)
            var light = PointLight(lamp.transform, GlowCentre(lamp, "GlasshouseLamp_Glow"), 10f, 2.4f, LanternLight);
            Flicker(light, 0.08f, 3f, 0.01f);
            lampModels.Add(lamp.transform);
            lampLights.Add(light);
        }
        // A lantern on a crook by the doors, outside.
        var outside = Put("orchard_props_lantern", door + new Vector3(-2.4f, 0f, -1.2f), 30f);
        Flicker(PointLight(outside.transform, GlowCentre(outside, "OrchardLantern_Glow"), 6f, 1.8f, LanternLight), 0.08f, 3f, 0.01f);
        // The doors: two leaves of iron and glass, open into the house; swung shut behind you while his fight is on.
        var gate = new GameObject("Glasshouse Doors").AddComponent<ArenaGate>();
        gate.transform.SetParent(root, true);
        gate.transform.position = door + new Vector3(0f, 0f, 0.15f);
        gate.wallSize = new Vector3(2.8f, 3f, 0.5f);
        Transform Leaf(float hingeX, float openYaw)
        {
            var leaf = Put("glasshouse_leaf", door + new Vector3(hingeX, 0f, 0.15f), openYaw);
            foreach (var c in leaf.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(c);
            return leaf.transform;
        }
        gate.leaves = new[] { Leaf(GlassX - 1.25f, -90f), Leaf(GlassX + 1.25f, 270f) };
        gate.shutYaw = new[] { 0f, 180f };
        gate.openYaw = new[] { -90f, 270f };
        gate.shutSound = Clips("field_gate_slam");
        gate.openSound = Clips("gate_open");
        var moth = FatherMoth(root, door, lampModels.ToArray(), lampLights.ToArray());
        gate.boss = moth;
        var zone = new GameObject("Arena Zone (the Glasshouse)").AddComponent<ArenaZone>(); // (nothing follows you in)
        zone.transform.SetParent(root, true);
        zone.transform.position = root.position + Vector3.up * 1f;
        zone.size = new Vector3(GlassWidth, 7f, GlassDepth);
    }

    // Father Moth, Who Gathered Autumn (the Gardener): folded on the dead orange tree like a leaf until you come in.
    static FatherMoth FatherMoth(Transform root, Vector3 door, Transform[] lampModels, Light[] lampLights)
    {
        var go = Place("Characters/father_moth", door + new Vector3(0f, 1.3f, GlassDepth - 1.6f), 180f); // (on the tree, facing the doors)
        go.name = "Father Moth";
        go.transform.SetParent(root, true);
        var controller = go.AddComponent<CharacterController>();
        controller.center = new Vector3(0f, 1.5f, 0f);
        controller.height = 2.6f;
        controller.radius = 1.0f;
        controller.minMoveDistance = 0f;
        var m = go.AddComponent<FatherMoth>();
        m.maxHealth = 800f;
        m.radius = 1.4f;
        m.knockbackResistance = 1f;
        m.staggerCapacity = 9999f;    // (spent on the floor, nothing cuts it short)
        m.aggroRadius = 30f;
        m.viewHalfAngle = 180f;
        m.noticeCloseRadius = 30f;
        m.leashRadius = 60f;
        m.giveUpRange = 60f;
        m.turnSpeed = 300f;
        m.obols = 400;
        m.eyeHeight = 3.1f;
        m.barHeight = 4.4f;
        m.deathDuration = 2.0f;
        m.dissolveDuration = 1.6f;
        m.fareMotes = 90;
        m.fireWeakness = 2.5f;        // (a moth, and the Orchard burns)
        m.floorY = UpperTop + 0.06f;
        m.middle = door + new Vector3(0f, 0.06f, GlassDepth / 2f);
        m.reach = new Vector2(GlassWidth / 2f - 2.5f, GlassDepth / 2f - 3f); // (kept in from the walls: seen from above, he's never over them)
        m.walls = new Vector2(GlassWidth / 2f - 0.4f, GlassDepth / 2f - 0.4f);
        m.wakeLine = door.z + 11f; // (far enough in to have seen him there, folded on his tree)
        m.hover = door + new Vector3(0f, 0.06f, 10.5f); // (over the north of the floor: the rest of it is yours, to dodge in)
        m.beat = new SouredAttack // (a wingbeat: his wings raised high, the lantern in his ribs flaring; then the leaves)
        {
            name = "Beat", windupClip = "GustDraw", strikeClip = "Gust", recoverClip = "Fly",
            telegraph = 0.6f, strike = 0.4f, recover = 0.8f, impactAt = 0.05f,
            lunge = 0f, maxLunge = 0f, trackUntil = 0.9f, damage = 0f, reach = 30f, arc = 180f, knockback = 0f, scripted = true,
        };
        // Phase 3, afoot: snips, a sweep, a lunge (his shears turn blows; a perfect block on a strike stuns him).
        SouredAttack Shears(string name, string windup, string strike, float telegraph, float strikeTime, float recover, float damage,
                            float reach, float arc, float lunge, float maxLunge, string windupSound) => new SouredAttack
        {
            name = name, windupClip = windup, strikeClip = strike, recoverClip = "Land",
            telegraph = telegraph, strike = strikeTime, recover = recover, impactAt = 0.45f,
            lunge = lunge, maxLunge = maxLunge, trackUntil = 0.75f, damage = damage, reach = reach, arc = arc, knockback = 8f,
            windupSound = Clips(windupSound), swingSound = Clips("laden_swing"), swingPitch = 1.2f,
        };
        m.snip = Shears("Snip", "ShearsDraw", "Shears", 0.45f, 0.22f, 0.25f, 14f, 3.0f, 70f, 0.4f, 1.5f, "father_moth_snip");
        m.sweep = Shears("Sweep", "ShearsDraw", "Shears", 0.65f, 0.3f, 0.45f, 20f, 3.6f, 130f, 0.6f, 2f, "father_moth_snip");
        m.lunge = Shears("Lunge", "ThrustDraw", "Thrust", 0.7f, 0.4f, 0.6f, 22f, 3.0f, 60f, 1f, 6.5f, "father_moth_stall");
        var leaf = GeneratedMaterial("M_DriftLeaf", "MyGame/Toon");
        leaf.SetColor("_BaseColor", Hex("#9a4a1e"));
        leaf.SetColor("_EmissionColor", new Color(0.6f, 0.22f, 0.05f)); // (smouldering a little: seen in the dark, still a dead leaf's brown-orange)
        leaf.SetFloat("_OutlineWidth", 0f);
        EditorUtility.SetDirty(leaf);
        m.leafMaterial = leaf;
        // Phase 4: the lamps his scream brings down, and his gathering.
        m.lampModels = lampModels;
        m.lamps = lampLights;
        m.screamSound = Clips("father_moth_scream");
        m.shatterSound = Clips("glass_lamp_shatter");
        m.roarSound = Clips("father_moth_stall"); // (to the middle, rearing: the lantern in his ribs roaring up)
        m.sectionSound = Clips("thresher_crash"); // (a great iron-and-glass impact)
        var iron = GeneratedMaterial("M_VaultIron", "MyGame/Toon");
        iron.SetColor("_BaseColor", Hex("#2f2d33"));
        EditorUtility.SetDirty(iron);
        var glass = GeneratedMaterial("M_VaultGlass", "MyGame/Toon");
        glass.SetColor("_BaseColor", Hex("#9db3aa"));
        glass.SetColor("_EmissionColor", new Color(0.08f, 0.1f, 0.09f));
        EditorUtility.SetDirty(glass);
        var shelter = GeneratedMaterial("M_ShelterGlass", "MyGame/Toon");
        shelter.SetColor("_BaseColor", Hex("#a9cfe0"));
        shelter.SetColor("_EmissionColor", new Color(0.35f, 0.6f, 0.85f)); // (a cold pale-blue glow: the shelter, against his orange)
        EditorUtility.SetDirty(shelter);
        m.shelterGlassMaterial = shelter;
        m.ironMaterial = iron;
        m.glassMaterial = glass;
        m.chargeSound = Clips("father_moth_charge");
        m.music = new[] // (Last Autumn grown huge: his leaves; more of them; afoot, the waltz broken; the dark)
        {
            AssetDatabase.LoadAssetAtPath<AudioClip>(Audio + "Music/music_father_moth_1.wav"),
            AssetDatabase.LoadAssetAtPath<AudioClip>(Audio + "Music/music_father_moth_2.wav"),
            AssetDatabase.LoadAssetAtPath<AudioClip>(Audio + "Music/music_father_moth_3.wav"),
            AssetDatabase.LoadAssetAtPath<AudioClip>(Audio + "Music/music_father_moth_4.wav"),
        };
        m.endTune = AssetDatabase.LoadAssetAtPath<AudioClip>(Audio + "Music/music_father_moth_end.wav");
        var orb = GeneratedMaterial("M_ChargeOrb", "MyGame/Toon");
        orb.SetColor("_BaseColor", new Color(1f, 0.85f, 0.6f));
        orb.SetColor("_EmissionColor", new Color(4f, 2.6f, 1.2f)); // (blazing)
        orb.SetFloat("_OutlineWidth", 0f);
        EditorUtility.SetDirty(orb);
        m.orbMaterial = orb;
        m.releaseSound = Clips("father_moth_release");
        var pod = GeneratedMaterial("M_SeedPod", "MyGame/Toon");
        pod.SetColor("_BaseColor", Hex("#6b5a34"));
        pod.SetColor("_EmissionColor", new Color(0.3f, 0.2f, 0.05f));
        pod.SetFloat("_OutlineWidth", 0f);
        EditorUtility.SetDirty(pod);
        m.podMaterial = pod;
        var fruit = GeneratedMaterial("M_WitheredFruit", "MyGame/Toon");
        fruit.SetColor("_BaseColor", Hex("#a8642a"));
        fruit.SetColor("_EmissionColor", new Color(0.35f, 0.15f, 0.04f));
        fruit.SetFloat("_OutlineWidth", 0f);
        EditorUtility.SetDirty(fruit);
        m.fruitMaterial = fruit;
        m.podSound = Clips("seed_pod_burst");
        m.fruitSound = Clips("fruit_fall");
        m.chest = PointLight(Bone(go, "Torso"), GlowCentre(go, "FatherMothLantern_Glow"), 6f, m.chestIntensity, DrownedMoon.Moonlight); // (the Drowned Moon: cold)
        m.hollow = Bone(go, "Hollow");
        m.hollowLight = PointLight(m.hollow, GlowCentre(go, "FatherMothHollow_Glow"), 5f, m.hollowIntensity, new Color(1f, 0.55f, 0.2f));
        m.wingSound = Clips("father_moth_wing");
        m.wakeSound = Clips("father_moth_wake");
        m.beatSound = Clips("father_moth_beat");
        m.downedSound = Clips("father_moth_downed");
        m.glanceSound = Clips("block_clang");
        m.deathSound = Clips("father_moth_death");
        m.pourSound = Clips("drowned_moon_pour");
        return m;
    }

    // The Gleaners: two on the middle terrace's west rows, two on the top terrace before the field's gate (where you
    // learn to turn fire back, before the Watchman's).
    static void GleanersInTheRows()
    {
        var root = new GameObject("Gleaners").transform;
        foreach (var (at, yaw) in new[] {
                     (new Vector3(-10.5f, MiddleTop, 6.4f), 160f), (new Vector3(-5.5f, MiddleTop, 7.2f), 200f),
                     (new Vector3(-9.5f, UpperTop, 14.6f), 170f), (new Vector3(-4.8f, UpperTop, 12.6f), 190f) })
            Gleaner("Gleaner", at, yaw).transform.SetParent(root, true);
    }

    // Lanterns on crooks along the ways, each warm, flickering, a few small pale moths circling it.
    static void OrchardLanterns()
    {
        int lit = 0;
        foreach (var (at, yaw) in new[] {
                     (new Vector3(-10.5f, GroundTop, -4.2f), 30f), (new Vector3(0.5f, GroundTop, -5.2f), -20f), (new Vector3(10f, GroundTop, 1.4f), 10f),
                     (new Vector3(-8f, MiddleTop, 2.8f), 90f), (new Vector3(3.6f, MiddleTop, 5.2f), -60f),
                     (new Vector3(-9.5f, UpperTop, 11f), 20f), (new Vector3(1.6f, UpperTop, 11.2f), -80f), (new Vector3(13f, UpperTop, 11.4f), 200f) })
        {
            var lantern = OrchardProp("lantern", at, yaw);
            Vector3 lamp = GlowCentre(lantern, "OrchardLantern_Glow");
            var light = PointLight(lantern.transform, lamp, 7f, 2.4f, LanternLight);
            Flicker(light, 0.08f, 3f, 0.01f);
            LanternMoths(lantern.transform, lamp);
            // The Moths: a flock round the middle terrace's east lantern (by the press house), another round the top
            // terrace's east one; not by the way in (they'd be on you the moment you came), nor by the field's gate.
            if (lit == 4 || lit == 7)
                MothFlockAt(lit == 4 ? "Moths (middle terrace)" : "Moths (top terrace)", lamp, light.transform, lit == 4 ? 6 : 5);
            lit++;
        }
    }

    // A few small pale moths circling a lamp (not the Moths: just moths; but you'll think of them later).
    static void LanternMoths(Transform parent, Vector3 lamp)
    {
        var go = new GameObject("Moths");
        go.transform.SetParent(parent, true);
        go.transform.position = lamp;
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.startLifetime = 5f;
        main.startSpeed = 0.2f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.06f);
        main.startColor = new Color(1f, 0.94f, 0.82f, 0.85f);
        main.maxParticles = 8;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        var emission = ps.emission;
        emission.rateOverTime = 1.6f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.35f;
        var orbit = ps.velocityOverLifetime;
        orbit.enabled = true;
        orbit.space = ParticleSystemSimulationSpace.Local;
        orbit.orbitalX = new ParticleSystem.MinMaxCurve(0f, 0f); // (every orbital axis in the same mode, two constants: or Unity complains)
        orbit.orbitalY = new ParticleSystem.MinMaxCurve(2.2f, 3.4f);
        orbit.orbitalZ = new ParticleSystem.MinMaxCurve(0f, 0f);
        orbit.radial = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.5f;
        noise.frequency = 2.5f;
        var fade = ps.colorOverLifetime;
        fade.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(1f, 0.85f), new GradientAlphaKey(0f, 1f) });
        fade.color = g;
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = sparkMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    // The air: leaves always drifting down through it (a few at a time, never many), and mist lying low on each terrace.
    static void OrchardAir()
    {
        var air = new GameObject("Orchard Air").transform;
        // Falling leaves, over the whole hillside.
        {
            var go = new GameObject("Falling Leaves");
            go.transform.SetParent(air, false);
            go.transform.position = new Vector3(0f, 9f, 4f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.duration = 10f;
            main.startLifetime = 26f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.26f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 900;
            var colours = new Gradient();
            colours.SetKeys(new[] { new GradientColorKey(Hex("#b5552f"), 0f), new GradientColorKey(Hex("#c9923a"), 0.35f),
                                    new GradientColorKey(Hex("#8a2e27"), 0.7f), new GradientColorKey(Hex("#7a5432"), 1f) },
                            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            main.startColor = new ParticleSystem.MinMaxGradient(colours) { mode = ParticleSystemGradientMode.RandomColor };
            var emission = ps.emission;
            emission.rateOverTime = 26f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(34f, 0.5f, 30f);
            var fall = ps.velocityOverLifetime;
            fall.enabled = true;
            fall.space = ParticleSystemSimulationSpace.World;
            fall.x = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);   // (a breath of air from the west)
            fall.y = new ParticleSystem.MinMaxCurve(-0.5f, -0.32f);
            fall.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.45f;
            noise.frequency = 0.35f;
            noise.scrollSpeed = 0.15f;
            var spin = ps.rotationOverLifetime;
            spin.enabled = true;
            spin.z = new ParticleSystem.MinMaxCurve(-2.2f, 2.2f);
            var land = ps.collision;
            land.enabled = true;
            land.type = ParticleSystemCollisionType.World;
            land.mode = ParticleSystemCollisionMode.Collision3D;
            land.quality = ParticleSystemCollisionQuality.Low;
            land.lifetimeLoss = 1f; // (down: gone, into the leaves already there)
            land.radiusScale = 0.3f;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = LitParticleMaterial("M_Leaf", LeafTexture("T_Leaf"), 0.9f);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        // Mist lying low on each terrace, and on the water.
        foreach (var (y, z0, z1) in new[] { (0.15f, -14f, GroundZ), (GroundTop + 0.35f, GroundZ, MiddleZ), (MiddleTop + 0.35f, MiddleZ, UpperZ), (UpperTop + 0.35f, UpperZ, OrchardBackZ) })
        {
            var go = new GameObject("Mist");
            go.transform.SetParent(air, false);
            go.transform.position = new Vector3(0f, y, (z0 + z1) / 2f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = 18f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(5f, 8f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new Color(0.78f, 0.7f, 0.82f, 0.045f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 80;
            var emission = ps.emission;
            emission.rateOverTime = 3.5f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(34f, 0.3f, Mathf.Abs(z1 - z0));
            var drift = ps.velocityOverLifetime;
            drift.enabled = true;
            drift.space = ParticleSystemSimulationSpace.World;
            drift.x = new ParticleSystem.MinMaxCurve(0.06f, 0.18f);
            drift.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            drift.z = new ParticleSystem.MinMaxCurve(-0.03f, 0.03f);
            var spin = ps.rotationOverLifetime;
            spin.enabled = true;
            spin.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            fade.color = g;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = LitParticleMaterial("M_Mist", RadialTexture("T_SoftDot", ring: false), 1f);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard; // (lying flat over the ground)
        }
    }

    static Material LitParticleMaterial(string name, Texture2D texture, float ambient)
    {
        var m = GeneratedMaterial(name, "MyGame/ParticleLit");
        m.SetTexture("_MainTex", texture);
        m.SetFloat("_Ambient", ambient);
        EditorUtility.SetDirty(m);
        return m;
    }

    // A 32x32 alpha texture: a leaf, almond-shaped, a little notch at its stalk, its midrib a shade thinner.
    static Texture2D LeafTexture(string name)
    {
        string path = $"{Generated}/{name}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null)
            return existing;
        const int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = name };
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size - 0.5f, v = (y + 0.5f) / size;
                float half = 0.36f * Mathf.Sin(Mathf.Clamp01(v) * Mathf.PI) * (v < 0.15f ? v / 0.15f : 1f);
                float a = Mathf.Clamp01((half - Mathf.Abs(u)) * 30f);
                if (Mathf.Abs(u) < 0.025f && v > 0.08f && v < 0.9f)
                    a *= 0.7f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        AssetDatabase.CreateAsset(tex, path);
        return tex;
    }
}
