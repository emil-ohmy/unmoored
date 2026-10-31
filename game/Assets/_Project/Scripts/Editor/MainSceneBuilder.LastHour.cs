using MyGame;
using UnityEditor;
using UnityEngine;

// The Hall of the Last Hour's lab (Area 3): its keep in the river, climbed floor by floor, a spire beside it joined by a
// drive-rod (art/models/environment/last_hour.py and last_hour_props.py). Its architecture: the storeys (each hidden while the
// Apprentice is on one below it), the stairs winding round it, the works stopped at the moment the clock stopped
// (the great wheel, the gears, the bells, the pendulum held mid-swing, the hands at a quarter past nine), the dust and
// the drops hanging still. Pale limestone, brass, green copper; overgrown at its foot; lit soft and pale (after the
// Clockwork City), not dark. The Tester's Post and a Mooring post on the landing before its door.
public static partial class MainSceneBuilder
{
    const string LastHourLabPath = "Assets/_Project/Scenes/Lab_LastHour.unity";

    // The keep's storeys (as last_hour.py lays them out, from the water): their floors.
    static readonly float[] TowerFloors = { 0.5f, 4.5f, 8.5f, 12.5f, 16.5f, 20.5f };
    const float TowerOut = 8f, TowerIn = 7.2f;

    [MenuItem("Assets/MyGame/Build Lab (Last Hour)")]
    static void BuildLastHourLabFromMenu()
    {
        if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        BuildLastHourLab();
    }

    public static void BuildLastHourLabFromCommandLine() => BuildLastHourLab();

    // The Hall of the Last Hour: a high pale moon and a soft pale sky (a hush, not a dark), the river's haze lying light
    // and far, the stone almost cream, the brass warm against it: still, and pale, and sad.
    static readonly AreaNight LastHourNight = new AreaNight
    {
        volume = "LastHourVolume", moon = Hex("#e4e6ee"), moonIntensity = 0.95f, moonAngles = new Vector3(48f, -28f, 0f), shadowStrength = 0.7f,
        sky = Hex("#59627a"), equator = Hex("#3e4555"), ground = Hex("#1a1b20"),
        fog = Hex("#3a4152"), fogStart = 26f, fogEnd = 80f,
        saturation = -4f, contrast = 8f, exposure = 0.2f, vignette = 0.28f, grain = 0.22f, filter = Hex("#fff7ea"),
    };

    static readonly Color TowerCold = Hex("#b8c8e8");   // the moon's light through its windows and its gears
    static readonly Color TowerBrass = Hex("#ffd79a");  // lamplight on brass

    static void BuildLastHourLab()
    {
        var scene = BeginScene(new Vector3(0f, 0f, -8f), LastHourNight);
        LastHour(Vector3.zero);

        Castle(Vector3.zero);

        // In the court, just inside the gate (the west wall's): the posts (where you come in).
        float f = TowerFloors[0];
        MooringPostAt(new Vector3(-11.8f, f, -20.4f), 90f);
        var tester = TesterPostAt(new Vector3(-11.8f, f, -13.6f), 90f);
        tester.spawner = LabTemplates();
        var start = new GameObject("Start Point").transform;
        start.position = new Vector3(-10.5f, f, -17f);
        var entrance = new GameObject("Area Entrance").AddComponent<AreaEntrance>();
        entrance.transform.position = start.position + Vector3.up;
        entrance.areaName = "The Hall of the Last Hour (Lab)";
        var apprentice = Apprentice(start.position);
        apprentice.GetComponent<Equipment>().startingCharms = new[] { Charm.TestersKnot.Id };
        // The Hall's time: stopped, everything not in time drained grey; set going (the central engine, not built yet;
        // the Tester's Post), the colour comes back in a wave out from the keep's clockwork.
        var time = new GameObject("The Hall's Time").AddComponent<HallTime>();
        time.transform.position = new Vector3(0f, f, 0f);
        var readout = new GameObject("Lab Readout").AddComponent<LabReadout>();
        readout.font = AssetDatabase.LoadAssetAtPath<Font>(Fonts + "IMFellEnglish-Regular.ttf");
        tester.readout = readout;

        FinishScene(scene, LastHourLabPath, start, apprentice, "music_last_hour_1", "music_last_hour_2"); // The Last Hour: stopped; awake
    }

