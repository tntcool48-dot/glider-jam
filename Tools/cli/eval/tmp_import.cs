UnityEditor.AssetDatabase.importPackageCompleted += n => UnityEngine.Debug.Log("[pkg] completed " + n);
UnityEditor.AssetDatabase.importPackageFailed += (n, e) => UnityEngine.Debug.LogError("[pkg] failed " + n + ": " + e);
var t = System.Type.GetType("TMPro.TMP_PackageResourceImporter, Unity.TextMeshPro");
t.GetMethod("ImportResources", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
 .Invoke(null, new object[] { true, false, false });
return "TMP essentials import requested; TMP Settings now: " + UnityEditor.AssetDatabase.FindAssets("t:TMP_Settings").Length;
