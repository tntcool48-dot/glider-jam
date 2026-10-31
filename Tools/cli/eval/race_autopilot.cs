// Test helper: toggle autopilot for both players on the scene's RaceManager (edit mode, not saved).
var rm = UnityEngine.Object.FindFirstObjectByType<GliderJam.Race.RaceManager>();
rm.autopilot = new bool[] { /*A*/, /*B*/ };
return "autopilot = " + rm.autopilot[0] + "," + rm.autopilot[1] + " mode=" + GliderJam.GameSettings.Mode;
