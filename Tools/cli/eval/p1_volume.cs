var sb = new System.Text.StringBuilder();
string src = "Assets/Settings/SkyandFogSettingsProfile.asset";
string dst = "Assets/GliderLevel/Settings/YazanSkyAndFog.asset";
if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/GliderLevel/Settings")) UnityEditor.AssetDatabase.CreateFolder("Assets/GliderLevel", "Settings");
if (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(dst) == null) UnityEditor.AssetDatabase.CopyAsset(src, dst);
var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(dst);
var go = UnityEngine.GameObject.Find("/Sky and Fog Volume");
if (go == null) go = new UnityEngine.GameObject("Sky and Fog Volume");
var vol = go.GetComponent<UnityEngine.Rendering.Volume>();
if (vol == null) vol = go.AddComponent<UnityEngine.Rendering.Volume>();
vol.isGlobal = true;
vol.priority = 0;
vol.sharedProfile = profile;
foreach (var c in profile.components)
{
    sb.Append(c.GetType().Name).Append(": ");
    var fields = c.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
    foreach (var f in fields)
    {
        var p = f.GetValue(c) as UnityEngine.Rendering.VolumeParameter;
        if (p == null || !p.overrideState) continue;
        sb.Append(f.Name).Append("=").Append(p.GetType().GetProperty("value").GetValue(p)).Append(" ");
    }
    sb.Append("\n");
}
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
return sb.ToString();