    // The castle round the keep (last_hour.py: courtyard, gatehouse, hall, hall_roof): the walled court before the keep's
    // door, the gate in its west wall (its tower with a stopped clock over the passage, the quay outside), the great hall
    // along its east side (its roof hidden while you're in it) with its clocks, its pendulums and its orrery; domed
    // turrets at the corners; green in the court; its light; the still air. `o` the keep's middle at the water.
    static void Castle(Vector3 o)
    {
        Vector3 T(float e, float u, float n) => o + new Vector3(e, u, n);
        float f = TowerFloors[0];
        var root = new GameObject("The Hall of the Last Hour (castle)").transform;
        GameObject Put(string model, Vector3 at, float yaw = 0f, Transform parent = null)
        {
            var go = Place("Environment/" + model, at, yaw);
            go.transform.SetParent(parent != null ? parent : root, true);
            return go;
        }
        Put("last_hour_courtyard", o);
        Put("last_hour_gatehouse", o);
        // The gate tower's clock, in its south face over the passage; its hands at a quarter past nine.
        foreach (var model in new[] { "dial", "hand_hour", "hand_minute" })
        {
            var d = Put("last_hour_props_" + model, T(-16.5f, f + 6.4f, -15.02f));
            d.transform.localScale = Vector3.one * 0.5f;
        }

        // The great hall: its ground (walls, floor, beams) and its roof (hidden while you're inside).
        var hallRoot = new GameObject("The Great Hall").transform;
        hallRoot.SetParent(root, true);
        hallRoot.position = T(22f, 0f, -11.4f);
        var hall = Put("last_hour_hall", o, 0f, hallRoot);
        var roof = Put("last_hour_hall_roof", o, 0f, hallRoot);
        var hallStoreys = hallRoot.gameObject.AddComponent<Storeys>();
        hallStoreys.footprint = new Vector2(6f, 15.4f);
        hallStoreys.storeys = new[]
        {
            new Storeys.Storey { floor = o.y + f, parts = new[] { hall } },
            new Storeys.Storey { floor = o.y + 99f, parts = new[] { roof } },
        };
        // Its door: a great gear stood in the doorway, outside; its drives (silver: the Stroke moves them) beside it in
        // the court, north, and just inside; struck at either's plate, it rolls aside along its rack, a while, then
        // rolls back and shuts it.
        var door = Put("last_hour_props_gear_large", T(15.7f, f + 2.0f, -12.5f));
        door.name = "The Great Hall's Gear Door";
        door.transform.rotation = Quaternion.Euler(0f, 0f, 90f) * door.transform.rotation;
        var doorDrive = Drive(T(15.55f, f + 1.35f, -9.3f), root);
        var doorWork = Machine("The Gear Door's Clockwork", doorDrive.transform, T(14.6f, f, -9.3f), 8f, root);
        var inside = Drive(T(17.05f, f + 1.35f, -9.6f), hall.transform);
        var inner = new GameObject("The Gear Door's Drive (inside)").AddComponent<ClockworkDrive>();
        inner.transform.SetParent(doorWork.transform, true);
        inner.transform.position = inside.transform.position;
        inner.clockwork = doorWork;
        inner.strikePoint = StrikePlate(T(18.0f, f, -9.6f), inner.transform);
        inner.strikeRadius = 1.3f;
        doorWork.spinners = new[]
        {
            new Clockwork.Spinner { part = doorDrive.transform, axis = Vector3.right, speed = 90f },
            new Clockwork.Spinner { part = inside.transform, axis = Vector3.right, speed = -90f },
        };
        Marks(doorWork, new GameObject[0], doorWork.strikePoint, inner.strikePoint);
        var rolls = door.AddComponent<ClockworkMover>();
        rolls.clockwork = doorWork;
        rolls.travel = new Vector3(0f, 0f, -4.2f);
        rolls.travelTime = 2f;
        rolls.rollAxis = Vector3.right;
        rolls.rollRadius = 2f;

        // Its clocks down both long walls (their backs to the walls; the door's stretch left clear), the pendulums
        // from the tie-beams stopped at their own angles, the orrery at its north end.
        for (float n = -24.4f; n < 2.2f; n += 2.6f)
        {
            if (n > -15.2f && n < -7.5f)
                continue; // (the door, and its drive inside)
            Put("last_hour_props_longcase", T(17.15f, f, n), -90f, hall.transform);
        }
        for (float n = -24.4f; n < 2.2f; n += 2.6f)
            Put("last_hour_props_longcase", T(26.85f, f, n), 90f, hall.transform);
        foreach (var (n, tilt) in new[] { (-20f, -14f), (-12f, 9f), (-4f, -5f) })
        {
            var p = Put("last_hour_props_pendulum_hall", T(22f, 9.5f - 0.6f - 0.2f, n), 0f, hall.transform);
            p.transform.rotation = Quaternion.AngleAxis(tilt, Vector3.forward) * p.transform.rotation;
        }
        Put("last_hour_props_orrery", T(22f, f, -0.4f), 30f, hall.transform);
        // Two of the household keep the orrery, side by side up the aisle, facing the door's end.
        foreach (var e in new[] { 20.6f, 23.4f })
            Punctual("Punctual", T(e, f, -4.6f), 180f);
        foreach (var n in new[] { -20f, -12f, -4f })
            foreach (var e in new[] { 19.4f, 24.6f })
            {
                var c = Put("last_hour_props_chandelier", T(e, 9.5f - 1.0f, n), 0f, hall.transform);
                foreach (var col in c.GetComponentsInChildren<Collider>())
                    Object.DestroyImmediate(col); // (hung overhead)
                Flicker(PointLight(c.transform, T(e, 9.5f - 3.0f, n), 7f, 1.6f, TowerBrass), 0.06f, 4f, 0.005f);
            }
        // Two mourners at the hall's south end, among the clocks they stopped, facing up the aisle.
        foreach (var (e, n) in new[] { (19.8f, -20.6f), (23.8f, -21.4f) })
            Keener("Keener", T(e, f, n), 0f);
        Put("last_hour_props_hourglass", T(18.6f, f, -25.2f), 0f, hall.transform);
        Put("last_hour_props_hourglass", T(25.4f, f, -25.2f), 0f, hall.transform);
        Put("last_hour_props_music_drum", T(22f, f, -23.4f), 0f, hall.transform);
        Put("last_hour_props_workbench", T(19.4f, f, -7.6f), -90f, hall.transform);
        Put("last_hour_props_workbench", T(24.6f, f, -17.4f), 90f, hall.transform);
        Put("last_hour_props_cart", T(24.4f, f, -8.4f), 90f, hall.transform);

        // Domed turrets at the north corners (nothing tall south of where you walk: the camera looks down from there).
        foreach (var (e, n) in new[] { (-14.6f, -7.6f), (28.2f, 3.8f) })
            Put("last_hour_props_turret", T(e, 0f, n));

        // The court's clutter (nothing grows here): the fountain, its water held in the air; a cart of gears; crates;
        // spare wheels against the hall's wall; a mainspring on its cradle; an hourglass, its sand caught falling.
        // Two of the household's hounds keep the court, by the hall's door.
        foreach (var e in new[] { 9.0f, 11.5f })
            Wound("Wound", T(e, f, -11.5f), 180f);
        Put("last_hour_props_fountain", T(6f, f, -21f));
        FountainWater(root, T(6f, f, -21f));
        Put("last_hour_props_cart", T(9.4f, f, -25.0f), 0f);
        Put("last_hour_props_crane", T(12.0f, f, -25.6f), -90f); // (by the south parapet, its crate stopped mid-air over the court)

        // The gear lift on the court's west side: its toothed column, its deck at the paving's level; its drive gear on
        // the column's far side; struck from the deck (the crescent's on it): it climbs to the walk along the west wall's top (a cache waits
        // there), a while, then sinks.
        Put("last_hour_props_lift_column", T(-10.7f, f, -23.2f));
        var deck = Put("last_hour_props_lift_deck", T(-11.8f, f, -23.2f));
        var liftDrive = Drive(T(-10.15f, f + 1.35f, -23.2f), root);
        // (struck from its deck: the crescent's in its middle, and rides up and down with it)
        var lift = Machine("The Gear Lift's Clockwork", liftDrive.transform, deck.transform.position, 8f, root, plate: false);
        lift.strikePoint.SetParent(deck.transform, true);
        lift.strikeRadius = 1.0f;
        lift.spinners = new[] { new Clockwork.Spinner { part = liftDrive.transform, axis = Vector3.right, speed = 90f } };
        Marks(lift, new[] { deck }, lift.strikePoint);
        var climbs = deck.AddComponent<ClockworkMover>();
        climbs.clockwork = lift;
        climbs.travel = new Vector3(0f, CurtainTop - f, 0f);
        climbs.travelTime = 3f;
        climbs.carries = true;
        climbs.deck = new Vector2(1.6f, 1.6f);
        var cache = Put("obol_cache", T(-13.6f, CurtainTop, -21.2f), 90f);
        cache.name = "Obol Cache (the west wall's walk)";
        cache.AddComponent<ObolCache>();
        var glow = cache.AddComponent<PickupGlow>();
        glow.tier = PickupGlow.Tier.Item;
        glow.colour = LanternLight;
        glow.height = 0.7f;
        Put("last_hour_props_crate_gears", T(12.6f, f, -24.2f), 10f);
        Put("last_hour_props_crate_gears", T(13.6f, f, -23.0f), 35f);
        Put("last_hour_props_crate_gears", T(-12.2f, f, -9.6f), 80f);
        Put("last_hour_props_gear_stack", T(15.4f, f, -20.4f), 90f);
        Put("last_hour_props_mainspring", T(-9.4f, f, -9.4f), 0f);
        Put("last_hour_props_hourglass", T(10.6f, f, -9.6f));

        // Light: lanterns at the gate and down the court, brass lamps in the hall, the moon's light at its windows.
        foreach (var at in new[] { T(-13.6f, f + 3.2f, -19.4f), T(-13.6f, f + 3.2f, -14.6f), T(-19.8f, f + 3.2f, -19.4f), T(-19.8f, f + 3.2f, -14.6f) })
            Flicker(PointLight(root, at, 8f, 2.6f, LanternLight), 0.12f, 5f, 0.01f);
        foreach (var at in new[] { T(-9f, f + 3.4f, -17f), T(9f, f + 3.4f, -17f), T(0f, f + 3.4f, -12f) })
            PointLight(root, at, 11f, 2.0f, TowerBrass);
        PointLight(hall.transform, T(22f, f + 4.5f, 1f), 11f, 2.8f, TowerBrass);              // (over the orrery)
        foreach (var n in new[] { -20f, -12f, -4f })
            PointLight(hall.transform, T(26.2f, f + 3.6f, n), 6f, 1.6f, TowerCold);

        // The still air: dust over the court and in the hall; the leaves that were falling in the court, held.
        void Still(Transform parent, Vector3 centre, Vector3 size, int motes, Color dust)
        {
            var go = new GameObject("Still Air");
            go.transform.SetParent(parent, true);
            go.transform.position = centre;
            var still = go.AddComponent<StillAir>();
            still.size = size;
            still.motes = motes;
            still.dust = dust;
            still.material = sparkMaterial;
        }
        Still(root, T(1f, f + 2.4f, -17f), new Vector3(28f, 4f, 17f), 220, new Color(0.95f, 0.9f, 0.78f, 0.3f));
        Still(root, T(1f, f + 1.8f, -17f), new Vector3(26f, 3.2f, 16f), 60, new Color(0.62f, 0.8f, 0.44f, 0.8f)); // (leaves)
        Still(hall.transform, T(22f, f + 3.2f, -11.4f), new Vector3(10f, 5.6f, 28f), 260, new Color(0.95f, 0.9f, 0.78f, 0.34f));
    }

