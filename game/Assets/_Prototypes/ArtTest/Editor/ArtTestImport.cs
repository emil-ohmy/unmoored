using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

// PROTOTYPE (wayfinder ticket "Art style test"). Throwaway.
// Import rules for FBX files in Assets/_Prototypes/ArtTest/Models/:
//  - Blender's material colours become ProtoToon materials (names ending in _Glow become emissive).
//  - Unity does the Z-up -> Y-up axis conversion (bakeAxisConversion); the FBX is exported without Blender's
//    experimental bake_space_transform, which is known to break armatures.
//  - soured.fbx imports as a Legacy animated model; everything else as a static mesh.
//  - Smoothed normals are baked into the tangents for the outline pass.
public class ArtTestImport : AssetPostprocessor
{
    const string Folder = "Assets/_Prototypes/ArtTest/Models/";
    const float GlowIntensity = 8f;

    bool InScope => assetPath.StartsWith(Folder);

    void OnPreprocessModel()
    {
        if (!InScope)
            return;
        var importer = (ModelImporter)assetImporter;
        importer.bakeAxisConversion = true;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        importer.importTangents = ModelImporterTangents.None;
        bool animated = assetPath.EndsWith("soured.fbx");
        importer.animationType = animated ? ModelImporterAnimationType.Legacy : ModelImporterAnimationType.None;
        importer.importAnimation = animated;
    }

    void OnPreprocessMaterialDescription(MaterialDescription description, Material material, AnimationClip[] clips)
    {
        if (!InScope)
            return;
        material.shader = Shader.Find("MyGame/Proto/Toon");
        // FBX colours from Blender are linear; SetVector stores them without another sRGB conversion.
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
