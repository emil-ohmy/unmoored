using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MyGame;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

// Builds Assets/_Project/Scenes/Main.unity: the first playable slice, a corner of the Drowned City.
// Assets menu > MyGame > Build Main Scene, or headless:
//   -executeMethod MainSceneBuilder.BuildFromCommandLine
//   -executeMethod MainSceneBuilder.BuildAndScreenshot   (env SHOTS = output folder, default builds/shots)
// Re-running replaces the scene; hand edits to Main.unity are lost, so change the layout here.
public static partial class MainSceneBuilder
{
    const string ScenePath = "Assets/_Project/Scenes/Main.unity";
    const string Models = "Assets/_Project/Art/Models/";
    const string Generated = "Assets/_Project/Art/Generated";
    const string Audio = "Assets/_Project/Audio/";
    const string Fonts = "Assets/_Project/Art/Fonts/";
    const float QuayTop = 0.5f;
    // Night: the world is lit by its fires. Light colour carries meaning (docs/brief.md):
    // warm = the world and safety, sickly green = a Soured, cold pale = the Apprentice's bow lantern.
    static readonly Color Fog = Hex("#10131b");
    static readonly Color LanternLight = Hex("#ffb866");
    static readonly Color TorchLight = Hex("#ff8a3d");
    static readonly Color SouredLight = Hex("#9ee860");
    static readonly Color BowLanternLight = Hex("#9fc4ff");
    static Material sparkMaterial;

