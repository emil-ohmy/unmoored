using System.Linq;
using MyGame;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Builds Assets/_Project/Scenes/Title.unity, the first scene of a build: the game's name over the dark water, where a
// Mooring post burns on a little landing with the ferry boat tied up, and the title menu (the save slots).
//   Assets menu > MyGame > Build Title Screen, or headless: -executeMethod MainSceneBuilder.BuildTitleFromCommandLine
public static partial class MainSceneBuilder
{
    const string TitleScenePath = "Assets/_Project/Scenes/Title.unity";

    [MenuItem("Assets/MyGame/Build Title Screen")]
    static void BuildTitleFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        BuildTitle();
    }

    public static void BuildTitleFromCommandLine() => BuildTitle();

    static void BuildTitle()
    {
        var scene = BeginScene(Vector3.zero);
        Place("Environment/jetty_landing", new Vector3(0f, QuayTop - 0.03f, 0f), 0f);
        var post = MooringPostAt(new Vector3(1.25f, QuayTop, -2.66f), 180f);
        post.litAtStart = true; // its lantern burning: the one warm light
        Place("Environment/boat", new Vector3(2.55f, -0.12f, -1.3f), 186f);
        Place("Environment/rock_a", new Vector3(-3.2f, -0.12f, -1.5f), 40f);
        Place("Environment/rock_c", new Vector3(4.8f, -0.15f, 2.4f), 200f);
        Place("Environment/islet_small", new Vector3(-7.5f, -0.6f, 6.5f), 120f);
        Boardwalk(new Vector3(0f, QuayTop - 0.03f, 4.76f), Vector3.forward, 1); // leading off into the dark

        var systems = new GameObject("Title");
        var sfx = systems.AddComponent<Sfx>();
        sfx.uiTick = Clips("ui_tick");
        var menu = systems.AddComponent<TitleMenu>();
        menu.titleFont = AssetDatabase.LoadAssetAtPath<Font>(Fonts + "IMFellEnglishSC-Regular.ttf");
        menu.textFont = AssetDatabase.LoadAssetAtPath<Font>(Fonts + "IMFellEnglish-Regular.ttf");
        var music = new GameObject("Music", typeof(AudioSource));
        music.GetComponent<AudioSource>().clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Audio + "Music/music_drowned_dark.wav");
        music.AddComponent<MusicPlayer>();

        // A still camera, low over the water, the post's light in the lower third.
        var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
        cam.tag = "MainCamera";
        cam.fieldOfView = 34f;
        cam.farClipPlane = 200f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Fog;
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        // (looking left of the landing: it sits to the right of the menu, the list over dark water)
        cam.transform.SetPositionAndRotation(new Vector3(6.5f, 4.2f, -11.5f), Quaternion.LookRotation(new Vector3(-3.6f, 1.2f, -0.2f) - new Vector3(6.5f, 4.2f, -11.5f)));
        var volume = new GameObject("Global Volume").AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = VolumeProfileAsset();

        EditorSceneManager.SaveScene(scene, TitleScenePath);
        AssetDatabase.SaveAssets();
        // The first scene of a build.
        var scenes = EditorBuildSettings.scenes.Where(s => s.path != TitleScenePath).ToList();
        scenes.Insert(0, new EditorBuildSettingsScene(TitleScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log($"[MainSceneBuilder] Built {TitleScenePath}");
    }
}