    // The fountain's water: its basin full and still, and the arcs it was throwing from its four spouts held in the air,
    // drop by drop.
    static void FountainWater(Transform parent, Vector3 at)
    {
        var water = GameObject.CreatePrimitive(PrimitiveType.Cylinder); // (a round sheet of it, brim-full)
        water.name = "Fountain Water";
        Object.DestroyImmediate(water.GetComponent<Collider>());
        water.transform.SetParent(parent, true);
        water.transform.SetPositionAndRotation(at + Vector3.up * 0.69f, Quaternion.identity);
        water.transform.localScale = new Vector3(3.9f, 0.005f, 3.9f);
        water.GetComponent<MeshRenderer>().sharedMaterial = ChannelWater();
        var drops = new System.Collections.Generic.List<Vector3>();
        for (int k = 0; k < 4; k++)
        {
            float a = Mathf.PI / 4f + k * Mathf.PI / 2f;
            var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            for (int i = 0; i <= 9; i++)
            {
                float t = i / 9f;
                drops.Add(at + dir * (0.9f + 0.95f * t) + Vector3.up * (2.25f + 0.7f * t - 1.55f * t * t));
            }
        }
        var go = new GameObject("Still Air (the fountain)");
        go.transform.SetParent(parent, true);
        go.transform.position = at + Vector3.up * 1.5f;
        var still = go.AddComponent<StillAir>();
        still.size = Vector3.one * 0.1f;
        still.motes = 0;
        still.drops = drops.ToArray();
        still.material = sparkMaterial;
    }

