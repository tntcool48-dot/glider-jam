var sh = UnityEngine.Shader.Find("HDRP/LayeredLit");
var sb = new System.Text.StringBuilder();
for (int i = 0; i < sh.GetPropertyCount(); i++)
{
    var n = sh.GetPropertyName(i);
    if (n.EndsWith("1") || n.EndsWith("2") || n.EndsWith("3")) continue; // per-layer duplicates
    if (n.StartsWith("_Stencil") || n.StartsWith("_Z") || n.StartsWith("unity_")) continue;
    sb.Append(n).Append(":").Append(sh.GetPropertyType(i).ToString().Substring(0, 3)).Append(" ");
}
return sb.ToString();
