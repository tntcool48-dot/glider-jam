using System.Collections.Generic;
using GliderJam.Race;
using UnityEditor;
using UnityEngine;

namespace GliderJam.EditorTools
{
	// Finds a flyable race line from the original start to the original finish through the reshaped pillars,
	// verifies it against the terrain colliders and stores it (with checkpoint rings) in a RaceRoute.
	public static class RouteBuilder
	{
		private static readonly Vector3 Start = new Vector3(342f, 210f, 213f);
		private static readonly Vector3 Finish = new Vector3(596f, 222f, 6766f);
		private const float Cell = 10f, MinX = 0f, MaxX = 1000f, MinZ = 0f, MaxZ = 7000f;

		public static string Build(float blockHeight = 170f, float checkpointSpacing = 480f, float minClear = 2.2f)
		{
			Terrain[] terrains = Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None);
			int nx = (int)((MaxX - MinX) / Cell), nz = (int)((MaxZ - MinZ) / Cell);
			var hmax = new float[nx, nz];
			for (int x = 0; x < nx; x++)
			{
				for (int z = 0; z < nz; z++)
				{
					float m = 0f;
					for (int sx = 0; sx < 3; sx++)
					{
						for (int sz = 0; sz < 3; sz++)
						{
							m = Mathf.Max(m, Height(terrains, MinX + (x + sx * 0.5f) * Cell, MinZ + (z + sz * 0.5f) * Cell));
						}
					}
					hmax[x, z] = m;
				}
			}
			// Clearance (distance in cells to the nearest blocked cell), via two-pass chamfer transform.
			var dist = new float[nx, nz];
			for (int x = 0; x < nx; x++) for (int z = 0; z < nz; z++) dist[x, z] = hmax[x, z] > blockHeight ? 0f : 9999f;
			for (int pass = 0; pass < 2; pass++)
			{
				for (int x = 0; x < nx; x++)
				{
					for (int z = 0; z < nz; z++)
					{
						int xx = pass == 0 ? x : nx - 1 - x, zz = pass == 0 ? z : nz - 1 - z, s = pass == 0 ? -1 : 1;
						float d = dist[xx, zz];
						if (xx + s >= 0 && xx + s < nx) d = Mathf.Min(d, dist[xx + s, zz] + 1f);
						if (zz + s >= 0 && zz + s < nz) d = Mathf.Min(d, dist[xx, zz + s] + 1f);
						if (xx + s >= 0 && xx + s < nx && zz + s >= 0 && zz + s < nz) d = Mathf.Min(d, dist[xx + s, zz + s] + 1.414f);
						if (xx - s >= 0 && xx - s < nx && zz + s >= 0 && zz + s < nz) d = Mathf.Min(d, dist[xx - s, zz + s] + 1.414f);
						dist[xx, zz] = d;
					}
				}
			}
			Vector2Int a = ToCell(Start), b = ToCell(Finish);
			DebugMap(dist, nx, nz, minClear, a, b);
			List<Vector2Int> cells = AStar(dist, nx, nz, a, b, minClear);
			if (cells == null)
			{
				return "no path at blockHeight " + blockHeight;
			}
			// To world, smooth (Chaikin), resample every 20 m with an altitude profile.
			var pts = new List<Vector3>();
			foreach (Vector2Int c in cells) pts.Add(new Vector3(MinX + (c.x + 0.5f) * Cell, 0f, MinZ + (c.y + 0.5f) * Cell));
			pts[0] = new Vector3(Start.x, 0f, Start.z);
			pts[pts.Count - 1] = new Vector3(Finish.x, 0f, Finish.z);
			for (int it = 0; it < 4; it++) pts = Chaikin(pts);
			List<Vector3> path = Resample(pts, 20f);
			var alt = new float[path.Count];
			for (int i = 0; i < path.Count; i++)
			{
				float local = 0f;
				for (float ox = -40f; ox <= 40f; ox += 20f)
				{
					for (float oz = -40f; oz <= 40f; oz += 20f)
					{
						local = Mathf.Max(local, Height(terrains, path[i].x + ox, path[i].z + oz));
					}
				}
				alt[i] = Mathf.Clamp(local + 55f, 150f, 245f);
			}
			for (int it = 0; it < 30; it++)
			{
				var s = (float[])alt.Clone();
				for (int i = 1; i < alt.Length - 1; i++) s[i] = Mathf.Max(alt[i], (alt[i - 1] + alt[i] + alt[i + 1]) / 3f);
				alt = s;
			}
			alt[0] = Start.y;
			alt[alt.Length - 1] = Finish.y;
			for (int i = 0; i < path.Count; i++) path[i] = new Vector3(path[i].x, alt[i], path[i].z);

