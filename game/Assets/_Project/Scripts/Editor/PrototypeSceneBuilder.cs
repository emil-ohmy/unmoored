using MyGame;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Builds a throwaway playable setup in the Main scene: ground, crates from art/models, a player.
// Run from the editor menu (MyGame > Build Prototype Scene) or headless:
//   Unity -batchmode -quit -projectPath game -executeMethod PrototypeSceneBuilder.BuildFromCommandLine
// Re-running replaces the previous "Prototype" objects.
public static class PrototypeSceneBuilder
{
    const string ScenePath = "Assets/_Project/Scenes/Main.unity";
    const string MaterialsDir = "Assets/_Project/Art/Materials";
    const string CratePath = "Assets/_Project/Art/Models/crate.fbx";

    [MenuItem("MyGame/Build Prototype Scene")]
    static void BuildFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        Build();
        Selection.activeGameObject = GameObject.Find("Prototype/Player");
    }

    public static void BuildFromCommandLine() => Build();

    static void Build()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var old = GameObject.Find("Prototype");
        if (old != null)
            Object.DestroyImmediate(old);
        var root = new GameObject("Prototype");

        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(root.transform);
        ground.transform.localScale = new Vector3(5f, 1f, 5f); // Plane is 10x10 m, so 50x50 m
        ground.GetComponent<Renderer>().sharedMaterial = LitMaterial("M_Ground", new Color(0.45f, 0.55f, 0.4f));

        var crates = new GameObject("Crates");
        crates.transform.SetParent(root.transform);
        var cratePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CratePath);
        if (cratePrefab == null)
            Debug.LogWarning($"No crate at {CratePath}; run tools/export-models.sh first. Skipping crates.");
        else
        {
            PlaceCrate(cratePrefab, crates.transform, new Vector3(3f, 0f, 3f));
            PlaceCrate(cratePrefab, crates.transform, new Vector3(4.2f, 0f, 3f));
            PlaceCrate(cratePrefab, crates.transform, new Vector3(3.6f, 1f, 3f)); // stacked
            PlaceCrate(cratePrefab, crates.transform, new Vector3(-4f, 0f, 5f));
            PlaceCrate(cratePrefab, crates.transform, new Vector3(-2f, 0f, -4f));
        }

        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.tag = "Player";
        player.transform.SetParent(root.transform);
        player.transform.position = new Vector3(0f, 1.1f, 0f);
        Object.DestroyImmediate(player.GetComponent<CapsuleCollider>()); // CharacterController is the collider
        // Default 0.001 skips tiny per-frame moves at high frame rates, which makes isGrounded flicker.
        player.AddComponent<CharacterController>().minMoveDistance = 0f;
        player.AddComponent<PlayerController>();
        player.GetComponent<Renderer>().sharedMaterial = LitMaterial("M_Player", new Color(0.95f, 0.55f, 0.2f));

        var cam = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
        if (cam != null)
        {
            if (!cam.TryGetComponent(out FollowCamera follow))
                follow = cam.gameObject.AddComponent<FollowCamera>();
            follow.Target = player.transform;
            cam.transform.position = player.transform.position + new Vector3(0f, 6f, -8f);
            cam.transform.LookAt(player.transform.position + Vector3.up);
            EditorUtility.SetDirty(follow);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[PrototypeSceneBuilder] Main scene built and saved.");
    }

    static void PlaceCrate(GameObject prefab, Transform parent, Vector3 basePosition)
    {
        var crate = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        crate.transform.position = basePosition;

        // Sit the crate on basePosition regardless of where the model's origin is.
        var bounds = crate.GetComponentInChildren<Renderer>().bounds;
        crate.transform.position += Vector3.up * (basePosition.y - bounds.min.y);

        foreach (var filter in crate.GetComponentsInChildren<MeshFilter>())
            filter.gameObject.AddComponent<BoxCollider>();
        var body = crate.AddComponent<Rigidbody>();
        body.mass = 5f;
    }

    static Material LitMaterial(string name, Color color)
    {
        string path = $"{MaterialsDir}/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return material;
    }
}