    const float CurtainTop = 5.0f; // (last_hour.py's CURTAIN: the curtain walls' tops, the walk along the west one)

    // A machine of the Hall: a Clockwork struck where `strike` is (on the floor by its drive), running `runTime`.
    static Clockwork Machine(string name, Transform drive, Vector3 strike, float runTime, Transform parent, bool plate = true)
    {
        var c = new GameObject(name).AddComponent<Clockwork>();
        c.transform.SetParent(parent, true);
        c.transform.position = drive.position;
        c.strikePoint = new GameObject("Strike").transform;
        c.strikePoint.SetParent(c.transform, true);
        c.strikePoint.position = strike;
        c.strikeRadius = 1.3f;
        if (plate)
            StrikePlate(strike, c.strikePoint);
        c.runTime = runTime;
        c.thawRadius = 7f;
        ClockworkSounds(c);
        return c;
    }

    // A machine's drive: the silver gear the Stroke sets running (stood up, its faces east and west).
    static GameObject Drive(Vector3 at, Transform parent)
    {
        var go = Place("Environment/last_hour_props_drive_gear", at, 0f);
        go.transform.SetParent(parent, true);
        go.transform.rotation = Quaternion.Euler(0f, 0f, 90f) * go.transform.rotation;
        return go;
    }

