var sb = new System.Text.StringBuilder();
foreach (var f in typeof(UnityEngine.Rendering.HighDefinition.VolumetricClouds).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
    sb.Append(f.Name).Append(" ");
foreach (var p in typeof(UnityEngine.Rendering.HighDefinition.VolumetricClouds).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
    if (p.Name.ToLower().Contains("preset")) sb.Append(" PROP:").Append(p.Name).Append(":").Append(p.PropertyType.Name);
return sb.ToString();
