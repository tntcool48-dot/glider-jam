using GliderJam.World;
using UnityEditor;
using UnityEngine;

namespace GliderJam.EditorTools
{
	// Reshapes the level's terrain tiles into natural sandstone towers while keeping the original layout.
	// Always works from a backup of the original TerrainData, so it can be re-run with new settings.
	public static class TerrainSculpt
	{
		private const string BackupDir = "Assets/GliderLevel/TerrainData/Original";

		public static string Run()
		{
			Terrain[] terrains = Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None);
			System.Array.Sort(terrains, (a, b) => a.transform.position.z.CompareTo(b.transform.position.z));
			if (!AssetDatabase.IsValidFolder(BackupDir))
			{
				AssetDatabase.CreateFolder("Assets/GliderLevel/TerrainData", "Original");
			}

			int res = terrains[0].terrainData.heightmapResolution;
			float height = terrains[0].terrainData.size.y;
			float cell = terrains[0].terrainData.size.x / (res - 1);
			int gx = res;
			int gz = terrains.Length * (res - 1) + 1;
			var orig = new float[gz, gx];

			for (int i = 0; i < terrains.Length; i++)
			{
				string src = AssetDatabase.GetAssetPath(terrains[i].terrainData);
				string dst = BackupDir + "/" + System.IO.Path.GetFileName(src);
				if (AssetDatabase.LoadAssetAtPath<TerrainData>(dst) == null)
				{
					AssetDatabase.CopyAsset(src, dst);
				}
				TerrainData backup = AssetDatabase.LoadAssetAtPath<TerrainData>(dst);
				float[,] h = backup.GetHeights(0, 0, res, res);
				for (int z = 0; z < res; z++)
				{
					for (int x = 0; x < res; x++)
					{
						orig[i * (res - 1) + z, x] = h[z, x] * backup.size.y;
					}
				}
			}

			float worldX = (gx - 1) * cell, worldZ = (gz - 1) * cell;
			var result = new float[gz, gx];
			for (int z = 0; z < gz; z++)
			{
				for (int x = 0; x < gx; x++)
				{
					float wx = x * cell, wz = z * cell;
					float border = Mathf.Min(Mathf.Min(wx, worldX - wx), Mathf.Min(wz, worldZ - wz));
					float warpFade = WorldNoise.SmoothStep(0f, 80f, border);
					float sx = wx + warpFade * 26f * WorldNoise.Fbm(wx / 170f, wz / 170f, 3);
					float sz = wz + warpFade * 26f * WorldNoise.Fbm(wx / 170f + 37.2f, wz / 170f - 11.4f, 3);
					float h = Sample(orig, sx / cell, sz / cell);

					// Steepness of the original surface at the sample point (1 = 45 degrees).
					float gxs = (Sample(orig, sx / cell + 1f, sz / cell) - Sample(orig, sx / cell - 1f, sz / cell)) / (2f * cell);
					float gzs = (Sample(orig, sx / cell, sz / cell + 1f) - Sample(orig, sx / cell, sz / cell - 1f)) / (2f * cell);
					float steep = WorldNoise.SmoothStep(0.55f, 1.3f, Mathf.Sqrt(gxs * gxs + gzs * gzs));

					// Columnar walls: two tanh bands steepen the mid walls and flatten ledges / pillar tops.
					h = Band(h, 165f, 125f, 2.2f);
					h = Band(h, 470f, 110f, 1.8f);

					// Sandstone strata: horizontal ledges on steep walls, period varies so layers are irregular.
					float period = 15f + 6f * WorldNoise.Fbm(wx / 400f, wz / 400f, 2);
					float layer = h / period;
					float step = (Mathf.Floor(layer) + WorldNoise.SmoothStep(0.35f, 0.65f, layer - Mathf.Floor(layer))) * period;
					h = Mathf.Lerp(h, step, steep * 0.55f);

					// Vertical fluting and rock breakup on walls, gentle undulation on flats.
					h += steep * (3.2f * (WorldNoise.Ridged(wx / 17f, wz / 17f, 2) - 0.45f) + 3f * WorldNoise.Fbm(wx / 45f, wz / 45f, 3));
					h += (1f - steep) * 1.4f * WorldNoise.Fbm(wx / 70f, wz / 70f, 3);

					result[z, x] = Mathf.Clamp(h, 0f, height);
				}
			}

			for (int i = 0; i < terrains.Length; i++)
			{
				var tile = new float[res, res];
				for (int z = 0; z < res; z++)
				{
					for (int x = 0; x < res; x++)
					{
						tile[z, x] = result[i * (res - 1) + z, x] / height;
					}
				}
				terrains[i].terrainData.SetHeights(0, 0, tile);
				EditorUtility.SetDirty(terrains[i].terrainData);
			}
			AssetDatabase.SaveAssets();
			return "sculpted " + terrains.Length + " tiles (" + gx + "x" + gz + " samples)";
		}

		private static float Band(float h, float center, float halfWidth, float k)
		{
			float d = h - center;
			if (Mathf.Abs(d) >= halfWidth)
			{
				return h;
			}
			return center + halfWidth * (float)System.Math.Tanh(k * d / halfWidth) / (float)System.Math.Tanh(k);
		}

		private static float Sample(float[,] h, float x, float z)
		{
			int w = h.GetLength(1), d = h.GetLength(0);
			x = Mathf.Clamp(x, 0f, w - 1.001f);
			z = Mathf.Clamp(z, 0f, d - 1.001f);
			int x0 = (int)x, z0 = (int)z;
			float fx = x - x0, fz = z - z0;
			float a = Mathf.Lerp(h[z0, x0], h[z0, x0 + 1], fx);
			float b = Mathf.Lerp(h[z0 + 1, x0], h[z0 + 1, x0 + 1], fx);
			return Mathf.Lerp(a, b, fz);
		}
	}
}
