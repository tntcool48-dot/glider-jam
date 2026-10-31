// Play mode: race progress per glider (checkpoint HUD text is the source of truth for players; this reads controller state).
var sb = new System.Text.StringBuilder("scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + " t=" + UnityEngine.Time.time.ToString("F1"));
foreach (var g in UnityEngine.Object.FindObjectsByType<GliderJam.Flight.GliderController>(UnityEngine.FindObjectsSortMode.None))
    sb.Append(" | P").Append(g.PlayerIndex + 1).Append(" pos=").Append(g.transform.position.ToString("F0")).Append(g.Crashed ? " CRASHED" : "").Append(g.Ghost > 0f ? " ghost" : "");
return sb.ToString();
