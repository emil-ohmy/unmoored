using System;
using System.IO;
using System.Linq;
using MyGame.Prototypes.ArtTest;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

// PROTOTYPE (wayfinder ticket "Art style test"). Throwaway.
// Right-click in the Project window > MyGame > Build Art Test Scene.
// Headless screenshots: -executeMethod ArtTestSceneBuilder.BuildAndScreenshot (env ARTTEST_SHOTS = output folder).
public static class ArtTestSceneBuilder
{
    const string Dir = "Assets/_Prototypes/ArtTest";
    const string Models = Dir + "/Models/";
    const string ScenePath = Dir + "/ArtTest.unity";
    const float CameraPitch = 50f;

    // Model front (Blender -Y) lands on Unity -Z, i.e. facing the camera at yaw 0 (verified by render).
    const float FacingCamera = 0f;

    static readonly Vector3 ViewTarget = new Vector3(1.0f, 0f, 5.2f);

    [MenuItem("Assets/MyGame/Build Art Test Scene")]
    static void BuildFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        Build();
    }

    public static void BuildAndScreenshot()
    {
        Build();
        Screenshots(Environment.GetEnvironmentVariable("ARTTEST_SHOTS") ?? Path.GetFullPath("../builds/art-test"));
    }

    static void Build()
    {
        // Make sure the import rules have run on the latest FBX exports.
        AssetDatabase.ImportAsset(Dir + "/Models", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Color fog = Hex("#5d6475");

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
        RenderSettings.fogColor = fog;
        RenderSettings.fogStartDistance = 24f;
        RenderSettings.fogEndDistance = 70f;

        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = Hex("#ffdcae");
        sun.intensity = 2.0f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.9f;
        sun.transform.rotation = Quaternion.Euler(48f, -38f, 0f);

        var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
        water.name = "Water";
        water.transform.localScale = new Vector3(14f, 1f, 14f);
        Object.DestroyImmediate(water.GetComponent<Collider>());
        water.GetComponent<Renderer>().sharedMaterial = WaterMaterial();

        // Layout: shore islet at the back, the jetty reaching out from it towards the camera.
        Place("islet_small", new Vector3(1.0f, -0.55f, 8.6f), 20f);
        Place("jetty", new Vector3(1.2f, 0.45f, 7.6f), 180f);
        Place("boat", new Vector3(2.65f, -0.12f, 4.6f), 4f);
        var apprentice = Place("apprentice", new Vector3(1.05f, 0.51f, 3.9f), FacingCamera + 35f); // turned towards the Soured
        apprentice.AddComponent<IdleBob>();
        var shelf = Place("islet_small", new Vector3(-3.6f, -0.6f, 4.4f), -40f);
        var souredGo = Place("soured", new Vector3(-3.3f, 0f, 4.2f), FacingCamera - 90f); // faces +X, towards the Apprentice
        SitOn(souredGo, shelf);
        souredGo.AddComponent<SouredAttackLoop>();
        var bigIslet = Place("islet_big", new Vector3(6.4f, -0.75f, 8.4f), -15f);
        var tree = Place("orchard_tree", new Vector3(6.2f, 0f, 8.4f), 30f);
        SitOn(tree, bigIslet);
        Place("rock_a", new Vector3(-1.2f, -0.12f, 1.4f), 10f);
        Place("rock_b", new Vector3(4.4f, -0.1f, 2.2f), 70f);
        Place("rock_c", new Vector3(-6.5f, -0.15f, 8.5f), -20f);
        Place("rock_b", new Vector3(-1.2f, -0.1f, 7.4f), 150f);
        Place("rock_a", new Vector3(9.6f, -0.12f, 5.0f), 200f);
        Place("rock_c", new Vector3(3.2f, -0.2f, 11.2f), 80f);

        var cam = new GameObject("Main Camera").AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.fieldOfView = 30f;
        cam.farClipPlane = 200f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = fog;
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        Aim(cam, ViewTarget, 19f);

        var volume = new GameObject("Global Volume").AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = VolumeProfileAsset();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"[ArtTestSceneBuilder] Built {ScenePath}");
    }

    static GameObject Place(string model, Vector3 position, float yaw)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Models + model + ".fbx");
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        // Keep the model's own root rotation: static FBX roots import with a 90° X axis-conversion rotation
        // (bakeAxisConversion doesn't remove it for single-mesh files), so only add the yaw on top.
        go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f) * prefab.transform.localRotation);
        return go;
    }

    // Stand `thing` on top of `ground` (at the ground's highest point under the thing's x/z).
    static void SitOn(GameObject thing, GameObject ground)
    {
        var mesh = ground.GetComponentInChildren<MeshFilter>().sharedMesh;
        var t = ground.GetComponentInChildren<MeshFilter>().transform;
        Vector3 p = thing.transform.position;
        float best = float.MinValue;
        foreach (var v in mesh.vertices)
        {
            Vector3 w = t.TransformPoint(v);
            if (new Vector2(w.x - p.x, w.z - p.z).sqrMagnitude < 0.6f)
                best = Mathf.Max(best, w.y);
        }
        if (best > float.MinValue)
            thing.transform.position = new Vector3(p.x, best - 0.03f, p.z);
    }

    static void Aim(Camera cam, Vector3 target, float distance)
    {
        var rotation = Quaternion.Euler(CameraPitch, 0f, 0f);
        cam.transform.SetPositionAndRotation(target + rotation * new Vector3(0f, 0f, -distance), rotation);
    }

    static Material WaterMaterial()
    {
        string path = Dir + "/M_Water.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("MyGame/Proto/Toon"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.shader = Shader.Find("MyGame/Proto/Toon"); // re-bind by name in case the shader asset's GUID changed
        m.SetColor("_BaseColor", Hex("#2f5d68"));
        m.SetColor("_ShadowTint", Hex("#7a86b0"));
        m.SetFloat("_OutlineWidth", 0f);
        m.SetFloat("_RimStrength", 0f);
        EditorUtility.SetDirty(m);
        return m;
    }

    static VolumeProfile VolumeProfileAsset()
    {
        string path = Dir + "/ArtTestVolume.asset";
        AssetDatabase.DeleteAsset(path);
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, path);

        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(1f);
        bloom.intensity.Override(0.9f);
        bloom.scatter.Override(0.65f);
        var tonemapping = profile.Add<Tonemapping>(true);
        tonemapping.mode.Override(TonemappingMode.Neutral);
        var vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.22f);
        foreach (var component in profile.components)
            AssetDatabase.AddObjectToAsset(component, profile);
        EditorUtility.SetDirty(profile);
        return profile;
    }

    // ------------------------------------------------------------ screenshots (headless iteration)

    static void Screenshots(string outDir)
    {
        Directory.CreateDirectory(outDir);
        EditorSettings.asyncShaderCompilation = false; // otherwise the first renders use placeholder shaders
        var cam = Camera.main;

        Render(cam, 1920, 1080, Path.Combine(outDir, "overview.png"));

        var close = Object.Instantiate(cam.gameObject).GetComponent<Camera>();
        Aim(close, new Vector3(-1.0f, 0.5f, 4.3f), 9f);
        Render(close, 1920, 1080, Path.Combine(outDir, "closeup.png"));

        // Soured attack strip, sampled from the game angle.
        var soured = Object.FindAnyObjectByType<SouredAttackLoop>().gameObject;
        var anim = soured.GetComponentInChildren<Animation>();
        AnimationClip Clip(string suffix) => AnimationUtility.GetAnimationClips(anim.gameObject).First(c => c.name.EndsWith(suffix));
        var frames = new (string clip, float t)[] { ("Idle", 0f), ("Windup", 0.2f), ("Windup", 99f), ("Strike", 0.1f), ("Strike", 99f), ("Recover", 0.3f) };
        Aim(close, soured.transform.position + Vector3.up * 0.8f, 6.5f);
        const int size = 420;
        var strip = new Texture2D(size * frames.Length, size, TextureFormat.RGB24, false);
        for (int i = 0; i < frames.Length; i++)
        {
            var clip = Clip(frames[i].clip);
            clip.SampleAnimation(anim.gameObject, Mathf.Min(frames[i].t, clip.length));
            // Edit-mode renders don't re-skin, so render a baked copy of the posed mesh instead.
            var skinned = soured.GetComponentInChildren<SkinnedMeshRenderer>();
            var baked = new Mesh();
            skinned.BakeMesh(baked, true);
            var stand = new GameObject("BakedPose", typeof(MeshFilter), typeof(MeshRenderer));
            stand.transform.SetPositionAndRotation(skinned.transform.position, skinned.transform.rotation);
            stand.GetComponent<MeshFilter>().sharedMesh = baked;
            stand.GetComponent<MeshRenderer>().sharedMaterials = skinned.sharedMaterials;
            skinned.enabled = false;
            var frame = RenderToTexture(close, size, size);
            skinned.enabled = true;
            Object.DestroyImmediate(stand);
            strip.SetPixels(i * size, 0, size, size, frame.GetPixels());
        }
        strip.Apply();
        File.WriteAllBytes(Path.Combine(outDir, "soured-strip.png"), strip.EncodeToPNG());
        Object.DestroyImmediate(close.gameObject);
        Debug.Log($"[ArtTestSceneBuilder] Screenshots written to {outDir}");
    }

    static void Render(Camera cam, int width, int height, string path) =>
        File.WriteAllBytes(path, RenderToTexture(cam, width, height).EncodeToPNG());

    static Texture2D RenderToTexture(Camera cam, int width, int height)
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
        return tex;
    }

    static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
}
