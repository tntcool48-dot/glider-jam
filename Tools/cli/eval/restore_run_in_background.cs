// The Pipeline's set_run_in_background override can get saved into ProjectSettings.asset; put the project value back.
var before = UnityEditor.PlayerSettings.runInBackground;
UnityEditor.PlayerSettings.runInBackground = false;
UnityEditor.AssetDatabase.SaveAssets();
return "runInBackground in memory before=" + before + " now=" + UnityEditor.PlayerSettings.runInBackground;
