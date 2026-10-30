var sb = new System.Text.StringBuilder();
foreach (var pkg in new[] { "com.unity.render-pipelines.high-definition", "com.unity.visualeffectgraph", "com.unity.shadergraph", "com.unity.ugui" })
{
    foreach (var s in UnityEditor.PackageManager.UI.Sample.FindByPackage(pkg, null))
        sb.Append(pkg).Append(" | ").Append(s.displayName).Append(" | imported=").Append(s.isImported).Append("\n");
}
var t = System.Type.GetType("TMPro.TMP_PackageResourceImporter, Unity.TextMeshPro.Editor");
sb.Append("TMP importer type: ").Append(t == null ? "not found" : t.FullName).Append("\n");
if (t != null) foreach (var m in t.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)) sb.Append("  ").Append(m.Name).Append("(").Append(string.Join(",", System.Array.ConvertAll(m.GetParameters(), p => p.ParameterType.Name))).Append(")\n");
sb.Append("TMP settings exists: ").Append(UnityEditor.AssetDatabase.FindAssets("t:TMP_Settings").Length).Append("\n");
sb.Append("shaders found: TMP SDF=").Append(UnityEngine.Shader.Find("TextMeshPro/Distance Field") != null)
  .Append(" HDRP/Lit=").Append(UnityEngine.Shader.Find("HDRP/Lit") != null)
  .Append(" HDRP/TerrainLit=").Append(UnityEngine.Shader.Find("HDRP/TerrainLit") != null)
  .Append(" HDRP/Unlit=").Append(UnityEngine.Shader.Find("HDRP/Unlit") != null).Append("\n");
return sb.ToString();
