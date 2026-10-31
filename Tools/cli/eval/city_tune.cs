// Live tuning of the night city look (play mode): window glow, moon, sky, exposure, rain.
float windows = /*WIN*/, moonLux = /*MOON*/, skyMul = /*SKY*/, ev = /*EV*/;
foreach (var n in new[] { "Glass", "Concrete", "Office" })
{
    var m = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/GliderJam/Art/City/City_Facade_" + n + ".mat");
    m.SetColor("_EmissiveColor", UnityEngine.Color.white * windows);
}
var moon = UnityEngine.GameObject.Find("Moon").GetComponent<UnityEngine.Light>();
moon.intensity = moonLux;
var vol = UnityEngine.GameObject.Find("Night Volume").GetComponent<UnityEngine.Rendering.Volume>();
vol.sharedProfile.TryGet<UnityEngine.Rendering.HighDefinition.GradientSky>(out var sky);
sky.multiplier.value = skyMul;
vol.sharedProfile.TryGet<UnityEngine.Rendering.HighDefinition.Exposure>(out var exp);
exp.fixedExposure.value = ev;
int rain = 0;
foreach (var ps in UnityEngine.Object.FindObjectsByType<UnityEngine.ParticleSystem>(UnityEngine.FindObjectsSortMode.None))
{
    if (ps.name != "Rain") continue;
    var main = ps.main; main.startSize = new UnityEngine.ParticleSystem.MinMaxCurve(0.06f, 0.1f);
    rain += ps.particleCount;
}
return "tuned; rain particles alive=" + rain;
