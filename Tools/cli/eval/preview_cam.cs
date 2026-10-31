// Render a temporary preview camera at POS looking at LOOK into Screenshots/NAME.png (W x H), letting
// exposure / temporal effects settle over several frames first.
var go = new UnityEngine.GameObject("Preview Camera") { hideFlags = UnityEngine.HideFlags.DontSave };
// Edit-mode renders don't advance time, so auto exposure would never adapt: force instant adaptation.
var expProfile = UnityEngine.ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
var exp = expProfile.Add<UnityEngine.Rendering.HighDefinition.Exposure>(false);
exp.adaptationMode.overrideState = true;
exp.adaptationMode.value = UnityEngine.Rendering.HighDefinition.AdaptationMode.Fixed;
var expVol = go.AddComponent<UnityEngine.Rendering.Volume>();
expVol.isGlobal = true; expVol.priority = 1000f; expVol.sharedProfile = expProfile;
try
{
    var cam = go.AddComponent<UnityEngine.Camera>();
    cam.farClipPlane = 20000f; cam.nearClipPlane = 0.5f; cam.fieldOfView = /*FOV*/f;
    go.AddComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>();
    var pos = new UnityEngine.Vector3(/*POS*/);
    var look = new UnityEngine.Vector3(/*LOOK*/);
    go.transform.position = pos;
    go.transform.rotation = UnityEngine.Quaternion.LookRotation(look - pos);
    int w = /*W*/, h = /*H*/;
    var rt = new UnityEngine.RenderTexture(w, h, 24, UnityEngine.RenderTextureFormat.ARGB32, UnityEngine.RenderTextureReadWrite.sRGB);
    rt.antiAliasing = 1;
    cam.targetTexture = rt;
    for (int i = 0; i < /*FRAMES*/; i++) cam.Render();
    var prev = UnityEngine.RenderTexture.active;
    UnityEngine.RenderTexture.active = rt;
    var tex = new UnityEngine.Texture2D(w, h, UnityEngine.TextureFormat.RGB24, false);
    tex.ReadPixels(new UnityEngine.Rect(0, 0, w, h), 0, 0);
    tex.Apply();
    UnityEngine.RenderTexture.active = prev;
    cam.targetTexture = null;
    System.IO.Directory.CreateDirectory("Screenshots");
    System.IO.File.WriteAllBytes("Screenshots//*NAME*/.png", UnityEngine.ImageConversion.EncodeToPNG(tex));
    UnityEngine.Object.DestroyImmediate(tex);
    rt.Release();
    return "Screenshots//*NAME*/.png";
}
finally
{
    UnityEngine.Object.DestroyImmediate(go);
    UnityEngine.Object.DestroyImmediate(expProfile);
}
