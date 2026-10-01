using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

// Import rules for FBX files built by tools/build-models.sh into Assets/_Project/Art/Models/:
//  - Characters/ import with Legacy animation clips; Environment/ import static, with mesh colliders.
//  - Blender's material colours become MyGame/Toon materials; names ending in _Glow become emissive, and in _Metal
//    metal (a glint from the moon and every lantern, a bright rim).
//  - Unity does the axis conversion (bakeAxisConversion): rigged FBX are exported without Blender's
//    bake_space_transform, which Blender flags as broken with armatures.
//  - Smoothed normals are baked into the tangents for the Toon shader's outline pass.
//  - Architecture (Cutaway below) gets the Toon shader's cutaway: it opens up when it stands between the camera and
//    the Apprentice. Characters and props (souls, lanterns, the boat, pickups) never do.
public class ModelImportRules : AssetPostprocessor
{
    const string Root = "Assets/_Project/Art/Models/";
    const float GlowIntensity = 5f;

    bool InScope => assetPath.StartsWith(Root) && assetPath.EndsWith(".fbx");
    bool IsCharacter => assetPath.StartsWith(Root + "Characters/");

    // (Not thin posts: a Mooring post, a street lantern, a hook post, the far light. They hide nothing worth opening,
    // and a hole in one just looks broken.)
    static readonly string[] Cutaway = { "house", "ruined_wall", "tower", "stone_block", "stone_stairs", "railing",
                                         "drowned_chapel", "islet", "rock", "quay", "jetty", "shortcut_gate", "crate", "barrel",
                                         "terrace", "orchard_steps", "orchard_wall", "press_house" }; // (buildings and walls: not trees)
    bool IsArchitecture => !IsCharacter && System.Array.Exists(Cutaway, c => System.IO.Path.GetFileName(assetPath).StartsWith(c));

    public override uint GetVersion() => 7; // (2: the cutaway flag; 3: environment meshes readable; 4: metal; 5: no cutaway for posts; 6: the Orchard; 7: not its trees. Bumping re-imports every model)

    void OnPreprocessModel()
    {
        if (!InScope)
            return;
        var importer = (ModelImporter)assetImporter;
        importer.bakeAxisConversion = true;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        importer.importTangents = ModelImporterTangents.None; // replaced by smoothed normals below
        importer.animationType = IsCharacter ? ModelImporterAnimationType.Legacy : ModelImporterAnimationType.None;
        importer.importAnimation = IsCharacter;
        importer.addCollider = !IsCharacter;
        importer.isReadable = !IsCharacter; // (the Soured's ways are built from these at runtime: NavMeshBaker)
    }

    void OnPreprocessMaterialDescription(MaterialDescription description, Material material, AnimationClip[] clips)
    {
        if (!InScope)
            return;
        material.shader = Shader.Find("MyGame/Toon");
        // Blender exports linear colours; SetVector stores them without another sRGB conversion.
        Vector4 colour = description.TryGetProperty("DiffuseColor", out Vector4 c) ? c : Vector4.one;
        colour.w = 1f;
        material.SetVector("_BaseColor", colour);
        material.SetFloat("_Cutaway", IsArchitecture ? 1f : 0f);
        if (description.materialName.StartsWith("Torch") && description.materialName.EndsWith("_Glow"))
        {
            // Torch flames: the animated flame shader (sways, stretches, flickers; hot core, dark red tip).
            material.shader = Shader.Find("MyGame/Flame");
            material.SetVector("_EmissionColor", colour * GlowIntensity);
            material.SetVector("_TipColor", new Vector4(colour.x, colour.y * 0.3f, colour.z * 0.2f, 1f) * GlowIntensity * 0.55f);
            return;
        }
        if (description.materialName.EndsWith("_Metal"))
        {
            // Metal: a glint, a bright rim, and deeper shadows (so its highlights stand out).
            material.SetFloat("_Specular", 1.1f);
            material.SetFloat("_SpecularSize", 0.1f);
            material.SetFloat("_RimStrength", 0.45f);
            material.SetVector("_RimColor", Vector4.Lerp(colour, Vector4.one, 0.5f));
            material.SetVector("_ShadowTint", new Vector4(0.5f, 0.46f, 0.6f, 1f));
            material.SetFloat("_AmbientStrength", 0.35f);
        }
        if (description.materialName.EndsWith("_Glow"))
        {
            material.SetVector("_EmissionColor", colour * GlowIntensity);
            material.SetFloat("_OutlineWidth", 0f);
        }
    }

    void OnPostprocessModel(GameObject root)
    {
        if (!InScope)
            return;
        foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            BakeSmoothNormalsIntoTangents(filter.sharedMesh);
        foreach (var skinned in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            BakeSmoothNormalsIntoTangents(skinned.sharedMesh);
    }

    static void BakeSmoothNormalsIntoTangents(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        var sums = new Dictionary<Vector3Int, Vector3>();
        Vector3Int Key(Vector3 v) => new Vector3Int(Mathf.RoundToInt(v.x * 10000f), Mathf.RoundToInt(v.y * 10000f), Mathf.RoundToInt(v.z * 10000f));

        for (int i = 0; i < vertices.Length; i++)
        {
            var key = Key(vertices[i]);
            sums[key] = (sums.TryGetValue(key, out var sum) ? sum : Vector3.zero) + normals[i];
        }

        var tangents = new Vector4[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 n = sums[Key(vertices[i])].normalized;
            tangents[i] = new Vector4(n.x, n.y, n.z, 1f);
        }
        mesh.tangents = tangents;
    }
}
