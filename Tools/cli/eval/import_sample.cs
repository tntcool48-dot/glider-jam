// Import one package sample by package + display name (edit the two strings).
string pkg = "/*PKG*/", name = "/*NAME*/";
foreach (var s in UnityEditor.PackageManager.UI.Sample.FindByPackage(pkg, null))
    if (s.displayName == name)
    {
        bool ok = s.Import(UnityEditor.PackageManager.UI.Sample.ImportOptions.HideImportWindow | UnityEditor.PackageManager.UI.Sample.ImportOptions.OverridePreviousImports);
        return name + " -> " + ok + " at " + s.importPath;
    }
return "sample not found: " + name;
