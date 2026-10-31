var sb = new System.Text.StringBuilder();
var want = new[] { "SSAO", "SSR", "SSGI", "ContactShadows", "Volumetrics", "VolumetricClouds", "MotionBlur", "DepthOfField", "Water", "RayTracing", "FilmGrain", "ChromaticAberration", "LensFlareDataDriven", "Bloom", "Tonemapping", "TransparentSSR", "ScreenSpaceShadows" };
var names = System.Enum.GetNames(typeof(UnityEngine.Rendering.HighDefinition.FrameSettingsField));
foreach (var w in want) sb.Append(w).Append(System.Array.IndexOf(names, w) >= 0 ? "+ " : "- ");
sb.Append("\nRayCastingMode: ").Append(string.Join(",", System.Enum.GetNames(typeof(UnityEngine.Rendering.HighDefinition.RayCastingMode))));
var drh = typeof(UnityEngine.Rendering.DynamicResolutionHandler);
foreach (var m in drh.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)) if (m.Name.StartsWith("Set")) sb.Append("\nDRH.").Append(m.Name).Append("(").Append(string.Join(",", System.Array.ConvertAll(m.GetParameters(), p => p.ParameterType.Name))).Append(")");
sb.Append("\nUpscaleFilters: ").Append(string.Join(",", System.Enum.GetNames(typeof(UnityEngine.Rendering.DynamicResUpscaleFilter))));
foreach (var t in new[] { typeof(UnityEngine.Rendering.HighDefinition.ScreenSpaceReflection), typeof(UnityEngine.Rendering.HighDefinition.GlobalIllumination), typeof(UnityEngine.Rendering.HighDefinition.ScreenSpaceAmbientOcclusion), typeof(UnityEngine.Rendering.HighDefinition.Fog) })
{
    sb.Append("\n").Append(t.Name).Append(": ");
    foreach (var f in t.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)) sb.Append(f.Name).Append(" ");
    foreach (var p in t.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)) if (p.CanWrite) sb.Append("P:").Append(p.Name).Append(" ");
}
var hd = typeof(UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData);
foreach (var n in new[] { "customRenderingSettings", "renderingPathCustomFrameSettings", "renderingPathCustomFrameSettingsOverrideMask", "allowDynamicResolution", "TAAQuality", "taaQualityLevel", "volumeLayerMask" })
    sb.Append("\nHDCam.").Append(n).Append(hd.GetField(n) != null || hd.GetProperty(n) != null ? " ok" : " MISSING");
var ws = System.Type.GetType("UnityEngine.Rendering.HighDefinition.WaterSurface, Unity.RenderPipelines.HighDefinition.Runtime");
sb.Append("\nWaterSurface: ").Append(ws != null);
if (ws != null) foreach (var f in ws.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)) if (f.Name.Contains("surfaceType") || f.Name.Contains("geometryType") || f.Name.Contains("Color") || f.Name.Contains("repetition") || f.Name.Contains("largeWind") || f.Name.Contains("ripple")) sb.Append(f.Name).Append(":").Append(f.FieldType.Name).Append(" ");
return sb.ToString();
