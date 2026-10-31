using System.Collections.Generic;
using System.IO;
using GliderJam.Race;
using GliderJam.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace GliderJam.EditorTools
{
	// Golden Pillars upgrade pass (run with the canyon scene open): valley water, bird flocks, dust motes,
	// drifting 3D-noise mist and sharper cumulus shapes.
	public static class CanyonSetup
	{
		private const string Dir = "Assets/GliderJam/Art/Canyon";
		private const string SampleDir = "Assets/Samples/High Definition Render Pipeline/17.3.0/Particle System Shader Samples/Materials/";

		public static string Run()
		{
			Directory.CreateDirectory(Dir);
			AtmosphereSetup.Missing.Clear();

			// Water: dark, glassy, gently rippled (reflects the pillars through SSR / ray tracing).
			var water = Lit("Canyon_Water", new Color(0.035f, 0.075f, 0.07f), 0.94f, AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/GliderJam/Art/City/Water_Normal.png"), 0.35f);
			var birds = Particle("FX_Birds", BirdSheet(), 0f);
			var dust = Particle("FX_Dust", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/GliderJam/Art/FX/fx_spark.png"), 1f);

			var go = GameObject.Find("/Canyon Ambience") ?? new GameObject("Canyon Ambience");
			var amb = go.GetComponent<CanyonAmbience>();
			if (amb == null) amb = go.AddComponent<CanyonAmbience>();
			amb.waterMaterial = water;
			amb.birdMaterial = birds;
			amb.dustMaterial = dust;
			amb.waterLevel = 2.5f;
			amb.waterArea = new Rect(0f, 0f, 1000f, 7000f);
			var route = Object.FindFirstObjectByType<RaceRoute>();
			var flocks = new List<Vector3>();
			for (int i = 30; i < route.path.Length - 20; i += 55)
			{
				Vector3 p = route.path[i];
				Vector3 f = (route.path[i + 2] - route.path[i]).normalized;
				Vector3 side = Vector3.Cross(Vector3.up, f).normalized * ((i / 55) % 2 == 0 ? 1f : -1f);
				Vector3 c = p + side * 140f + Vector3.up * 35f;
				c.y = Mathf.Min(c.y, route.ceiling - 50f);
				flocks.Add(c);
			}
			amb.flocks = flocks.ToArray();
			amb.waterfallMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/GliderJam/Art/FX/FX_Smoke.mat");
			amb.waterfalls = FindWaterfalls(route).ToArray();
			EditorUtility.SetDirty(amb);

			// Mist: drifting 3D noise in the valley fog volumes.
			Texture3D noise = MistNoise();
			int volumes = 0;
			foreach (LocalVolumetricFog f in Object.FindObjectsByType<LocalVolumetricFog>(FindObjectsSortMode.None))
			{
				LocalVolumetricFogArtistParameters p = f.parameters;
				p.volumeMask = noise;
				p.textureTiling = new Vector3(6f, 1.5f, 6f);
				p.textureScrollingSpeed = new Vector3(0.012f, 0.002f, 0.007f);
				f.parameters = p;
				EditorUtility.SetDirty(f);
				volumes++;
			}

			// Cumulus with clearer individual shapes.
			var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(AtmosphereSetup.ProfilePath);
			var clouds = AtmosphereSetup.Get<VolumetricClouds>(profile);
			AtmosphereSetup.P(clouds, "densityMultiplier", 0.45f);
			AtmosphereSetup.P(clouds, "shapeFactor", 0.82f);
			AtmosphereSetup.P(clouds, "shapeScale", 3.2f);
			AtmosphereSetup.P(clouds, "erosionFactor", 0.65f);
			AtmosphereSetup.P(clouds, "erosionScale", 120f);
			EditorUtility.SetDirty(profile);

			EditorSceneManager.MarkSceneDirty(go.scene);
			EditorSceneManager.SaveOpenScenes();
			AssetDatabase.SaveAssets();
			return "canyon ambience: " + flocks.Count + " flocks, " + amb.waterfalls.Length + " waterfalls, mist volumes " + volumes + (AtmosphereSetup.Missing.Count > 0 ? "; missing: " + string.Join(", ", AtmosphereSetup.Missing) : "");
		}

		// Pillar edges beside the race line where water can spill towards it: walk outwards from the line until the
		// terrain rises steeply, climb to the lip, keep lips below the ceiling, about 800 m apart, alternating sides.
		private static List<Vector4> FindWaterfalls(RaceRoute route)
		{
			Terrain[] terrains = Terrain.activeTerrains;
			float H(float x, float z)
			{
				foreach (Terrain t in terrains)
				{
					Vector3 p = t.transform.position, s = t.terrainData.size;
					if (x >= p.x && x <= p.x + s.x && z >= p.z && z <= p.z + s.z) return t.SampleHeight(new Vector3(x, 0f, z)) + p.y;
				}
				return float.NaN;
			}
			var falls = new List<Vector4>();
			float lastZ = float.MinValue;
			int flip = 0;
			for (int i = 20; i < route.path.Length - 10 && falls.Count < 7; i += 3)
			{
				Vector3 p = route.path[i];
				if (p.z - lastZ < 800f) continue;
				Vector3 f = Vector3.ProjectOnPlane(route.path[i + 2] - route.path[i], Vector3.up).normalized;
				foreach (float sgn in flip % 2 == 0 ? new[] { 1f, -1f } : new[] { -1f, 1f })
				{
					Vector3 side = Vector3.Cross(Vector3.up, f) * sgn;
					float prev = H(p.x, p.z);
					bool found = false;
					for (float d = 30f; d < 260f && !found; d += 6f)
					{
						Vector3 q = p + side * d;
						float h = H(q.x, q.z);
						if (float.IsNaN(h) || float.IsNaN(prev)) break;
						if (h - prev > 18f && h > 60f)
						{
							float top = h;
							Vector3 lip = q;
							for (float d2 = d + 3f; d2 < d + 80f; d2 += 3f)
							{
								Vector3 q2 = p + side * d2;
								float h2 = H(q2.x, q2.z);
								if (float.IsNaN(h2) || h2 - top < 1.5f) break;
								top = h2;
								lip = q2;
							}
							if (top > 90f && top < route.ceiling - 20f)
							{
								Vector3 edge = new Vector3(lip.x, top + 1f, lip.z) - side * 1.5f;
								falls.Add(new Vector4(edge.x, edge.y, edge.z, Mathf.Atan2(-side.x, -side.z) * Mathf.Rad2Deg));
								lastZ = p.z;
								flip++;
								found = true;
							}
							break;
						}
						prev = h;
					}
					if (found) break;
				}
			}
			return falls;
		}

		// Four wing-flap frames of a distant bird silhouette (256 x 64).
		private static Texture2D BirdSheet()
		{
			const int fw = 64, h = 64, frames = 4;
			float[] tip = { 0.42f, 0.12f, -0.22f, 0.1f };
			var px = new Color[fw * frames * h];
			for (int k = 0; k < frames; k++)
			{
				for (int y = 0; y < h; y++)
				{
					for (int x = 0; x < fw; x++)
					{
						float u = (x + 0.5f) / fw * 2f - 1f, v = (y + 0.5f) / h * 2f - 1f;
						float ax = Mathf.Abs(u);
						float wingY = tip[k] * Mathf.Pow(ax, 1.4f) - 0.05f * ax;
						float thick = Mathf.Lerp(0.11f, 0.02f, ax);
						float wing = ax < 0.95f ? Mathf.Clamp01((thick - Mathf.Abs(v - wingY)) * 40f) : 0f;
						float body = Mathf.Clamp01((1f - (u * u / 0.012f + (v + 0.02f) * (v + 0.02f) / 0.006f)) * 4f);
						float a = Mathf.Max(wing, body);
						px[y * fw * frames + k * fw + x] = new Color(1f, 1f, 1f, a);
					}
				}
			}
			string path = Dir + "/fx_birds.png";
			var t = new Texture2D(fw * frames, h, TextureFormat.RGBA32, true);
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

		// Tileable 32^3 value-noise fBm in alpha (fog density mask), white RGB.
		private static Texture3D MistNoise()
		{
			const int n = 32;
			var tex = new Texture3D(n, n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, name = "MistNoise" };
			var c = new Color[n * n * n];
			for (int z = 0; z < n; z++)
			{
				for (int y = 0; y < n; y++)
				{
					for (int x = 0; x < n; x++)
					{
						float sum = 0f, amp = 1f, norm = 0f;
						for (int o = 0; o < 3; o++)
						{
							int f = 4 << o;
							sum += Noise3(x * f / (float)n, y * f / (float)n, z * f / (float)n, f, o) * amp;
							norm += amp;
							amp *= 0.5f;
						}
						float d = Mathf.Clamp01((sum / norm - 0.3f) * 1.8f);
						c[(z * n + y) * n + x] = new Color(1f, 1f, 1f, d);
					}
				}
			}
			tex.SetPixels(c);
			tex.Apply();
			string path = Dir + "/MistNoise.asset";
			AssetDatabase.DeleteAsset(path);
			AssetDatabase.CreateAsset(tex, path);
			return tex;
		}

		private static float Noise3(float x, float y, float z, int period, int seed)
		{
			int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y), iz = Mathf.FloorToInt(z);
			float fx = x - ix, fy = y - iy, fz = z - iz;
			fx = fx * fx * (3f - 2f * fx);
			fy = fy * fy * (3f - 2f * fy);
			fz = fz * fz * (3f - 2f * fz);
			float H(int a, int b, int cc) => WorldNoise.Hash(((a % period) + period) % period + 97 * (((cc % period) + period) % period), ((b % period) + period) % period, seed + 11);
			float x00 = Mathf.Lerp(H(ix, iy, iz), H(ix + 1, iy, iz), fx), x10 = Mathf.Lerp(H(ix, iy + 1, iz), H(ix + 1, iy + 1, iz), fx);
			float x01 = Mathf.Lerp(H(ix, iy, iz + 1), H(ix + 1, iy, iz + 1), fx), x11 = Mathf.Lerp(H(ix, iy + 1, iz + 1), H(ix + 1, iy + 1, iz + 1), fx);
			return Mathf.Lerp(Mathf.Lerp(x00, x10, fy), Mathf.Lerp(x01, x11, fy), fz);
		}

		private static Material Lit(string name, Color color, float smooth, Texture2D normal, float normalScale)
		{
			string path = Dir + "/" + name + ".mat";
			var m = AssetDatabase.LoadAssetAtPath<Material>(path);
			if (m == null)
			{
				m = new Material(Shader.Find("HDRP/Lit"));
				AssetDatabase.CreateAsset(m, path);
			}
			m.SetColor("_BaseColor", color);
			m.SetFloat("_Smoothness", smooth);
			m.SetFloat("_Metallic", 0f);
			m.SetTexture("_NormalMap", normal);
			m.SetFloat("_NormalScale", normalScale);
			UnityEditor.Rendering.HighDefinition.HDShaderUtils.ResetMaterialKeywords(m);
			EditorUtility.SetDirty(m);
			return m;
		}

		private static Material Particle(string name, Texture2D tex, float blendMode)
		{
			var sample = AssetDatabase.LoadAssetAtPath<Material>(SampleDir + "Unlit.mat");
			string path = Dir + "/" + name + ".mat";
			var m = AssetDatabase.LoadAssetAtPath<Material>(path);
			if (m == null)
			{
				m = new Material(sample);
				AssetDatabase.CreateAsset(m, path);
			}
			m.shader = sample.shader;
			m.CopyPropertiesFromMaterial(sample);
			for (int i = 0; i < m.shader.GetPropertyCount(); i++)
			{
				string p = m.shader.GetPropertyName(i);
				if (p.StartsWith("Texture2D_") && m.shader.GetPropertyType(i) == ShaderPropertyType.Texture)
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
	}
}
