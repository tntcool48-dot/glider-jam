// Phase 1 placeholders for assets the package referenced but did not contain:
// terrain layers (painted splat maps are kept) and tree prefabs (tree positions are kept).
var sb = new System.Text.StringBuilder();
string dir = "Assets/GliderLevel/Placeholder";
if (!UnityEditor.AssetDatabase.IsValidFolder(dir)) UnityEditor.AssetDatabase.CreateFolder("Assets/GliderLevel", "Placeholder");

// ---- Terrain layers ----
var palette = new UnityEngine.Color[]
{
    new UnityEngine.Color(0.30f, 0.33f, 0.18f), // slot0 grass
    new UnityEngine.Color(0.45f, 0.40f, 0.26f), // slot1 dry grass
    new UnityEngine.Color(0.42f, 0.40f, 0.38f), // slot2 rock
    new UnityEngine.Color(0.30f, 0.28f, 0.27f), // slot3 dark rock
    new UnityEngine.Color(0.40f, 0.31f, 0.22f), // slot4 dirt
    new UnityEngine.Color(0.55f, 0.53f, 0.50f), // slot5 pale rock
};
var layers = new UnityEngine.TerrainLayer[palette.Length];
for (int i = 0; i < palette.Length; i++)
{
    string texPath = dir + "/PlaceholderTerrain" + i + ".png";
    string layerPath = dir + "/PlaceholderTerrain" + i + ".terrainlayer";
    if (!System.IO.File.Exists(texPath))
    {
        var t = new UnityEngine.Texture2D(256, 256, UnityEngine.TextureFormat.RGBA32, false);
        var px = new UnityEngine.Color[256 * 256];
        for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++)
        {
            float n = UnityEngine.Mathf.PerlinNoise(x * 0.05f + i * 13f, y * 0.05f) * 0.6f + UnityEngine.Mathf.PerlinNoise(x * 0.21f, y * 0.21f + i * 7f) * 0.4f;
            px[y * 256 + x] = palette[i] * (0.75f + 0.5f * n);
        }
        t.SetPixels(px); t.Apply();
        System.IO.File.WriteAllBytes(texPath, UnityEngine.ImageConversion.EncodeToPNG(t));
        UnityEditor.AssetDatabase.ImportAsset(texPath);
    }
    var layer = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TerrainLayer>(layerPath);
    if (layer == null)
    {
        layer = new UnityEngine.TerrainLayer();
        layer.diffuseTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(texPath);
        layer.tileSize = new UnityEngine.Vector2(20f, 20f);
        layer.smoothness = 0.1f;
        layer.metallic = 0f;
        UnityEditor.AssetDatabase.CreateAsset(layer, layerPath);
    }
    layers[i] = layer;
}
var terrains = UnityEngine.Object.FindObjectsByType<UnityEngine.Terrain>(UnityEngine.FindObjectsSortMode.None);
foreach (var t in terrains)
{
    var d = t.terrainData;
    var arr = new UnityEngine.TerrainLayer[d.alphamapLayers];
    for (int i = 0; i < arr.Length; i++) arr[i] = layers[i % layers.Length];
    d.terrainLayers = arr;
    UnityEditor.EditorUtility.SetDirty(d);
    sb.Append(t.name).Append(": ").Append(arr.Length).Append(" placeholder layers\n");
}

