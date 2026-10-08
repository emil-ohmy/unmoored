using MyGame;
using UnityEditor;
using UnityEngine;

// The Stopped Clocktower's lab (Area 3): the tower alone in the river, climbed floor by floor (art/models/environment/
// clocktower.py and clocktower_props.py). This first step is its architecture: the storeys (each hidden while the
// Apprentice is on one below it), the stairs winding round it, the works stopped at the moment the clock stopped
// (the great wheel, the gears, the bells, the pendulum held mid-swing, the hands at a quarter past nine), the dust and
// the drops hanging still, the cold light. The Tester's Post and a Mooring post on the landing before its door.
public static partial class MainSceneBuilder
{
    const string ClocktowerLabPath = "Assets/_Project/Scenes/Lab_Clocktower.unity";

    // The tower's storeys (as clocktower.py lays them out, from the water): their floors.
    static readonly float[] TowerFloors = { 0.5f, 4.5f, 8.5f, 12.5f, 16.5f, 20.5f };
    const float TowerOut = 8f, TowerIn = 7.2f;

    [MenuItem("Assets/MyGame/Build Lab (Clocktower)")]
    static void BuildClocktowerLabFromMenu()
    {
        if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        BuildClocktowerLab();
    }

    public static void BuildClocktowerLabFromCommandLine() => BuildClocktowerLab();

    // The Stopped Clocktower: no moon to speak of (high, small, cold), a blue-black ambient, the river's mist close in,
    // most of the colour gone out of it: still, and cold, and dark.
    static readonly AreaNight ClocktowerNight = new AreaNight
    {
        volume = "ClocktowerVolume", moon = Hex("#9aaed0"), moonIntensity = 0.4f, moonAngles = new Vector3(62f, -24f, 0f), shadowStrength = 0.85f,
        sky = Hex("#252d40"), equator = Hex("#181d29"), ground = Hex("#08090c"),
        fog = Hex("#0e1118"), fogStart = 16f, fogEnd = 44f,
        saturation = -24f, contrast = 14f, exposure = -0.15f, vignette = 0.42f, grain = 0.3f, filter = Hex("#e6eeff"),
    };

    static readonly Color TowerCold = Hex("#8ea6d8");   // light through its windows and its gears

    static void BuildClocktowerLab()
    {
        var scene = BeginScene(new Vector3(0f, 0f, -8f), ClocktowerNight);
        Clocktower(Vector3.zero);

        // On the landing before the south door: the posts (where you come in).
        float f = TowerFloors[0];
        MooringPostAt(new Vector3(-3.2f, f, -12.6f), 0f);
        var tester = TesterPostAt(new Vector3(3.2f, f, -12.6f), 0f);
        tester.spawner = LabTemplates();
        var start = new GameObject("Start Point").transform;
        start.position = new Vector3(0f, f, -12.8f);
        var entrance = new GameObject("Area Entrance").AddComponent<AreaEntrance>();
        entrance.transform.position = start.position + Vector3.up;
        entrance.areaName = "The Stopped Clocktower (Lab)";
        var apprentice = Apprentice(start.position);
        apprentice.GetComponent<Equipment>().startingCharms = new[] { Charm.TestersKnot.Id };
        var readout = new GameObject("Lab Readout").AddComponent<LabReadout>();
        readout.font = AssetDatabase.LoadAssetAtPath<Font>(Fonts + "IMFellEnglish-Regular.ttf");
        tester.readout = readout;

        FinishScene(scene, ClocktowerLabPath, start, apprentice, "music_clocktower"); // (none yet: silence, and the river)
    }

    // The tower, its storeys hidden above the Apprentice inside (Storeys), its works placed, still, its light; `o` its
    // middle at the water. A point in it is (east, up, north) from `o`.
    static void Clocktower(Vector3 o)
    {
        Vector3 T(float e, float u, float n) => o + new Vector3(e, u, n);
        var root = new GameObject("The Clocktower");
        root.transform.position = o;
        var storeys = root.AddComponent<Storeys>();
        storeys.footprint = new Vector2(TowerOut + 0.3f, TowerOut + 0.3f);
        GameObject Storey(string variant)
        {
            var go = Place("Environment/clocktower_" + variant, o, 0f);
            go.transform.SetParent(root.transform, true);
            return go;
        }
        var undercroft = Storey("undercroft");
        var works = Storey("works");
        var gallery = Storey("gallery");
        var face = Storey("face");
        var belfry = Storey("belfry");
        var roof = Storey("roof");
        Place("Environment/clocktower_landing", o, 0f).transform.SetParent(root.transform, true);
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
            var go = Place("Environment/clocktower_props_" + model, at, 0f);
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

        // The stairs' ramps (the Soured's ways; everyone climbs them smoothly): as clocktower.py lays the stairs out.
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
        Lamp(undercroft, T(0f, 0.4f, 4f), 9f, 1.6f, Hex("#5f86c8"));                 // (the river, under the wheel)
        Lamp(undercroft, T(4.5f, F(0) + 2.6f, -1.5f), 9f, 1.6f, TowerCold);
        Lamp(works, T(-4.5f, F(1) + 2.2f, -6.2f), 8f, 2.2f, TowerCold);
        Lamp(works, T(-6.2f, F(1) + 2.2f, 3.0f), 8f, 2.2f, TowerCold);
        Lamp(works, T(4.0f, F(1) + 2.6f, -5.0f), 8f, 2.0f, TowerCold);
        Lamp(gallery, T(0f, F(2) + 2.6f, 0f), 13f, 2.4f, TowerCold);
        Lamp(gallery, T(-5.8f, F(2) + 2.2f, -5.8f), 7f, 1.6f, TowerCold);
        Lamp(face, T(0f, F(3) + 1.6f, -6.2f), 9f, 2.2f, TowerCold);                   // (the dial's back, glowing)
        Lamp(face, T(-3f, F(3) + 2.2f, 3.5f), 8f, 1.8f, TowerCold);
        Lamp(face, T(0f, F(3) + 1.0f, -9.6f), 8f, 1.6f, TowerCold);                   // (and its face, outside)
        Lamp(belfry, T(0f, F(4) + 2.5f, 0f), 12f, 1.8f, TowerCold);
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
