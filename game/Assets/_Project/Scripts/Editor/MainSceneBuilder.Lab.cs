using MyGame;
using UnityEditor;
using UnityEngine;

// Assets/_Project/Scenes/Lab_DrownedCity.unity: the Drowned City's lab, the sandbox for trying its Soured, its
// miniboss, its architecture and every upgrade side by side (each Area gets a lab of its own). Two
// quays joined into one open floor, a Mooring post (Rest to re-form everything; fit Charms) and beside it the Tester's
// Post: spawn any Soured in front of you (a count, aggressive or idle), give the Apprentice anything, reset. A training
// Laden stands on the east quay (idle and immortal: it never fights back and never dies), and a readout top right shows
// the numbers. Nothing here is saved.
//   Assets menu > MyGame > Build Lab (Drowned City), or headless: -executeMethod MainSceneBuilder.BuildLabFromCommandLine
public static partial class MainSceneBuilder
{
    const string LabScenePath = "Assets/_Project/Scenes/Lab_DrownedCity.unity";

    [MenuItem("Assets/MyGame/Build Lab (Drowned City)")]
    static void BuildLabFromMenu()
    {
        if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        BuildLab();
    }

    public static void BuildLabFromCommandLine() => BuildLab();

    static void BuildLab()
    {
        var scene = BeginScene(new Vector3(22f, 0f, 0f));

        // The floor: four quays side by side (x -8 .. 55, z -5.5 .. 5.5), lit all round; the heights to the east.
        Place("Environment/quay", Vector3.zero, 0f);
        Place("Environment/quay", new Vector3(15.5f, 0f, 0f), 0f);
        Place("Environment/quay", new Vector3(31f, 0f, 0f), 0f);
        Place("Environment/quay", new Vector3(46.5f, 0f, 0f), 0f);
        Heights();
        foreach (var p in new[] { new Vector3(-7.2f, 0f, -4.3f), new Vector3(-7.3f, 0f, 3.6f), new Vector3(7.6f, 0f, -4.8f), new Vector3(7.8f, 0f, 4.4f),
                                  new Vector3(22.6f, 0f, -4.3f), new Vector3(22.8f, 0f, 3.8f) })
            Torch(new Vector3(p.x, QuayTop, p.z));
        StreetLantern(new Vector3(0f, QuayTop, 4.6f), 180f);
        StreetLantern(new Vector3(15.5f, QuayTop, -4.8f), 0f);
        StreetLantern(new Vector3(15.5f, QuayTop, 4.6f), 180f);

        FindsRow(new Vector3(1.0f, QuayTop, 3.3f), 1.25f);

        // The Mooring post and the Tester's Post, side by side at the west end.
        MooringPostAt(new Vector3(-6.0f, QuayTop, -3.4f), 0f);
        var tester = TesterPostAt(new Vector3(-3.4f, QuayTop, -4.1f), 20f);
        var heightsTester = TesterPostAt(new Vector3(20.6f, QuayTop, -3.6f), 0f); // a second, by the heights (the walkway's stair)

        var start = new GameObject("Start Point").transform;
        start.position = new Vector3(-4.6f, QuayTop, -1.6f);
        var entrance = new GameObject("Area Entrance").AddComponent<AreaEntrance>();
        entrance.transform.position = start.position + Vector3.up;
        entrance.areaName = "The Drowned City (Lab)";
        var apprentice = Apprentice(start.position);
        apprentice.GetComponent<Equipment>().startingCharms = new[] { Charm.TestersKnot.Id };

        var spawner = LabTemplates();
        tester.spawner = heightsTester.spawner = spawner;

        // Two Drowned waiting off the quay's south edge, east of the posts (only bubbles show), and crates on the edge
        // between them to hide behind (hide, and they come to the shore to look for you).
        Drowned("Drowned (south edge)", new Vector3(9f, 0f, -6.1f), 0f);
        Drowned("Drowned (south edge, further)", new Vector3(13.5f, 0f, -6.1f), 0f);
        Place("Environment/crate", new Vector3(11.0f, QuayTop, -4.7f), 10f);
        Place("Environment/crate", new Vector3(11.9f, QuayTop, -4.8f), -8f);
        Place("Environment/barrel", new Vector3(12.7f, QuayTop, -4.6f), 0f);

        // The training Laden: idle and immortal, facing the way in.
        var dummy = Laden("Training Laden", new Vector3(18f, QuayTop, 0f), -90f);
        dummy.passive = true;
        dummy.immortal = true;

        var readout = new GameObject("Lab Readout").AddComponent<LabReadout>();
        readout.font = AssetDatabase.LoadAssetAtPath<Font>(Fonts + "IMFellEnglish-Regular.ttf");
        tester.readout = heightsTester.readout = readout;

        FinishScene(scene, LabScenePath, start, apprentice, "music_drowned_dark");
    }

