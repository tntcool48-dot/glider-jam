using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace GliderJam.World
{
	// Renders the level's Unity Terrain tiles as chunked LOD meshes (HDRP Layered Lit: triplanar cliffs,
	// world-planar ground) and surrounds the corridor with procedural mountains and pillar fields.
	// Unity's Terrain keeps its colliders and trees; only its heightmap rendering is turned off.
	// Generated objects are never saved; they are rebuilt on enable (edit mode and play mode).
	[ExecuteAlways]
	public class MeshTerrain : MonoBehaviour
	{
		public Material material;

		[Header("Corridor (from Unity Terrain tiles)")]
		public int chunkCells = 128;

		[Header("Surrounding landscape")]
		public bool buildOuter = true;
		public Vector2 outerMin = new Vector2(-5000f, -4000f);
		public Vector2 outerMax = new Vector2(6000f, 11000f);
		public float outerChunkSize = 1000f;
		public int outerChunkCells = 48;

		[Header("Misc")]
		public float skirtDepth = 8f;
		public bool buildInEditMode = true;

		private const string RootName = "__MeshTerrainChunks";

		private float[,] _h;
		private float _cell;
		private Vector3 _origin;
		private float _sizeX, _sizeZ;
		private readonly List<Terrain> _terrains = new List<Terrain>();

		public float CorridorMinX => _origin.x;
		public float CorridorMaxX => _origin.x + _sizeX;
		public float CorridorMinZ => _origin.z;
		public float CorridorMaxZ => _origin.z + _sizeZ;

		private void OnEnable()
		{
			if (!Application.isPlaying && !buildInEditMode)
			{
				return;
			}
			Rebuild();
		}

		private void OnDisable()
		{
			Clear();
			foreach (Terrain t in _terrains)
			{
				if (t != null)
				{
					t.drawHeightmap = true;
				}
			}
		}

		public void Clear()
		{
			for (int i = transform.childCount - 1; i >= 0; i--)
			{
				Transform c = transform.GetChild(i);
				if (c.name == RootName)
				{
					DestroyImmediate(c.gameObject);
				}
			}
		}

		public void Rebuild()
		{
			Clear();
			if (material == null || !LoadHeightField())
			{
				return;
			}
			var root = new GameObject(RootName) { hideFlags = HideFlags.DontSave };
			root.transform.SetParent(transform, false);
			root.layer = gameObject.layer;

			// Corridor chunks
			int res = _h.GetLength(1);
			int cellsX = res - 1, cellsZ = _h.GetLength(0) - 1;
			var jobs = new List<ChunkJob>();
			for (int cz = 0; cz < cellsZ; cz += chunkCells)
			{
				for (int cx = 0; cx < cellsX; cx += chunkCells)
				{
					jobs.Add(new ChunkJob { corridor = true, x0 = cx, z0 = cz, cells = chunkCells, size = chunkCells * _cell });
				}
			}
			// Surrounding chunks (skip those covering the corridor)
			if (buildOuter)
			{
				for (float z = outerMin.y; z < outerMax.y - 1f; z += outerChunkSize)
				{
					for (float x = outerMin.x; x < outerMax.x - 1f; x += outerChunkSize)
					{
						bool insideCorridor = x >= CorridorMinX - 1f && x + outerChunkSize <= CorridorMaxX + 1f && z >= CorridorMinZ - 1f && z + outerChunkSize <= CorridorMaxZ + 1f;
						if (!insideCorridor)
						{
							jobs.Add(new ChunkJob { corridor = false, wx0 = x, wz0 = z, cells = outerChunkCells, size = outerChunkSize });
						}
					}
				}
			}

			Parallel.For(0, jobs.Count, i => BuildChunkData(jobs[i]));

			foreach (ChunkJob job in jobs)
			{
				CreateChunkObject(root.transform, job);
			}
			foreach (Terrain t in _terrains)
			{
				t.drawHeightmap = false;
			}
		}

		// ---------------------------------------------------------------- height field

		private bool LoadHeightField()
		{
			_terrains.Clear();
			_terrains.AddRange(FindObjectsByType<Terrain>(FindObjectsSortMode.None));
			if (_terrains.Count == 0)
			{
				return false;
			}
			_terrains.Sort((a, b) => a.transform.position.z.CompareTo(b.transform.position.z));
			TerrainData d0 = _terrains[0].terrainData;
			int res = d0.heightmapResolution;
			_cell = d0.size.x / (res - 1);
			_origin = _terrains[0].transform.position;
			_sizeX = d0.size.x;
			_sizeZ = d0.size.z * _terrains.Count;
			_h = new float[_terrains.Count * (res - 1) + 1, res];
			for (int i = 0; i < _terrains.Count; i++)
			{
				TerrainData d = _terrains[i].terrainData;
				float[,] h = d.GetHeights(0, 0, res, res);
				float y0 = _terrains[i].transform.position.y;
				for (int z = 0; z < res; z++)
				{
					for (int x = 0; x < res; x++)
					{
						_h[i * (res - 1) + z, x] = y0 + h[z, x] * d.size.y;
					}
				}
			}
			return true;
		}

		private float Grid(int x, int z)
		{
			x = Mathf.Clamp(x, 0, _h.GetLength(1) - 1);
			z = Mathf.Clamp(z, 0, _h.GetLength(0) - 1);
			return _h[z, x];
		}

		private float SampleCorridor(float wx, float wz)
		{
			float x = Mathf.Clamp((wx - _origin.x) / _cell, 0f, _h.GetLength(1) - 1.001f);
			float z = Mathf.Clamp((wz - _origin.z) / _cell, 0f, _h.GetLength(0) - 1.001f);
			int x0 = (int)x, z0 = (int)z;
			float fx = x - x0, fz = z - z0;
			float a = Mathf.Lerp(_h[z0, x0], _h[z0, x0 + 1], fx);
			float b = Mathf.Lerp(_h[z0 + 1, x0], _h[z0 + 1, x0 + 1], fx);
			return Mathf.Lerp(a, b, fz);
		}

		// Height of the surrounding landscape: walls blend out of the corridor edge into a low basin with
		// sandstone pillar fields, rising to big ridged ranges in the distance.
		public float OuterHeight(float x, float z)
		{
			float cx = Mathf.Clamp(x, CorridorMinX, CorridorMaxX);
			float cz = Mathf.Clamp(z, CorridorMinZ, CorridorMaxZ);
			float dx = x - cx, dz = z - cz;
			float d = Mathf.Sqrt(dx * dx + dz * dz);
			float edge = SampleCorridor(cx, cz);
			if (d < 0.01f)
			{
				return edge;
			}
			// Rolling forested hills with ridges, scattered sandstone towers of varied size.
			float baseH = 130f + 90f * WorldNoise.Fbm(x / 700f, z / 700f, 4) + 110f * (WorldNoise.Ridged(x / 520f, z / 520f, 3) - 0.35f);
			float f1 = WorldNoise.Cellular(x / 260f, z / 260f, out float cellRand);
			float radius = 0.12f + 0.16f * cellRand;
			float towerMask = 1f - WorldNoise.SmoothStep(radius, radius + 0.07f, f1 + 0.06f * WorldNoise.Fbm(x / 45f, z / 45f, 2));
			float tower = towerMask * (180f + 420f * WorldNoise.Hash((int)(x / 260f), (int)(z / 260f), 9)) * WorldNoise.SmoothStep(0.5f, 0.62f, cellRand);
			float r = WorldNoise.Ridged(x / 1900f, z / 1900f, 5);
			float far = WorldNoise.SmoothStep(700f, 3800f, d);
			float mountains = baseH + far * (r * 1250f - 120f);
			float outer = Mathf.Max(baseH + tower, mountains) + 6f * WorldNoise.Fbm(x / 60f, z / 60f, 3);
			float blend = WorldNoise.SmoothStep(0f, 420f, d);
			return Mathf.Lerp(edge, outer, blend);
		}

		// ---------------------------------------------------------------- chunk generation

		private class LodData
		{
			public Vector3[] vertices;
			public Vector3[] normals;
			public Color32[] colors;
			public int[] triangles;
		}

		private class ChunkJob
		{
			public bool corridor;
			public int x0, z0;      // corridor: grid cell origin
			public float wx0, wz0;  // outer: world origin
			public int cells;
			public float size;
			public LodData[] lods;
			public Vector3 center;
		}

		private void BuildChunkData(ChunkJob job)
		{
			int n = job.cells;
			// Sample a (n+3)^2 grid (1-cell border) for heights; normals from central differences.
			var hg = new float[n + 3, n + 3];
			float step = job.size / n;
			float ox = job.corridor ? _origin.x + job.x0 * _cell : job.wx0;
			float oz = job.corridor ? _origin.z + job.z0 * _cell : job.wz0;
			for (int z = -1; z <= n + 1; z++)
			{
				for (int x = -1; x <= n + 1; x++)
				{
					hg[z + 1, x + 1] = job.corridor ? Grid(job.x0 + x, job.z0 + z) : OuterHeight(ox + x * step, oz + z * step);
				}
			}
			var pos = new Vector3[(n + 1) * (n + 1)];
			var nrm = new Vector3[pos.Length];
			var col = new Color32[pos.Length];
			for (int z = 0; z <= n; z++)
			{
				for (int x = 0; x <= n; x++)
				{
					float h = hg[z + 1, x + 1];
					float wx = ox + x * step, wz = oz + z * step;
					Vector3 nn = new Vector3(hg[z + 1, x] - hg[z + 1, x + 2], 2f * step, hg[z, x + 1] - hg[z + 2, x + 1]).normalized;
					int i = z * (n + 1) + x;
					pos[i] = new Vector3(wx, h, wz);
					nrm[i] = nn;
					col[i] = Weights(wx, h, wz, nn);
				}
			}
			job.center = new Vector3(ox + job.size * 0.5f, 0f, oz + job.size * 0.5f);
			job.lods = new LodData[3];
			for (int l = 0; l < 3; l++)
			{
				job.lods[l] = Decimate(pos, nrm, col, n, 1 << l, job.center);
			}
		}

		private LodData Decimate(Vector3[] pos, Vector3[] nrm, Color32[] col, int n, int stride, Vector3 center)
		{
			int m = n / stride;
			int ring = 4 * m;
			int count = (m + 1) * (m + 1);
			var lod = new LodData
			{
				vertices = new Vector3[count + ring],
				normals = new Vector3[count + ring],
				colors = new Color32[count + ring]
			};
			for (int z = 0; z <= m; z++)
			{
				for (int x = 0; x <= m; x++)
				{
					int src = (z * stride) * (n + 1) + x * stride;
					int dst = z * (m + 1) + x;
					lod.vertices[dst] = pos[src] - center;
					lod.normals[dst] = nrm[src];
					lod.colors[dst] = col[src];
				}
			}
			var tris = new List<int>(m * m * 6 + ring * 6);
			for (int z = 0; z < m; z++)
			{
				for (int x = 0; x < m; x++)
				{
					int a = z * (m + 1) + x, b = a + 1, c = a + (m + 1), d = c + 1;
					tris.Add(a); tris.Add(c); tris.Add(b);
					tris.Add(b); tris.Add(c); tris.Add(d);
				}
			}
			// Skirt: a downward flap around the border hides cracks between LODs / resolutions.
			var border = new List<int>(ring);
			for (int x = 0; x < m; x++) border.Add(x);
			for (int z = 0; z < m; z++) border.Add(z * (m + 1) + m);
			for (int x = m; x > 0; x--) border.Add(m * (m + 1) + x);
			for (int z = m; z > 0; z--) border.Add(z * (m + 1));
			for (int i = 0; i < ring; i++)
			{
				int v = border[i];
				lod.vertices[count + i] = lod.vertices[v] - new Vector3(0f, skirtDepth, 0f);
				lod.normals[count + i] = lod.normals[v];
				lod.colors[count + i] = lod.colors[v];
			}
			for (int i = 0; i < ring; i++)
			{
				int a = border[i], b = border[(i + 1) % ring];
				int sa = count + i, sb = count + (i + 1) % ring;
				tris.Add(a); tris.Add(b); tris.Add(sa);
				tris.Add(b); tris.Add(sb); tris.Add(sa);
			}
			lod.triangles = tris.ToArray();
			return lod;
		}

		// Layer weights for HDRP Layered Lit (vertex color, Multiply mode):
		// base = sandstone cliff, R = grass/forest floor, G = scree, B = moss streaks.
		private static Color32 Weights(float x, float y, float z, Vector3 n)
		{
			float slope = Mathf.Acos(Mathf.Clamp(n.y, -1f, 1f)) * Mathf.Rad2Deg;
			float patch = WorldNoise.Fbm(x / 85f, z / 85f, 3);
			float grass = WorldNoise.SmoothStep(37f, 27f, slope + patch * 7f);
			float scree = WorldNoise.SmoothStep(20f, 30f, slope + patch * 5f) * WorldNoise.SmoothStep(48f, 36f, slope);
			scree = Mathf.Clamp01(scree * (0.7f + 0.6f * WorldNoise.Fbm(x / 40f + 3f, z / 40f, 2)));
			float streak = WorldNoise.Perlin3(x / 28f, y / 9f, z / 28f);
			float humidity = WorldNoise.SmoothStep(260f, 30f, y);
			float moss = WorldNoise.SmoothStep(38f, 52f, slope) * Mathf.Clamp01(0.45f + 1.6f * streak) * (0.25f + 0.75f * humidity);
			// Large-scale breakup on flat ground so the planar textures don't read as a repeating grid from the air.
			float macroA = WorldNoise.Fbm(x / 230f, z / 230f, 3), macroB = WorldNoise.Fbm(x / 310f - 40f, z / 310f + 17f, 3);
			scree = Mathf.Max(scree, grass * WorldNoise.SmoothStep(0.3f, 0.55f, macroA) * 0.45f);
			moss = Mathf.Max(moss, grass * WorldNoise.SmoothStep(-0.1f, -0.4f, macroB) * 0.9f);
			return new Color32((byte)(grass * 255f), (byte)(scree * 255f), (byte)(moss * 255f), 255);
		}

		private void CreateChunkObject(Transform root, ChunkJob job)
		{
			var go = new GameObject(job.corridor ? "Chunk" : "Outer") { hideFlags = HideFlags.DontSave };
			go.transform.SetParent(root, false);
			go.transform.position = job.center;
			go.layer = root.gameObject.layer;
			go.isStatic = false;
			var lodGroup = go.AddComponent<LODGroup>();
			var lods = new LOD[3];
			float[] thresholds = job.corridor ? new[] { 0.5f, 0.16f, 0f } : new[] { 0.45f, 0.14f, 0f };
			for (int l = 0; l < 3; l++)
			{
				var child = new GameObject("LOD" + l) { hideFlags = HideFlags.DontSave };
				child.transform.SetParent(go.transform, false);
				child.layer = go.layer;
				var mesh = new Mesh { name = go.name + "_LOD" + l, hideFlags = HideFlags.DontSave };
				mesh.SetVertices(job.lods[l].vertices);
				mesh.SetNormals(job.lods[l].normals);
				mesh.SetColors(job.lods[l].colors);
				mesh.SetTriangles(job.lods[l].triangles, 0, true);
				child.AddComponent<MeshFilter>().sharedMesh = mesh;
				var mr = child.AddComponent<MeshRenderer>();
				mr.sharedMaterial = material;
				mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
				lods[l] = new LOD(thresholds[l], new Renderer[] { mr });
			}
			lodGroup.SetLODs(lods);
			lodGroup.RecalculateBounds();
		}
	}
}
