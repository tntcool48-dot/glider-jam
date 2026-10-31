var sb = new System.Text.StringBuilder();
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
sb.Append(scene.path).Append(" dirty=").Append(scene.isDirty).Append(" roots: ");
foreach (var r in scene.GetRootGameObjects()) sb.Append(r.name).Append(r.activeSelf ? "" : "(off)").Append(r.hideFlags != 0 ? "[" + r.hideFlags + "]" : "").Append(", ");
var vol = UnityEngine.GameObject.Find("/Sky and Fog Volume");
if (vol != null) { var v = vol.GetComponent<UnityEngine.Rendering.Volume>(); sb.Append(" | volume enabled=").Append(v.enabled).Append(" profile=").Append(v.sharedProfile ? v.sharedProfile.name : "null").Append(" weight=").Append(v.weight); }
foreach (var g in UnityEngine.Object.FindObjectsByType<UnityEngine.Transform>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None))
    if (g.parent == null && g.name.StartsWith("Glider")) sb.Append(" | stray: ").Append(g.name).Append(" ").Append(g.position);
return sb.ToString();
