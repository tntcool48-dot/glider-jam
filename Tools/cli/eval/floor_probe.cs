// Canyon: terrain height statistics over the corridor and under the race line (to place water).
var terrains = UnityEngine.Terrain.activeTerrains;
System.Func<float, float, float> H = (x, z) => {
    foreach (var t in terrains) { var p = t.transform.position; var s = t.terrainData.size; if (x >= p.x && x <= p.x + s.x && z >= p.z && z <= p.z + s.z) return t.SampleHeight(new UnityEngine.Vector3(x, 0f, z)) + p.y; }
    return float.NaN; };
float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
foreach (var t in terrains) { var p = t.transform.position; var s = t.terrainData.size; minX = UnityEngine.Mathf.Min(minX, p.x); maxX = UnityEngine.Mathf.Max(maxX, p.x + s.x); minZ = UnityEngine.Mathf.Min(minZ, p.z); maxZ = UnityEngine.Mathf.Max(maxZ, p.z + s.z); }
var all = new System.Collections.Generic.List<float>();
for (float x = minX; x < maxX; x += 20f) for (float z = minZ; z < maxZ; z += 20f) { float h = H(x, z); if (!float.IsNaN(h)) all.Add(h); }
all.Sort();
var route = UnityEngine.Object.FindFirstObjectByType<GliderJam.Race.RaceRoute>();
var under = new System.Collections.Generic.List<float>();
foreach (var p in route.path) { float h = H(p.x, p.z); if (!float.IsNaN(h)) under.Add(h); }
var u2 = new System.Collections.Generic.List<float>(under); u2.Sort();
var sb = new System.Text.StringBuilder();
sb.Append("terrains=").Append(terrains.Length).Append(" bounds x[").Append(minX).Append("..").Append(maxX).Append("] z[").Append(minZ).Append("..").Append(maxZ).Append("]");
sb.Append(" | corridor h pct 1/5/10/25/50: ");
foreach (float q in new[] { 0.01f, 0.05f, 0.1f, 0.25f, 0.5f }) sb.Append(all[(int)(q * (all.Count - 1))].ToString("F0")).Append(" ");
sb.Append(" | under route min/10/50/90: ").Append(u2[0].ToString("F0")).Append(" ").Append(u2[(int)(0.1f * (u2.Count - 1))].ToString("F0")).Append(" ").Append(u2[u2.Count / 2].ToString("F0")).Append(" ").Append(u2[(int)(0.9f * (u2.Count - 1))].ToString("F0"));
sb.Append(" | route y min/max: ");
float rmin = float.MaxValue, rmax = float.MinValue; foreach (var p in route.path) { rmin = UnityEngine.Mathf.Min(rmin, p.y); rmax = UnityEngine.Mathf.Max(rmax, p.y); }
sb.Append(rmin.ToString("F0")).Append(" ").Append(rmax.ToString("F0"));
sb.Append(" | under route every 25: ");
for (int i = 0; i < under.Count; i += 25) sb.Append(under[i].ToString("F0")).Append(" ");
return sb.ToString();
