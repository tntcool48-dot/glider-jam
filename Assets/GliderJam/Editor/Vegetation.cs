using System.Collections.Generic;
using System.IO;
using GliderJam.World;
using UnityEditor;
using UnityEngine;

namespace GliderJam.EditorTools
{
	// Procedural conifers (generated meshes + textures) and the forest scatter for the level's terrain tiles.
	public static class Vegetation
	{
		private const string Dir = "Assets/GliderJam/Art/Trees";

		public static string BuildTrees()
		{
			Directory.CreateDirectory(Dir);
			Texture2D needles = BuildNeedleTexture(out Texture2D needleNormal);
			Material foliage = LitMaterial("ConiferFoliage", new Color(1f, 1f, 1f), 0.25f);
			foliage.SetTexture("_BaseColorMap", needles);
			foliage.SetTexture("_NormalMap", needleNormal);
			foliage.SetFloat("_AlphaCutoffEnable", 1f);
			foliage.SetFloat("_AlphaCutoff", 0.45f);
			foliage.SetFloat("_DoubleSidedEnable", 1f);
			foliage.SetFloat("_DoubleSidedNormalMode", 1f);
			UnityEditor.Rendering.HighDefinition.HDShaderUtils.ResetMaterialKeywords(foliage);
			Material bark = LitMaterial("ConiferBark", new Color(0.23f, 0.16f, 0.11f), 0.15f);

			var variants = new[]
			{
				new TreeShape { height = 22f, width = 0.36f, tiers = 10, seed = 1, tint = new Color(0.85f, 0.95f, 0.85f) },
				new TreeShape { height = 17f, width = 0.42f, tiers = 8, seed = 2, tint = new Color(1f, 1f, 0.9f) },
				new TreeShape { height = 27f, width = 0.30f, tiers = 12, seed = 3, tint = new Color(0.8f, 0.92f, 0.95f) },
				new TreeShape { height = 13f, width = 0.48f, tiers = 7, seed = 4, tint = new Color(1f, 1.05f, 0.85f) },
			};
			var sb = new System.Text.StringBuilder();
			for (int i = 0; i < variants.Length; i++)
			{
				Material leaf = new Material(foliage) { name = "ConiferFoliage" + i };
				leaf.SetColor("_BaseColor", variants[i].tint);
				string leafPath = Dir + "/ConiferFoliage" + i + ".mat";
				AssetDatabase.DeleteAsset(leafPath);
				AssetDatabase.CreateAsset(leaf, leafPath);

				Mesh lod0 = BuildConifer(variants[i], 14, 1f, true);
				Mesh lod1 = BuildConifer(variants[i], 7, 0.55f, false);
				SaveMesh(lod0, Dir + "/Conifer" + i + "_LOD0.asset");
				SaveMesh(lod1, Dir + "/Conifer" + i + "_LOD1.asset");

				var root = new GameObject("Conifer" + i);
				var r0 = MakeLod(root, "LOD0", lod0, bark, leaf);
				var r1 = MakeLod(root, "LOD1", lod1, bark, leaf);
				var group = root.AddComponent<LODGroup>();
				group.SetLODs(new[] { new LOD(0.06f, new Renderer[] { r0 }), new LOD(0.006f, new Renderer[] { r1 }) });
				group.RecalculateBounds();
				PrefabUtility.SaveAsPrefabAsset(root, Dir + "/Conifer" + i + ".prefab");
				Object.DestroyImmediate(root);
				sb.Append("Conifer").Append(i).Append(" tris=").Append(lod0.triangles.Length / 3).Append("/").Append(lod1.triangles.Length / 3).Append(" ");
			}
			AssetDatabase.SaveAssets();
			return sb.ToString();
		}

