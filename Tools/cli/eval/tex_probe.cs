// Sample the imported (GPU) wing panel texture along a row crossing the root joint line (u = 0.112).
var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>("Assets/GliderJam/Art/Glider/Panels_Wing.png");
var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/GliderJam/Art/Glider/Paint_Wing.mat");
var rt = UnityEngine.RenderTexture.GetTemporary(tex.width, tex.height, 0, UnityEngine.RenderTextureFormat.ARGB32);
UnityEngine.Graphics.Blit(tex, rt);
UnityEngine.RenderTexture.active = rt;
var read = new UnityEngine.Texture2D(tex.width, tex.height, UnityEngine.TextureFormat.RGBA32, false);
read.ReadPixels(new UnityEngine.Rect(0, 0, tex.width, tex.height), 0, 0);
UnityEngine.RenderTexture.active = null;
UnityEngine.RenderTexture.ReleaseTemporary(rt);
var sb = new System.Text.StringBuilder("row v=0.7: ");
for (int x = 220; x < 240; x += 2) sb.Append(read.GetPixel(x, (int)(0.7f * tex.height)).r.ToString("F2")).Append(" ");
sb.Append(" | mat tex=").Append(mat.GetTexture("_BaseColorMap") == tex).Append(" color=").Append(mat.GetColor("_BaseColor")).Append(" st=").Append(mat.GetVector("_BaseColorMap_ST"));
sb.Append(" uvBase=").Append(mat.GetFloat("_UVBase")).Append(" shader=").Append(mat.shader.name);
UnityEngine.Object.DestroyImmediate(read);
return sb.ToString();