    // An invisible ramp over a flight of steps, its top along their nosings from `bottom` (the first step's front edge)
    // to `top` (the last's), `width` across: a smooth slope to walk and find the way on.
    static void Ramp(Transform parent, Vector3 bottom, Vector3 top, float width)
    {
        var ramp = new GameObject("Stair Ramp").transform;
        ramp.SetParent(parent, true);
        Vector3 along = top - bottom;
        ramp.rotation = Quaternion.LookRotation(along, Vector3.up);
        const float thickness = 0.1f;
        ramp.position = (bottom + top) / 2f - ramp.up * (thickness / 2f);
        var box = ramp.gameObject.AddComponent<BoxCollider>();
        box.size = new Vector3(width, thickness, along.magnitude);
    }

    // A Tester's Post (the lab's: set Soured down, give the Apprentice anything), its own pale light.
    static TesterPost TesterPostAt(Vector3 at, float yaw)
    {
        var go = Place("Environment/mooring_post", at, yaw);
        go.name = "Tester's Post";
        var post = go.AddComponent<TesterPost>();
        Flicker(PointLight(go.transform, GlowCentre(go, "Lantern_Glow"), 4f, 2.4f, Hex("#e8f0ff")), 0.05f, 2f, 0.005f);
        return post;
    }

    // The kinds a lab's Tester's Post can set down: switched-off templates, built exactly as in the world.
    static LabSpawner LabTemplates()
    {
        var templates = new GameObject("Lab Templates");
        var spawner = templates.AddComponent<LabSpawner>();
        GameObject Template(GameObject go)
        {
            go.transform.SetParent(templates.transform, true);
            foreach (var soured in go.GetComponentsInChildren<Soured>(true))
                if (soured.droppedLantern != null)
                {
                    soured.droppedLantern.transform.SetParent(templates.transform, true);
                    soured.droppedLantern.gameObject.SetActive(false);
                }
            go.SetActive(false);
            return go;
        }
        var laden = Template(Laden("Laden", new Vector3(0f, QuayTop, 0f), 0f).gameObject);
        var archer = Template(LadenArcher("Laden archer", new Vector3(0f, QuayTop, 0f), 0f).gameObject);
        var clinger = Template(Clinger("Clinger", new Vector3(0f, QuayTop, 0f), 0f).gameObject);
        var unparted = Template(TheUnparted("The Unparted", new Vector3(0f, QuayTop, 0f), 0f).gameObject);
        var drowned = Template(Drowned("Drowned", new Vector3(0f, 0f, -8f), 0f).gameObject);
        var moths = Template(MothFlockAt("Moths", new Vector3(0f, QuayTop + 1.5f, 0f), null, 6).gameObject);
        var gleaner = Template(Gleaner("Gleaner", new Vector3(0f, QuayTop, 0f), 0f).gameObject);
        spawner.kinds = new[]
        {
            new LabSpawner.Kind { name = "Laden", template = laden },
            new LabSpawner.Kind { name = "Laden archer", template = archer },
            new LabSpawner.Kind { name = "Clinger", template = clinger }, // (two near each other pair up)
            new LabSpawner.Kind { name = "The Unparted", template = unparted, listed = false }, // (the miniboss: Formations)
            new LabSpawner.Kind { name = "Drowned", template = drowned, inWater = true }, // (in the water nearest you)
            new LabSpawner.Kind { name = "Moths", template = moths }, // (a flock of six)
            new LabSpawner.Kind { name = "Gleaner", template = gleaner },
        };
        return spawner;
    }