		public static string Scatter(float spacing = 9f)
		{
			var prefabs = new List<GameObject>();
			for (int i = 0; i < 4; i++)
			{
				prefabs.Add(AssetDatabase.LoadAssetAtPath<GameObject>(Dir + "/Conifer" + i + ".prefab"));
			}
			var protos = new TreePrototype[prefabs.Count];
			for (int i = 0; i < protos.Length; i++)
			{
				protos[i] = new TreePrototype { prefab = prefabs[i] };
			}
			int total = 0;
			foreach (Terrain t in Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None))
			{
				TerrainData d = t.terrainData;
				Vector3 o = t.transform.position, size = d.size;
				var list = new List<TreeInstance>();
				for (float z = spacing * 0.5f; z < size.z; z += spacing)
				{
					for (float x = spacing * 0.5f; x < size.x; x += spacing)
					{
						float wx = o.x + x, wz = o.z + z;
						int ix = Mathf.FloorToInt(wx / spacing), iz = Mathf.FloorToInt(wz / spacing);
						float jx = (WorldNoise.Hash(ix, iz, 21) - 0.5f) * spacing * 0.9f;
						float jz = (WorldNoise.Hash(ix, iz, 22) - 0.5f) * spacing * 0.9f;
						float u = Mathf.Clamp01((x + jx) / size.x), v = Mathf.Clamp01((z + jz) / size.z);
						float steep = d.GetSteepness(u, v);
						if (steep > 27f)
						{
							continue;
						}
						float forest = WorldNoise.SmoothStep(-0.25f, 0.25f, WorldNoise.Fbm(wx / 320f, wz / 320f, 3) + 0.15f);
						forest *= WorldNoise.SmoothStep(27f, 15f, steep);
						if (WorldNoise.Hash(ix, iz, 23) > forest * 0.85f)
						{
							continue;
						}
						float s = 0.75f + 0.55f * WorldNoise.Hash(ix, iz, 24);
						list.Add(new TreeInstance
						{
							position = new Vector3(u, 0f, v),
							prototypeIndex = Mathf.Min(3, (int)(WorldNoise.Hash(ix, iz, 25) * 4f)),
							widthScale = s * (0.9f + 0.2f * WorldNoise.Hash(ix, iz, 26)),
							heightScale = s,
							rotation = WorldNoise.Hash(ix, iz, 27) * Mathf.PI * 2f,
							color = Color.white,
							lightmapColor = Color.white
						});
					}
				}
				d.treeInstances = new TreeInstance[0];
				d.treePrototypes = protos;
				d.RefreshPrototypes();
				d.SetTreeInstances(list.ToArray(), true);
				EditorUtility.SetDirty(d);
				t.treeDistance = 4000f;
				t.treeMaximumFullLODCount = 400;
				t.Flush();
				total += list.Count;
			}
			AssetDatabase.SaveAssets();
			return "trees placed: " + total;
		}

		// ---------------------------------------------------------------- mesh

		private struct TreeShape
		{
			public float height, width;
			public int tiers, seed;
			public Color tint;
		}

