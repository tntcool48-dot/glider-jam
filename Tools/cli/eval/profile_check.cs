var prof = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>("Assets/GliderJam/Settings/GoldenHour.asset");
if (prof == null) return "profile missing; volume uses: " + UnityEngine.GameObject.Find("/Sky and Fog Volume").GetComponent<UnityEngine.Rendering.Volume>().sharedProfile;
var sb = new System.Text.StringBuilder("components: ");
foreach (var c in prof.components) sb.Append(c == null ? "NULL" : c.GetType().Name + (c.active ? "" : "(off)")).Append(", ");
return sb.ToString();
