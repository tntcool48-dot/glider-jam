// Play mode: switch race mode (Solo / SplitScreen) and graphics preset live, e.g. for ray tracing comparisons.
GliderJam.GameSettings.Mode = GliderJam.GameMode./*MODE*/;
var c = GliderJam.GameConfig.Current;
c.ApplyPreset(GliderJam.QualityPreset./*PRESET*/);
c.Commit();
return "mode=" + GliderJam.GameSettings.Mode + " preset=" + c.preset + " level=" + UnityEngine.QualitySettings.names[UnityEngine.QualitySettings.GetQualityLevel()] + " rt=" + GliderJam.QualityManager.RayTracingAllowed;