		private static Mesh BuildConifer(TreeShape s, int segments, float tierScale, bool doubleLayer)
		{
			var v = new List<Vector3>();
			var n = new List<Vector3>();
			var uv = new List<Vector2>();
			var trunk = new List<int>();
			var leaves = new List<int>();
			var rng = new System.Random(s.seed * 7919);

			// Trunk
			int ts = 7;
			float tr = s.height * 0.022f;
			for (int i = 0; i <= ts; i++)
			{
				float a = i * Mathf.PI * 2f / ts;
				var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
				v.Add(dir * tr); n.Add(dir); uv.Add(new Vector2(i / (float)ts, 0f));
				v.Add(dir * tr * 0.25f + Vector3.up * s.height * 0.9f); n.Add(dir); uv.Add(new Vector2(i / (float)ts, 1f));
			}
			for (int i = 0; i < ts; i++)
			{
				int a = i * 2;
				trunk.AddRange(new[] { a, a + 1, a + 2, a + 1, a + 3, a + 2 });
			}

			// Foliage tiers: drooping jagged skirts, wider at the bottom.
			int tiers = Mathf.Max(3, Mathf.RoundToInt(s.tiers * tierScale));
			float start = s.height * 0.16f;
			for (int t = 0; t < tiers; t++)
			{
				float k = t / (float)(tiers - 1);
				float yTop = Mathf.Lerp(start + s.height * 0.12f, s.height, Mathf.Pow(k, 0.85f));
				float radius = s.height * s.width * Mathf.Lerp(1f, 0.08f, Mathf.Pow(k, 0.9f)) * (0.9f + 0.2f * (float)rng.NextDouble());
				float droop = radius * (0.75f + 0.3f * (float)rng.NextDouble());
				for (int layer = 0; layer < (doubleLayer ? 2 : 1); layer++)
				{
					float rot = (float)rng.NextDouble() * Mathf.PI * 2f + layer * Mathf.PI / segments;
					float lr = radius * (layer == 0 ? 1f : 0.78f);
					float ly = yTop - (layer == 0 ? 0f : droop * 0.25f);
					int baseIndex = v.Count;
					v.Add(new Vector3(0f, ly, 0f)); n.Add(Vector3.up); uv.Add(new Vector2(0.5f, 0.02f));
					for (int i = 0; i <= segments; i++)
					{
						float a = rot + i * Mathf.PI * 2f / segments;
						float jag = (i == segments) ? 1f : 0.82f + 0.36f * (float)rng.NextDouble();
						var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
						float r = lr * jag;
						Vector3 tip = dir * r + Vector3.up * (ly - droop * jag * (layer == 0 ? 1f : 0.85f));
						Vector3 mid = dir * r * 0.55f + Vector3.up * (ly - droop * 0.35f);
						Vector3 normal = (dir * 0.55f + Vector3.up).normalized;
						v.Add(mid); n.Add(normal); uv.Add(new Vector2(i / (float)segments * 3f, 0.5f));
						v.Add(tip); n.Add((dir * 0.8f + Vector3.up * 0.6f).normalized); uv.Add(new Vector2(i / (float)segments * 3f, 0.98f));
					}
					for (int i = 0; i < segments; i++)
					{
						int m0 = baseIndex + 1 + i * 2, t0 = m0 + 1, m1 = m0 + 2, t1 = m0 + 3;
						leaves.AddRange(new[] { baseIndex, m1, m0 });
						leaves.AddRange(new[] { m0, m1, t0, m1, t1, t0 });
					}
				}
			}
			var mesh = new Mesh { name = "Conifer" };
			mesh.SetVertices(v);
			mesh.SetNormals(n);
			mesh.SetUVs(0, uv);
			mesh.subMeshCount = 2;
			mesh.SetTriangles(trunk, 0);
			mesh.SetTriangles(leaves, 1);
			mesh.RecalculateTangents();
			mesh.RecalculateBounds();
			return mesh;
		}

		private static Renderer MakeLod(GameObject root, string name, Mesh mesh, Material bark, Material leaf)
		{
			var go = new GameObject(name);
			go.transform.SetParent(root.transform, false);
			go.AddComponent<MeshFilter>().sharedMesh = mesh;
			var mr = go.AddComponent<MeshRenderer>();
			mr.sharedMaterials = new[] { bark, leaf };
			return mr;
		}

		private static void SaveMesh(Mesh mesh, string path)
		{
			AssetDatabase.DeleteAsset(path);
			AssetDatabase.CreateAsset(mesh, path);
		}

		// ---------------------------------------------------------------- textures / materials

