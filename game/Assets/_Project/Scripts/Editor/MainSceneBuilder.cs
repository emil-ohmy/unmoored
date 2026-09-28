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
    const string Audio = "Assets/_Project/Audio/";
    const float QuayTop = 0.5f;
    // Night: the world is lit by its fires. Light colour carries meaning (docs/brief.md):
    // warm = the world and safety, sickly green = a Soured, cold pale = the Apprentice's bow lantern.
    static readonly Color Fog = Hex("#10131b");
    static readonly Color LanternLight = Hex("#ffb866");
    static readonly Color TorchLight = Hex("#ff8a3d");
    static readonly Color SouredLight = Hex("#9ee860");
    static readonly Color BowLanternLight = Hex("#9fc4ff");
    static Material sparkMaterial;

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
        AssetDatabase.ImportAsset(Audio.TrimEnd('/'), ImportAssetOptions.ImportRecursive);
        Directory.CreateDirectory(Generated);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        sparkMaterial = ParticleMaterial("M_Spark", RadialTexture("T_SoftDot", ring: false));
        Lighting();

        var water = new GameObject("Water", typeof(MeshFilter), typeof(MeshRenderer));
        water.GetComponent<MeshFilter>().sharedMesh = WaterGrid(90f, 1f);
        water.GetComponent<MeshRenderer>().sharedMaterial = WaterMaterial();

        // The quay and what stands on it.
        Place("Environment/quay", new Vector3(0f, 0f, 0f), 180f);
        Place("Environment/ruined_wall_long", new Vector3(-3.2f, QuayTop, 1.8f), 194f);
        Place("Environment/ruined_wall_short", new Vector3(3.4f, QuayTop, 2.6f), 148f);
        Place("Environment/ruined_wall_short", new Vector3(-6.2f, QuayTop, -2.2f), 255f);
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
        const float walkX = 2.4f, segment = 4.76f; // a walk piece is 4.42 m of planks; + one gap keeps the spacing
        for (int k = 0; k < 4; k++)
            Place("Environment/jetty_walk", new Vector3(walkX, QuayTop - 0.03f, -5.0f - segment * k), 0f);
        Place("Environment/jetty_landing", new Vector3(walkX, QuayTop - 0.03f, -5.0f - segment * 4), 0f);
        MooringPostAt(new Vector3(walkX + 1.25f, QuayTop, -26.7f), 180f);
        Place("Environment/boat", new Vector3(walkX + 2.55f, -0.12f, -25.6f), 186f);
        StreetLantern(new Vector3(walkX + 0.75f, QuayTop, -11.8f), -90f, 2.2f);
        StreetLantern(new Vector3(walkX - 0.75f, QuayTop, -19.4f), 90f, 2.2f);
        Place("Environment/house_b", new Vector3(-2.8f, 0f, -14.5f), 240f);
        Place("Environment/house_a", new Vector3(8.4f, 0f, -19.5f), 70f);
        Place("Environment/rock_b", new Vector3(5.3f, -0.1f, -10.6f), 40f);
        Place("Environment/rock_a", new Vector3(-0.9f, -0.12f, -21.3f), 300f);
        Place("Environment/rock_c", new Vector3(6.6f, -0.15f, -15.8f), 120f);
        Place("Environment/islet_small", new Vector3(-4.6f, -0.6f, -24.5f), 80f);

        // Half-sunken houses around the quay, rocks and islets in the water.
        Place("Environment/house_a", new Vector3(-10.4f, 0f, 3.0f), 280f);
        Place("Environment/house_b", new Vector3(-3.6f, 0f, 8.2f), 8f);
        Place("Environment/house_a", new Vector3(6.4f, 0f, 8.6f), 352f);
        Place("Environment/house_b", new Vector3(11.0f, 0f, 1.4f), 96f);
        Place("Environment/rock_a", new Vector3(-2.6f, -0.12f, -7.4f), 190f);
        Place("Environment/rock_c", new Vector3(-8.8f, -0.15f, -5.6f), 150f);
        Place("Environment/rock_b", new Vector3(8.8f, -0.1f, -5.8f), 250f);
        Place("Environment/rock_b", new Vector3(0.6f, -0.1f, 7.4f), 330f);
        Place("Environment/islet_small", new Vector3(-12.5f, -0.6f, -3.5f), 210f);
        Place("Environment/islet_big", new Vector3(13.5f, -0.8f, 9.5f), 165f);
        Place("Environment/rock_c", new Vector3(12.8f, -0.15f, -3.2f), 20f);

        // Characters.
        var start = new GameObject("Start Point").transform;
        start.position = new Vector3(2.4f, QuayTop, -25.3f); // on the landing, by the Mooring post
        var apprentice = Apprentice(start.position);
        Laden(new Vector3(4.2f, QuayTop, 0.4f), 20f); // back turned: sneak up for a free hit (one enemy while tuning)

        var systems = new GameObject("Game");
        systems.AddComponent<WorldState>().startPoint = start;
        var effects = systems.AddComponent<Effects>();
        effects.sparkMaterial = sparkMaterial;
        effects.flashMaterial = ParticleMaterial("M_BlockFlash", RadialTexture("T_Ring", ring: true));
        systems.AddComponent<Hud>().apprentice = apprentice;
        var sfx = systems.AddComponent<Sfx>();
        sfx.oarSwing = Clips("oar_swing");
        sfx.oarSwingHeavy = Clips("oar_swing_heavy");
        sfx.dash = Clips("dash");
        sfx.blockClang = Clips("block_clang");
        sfx.parry = Clips("parry");
        sfx.soulAbsorb = Clips("soul_absorb");
        var music = new GameObject("Music", typeof(AudioSource));
        music.GetComponent<AudioSource>().clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Audio + "Music/music_drowned_dark.wav");
        music.AddComponent<MusicPlayer>();

        var cam = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>(); // CameraRig adds the listener
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
        var apprentice = go.AddComponent<Apprentice>();
        go.AddComponent<DashGhosts>().ghostMaterial = GeneratedMaterial("M_DashGhost", "MyGame/Ghost");
        var lantern = PointLight(Bone(go, "Body"), GlowCentre(go, "SoulLantern_Glow"), 3.5f, 2.2f, BowLanternLight);
        Flicker(lantern, 0.06f, 2f, 0.01f); // a slow, steady pulse, not a flame
        go.AddComponent<BowLantern>().lanternLight = lantern;
        return apprentice;
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
        var lantern = PointLight(Bone(go, "Spine"), GlowCentre(go, "SouredLantern_Glow"), 3.8f, 1.8f, SouredLight);
        Flicker(lantern, 0.15f, 5f, 0.02f);
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

    static void Torch(Vector3 position)
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
        RenderSettings.ambientSkyColor = Hex("#1f2436");
        RenderSettings.ambientEquatorColor = Hex("#141a24");
        RenderSettings.ambientGroundColor = Hex("#08090c");
        var sh = new SphericalHarmonicsL2();
        sh.AddAmbientLight(Hex("#141a24").linear);
        sh.AddDirectionalLight(Vector3.up, Hex("#1f2436").linear, 0.45f);
        RenderSettings.ambientProbe = sh;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = Fog;
        RenderSettings.fogStartDistance = 14f;
        RenderSettings.fogEndDistance = 38f;

        // A weak, cold moon: enough to read shapes and cast long shadows, never enough to light the scene.
        var moon = new GameObject("Moon").AddComponent<Light>();
        moon.type = LightType.Directional;
        moon.color = Hex("#8ea8d8");
        moon.intensity = 0.25f;
        moon.shadows = LightShadows.Soft;
        moon.shadowStrength = 0.8f;
        moon.transform.rotation = Quaternion.Euler(56f, -38f, 0f);
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
        m.SetColor("_DeepColor", Hex("#081418"));
        m.SetColor("_ShallowColor", Hex("#10262a"));
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
        string path = Generated + "/MainVolume.asset";
        AssetDatabase.DeleteAsset(path);
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, path);
        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(1f);
        bloom.intensity.Override(1.3f);
        bloom.scatter.Override(0.7f);
        profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.Neutral);
        profile.Add<Vignette>(true).intensity.Override(0.36f);
        var grain = profile.Add<FilmGrain>(true);
        grain.type.Override(FilmGrainLookup.Medium2);
        grain.intensity.Override(0.35f);
        var grade = profile.Add<ColorAdjustments>(true);
        grade.contrast.Override(12f);
        grade.saturation.Override(-12f);
        grade.postExposure.Override(-0.25f);
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