			int carved = Carve(terrains, path, 38f, 45f, 42f);
			Physics.SyncTransforms();

			// Verify against colliders.
			int blocked = 0;
			for (int i = 1; i < path.Count; i++)
			{
				Vector3 d = path[i] - path[i - 1];
				if (Physics.SphereCast(path[i - 1], 9f, d.normalized, out RaycastHit hit, d.magnitude, ~0, QueryTriggerInteraction.Ignore) && hit.collider is TerrainCollider)
				{
					blocked++;
				}
			}

			// Checkpoints by arc length; the last is the finish.
			var cps = new List<int>();
			float acc = 0f, next = checkpointSpacing;
			for (int i = 1; i < path.Count; i++)
			{
				acc += Vector3.Distance(path[i], path[i - 1]);
				if (acc >= next && acc < TotalLength(path) - checkpointSpacing * 0.5f)
				{
					cps.Add(i);
					next += checkpointSpacing;
				}
			}
			cps.Add(path.Count - 1);

			var go = GameObject.Find("/Race Route") ?? new GameObject("Race Route");
			var route = go.GetComponent<RaceRoute>();
			if (route == null) route = go.AddComponent<RaceRoute>();
			route.path = path.ToArray();
			route.checkpoints = cps.ToArray();
			EditorUtility.SetDirty(route);
			UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
			return "route: " + path.Count + " points, " + TotalLength(path).ToString("F0") + " m, " + cps.Count + " checkpoints, carved samples: " + carved + ", blocked segments: " + blocked;
		}

		// Lowers terrain under and beside the race line so it keeps `below` metres of clearance within `radius`
		// (horizontal), blending back to the original over `falloff`. Keeps narrow necks raceable.
		private static int Carve(Terrain[] terrains, List<Vector3> path, float radius, float falloff, float below)
		{
			int changed = 0;
			foreach (Terrain t in terrains)
			{
				TerrainData d = t.terrainData;
				int res = d.heightmapResolution;
				Vector3 o = t.transform.position, size = d.size;
				float[,] h = d.GetHeights(0, 0, res, res);
				var allowed = new float[res, res];
				for (int z = 0; z < res; z++) for (int x = 0; x < res; x++) allowed[z, x] = float.MaxValue;
				float cell = size.x / (res - 1);
				float reach = radius + falloff;
				for (int i = 1; i < path.Count; i++)
				{
					Vector3 a = path[i - 1], b = path[i];
					float minX = Mathf.Min(a.x, b.x) - reach - o.x, maxX = Mathf.Max(a.x, b.x) + reach - o.x;
					float minZ = Mathf.Min(a.z, b.z) - reach - o.z, maxZ = Mathf.Max(a.z, b.z) + reach - o.z;
					if (maxX < 0 || maxZ < 0 || minX > size.x || minZ > size.z) continue;
					int x0 = Mathf.Max(0, (int)(minX / cell)), x1 = Mathf.Min(res - 1, (int)(maxX / cell) + 1);
					int z0 = Mathf.Max(0, (int)(minZ / cell)), z1 = Mathf.Min(res - 1, (int)(maxZ / cell) + 1);
					var a2 = new Vector2(a.x, a.z);
					var ab = new Vector2(b.x - a.x, b.z - a.z);
					for (int z = z0; z <= z1; z++)
					{
						for (int x = x0; x <= x1; x++)
						{
							var p = new Vector2(o.x + x * cell, o.z + z * cell);
							float tt = Mathf.Clamp01(Vector2.Dot(p - a2, ab) / Mathf.Max(0.001f, ab.sqrMagnitude));
							float dist = (p - (a2 + ab * tt)).magnitude;
							if (dist > reach) continue;
							float floor = Mathf.Lerp(a.y, b.y, tt) - below;
							float blend = GliderJam.World.WorldNoise.SmoothStep(radius, reach, dist);
							float lim = floor + blend * 600f;
							if (lim < allowed[z, x]) allowed[z, x] = lim;
						}
					}
				}
				for (int z = 0; z < res; z++)
				{
					for (int x = 0; x < res; x++)
					{
						float cur = o.y + h[z, x] * size.y;
						if (cur > allowed[z, x])
						{
							h[z, x] = Mathf.Max(0f, (allowed[z, x] - o.y) / size.y);
							changed++;
						}
					}
				}
				d.SetHeights(0, 0, h);
				EditorUtility.SetDirty(d);
			}
			AssetDatabase.SaveAssets();
			return changed;
		}