		// Needle texture: v = 0 at the trunk (dark), v = 1 at the branch tips (lighter, alpha-cut jagged fringe).
		private static Texture2D BuildNeedleTexture(out Texture2D normal)
		{
			const int W = 256, H = 256;
			var col = new Color32[W * H];
			var hgt = new float[W * H];
			var rng = new System.Random(42);
			for (int y = 0; y < H; y++)
			{
				float v = y / (float)(H - 1);
				for (int x = 0; x < W; x++)
				{
					float u = x / (float)W;
					// needles: thin diagonal streaks along v
					float streak = 0.5f + 0.5f * WorldNoise.Perlin(u * 90f, v * 6f);
					float fine = 0.5f + 0.5f * WorldNoise.Perlin(u * 260f + v * 30f, v * 14f);
					float h = Mathf.Clamp01(streak * 0.6f + fine * 0.5f);
					float fringe = 0.86f + 0.12f * WorldNoise.Perlin(u * 70f, 3.1f) + 0.06f * WorldNoise.Perlin(u * 220f, 7.7f);
					float alpha = v < fringe ? 1f : 0f;
					alpha *= h > 0.32f || v < 0.6f ? 1f : 0f;
					Color dark = new Color(0.025f, 0.05f, 0.03f), light = new Color(0.12f, 0.20f, 0.085f), tipC = new Color(0.20f, 0.27f, 0.11f);
					Color c = Color.Lerp(dark, light, Mathf.SmoothStep(0f, 1f, v)) * (0.65f + 0.6f * h);
					c = Color.Lerp(c, tipC * (0.7f + 0.5f * h), Mathf.SmoothStep(0.7f, 1f, v) * 0.6f);
					int i = y * W + x;
					col[i] = new Color(c.r, c.g, c.b, alpha);
					hgt[i] = h;
				}
			}
			var tex = new Texture2D(W, H, TextureFormat.RGBA32, true);
			tex.SetPixels32(col);
			tex.Apply();
			string albedoPath = Dir + "/needles_albedo.png";
			File.WriteAllBytes(albedoPath, tex.EncodeToPNG());
			var nrm = new Color32[W * H];
			for (int y = 0; y < H; y++)
			{
				for (int x = 0; x < W; x++)
				{
					float dx = hgt[y * W + (x + 1) % W] - hgt[y * W + (x + W - 1) % W];
					float dy = hgt[Mathf.Min(H - 1, y + 1) * W + x] - hgt[Mathf.Max(0, y - 1) * W + x];
					var nn = new Vector3(-dx * 2.5f, -dy * 2.5f, 1f).normalized;
					nrm[y * W + x] = new Color(nn.x * 0.5f + 0.5f, nn.y * 0.5f + 0.5f, nn.z * 0.5f + 0.5f, 1f);
				}
			}
			var ntex = new Texture2D(W, H, TextureFormat.RGBA32, true, true);
			ntex.SetPixels32(nrm);
			ntex.Apply();
			string normalPath = Dir + "/needles_normal.png";
			File.WriteAllBytes(normalPath, ntex.EncodeToPNG());
			Object.DestroyImmediate(tex);
			Object.DestroyImmediate(ntex);
			AssetDatabase.ImportAsset(albedoPath);
			AssetDatabase.ImportAsset(normalPath);
			var ti = (TextureImporter)AssetImporter.GetAtPath(albedoPath);
			ti.alphaIsTransparency = true;
			ti.mipMapsPreserveCoverage = true;
			ti.alphaTestReferenceValue = 0.45f;
			ti.SaveAndReimport();
			var ni = (TextureImporter)AssetImporter.GetAtPath(normalPath);
			ni.textureType = TextureImporterType.NormalMap;
			ni.SaveAndReimport();
			normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
			return AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
		}

		private static Material LitMaterial(string name, Color color, float smoothness)
		{
			string path = Dir + "/" + name + ".mat";
			var m = AssetDatabase.LoadAssetAtPath<Material>(path);
			if (m == null)
			{
				m = new Material(Shader.Find("HDRP/Lit"));
				AssetDatabase.CreateAsset(m, path);
			}
			m.SetColor("_BaseColor", color);
			m.SetFloat("_Smoothness", smoothness);
			m.SetFloat("_Metallic", 0f);
			UnityEditor.Rendering.HighDefinition.HDShaderUtils.ResetMaterialKeywords(m);
			EditorUtility.SetDirty(m);
			return m;
		}
	}
}
