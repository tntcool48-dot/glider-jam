// The original player, camera and HUD are replaced by the race system (RaceManager); remove them from the level.
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var removed = new System.Collections.Generic.List<string>();
foreach (var root in scene.GetRootGameObjects())
    if (root.name == "player" || root.name == "Main Camera" || root.name == "[UI]") { removed.Add(root.name); UnityEngine.Object.DestroyImmediate(root); }
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return scene.name + ": removed " + string.Join(", ", removed);
