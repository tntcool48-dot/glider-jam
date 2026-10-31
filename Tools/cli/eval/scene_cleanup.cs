// Disable the original scene's player / camera / HUD / portal effects in the saved scene (RaceManager replaces them).
var sb = new System.Text.StringBuilder();
foreach (var path in new[] { "/player", "/Main Camera", "/[UI]", "/portal/Portal yellow", "/portal/Portal green", "/portal/Portal red", "/portal/Portal blue", "/portal/Magic circle" })
{
    var go = UnityEngine.GameObject.Find(path);
    if (go != null) { go.SetActive(false); sb.Append("off: ").Append(path).Append(" "); }
}
var prof = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>("Assets/GliderJam/Settings/GoldenHour.asset");
prof.TryGet(out UnityEngine.Rendering.HighDefinition.VolumetricClouds clouds);
prof.TryGet(out UnityEngine.Rendering.HighDefinition.Fog fog);
sb.Append("| clouds.enable=").Append(clouds.enable.value).Append(" active=").Append(clouds.active).Append(" fog.enabled=").Append(fog.enabled.value).Append(" volFog=").Append(fog.enableVolumetricFog.value);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
return sb.ToString();
