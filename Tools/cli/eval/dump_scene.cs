var sb = new System.Text.StringBuilder();
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
System.Action<UnityEngine.Transform, int> walk = null;
walk = (t, depth) =>
{
    var go = t.gameObject;
    var comps = new System.Collections.Generic.List<string>();
    foreach (var c in go.GetComponents<UnityEngine.Component>())
    {
        if (c == null) { comps.Add("MISSING"); continue; }
        var n = c.GetType().Name;
        if (n == "Transform") continue;
        if (c is UnityEngine.Renderer r)
        {
            var mats = new System.Collections.Generic.List<string>();
            foreach (var m in r.sharedMaterials) mats.Add(m == null ? "null" : m.name + "<" + (m.shader == null ? "?" : m.shader.name) + ">");
            n += "[" + string.Join(",", mats) + "]";
        }
        if (c is UnityEngine.Collider col) n += (col.isTrigger ? "(trigger)" : "");
        comps.Add(n);
    }
    var p = t.position;
    var s = t.lossyScale;
    sb.Append(new string(' ', depth * 2)).Append(go.name)
      .Append(go.activeSelf ? "" : " (inactive)")
      .Append(" tag=").Append(go.tag)
      .Append(" pos=(").Append(p.x.ToString("F0")).Append(",").Append(p.y.ToString("F0")).Append(",").Append(p.z.ToString("F0")).Append(")")
      .Append(" scl=(").Append(s.x.ToString("F1")).Append(",").Append(s.y.ToString("F1")).Append(",").Append(s.z.ToString("F1")).Append(")")
      .Append(" ").Append(string.Join(" ", comps)).Append("\n");
    if (depth < 3) foreach (UnityEngine.Transform ch in t) walk(ch, depth + 1);
    else if (t.childCount > 0) sb.Append(new string(' ', (depth + 1) * 2)).Append("... ").Append(t.childCount).Append(" children\n");
};
foreach (var root in scene.GetRootGameObjects()) walk(root.transform, 0);
foreach (var terrain in UnityEngine.Object.FindObjectsByType<UnityEngine.Terrain>(UnityEngine.FindObjectsSortMode.None))
{
    var d = terrain.terrainData;
    sb.Append("TERRAIN ").Append(terrain.name).Append(" size=").Append(d.size).Append(" heightRes=").Append(d.heightmapResolution)
      .Append(" trees=").Append(d.treeInstanceCount).Append(" protos=").Append(d.treePrototypes.Length)
      .Append(" layers=").Append(d.terrainLayers.Length).Append(" details=").Append(d.detailPrototypes.Length)
      .Append(" mat=").Append(terrain.materialTemplate == null ? "null" : terrain.materialTemplate.name + "<" + terrain.materialTemplate.shader.name + ">")
      .Append("\n");
}
sb.Append("RenderSettings.skybox=").Append(UnityEngine.RenderSettings.skybox == null ? "null" : UnityEngine.RenderSettings.skybox.name).Append("\n");
return sb.ToString();
