var sb = new System.Text.StringBuilder();
foreach (var path in new[] { "Assets/GliderJam/Art/FX/FX_Vapor.mat", "Assets/GliderJam/Art/FX/FX_Smoke.mat" })
{
    var m = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
    sb.Append(path).Append(" shader=").Append(m.shader.name).Append(": ");
    for (int i = 0; i < m.shader.GetPropertyCount(); i++)
    {
        var t = m.shader.GetPropertyType(i);
        string n = m.shader.GetPropertyName(i);
        if (n.StartsWith("_") && (n.Contains("Stencil") || n.Contains("ZTest") || n.Contains("Cull") || n.Contains("Blend") && n != "_BlendMode")) continue;
        sb.Append(n).Append(":").Append(t).Append(" ");
    }
    sb.Append(" || ");
}
sb.Append(" LightUnitOnLight=").Append(typeof(UnityEngine.Light).GetProperty("lightUnit") != null);
sb.Append(" SkyType.Gradient=").Append((int)UnityEngine.Rendering.HighDefinition.SkyType.Gradient);
var gs = typeof(UnityEngine.Rendering.HighDefinition.GradientSky);
foreach (var f in gs.GetFields()) sb.Append(" gs.").Append(f.Name);
foreach (var f in typeof(UnityEngine.Rendering.HighDefinition.SkySettings).GetFields()) sb.Append(" sky.").Append(f.Name);
foreach (var f in typeof(UnityEngine.Rendering.HighDefinition.Exposure).GetFields()) sb.Append(" exp.").Append(f.Name);
return sb.ToString();
