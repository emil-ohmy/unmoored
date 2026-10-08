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

        // On the landing before the south door: the posts (where you come in).
        float f = TowerFloors[0];
        MooringPostAt(new Vector3(-3.2f, f, -12.6f), 0f);
        var tester = TesterPostAt(new Vector3(3.2f, f, -12.6f), 0f);
        tester.spawner = LabTemplates();
        var start = new GameObject("Start Point").transform;
        start.position = new Vector3(0f, f, -12.8f);
        var entrance = new GameObject("Area Entrance").AddComponent<AreaEntrance>();
        entrance.transform.position = start.position + Vector3.up;
        entrance.areaName = "The Hall of the Last Hour (Lab)";
        var apprentice = Apprentice(start.position);
        apprentice.GetComponent<Equipment>().startingCharms = new[] { Charm.TestersKnot.Id };
        var readout = new GameObject("Lab Readout").AddComponent<LabReadout>();
        readout.font = AssetDatabase.LoadAssetAtPath<Font>(Fonts + "IMFellEnglish-Regular.ttf");
        tester.readout = readout;

        FinishScene(scene, LastHourLabPath, start, apprentice, "music_last_hour"); // (none yet: silence, and the river)
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
        Place("Environment/last_hour_landing", o, 0f).transform.SetParent(root.transform, true);
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
        Works(undercroft, "great_wheel", T(0f, 0.4f, 4f), flat);
        Works(undercroft, "gear_small", T(0f, 3.1f, 4f), flat);
        Works(undercroft, "shaft", T(0f, 3.0f, 4f), flat);
        Works(undercroft, "pendulum", T(0f, F(4) - 0.2f, -0.5f), Quaternion.AngleAxis(-12f, Vector3.forward));
        // The works: a great gear stood up on the shaft's line, a crown gear on the shaft, a pair beside it.
        Works(works, "gear_large", T(-1.5f, F(1) + 2.3f, 3.0f), upright); // (clear of the way to the stair up)
        Works(works, "gear_small", T(0f, F(1) + 1.2f, 4f), flat);
        Works(works, "gear_small", T(-3.6f, F(1) + 1.6f, 4.6f), upright);
        Works(works, "gear_large", T(-4.6f, F(1) + 0.5f, -4.2f), flat);
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
        Works(face, "gear_small", T(0f, F(3) + 2.2f, 4.4f), upright);
        Works(face, "gear_large", T(0f, F(3) + 2.4f, 0.4f), upright);

        // Outside: brass gears hung on the walls (each with its storey: hidden with it), the spire in the river to the
        // west with the drive-rod across to it from the keep, green over the landing.
        var facingSouth = Quaternion.Euler(90f, 0f, 0f);   // a wall gear on the south face, its axle north
        var facingEast = Quaternion.Euler(0f, 0f, 90f);    // on the east face, its axle east
        const float face_ = TowerOut + 0.12f;
        Works(works, "gear_wall_large", T(-5.6f, F(1) + 2.6f, -face_), facingSouth);
        Works(works, "gear_wall_small", T(5.8f, F(1) + 1.9f, -face_), facingSouth);
        Works(works, "gear_wall_large", T(face_, F(1) + 2.4f, -2.2f), facingEast);
        Works(gallery, "gear_wall_small", T(-5.8f, F(2) + 2.8f, -face_), facingSouth);
        Works(gallery, "gear_wall_small", T(face_, F(2) + 2.6f, 3.6f), facingEast);
        Works(face, "gear_wall_small", T(face_, F(3) + 2.2f, -3.8f), facingEast);
        var spire = Place("Environment/last_hour_props_spire", T(-15.6f, 0f, 2f), 0f);
        spire.name = "The Spire";
        Place("Environment/last_hour_props_drive", T(-13.2f, F(2) + 2.0f, 2f), 0f).name = "The Drive-Rod";
        foreach (var (model, e, n, yaw) in new[] { ("tree", -3.9f, -9.4f, 30f), ("tree", 3.9f, -9.2f, 200f), ("shrub", -4.0f, -13.4f, 0f),
                                                  ("shrub", 4.0f, -13.5f, 90f), ("shrub", -2.4f, -8.5f, 45f), ("shrub", 2.5f, -8.6f, 120f) })
        {
            var green = Place("Environment/last_hour_props_" + model, T(e, F(0), n), yaw);
            foreach (var c in green.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(c); // (leaves stop nothing)
            if (model == "tree")
            {
                var trunk = green.AddComponent<CapsuleCollider>();
                trunk.center = new Vector3(0f, 1f, 0f);
                trunk.radius = 0.18f;
                trunk.height = 2f;
            }
        }

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
