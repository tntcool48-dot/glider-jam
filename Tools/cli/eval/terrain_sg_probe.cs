var sb = new System.Text.StringBuilder();
string dir = "Assets/Samples/Shader Graph/17.3.0/Terrain Shaders/Shaders/";
foreach (var f in new[] { "CSNOH Auto Material Hex", "CSNOH Auto Material Simple", "CSNOH Hex Height" })
{
    var sh = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Shader>(dir + f + ".shadergraph");
    if (sh == null) { sb.Append(f).Append(": not loaded\n"); continue; }
    sb.Append(f).Append(" => ").Append(sh.name).Append(" supported=").Append(sh.isSupported).Append(" passes=").Append(sh.passCount).Append("\n  ");
    for (int i = 0; i < sh.GetPropertyCount(); i++)
    {
        var n = sh.GetPropertyName(i);
        if (n.StartsWith("_Stencil") || n.StartsWith("_Z") || n.StartsWith("_Cull") || n.StartsWith("_Src") || n.StartsWith("_Dst") || n.StartsWith("_Alpha") || n.StartsWith("_Transparent") || n.StartsWith("_Double") || n.StartsWith("_Blend") || n.StartsWith("_Surface") || n.StartsWith("_Render") || n.StartsWith("_Depth") || n.StartsWith("_Conservative") || n.StartsWith("_Use") || n.StartsWith("_Add") || n.StartsWith("_Enable") || n.StartsWith("_Receive") || n.StartsWith("_Exclude") || n.StartsWith("_Opaque") || n.StartsWith("_Per")) continue;
        sb.Append(n).Append(":").Append(sh.GetPropertyType(i)).Append("[").Append(sh.GetPropertyDescription(i)).Append("] ");
    }
    sb.Append("\n");
}
// what does the HDRP sample scene's terrain use?
var txt = System.IO.File.ReadAllText("Assets/Samples/Shader Graph/17.3.0/Terrain Shaders/Scenes/TerrainHDRP.unity");
var m = System.Text.RegularExpressions.Regex.Matches(txt, @"m_MaterialTemplate: \{fileID: \d+, guid: (\w+)");
foreach (System.Text.RegularExpressions.Match mm in m) sb.Append("HDRP scene terrain material: ").Append(UnityEditor.AssetDatabase.GUIDToAssetPath(mm.Groups[1].Value)).Append("\n");
return sb.ToString();
