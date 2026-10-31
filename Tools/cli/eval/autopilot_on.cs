// Play mode: hand every racing glider to the autopilot (removes the player drivers).
var route = UnityEngine.Object.FindFirstObjectByType<GliderJam.Race.RaceRoute>();
int n = 0;
foreach (var g in UnityEngine.Object.FindObjectsByType<GliderJam.Flight.GliderController>(UnityEngine.FindObjectsSortMode.None))
{
    var pd = g.GetComponent<GliderJam.Flight.PlayerDriver>();
    if (pd != null) UnityEngine.Object.Destroy(pd);
    if (g.GetComponent<GliderJam.Flight.AutopilotDriver>() != null) continue;
    var ap = g.gameObject.AddComponent<GliderJam.Flight.AutopilotDriver>();
    ap.route = route;
    ap.lateralOffset = g.PlayerIndex == 0 ? -10f : 10f;
    n++;
}
return "autopilot on " + n + " gliders";
