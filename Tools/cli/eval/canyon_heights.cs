// Canyon corridor: share of the area whose terrain rises above various heights (how much you can fly over).
var route = UnityEngine.Object.FindFirstObjectByType<GliderJam.Race.RaceRoute>();
var terrains = UnityEngine.Terrain.activeTerrains;
int total = 0; var above = new int[6]; float[] lv = { 150f, 180f, 200f, 220f, 250f, 288f };
for (float x = 15f; x < 985f; x += 15f)
for (float z = route.StartZ; z < route.FinishZ; z += 15f)
{
    float h = float.NaN;
    foreach (var t in terrains) { var o = t.transform.position; var s = t.terrainData.size; if (x >= o.x && x <= o.x + s.x && z >= o.z && z <= o.z + s.z) h = o.y + t.SampleHeight(new UnityEngine.Vector3(x, 0f, z)); }
    if (float.IsNaN(h)) continue;
    total++;
    for (int k = 0; k < lv.Length; k++) if (h > lv[k]) above[k]++;
}
var sb = new System.Text.StringBuilder("corridor cells=" + total + " | share above: ");
for (int k = 0; k < lv.Length; k++) sb.Append(lv[k]).Append("m=").Append((100f * above[k] / total).ToString("F0")).Append("% ");
float ymin = 1e9f, ymax = -1e9f; foreach (var p in route.path) { ymin = UnityEngine.Mathf.Min(ymin, p.y); ymax = UnityEngine.Mathf.Max(ymax, p.y); }
sb.Append("| race line y ").Append(ymin.ToString("F0")).Append("..").Append(ymax.ToString("F0")).Append(" ceiling ").Append(route.ceiling);
return sb.ToString();
