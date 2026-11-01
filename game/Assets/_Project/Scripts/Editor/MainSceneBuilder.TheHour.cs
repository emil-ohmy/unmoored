using MyGame;
using UnityEditor;
using UnityEngine;

// The Hour's lab (the One Who Waits, the Hall of the Last Hour's Boss): its chamber behind the great dial on its own,
// high in the clock (the_hour_chamber.py), the Hour hung over the middle of its floor, the floor dial's hands, the
// pendulums; a landing south of the bridge in with the Tester's Post and a Mooring post. The Hall's time is stopped, as
// you find it (the Hour limp: a husk); the Tester's Post sets it running (the real fight) and back, and begins the
// fight at any quarter.
public static partial class MainSceneBuilder
{
    const string TheHourLabPath = "Assets/_Project/Scenes/Lab_OneWhoWaits.unity";
    const float HourFloor = 10f; // (its floor, high in the clock: the dark of the works below, the river far under)

    [MenuItem("Assets/MyGame/Build Lab (The One Who Waits)")]
    static void BuildTheHourLabFromMenu()
    {
        if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        BuildTheHourLab();
    }

    public static void BuildTheHourLabFromCommandLine() => BuildTheHourLab();

    static void BuildTheHourLab()
    {
        var scene = BeginScene(new Vector3(0f, 0f, -8f), LastHourNight);
        var middle = new Vector3(0f, HourFloor, 0f);
        Vector3 T(float e, float u, float n) => middle + new Vector3(e, u, n);
        var root = new GameObject("The Hour's Chamber").transform;
        var chamber = Place("Environment/the_hour_chamber_chamber", middle, 0f);
        chamber.transform.SetParent(root, true);

        // The floor dial's hands, stopped at three minutes to twelve (the hour hand just short of it).
        GameObject Hand(string model, float yaw)
        {
            var go = Place("Environment/" + model, middle + Vector3.up * 0.01f, 0f);
            go.transform.rotation = Quaternion.AngleAxis(yaw, Vector3.up) * go.transform.rotation;
            go.transform.SetParent(root, true);
            foreach (var c in go.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(c); // (low: it sweeps through you, or under you)
            go.AddComponent<NavMeshIgnore>();
            return go;
        }
        var minute = Hand("the_hour_chamber_hand_long", -18f);
        var hour = Hand("the_hour_chamber_hand_short", -1.5f);
        minute.name = "The Floor Dial's Minute Hand";
        hour.name = "The Floor Dial's Hour Hand";

        // The pendulums, west and east of it, hung from their beams, swinging north and south, held mid-swing (it sets
        // them going).
        var pendulums = new System.Collections.Generic.List<Clockwork>();
        foreach (var (e, tilt) in new[] { (-4.6f, 22f), (4.6f, -18f) })
            pendulums.Add(PendulumTrap(root, T(e, 8.2f, 0f), tilt, HourFloor, plate: false, northSouth: true).clockwork);

        var boss = TheHourBoss(middle);
        boss.minuteHand = minute.transform;
        boss.hourHand = hour.transform;
        boss.pendulums = pendulums.ToArray();

        // Its light: the great dial lit through from outside (cold), lamps on the side walls (brass), a glow under the
        // floor's edge from the works.
        var dialLight = PointLight(root, T(0f, 6.8f, 6.2f), 22f, 3.2f, Hex("#cfd8e8"));
        dialLight.shadows = LightShadows.None;
        foreach (var (e, n) in new[] { (-8.9f, 5.5f), (8.9f, 5.5f), (-8.9f, -0.2f), (8.9f, -0.2f) })
            Flicker(PointLight(root, T(e, 3.2f, n), 9f, 2.2f, TowerBrass), 0.06f, 4f, 0.005f);
        PointLight(root, T(0f, -4f, -10f), 16f, 1.6f, Hex("#8a6a3a"));
        PointLight(root, T(0f, 5f, 0f), 14f, 1.4f, TowerCold); // (on it: its mask, its robe)

        // The still air: dust hanging in the chamber.
        var still = new GameObject("Still Air").AddComponent<StillAir>();
        still.transform.SetParent(root, true);
        still.transform.position = T(0f, 3f, 0f);
        still.size = new Vector3(18f, 6f, 16f);
        still.motes = 260;
        still.dust = new Color(0.95f, 0.9f, 0.78f, 0.32f);
        still.material = sparkMaterial;

        // The landing: the posts, and where you come in.
        MooringPostAt(T(-2.6f, 0f, -16.2f), 90f);
        var tester = TesterPostAt(T(2.6f, 0f, -16.2f), -90f);
        tester.spawner = LabTemplates();
        var start = new GameObject("Start Point").transform;
        start.position = T(0f, 0f, -15.6f);
        var entrance = new GameObject("Area Entrance").AddComponent<AreaEntrance>();
        entrance.transform.position = start.position + Vector3.up;
        entrance.areaName = "The One Who Waits (Lab)";
        var apprentice = Apprentice(start.position);
        apprentice.GetComponent<Equipment>().startingCharms = new[] { Charm.TestersKnot.Id };
        var time = new GameObject("The Hall's Time").AddComponent<HallTime>();
        time.transform.position = middle;
        var readout = new GameObject("Lab Readout").AddComponent<LabReadout>();
        readout.font = AssetDatabase.LoadAssetAtPath<Font>(Fonts + "IMFellEnglish-Regular.ttf");
        tester.readout = readout;

        FinishScene(scene, TheHourLabPath, start, apprentice, "music_last_hour_1", "music_last_hour_2");
    }

    // The One Who Waits: the Hour, hung over the middle of its chamber, facing the bridge in (south).
    static TheHour TheHourBoss(Vector3 middle)
    {
        var go = Place("Characters/the_hour", middle, 180f);
        go.name = "The One Who Waits";
        var controller = go.AddComponent<CharacterController>();
        controller.center = new Vector3(0f, 1.8f, 0f);
        controller.height = 3.4f;
        controller.radius = 1.0f;
        controller.minMoveDistance = 0f;
        var h = go.AddComponent<TheHour>();
        h.maxHealth = 1100f;
        h.radius = 1.5f;
        h.knockbackResistance = 1f;
        h.staggerCapacity = 260f;     // (a long run of blows: a moment's opening)
        h.staggerDuration = 1.6f;
        h.aggroRadius = 30f;
        h.viewHalfAngle = 180f;
        h.noticeCloseRadius = 30f;
        h.leashRadius = 60f;
        h.giveUpRange = 60f;
        h.turnSpeed = 80f;            // (a giant hung on cables: it comes round slowly; circling it is a way)
        h.obols = 600;
        h.eyeHeight = 6.4f;
        h.barHeight = 7.6f;
        h.deathDuration = 2.2f;
        h.dissolveDuration = 2.0f;
        h.fareMotes = 120;
        h.floorY = middle.y;
        h.middle = middle;
        SouredAttack Blow(string name, string windup, string strike, string recover, float telegraph, float strikeTime, float recoverTime,
                          float impactAt, float trackUntil, AudioClip[] windupSound, AudioClip[] swingSound) => new SouredAttack
        {
            name = name, windupClip = windup, strikeClip = strike, recoverClip = recover,
            telegraph = telegraph, strike = strikeTime, recover = recoverTime, impactAt = impactAt,
            lunge = 0f, maxLunge = 0f, trackUntil = trackUntil, damage = 0f, reach = 30f, arc = 180f, knockback = 0f, scripted = true,
            windupSound = windupSound, swingSound = swingSound,
        };
        h.hammer = Blow("Hammer", "HammerRaise", "HammerSlam", "HammerRecover", 1.1f, 0.3f, 1.3f, 0.85f, 0.75f, Clips("trap_jack_wind"), Clips("trap_blade_swish"));
        h.blade = Blow("Blade", "BladeDraw", "BladeSweep", "BladeRecover", 0.95f, 0.5f, 1.0f, 0.5f, 0.6f, Clips("trap_gear_release"), Clips("trap_blade_swish"));
        h.grind = Blow("Grind", "GrindDraw", "Grind", "Idle", 0.7f, 0.6f, 0.6f, 0.4f, 1f, Clips("clockwork_start"), Clips("trap_gear_crash"));
        h.stop = Blow("Stop", "StopCast", "StopCast", "Idle", 1.2f, 0.2f, 0.05f, 0.5f, 1f, null, null);
        h.stopSlam = Blow("StopSlam", "HammerRaise", "HammerSlam", "HammerRecover", 0.45f, 0.25f, 1.4f, 0.6f, 1f, null, Clips("trap_blade_swish"));
        h.twitch = Blow("Twitch", "TwitchDraw", "TwitchSwing", "Limp", 1.6f, 1.0f, 0.8f, 0.5f, 1f, Clips("the_hour_twitch"), null);
        h.music = new[] { AssetDatabase.LoadAssetAtPath<AudioClip>(Audio + "Music/music_last_hour_2.wav") };
        h.handSound = Clips("trap_grind");
        h.wakeSound = Clips("the_hour_wake");
        h.chimeSound = Clips("the_hour_chime");
        h.slamSound = Clips("the_hour_slam");
        h.rewindSound = Clips("the_hour_rewind");
        h.stopSound = Clips("the_hour_stop");
        h.stoppedSound = Clips("the_hour_stopped");
        h.deathSound = Clips("last_hour_toll");   // (the hour, struck at last)
        h.limpDeathSound = Clips("the_hour_limp_death");
        h.glanceSound = Clips("clockwork_stop");
        return h;
    }
}
