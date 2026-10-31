var sb = new System.Text.StringBuilder();
sb.Append("scene=").Append(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name).Append(" t=").Append(UnityEngine.Time.time.ToString("F1")).Append(" fps=").Append((1f / UnityEngine.Time.smoothDeltaTime).ToString("F0"));
foreach (var g in UnityEngine.Object.FindObjectsByType<GliderJam.Flight.GliderController>(UnityEngine.FindObjectsSortMode.InstanceID))
    sb.Append(" | ").Append(g.name).Append(" pos=").Append(g.transform.position.ToString("F0")).Append(" v=").Append(g.Speed.ToString("F0")).Append(g.Crashed ? " CRASHED" : "").Append(g.Frozen ? " frozen" : "").Append(g.Ghost > 0 ? " ghost" : "");
var rm = UnityEngine.Object.FindFirstObjectByType<GliderJam.Race.RaceManager>();
if (rm != null) sb.Append(" | raceTime=").Append(rm.RaceTime.ToString("F1"));
return sb.ToString();