		private static void DebugMap(float[,] dist, int nx, int nz, float minClear, Vector2Int a, Vector2Int b)
		{
			var tex = new Texture2D(nz, nx, TextureFormat.RGB24, false);
			for (int x = 0; x < nx; x++)
			{
				for (int z = 0; z < nz; z++)
				{
					Color c = dist[x, z] <= 0f ? Color.black : (dist[x, z] < minClear ? new Color(0.6f, 0.2f, 0.2f) : new Color(0.2f, 0.7f, 0.3f));
					tex.SetPixel(z, x, c);
				}
			}
			tex.SetPixel(a.y, a.x, Color.yellow);
			tex.SetPixel(b.y, b.x, Color.cyan);
			tex.Apply();
			System.IO.File.WriteAllBytes("Screenshots/route_blockmap.png", tex.EncodeToPNG());
			Object.DestroyImmediate(tex);
		}

		private static float TotalLength(List<Vector3> p)
		{
			float l = 0f;
			for (int i = 1; i < p.Count; i++) l += Vector3.Distance(p[i], p[i - 1]);
			return l;
		}

		private static float Height(Terrain[] terrains, float x, float z)
		{
			foreach (Terrain t in terrains)
			{
				Vector3 p = t.transform.position, s = t.terrainData.size;
				if (x >= p.x && x <= p.x + s.x && z >= p.z && z <= p.z + s.z)
				{
					return t.SampleHeight(new Vector3(x, 0f, z)) + p.y;
				}
			}
			return 600f;
		}

		private static Vector2Int ToCell(Vector3 w)
		{
			return new Vector2Int((int)((w.x - MinX) / Cell), (int)((w.z - MinZ) / Cell));
		}

		private static List<Vector2Int> AStar(float[,] dist, int nx, int nz, Vector2Int a, Vector2Int b, float minClear)
		{
			var g = new float[nx, nz];
			var from = new Vector2Int[nx, nz];
			for (int x = 0; x < nx; x++) for (int z = 0; z < nz; z++) g[x, z] = float.MaxValue;
			var open = new SortedSet<(float f, int x, int z)>();
			g[a.x, a.y] = 0f;
			open.Add((0f, a.x, a.y));
			int[] dx = { 1, -1, 0, 0, 1, 1, -1, -1 }, dz = { 0, 0, 1, -1, 1, -1, 1, -1 };
			while (open.Count > 0)
			{
				var cur = open.Min;
				open.Remove(cur);
				if (cur.x == b.x && cur.z == b.y)
				{
					var path = new List<Vector2Int>();
					var c = b;
					while (c != a) { path.Add(c); c = from[c.x, c.y]; }
					path.Add(a);
					path.Reverse();
					return path;
				}
				for (int k = 0; k < 8; k++)
				{
					int x = cur.x + dx[k], z = cur.z + dz[k];
					if (x < 0 || z < 0 || x >= nx || z >= nz) continue;
					bool nearEnd = (Mathf.Abs(x - b.x) + Mathf.Abs(z - b.y) < 6) || (Mathf.Abs(x - a.x) + Mathf.Abs(z - a.y) < 6);
					if (dist[x, z] < minClear && !nearEnd) continue;
					float step = k < 4 ? 1f : 1.414f;
					float cost = step * (1f + 10f / (dist[x, z] + 0.5f));
					float ng = g[cur.x, cur.z] + cost;
					if (ng < g[x, z])
					{
						if (g[x, z] < float.MaxValue) open.Remove((g[x, z] + H(x, z, b), x, z));
						g[x, z] = ng;
						from[x, z] = new Vector2Int(cur.x, cur.z);
						open.Add((ng + H(x, z, b), x, z));
					}
				}
			}
			return null;
		}

		private static float H(int x, int z, Vector2Int b)
		{
			return Mathf.Sqrt((x - b.x) * (x - b.x) + (z - b.y) * (z - b.y));
		}

		private static List<Vector3> Chaikin(List<Vector3> p)
		{
			var o = new List<Vector3> { p[0] };
			for (int i = 0; i < p.Count - 1; i++)
			{
				o.Add(Vector3.Lerp(p[i], p[i + 1], 0.25f));
				o.Add(Vector3.Lerp(p[i], p[i + 1], 0.75f));
			}
			o.Add(p[p.Count - 1]);
			return o;
		}

		private static List<Vector3> Resample(List<Vector3> p, float step)
		{
			var o = new List<Vector3> { p[0] };
			float carry = 0f;
			for (int i = 1; i < p.Count; i++)
			{
				Vector3 a = p[i - 1], b = p[i];
				float seg = Vector3.Distance(a, b);
				float t = step - carry;
				while (t <= seg)
				{
					o.Add(Vector3.Lerp(a, b, t / seg));
					t += step;
				}
				carry = seg - (t - step);
			}
			if ((o[o.Count - 1] - p[p.Count - 1]).magnitude > 1f) o.Add(p[p.Count - 1]);
			return o;
		}
	}
}
