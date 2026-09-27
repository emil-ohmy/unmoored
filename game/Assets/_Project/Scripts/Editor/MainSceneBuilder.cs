using System;
using System.IO;
using MyGame;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

// Builds Assets/_Project/Scenes/Main.unity: the first playable slice, a corner of the Drowned Village.
// Assets menu > MyGame > Build Main Scene, or headless:
//   -executeMethod MainSceneBuilder.BuildFromCommandLine
//   -executeMethod MainSceneBuilder.BuildAndScreenshot   (env SHOTS = output folder, default builds/shots)
// Re-running replaces the scene; hand edits to Main.unity are lost, so change the layout here.
public static class MainSceneBuilder
{
    const string ScenePath = "Assets/_Project/Scenes/Main.unity";
    const string Models = "Assets/_Project/Art/Models/";
    const string Generated = "Assets/_Project/Art/Generated";
    const float QuayTop = 0.5f;
    static readonly Color Fog = Hex("#5d6475");
    static readonly Color LanternLight = Hex("#ffb866");

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
        AssetDatabase.ImportAsset(Models.TrimEnd('/'), ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
        Directory.CreateDirectory(Generated);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Lighting();

        var water = new GameObject("Water", typeof(MeshFilter), typeof(MeshRenderer));
        water.GetComponent<MeshFilter>().sharedMesh = WaterGrid(90f, 1f);
        water.GetComponent<MeshRenderer>().sharedMaterial = WaterMaterial();

        // The quay and what stands on it.
        Place("Environment/quay", new Vector3(0f, 0f, 0f), 0f);
        Place("Environment/ruined_wall_long", new Vector3(-3.2f, QuayTop, 1.8f), 14f);
        Place("Environment/ruined_wall_short", new Vector3(3.4f, QuayTop, 2.6f), -32f);
        Place("Environment/ruined_wall_short", new Vector3(-6.2f, QuayTop, -2.2f), 75f);
        Place("Environment/barrel", new Vector3(-4.9f, QuayTop, 2.6f), 0f);
        Place("Environment/barrel", new Vector3(-4.2f, QuayTop, 3.0f), 40f);
        Place("Environment/crate", new Vector3(-5.6f, QuayTop, 3.4f), 12f);
        Place("Environment/crate", new Vector3(5.9f, QuayTop, -3.9f), -20f);
        Place("Environment/barrel", new Vector3(6.6f, QuayTop, -3.2f), 0f);
        StreetLantern(new Vector3(-6.4f, QuayTop, -3.9f), 90f);
        StreetLantern(new Vector3(6.2f, QuayTop, 4.0f), -90f);

        // The jetty reaches out from the quay's front edge towards the camera, with the Mooring post at its end.
        Place("Environment/jetty", new Vector3(2.4f, QuayTop - 0.03f, -5.0f), 180f);
        MooringPostAt(new Vector3(3.25f, QuayTop, -9.3f), 0f);
        Place("Environment/boat", new Vector3(3.95f, -0.12f, -8.4f), 6f);

        // Half-sunken houses around the quay, rocks and islets in the water.
        Place("Environment/house_a", new Vector3(-10.4f, 0f, 3.0f), 100f);
        Place("Environment/house_b", new Vector3(-3.6f, 0f, 8.2f), 188f);
        Place("Environment/house_a", new Vector3(6.4f, 0f, 8.6f), 172f);
        Place("Environment/house_b", new Vector3(11.0f, 0f, 1.4f), -84f);
        Place("Environment/rock_a", new Vector3(-2.6f, -0.12f, -7.4f), 10f);
        Place("Environment/rock_c", new Vector3(-8.8f, -0.15f, -5.6f), -30f);
        Place("Environment/rock_b", new Vector3(8.8f, -0.1f, -5.8f), 70f);
        Place("Environment/rock_b", new Vector3(0.6f, -0.1f, 7.4f), 150f);
        Place("Environment/islet_small", new Vector3(-12.5f, -0.6f, -3.5f), 30f);
        Place("Environment/islet_big", new Vector3(13.5f, -0.8f, 9.5f), -15f);
        Place("Environment/rock_c", new Vector3(12.8f, -0.15f, -3.2f), 200f);

        // Characters.
        var start = new GameObject("Start Point").transform;
        start.position = new Vector3(2.4f, QuayTop, -3.6f);
        var apprentice = Apprentice(start.position);
        Laden(new Vector3(-1.4f, QuayTop, 3.6f), 170f);
        Laden(new Vector3(4.2f, QuayTop, 0.4f), -120f);

        var systems = new GameObject("Game");
        systems.AddComponent<WorldState>().startPoint = start;
        systems.AddComponent<Hud>().apprentice = apprentice;

        var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
        cam.tag = "MainCamera";
        cam.fieldOfView = 30f;
        cam.farClipPlane = 200f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Fog;
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        var rig = cam.gameObject.AddComponent<CameraRig>();
        rig.target = apprentice.transform;
        cam.transform.SetPositionAndRotation(
            apprentice.transform.position + Quaternion.Euler(rig.pitch, 0f, 0f) * new Vector3(0f, 0f, -rig.distance),
            Quaternion.Euler(rig.pitch, 0f, 0f));

        var volume = new GameObject("Global Volume").AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = VolumeProfileAsset();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"[MainSceneBuilder] Built {ScenePath}");
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
        return go.AddComponent<Apprentice>();
    }

    static void Laden(Vector3 position, float yaw)
    {
        var go = Place("Characters/laden", position, yaw);
        go.name = "Laden";
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
        laden.aggroRadius = 8f;
        laden.attackRange = 2.0f;
        laden.chaseSpeed = 1.9f;
        laden.telegraphDuration = 0.9f;
        laden.strikeDuration = 0.3f;
        laden.recoverDuration = 1.2f;
        laden.impactAt = 0.5f;
        laden.lungeDistance = 0.5f;
        laden.damage = 30f;
        laden.strikeReach = 2.2f;
    }

    static MooringPost MooringPostAt(Vector3 position, float yaw)
    {
        var go = Place("Environment/mooring_post", position, yaw);
        go.name = "Mooring Post";
        var post = go.AddComponent<MooringPost>();
        var respawn = new GameObject("Respawn Point").transform;
        respawn.SetParent(go.transform, false);
        respawn.position = position + new Vector3(-0.9f, 0f, 0.9f);
        post.respawnPoint = respawn;
        post.lanternLight = PointLight(go.transform, GlowCentre(go), 5.5f, 3.5f);
        post.lanternLight.enabled = false; // dark until the Apprentice first Rests here
        return post;
    }

    static void StreetLantern(Vector3 position, float yaw)
    {
        var go = Place("Environment/street_lantern", position, yaw);
        PointLight(go.transform, GlowCentre(go), 7f, 4f);
    }

    static Light PointLight(Transform parent, Vector3 position, float range, float intensity)
    {
        var light = new GameObject("Lantern Light").AddComponent<Light>();
        light.transform.SetParent(parent, true);
        light.transform.position = position;
        light.type = LightType.Point;
        light.color = LanternLight;
        light.range = range;
        light.intensity = intensity;
        light.shadows = LightShadows.None;
        return light;
    }

    // World-space centre of the vertices that use the "Lantern_Glow" material.
    static Vector3 GlowCentre(GameObject go)
    {
        foreach (var filter in go.GetComponentsInChildren<MeshFilter>())
        {
            var materials = filter.GetComponent<MeshRenderer>().sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null || !materials[i].name.StartsWith("Lantern_Glow"))
                    continue;
                var mesh = filter.sharedMesh;
                var vertices = mesh.vertices;
                var sum = Vector3.zero;
                int n = 0;
                foreach (int index in mesh.GetTriangles(i))
                {
                    sum += vertices[index];
                    n++;
                }
                return filter.transform.TransformPoint(sum / Mathf.Max(1, n));
            }
        }
        return go.transform.position + Vector3.up;
    }

    // ------------------------------------------------------------ look

    static void Lighting()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Hex("#8a86a6");
        RenderSettings.ambientEquatorColor = Hex("#5a6a78");
        RenderSettings.ambientGroundColor = Hex("#2a2f38");
        var sh = new SphericalHarmonicsL2();
        sh.AddAmbientLight(Hex("#5a6a78").linear);
        sh.AddDirectionalLight(Vector3.up, Hex("#8a86a6").linear, 0.6f);
        RenderSettings.ambientProbe = sh;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = Fog;
        RenderSettings.fogStartDistance = 20f;
        RenderSettings.fogEndDistance = 60f;

        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = Hex("#ffdcae");
        sun.intensity = 1.6f; // a little dimmer than the art test so the lanterns read
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.9f;
        sun.transform.rotation = Quaternion.Euler(48f, -38f, 0f);
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
        string path = Generated + "/M_Water.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("MyGame/Water"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.shader = Shader.Find("MyGame/Water");
        return m;
    }

    static VolumeProfile VolumeProfileAsset()
    {
        string path = Generated + "/MainVolume.asset";
        AssetDatabase.DeleteAsset(path);
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, path);
        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(1f);
        bloom.intensity.Override(1.1f);
        bloom.scatter.Override(0.65f);
        profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.Neutral);
        profile.Add<Vignette>(true).intensity.Override(0.22f);
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
