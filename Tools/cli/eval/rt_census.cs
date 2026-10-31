// Which renderers would enter the ray tracing acceleration structure, grouped by root object (count, triangles, materials).
var groups = new System.Collections.Generic.Dictionary<string, long[]>();
var mats = new System.Collections.Generic.HashSet<UnityEngine.Material>();
int lodNonZero = 0;
foreach (var r in UnityEngine.Object.FindObjectsByType<UnityEngine.Renderer>(UnityEngine.FindObjectsSortMode.None))
{
    if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
    if (r.rayTracingMode == UnityEngine.Experimental.Rendering.RayTracingMode.Off) continue;
    if (r is UnityEngine.ParticleSystemRenderer || r is UnityEngine.TrailRenderer || r is UnityEngine.LineRenderer) continue;
    var mf = r.GetComponent<UnityEngine.MeshFilter>();
    long tris = 0;
    if (mf != null && mf.sharedMesh != null) for (int s = 0; s < mf.sharedMesh.subMeshCount; s++) tris += (long)mf.sharedMesh.GetIndexCount(s) / 3;
    var lg = r.GetComponentInParent<UnityEngine.LODGroup>();
    if (lg != null && !r.name.Contains("LOD0") && !r.transform.parent.name.Contains("LOD0")) lodNonZero++;
    string root = r.transform.root.name;
    if (!groups.TryGetValue(root, out var g)) groups[root] = g = new long[2];
    g[0]++; g[1] += tris;
    foreach (var m in r.sharedMaterials) if (m != null) mats.Add(m);
}
var sb = new System.Text.StringBuilder();
foreach (var kv in groups) sb.Append(kv.Key).Append(": ").Append(kv.Value[0]).Append(" renderers, ").Append(kv.Value[1] / 1000).Append("k tris | ");
var shaders = new System.Collections.Generic.HashSet<string>();
foreach (var m in mats) shaders.Add(m.shader.name);
sb.Append(" materials=").Append(mats.Count).Append(" shaders=").Append(string.Join(",", shaders)).Append(" nonLOD0InLodGroups=").Append(lodNonZero);
return sb.ToString();
