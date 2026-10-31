// Render a top-down height map of all terrain tiles (x 0..1000, z 0..7000) to Screenshots/map_topdown.png.
var terrains = UnityEngine.Object.FindObjectsByType<UnityEngine.Terrain>(UnityEngine.FindObjectsSortMode.None);
int W = 200, H = 1400; // 5 m per pixel
var tex = new UnityEngine.Texture2D(W, H, UnityEngine.TextureFormat.RGB24, false);
float maxH = 0f;
var hs = new float[W, H];
for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
{
    var wp = new UnityEngine.Vector3(x * 5f + 2.5f, 0, y * 5f + 2.5f);
    float h = -1f;
    foreach (var t in terrains)
    {
        var p = t.transform.position; var s = t.terrainData.size;
        if (wp.x >= p.x && wp.x <= p.x + s.x && wp.z >= p.z && wp.z <= p.z + s.z) { h = t.SampleHeight(wp) + p.y; break; }
    }
    hs[x, y] = h; if (h > maxH) maxH = h;
}
for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
{
    float h = hs[x, y];
    UnityEngine.Color c;
    if (h < 0) c = UnityEngine.Color.magenta;
    else if (h > 295f) c = UnityEngine.Color.Lerp(new UnityEngine.Color(0.55f, 0.1f, 0.1f), UnityEngine.Color.white, (h - 295f) / 305f); // above ceiling = wall
    else c = UnityEngine.Color.Lerp(new UnityEngine.Color(0.05f, 0.1f, 0.35f), new UnityEngine.Color(0.3f, 0.8f, 0.3f), h / 295f); // flyable band
    tex.SetPixel(x, y, c);
}
// start (342,213) and finish (596,6766)
for (int d = -4; d <= 4; d++) for (int e = -4; e <= 4; e++) { tex.SetPixel(68 + d, 42 + e, UnityEngine.Color.yellow); tex.SetPixel(119 + d, 1353 + e, UnityEngine.Color.cyan); }
tex.Apply();
System.IO.Directory.CreateDirectory("Screenshots");
System.IO.File.WriteAllBytes("Screenshots/map_topdown.png", UnityEngine.ImageConversion.EncodeToPNG(tex));
return "maxH=" + maxH;
