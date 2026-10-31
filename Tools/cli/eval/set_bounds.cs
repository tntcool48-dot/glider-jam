// Set the free-route course side limits on the open scene's RaceRoute and save the scene.
var r = UnityEngine.Object.FindFirstObjectByType<GliderJam.Race.RaceRoute>();
r.minX = /*MINX*/;
r.maxX = /*MAXX*/;
UnityEditor.EditorUtility.SetDirty(r);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(r.gameObject.scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(r.gameObject.scene);
return r.gameObject.scene.name + ": x " + r.minX + ".." + r.maxX + " startZ=" + r.StartZ.ToString("F0") + " finishZ=" + r.FinishZ.ToString("F0") + " start=" + r.path[0].ToString("F0") + " finish=" + r.path[r.checkpoints[r.checkpoints.Length - 1]].ToString("F0");
