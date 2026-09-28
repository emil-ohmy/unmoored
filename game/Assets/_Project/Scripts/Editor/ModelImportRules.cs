using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

// Import rules for FBX files built by tools/build-models.sh into Assets/_Project/Art/Models/:
//  - Characters/ import with Legacy animation clips; Environment/ import static, with mesh colliders.
//  - Blender's material colours become MyGame/Toon materials; names ending in _Glow become emissive.
//  - Unity does the axis conversion (bakeAxisConversion): rigged FBX are exported without Blender's
//    bake_space_transform, which Blender flags as broken with armatures.
//  - Smoothed normals are baked into the tangents for the Toon shader's outline pass.
public class ModelImportRules : AssetPostprocessor
{
    const string Root = "Assets/_Project/Art/Models/";
    const float GlowIntensity = 5f;

    bool InScope => assetPath.StartsWith(Root) && assetPath.EndsWith(".fbx");
    bool IsCharacter => assetPath.StartsWith(Root + "Characters/");

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
