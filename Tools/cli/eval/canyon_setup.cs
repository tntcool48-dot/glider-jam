// Edit mode: open the canyon level, re-run the canyon pass (ambience, spires, ivy, extra trees, clouds) and save.
var s = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/GliderLevel/Scenes/yazan scene.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
string r = GliderJam.EditorTools.CanyonSetup.Run();
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s);
UnityEditor.AssetDatabase.SaveAssets();
return r;
