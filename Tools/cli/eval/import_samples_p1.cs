var sb = new System.Text.StringBuilder();
foreach (var s in UnityEditor.PackageManager.UI.Sample.FindByPackage("com.unity.render-pipelines.high-definition", null))
{
    if (s.displayName != "Particle System Shader Samples") continue;
    bool ok = s.Import(UnityEditor.PackageManager.UI.Sample.ImportOptions.HideImportWindow | UnityEditor.PackageManager.UI.Sample.ImportOptions.OverridePreviousImports);
    sb.Append(s.displayName).Append(" -> ").Append(ok).Append(" at ").Append(s.importPath).Append("\n");
}
foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
{
    System.Type[] types;
    try { types = asm.GetTypes(); } catch { continue; }
    foreach (var t in types) if (t.Name == "TMP_PackageResourceImporter" || t.Name == "TMP_PackageResourceImporterWindow") sb.Append("TMP: ").Append(t.AssemblyQualifiedName).Append("\n");
}
return sb.ToString();
