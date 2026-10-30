// Per tile and per layer slot: coverage, and mean height / slope where the slot dominates.
var sb = new System.Text.StringBuilder();
var terrains = UnityEngine.Object.FindObjectsByType<UnityEngine.Terrain>(UnityEngine.FindObjectsSortMode.None);
System.Array.Sort(terrains, (a, b) => a.transform.position.z.CompareTo(b.transform.position.z));
foreach (var t in terrains)
{
    var d = t.terrainData;
    int res = d.alphamapResolution, n = d.alphamapLayers;
    var a = d.GetAlphamaps(0, 0, res, res);
    var cov = new double[n]; var hSum = new double[n]; var sSum = new double[n]; var cnt = new int[n];
    float hmin = float.MaxValue, hmax = float.MinValue;
    for (int y = 0; y < res; y += 2) for (int x = 0; x < res; x += 2)
    {
        float u = (x + 0.5f) / res, v = (y + 0.5f) / res;
        float h = d.GetInterpolatedHeight(u, v); float s = d.GetSteepness(u, v);
        hmin = System.Math.Min(hmin, h); hmax = System.Math.Max(hmax, h);
        for (int l = 0; l < n; l++)
        {
            float w = a[y, x, l]; cov[l] += w;
            if (w > 0.5f) { hSum[l] += h; sSum[l] += s; cnt[l]++; }
        }
    }
    int samples = (res / 2) * (res / 2);
    sb.Append(t.name).Append(" h=[").Append(hmin.ToString("F0")).Append("..").Append(hmax.ToString("F0")).Append("]\n");
    for (int l = 0; l < n; l++)
        sb.Append("   slot").Append(l).Append(": cover=").Append((cov[l] / samples * 100).ToString("F1")).Append("%")
          .Append(" h=").Append(cnt[l] > 0 ? (hSum[l] / cnt[l]).ToString("F0") : "-")
          .Append(" slope=").Append(cnt[l] > 0 ? (sSum[l] / cnt[l]).ToString("F0") : "-").Append("\n");
    if (d.treeInstanceCount > 0)
    {
        var protoCount = new int[d.treePrototypes.Length];
        foreach (var ti in d.treeInstances) protoCount[ti.prototypeIndex]++;
        sb.Append("   trees per proto: ").Append(string.Join(",", protoCount)).Append(" sample scale=").Append(d.treeInstances[0].heightScale.ToString("F2")).Append("\n");
    }
}
return sb.ToString();
