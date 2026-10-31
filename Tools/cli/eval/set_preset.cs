var cfg = GliderJam.GameConfig.Current;
cfg.ApplyPreset(GliderJam.QualityPreset./*P*/);
cfg.Commit();
return "preset=" + cfg.preset + " quality=" + UnityEngine.QualitySettings.names[UnityEngine.QualitySettings.GetQualityLevel()] + " rt=" + cfg.UsesRayTracing + " rp=" + UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.name;