    // The heights: stairs up to a walkway a storey high, into a tower's first floor. Inside, a stair down to the ground
    // floor (its door south) and one up to the roof; a balcony. From the roof, the Hook across to a high platform
    // (too high to drop from: railed), a drop onto a low one, and a drop to the ground: back where you started.
    static void Heights()
    {
        const float storey = 2.4f;
        Vector3 tower = new Vector3(36f, QuayTop, 0.5f);
        var towerRoot = new GameObject("Tower");
        towerRoot.transform.position = tower;
        var storeys = towerRoot.AddComponent<Storeys>();
        GameObject Storey(string model)
        {
            var go = Place("Environment/" + model, tower, 0f);
            go.transform.SetParent(towerRoot.transform, true);
            return go;
        }
        var ground = Storey("tower_ground");
        var middle = Storey("tower_middle");
        var top = Storey("tower_top");
        // Furnished, and lit from within: each storey's things are hidden with it. (Tower-local spots, game directions;
        // a prop's back to the wall: yaw 0 north, 90 east, -90 west.)
        GameObject Prop(GameObject storeyGo, string model, float e, float u, float n, float yaw)
        {
            var go = Place("Environment/" + model, tower + new Vector3(e, u, n), yaw);
            go.transform.SetParent(storeyGo.transform, true);
            return go;
        }
        void Glow(GameObject prop, string material, float range, float intensity, Color colour, float flicker)
        {
            var light = PointLight(prop.transform, GlowCentre(prop, material), range, intensity, colour);
            Flicker(light, flicker, 5f, 0.01f);
        }
        const float g = 0.08f; // the ground floor's flagstones
        const float OutsideWall = 3.5f; // (a lantern on the outside of a wall, hung from it)
        Prop(ground, "tower_props_oar_rack", -3.1f, g, 1.6f, -90f);
        Prop(ground, "tower_props_sacks", -2.4f, g, 2.5f, 0f);
        Prop(ground, "barrel", -2.6f, g, -2.5f, 30f);
        Prop(ground, "crate", -1.8f, g, -2.6f, 12f);
        Prop(ground, "tower_props_rope", -0.4f, g, 2.4f, 0f);
        Glow(Prop(ground, "tower_props_lantern", 0.6f, g, 3.15f, 0f), "HangingLantern_Glow", 6f, 2.6f, LanternLight, 0.12f);
        Prop(middle, "tower_props_table", -1.0f, storey, -1.2f, 0f).name = "Table";
        Glow(GameObject.Find("Table"), "Candle_Glow", 3.5f, 1.4f, Hex("#ffcf7a"), 0.2f);
        Prop(middle, "tower_props_stool", -1.0f, storey, -2.0f, 20f);
        Prop(middle, "tower_props_stool", 0.05f, storey, -1.3f, 70f);
        Prop(middle, "tower_props_cot", -2.6f, storey, -2.1f, 0f);
        Prop(middle, "tower_props_shelf", -3.15f, storey, 2.0f, -90f);
        Prop(middle, "tower_props_sacks", 2.4f, storey, -2.7f, 40f); // (south of the stair's opening: clear of the doors)
        Glow(Prop(middle, "tower_props_lantern", -1.9f, storey, -3.15f, 180f), "HangingLantern_Glow", 6f, 2.4f, LanternLight, 0.12f); // over the cot
        Glow(Prop(top, "tower_props_brazier", -2.1f, 2 * storey, -2.0f, 0f), "Coals_Glow", 7f, 3f, TorchLight, 0.25f);
        Prop(top, "tower_props_rope", 2.3f, 2 * storey, -2.1f, 180f);
        // Warm lanterns by its doors, outside (the village marks its ways in with warm light), and one at the stair.
        Glow(Prop(ground, "tower_props_lantern", -1.25f, 0f, -OutsideWall, 0f), "HangingLantern_Glow", 6f, 2.8f, LanternLight, 0.12f);
        Glow(Prop(middle, "tower_props_lantern", -OutsideWall, storey, -1.25f, 90f), "HangingLantern_Glow", 6f, 2.8f, LanternLight, 0.12f);
        StreetLantern(new Vector3(21.7f, QuayTop, -0.9f), 90f, 2.6f);
        // At its foot, outside: a few crates and barrels against the walls.
        Place("Environment/crate", tower + new Vector3(-4.1f, 0f, -2.6f), 200f);
        Place("Environment/barrel", tower + new Vector3(-4.2f, 0f, -1.7f), 0f);
        Place("Environment/barrel", tower + new Vector3(4.1f, 0f, 2.8f), 60f);
        // Invisible ramps along the stairs' nosings (the Soured's ways follow a slope where they'd trip on steps; and
        // everyone climbs smoothly): the ground floor's, up the east wall rising north; the first floor's, up the north
        // wall rising west. (Tower-local, as tower.py lays them out.)
        Vector3 T(float e, float u, float n) => tower + new Vector3(e, u, n);
        Ramp(ground.transform, T(2.375f, 0.08f + 0.211f, -1.3f), T(2.375f, storey, 1.7f), 1.55f);
        Ramp(middle.transform, T(1.55f, storey + 0.2f, 2.575f), T(-1.75f, 2 * storey, 2.575f), 1.15f);
        storeys.storeys = new[]
        {
            new Storeys.Storey { floor = tower.y, parts = new[] { ground } },
            new Storeys.Storey { floor = tower.y + storey, parts = new[] { middle } },
            new Storeys.Storey { floor = tower.y + 2 * storey, parts = new[] { top } },
        };

        // The walkway into the first floor's west door, and the stair up to it from the west. Railed along both sides
        // and across its west end (but for the stair).
        float walkTop = QuayTop + storey;
        Place("Environment/stone_block_walkway", new Vector3(29.5f, QuayTop, 0.5f), 0f);
        var walkStair = Place("Environment/stone_stairs_storey", new Vector3(22.5f, QuayTop, 0.5f), 90f);
        Ramp(walkStair.transform, new Vector3(22.5f, QuayTop + 0.2f, 0.5f), new Vector3(22.5f + 11 * 0.333f, walkTop, 0.5f), 2.0f);
        for (float x = 26.5f; x < 32.4f; x += 2f)
        {
            Place("Environment/railing_long", new Vector3(x, walkTop, 1.94f), 0f);
            Place("Environment/railing_long", new Vector3(x, walkTop, -0.94f), 0f);
        }
        Place("Environment/railing_stub", new Vector3(26.56f, walkTop, -1f), -90f); // (either side of the 2 m stair)
        Place("Environment/railing_stub", new Vector3(26.56f, walkTop, 1.55f), -90f);

        // The high platform (railed on three sides: its east side drops onto the low one) and the low one.
        Vector3 high = new Vector3(45.5f, QuayTop, 0.5f), low = new Vector3(49.5f, QuayTop, 0.5f);
        Place("Environment/stone_block_high", high, 0f);
        Place("Environment/stone_block_low", low, 0f);
        float highTop = QuayTop + 2.8f;
        Place("Environment/railing_long", new Vector3(43.56f, highTop, -1.5f), -90f);
        Place("Environment/railing_long", new Vector3(43.56f, highTop, 0.5f), -90f);
        Place("Environment/railing_long", new Vector3(43.5f, highTop, 2.44f), 0f);
        Place("Environment/railing_long", new Vector3(45.5f, highTop, 2.44f), 0f);
        Place("Environment/railing_long", new Vector3(43.5f, highTop, -1.44f), 0f);
        Place("Environment/railing_long", new Vector3(45.5f, highTop, -1.44f), 0f);
        StreetLantern(new Vector3(47f, highTop, 2.1f), 180f, 2.2f);

        // The Hook between the roof (through the chained gap in its parapet) and the high platform.
        float roofTop = QuayTop + 2 * storey;
        Vector3 roofPost = new Vector3(38.2f, roofTop, 0.5f), highPost = new Vector3(44.3f, highTop, 1.6f);
        HookPostAt(roofPost, new Vector3(38.2f, roofTop, -0.4f), highPost).arc = 1.6f;
        HookPostAt(highPost, new Vector3(44.8f, highTop, 0.5f), roofPost).arc = 1.6f;

        // Things to find up there (the Tester's Post gives any of them too; resetting the Apprentice puts them back):
        // Ferry Nails at the tower's foot, on its roof and on the high platform; a Held Breath in the middle storey; a
        // Warm Stone on the low platform.
        UpgradePieceAt(UpgradePiece.Kind.FerryNail, tower + new Vector3(3.9f, 0f, -2.2f), 30f);
        UpgradePieceAt(UpgradePiece.Kind.FerryNail, tower + new Vector3(-2.2f, 2 * storey, 1.8f), -20f).transform.SetParent(top.transform, true);
        UpgradePieceAt(UpgradePiece.Kind.FerryNail, new Vector3(46.6f, highTop, -0.5f), 60f);
        UpgradePieceAt(UpgradePiece.Kind.HeldBreath, tower + new Vector3(-2.4f, storey, 1.1f), 0f).transform.SetParent(middle.transform, true);
        UpgradePieceAt(UpgradePiece.Kind.WarmStone, new Vector3(50.4f, QuayTop + 1.4f, 1.3f), 0f);
    }

