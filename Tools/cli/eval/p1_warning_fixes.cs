var sb = new System.Text.StringBuilder();
// Placeholder trees: a LODGroup makes terrain treat them as mesh trees (no Soft Occlusion requirement) and culls them at distance.
for (int i = 0; i < 4; i++)
{
    string path = "Assets/GliderLevel/Placeholder/PlaceholderConifer" + i + ".prefab";
    var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
    var lod = root.GetComponent<UnityEngine.LODGroup>();
    if (lod == null) lod = root.AddComponent<UnityEngine.LODGroup>();
    lod.SetLODs(new[] { new UnityEngine.LOD(0.004f, new UnityEngine.Renderer[] { root.GetComponent<UnityEngine.MeshRenderer>() }) });
    lod.RecalculateBounds();
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
    UnityEditor.PrefabUtility.UnloadPrefabContents(root);
    sb.Append("LODGroup on ").Append(path).Append("\n");
}
foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.Terrain>(UnityEngine.FindObjectsSortMode.None))
    if (t.terrainData.treePrototypes.Length > 0) { t.terrainData.RefreshPrototypes(); t.Flush(); }
// Main object names that don't match their file names
foreach (var path in new[] { "Assets/GliderLevel/Material/glass_0.mat", "Assets/GliderLevel/Resources/fonts & materials/LiberationSans SDF Material_0.mat" })
{
    var m = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
    m.name = System.IO.Path.GetFileNameWithoutExtension(path);
    UnityEditor.EditorUtility.SetDirty(m);
    sb.Append("renamed main object of ").Append(path).Append("\n");
}
UnityEditor.AssetDatabase.SaveAssets();
UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
return sb.ToString();
