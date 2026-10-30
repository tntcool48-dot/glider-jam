UnityEditor.AssetDatabase.importPackageStarted += n => UnityEngine.Debug.Log("[pkg] started " + n);
UnityEditor.AssetDatabase.importPackageCompleted += n => UnityEngine.Debug.Log("[pkg] completed " + n);
UnityEditor.AssetDatabase.importPackageFailed += (n, e) => UnityEngine.Debug.LogError("[pkg] failed " + n + ": " + e);
UnityEditor.AssetDatabase.importPackageCancelled += n => UnityEngine.Debug.LogWarning("[pkg] cancelled " + n);
var path = @"F:\drg201 game\yazan_glider_level.unitypackage";
UnityEngine.Debug.Log("[pkg] exists=" + System.IO.File.Exists(path));
UnityEditor.AssetDatabase.ImportPackage(path, false);
return "queued";