    // Every kind of thing there is to find, side by side along the quay's north edge, west to east: the pieces that
    // make the Apprentice stronger (a Ferry Nail, a Warm Stone, a Held Breath, an Ember), the four Provisions, an Obol
    // Cache, a keepsake (Old Hesper's son's boat-lantern) and the Hook. Resetting the Apprentice puts them back.
    static void FindsRow(Vector3 start, float step)
    {
        var finds = new GameObject("Lab Finds").AddComponent<LabFinds>();
        int n = 0;
        Vector3 Next() => start + Vector3.right * (step * n++);
        UpgradePieceAt(UpgradePiece.Kind.FerryNail, Next(), 20f);
        UpgradePieceAt(UpgradePiece.Kind.WarmStone, Next(), 0f);
        UpgradePieceAt(UpgradePiece.Kind.HeldBreath, Next(), 0f);
        var ember = Place("Environment/charge_ember", Next(), 0f);
        ember.name = "Ember (Healing Charge Piece)";
        ember.AddComponent<ChargePiece>();
        var emberGlow = ember.AddComponent<PickupGlow>();
        emberGlow.tier = PickupGlow.Tier.Item;
        emberGlow.colour = LanternLight;
        emberGlow.height = 1.0f;
        foreach (var p in Provision.All)
            ProvisionAt(p.Id, 1, Next(), 20f * n);
        var cache = Place("Environment/obol_cache", Next(), 30f);
        cache.name = "Obol Cache";
        cache.AddComponent<ObolCache>();
        var cacheGlow = cache.AddComponent<PickupGlow>();
        cacheGlow.tier = PickupGlow.Tier.Item;
        cacheGlow.colour = LanternLight;
        cacheGlow.height = 0.7f;
        var lantern = Place("Environment/boat_lantern", Next(), 200f);
        lantern.name = "Boat-Lantern (Keepsake)";
        var keepsake = lantern.AddComponent<Keepsake>();
        keepsake.id = HesperKeepsake;
        keepsake.title = "A Boat-Lantern";
        keepsake.hint = "Its glass is painted, crookedly. It's still burning. Old Hesper will know it.";
        keepsake.interactRadius = 1.0f;
        var lanternGlow = lantern.AddComponent<PickupGlow>();
        lanternGlow.tier = PickupGlow.Tier.Item;
        lanternGlow.colour = LanternLight;
        lanternGlow.height = 0.6f;
        var hookSpot = new GameObject("Hook Spot").transform;
        hookSpot.position = Next();
        hookSpot.SetParent(finds.transform, true);
        finds.hookSpot = hookSpot;
        finds.hookModel = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "Environment/hook_head.fbx");
    }

    // Provisions lying about, glowing where they lie (an item).
    static ProvisionFind ProvisionAt(string id, int count, Vector3 at, float yaw)
    {
        var go = Place("Environment/provision_" + id, at, yaw);
        go.name = Provision.ById(id).Name;
        foreach (var c in go.GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(c); // (small: nothing to walk into)
        var find = go.AddComponent<ProvisionFind>();
        find.provision = id;
        find.count = count;
        var glow = go.AddComponent<PickupGlow>();
        glow.tier = PickupGlow.Tier.Item;
        glow.colour = LanternLight;
        glow.height = 0.4f;
        return find;
    }

    // A piece that makes the Apprentice stronger for good, glowing where it lies (an item).
    static UpgradePiece UpgradePieceAt(UpgradePiece.Kind kind, Vector3 at, float yaw)
    {
        var (model, name, colour) = kind switch
        {
            UpgradePiece.Kind.FerryNail => ("upgrade_piece_ferry_nail", "Ferry Nail", Hex("#9fc4ff")),
            UpgradePiece.Kind.WarmStone => ("upgrade_piece_warm_stone", "Warm Stone", Hex("#ff8a4a")),
            _ => ("upgrade_piece_held_breath", "Held Breath", Hex("#d4ecff")),
        };
        var go = Place("Environment/" + model, at, yaw);
        go.name = name;
        var piece = go.AddComponent<UpgradePiece>();
        piece.kind = kind;
        var glow = go.AddComponent<PickupGlow>();
        glow.tier = PickupGlow.Tier.Item;
        glow.colour = colour;
        glow.height = 0.5f;
        return piece;
    }
}
