var sb = new System.Text.StringBuilder();
foreach (var name in new[] { "WingL", "WingR" })
{
    var mesh = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>("Assets/GliderJam/Art/Glider/Meshes/" + name + ".asset");
    var v = mesh.vertices; var uv = mesh.uv;
    sb.Append(name).Append(" subMeshes=").Append(mesh.subMeshCount).Append(" uvChannels=").Append(mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0)).Append(": ");
    int shown = 0;
    for (int i = 0; i < v.Length && shown < 6; i += 7)
    {
        if (UnityEngine.Mathf.Abs(v[i].x) > 0.8f && UnityEngine.Mathf.Abs(v[i].x) < 1.3f) { sb.Append(v[i].ToString("F2")).Append("->").Append(uv[i].ToString("F3")).Append(" "); shown++; }
    }
    sb.Append(" | ");
}
var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>("Assets/GliderJam/Art/Glider/Panels_Wing.png");
sb.Append("tex ").Append(tex.width).Append("x").Append(tex.height).Append(" fmt=").Append(tex.format);
var ti = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath("Assets/GliderJam/Art/Glider/Panels_Wing.png");
sb.Append(" type=").Append(ti.textureType).Append(" max=").Append(ti.maxTextureSize);
return sb.ToString();
