// Play mode: render a still from a free camera (position / look target) to Screenshots//*NAME*/.png.
var pos = new UnityEngine.Vector3(/*POS*/);
var target = new UnityEngine.Vector3(/*TARGET*/);
var go = new UnityEngine.GameObject("Photo Camera");
var cam = go.AddComponent<UnityEngine.Camera>();
cam.nearClipPlane = 0.3f; cam.farClipPlane = 20000f; cam.fieldOfView = 60f;
var hd = go.AddComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>();
GliderJam.QualityManager.ConfigureCamera(cam, GliderJam.GameConfig.Current);
go.transform.position = pos;
go.transform.rotation = UnityEngine.Quaternion.LookRotation(target - pos);
var rt = new UnityEngine.RenderTexture(1600, 900, 24, UnityEngine.RenderTextureFormat.ARGB32, UnityEngine.RenderTextureReadWrite.sRGB);
cam.targetTexture = rt;
try
{
    for (int i = 0; i < 24; i++) cam.Render();
    UnityEngine.RenderTexture.active = rt;
    var tex = new UnityEngine.Texture2D(1600, 900, UnityEngine.TextureFormat.RGB24, false);
    tex.ReadPixels(new UnityEngine.Rect(0, 0, 1600, 900), 0, 0); tex.Apply();
    UnityEngine.RenderTexture.active = null;
    System.IO.File.WriteAllBytes("Screenshots//*NAME*/.png", UnityEngine.ImageConversion.EncodeToPNG(tex));
    UnityEngine.Object.DestroyImmediate(tex);
}
finally
{
    cam.targetTexture = null; rt.Release();
    UnityEngine.Object.DestroyImmediate(go);
}
return "Screenshots//*NAME*/.png";
