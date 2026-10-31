using System.IO;
using GliderJam.World;
using UnityEditor;
using UnityEngine;

namespace GliderJam.EditorTools
{
	// Imports the Poly Haven CC0 texture sets, builds HDRP mask maps and the Layered Lit terrain material,
	// and wires the MeshTerrain renderer into the level.
	public static class TerrainArtSetup
	{
		public const string DownloadDir = @"F:\glider_jam\_downloads\polyhaven";
		public const string ArtDir = "Assets/GliderJam/Art/Terrain";
		public const string MaterialPath = ArtDir + "/TerrainLayered.mat";

		// layer order: 0 = base (cliff, triplanar), 1 = R, 2 = G, 3 = B
		// Layer 2 is a second, darker cliff rock (strata bands on the pillars and rocky slopes; see MeshTerrain.Weights).
		private static readonly string[] Ids = { "cliff_side", "rocky_terrain_02", "dark_rock", "mossy_rock" };
		private static readonly int[] UvMode = { 5, 4, 5, 5 };           // 4 = planar (world XZ), 5 = triplanar
		private static readonly float[] MetersPerTile = { 29f, 34f, 26f, 15f };
		private static readonly Color[] Tint =
		{
			new Color(0.82f, 0.74f, 0.70f), new Color(0.9f, 1.0f, 0.86f), new Color(1f, 0.97f, 0.94f), new Color(0.85f, 0.95f, 0.85f)
		};

		public static string ImportTextures()
		{
			var sb = new System.Text.StringBuilder();
			foreach (string id in Ids)
			{
				string dir = ArtDir + "/" + id;
				Directory.CreateDirectory(dir);
				string src = Path.Combine(DownloadDir, id);
				CopyIfMissing(Path.Combine(src, id + "_diff_2k.jpg"), dir + "/" + id + "_diff.jpg");
				CopyIfMissing(Path.Combine(src, id + "_nor_gl_2k.jpg"), dir + "/" + id + "_normal.jpg");
				CopyIfMissing(Path.Combine(src, id + "_disp_2k.jpg"), dir + "/" + id + "_height.jpg");
				string mask = dir + "/" + id + "_mask.png";
				if (!File.Exists(mask))
				{
					var ao = Load(Path.Combine(src, id + "_ao_2k.jpg"));
					var rough = Load(Path.Combine(src, id + "_rough_2k.jpg"));
					var px = new Color32[ao.width * ao.height];
					Color32[] a = ao.GetPixels32(), r = rough.GetPixels32();
					for (int i = 0; i < px.Length; i++)
					{
						px[i] = new Color32(0, a[i].r, 255, (byte)(255 - r[i].r));
					}
					var m = new Texture2D(ao.width, ao.height, TextureFormat.RGBA32, false, true);
					m.SetPixels32(px);
					File.WriteAllBytes(mask, m.EncodeToPNG());
					Object.DestroyImmediate(ao);
					Object.DestroyImmediate(rough);
					Object.DestroyImmediate(m);
				}
				sb.Append(id).Append(" ");
			}
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
			foreach (string id in Ids)
			{
				string dir = ArtDir + "/" + id;
				Configure(dir + "/" + id + "_diff.jpg", TextureImporterType.Default, true);
				Configure(dir + "/" + id + "_normal.jpg", TextureImporterType.NormalMap, false);
				Configure(dir + "/" + id + "_mask.png", TextureImporterType.Default, false);
				Configure(dir + "/" + id + "_height.jpg", TextureImporterType.Default, false);
			}
			return "imported " + sb;
		}

		public static string CreateMaterial()
		{
			var shader = Shader.Find("HDRP/LayeredLit");
			var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
			if (mat == null)
			{
				mat = new Material(shader);
				AssetDatabase.CreateAsset(mat, MaterialPath);
			}
			mat.shader = shader;
			mat.SetFloat("_LayerCount", 4f);
			mat.SetFloat("_VertexColorMode", 1f);
			mat.SetFloat("_UseHeightBasedBlend", 1f);
			mat.SetFloat("_HeightTransition", 0.25f);
			mat.SetFloat("_ObjectScaleAffectTile", 0f);
			for (int i = 0; i < 4; i++)
			{
				string id = Ids[i], dir = ArtDir + "/" + id + "/" + id;
				mat.SetTexture("_BaseColorMap" + i, AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "_diff.jpg"));
				mat.SetTexture("_NormalMap" + i, AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "_normal.jpg"));
				mat.SetTexture("_MaskMap" + i, AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "_mask.png"));
				mat.SetTexture("_HeightMap" + i, AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "_height.jpg"));
				mat.SetColor("_BaseColor" + i, Tint[i]);
				mat.SetFloat("_UVBase" + i, UvMode[i]);
				mat.SetFloat("_TexWorldScale" + i, 1f / MetersPerTile[i]);
				mat.SetFloat("_NormalScale" + i, 1.2f);
				mat.SetFloat("_SmoothnessRemapMin" + i, 0f);
				mat.SetFloat("_SmoothnessRemapMax" + i, 0.8f);
				mat.SetFloat("_HeightMapParametrization" + i, 0f);
				mat.SetFloat("_HeightMin" + i, -1f);
				mat.SetFloat("_HeightMax" + i, 1f);
			}
			UnityEditor.Rendering.HighDefinition.HDShaderUtils.ResetMaterialKeywords(mat);
			EditorUtility.SetDirty(mat);
			AssetDatabase.SaveAssets();
			return "material " + MaterialPath + " keywords: " + string.Join(",", mat.shaderKeywords);
		}

		// Adds (or refreshes) the MeshTerrain renderer in the open level scene.
		public static string InstallInScene()
		{
			var go = GameObject.Find("/World Mesh Terrain");
			if (go == null)
			{
				go = new GameObject("World Mesh Terrain");
			}
			var mt = go.GetComponent<MeshTerrain>();
			if (mt == null)
			{
				mt = go.AddComponent<MeshTerrain>();
			}
			mt.material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
			// Large chunks keep draw calls low (each chunk is drawn per camera and per shadow cascade).
			mt.chunkCells = 128;
			mt.outerChunkSize = 1000f;
			mt.outerChunkCells = 48;
			mt.Rebuild();
			UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
			return "MeshTerrain chunks: " + go.transform.GetChild(0).childCount;
		}

		private static void CopyIfMissing(string src, string dst)
		{
			if (!File.Exists(dst))
			{
				File.Copy(src, dst);
			}
		}

		private static Texture2D Load(string path)
		{
			var t = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
			t.LoadImage(File.ReadAllBytes(path));
			return t;
		}

		private static void Configure(string path, TextureImporterType type, bool srgb)
		{
			var ti = (TextureImporter)AssetImporter.GetAtPath(path);
			ti.textureType = type;
			ti.sRGBTexture = srgb;
			ti.maxTextureSize = 2048;
			ti.mipmapEnabled = true;
			ti.anisoLevel = 8;
			ti.textureCompression = TextureImporterCompression.CompressedHQ;
			ti.SaveAndReimport();
		}
	}
}