// ---- Tree placeholder: low-poly conifer (trunk + 3 stacked cones), 2 submeshes ----
string meshPath = dir + "/PlaceholderConifer.asset";
var mesh = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(meshPath);
if (mesh == null)
{
    var v = new System.Collections.Generic.List<UnityEngine.Vector3>();
    var trunk = new System.Collections.Generic.List<int>();
    var leaves = new System.Collections.Generic.List<int>();
    int seg = 10;
    System.Action<float, float, float, float, System.Collections.Generic.List<int>> frustum = (y0, y1, r0, r1, tris) =>
    {
        int b = v.Count;
        for (int s = 0; s <= seg; s++)
        {
            float a = s * UnityEngine.Mathf.PI * 2f / seg;
            float c = UnityEngine.Mathf.Cos(a), sn = UnityEngine.Mathf.Sin(a);
            v.Add(new UnityEngine.Vector3(c * r0, y0, sn * r0));
            v.Add(new UnityEngine.Vector3(c * r1, y1, sn * r1));
        }
        for (int s = 0; s < seg; s++)
        {
            int i0 = b + s * 2;
            tris.AddRange(new[] { i0, i0 + 1, i0 + 2, i0 + 1, i0 + 3, i0 + 2 });
        }
        // bottom cap
        int center = v.Count; v.Add(new UnityEngine.Vector3(0, y0, 0));
        for (int s = 0; s < seg; s++) tris.AddRange(new[] { center, b + s * 2 + 2, b + s * 2 });
    };
    frustum(0f, 3.5f, 0.35f, 0.25f, trunk);
    frustum(2.5f, 8.5f, 3.2f, 0.05f, leaves);
    frustum(5.5f, 11.5f, 2.5f, 0.05f, leaves);
    frustum(8.5f, 14f, 1.7f, 0.02f, leaves);
    mesh = new UnityEngine.Mesh { name = "PlaceholderConifer" };
    mesh.SetVertices(v);
    mesh.subMeshCount = 2;
    mesh.SetTriangles(trunk, 0);
    mesh.SetTriangles(leaves, 1);
    mesh.RecalculateNormals();
    mesh.RecalculateBounds();
    UnityEditor.AssetDatabase.CreateAsset(mesh, meshPath);
}
var hdLit = UnityEngine.Shader.Find("HDRP/Lit");
System.Func<string, UnityEngine.Color, UnityEngine.Material> mat = (name, col) =>
{
    string p = dir + "/" + name + ".mat";
    var m = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(p);
    if (m == null) { m = new UnityEngine.Material(hdLit); m.SetColor("_BaseColor", col); m.SetFloat("_Smoothness", 0.15f); UnityEditor.Rendering.HighDefinition.HDShaderUtils.ResetMaterialKeywords(m); UnityEditor.AssetDatabase.CreateAsset(m, p); }
    return m;
};
var bark = mat("PlaceholderBark", new UnityEngine.Color(0.22f, 0.15f, 0.10f));
var tints = new[] { new UnityEngine.Color(0.10f, 0.20f, 0.10f), new UnityEngine.Color(0.13f, 0.24f, 0.11f), new UnityEngine.Color(0.09f, 0.17f, 0.12f), new UnityEngine.Color(0.15f, 0.22f, 0.09f) };
var scales = new[] { 1.0f, 1.25f, 0.85f, 1.1f };
var protos = new UnityEngine.TreePrototype[4];
for (int i = 0; i < 4; i++)
{
    string prefabPath = dir + "/PlaceholderConifer" + i + ".prefab";
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(prefabPath);
    if (prefab == null)
    {
        var go = new UnityEngine.GameObject("PlaceholderConifer" + i);
        go.AddComponent<UnityEngine.MeshFilter>().sharedMesh = mesh;
        go.AddComponent<UnityEngine.MeshRenderer>().sharedMaterials = new[] { bark, mat("PlaceholderLeaves" + i, tints[i]) };
        go.transform.localScale = UnityEngine.Vector3.one * scales[i];
        prefab = UnityEditor.PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        UnityEngine.Object.DestroyImmediate(go);
    }
    protos[i] = new UnityEngine.TreePrototype { prefab = prefab };
}
foreach (var t in terrains)
{
    var d = t.terrainData;
    if (d.treePrototypes.Length == 4)
    {
        d.treePrototypes = protos;
        d.RefreshPrototypes();
        UnityEditor.EditorUtility.SetDirty(d);
        sb.Append(t.name).Append(": 4 placeholder tree prototypes, ").Append(d.treeInstanceCount).Append(" trees kept\n");
    }
}
UnityEditor.AssetDatabase.SaveAssets();
return sb.ToString();
