// Crash player /*P*/ (index) right where it is, to test the explosion / respawn.
foreach (var g in UnityEngine.Object.FindObjectsByType<GliderJam.Flight.GliderController>(UnityEngine.FindObjectsSortMode.None))
    if (g.PlayerIndex == /*P*/) { g.Crash(g.transform.position + g.transform.forward * 4f); return "crashed " + g.name + " at " + g.transform.position; }
return "no glider";
