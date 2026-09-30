using MyGame;
using UnityEditor;
using UnityEngine;

// Assets/_Project/Scenes/Lab.unity: the sandbox for trying every kind of Soured and every upgrade side by side. Two
// quays joined into one open floor, a Mooring post (Rest to re-form everything; fit Charms) and beside it the Tester's
// Post: spawn any Soured in front of you (a count, aggressive or idle), give the Apprentice anything, reset. A training
// Laden stands on the east quay (idle and immortal: it never fights back and never dies), and a readout top right shows
// the numbers. Nothing here is saved.
//   Assets menu > MyGame > Build Lab, or headless: -executeMethod MainSceneBuilder.BuildLabFromCommandLine
public static partial class MainSceneBuilder
{
    const string LabScenePath = "Assets/_Project/Scenes/Lab.unity";

    [MenuItem("Assets/MyGame/Build Lab")]
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

        // The Mooring post and the Tester's Post, side by side at the west end.
        MooringPostAt(new Vector3(-6.0f, QuayTop, -3.4f), 0f);
        var testerGo = Place("Environment/mooring_post", new Vector3(-3.4f, QuayTop, -4.1f), 20f);
        testerGo.name = "Tester's Post";
        var tester = testerGo.AddComponent<TesterPost>();
        var testerLight = PointLight(testerGo.transform, GlowCentre(testerGo, "Lantern_Glow"), 4f, 2.4f, Hex("#e8f0ff"));
        Flicker(testerLight, 0.05f, 2f, 0.005f);

        var start = new GameObject("Start Point").transform;
        start.position = new Vector3(-4.6f, QuayTop, -1.6f);
        var entrance = new GameObject("Area Entrance").AddComponent<AreaEntrance>();
        entrance.transform.position = start.position + Vector3.up;
        entrance.areaName = "The Lab";
        var apprentice = Apprentice(start.position);
        apprentice.GetComponent<Equipment>().startingCharms = new[] { Charm.TestersKnot.Id };

        // The kinds the Tester's Post can set down: switched-off templates, built exactly as in the world.
        var templates = new GameObject("Lab Templates");
        var spawner = templates.AddComponent<LabSpawner>();
        Soured Template(Soured soured)
        {
            soured.transform.SetParent(templates.transform, true);
            if (soured.droppedLantern != null)
            {
                soured.droppedLantern.transform.SetParent(templates.transform, true);
                soured.droppedLantern.gameObject.SetActive(false);
            }
            soured.gameObject.SetActive(false);
            return soured;
        }
        var laden = Template(Laden("Laden", new Vector3(0f, QuayTop, 0f), 0f));
        var archer = Template(LadenArcher("Laden archer", new Vector3(0f, QuayTop, 0f), 0f));
        var clinger = Template(Clinger("Clinger", new Vector3(0f, QuayTop, 0f), 0f));
        spawner.kinds = new[]
        {
            new LabSpawner.Kind { name = "Laden", template = laden.gameObject },
            new LabSpawner.Kind { name = "Laden archer", template = archer.gameObject },
            new LabSpawner.Kind { name = "Clinger", template = clinger.gameObject }, // (two near each other pair up)
        };
        tester.spawner = spawner;

        // The training Laden: idle and immortal, facing the way in.
        var dummy = Laden("Training Laden", new Vector3(18f, QuayTop, 0f), -90f);
        dummy.passive = true;
        dummy.immortal = true;

        var readout = new GameObject("Lab Readout").AddComponent<LabReadout>();
        readout.font = AssetDatabase.LoadAssetAtPath<Font>(Fonts + "IMFellEnglish-Regular.ttf");
        tester.readout = readout;

        FinishScene(scene, LabScenePath, start, apprentice, "music_drowned_dark");
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
        Prop(middle, "tower_props_sacks", -2.5f, storey, 0.9f, 40f);
        Glow(Prop(middle, "tower_props_lantern", -3.15f, storey, -0.95f, -90f), "HangingLantern_Glow", 6f, 2.4f, LanternLight, 0.12f);
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
        Place("Environment/stone_stairs_storey", new Vector3(22.5f, QuayTop, 0.5f), 90f);
        for (float x = 26.5f; x < 32.4f; x += 2f)
        {
            Place("Environment/railing_long", new Vector3(x, walkTop, 1.94f), 0f);
            Place("Environment/railing_long", new Vector3(x, walkTop, -0.94f), 0f);
        }
        Place("Environment/railing_short", new Vector3(26.56f, walkTop, -1f), -90f);
        Place("Environment/railing_short", new Vector3(26.56f, walkTop, 1.2f), -90f);

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
    }
}
