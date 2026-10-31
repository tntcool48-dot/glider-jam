var m = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/GliderJam/Art/Glider/Paint_Wing.mat");
var mesh = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>("Assets/GliderJam/Art/Glider/Meshes/WingR.asset");
var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/GliderJam/Art/Glider/Glider_P1.prefab");
var sb = new System.Text.StringBuilder();
sb.Append("mat base=").Append(m.GetTexture("_BaseColorMap")).Append(" normal=").Append(m.GetTexture("_NormalMap")).Append(" kw=").Append(string.Join(",", m.shaderKeywords));
var uv = mesh.uv;
float umin = 9, umax = -9, vmin = 9, vmax = -9;
foreach (var p in uv) { umin = UnityEngine.Mathf.Min(umin, p.x); umax = UnityEngine.Mathf.Max(umax, p.x); vmin = UnityEngine.Mathf.Min(vmin, p.y); vmax = UnityEngine.Mathf.Max(vmax, p.y); }
sb.Append(" | mesh verts=").Append(mesh.vertexCount).Append(" uv u[").Append(umin).Append("..").Append(umax).Append("] v[").Append(vmin).Append("..").Append(vmax).Append("]");
foreach (var r in prefab.GetComponentsInChildren<UnityEngine.MeshRenderer>()) if (r.name.StartsWith("Wing") || r.name.StartsWith("Aileron")) sb.Append(" | ").Append(r.name).Append("=").Append(r.sharedMaterial.name);
return sb.ToString();
