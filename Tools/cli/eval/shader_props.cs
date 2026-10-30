var sb = new System.Text.StringBuilder();
string root = "Assets/Samples/High Definition Render Pipeline/17.3.0/Particle System Shader Samples/";
foreach (var name in new[] { "Materials/UnlitSoft.mat", "Materials/UnlitDistortion.mat", "Materials/Unlit.mat" })
{
    var m = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(root + name);
    sb.Append(name).Append(" shader=").Append(m.shader.name).Append(" queue=").Append(m.renderQueue).Append(" kw=").Append(string.Join(",", m.shaderKeywords)).Append("\n  ");
    for (int i = 0; i < m.shader.GetPropertyCount(); i++)
    {
        var pn = m.shader.GetPropertyName(i);
        if (pn.StartsWith("unity_") ) continue;
        var pt = m.shader.GetPropertyType(i);
        string v = pt == UnityEngine.Rendering.ShaderPropertyType.Texture ? (m.GetTexture(pn) == null ? "null" : m.GetTexture(pn).name)
                 : pt == UnityEngine.Rendering.ShaderPropertyType.Color ? m.GetColor(pn).ToString()
                 : pt == UnityEngine.Rendering.ShaderPropertyType.Vector ? m.GetVector(pn).ToString()
                 : m.GetFloat(pn).ToString();
        sb.Append(pn).Append("=").Append(v).Append("; ");
    }
    sb.Append("\n");
}
// URP source materials
foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:Material", new[] { "Assets/GliderLevel/Material" }))
{
    var p = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
    var yaml = System.IO.File.ReadAllText(p);
    var tex = System.Text.RegularExpressions.Regex.Matches(yaml, @"- (_\w+):\s*\n\s*m_Texture: \{fileID: (\d+)(?:, guid: (\w+))?");
    var texs = new System.Collections.Generic.List<string>();
    foreach (System.Text.RegularExpressions.Match t in tex) if (t.Groups[2].Value != "0") texs.Add(t.Groups[1].Value + "=" + UnityEditor.AssetDatabase.GUIDToAssetPath(t.Groups[3].Value));
    var cols = System.Text.RegularExpressions.Regex.Matches(yaml, @"- (_BaseColor|_Color|_EmissionColor): \{r: ([\d.]+), g: ([\d.]+), b: ([\d.]+), a: ([\d.]+)\}");
    var cl = new System.Collections.Generic.List<string>();
    foreach (System.Text.RegularExpressions.Match c in cols) cl.Add(c.Groups[1].Value + "=(" + c.Groups[2].Value + "," + c.Groups[3].Value + "," + c.Groups[4].Value + "," + c.Groups[5].Value + ")");
    var fl = System.Text.RegularExpressions.Regex.Matches(yaml, @"- (_Surface|_Blend|_Metallic|_Smoothness|_Glossiness|_ColorMode|_SoftParticlesEnabled|_DistortionStrength): ([\d.\-]+)");
    var fs = new System.Collections.Generic.List<string>();
    foreach (System.Text.RegularExpressions.Match f in fl) fs.Add(f.Groups[1].Value + "=" + f.Groups[2].Value);
    sb.Append(System.IO.Path.GetFileName(p)).Append(": ").Append(string.Join(" ", texs)).Append(" | ").Append(string.Join(" ", cl)).Append(" | ").Append(string.Join(" ", fs)).Append("\n");
}
return sb.ToString();
