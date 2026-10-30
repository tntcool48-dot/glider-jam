var m = typeof(UnityEditor.AssetDatabase).GetMethod("ImportPackageImmediately",
    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public,
    null, new System.Type[] { typeof(string) }, null);
if (m == null) return "ImportPackageImmediately not found";
m.Invoke(null, new object[] { @"F:\drg201 game\yazan_glider_level.unitypackage" });
return "imported: " + UnityEditor.AssetDatabase.IsValidFolder("Assets/GliderLevel");