    // Every generated scene, one after another (the labs, the Boss's arena, the title screen), Main last (left open).
    [MenuItem("Assets/MyGame/Build All Scenes")]
    static void BuildAllFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        BuildAll();
    }

    public static void BuildAllFromCommandLine() => BuildAll();

    static void BuildAll()
    {
        BuildLab();
        BuildOrchardLab();
        BuildBossArena();
        BuildTitle();
        Build();
        Debug.Log("[MainSceneBuilder] Built every scene");
    }

    [MenuItem("Assets/MyGame/Build Main Scene")]
    static void BuildFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        Build();
    }

    public static void BuildFromCommandLine() => Build();

    public static void BuildAndScreenshot()
    {
        Build();
        Screenshots(Environment.GetEnvironmentVariable("SHOTS") ?? Path.GetFullPath("../builds/shots"));
    }

    static void Build()
    {
        var scene = BeginScene(new Vector3(-15f, 0f, -17f)); // the water, centred under the whole loop (the chapel's far out)

        // The quay and what stands on it.
        Place("Environment/quay", new Vector3(0f, 0f, 0f), 180f);
        Place("Environment/ruined_wall_long", new Vector3(-3.2f, QuayTop, 1.8f), 194f);
        Place("Environment/ruined_wall_short", new Vector3(3.4f, QuayTop, 2.6f), 148f);
        Place("Environment/ruined_wall_short", new Vector3(-5.4f, QuayTop, 4.4f), 200f); // clear of the way to the yard
        Place("Environment/barrel", new Vector3(-4.9f, QuayTop, 2.6f), 180f);
        Place("Environment/barrel", new Vector3(-4.2f, QuayTop, 3.0f), 220f);
        Place("Environment/crate", new Vector3(-5.6f, QuayTop, 3.4f), 192f);
        Place("Environment/crate", new Vector3(5.9f, QuayTop, -3.9f), 160f);
        Place("Environment/barrel", new Vector3(6.6f, QuayTop, -3.2f), 180f);
        StreetLantern(new Vector3(-6.4f, QuayTop, -3.9f), 90f);
        StreetLantern(new Vector3(6.2f, QuayTop, 4.0f), -90f);
        foreach (var p in new[] { new Vector3(-7.2f, 0f, -4.3f), new Vector3(7.2f, 0f, -4.5f), new Vector3(-7.3f, 0f, 3.6f),
                                  new Vector3(7.3f, 0f, 1.2f), new Vector3(-2.2f, 0f, 2.9f), new Vector3(0.4f, 0f, 4.9f) })
            Torch(new Vector3(p.x, QuayTop, p.z));

        // The way in: the Apprentice arrives by boat at a landing with the Mooring post, far out in the dark, and walks
        // a long boardwalk towards the torchlit quay. Nothing hostile until the quay: the build-up.
        const float walkX = 2.4f;
        Boardwalk(new Vector3(walkX, QuayTop - 0.03f, -5.0f), Vector3.back, 4);
        Place("Environment/jetty_landing", new Vector3(walkX, QuayTop - 0.03f, -5.0f - Segment * 4), 0f);
        MooringPostAt(new Vector3(walkX + 1.25f, QuayTop, -26.7f), 180f);
        MerchantAt(new Vector3(walkX - 1.25f, QuayTop, -24.35f), 127f); // the landing's north-west corner, clear of the gate
        // The Ferryman's boat, moored along the landing's east side; out in the dark to the south-east, the Far shore.
        var boat = BoatAt(new Vector3(walkX + 2.55f, -0.12f, -25.6f), 6f, new Vector3(walkX + 1.15f, QuayTop, -25.2f));
        var far = FarShore(boat);
        HesperAt(new Vector3(walkX - 1.3f, QuayTop, -26.9f), 180f, new Vector3(walkX + 1.15f, QuayTop, -24.3f), 90f, boat, far); // the south-west corner, looking out
        StreetLantern(new Vector3(walkX + 0.75f, QuayTop, -11.8f), -90f, 2.2f);
        // The title card comes up as the torchlit quay swims into view, before anything there notices you.
        var entrance = new GameObject("Area Entrance").AddComponent<AreaEntrance>();
        entrance.transform.position = new Vector3(walkX, QuayTop + 1f, -10.5f);
        entrance.areaName = "The Drowned City";
        StreetLantern(new Vector3(walkX - 0.75f, QuayTop, -19.4f), 90f, 2.2f);
        Place("Environment/house_b", new Vector3(-2.8f, 0f, -14.5f), 240f);
        Place("Environment/house_a", new Vector3(8.4f, 0f, -19.5f), 70f);
        Place("Environment/rock_b", new Vector3(5.3f, -0.1f, -10.6f), 40f);
        Place("Environment/rock_a", new Vector3(-0.9f, -0.12f, -21.3f), 300f);
        Place("Environment/rock_c", new Vector3(6.6f, -0.15f, -15.8f), 120f);
        Place("Environment/islet_small", new Vector3(-6.0f, -0.6f, -31.0f), 80f);

        // Half-sunken houses around the quay, rocks and islets in the water.
        Place("Environment/house_a", new Vector3(-10.4f, 0f, 3.0f), 280f);
        Place("Environment/house_b", new Vector3(-3.6f, 0f, 8.2f), 8f);
        Place("Environment/house_a", new Vector3(6.4f, 0f, 8.6f), 352f);
        Place("Environment/house_b", new Vector3(11.0f, 0f, 1.4f), 96f);
        Place("Environment/rock_a", new Vector3(-2.6f, -0.12f, -7.4f), 190f);
        Place("Environment/rock_c", new Vector3(-8.8f, -0.15f, -5.6f), 150f);
        Place("Environment/rock_b", new Vector3(8.8f, -0.1f, -5.8f), 250f);
        Place("Environment/rock_b", new Vector3(0.6f, -0.1f, 7.4f), 330f);
        Place("Environment/islet_small", new Vector3(-12.5f, -0.6f, -8.0f), 210f);
        Place("Environment/islet_big", new Vector3(13.5f, -0.8f, 9.5f), 165f);
        Place("Environment/rock_c", new Vector3(12.8f, -0.15f, -3.2f), 20f);

        // The rest of the loop: quay -> yard -> crossroads -> back to the landing through a shortcut gate; and from the
        // crossroads, south to a second Mooring post and the Boss.
        Yard();
        HookIslet();
        CacheDeck();
        Crossroads();
        Loopback();
        ToTheChapel();

        // Characters. Met in order: the Laden alone on the quay, the archer alone in the yard, then both at the
        // crossroads; beyond them, the gate home; and south of the crossroads, past a Mooring post, the Anchoress.
        var start = new GameObject("Start Point").transform;
        start.position = new Vector3(2.4f, QuayTop, -25.3f); // on the landing, by the Mooring post
        var apprentice = Apprentice(start.position);
        Laden("Laden", new Vector3(4.2f, QuayTop, 0.4f), 20f); // back turned: sneak up for a free hit
        LadenArcher("Laden Archer (Yard)", new Vector3(-30.0f, QuayTop, 0.8f), 90f); // at the far end, facing the way in
        Laden("Laden (Crossroads)", new Vector3(-23.0f, QuayTop, -23.0f), 0f); // holds the front, facing the way in
        LadenArcher("Laden Archer (Crossroads)", new Vector3(-19.5f, QuayTop, -28.5f), -30f); // behind it, covering
        // For play-testing: the Tester's Knot (four times the damage) is owned; fit it at any Mooring post.
        apprentice.GetComponent<Equipment>().startingCharms = new[] { Charm.TestersKnot.Id };

        FinishScene(scene, ScenePath, start, apprentice, "music_drowned_dark");
    }

    // The night's look for an Area: its moon, its ambient, its fog and its grade (the post-processing volume). Each Area
    // has one; its lab uses it. Light still carries meaning everywhere: warm firelight the most saturated orange (the
    // world, safety), green the Soured, cold blue the Apprentice.
    sealed class AreaNight
    {
        public string volume;                      // the post-processing profile's asset name
        public Color moon; public float moonIntensity; public Vector3 moonAngles; public float shadowStrength = 0.8f;
        public Color sky, equator, ground;         // the ambient, top to bottom
        public Color fog; public float fogStart, fogEnd;
        public float saturation, contrast, exposure, vignette, grain;
        public Color filter = Color.white;
    }

    // The Drowned City: a weak, cold moon high up, a cold teal-grey murk, fog close in.
    static readonly AreaNight DrownedNight = new AreaNight
    {
        volume = "MainVolume", moon = Hex("#8ea8d8"), moonIntensity = 0.25f, moonAngles = new Vector3(56f, -38f, 0f),
        sky = Hex("#1f2436"), equator = Hex("#141a24"), ground = Hex("#08090c"),
        fog = Hex("#10131b"), fogStart = 14f, fogEnd = 38f,
        saturation = -12f, contrast = 12f, exposure = -0.25f, vignette = 0.36f, grain = 0.35f,
    };

    // The Orchard of Last Autumn: a huge low harvest moon, pale honey-silver, throwing long shadows across the rows; a
    // dusky violet ambient; the air clearer (the mist lies low instead, in the rows); the autumn colour kept.
    static readonly AreaNight OrchardNight = new AreaNight
    {
        volume = "OrchardVolume", moon = Hex("#e6cb94"), moonIntensity = 1.05f, moonAngles = new Vector3(24f, -62f, 0f), shadowStrength = 0.9f,
        sky = Hex("#3a3046"), equator = Hex("#2a2229"), ground = Hex("#0e0b0b"),
        fog = Hex("#221b25"), fogStart = 22f, fogEnd = 56f,
        saturation = -2f, contrast = 16f, exposure = 0f, vignette = 0.38f, grain = 0.3f, filter = Hex("#fff3e2"),
    };

    static AreaNight night = DrownedNight; // (the scene being built's)

    // A new, empty scene with what every scene has before its layout: the night's lighting and the water.
    static UnityEngine.SceneManagement.Scene BeginScene(Vector3 waterCentre, AreaNight areaNight = null)
    {
        night = areaNight ?? DrownedNight;
        AssetDatabase.ImportAsset(Models.TrimEnd('/'), ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(Audio.TrimEnd('/'), ImportAssetOptions.ImportRecursive);
        AssetDatabase.ImportAsset(Fonts.TrimEnd('/'), ImportAssetOptions.ImportRecursive);
        Directory.CreateDirectory(Generated);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        sparkMaterial = ParticleMaterial("M_Spark", RadialTexture("T_SoftDot", ring: false));
        Lighting();

        var water = new GameObject("Water", typeof(MeshFilter), typeof(MeshRenderer));
        water.GetComponent<MeshFilter>().sharedMesh = WaterGrid(90f, 1f);
        water.GetComponent<MeshRenderer>().sharedMaterial = WaterMaterial();
        water.transform.position = waterCentre;
        return scene;
    }

    // What every scene has after its layout: the game's systems (world state, effects, HUD, sounds), the music, the
    // camera and the look; then it's saved (and listed in the build).
    static void FinishScene(UnityEngine.SceneManagement.Scene scene, string path, Transform start, Apprentice apprentice, string musicName)
    {
        var systems = new GameObject("Game");
        systems.AddComponent<WorldState>().startPoint = start;
        var effects = systems.AddComponent<Effects>();
        effects.sparkMaterial = sparkMaterial;
        effects.flashMaterial = ParticleMaterial("M_BlockFlash", RadialTexture("T_Ring", ring: true));
        effects.flameMaterial = ParticleMaterial("M_Flame", FlameTexture("T_Flame"));
        effects.strawMaterial = LitParticleMaterial("M_Straw", RadialTexture("T_SoftDot", ring: false), 0.9f);
        var hud = systems.AddComponent<Hud>();
        hud.apprentice = apprentice;
        hud.titleFont = AssetDatabase.LoadAssetAtPath<Font>(Fonts + "IMFellEnglishSC-Regular.ttf");
        hud.textFont = AssetDatabase.LoadAssetAtPath<Font>(Fonts + "IMFellEnglish-Regular.ttf");
        systems.AddComponent<Cutaway>(); // architecture between the camera and the Apprentice opens up
        systems.AddComponent<NavMeshBaker>(); // the ways the Soured can walk, built as the scene loads
        systems.AddComponent<SaveSystem>(); // keeps the slot being played (from the title screen) in step with the world
        var pause = systems.AddComponent<PauseMenu>();
        pause.titleFont = hud.titleFont;
        pause.textFont = hud.textFont;
        var character = systems.AddComponent<CharacterScreen>(); // I / C / Select: the Apprentice, the oar, Charms, the lantern
        character.titleFont = hud.titleFont;
        character.textFont = hud.textFont;
        character.oarModels = new GameObject[Oar.Steps.Length]; // (turning on its Oar page; no colliders: they're not in the world)
        for (int step = 0; step < Oar.Steps.Length; step++)
        {
            var oar = Place($"Environment/oar_display_{step}", new Vector3(0f, -400f, 0f), 0f);
            oar.name = $"Oar {step} (character screen)";
            foreach (var c in oar.GetComponentsInChildren<Collider>())
                UnityEngine.Object.DestroyImmediate(c);
            oar.SetActive(false);
            character.oarModels[step] = oar;
        }
        character.provisionModels = new GameObject[Provision.All.Length]; // (turning on its Satchel page)
        for (int i = 0; i < Provision.All.Length; i++)
        {
            var model = Place($"Environment/provision_{Provision.All[i].Id}", new Vector3(0f, -400f, 0f), 0f);
            model.name = $"{Provision.All[i].Name} (character screen)";
            foreach (var c in model.GetComponentsInChildren<Collider>())
                UnityEngine.Object.DestroyImmediate(c);
            model.SetActive(false);
            character.provisionModels[i] = model;
        }
        character.ledgerModels = new GameObject[LedgerEntry.All.Length]; // (turning on its Ledger page)
        for (int i = 0; i < LedgerEntry.All.Length; i++)
        {
            var model = Place($"Characters/{LedgerEntry.All[i].Model}", new Vector3(0f, -400f, 0f), 0f);
            model.name = $"{LedgerEntry.All[i].Name} (character screen)";
            model.AddComponent<StagePose>(); // (standing as it idles)
            if (LedgerEntry.All[i] == LedgerEntry.Unparted)
            {
                // The Unparted is two: Fast rides Hold's back, clinging, as in the fight.
                var back = Bone(model, "Back");
                var fast = Place("Characters/clinger", back.position, 0f);
                fast.name = "Fast";
                fast.transform.SetParent(back, true);
                var pose = fast.AddComponent<StagePose>();
                pose.clip = "Cling";
                pose.at = 1f;
            }
            foreach (var c in model.GetComponentsInChildren<Collider>())
                UnityEngine.Object.DestroyImmediate(c);
            model.SetActive(false);
            character.ledgerModels[i] = model;
        }
        var sfx = systems.AddComponent<Sfx>();
        sfx.oarSwing = Clips("oar_swing");
        sfx.oarSwingHeavy = Clips("oar_swing_heavy");
        sfx.dash = Clips("dash");
        sfx.blockClang = Clips("block_clang");
        sfx.parry = Clips("parry");
        sfx.soulAbsorb = Clips("soul_absorb");
        sfx.deathToll = Clips("death_toll");
        sfx.lanternLit = Clips("lantern_lit");
        sfx.oarHit = Clips("oar_hit");
        sfx.hurt = Clips("apprentice_hurt");
        sfx.obolClink = Clips("obol_clink");
        sfx.charmFit = Clips("charm_fit");
        sfx.uiTick = Clips("ui_tick");
        sfx.heal = Clips("heal");
        sfx.splash = Clips("splash");
        sfx.arrowThunk = Clips("arrow_thunk");
        sfx.gateOpen = Clips("gate_open");
        sfx.gateRattle = Clips("gate_rattle");
        sfx.hookThrow = Clips("hook_throw");
        sfx.hookCatch = Clips("hook_catch");
        sfx.hookWhirl = Clips("hook_whirl");
        sfx.itemTake = Clips("item_take");
        sfx.keyItem = Clips("key_item");
        sfx.lanternSnuff = Clips("lantern_snuff");
        sfx.murmur = Clips("lingerer_murmur");
        sfx.oarStroke = Clips("oar_stroke");
        sfx.bossFarewell = Clips("boss_farewell");
        sfx.pitchBurst = Clips("pitch_burst");
        sfx.drink = Clips("drink");
        sfx.tarSizzle = Clips("tar_sizzle");
        sfx.wayBack = Clips("way_back");
        sfx.spitSplat = Clips("spit_splat");
        var music = new GameObject("Music", typeof(AudioSource));
        music.GetComponent<AudioSource>().clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Audio + "Music/" + musicName + ".wav");
        music.AddComponent<MusicPlayer>();

        var cam = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>(); // CameraRig adds the listener
        cam.tag = "MainCamera";
        cam.fieldOfView = 30f;
        cam.farClipPlane = 200f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = night.fog;
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        var rig = cam.gameObject.AddComponent<CameraRig>();
        rig.target = apprentice.transform;
        cam.transform.SetPositionAndRotation(
            apprentice.transform.position + Quaternion.Euler(rig.pitch, 0f, 0f) * new Vector3(0f, 0f, -rig.distance),
            Quaternion.Euler(rig.pitch, 0f, 0f));

        var volume = new GameObject("Global Volume").AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = VolumeProfileAsset();

        EditorSceneManager.SaveScene(scene, path);
        AssetDatabase.SaveAssets();
        // Listed in the build (and any scene since removed or renamed dropped from the list).
        var listed = EditorBuildSettings.scenes.Where(s => File.Exists(s.path)).ToList();
        if (!listed.Exists(s => s.path == path))
            listed.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = listed.ToArray();
        Debug.Log($"[MainSceneBuilder] Built {path}");
    }

    // ------------------------------------------------------------ pieces

    static GameObject Place(string model, Vector3 position, float yaw)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Models + model + ".fbx");
        if (prefab == null)
            throw new Exception($"Missing model {model}; run tools/build-models.sh");
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        // Keep the model's own root rotation (rigged FBX roots carry the axis conversion); add yaw on top.
        go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f) * prefab.transform.localRotation);
        return go;
    }

    static Apprentice Apprentice(Vector3 position)
    {
        var go = Place("Characters/apprentice", position, 0f);
        go.name = "Apprentice";
        var controller = go.AddComponent<CharacterController>();
        controller.center = new Vector3(0f, 0.56f, 0f);
        controller.height = 1.1f;
        controller.radius = 0.32f;
        controller.stepOffset = 0.3f;
        controller.minMoveDistance = 0f;
        go.AddComponent<Equipment>();
        var apprentice = go.AddComponent<Apprentice>();
        apprentice.catchSound = Clips("apprentice_catch");
        apprentice.burningSound = Clips("apprentice_burning");
        apprentice.sackSound = Clips("sodden_sack");
        apprentice.strokeDrawSound = Clips("stroke_draw");
        apprentice.strokeSound = Clips("stroke_wave");
        apprentice.strokeChargedSound = Clips("stroke_charged");
        apprentice.douseSound = Clips("steam_hiss");
        apprentice.boatSurgeSound = Clips("boat_surge");
        go.AddComponent<DashGhosts>().ghostMaterial = GeneratedMaterial("M_DashGhost", "MyGame/Ghost");
        go.AddComponent<HealGlow>();
        go.AddComponent<OarLook>(); // the oar's step: its trims and its wood
        Swoosh(go, "Oar", "Oar0Blade", ApprenticeSwoosh, 0.16f, towardsPivot: 0.55f); // from the tip to half-way down the shaft: a wide crescent
        apprentice.hookHeadModel = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "Environment/hook_head.fbx");
        apprentice.pitchPotModel = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "Environment/provision_pitch_pot.fbx");
        var lantern = PointLight(Bone(go, "Body"), GlowCentre(go, "SoulLantern_Glow"), 3.5f, 2.2f, BowLanternLight);
        Flicker(lantern, 0.06f, 2f, 0.01f); // a slow, steady pulse, not a flame
        go.AddComponent<BowLantern>().lanternLight = lantern;
        return apprentice;
    }

    // ---- The loop beyond the quay

    const float Segment = 4.76f; // a walk piece is 4.42 m of planks; + one gap keeps the spacing

    // A run of boardwalk from `start` along `direction` (pieces extend away from their origin, towards -Z at yaw 0).
    static void Boardwalk(Vector3 start, Vector3 direction, int pieces)
    {
        float yaw = Mathf.Atan2(-direction.x, -direction.z) * Mathf.Rad2Deg;
        for (int k = 0; k < pieces; k++)
            Place("Environment/jetty_walk", start + direction * (Segment * k), yaw);
    }

    // West of the quay: the net yard, a second quay with cover (walls, crates) to break an archer's line of sight.
    static void Yard()
    {
        Boardwalk(new Vector3(-7.3f, QuayTop - 0.03f, -2.0f), Vector3.left, 2);
        StreetLantern(new Vector3(-11.9f, QuayTop, -1.2f), 0f, 2.2f);
        Place("Environment/quay", new Vector3(-24.6f, 0f, 0f), 0f);
        Place("Environment/ruined_wall_long", new Vector3(-24.5f, QuayTop, 3.4f), 96f);
        Place("Environment/ruined_wall_short", new Vector3(-22.3f, QuayTop, -2.9f), 80f);
        Place("Environment/ruined_wall_short", new Vector3(-28.4f, QuayTop, -3.3f), 118f);
        Place("Environment/crate", new Vector3(-20.8f, QuayTop, 2.6f), 170f);
        Place("Environment/crate", new Vector3(-26.8f, QuayTop, 3.8f), 200f);
        Place("Environment/barrel", new Vector3(-26.3f, QuayTop, -3.6f), 180f);
        Place("Environment/barrel", new Vector3(-20.4f, QuayTop, -3.9f), 210f);
        foreach (var p in new[] { new Vector3(-17.6f, 0f, -4.2f), new Vector3(-31.8f, 0f, 3.6f), new Vector3(-31.6f, 0f, -3.2f), new Vector3(-17.8f, 0f, 4.3f) })
            Torch(new Vector3(p.x, QuayTop, p.z));
        Place("Environment/house_b", new Vector3(-24.0f, 0f, 8.6f), 186f);
        Place("Environment/house_a", new Vector3(-35.0f, 0f, 1.5f), 96f);
        Place("Environment/rock_b", new Vector3(-18.5f, -0.1f, 7.5f), 60f);
        Place("Environment/rock_a", new Vector3(-32.5f, -0.12f, -7.5f), 20f);
    }

    // A hook post: the pile (with its ring for the tether) and the spot on its platform where the pull sets you down.
    // It faces `across`, the far side of the gap it crosses (its ring face-on to it, its snapped rope trailing into the
    // water between).
    static HookPost HookPostAt(Vector3 position, Vector3 landing, Vector3 across)
    {
        Vector3 to = across - position;
        var go = Place("Environment/hook_post", position, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg);
        go.name = "Hook Post";
        var post = go.AddComponent<HookPost>();
        post.ring = new GameObject("Ring").transform;
        post.ring.SetParent(go.transform, false);
        post.ring.position = position + Vector3.up * 2.42f;
        post.landing = new GameObject("Landing").transform;
        post.landing.SetParent(go.transform, false);
        post.landing.position = landing;
        return post;
    }

    // Off the yard's north edge, across the water: a small deck with a Healing charge piece, glowing where you can
    // see it but can't reach it. The Hook's first gate: a hook post on each side.
    static void HookIslet()
    {
        // Each post stands beside its landing spot, never between the two (the pull would run into it).
        HookPostAt(new Vector3(-30.1f, QuayTop, 4.6f), new Vector3(-29.0f, QuayTop, 3.4f), new Vector3(-30.2f, QuayTop, 8.5f));
        // Close enough to be seen from the yard's edge (the camera shows ~6 m ahead), too far to reach without the Hook.
        Place("Environment/jetty_landing", new Vector3(-29.0f, QuayTop - 0.03f, 11.2f), 0f);
        HookPostAt(new Vector3(-30.2f, QuayTop, 8.5f), new Vector3(-29.0f, QuayTop, 9.5f), new Vector3(-30.1f, QuayTop, 4.6f));
        var ember = Place("Environment/charge_ember", new Vector3(-28.4f, QuayTop, 10.5f), 0f);
        ember.name = "Healing Charge Piece";
        ember.AddComponent<ChargePiece>();
        var glow = ember.AddComponent<PickupGlow>(); // an item: a soft warm glow (its ember already burns)
        glow.tier = PickupGlow.Tier.Item;
        glow.colour = LanternLight;
        glow.height = 1.0f;
        // Beside it, a little lantern still burning: Hesper's son's (she saw its light from the landing).
        var lantern = Place("Environment/boat_lantern", new Vector3(-30.1f, QuayTop, 10.7f), 200f);
        lantern.name = "Boat-Lantern (Keepsake)";
        var keepsake = lantern.AddComponent<Keepsake>();
        keepsake.id = HesperKeepsake;
        keepsake.title = "A Boat-Lantern";
        keepsake.hint = "Its glass is painted, crookedly. It's still burning. Old Hesper will know it.";
        keepsake.interactRadius = 1.2f;
        var lanternGlow = lantern.AddComponent<PickupGlow>();
        lanternGlow.tier = PickupGlow.Tier.Item;
        lanternGlow.colour = LanternLight;
        lanternGlow.height = 0.6f;
    }

    // Seen on the way in, long before the Hook: beside the half-sunken house west of the boardwalk, a little deck with
    // an Obol cache on it, and a hook post on each side of the water between. Walked past at the start; reached once
    // you have the Hook (the shortcut brings you back to it).
    static void CacheDeck()
    {
        Vector3 walkPost = new Vector3(1.55f, QuayTop, -17.3f), deckPost = new Vector3(-2.6f, QuayTop, -17.9f);
        HookPostAt(walkPost, new Vector3(2.4f, QuayTop, -18.2f), deckPost);
        Place("Environment/jetty_landing", new Vector3(-4.0f, QuayTop - 0.03f, -17.6f), 0f);
        HookPostAt(deckPost, new Vector3(-3.4f, QuayTop, -19.0f), walkPost);
        var cache = Place("Environment/obol_cache", new Vector3(-4.3f, QuayTop, -19.7f), 30f);
        cache.name = "Obol Cache";
        cache.AddComponent<ObolCache>();
        var glow = cache.AddComponent<PickupGlow>(); // an item: a soft warm glow
        glow.tier = PickupGlow.Tier.Item;
        glow.colour = LanternLight;
        glow.height = 0.7f;
    }

    // South of the yard: the crossroads, where a Laden and an archer hold it together.
    static void Crossroads()
    {
        Boardwalk(new Vector3(-25.5f, QuayTop - 0.03f, -5.1f), Vector3.back, 3);
        StreetLantern(new Vector3(-24.7f, QuayTop, -12.0f), -90f, 2.2f);
        Place("Environment/quay", new Vector3(-22.6f, 0f, -24.4f), 180f);
        Place("Environment/ruined_wall_long", new Vector3(-20.2f, QuayTop, -24.6f), 32f);
        Place("Environment/ruined_wall_short", new Vector3(-27.4f, QuayTop, -26.6f), 160f);
        Place("Environment/crate", new Vector3(-29.0f, QuayTop, -21.6f), 150f);
        Place("Environment/barrel", new Vector3(-16.9f, QuayTop, -20.8f), 190f);
        Place("Environment/barrel", new Vector3(-17.5f, QuayTop, -21.4f), 230f);
        foreach (var p in new[] { new Vector3(-29.8f, 0f, -20.2f), new Vector3(-15.4f, 0f, -21.0f), new Vector3(-29.6f, 0f, -28.6f), new Vector3(-15.8f, 0f, -28.8f) })
            Torch(new Vector3(p.x, QuayTop, p.z));
        Place("Environment/house_a", new Vector3(-33.0f, 0f, -26.5f), 100f);
        Place("Environment/house_b", new Vector3(-10.5f, 0f, -37.5f), 330f); // (clear of the way south, to the chapel)
        Place("Environment/rock_c", new Vector3(-12.5f, -0.15f, -31.0f), 80f);
    }

    // South from the crossroads: a boardwalk to a landing with the Area's second Mooring post (always one before a
    // Boss), then west along a short boardwalk to the drowned chapel's way in (the chapel turned so its altar is to the
    // west: its tall pillars stand at the far side, away from the camera).
    static Anchoress ToTheChapel()
    {
        Vector3 from = new Vector3(-20.2f, QuayTop - 0.03f, -29.4f); // the crossroads quay's south edge
        Boardwalk(from, Vector3.back, 2);
        float landing = from.z - Segment * 2;
        Place("Environment/jetty_landing", new Vector3(from.x, QuayTop - 0.03f, landing), 0f);
        MooringPostAt(new Vector3(from.x + 1.25f, QuayTop, landing - 2.66f), 180f);
        StreetLantern(new Vector3(from.x + 0.75f, QuayTop, from.z - 4.0f), -90f, 2.2f);
        float z = landing - 1.7f; // the landing's middle
        Boardwalk(new Vector3(from.x - 1.7f, QuayTop - 0.03f, z), Vector3.left, 1);
        var centre = new Vector3(from.x - 1.7f - Segment - 7.8f + 0.35f, 0f, z);
        var entrance = new GameObject("Area Entrance (Chapel)").AddComponent<AreaEntrance>();
        entrance.transform.SetPositionAndRotation(new Vector3(from.x - 3.5f, QuayTop + 1f, z), Quaternion.Euler(0f, 90f, 0f)); // across this boardwalk
        entrance.areaName = "The Drowned Chapel";
        return DrownedChapel(centre, -90f);
    }

    // East from the crossroads, back to the landing: the shortcut. It ends at a barred gate beside the Mooring post,
    // opened only from this (the crossroads) side.
    static void Loopback()
    {
        Boardwalk(new Vector3(-14.8f, QuayTop - 0.03f, -25.6f), Vector3.right, 3);
        StreetLantern(new Vector3(-7.8f, QuayTop, -24.8f), 180f, 2.2f);
        var go = Place("Environment/shortcut_gate", new Vector3(-0.1f, QuayTop - 0.03f, -25.6f), 90f);
        go.name = "Shortcut Gate";
        go.AddComponent<ShortcutGate>().openFrom = Vector3.left;
    }

    static Laden Laden(string name, Vector3 position, float yaw)
    {
        var go = Place("Characters/laden", position, yaw);
        go.name = name;
        var controller = go.AddComponent<CharacterController>();
        controller.center = new Vector3(0f, 0.9f, 0f);
        controller.height = 1.8f;
        controller.radius = 0.6f;
        controller.minMoveDistance = 0f;
        var laden = go.AddComponent<Laden>();
        laden.maxHealth = 160f;
        laden.radius = 0.65f;
        laden.knockbackResistance = 0.7f;
        laden.staggerCapacity = 150f; // huge: it takes a committed flurry (or heavies) to stagger
        laden.staggerDrainDelay = 1.5f;
        laden.staggerDrainPerSecond = 25f;
        laden.staggerDuration = 1.2f;
        laden.yankStagger = 1.3f; // yanked over on the Hook, heap and all: it takes a moment to find its feet
        laden.aggroRadius = 5.5f; // lets the Apprentice reach the quay before anything notices
        laden.attackRange = 2.0f;
        laden.chaseSpeed = 1.9f;
        laden.primary = new SouredAttack
        {
            name = "Slam", windupClip = "Windup", strikeClip = "Strike", recoverClip = "Recover",
            telegraph = 0.9f, strike = 0.3f, recover = 1.2f, impactAt = 0.5f,
            lunge = 0.5f, maxLunge = 2.2f, trackUntil = 0.9f, damage = 30f, reach = 2.2f, arc = 60f, knockback = 7f,
        };
        laden.barHeight = 2.3f; // just above the raised heap
        // Each attack sounds its own tell, so the slam and the sweep can be told apart by ear.
        laden.primary.windupSound = Clips("laden_slam_windup");
        laden.primary.swingSound = Clips("laden_swing");
        laden.primary.impactSound = Clips("laden_slam_impact");
        laden.sweep.windupSound = Clips("laden_sweep_windup");
        laden.sweep.swingSound = Clips("laden_swing");
        laden.sweep.swingPitch = 1.15f; // a quicker, lighter swing than the slam
        BeltLantern(go, laden);
        Swoosh(go, "Heap", "FrameGold", SouredSwoosh, 0.18f, "HeapWood"); // the whole heap, top to chest: a big heavy smear
        return laden;
    }

    // The Drowned: waits under the water, rises to spit poison at you (a perfect block turns it back: it's slain by its
    // own spit); hide from it and it comes to the shore, open to the oar; hooked out, it's stranded on the ground.
    // `position`: where it waits (at the water's surface; it sinks out of sight).
    static Drowned Drowned(string name, Vector3 position, float yaw)
    {
        var go = Place("Characters/drowned", position, yaw);
        go.name = name;
        var controller = go.AddComponent<CharacterController>();
        controller.center = new Vector3(0f, 0.55f, 0f);
        controller.height = 1.1f;
        controller.radius = 0.35f;
        controller.minMoveDistance = 0f;
        var drowned = go.AddComponent<Drowned>();
        drowned.maxHealth = 70f;
        drowned.radius = 0.45f;
        drowned.staggerCapacity = 40f;
        drowned.staggerDuration = 1.0f;
        drowned.aggroRadius = 9f;         // (it feels you come near its water)
        drowned.viewHalfAngle = 180f;     // (under the water, it senses all round)
        drowned.noticeCloseRadius = 9f;
        drowned.leashRadius = 9f;         // its stretch of water
        drowned.giveUpRange = 13f;
        drowned.chaseSpeed = 3f;
        drowned.turnSpeed = 540f;
        drowned.attackRange = 2.0f;
        drowned.obols = 35;
        drowned.eyeHeight = 0.9f;
        drowned.barHeight = 1.5f;
        drowned.primary = new SouredAttack
        {
            name = "Spit", windupClip = "Rise", strikeClip = "Spit", recoverClip = "Clutch",
            telegraph = 0.85f, strike = 0.3f, recover = 0.9f, impactAt = 0.35f,
            lunge = 0f, maxLunge = 0f, trackUntil = 0.75f, damage = 6f, reach = 9f, arc = 180f, knockback = 2f,
            ranged = true, projectileSpeed = 9f,
        };
        drowned.primary.windupSound = Clips("drowned_rise");
        drowned.primary.releaseSound = Clips("drowned_spit");
        drowned.bubbleSound = Clips("drowned_bubbles");
        return drowned;
    }

    // A Moth: one-hit, on the wing, in a flock (the flock decides where it flies and when it dives).
    static Moth Moth(string name, Vector3 position, float yaw)
    {
        var go = Place("Characters/moth", position, yaw);
        go.name = name;
        var controller = go.AddComponent<CharacterController>();
        controller.center = Vector3.zero;
        controller.height = 0.36f;
        controller.radius = 0.18f;
        controller.minMoveDistance = 0f;
        var moth = go.AddComponent<Moth>();
        moth.maxHealth = 1f;          // any blow
        moth.radius = 0.32f;
        moth.staggerCapacity = 999f;
        moth.aggroRadius = 7f;
        moth.viewHalfAngle = 180f;    // (it comes to light, from any side)
        moth.noticeCloseRadius = 7f;
        moth.leashRadius = 12f;
        moth.giveUpRange = 12f;
        moth.chaseSpeed = 4.5f;
        moth.turnSpeed = 720f;
        moth.obols = 8;
        moth.eyeHeight = 0f;
        moth.barHeight = 0.45f;
        moth.fireWeakness = 3f;       // paper
        moth.deathDuration = 0.45f;
        moth.dissolveDuration = 0.55f;
        moth.fareMotes = 10;
        moth.primary = new SouredAttack
        {
            name = "Dive", windupClip = "Stall", strikeClip = "Dive", recoverClip = "Fly",
            telegraph = 0.5f, strike = 0.7f, recover = 0.5f, impactAt = 0.1f,
            lunge = 0f, maxLunge = 0f, trackUntil = 0.85f, damage = 6f, reach = 8f, arc = 180f, knockback = 2f, scripted = true,
        };
        moth.diveSound = Clips("moth_dive");
        moth.releaseSound = Clips("moth_release");
        return moth;
    }

    // A Gleaner: keeps its distance and skips away from the oar; flings fire from its pole's lantern (turned back, its
    // own fire burns it up).
    static Gleaner Gleaner(string name, Vector3 position, float yaw)
    {
        var go = Place("Characters/gleaner", position, yaw);
        go.name = name;
        var controller = go.AddComponent<CharacterController>();
        controller.center = new Vector3(0f, 0.85f, 0f);
        controller.height = 1.7f;
        controller.radius = 0.35f;
        controller.minMoveDistance = 0f;
        var g = go.AddComponent<Gleaner>();
        g.maxHealth = 50f;
        g.radius = 0.4f;
        g.staggerCapacity = 40f;
        g.staggerDuration = 0.9f;
        g.aggroRadius = 12f;
        g.viewHalfAngle = 110f;
        g.noticeCloseRadius = 4f;
        g.chaseSpeed = 2.4f;
        g.turnSpeed = 540f;
        g.leashRadius = 16f;
        g.giveUpRange = 18f;
        g.obols = 30;
        g.eyeHeight = 1.5f;
        g.barHeight = 2.2f;
        g.primary = new SouredAttack
        {
            name = "Fling", windupClip = "Windup", strikeClip = "Fling", recoverClip = "Recover",
            telegraph = 0.9f, strike = 0.35f, recover = 0.8f, impactAt = 0.45f,
            lunge = 0f, maxLunge = 0f, trackUntil = 0.8f, damage = 16f, reach = 13f, arc = 180f, knockback = 3f,
            ranged = true, projectileSpeed = 7f,
            windupSound = Clips("gleaner_windup"), releaseSound = Clips("fireball_whoosh"),
        };
        g.fireMaterial = FireballMaterial();
        g.lantern = PointLight(Bone(go, "Pole"), GlowCentre(go, "GleanerLantern_Glow"), 6.5f, g.lanternIntensity, LanternLight); // (a pool of light round it: seen in the dark rows)
        g.hopSound = Clips("gleaner_hop");
        g.gutterSound = Clips("fireball_gutter");
        BeltLantern(go, g);
        return g;
    }

    // A Thresher: an iron threshing machine, its driver grown into the seat. Charges in straight lines; crashes into what's
    // solid; the oar glances off its front; fire stokes it; the Hook wrenches its chain off.
    static Thresher Thresher(string name, Vector3 position, float yaw)
    {
        var go = Place("Characters/thresher", position, yaw);
        go.name = name;
        var controller = go.AddComponent<CharacterController>();
        controller.center = new Vector3(0f, 1.0f, 0f);
        controller.height = 2.0f;
        controller.radius = 0.8f;   // (round: it slides off a trunk or a post rather than catching on it)
        controller.stepOffset = 0.3f;
        controller.minMoveDistance = 0f;
        var t = go.AddComponent<Thresher>();
        t.maxHealth = 240f;
        t.radius = 0.9f;
        t.knockbackResistance = 1f;
        t.staggerCapacity = 9999f; // (iron: it doesn't reel)
        t.aggroRadius = 13f;
        t.viewHalfAngle = 120f;
        t.noticeCloseRadius = 3f;
        t.chaseSpeed = 1.6f;
        t.turnSpeed = 70f;         // (it barely turns)
        t.leashRadius = 22f;
        t.giveUpRange = 20f;
        t.obols = 120;
        t.eyeHeight = 1.8f;
        t.barHeight = 2.9f;
        t.deathDuration = 1.2f;
        t.fireWeakness = 0f;
        t.primary = new SouredAttack
        {
            name = "Sweep", windupClip = "SweepWindup", strikeClip = "Sweep", recoverClip = "Recover",
            telegraph = 0.7f, strike = 0.3f, recover = 0.75f, impactAt = 0.4f,
            lunge = 0.3f, maxLunge = 0.6f, trackUntil = 0.8f, damage = 20f, reach = 3.0f, arc = 120f, knockback = 9f,
        };
        t.primary.windupSound = Clips("thresher_rev");
        t.primary.swingSound = Clips("laden_swing");
        t.primary.swingPitch = 0.8f;
        t.charge = new SouredAttack
        {
            name = "Charge", windupClip = "Rev", strikeClip = "Charge", recoverClip = "Idle",
            telegraph = 1.1f, strike = 2.2f, recover = 0.8f, impactAt = 0f,
            lunge = 0f, maxLunge = 0f, trackUntil = 0.8f, damage = 30f, reach = 15f, arc = 180f, knockback = 12f, scripted = true,
            windupSound = Clips("thresher_rev"), swingSound = Clips("thresher_charge"),
        };
        t.vent = new SouredAttack
        {
            name = "Vent", windupClip = "Idle", strikeClip = "Vent", recoverClip = "Idle",
            telegraph = 0.55f, strike = 0.5f, recover = 0.6f, impactAt = 0f,
            lunge = 0f, maxLunge = 0f, trackUntil = 0f, damage = 10f, reach = 2.4f, arc = 360f, knockback = 7f, scripted = true,
            windupSound = Clips("thresher_hiss"),
        };
        t.reel = Bone(go, "Reel");
        var stack = new GameObject("Stack Top").transform;
        stack.SetParent(Bone(go, "Body"), false);
        stack.position = go.transform.position + Vector3.up * 2.55f + go.transform.forward * 0.18f;
        t.stack = stack;
        t.crashSound = Clips("thresher_crash");
        t.chainSound = Clips("thresher_chain");
        t.stokeSound = Clips("thresher_stoke");
        t.ventSound = Clips("thresher_vent");
        BeltLantern(go, t, "Body");
        return t;
    }

    // A ball of fire (the Watchman's, a Gleaner's): glowing orange, no outline.
    static Material FireballMaterial()
    {
        var m = GeneratedMaterial("M_Fireball", "MyGame/Toon");
        m.SetColor("_BaseColor", new Color(1f, 0.6f, 0.25f));
        m.SetColor("_EmissionColor", new Color(1.6f, 0.7f, 0.2f));
        m.SetFloat("_OutlineWidth", 0f);
        EditorUtility.SetDirty(m);
        return m;
    }

    // A flock of Moths round a light (`lamp`: none, round where it's set down).
    static MothFlock MothFlockAt(string name, Vector3 at, Transform lamp, int count)
    {
        var root = new GameObject(name);
        root.transform.position = lamp != null ? lamp.position : at;
        var flock = root.AddComponent<MothFlock>();
        flock.lamp = lamp;
        for (int i = 0; i < count; i++)
        {
            float a = i * Mathf.PI * 2f / count;
            var moth = Moth($"Moth {i + 1}", root.transform.position + new Vector3(Mathf.Cos(a) * flock.ringRadius, flock.ringHeight, Mathf.Sin(a) * flock.ringRadius), i * 47f);
            moth.transform.SetParent(root.transform, true);
        }
        return flock;
    }

    // The Clinger: fast, fragile, always two. Circles, leaps, clings to your back; dash it off.
    static Clinger Clinger(string name, Vector3 position, float yaw)
    {
        var go = Place("Characters/clinger", position, yaw);
        go.name = name;
        var controller = go.AddComponent<CharacterController>();
        controller.center = new Vector3(0f, 0.5f, 0f);
        controller.height = 1.0f;
        controller.radius = 0.35f;
        controller.minMoveDistance = 0f;
        var clinger = go.AddComponent<Clinger>();
        clinger.maxHealth = 40f;   // three light blows
        clinger.radius = 0.4f;
        clinger.knockbackResistance = 0f;
        clinger.staggerCapacity = 30f;
        clinger.staggerDuration = 0.9f;
        clinger.aggroRadius = 8f;
        clinger.noticeCloseRadius = 1.8f;
        clinger.chaseSpeed = 4.2f;
        clinger.turnSpeed = 720f;
        clinger.obols = 20;
        clinger.eyeHeight = 0.85f;
        clinger.barHeight = 1.35f;
        clinger.yankStagger = 0.8f;
        clinger.deathDuration = 0.8f;
        clinger.leap.windupSound = Clips("clinger_hiss");
        clinger.leap.swingSound = Clips("dash");
        clinger.leap.swingPitch = 1.35f;
        clinger.skitterSound = Clips("clinger_skitter");
        clinger.grabSound = Clips("clinger_grab");
        clinger.throwSound = Clips("clinger_throw");
        clinger.keenSound = Clips("clinger_keen");
        clinger.whisperSound = Clips("clinger_whisper");
        BeltLantern(go, clinger, "Spine");
        return clinger;
    }

    // The Unparted, of the Last Embrace (the Drowned City's miniboss): Hold, vast and slow, and Fast, a Clinger riding
    // its back. One fight (the Unparted: its name plate and two bars), two souls.
    static Unparted TheUnparted(string name, Vector3 position, float yaw)
    {
        var root = new GameObject(name);
        root.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        var go = Place("Characters/unparted", position, yaw);
        go.name = "Hold";
        go.transform.SetParent(root.transform, true);
        var controller = go.AddComponent<CharacterController>();
        controller.center = new Vector3(0f, 1.0f, 0f);
        controller.height = 2.0f;
        controller.radius = 0.75f;
        controller.minMoveDistance = 0f;
        var hold = go.AddComponent<UnpartedHold>();
        hold.maxHealth = 420f;
        hold.radius = 0.8f;
        hold.knockbackResistance = 0.85f;
        hold.staggerCapacity = 220f;
        hold.staggerDrainDelay = 1.5f;
        hold.staggerDrainPerSecond = 25f;
        hold.staggerDuration = 1.4f;
        hold.aggroRadius = 9f;
        hold.attackRange = 3.0f;
        hold.chaseSpeed = 1.8f;
        hold.turnSpeed = 240f;
        hold.obols = 250;
        hold.eyeHeight = 1.7f;
        hold.barHeight = 2.8f;
        hold.deathDuration = 1.1f;
        hold.primary = new SouredAttack
        {
            name = "Slam", windupClip = "SlamWindup", strikeClip = "Slam", recoverClip = "SlamRecover",
            telegraph = 1.1f, strike = 0.25f, recover = 1.2f, impactAt = 0.7f,
            lunge = 0.6f, maxLunge = 2.0f, trackUntil = 0.75f, damage = 32f, reach = 2.6f, arc = 40f, knockback = 10f,
        };
        hold.primary.windupSound = Clips("unparted_groan");
        hold.primary.swingSound = Clips("laden_swing");
        hold.primary.swingPitch = 0.75f;
        hold.primary.impactSound = Clips("laden_slam_impact");
        hold.sweep.windupSound = Clips("unparted_groan");
        hold.sweep.swingSound = Clips("laden_swing");
        hold.sweep.swingPitch = 0.85f;
        hold.howlSound = Clips("unparted_howl");
        BeltLantern(go, hold, "Spine");
        var fast = Clinger("Fast", position + Quaternion.Euler(0f, yaw, 0f) * Vector3.back * 0.3f, yaw);
        fast.transform.SetParent(root.transform, true);
        fast.mount = Bone(go, "Back");
        fast.other = hold;
        fast.maxHealth = 110f;
        fast.staggerCapacity = 45f;
        fast.obols = 80;
        hold.fast = fast;
        var unparted = root.AddComponent<Unparted>();
        unparted.hold = hold;
        unparted.fast = fast;
        unparted.music = new[]
        {
            AssetDatabase.LoadAssetAtPath<AudioClip>(Audio + "Music/music_unparted_1.wav"),
            AssetDatabase.LoadAssetAtPath<AudioClip>(Audio + "Music/music_unparted_2.wav"),
        };
        return unparted;
    }

    // The Laden archer: keeps its distance and shoots.
    static LadenArcher LadenArcher(string name, Vector3 position, float yaw)
    {
        var go = Place("Characters/laden_archer", position, yaw);
        go.name = name;
        var controller = go.AddComponent<CharacterController>();
        controller.center = new Vector3(0f, 0.85f, 0f);
        controller.height = 1.7f;
        controller.radius = 0.5f;
        controller.minMoveDistance = 0f;
        var archer = go.AddComponent<LadenArcher>();
        archer.maxHealth = 90f;   // no guard, less to it: the one to rush
        archer.radius = 0.55f;
        archer.knockbackResistance = 0.3f;
        archer.staggerCapacity = 60f;
        archer.staggerDuration = 1.0f;
        archer.aggroRadius = 9f;  // it sees you coming from further off than the Laden
        archer.attackRange = 1.7f; // the bow-jab, when cornered
        archer.chaseSpeed = 1.7f;
        archer.obols = 30;
        archer.barHeight = 2.0f;
        archer.primary = new SouredAttack
        {
            name = "Bash", windupClip = "BashWindup", strikeClip = "Bash", recoverClip = "BashRecover",
            telegraph = 0.45f, strike = 0.2f, recover = 0.6f, impactAt = 0.5f,
            lunge = 0.3f, maxLunge = 0.6f, trackUntil = 0.8f, damage = 12f, reach = 1.6f, arc = 70f, knockback = 9f,
        };
        archer.primary.windupSound = Clips("laden_sweep_windup");
        archer.primary.swingSound = Clips("laden_swing");
        archer.primary.swingPitch = 1.35f;
        archer.shot.windupSound = Clips("archer_draw");
        archer.shot.releaseSound = Clips("arrow_release"); // one per arrow
        archer.volley.windupSound = Clips("archer_volley_draw");
        archer.volley.releaseSound = Clips("arrow_release");
        archer.trailMaterial = sparkMaterial;
        BeltLantern(go, archer);
        Swoosh(go, "Bow", "BowWood", SouredSwoosh, 0.1f); // the bow-jab (inner edge: the wood nearest the shoulders, the grip)
        return archer;
    }

    // Swooshes are white with a cold tint for the Apprentice, sickly green for the Soured (green = danger).
    static readonly Color ApprenticeSwoosh = new Color(0.86f, 0.92f, 1f, 0.65f);
    static readonly Color SouredSwoosh = new Color(0.78f, 0.96f, 0.62f, 0.6f);

    // A swing's smear (SwingTrail), traced by two points on the weapon measured from the model itself (so no
    // guessing at bone axes): the tip is the `tipMaterial` vertex farthest from the bone; the inner edge is the
    // `innerMaterial` part's centre, or (with none) the tip part's vertex nearest the bone. Both ride the bone.
    static void Swoosh(GameObject go, string bone, string tipMaterial, Color colour, float lifetime, string innerMaterial = null, float towardsPivot = 0f)
    {
        var skinned = go.GetComponentInChildren<SkinnedMeshRenderer>();
        var baked = new Mesh();
        skinned.BakeMesh(baked, true);
        var materials = skinned.sharedMaterials;
        var boneTransform = Bone(go, bone);
        Vector3 pivot = boneTransform.position;
        Vector3[] Points(string material)
        {
            int index = Array.FindIndex(materials, m => m != null && m.name.StartsWith(material));
            if (index < 0)
                throw new Exception($"{go.name}: no {material} material for its swoosh");
            var verts = baked.vertices;
            var tris = baked.GetTriangles(index);
            var points = new Vector3[tris.Length];
            for (int i = 0; i < tris.Length; i++)
                points[i] = skinned.transform.TransformPoint(verts[tris[i]]);
            return points;
        }
        var tipPoints = Points(tipMaterial);
        Vector3 tipAt = tipPoints[0], nearAt = tipPoints[0];
        foreach (var p in tipPoints)
        {
            if (Vector3.Distance(p, pivot) > Vector3.Distance(tipAt, pivot))
                tipAt = p;
            if (Vector3.Distance(p, pivot) < Vector3.Distance(nearAt, pivot))
                nearAt = p;
        }
        Vector3 innerAt = Vector3.Lerp(nearAt, pivot, towardsPivot); // (the Apprentice's reaches down the shaft: wider)
        if (innerMaterial != null)
        {
            var innerPoints = Points(innerMaterial);
            innerAt = Vector3.zero;
            foreach (var p in innerPoints)
                innerAt += p / innerPoints.Length;
        }
        var tip = new GameObject("Swing Tip").transform;
        tip.SetParent(boneTransform, false);
        tip.position = tipAt;
        var inner = new GameObject("Swing Inner").transform;
        inner.SetParent(boneTransform, false);
        inner.position = innerAt;
        var trail = go.AddComponent<SwingTrail>();
        trail.tip = tip;
        trail.inner = inner;
        trail.material = GeneratedMaterial("M_Swoosh", "MyGame/Swoosh");
        trail.colour = colour;
        trail.lifetime = lifetime;
    }

    // Every Soured's belt lantern (sickly green light; the Anchoress hangs hers on the anchor) and the prop it drops where it falls, glowing there until the
    // Soured re-forms.
    static void BeltLantern(GameObject go, Soured soured, string bone = "Spine")
    {
        var lantern = PointLight(Bone(go, bone), GlowCentre(go, "SouredLantern_Glow"), 3.8f, 1.8f, SouredLight);
        Flicker(lantern, 0.15f, 5f, 0.02f);
        var dropped = new GameObject("Dropped Lantern (" + go.name + ")").AddComponent<DroppedLantern>();
        var body = Place("Environment/soured_lantern", Vector3.zero, 0f);
        body.name = "Body";
        body.transform.SetParent(dropped.transform, false);
        body.transform.localScale = Vector3.one * 1.3f; // a touch larger than on the belt, to read on the ground
        dropped.carried = lantern;
        dropped.body = body.transform;
        soured.droppedLantern = dropped;
    }

    const string HesperKeepsake = "hesper-boat-lantern";

    static Transform Child(Transform parent, string name, Vector3 local)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.localPosition = local;
        return t;
    }

    static Dock DockAt(string name, Vector3 mooring, float bowYaw, Vector3 ashore, bool farShore)
    {
        var dock = new GameObject($"Dock ({name})").AddComponent<Dock>();
        dock.displayName = name;
        dock.transform.position = ashore;
        dock.mooring = new GameObject("Mooring").transform;
        dock.mooring.SetParent(dock.transform, false);
        dock.mooring.SetPositionAndRotation(mooring, Quaternion.Euler(0f, bowYaw, 0f));
        dock.ashore = new GameObject("Ashore").transform;
        dock.ashore.SetParent(dock.transform, false);
        dock.ashore.position = ashore;
        dock.farShore = farShore;
        return dock;
    }

    // The Ferryman's boat (Boat), moored at `at` with its bow along `bowYaw`, stepped into from `ashore`. The root
    // carries the course (its forward is the bow); the hull bobs under it. The hull has no colliders: the boat meets
    // the world by its own casts, and nobody stands on it.
    static Boat BoatAt(Vector3 at, float bowYaw, Vector3 ashore)
    {
        var root = new GameObject("Boat");
        root.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, bowYaw, 0f));
        var model = Place("Environment/boat", at, bowYaw + 180f); // (a static model: its visual front is its back)
        foreach (var c in model.GetComponentsInChildren<Collider>())
            UnityEngine.Object.DestroyImmediate(c);
        model.transform.SetParent(root.transform, true);
        var boat = root.AddComponent<Boat>();
        boat.model = model.transform;
        boat.seat = Child(root.transform, "Seat", new Vector3(0f, 0.21f, -0.45f));
        boat.passengerSeat = Child(root.transform, "Passenger Seat", new Vector3(0f, -0.16f, 0.45f));
        // The wake: a pale ribbon laid flat on the water behind the stern.
        boat.stern = Child(root.transform, "Stern", new Vector3(0f, 0.16f, -1.25f));
        boat.stern.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        var wake = boat.stern.gameObject.AddComponent<TrailRenderer>();
        wake.alignment = LineAlignment.TransformZ;
        wake.time = 1.8f;
        wake.minVertexDistance = 0.2f;
        wake.widthCurve = new AnimationCurve(new Keyframe(0f, 0.9f), new Keyframe(1f, 2.2f));
        var wakeColour = new Gradient();
        wakeColour.SetKeys(new[] { new GradientColorKey(new Color(0.75f, 0.85f, 1f), 0f), new GradientColorKey(new Color(0.6f, 0.72f, 0.9f), 1f) },
                           new[] { new GradientAlphaKey(0.28f, 0f), new GradientAlphaKey(0f, 1f) });
        wake.colorGradient = wakeColour;
        var wakeMaterial = GeneratedMaterial("M_Wake", "MyGame/Particle");
        wakeMaterial.SetFloat("_Intensity", 1f);
        wake.sharedMaterial = wakeMaterial;
        wake.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        wake.emitting = false;
        WaterMask(root.transform, model.transform);
        // The bow lantern, hung from the bow pole while rowing (the Apprentice's own light moves into it).
        var lantern = Place("Environment/bow_lantern", GlowCentre(model, "BowHook"), bowYaw);
        foreach (var c in lantern.GetComponentsInChildren<Collider>())
            UnityEngine.Object.DestroyImmediate(c);
        lantern.name = "Bow Lantern";
        lantern.transform.SetParent(model.transform, true);
        boat.bowLantern = lantern;
        boat.bowLanternGlass = new GameObject("Glass").transform;
        boat.bowLanternGlass.SetParent(lantern.transform, false);
        boat.bowLanternGlass.position = GlowCentre(lantern, "SoulLantern_Glow");
        boat.home = DockAt("the landing", at, bowYaw, ashore, false);
        boat.docks = new[] { boat.home };
        return boat;
    }

    // The boat's hull, as boat.py lofts it: sections along it (0 stern .. 1 bow), their half-widths at the rim, depth
    // and rim height. Used for the lid that keeps the water out of it.
    static readonly float[] HullT = { 0f, 0.15f, 0.35f, 0.55f, 0.75f, 0.9f, 1f };
    static readonly float[] HullHalfWidth = { 0.5f, 0.6f, 0.63f, 0.6f, 0.48f, 0.27f, 0.03f };
    static readonly float[] HullDepth = { 0.34f, 0.4f, 0.42f, 0.42f, 0.39f, 0.34f, 0.3f };
    static readonly float[] HullRim = { 0.42f, 0.4f, 0.39f, 0.4f, 0.43f, 0.5f, 0.6f };

    // An invisible lid across the inside of the hull just above the waves (MyGame/WaterMask): without it the water's
    // surface shows through the boat's floor, which sits below the waterline. It rides the hull's bob.
    static void WaterMask(Transform root, Transform hull)
    {
        const float height = 0.22f; // above the keel: the waves (up to ~8 cm) never reach it, the seats (0.28) are above
        var verts = new List<Vector3>();
        var tris = new List<int>();
        for (int i = 0; i < HullT.Length; i++)
        {
            // The hull's inner half-width at `height`: along its profile, keel (0, rim - depth) -> (0.82 w, rim - 0.6 depth)
            // -> (w, rim), less the planking's thickness and a little more.
            float w = HullHalfWidth[i], d = HullDepth[i], r = HullRim[i];
            float keel = r - d, knee = r - 0.6f * d;
            float x = height <= keel ? 0f
                : height <= knee ? Mathf.Lerp(0f, 0.82f * w, Mathf.InverseLerp(keel, knee, height))
                : Mathf.Lerp(0.82f * w, w, Mathf.InverseLerp(knee, r, height));
            x = Mathf.Max(0f, x - 0.06f);
            float z = -1.3f + HullT[i] * 2.7f;
            verts.Add(new Vector3(-x, height, z));
            verts.Add(new Vector3(x, height, z));
            if (i > 0)
            {
                int a = 2 * (i - 1);
                tris.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 });
            }
        }
        var mesh = new Mesh { name = "BoatWaterMask" };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        string path = $"{Generated}/BoatWaterMask.asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mesh, path);
        var lid = new GameObject("Water Mask", typeof(MeshFilter), typeof(MeshRenderer));
        lid.transform.SetParent(root, false); // (laid out in the root's frame: bow +Z, up +Y)
        lid.GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = lid.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = GeneratedMaterial("M_WaterMask", "MyGame/WaterMask");
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        lid.transform.SetParent(hull, true);
    }

    struct FarShoreParts
    {
        public Dock dock;
        public Light light;
        public GameObject leftBehind;
    }

    static readonly Color FarLightColour = Hex("#fff1d6");

    // Out in the dark south-east of the landing: the Far shore, where the dead go on. A small jetty, a tall post with
    // a pale warm light, and beyond it the way on, glowing faintly through the dark. A Lingerer's Crossing ends here.
    static FarShoreParts FarShore(Boat boat)
    {
        var jetty = new Vector3(14.0f, QuayTop - 0.03f, -43.5f); // its planks run south, to z -46.6
        Place("Environment/jetty_landing", jetty, 0f);
        Place("Environment/islet_big", new Vector3(16.5f, -0.8f, -53.5f), 30f);
        var post = Place("Environment/far_light", new Vector3(15.1f, QuayTop, -46.1f), 200f);
        var light = PointLight(post.transform, GlowCentre(post, "FarLight_Glow"), 9f, 3.4f, FarLightColour);
        Flicker(light, 0.05f, 1.2f, 0.005f); // steady: it isn't a flame
        var motes = new GameObject("Motes").AddComponent<Motes>();
        motes.transform.SetParent(post.transform, false);
        motes.transform.position = GlowCentre(post, "FarLight_Glow");
        var beyond = PointLight(post.transform, new Vector3(15.5f, 1.8f, -49.5f), 11f, 1.8f, FarLightColour); // the way on
        beyond.name = "The Way On";
        var way = new GameObject("Motes (the Way On)").AddComponent<Motes>();
        way.transform.SetParent(post.transform, false);
        way.transform.position = new Vector3(15.5f, 0.3f, -49.5f);
        way.radius = 3f;
        way.perSecond = 4f;
        var dock = DockAt("the Far shore", new Vector3(11.45f, -0.12f, -45.0f), 180f, new Vector3(12.95f, QuayTop, -45.0f), true);
        boat.docks = new[] { boat.home, dock };
        // What Hesper leaves there once she's crossed: his lantern, set down by the post, still burning.
        var left = Place("Environment/boat_lantern", new Vector3(14.55f, QuayTop, -45.75f), 160f);
        left.name = "Boat-Lantern (Left on the Far shore)";
        foreach (var c in left.GetComponentsInChildren<Collider>())
            UnityEngine.Object.DestroyImmediate(c);
        Flicker(PointLight(left.transform, GlowCentre(left, "BoatLantern_Glow"), 2.6f, 1.4f, LanternLight), 0.1f, 5f, 0.01f);
        return new FarShoreParts { dock = dock, light = light, leftBehind = left };
    }

    // Old Hesper, a Lingerer: an old soul on a crate at the landing's edge, keeping a lamp lit for her son, who went out
    // the night the water came up. Bring her his boat-lantern (on the islet north of the yard) and she can let him go;
    // pay her fare and she waits by the boat for the Crossing (`waitAt`, `waitYaw`). She never jokes.
    static void HesperAt(Vector3 position, float yaw, Vector3 waitAt, float waitYaw, Boat boat, FarShoreParts far)
    {
        var root = new GameObject("Old Hesper");
        root.transform.position = position;
        var go = Place("Environment/hesper", position, yaw);
        go.name = "Hesper (on the landing)";
        go.transform.SetParent(root.transform, true);
        go.AddComponent<SoulFade>();
        var hesper = root.AddComponent<Lingerer>();
        hesper.figure = go.transform;
        hesper.id = "hesper";
        hesper.displayName = "Old Hesper, who keeps a light";
        hesper.keepsake = HesperKeepsake;
        hesper.fare = 100;
        hesper.interactRadius = 1.0f; // short: the landing is crowded (the gate home comes in just north of her)
        hesper.payLabel = "Pay her fare";
        var wait = new GameObject("Hesper Waits Here").transform;
        wait.SetPositionAndRotation(waitAt, Quaternion.Euler(0f, waitYaw - yaw, 0f) * go.transform.rotation);
        hesper.waitAt = wait;
        hesper.kept = PointLight(go.transform, GlowCentre(go, "HesperLamp_Glow"), 3.0f, 1.3f, LanternLight);
        Flicker(hesper.kept, 0.14f, 4f, 0.01f);
        // His lantern, in her lap once it's brought to her (hidden until then).
        var held = Place("Environment/boat_lantern", GlowCentre(go, "HesperEyes_Glow") + Vector3.down * 0.45f, yaw);
        foreach (var c in held.GetComponentsInChildren<Collider>())
            UnityEngine.Object.DestroyImmediate(c);
        held.name = "Boat-Lantern (Held)";
        held.transform.SetParent(go.transform, true);
        Flicker(PointLight(held.transform, GlowCentre(held, "BoatLantern_Glow"), 2.6f, 1.4f, LanternLight), 0.1f, 5f, 0.01f);
        held.SetActive(false);
        hesper.heldKeepsake = held;

        // For her Crossing: her, sat in the boat's bow with his lantern in her lap (hidden until she comes aboard).
        var aboard = Place("Environment/hesper_boat", boat.passengerSeat.position, boat.transform.eulerAngles.y);
        aboard.name = "Hesper (in the boat)";
        foreach (var c in aboard.GetComponentsInChildren<Collider>())
            UnityEngine.Object.DestroyImmediate(c);
        var lap = Place("Environment/boat_lantern", GlowCentre(aboard, "HesperEyes_Glow") + Vector3.down * 0.45f, boat.transform.eulerAngles.y);
        foreach (var c in lap.GetComponentsInChildren<Collider>())
            UnityEngine.Object.DestroyImmediate(c);
        lap.transform.SetParent(aboard.transform, true);
        Flicker(PointLight(lap.transform, GlowCentre(lap, "BoatLantern_Glow"), 2.6f, 1.4f, LanternLight), 0.1f, 5f, 0.01f);
        aboard.transform.SetParent(boat.model, true); // (she rides the hull's bob)
        aboard.AddComponent<SoulFade>();
        hesper.boatFigure = aboard;
        hesper.farLight = far.light;
        hesper.leftBehind = far.leftBehind;
        hesper.giftCharm = Charm.HespersWick.Id;
        hesper.giftHint = "A Charm, from Old Hesper. Fit it at a Mooring post.";

        hesper.meeting = new[]
        {
            "Oh. You're not him.",
            "Forgive me. I keep a lamp here for my boy. He went out the night the water came up, and he hasn't found his way back.",
            "I saw his light, though. Out on the water, on the little deck north of the yard. Burning, all this time.",
            "I can't reach it. My old legs were never for water. Nor for all that rolling about you young ones do.",
            "But you... you carry the Ferryman's things, don't you?",
            "If you could bring it to me. I'd know him by it.",
        };
        hesper.waiting = new[] { "His light's still out there. On the little deck, north of the yard." };
        // Her own dry asides, one a visit. She's wry, never about his loss: the grief lines stay straight.
        hesper.waitingAsides = new[]
        {
            "You're back. In one piece, mostly. That's more than most who go past the quay.",
            "That post of yours. You sit at it, and every one of those poor things out there gets back up. Did you notice? I'd find another post.",
            "I watched you fall in the water, earlier. And then you were back on the landing, dripping. You do that a lot, don't you, child?",
            "You needn't keep coming to tell me you haven't found it. But it's nice. Nobody visits.",
        };
        hesper.waitingWithoutHook = new[] { "There's no bridge. There never was. The Ferryman used to cross with a hook, you know. Not by jumping. Please don't jump." };
        hesper.given = new[]
        {
            "That's it. That's his.",
            "He painted the glass himself. Crooked, see? He never could hold a brush still.",
            "It's still warm.",
            "Then he didn't go astray. He went across. He must have. The light's still burning.",
            "I can let him go now. I can go too.",
        };
        hesper.fareAsked = new[] { "They say the crossing costs a coin. I have none. I spent them all on oil." };
        hesper.fareAgain = new[] { "The crossing costs a coin, they say. I've nothing left but oil. And you're not having the oil." };
        hesper.declined = new[] { "Take your time. I've waited this long. I'm very good at it." };
        hesper.paid = new[]
        {
            "Thank you, child.",
            "A hundred Obols. You carried all that about? Past those things on the quay? Well. Young people.",
            "I'll wait by the boat. When you're ready to row, I'll be there.",
        };
        hesper.ready = new[] { "The boat's there, and I'm ready. Whenever you are." };
        // Out on the water (the memory of the Ferryman), and at the Far shore.
        hesper.crossing = new[]
        {
            "It's colder out here. I thought it would be.",
            "You row better than he did. Don't tell him I said so.",
            "I remember the Ferryman slowing, every night, to wave at my lamp. Then one night he didn't. Not a wave. Not a look.",
            "Whatever took him, it had him a long while before it had the rest of us.",
        };
        hesper.farewell = new[]
        {
            "This is far enough, child. I can see the way from here.",
            "Here. For that lamp of yours. Mine won't need it where I'm going.",
            "Keep it lit. Someone might be looking for it.",
        };
        hesper.readyAsides = new[]
        {
            "You keep coming to check on me. I'm not going anywhere, child. That was rather the trouble.",
            "Don't hurry on my account. I've been dead for years. A few more minutes won't hurt.",
            "I've never been in a boat. I spent my whole life by the water and never once. Isn't that silly.",
        };
    }

    // The merchant soul (standing in for the Hub's), with the warm lantern on its staff: warm light is safety.
    static void MerchantAt(Vector3 position, float yaw)
    {
        var go = Place("Environment/merchant_soul", position, yaw);
        go.name = "Merchant Soul";
        go.AddComponent<Merchant>().interactRadius = 1.2f; // short: the landing is small (post, gate and spawn nearby)
        var light = PointLight(go.transform, GlowCentre(go, "MerchantLantern_Glow"), 3.2f, 1.6f, LanternLight);
        Flicker(light, 0.12f, 5f, 0.015f);
    }

    static MooringPost MooringPostAt(Vector3 position, float yaw)
    {
        var go = Place("Environment/mooring_post", position, yaw);
        go.name = "Mooring Post";
        var post = go.AddComponent<MooringPost>();
        var respawn = new GameObject("Respawn Point").transform;
        respawn.SetParent(go.transform, false);
        respawn.position = position + new Vector3(-0.9f, 0f, 0.9f); // on the jetty, beside the post
        post.respawnPoint = respawn;
        post.lanternLight = PointLight(go.transform, GlowCentre(go, "Lantern_Glow"), 5.5f, 3.5f, LanternLight);
        Flicker(post.lanternLight, 0.1f, 5f, 0.01f);
        post.lanternLight.enabled = false; // dark until the Apprentice first Rests here
        return post;
    }

    static void StreetLantern(Vector3 position, float yaw, float intensity = 3f)
    {
        var go = Place("Environment/street_lantern", position, yaw + 180f);
        Flicker(PointLight(go.transform, GlowCentre(go, "Lantern_Glow"), 6.5f, intensity, LanternLight), 0.1f, 5f, 0.01f);
    }

    static GameObject Torch(Vector3 position)
    {
        var go = Place("Environment/torch", position, Mathf.Repeat(position.x * 37f + position.z * 91f, 360f)); // varied but repeatable
        var light = PointLight(go.transform, GlowCentre(go, "Torch_Glow") + Vector3.up * 0.15f, 6f, 3.2f, TorchLight);
        Flicker(light, 0.25f, 7f, 0.04f);
        var embers = new GameObject("Embers");
        embers.transform.SetParent(go.transform, false);
        embers.transform.position = GlowCentre(go, "Torch_Glow") + Vector3.up * 0.2f;
        var crackle = embers.AddComponent<AudioSource>();
        crackle.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Audio + "torch_loop.wav");
        crackle.loop = true;
        crackle.playOnAwake = false; // TorchFlame starts it at a random point in the loop
        crackle.spatialBlend = 1f;
        crackle.rolloffMode = AudioRolloffMode.Linear;
        crackle.minDistance = 1f;
        crackle.maxDistance = 7f;
        crackle.volume = 0.5f;
        crackle.dopplerLevel = 0f;
        embers.AddComponent<TorchFlame>().emberMaterial = sparkMaterial;
        return go;
    }

    static void Flicker(Light light, float amount, float speed, float wobble)
    {
        var flicker = light.gameObject.AddComponent<LightFlicker>();
        flicker.amount = amount;
        flicker.speed = speed;
        flicker.wobble = wobble;
    }

    static Transform Bone(GameObject character, string name)
    {
        foreach (var t in character.GetComponentsInChildren<Transform>())
            if (t.name == name)
                return t;
        throw new Exception($"{character.name} has no bone {name}");
    }

    static Light PointLight(Transform parent, Vector3 position, float range, float intensity, Color colour)
    {
        var light = new GameObject("Lantern Light").AddComponent<Light>();
        light.transform.SetParent(parent, true);
        light.transform.position = position;
        light.type = LightType.Point;
        light.color = colour;
        light.range = range;
        light.intensity = intensity;
        light.shadows = LightShadows.None;
        return light;
    }

    // World-space centre of the vertices that use a material whose name starts with `prefix` (a lantern's glass).
    static Vector3 GlowCentre(GameObject go, string prefix)
    {
        foreach (var renderer in go.GetComponentsInChildren<Renderer>())
        {
            var materials = renderer.sharedMaterials;
            int index = Array.FindIndex(materials, m => m != null && m.name.StartsWith(prefix));
            if (index < 0)
                continue;
            Mesh mesh;
            Func<Vector3, Vector3> toWorld;
            if (renderer is SkinnedMeshRenderer skinned)
            {
                mesh = new Mesh(); // raw skinned vertices are in bind-pose space: bake the posed mesh
                skinned.BakeMesh(mesh, true);
                toWorld = v => skinned.transform.position + skinned.transform.rotation * v;
            }
            else
            {
                mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                toWorld = renderer.transform.TransformPoint;
            }
            var vertices = mesh.vertices;
            var triangles = mesh.GetTriangles(index);
            var sum = Vector3.zero;
            foreach (int i in triangles)
                sum += vertices[i];
            return toWorld(sum / Mathf.Max(1, triangles.Length));
        }
        throw new Exception($"{go.name} has no {prefix} material");
    }

    // ------------------------------------------------------------ look

    static void Lighting()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = night.sky;
        RenderSettings.ambientEquatorColor = night.equator;
        RenderSettings.ambientGroundColor = night.ground;
        var sh = new SphericalHarmonicsL2();
        sh.AddAmbientLight(night.equator.linear);
        sh.AddDirectionalLight(Vector3.up, night.sky.linear, 0.45f);
        RenderSettings.ambientProbe = sh;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = night.fog;
        RenderSettings.fogStartDistance = night.fogStart;
        RenderSettings.fogEndDistance = night.fogEnd;

        // The moon: enough to read shapes and cast long shadows, never enough to light the scene.
        var moon = new GameObject("Moon").AddComponent<Light>();
        moon.type = LightType.Directional;
        moon.color = night.moon;
        moon.intensity = night.moonIntensity;
        moon.shadows = LightShadows.Soft;
        moon.shadowStrength = night.shadowStrength;
        moon.transform.rotation = Quaternion.Euler(night.moonAngles);
    }

    static Mesh WaterGrid(float size, float cell)
    {
        string path = Generated + "/WaterGrid.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
            return existing;
        int n = Mathf.RoundToInt(size / cell);
        var vertices = new Vector3[(n + 1) * (n + 1)];
        for (int z = 0; z <= n; z++)
            for (int x = 0; x <= n; x++)
                vertices[z * (n + 1) + x] = new Vector3(x * cell - size / 2f, 0f, z * cell - size / 2f);
        var triangles = new int[n * n * 6];
        for (int z = 0, t = 0; z < n; z++)
            for (int x = 0; x < n; x++)
            {
                int i = z * (n + 1) + x;
                triangles[t++] = i; triangles[t++] = i + n + 1; triangles[t++] = i + 1;
                triangles[t++] = i + 1; triangles[t++] = i + n + 1; triangles[t++] = i + n + 2;
            }
        var mesh = new Mesh { name = "WaterGrid", indexFormat = IndexFormat.UInt32, vertices = vertices, triangles = triangles };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.bounds = new Bounds(Vector3.zero, new Vector3(size, 2f, size)); // room for the waves
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    static Material WaterMaterial()
    {
        var m = GeneratedMaterial("M_Water", "MyGame/Water");
        m.SetColor("_DeepColor", Hex("#0a1b21"));
        m.SetColor("_ShallowColor", Hex("#153137"));
        m.SetColor("_FoamColor", Hex("#8a9c9a"));
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material ParticleMaterial(string name, Texture2D texture)
    {
        var m = GeneratedMaterial(name, "MyGame/Particle");
        m.SetTexture("_MainTex", texture);
        EditorUtility.SetDirty(m);
        return m;
    }

    // A 64x64 alpha texture: a soft round dot, or a thin soft ring.
    static Texture2D RadialTexture(string name, bool ring)
    {
        string path = $"{Generated}/{name}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null)
            return existing;
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = name };
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = new Vector2(x + 0.5f - size / 2f, y + 0.5f - size / 2f).magnitude / (size / 2f);
                float a = ring ? Mathf.Clamp01(1f - Mathf.Abs(d - 0.8f) / 0.12f) : Mathf.Clamp01(1f - d);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        tex.Apply();
        AssetDatabase.CreateAsset(tex, path);
        return tex;
    }

    // A 64x96 alpha texture: a tongue of flame, round and dense at the foot, drawn up into a wavering point.
    static Texture2D FlameTexture(string name)
    {
        string path = $"{Generated}/{name}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null)
            return existing;
        const int w = 64, h = 96;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = name };
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f) / w - 0.5f, v = (y + 0.5f) / h;
                float wave = 0.04f * Mathf.Sin(v * 11f) * v;
                float half = v < 0.3f ? Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow((0.3f - v) / 0.3f, 2f))) * 0.4f
                                      : 0.4f * Mathf.Pow(1f - (v - 0.3f) / 0.7f, 1.2f);
                float d = Mathf.Abs(u - wave) / Mathf.Max(0.001f, half);
                float a = Mathf.Clamp01(1f - d) * Mathf.Clamp01(1.2f - v * 0.9f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, a)));
            }
        tex.Apply();
        AssetDatabase.CreateAsset(tex, path);
        return tex;
    }

    // All variants of a sound: name.wav, or name_1.wav, name_2.wav ...
    static AudioClip[] Clips(string name)
    {
        var single = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Audio}{name}.wav");
        if (single != null)
            return new[] { single };
        var clips = new System.Collections.Generic.List<AudioClip>();
        for (int i = 1; AssetDatabase.LoadAssetAtPath<AudioClip>($"{Audio}{name}_{i}.wav") is AudioClip clip; i++)
            clips.Add(clip);
        if (clips.Count == 0)
            Debug.LogWarning($"[MainSceneBuilder] no sound {name}; run tools/build-audio.sh");
        return clips.ToArray();
    }

    static Material GeneratedMaterial(string name, string shader)
    {
        string path = $"{Generated}/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find(shader));
            AssetDatabase.CreateAsset(m, path);
        }
        m.shader = Shader.Find(shader); // re-bind by name in case the shader asset's GUID changed
        return m;
    }

    static VolumeProfile VolumeProfileAsset()
    {
        string path = Generated + $"/{night.volume}.asset";
        AssetDatabase.DeleteAsset(path);
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, path);
        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(1f);
        bloom.intensity.Override(1.3f);
        bloom.scatter.Override(0.7f);
        profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.Neutral);
        profile.Add<Vignette>(true).intensity.Override(night.vignette);
        var grain = profile.Add<FilmGrain>(true);
        grain.type.Override(FilmGrainLookup.Medium2);
        grain.intensity.Override(night.grain);
        var grade = profile.Add<ColorAdjustments>(true);
        grade.contrast.Override(night.contrast);
        grade.saturation.Override(night.saturation);
        grade.postExposure.Override(night.exposure);
        if (night.filter != Color.white)
            grade.colorFilter.Override(night.filter);
        foreach (var component in profile.components)
            AssetDatabase.AddObjectToAsset(component, profile);
        EditorUtility.SetDirty(profile);
        return profile;
    }

    // ------------------------------------------------------------ screenshots (edit-mode, for headless review)

    static void Screenshots(string outDir)
    {
        Directory.CreateDirectory(outDir);
        EditorSettings.asyncShaderCompilation = false; // otherwise the first renders use placeholder shaders
        var cam = Camera.main;
        Render(cam, 1920, 1080, Path.Combine(outDir, "main-gameplay.png"));
        var wide = Object.Instantiate(cam.gameObject).GetComponent<Camera>();
        wide.GetComponent<CameraRig>().enabled = false;
        var rotation = Quaternion.Euler(50f, 0f, 0f);
        wide.transform.SetPositionAndRotation(new Vector3(0.5f, 0f, 0f) + rotation * new Vector3(0f, 0f, -30f), rotation);
        Render(wide, 1920, 1080, Path.Combine(outDir, "main-overview.png"));
        Object.DestroyImmediate(wide.gameObject);
        Debug.Log($"[MainSceneBuilder] Screenshots written to {outDir}");
    }

    static void Render(Camera cam, int width, int height, string path)
    {
        var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
        var request = new RenderPipeline.StandardRequest { destination = rt };
        if (RenderPipeline.SupportsRenderRequest(cam, request))
            RenderPipeline.SubmitRenderRequest(cam, request);
        else
        {
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = null;
        }
        var previous = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        RenderTexture.active = previous;
        rt.Release();
        File.WriteAllBytes(path, tex.EncodeToPNG());
    }

    static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
}
