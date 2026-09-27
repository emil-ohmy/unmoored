using MyGame.Prototypes.CombatFeel;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// PROTOTYPE (wayfinder ticket "Combat feel: core verbs and pace"). Throwaway code, not production.
// Builds the CombatFeel scene: MyGame > Prototypes > Build Combat Feel Scene (Cmd+K, "combat feel").
public static class CombatFeelSceneBuilder
{
    const string Dir = "Assets/_Prototypes/CombatFeel";
    const string ScenePath = Dir + "/CombatFeel.unity";

    [MenuItem("MyGame/Prototypes/Build Combat Feel Scene")]
    static void BuildFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        Build();
    }

    public static void BuildFromCommandLine() => Build();

    static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Arena Floor";
        ground.transform.localScale = new Vector3(3f, 1f, 3f); // 30x30 m
        ground.GetComponent<Renderer>().sharedMaterial = CheckerMaterial();

        var wallMat = Lit("M_Wall", new Color(0.35f, 0.4f, 0.48f));
        Box("Wall N", new Vector3(0f, 0.5f, 15f), new Vector3(30f, 1f, 0.5f), wallMat);
        Box("Wall S", new Vector3(0f, 0.5f, -15f), new Vector3(30f, 1f, 0.5f), wallMat);
        Box("Wall E", new Vector3(15f, 0.5f, 0f), new Vector3(0.5f, 1f, 30f), wallMat);
        Box("Wall W", new Vector3(-15f, 0.5f, 0f), new Vector3(0.5f, 1f, 30f), wallMat);
        Box("Pillar 1", new Vector3(-6f, 1f, 5f), new Vector3(1.2f, 2f, 1.2f), wallMat);
        Box("Pillar 2", new Vector3(7f, 1f, -4f), new Vector3(1.2f, 2f, 1.2f), wallMat);
        Box("Pillar 3", new Vector3(5f, 1f, 8f), new Vector3(1.2f, 2f, 1.2f), wallMat);

        // Player: capsule + a nose showing facing + the oar on a pivot.
        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Apprentice (proto)";
        player.transform.position = new Vector3(0f, 1.05f, -5f);
        Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
        player.AddComponent<CharacterController>().minMoveDistance = 0f;
        player.GetComponent<Renderer>().sharedMaterial = Lit("M_Apprentice", new Color(0.95f, 0.55f, 0.2f));
        var nose = Box("Nose", Vector3.zero, new Vector3(0.3f, 0.2f, 0.3f), Lit("M_Dark", new Color(0.15f, 0.12f, 0.1f)));
        Object.DestroyImmediate(nose.GetComponent<BoxCollider>());
        nose.transform.SetParent(player.transform, false);
        nose.transform.localPosition = new Vector3(0f, 0.45f, 0.45f);
        var oarPivot = new GameObject("Oar Pivot").transform;
        oarPivot.SetParent(player.transform, false);
        oarPivot.localPosition = new Vector3(0f, 0.1f, 0f);
        var oar = Box("Oar", Vector3.zero, new Vector3(0.14f, 0.1f, 2.2f), Lit("M_Oar", new Color(0.45f, 0.3f, 0.18f)));
        Object.DestroyImmediate(oar.GetComponent<BoxCollider>());
        oar.transform.SetParent(oarPivot, false);
        oar.transform.localPosition = new Vector3(0f, 0f, 1.2f);
        var blade = Box("Blade", Vector3.zero, new Vector3(0.45f, 0.06f, 0.6f), Lit("M_Oar", new Color(0.45f, 0.3f, 0.18f)));
        Object.DestroyImmediate(blade.GetComponent<BoxCollider>());
        blade.transform.SetParent(oarPivot, false);
        blade.transform.localPosition = new Vector3(0f, 0f, 2.1f);
        var protoPlayer = player.AddComponent<ProtoPlayer>();
        protoPlayer.body = player.GetComponent<Renderer>();
        protoPlayer.oarPivot = oarPivot;

        // One Soured.
        var soured = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        soured.name = "Soured (proto)";
        soured.transform.position = new Vector3(0f, 1.35f, 5f);
        soured.transform.localScale = Vector3.one * 1.3f;
        soured.transform.rotation = Quaternion.LookRotation(Vector3.back);
        Object.DestroyImmediate(soured.GetComponent<CapsuleCollider>());
        soured.AddComponent<CharacterController>().minMoveDistance = 0f;
        soured.GetComponent<Renderer>().sharedMaterial = Lit("M_Soured", new Color(0.36f, 0.26f, 0.48f));
        var eye = Box("Eye", Vector3.zero, new Vector3(0.5f, 0.12f, 0.2f), Unlit("M_SouredEye", new Color(0.9f, 0.95f, 0.7f)));
        Object.DestroyImmediate(eye.GetComponent<BoxCollider>());
        eye.transform.SetParent(soured.transform, false);
        eye.transform.localPosition = new Vector3(0f, 0.45f, 0.42f);
        var indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
        indicator.name = "Soured Telegraph";
        Object.DestroyImmediate(indicator.GetComponent<BoxCollider>());
        indicator.GetComponent<Renderer>().sharedMaterial = Unlit("M_Telegraph", new Color(0.85f, 0.1f, 0.08f));
        indicator.SetActive(false);
        var protoSoured = soured.AddComponent<ProtoSoured>();
        protoSoured.body = soured.GetComponent<Renderer>();
        protoSoured.indicator = indicator.transform;

        var systems = new GameObject("Combat Tuning (edit presets here during Play)");
        var tuning = systems.AddComponent<CombatTuning>();
        tuning.presets = CombatTuning.DefaultPresets();
        tuning.current = 1;
        systems.AddComponent<CombatHud>();

        var cam = Camera.main;
        cam.fieldOfView = 30f;
        cam.backgroundColor = new Color(0.12f, 0.13f, 0.16f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        var follow = cam.gameObject.AddComponent<FixedAngleCamera>();
        follow.target = player.transform;
        cam.transform.SetPositionAndRotation(
            player.transform.position + Quaternion.Euler(follow.pitch, 0f, 0f) * new Vector3(0f, 0f, -follow.distance),
            Quaternion.Euler(follow.pitch, 0f, 0f));

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"[CombatFeelSceneBuilder] Built and saved {ScenePath}");
    }

    static GameObject Box(string name, Vector3 position, Vector3 scale, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position = position;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
    }

    static Material Lit(string name, Color color) => MaterialAsset(name, "Universal Render Pipeline/Lit", color);

    static Material Unlit(string name, Color color) => MaterialAsset(name, "Universal Render Pipeline/Unlit", color);

    static Material MaterialAsset(string name, string shader, Color color)
    {
        string path = $"{Dir}/Materials/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find(shader));
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(material);
        return material;
    }

    // 1 m checker squares so speed and distance are easy to read.
    static Material CheckerMaterial()
    {
        string texPath = $"{Dir}/Materials/T_Checker.asset";
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (tex == null)
        {
            tex = new Texture2D(2, 2) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
            Color a = new Color(0.42f, 0.44f, 0.42f), b = new Color(0.5f, 0.52f, 0.5f);
            tex.SetPixels(new[] { a, b, b, a });
            tex.Apply();
            AssetDatabase.CreateAsset(tex, texPath);
        }
        var material = Lit("M_Checker", Color.white);
        material.SetTexture("_BaseMap", tex);
        material.SetTextureScale("_BaseMap", new Vector2(15f, 15f)); // 30 m plane, 2x2 texture → 1 m squares
        return material;
    }
}
