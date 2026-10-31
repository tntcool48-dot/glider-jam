// Play mode: tweak the live ray tracing volume (QualityManager's runtime profile) for A/B renders.
var qm = UnityEngine.Object.FindFirstObjectByType<GliderJam.QualityManager>();
var prof = (UnityEngine.Rendering.VolumeProfile)typeof(GliderJam.QualityManager).GetField("_profile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(qm);
prof.TryGet<UnityEngine.Rendering.HighDefinition.ScreenSpaceReflection>(out var ssr);
prof.TryGet<UnityEngine.Rendering.HighDefinition.ScreenSpaceAmbientOcclusion>(out var ao);
prof.TryGet<UnityEngine.Rendering.HighDefinition.GlobalIllumination>(out var gi);
ssr.mode.Override(UnityEngine.Rendering.HighDefinition.RayTracingMode./*MODE*/);
ssr.bounceCount.Override(/*BOUNCES*/);
var clamp = typeof(UnityEngine.Rendering.HighDefinition.ScreenSpaceReflection).GetField("m_ClampValue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(ssr) as UnityEngine.Rendering.VolumeParameter<float>;
clamp.Override(/*CLAMP*/);
ao.rayTracing.Override(/*RTAO*/);
gi.tracing.Override(/*RTGI*/ ? UnityEngine.Rendering.HighDefinition.RayCastingMode.RayTracing : UnityEngine.Rendering.HighDefinition.RayCastingMode.RayMarching);
return "ssr mode=" + ssr.mode.value + " bounces=" + ssr.bounceCount.value + " clamp=" + clamp.value + " rtao=" + ao.rayTracing.value + " gi=" + gi.tracing.value;