    // Where to use the Stroke on a machine: a silver ring in the floor, the crescent in it (stand on it, or send the
    // wave at it); under `parent`, so the machine's own.
    static Transform StrikePlate(Vector3 at, Transform parent)
    {
        var go = Place("Environment/last_hour_props_strike_plate", at, 0f);
        foreach (var col in go.GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(col); // (flush with the floor)
        go.transform.SetParent(parent, true);
        var point = parent.name.StartsWith("Strike") ? parent : go.transform;
        go.name = "Strike Plate";
        return point;
    }

    // A machine's crescents (its plates; a lift's deck) glow when the Stroke can set it running.
    static void Marks(Clockwork c, GameObject[] parts, params Transform[] points)
    {
        foreach (var p in parts)
        {
            var mark = p.AddComponent<StrokeMark>();
            mark.clockwork = c;
            mark.points = points;
        }
        foreach (var point in points)
        {
            var plate = point.name == "Strike Plate" ? point : point.Find("Strike Plate");
            if (plate == null)
                continue;
            var mark = plate.gameObject.AddComponent<StrokeMark>();
            mark.clockwork = c;
            mark.points = points;
        }
    }

    static void ClockworkSounds(Clockwork c)
    {
        c.startSound = Clips("clockwork_start");
        c.tickSound = Clips("clockwork_tick");
        c.stopSound = Clips("clockwork_stop");
    }

    // The keep's clutter, storey by storey: a working clock-house's (benches, shelves of movements, crates of spare
    // wheels, the lathe, hoists, mainsprings, hourglasses), clear of every way through. (`works` etc. its storeys.)
    static void KeepClutter(System.Func<GameObject, string, Vector3, float, GameObject> clutter, GameObject undercroft, GameObject works,
                            GameObject gallery, GameObject face, System.Func<float, float, float, Vector3> T)
    {
        float F(int i) => TowerFloors[i];
        // The undercroft: crates and a stack of wheels by the west wall, a shelf by the door, a mainspring, a bench by
        // the channel's rail; hoists over the river.
        clutter(undercroft, "crate_gears", T(-6.3f, F(0), -6.3f), 0f);
        clutter(undercroft, "crate_gears", T(-5.0f, F(0), -6.5f), 15f);
        clutter(undercroft, "gear_stack", T(-6.6f, F(0), -2.6f), -90f);
        clutter(undercroft, "shelf", T(-4.6f, F(0), -6.95f), 180f);
        clutter(undercroft, "mainspring", T(-3.0f, F(0), -6.0f), 0f);
        clutter(undercroft, "workbench", T(-1.8f, F(0), 1.55f), 0f);
        foreach (var e in new[] { 3.4f, -5.0f })
            foreach (var c in clutter(undercroft, "hoist", T(e, F(1) - 0.2f, 4f), 0f).GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(c);
        // The works: a bench and the lathe along the south wall, a shelf on the west, crates in the north-west corner, a
        // mainspring; an hourglass by the stair; a hoist over the great floor-gear.
        clutter(works, "workbench", T(2.6f, F(1), -6.6f), 180f);
        clutter(works, "lathe", T(-0.6f, F(1), -6.5f), 180f);
        clutter(works, "shelf", T(-6.95f, F(1), -0.5f), -90f);
        clutter(works, "crate_gears", T(-6.3f, F(1), 5.0f), 0f);
        clutter(works, "crate_gears", T(-4.9f, F(1), 6.4f), 25f);
        clutter(works, "mainspring", T(-3.2f, F(1), 6.3f), 0f);
        clutter(works, "hourglass", T(4.3f, F(1), -4.6f), 0f);
        foreach (var c in clutter(works, "hoist", T(-4.2f, F(2) - 0.2f, -4.4f), 0f).GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(c);
        // The gallery: shelves along the south, a bench on the east, crates.
        clutter(gallery, "shelf", T(-5.6f, F(2), -6.95f), 180f);
        clutter(gallery, "shelf", T(5.6f, F(2), -6.95f), 180f);
        clutter(gallery, "workbench", T(6.6f, F(2), -2.0f), 90f);
        clutter(gallery, "crate_gears", T(6.3f, F(2), 3.8f), 0f);
        // The face room: a bench and a shelf along the north wall, an hourglass, a mainspring, crates by the dial.
        clutter(face, "workbench", T(-3.4f, F(3), 6.55f), 0f);
        clutter(face, "shelf", T(3.0f, F(3), 6.95f), 0f);
        clutter(face, "hourglass", T(-4.6f, F(3), 4.0f), 0f);
        clutter(face, "mainspring", T(3.4f, F(3), 2.8f), 0f);
        clutter(face, "crate_gears", T(-2.8f, F(3), -6.5f), 0f);
        clutter(face, "crate_gears", T(2.8f, F(3), -6.6f), 40f);
        clutter(face, "dial_train", T(0f, F(3), -6.2f), 0f);                   // (behind the dial: what turns its hands)
        clutter(undercroft, "gauge_panel", T(6.9f, F(0), -6.2f), 90f);
    }

    // The keep, its storeys hidden above the Apprentice inside (Storeys), its works placed, still, its light; the spire
    // beside it and the drive-rod between; the brass gears on its walls; the green over its foot. `o` its middle at the
    // water. A point in it is (east, up, north) from `o`.
    static void LastHour(Vector3 o)
    {
        Vector3 T(float e, float u, float n) => o + new Vector3(e, u, n);
        var root = new GameObject("The Hall of the Last Hour (keep)");
        root.transform.position = o;
        var storeys = root.AddComponent<Storeys>();
        storeys.footprint = new Vector2(TowerOut + 0.3f, TowerOut + 0.3f);
        GameObject Storey(string variant)
        {
            var go = Place("Environment/last_hour_" + variant, o, 0f);
            go.transform.SetParent(root.transform, true);
            return go;
        }
        var undercroft = Storey("undercroft");
        var works = Storey("works");
        var gallery = Storey("gallery");
        var face = Storey("face");
        var belfry = Storey("belfry");
        var roof = Storey("roof");
        storeys.storeys = new[]
        {
            new Storeys.Storey { floor = o.y + TowerFloors[0], parts = new[] { undercroft } },
            new Storeys.Storey { floor = o.y + TowerFloors[1], parts = new[] { works } },
            new Storeys.Storey { floor = o.y + TowerFloors[2], parts = new[] { gallery } },
            new Storeys.Storey { floor = o.y + TowerFloors[3], parts = new[] { face } },
            new Storeys.Storey { floor = o.y + TowerFloors[4], parts = new[] { belfry } },
            new Storeys.Storey { floor = o.y + 99f, parts = new[] { roof } }, // (never stood on: gone whenever you're inside)
        };

        // The works, still. (`storey`: whose it is, hidden with it.)
        GameObject Works(GameObject storey, string model, Vector3 at, Quaternion turn)
        {
            var go = Place("Environment/last_hour_props_" + model, at, 0f);
            go.transform.rotation = turn * go.transform.rotation;
            go.transform.SetParent(storey.transform, true);
            return go;
        }
        var upright = Quaternion.Euler(90f, 0f, 0f);   // a gear stood on its edge, its axle north
        var flat = Quaternion.identity;
        float F(int i) => TowerFloors[i];
        // The undercroft: the great wheel in the river's channel; above it the crown gear and the main shaft going up
        // through every floor; the pendulum hanging down the middle from the face room, held mid-swing (a quarter of its
        // arc out to the west), its bob in the undercroft. (Long things belong to the lowest storey: never hidden
        // while you're inside.)
        // (what turns when the keep's clockwork runs: each part, its world axle, its speed)
        var turning = new System.Collections.Generic.List<Clockwork.Spinner>();
        GameObject Turning(GameObject go, Vector3 axis, float speed)
        {
            turning.Add(new Clockwork.Spinner { part = go.transform, axis = axis, speed = speed });
            return go;
        }
        Turning(Works(undercroft, "great_wheel", T(0f, 0.4f, 4f), flat), Vector3.forward, 30f);
        Turning(Works(undercroft, "gear_small", T(0f, 3.1f, 4f), flat), Vector3.up, -60f);
        Turning(Works(undercroft, "shaft", T(0f, 3.0f, 4f), flat), Vector3.up, 60f);
        var pendulum = Works(undercroft, "pendulum", T(0f, F(4) - 0.2f, -0.5f), Quaternion.AngleAxis(-12f, Vector3.forward));
        // The works: a great gear stood up on the shaft's line, a crown gear on the shaft, a pair beside it.
        Turning(Works(works, "gear_large", T(-1.5f, F(1) + 2.3f, 3.0f), upright), Vector3.forward, 25f); // (clear of the way to the stair up)
        Turning(Works(works, "gear_small", T(0f, F(1) + 1.2f, 4f), flat), Vector3.up, 60f);
        Turning(Works(works, "gear_small", T(-3.6f, F(1) + 1.6f, 4.6f), upright), Vector3.forward, -45f);
        Turning(Works(works, "gear_large", T(-4.6f, F(1) + 0.5f, -4.2f), flat), Vector3.up, -20f);
        // The bells, hung from the beams over the open middle.
        Works(gallery, "bell_large", T(-2.0f, F(3) - 0.7f, 2.6f), flat);
        Works(gallery, "bell_small", T(2.4f, F(3) - 0.7f, 2.6f), flat);
        Works(gallery, "bell_small", T(2.6f, F(3) - 0.7f, -2.6f), flat);
        // The face: the dial in its frame (from outside: the south), the hands stopped at a quarter past nine (the
        // hour hand to the west, the minute hand to the east: bridges along the face between the ledges); inside, the
        // escapement's gears on the shaft's head.
        Works(face, "dial", T(0f, F(3), -TowerOut), flat);
        Works(face, "hand_hour", T(0f, F(3), -TowerOut), flat);
        Works(face, "hand_minute", T(0f, F(3), -TowerOut), flat);
        Turning(Works(face, "gear_small", T(0f, F(3) + 2.2f, 4.4f), upright), Vector3.forward, 50f);
        Turning(Works(face, "gear_large", T(0f, F(3) + 2.4f, 0.4f), upright), Vector3.forward, -25f);

        // Outside: brass gears hung on the walls (each with its storey: hidden with it), the spire in the river to the
        // west with the drive-rod across to it from the keep, green over the landing.
        var facingSouth = Quaternion.Euler(90f, 0f, 0f);   // a wall gear on the south face, its axle north
        var facingEast = Quaternion.Euler(0f, 0f, 90f);    // on the east face, its axle east
        const float face_ = TowerOut + 0.12f;
        Turning(Works(works, "gear_wall_large", T(-5.6f, F(1) + 2.6f, -face_), facingSouth), Vector3.forward, 18f);
        Turning(Works(works, "gear_wall_small", T(5.8f, F(1) + 1.9f, -face_), facingSouth), Vector3.forward, -34f);
        Turning(Works(works, "gear_wall_large", T(face_, F(1) + 2.4f, -2.2f), facingEast), Vector3.right, -18f);
        Turning(Works(gallery, "gear_wall_small", T(-5.8f, F(2) + 2.8f, -face_), facingSouth), Vector3.forward, 34f);
        Turning(Works(gallery, "gear_wall_small", T(face_, F(2) + 2.6f, 3.6f), facingEast), Vector3.right, 34f);
        Turning(Works(face, "gear_wall_small", T(face_, F(3) + 2.2f, -3.8f), facingEast), Vector3.right, -34f);
        // The clock's heart where it can be seen: the escapement hung in the belfry's south arch, stopped mid-tick; on the
        // east face, counterweights hanging on their chains from the cogged crowns, held where they stopped.
        var escapement = Works(belfry, "escapement", T(0f, F(4) + 1.9f, -TowerOut - 0.35f), flat);
        Works(belfry, "great_bell", T(-2.0f, F(4), 1.6f), flat);                 // the hour bell, in its frame
        Works(belfry, "winding_drum", T(-4.6f, F(4), -4.6f), flat);
        Works(works, "counterweight", T(TowerOut + 0.5f, F(3) - 0.4f, 1.4f), flat);
        Works(gallery, "counterweight", T(TowerOut + 0.5f, F(4) - 0.4f, 5.6f), flat);
        var spire = Place("Environment/last_hour_props_spire", T(-15.6f, 0f, 2f), 0f);
        spire.name = "The Spire";
        var drive = Place("Environment/last_hour_props_drive", T(-13.2f, F(2) + 2.0f, 2f), 0f);
        drive.name = "The Drive-Rod";
        Turning(drive, Vector3.right, 40f);

        // The keep's clockwork: struck at the pendulum's bob (in the undercroft), the pendulum swings from where it was
        // held, the escapement ticks, and every wheel of it turns, inside and out, a while; time moves all through it.
        var clockwork = new GameObject("The Keep's Clockwork (the pendulum)").AddComponent<Clockwork>();
        clockwork.transform.SetParent(root.transform, true);
        clockwork.transform.position = T(0f, F(2), 0f);
        clockwork.strikePoint = new GameObject("Strike (the pendulum's bob)").transform;
        clockwork.strikePoint.SetParent(clockwork.transform, true);
        clockwork.strikePoint.position = T(-2.7f, F(0), -0.5f);
        clockwork.strikeRadius = 1.2f;
        StrikePlate(clockwork.strikePoint.position, clockwork.strikePoint);
        clockwork.runTime = 8f;
        clockwork.spinners = turning.ToArray();
        clockwork.swingers = new[]
        {
            new Clockwork.Swinger { part = pendulum.transform, axis = Vector3.forward, amplitude = 12f, period = 2.2f, heldAt = -12f },
            new Clockwork.Swinger { part = escapement.transform, axis = Vector3.forward, amplitude = 3f, period = 1.1f, heldAt = 0f },
        };
        clockwork.thawRadius = 30f;
        ClockworkSounds(clockwork);
        Marks(clockwork, new GameObject[0], clockwork.strikePoint);

        KeepClutter((storey, model, at, yaw) =>
        {
            var go = Place("Environment/last_hour_props_" + model, at, yaw);
            go.transform.SetParent(storey.transform, true);
            return go;
        }, undercroft, works, gallery, face, T);

        // The stairs' ramps (the Soured's ways; everyone climbs them smoothly): as last_hour.py lays the stairs out.
        const float rise = 0.25f, length = 5.12f;
        Ramp(undercroft.transform, T(6.5f, F(0) + rise, -3.5f), T(6.5f, F(1), -3.5f + length), 1.4f);
        Ramp(works.transform, T(3.5f, F(1) + rise, 6.5f), T(3.5f - length, F(2), 6.5f), 1.4f);
        Ramp(gallery.transform, T(-6.5f, F(2) + rise, 3.5f), T(-6.5f, F(3), 3.5f - length), 1.4f);
        Ramp(face.transform, T(6.5f, F(3) + rise, -3.5f), T(6.5f, F(4), -3.5f + length), 1.4f);

        // Its light: a lantern left burning by the door inside (and two outside it), the river's cold glow in the
        // undercroft, cold light at the windows above, the dial lit faintly from behind.
        Light Lamp(GameObject storey, Vector3 at, float range, float intensity, Color colour)
        {
            var light = PointLight(storey.transform, at, range, intensity, colour);
            return light;
        }
        Flicker(Lamp(undercroft, T(-2.4f, F(0) + 2.4f, -6.7f), 7f, 2.2f, LanternLight), 0.12f, 5f, 0.01f);
        Flicker(Lamp(undercroft, T(-1.9f, F(0) + 2.3f, -8.4f), 6f, 2.4f, LanternLight), 0.12f, 5f, 0.01f);
        Flicker(Lamp(undercroft, T(1.9f, F(0) + 2.3f, -8.4f), 6f, 2.4f, LanternLight), 0.12f, 5f, 0.01f);
        Lamp(undercroft, T(0f, 0.4f, 4f), 10f, 2.2f, Hex("#8fb4e0"));                // (the river, under the wheel)
        Lamp(undercroft, T(4.5f, F(0) + 2.8f, -1.5f), 11f, 2.6f, TowerBrass);
        Lamp(undercroft, T(-4.5f, F(0) + 2.8f, -1.5f), 11f, 2.2f, TowerCold);
        Lamp(works, T(-4.5f, F(1) + 2.4f, -5.6f), 11f, 2.8f, TowerCold);
        Lamp(works, T(-5.6f, F(1) + 2.4f, 3.0f), 10f, 2.4f, TowerBrass);
        Lamp(works, T(4.0f, F(1) + 2.8f, -4.0f), 11f, 2.8f, TowerBrass);
        Lamp(gallery, T(0f, F(2) + 2.6f, 0f), 15f, 3.2f, TowerBrass);
        Lamp(gallery, T(-5.6f, F(2) + 2.4f, -5.6f), 10f, 2.4f, TowerCold);
        Lamp(gallery, T(5.6f, F(2) + 2.4f, -5.6f), 10f, 2.4f, TowerCold);
        Lamp(face, T(0f, F(3) + 1.6f, -6.0f), 11f, 2.8f, TowerBrass);                 // (the dial's back, glowing)
        Lamp(face, T(-3f, F(3) + 2.4f, 3.5f), 11f, 2.4f, TowerCold);
        Lamp(face, T(0f, F(3) + 1.0f, -9.8f), 10f, 2.2f, TowerBrass);                 // (and its face, outside)
        Lamp(belfry, T(0f, F(4) + 2.6f, 0f), 14f, 2.4f, TowerCold);
        // A lantern left at the head of every stair (warm: the way on), small.
        foreach (var (storey, at) in new[] { (works, T(6.4f, F(1) + 2.2f, 2.6f)), (gallery, T(-2.6f, F(2) + 2.2f, 6.6f)),
                                            (face, T(-6.4f, F(3) + 2.2f, -2.6f)), (belfry, T(6.4f, F(4) + 2.2f, 2.6f)) })
            Flicker(Lamp(storey, at, 6f, 1.8f, LanternLight), 0.12f, 5f, 0.01f);

        // Time stopped: dust hanging in every storey; under the pipe in the undercroft, the drops it let fall, held.
        void Still(GameObject storey, Vector3 centre, Vector3 size, int motes, Vector3[] drops = null)
        {
            var go = new GameObject("Still Air");
            go.transform.SetParent(storey.transform, true);
            go.transform.position = centre;
            var still = go.AddComponent<StillAir>();
            still.size = size;
            still.motes = motes;
            still.drops = drops ?? new Vector3[0];
            still.material = sparkMaterial;
        }
        var inside = new Vector3(TowerIn * 2f - 0.6f, 3.2f, TowerIn * 2f - 0.6f);
        Still(undercroft, T(0f, F(0) + 2f, -1.5f), new Vector3(13.8f, 3.2f, 9f), 140,
              new[] { T(-3f, 2.25f, 1.25f), T(-3f, 1.95f, 1.24f), T(-3f, 1.58f, 1.25f), T(-3f, 1.14f, 1.23f), T(-3f, 0.72f, 1.25f) });
        Still(works, T(0f, F(1) + 2f, 0f), inside, 160);
        Still(gallery, T(0f, F(2) + 2f, 0f), inside, 160);
        Still(face, T(0f, F(3) + 2f, 0f), inside, 140);
        Still(belfry, T(0f, F(4) + 2f, 0f), inside, 90);
    }
}
