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
        var scene = BeginScene(new Vector3(8f, 0f, 0f));

        // The floor: two quays side by side (x -8 .. 24, z -5.5 .. 5.5), lit all round.
        Place("Environment/quay", Vector3.zero, 0f);
        Place("Environment/quay", new Vector3(15.5f, 0f, 0f), 0f);
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
        spawner.kinds = new[]
        {
            new LabSpawner.Kind { name = "Laden", template = laden.gameObject },
            new LabSpawner.Kind { name = "Laden archer", template = archer.gameObject },
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
}
