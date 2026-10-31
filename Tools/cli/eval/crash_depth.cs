// Play mode: P1 crash state and how far below the terrain surface its position is (0 = at the surface).
GliderJam.Flight.GliderController g = null;
foreach (var c in UnityEngine.Object.FindObjectsByType<GliderJam.Flight.GliderController>(UnityEngine.FindObjectsSortMode.None)) if (c.PlayerIndex == 0) g = c;
var p = g.transform.position;
float h = float.NaN;
foreach (var t in UnityEngine.Terrain.activeTerrains) { var o = t.transform.position; var s = t.terrainData.size; if (p.x >= o.x && p.x <= o.x + s.x && p.z >= o.z && p.z <= o.z + s.z) h = o.y + t.SampleHeight(p); }
return "P1 " + (g.Crashed ? "CRASHED" : "flying") + " at " + p.ToString("F1") + " terrain=" + h.ToString("F1") + " depthBelowSurface=" + (h - p.y).ToString("F1");
