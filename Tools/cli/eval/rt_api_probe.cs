// List ray tracing related members of the SSR / GI / AO volume components and the HDRP asset's RT quality arrays.
var sb = new System.Text.StringBuilder();
foreach (var t in new[] { typeof(UnityEngine.Rendering.HighDefinition.ScreenSpaceReflection), typeof(UnityEngine.Rendering.HighDefinition.GlobalIllumination), typeof(UnityEngine.Rendering.HighDefinition.ScreenSpaceAmbientOcclusion) })
{
    sb.Append(t.Name).Append(" fields: ");
    foreach (var f in t.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)) sb.Append(f.Name).Append(":").Append(f.FieldType.Name).Append(" ");
    sb.Append(" props: ");
    foreach (var p in t.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly)) sb.Append(p.Name).Append(p.CanWrite ? "(rw) " : " ");
    sb.Append(" || ");
}
var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.RenderPipelineAsset>("Assets/GliderJam/Settings/HDRP Ultra RT.asset");
var so = new UnityEditor.SerializedObject(asset);
var q = so.FindProperty("m_RenderPipelineSettings.lightingQualitySettings");
if (q != null)
{
    var it = q.Copy(); var end = q.GetEndProperty(); it.NextVisible(true);
    sb.Append(" lightingQuality: ");
    while (!UnityEditor.SerializedProperty.EqualContents(it, end)) { if (it.name.StartsWith("RTR") || it.name.StartsWith("RTGI") || it.name.StartsWith("RTAO")) sb.Append(it.name).Append(it.isArray ? "[" + it.arraySize + "]" : "").Append(" "); if (!it.NextVisible(false)) break; }
}
return sb.ToString();
