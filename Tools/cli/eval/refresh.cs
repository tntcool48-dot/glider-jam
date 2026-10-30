UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport);
return "refreshed: " + UnityEditor.AssetDatabase.FindAssets("", new[] { "Assets/GliderLevel" }).Length + " assets under GliderLevel";
