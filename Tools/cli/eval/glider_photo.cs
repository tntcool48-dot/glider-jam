// Studio shots of a glider prefab placed in the sky over the level: Screenshots/NAME_a.png and _b.png
var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/GliderJam/Art/Glider/Glider_/*TAG*/.prefab");
var g = (UnityEngine.GameObject)UnityEngine.Object.Instantiate(prefab);
g.hideFlags = UnityEngine.HideFlags.DontSave;
var center = new UnityEngine.Vector3(600f, 190f, 1200f);
g.transform.position = center;
g.transform.rotation = UnityEngine.Quaternion.Euler(-4f, 20f, -12f);
var cams = new[] {
    new UnityEngine.Vector3(-9f, 3.2f, 8.5f),   // front-left three-quarter, slightly above
    new UnityEngine.Vector3(7f, 1.4f, -11f),    // rear-right
};
var expProfile = UnityEngine.ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
var exp = expProfile.Add<UnityEngine.Rendering.HighDefinition.Exposure>(false);
exp.adaptationMode.overrideState = true;
exp.adaptationMode.value = UnityEngine.Rendering.HighDefinition.AdaptationMode.Fixed;
var dof = expProfile.Add<UnityEngine.Rendering.HighDefinition.MotionBlur>(false);
dof.intensity.overrideState = true; dof.intensity.value = 0f;
var go = new UnityEngine.GameObject("Photo Camera") { hideFlags = UnityEngine.HideFlags.DontSave };
var vol = go.AddComponent<UnityEngine.Rendering.Volume>(); vol.isGlobal = true; vol.priority = 1000f; vol.sharedProfile = expProfile;
var cam = go.AddComponent<UnityEngine.Camera>(); cam.nearClipPlane = 0.1f; cam.farClipPlane = 20000f; cam.fieldOfView = 40f;
go.AddComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>();
var rt = new UnityEngine.RenderTexture(1600, 900, 24, UnityEngine.RenderTextureFormat.ARGB32, UnityEngine.RenderTextureReadWrite.sRGB);
cam.targetTexture = rt;
var outNames = new System.Collections.Generic.List<string>();
try
{
    for (int k = 0; k < cams.Length; k++)
    {
        go.transform.position = center + g.transform.rotation * cams[k];
        go.transform.rotation = UnityEngine.Quaternion.LookRotation(center + UnityEngine.Vector3.up * 0.3f - go.transform.position);
        for (int i = 0; i < 16; i++) cam.Render();
        UnityEngine.RenderTexture.active = rt;
        var tex = new UnityEngine.Texture2D(1600, 900, UnityEngine.TextureFormat.RGB24, false);
        tex.ReadPixels(new UnityEngine.Rect(0, 0, 1600, 900), 0, 0); tex.Apply();
        UnityEngine.RenderTexture.active = null;
        string f = "Screenshots//*NAME*/_" + (char)('a' + k) + ".png";
        System.IO.File.WriteAllBytes(f, UnityEngine.ImageConversion.EncodeToPNG(tex));
        UnityEngine.Object.DestroyImmediate(tex);
        outNames.Add(f);
    }
}
finally
{
    cam.targetTexture = null; rt.Release();
    UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(g); UnityEngine.Object.DestroyImmediate(expProfile);
}
return string.Join(", ", outNames);
