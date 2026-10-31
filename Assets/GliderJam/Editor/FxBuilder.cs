using System.IO;
using GliderJam.World;
using UnityEditor;
using UnityEngine;

namespace GliderJam.EditorTools
{
	// Generates FX textures and materials (HDRP particle Shader Graph samples, emissive HDRP Lit rings)
	// and the FxLibrary asset the race loads at runtime.
	public static class FxBuilder
	{
		private const string Dir = "Assets/GliderJam/Art/FX";
		private const string SampleDir = "Assets/Samples/High Definition Render Pipeline/17.3.0/Particle System Shader Samples/Materials/";
		public const string LibraryPath = "Assets/GliderJam/Settings/FxLibrary.asset";

		public static string Build()
		{
			Directory.CreateDirectory(Dir);
			Texture2D trailTex = Tex("fx_trail", 64, 64, (u, v) => { float a = Mathf.Sin(Mathf.PI * v); return new Color(1f, 1f, 1f, a * a); });
			Texture2D puffTex = Tex("fx_puff", 256, 256, Puff);
			Texture2D sparkTex = Tex("fx_spark", 64, 64, (u, v) =>
			{
				float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f;
				float a = Mathf.Clamp01(1f - d);
				return new Color(1f, 1f, 1f, a * a * a);
			});

			Material unlit = AssetDatabase.LoadAssetAtPath<Material>(SampleDir + "Unlit.mat");
			Material lit = AssetDatabase.LoadAssetAtPath<Material>(SampleDir + "LitSoft.mat") ?? AssetDatabase.LoadAssetAtPath<Material>(SampleDir + "Lit.mat");

			var lib = AssetDatabase.LoadAssetAtPath<FxLibrary>(LibraryPath);
			if (lib == null)
			{
				lib = ScriptableObject.CreateInstance<FxLibrary>();
				AssetDatabase.CreateAsset(lib, LibraryPath);
			}
			lib.trail = Particle("FX_Trail", unlit, trailTex, 0f);
			lib.vapor = Particle("FX_Vapor", unlit, trailTex, 0f);
			lib.wingVapor = Particle("FX_WingVapor", unlit, puffTex, 0f);
			lib.fire = Particle("FX_Fire", unlit, puffTex, 1f);
			lib.spark = Particle("FX_Spark", unlit, sparkTex, 1f);
			lib.smoke = Particle("FX_Smoke", lit, puffTex, 0f);
			lib.ring = Emissive("Ring_Checkpoint", new Color(1f, 0.86f, 0.62f), 9000f);
			lib.ringFinish = Emissive("Ring_Finish", new Color(1f, 0.74f, 0.25f), 16000f);
			lib.ringGlow = Particle("Ring_Glow", unlit, trailTex, 1f);
			lib.gliderPrefabs = new[]
			{
				AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GliderJam/Art/Glider/Glider_P1.prefab"),
				AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GliderJam/Art/Glider/Glider_P2.prefab")
			};
			lib.explosionClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/GliderLevel/AudioClip/Grenade1Short.ogg");
			lib.windClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/GliderLevel/AudioClip/loud-wind-429220.ogg");
			lib.engineClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/GliderLevel/AudioClip/C-130.ogg");
			EditorUtility.SetDirty(lib);
			AssetDatabase.SaveAssets();
			return "fx library ok (unlit=" + (unlit != null) + ", lit=" + (lit != null ? lit.shader.name : "null") + ")";
		}

		// Soft, lumpy puff for fire and smoke.
		private static Color Puff(float u, float v)
		{
			float dx = u - 0.5f, dy = v - 0.5f;
			float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
			float n = WorldNoise.Fbm(u * 6f, v * 6f, 4) * 0.5f + 0.5f;
			float edge = Mathf.Clamp01(1f - d * (0.85f + 0.5f * (1f - n)));
			float a = edge * edge * (3f - 2f * edge);
			float shade = Mathf.Lerp(0.7f, 1f, n);
			return new Color(shade, shade, shade, a);
		}

		private static Texture2D Tex(string name, int w, int h, System.Func<float, float, Color> f)
		{
			string path = Dir + "/" + name + ".png";
			var t = new Texture2D(w, h, TextureFormat.RGBA32, true);
			var px = new Color[w * h];
			for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) px[y * w + x] = f((x + 0.5f) / w, (y + 0.5f) / h);
			t.SetPixels(px);
			t.Apply();
			File.WriteAllBytes(path, t.EncodeToPNG());
			Object.DestroyImmediate(t);
			AssetDatabase.ImportAsset(path);
			var ti = (TextureImporter)AssetImporter.GetAtPath(path);
			ti.alphaIsTransparency = true;
			ti.wrapMode = TextureWrapMode.Clamp;
			ti.SaveAndReimport();
			return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
		}

		private static Material Particle(string name, Material sample, Texture2D tex, float blendMode)
		{
			string path = Dir + "/" + name + ".mat";
			var m = AssetDatabase.LoadAssetAtPath<Material>(path);
			if (m == null)
			{
				m = new Material(sample);
				AssetDatabase.CreateAsset(m, path);
			}
			m.shader = sample.shader;
			m.CopyPropertiesFromMaterial(sample);
			// The sample graphs expose their main texture as the first "Texture2D_*" property.
			for (int i = 0; i < m.shader.GetPropertyCount(); i++)
			{
				string p = m.shader.GetPropertyName(i);
				if (p.StartsWith("Texture2D_") && m.shader.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Texture)
				{
					m.SetTexture(p, tex);
					break;
				}
			}
			m.SetFloat("_BlendMode", blendMode);
			UnityEditor.Rendering.HighDefinition.HDShaderUtils.ResetMaterialKeywords(m);
			EditorUtility.SetDirty(m);
			return m;
		}

		private static Material Emissive(string name, Color color, float nits)
		{
			string path = Dir + "/" + name + ".mat";
			var m = AssetDatabase.LoadAssetAtPath<Material>(path);
			if (m == null)
			{
				m = new Material(Shader.Find("HDRP/Lit"));
				AssetDatabase.CreateAsset(m, path);
			}
			m.SetColor("_BaseColor", color * 0.4f);
			m.SetFloat("_Smoothness", 0.7f);
			m.SetColor("_EmissiveColor", color * nits);
			m.SetColor("_EmissiveColorLDR", color);
			UnityEditor.Rendering.HighDefinition.HDShaderUtils.ResetMaterialKeywords(m);
			EditorUtility.SetDirty(m);
			return m;
		}
	}
}
