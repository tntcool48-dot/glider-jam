using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using GliderJam.Race;
using GliderJam.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace GliderJam.EditorTools
{
	// Generates the "Neon City" night map: a street grid of procedural towers with lit-window facades, neon
	// blade signs, rooftop outlines and billboards, wet reflective streets with lamps, skybridges over the
	// race line, a harbor with the finish over the water, a distant skyline, night atmosphere and rain.
	// Geometry is merged into 720 m chunk meshes (one submesh per material); buildings get box colliders.
	public static class CityBuilder
	{
		private const string Dir = "Assets/GliderJam/Art/City";
		public const string ScenePath = "Assets/GliderJam/Scenes/City.unity";
		private const string ProfilePath = "Assets/GliderJam/Settings/NightCity.asset";
		private const string SampleDir = "Assets/Samples/High Definition Render Pipeline/17.3.0/Particle System Shader Samples/Materials/";
		private const float Pitch = 180f, Street = 64f, HalfStreet = Street * 0.5f;
		private const float Ceiling = 150f, MinTower = 165f, ChunkSize = 720f, WaterZ = 4172f, FarShoreZ = 6300f;
		private const float ColW = 4f, FloorH = 3.6f;
		private const int TileCols = 16, TileFloors = 32, CellPx = 64, FloorPx = 48;
		private const int CoreI0 = -5, CoreI1 = 5, CoreJ0 = -3, CoreJ1 = 22;

		private enum Slot { Glass, GlassPink, Housing, Concrete, Office, Dim, Mirror, MirrorGold, Chrome, Marble, Roof, Pavement, Marking, Metal, Lamp, Water, NeonPink, NeonCyan, NeonViolet, NeonAmber, NeonGreen, Count }
		private static readonly Slot[] NeonSlots = { Slot.NeonPink, Slot.NeonCyan, Slot.NeonViolet, Slot.NeonAmber, Slot.NeonGreen };
		private static readonly Color[] NeonColors = { new Color(1f, 0.12f, 0.5f), new Color(0.05f, 0.85f, 1f), new Color(0.62f, 0.25f, 1f), new Color(1f, 0.55f, 0.1f), new Color(0.2f, 1f, 0.45f) };

		// Race line through the grid: (avenue index, street index, altitude); corners are rounded.
		private static readonly Vector3[] Waypoints =
		{
			W(0f, -1.5f, 80f), W(0f, 5f, 85f), W(2f, 5f, 95f), W(2f, 9f, 100f), W(-2f, 9f, 105f), W(-2f, 13f, 100f),
			W(0f, 13f, 95f), W(0f, 17f, 85f), W(3f, 17f, 90f), W(3f, 21f, 75f), W(3f, 24.6f, 40f), W(3f, 28.5f, 40f)
		};
		private const int FinishWaypoint = 10;
		private const float CornerD = 165f;

		private struct LightSpec { public Vector3 pos; public Color color; public float lumen, range, volumetric; }
		private struct BoardSpec { public Vector3 center, normal; public float w, h; public int mat; }

		private static Random _rng;
		private static Material[] _mats;
		private static List<Vector3> _route;
		private static readonly List<LightSpec> Lights = new List<LightSpec>();
		private static readonly List<Bounds> Bridges = new List<Bounds>();
		private static readonly List<(Bounds box, TrickZone.Kind kind)> Tricks = new List<(Bounds, TrickZone.Kind)>();
		private static readonly List<BoardSpec> Boards = new List<BoardSpec>();
		private static readonly Dictionary<Vector2Int, List<Bounds>> Solids = new Dictionary<Vector2Int, List<Bounds>>();
		private static int _signLights;

		private static Vector3 W(float i, float j, float y) => new Vector3(i * Pitch, y, j * Pitch);
		private static float R() => (float)_rng.NextDouble();
		private static float R(float a, float b) => a + (b - a) * (float)_rng.NextDouble();
		private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

		public static string Build()
		{
			_rng = new Random(20261031);
			Lights.Clear();
			Bridges.Clear();
			Tricks.Clear();
			Boards.Clear();
			Solids.Clear();
			Cylinders.Clear();
			_signLights = 0;
			AtmosphereSetup.Missing.Clear();
			Directory.CreateDirectory(Dir + "/Meshes");
			Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
			foreach (string guid in AssetDatabase.FindAssets("t:Mesh", new[] { Dir + "/Meshes" })) AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));
			EditorSceneManager.SaveOpenScenes();
			Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

			_mats = MakeMaterials(out Material rain, out Material[] boards);
			_route = MakeRoute(out int[] checkpoints);
			var core = new Dictionary<Vector2Int, MB>();
			var far = new Dictionary<Vector2Int, MB>();
			var flicker = new[] { new MB(), new MB(), new MB() };
			MakeBlockers(core);
			MakeBridges(core);
			MakeBlocks(core, flicker);
			MakeStreets(core);
			MakeBackdrop(far);
			MakeHarbor(core, far);

			var city = new GameObject("City").transform;
			int verts = Emit(core, "Core", city, true);
			var backdrop = new GameObject("City Backdrop").transform;
			int farVerts = Emit(far, "Far", backdrop, false);
			var flickerRenderers = new List<Renderer>();
			for (int i = 0; i < flicker.Length; i++)
			{
				GameObject go = EmitOne(flicker[i], "Flicker_" + i, city, false);
				if (go != null) flickerRenderers.Add(go.GetComponent<Renderer>());
			}
			MakeColliders(city);
			MakeTricks(city);
			MakeLights();
			Renderer[] boardRenderers = MakeBillboards(city, boards);
			Atmosphere();

			var weather = new GameObject("City Weather").AddComponent<CityWeather>();
			weather.rainMaterial = rain;
			weather.backdrop = backdrop.gameObject;
			weather.billboards = boardRenderers;
			weather.flicker = flickerRenderers.ToArray();
			MakeTraffic(weather);

			var rr = new GameObject("Race Route").AddComponent<RaceRoute>();
			rr.path = _route.ToArray();
			rr.checkpoints = checkpoints;
			rr.checkpointRadius = 24f;
			rr.finishRadius = 34f;
			rr.ceiling = Ceiling;
			rr.minX = CoreI0 * Pitch - HalfStreet + 4f; // the outer avenues' far kerbs: the skyline beyond has no collision
			rr.maxX = (CoreI1 + 1) * Pitch + HalfStreet - 4f;
			var rm = new GameObject("Race Manager").AddComponent<RaceManager>();
			rm.fx = AssetDatabase.LoadAssetAtPath<FxLibrary>(FxBuilder.LibraryPath);
			rm.route = rr;

			EditorSceneManager.SaveScene(scene, ScenePath);
			AddToBuild();
			AssetDatabase.SaveAssets();
			return "city: " + verts + " core verts, " + farVerts + " skyline verts, " + Lights.Count + " lights, " + Boards.Count + " billboards, route "
				+ rr.path.Length + " samples / " + checkpoints.Length + " checkpoints; " + _mazeStats + "; " + Verify(rr)
				+ (AtmosphereSetup.Missing.Count > 0 ? "; missing: " + string.Join(", ", AtmosphereSetup.Missing) : "");
		}

		// ================================================================ mesh building

		private class MB
		{
			public readonly List<Vector3> v = new List<Vector3>();
			public readonly List<Vector3> n = new List<Vector3>();
			public readonly List<Vector2> uv = new List<Vector2>();
			public readonly List<int>[] t = new List<int>[(int)Slot.Count];

			public MB()
			{
				for (int i = 0; i < t.Length; i++) t[i] = new List<int>();
			}

			// a, b, c, d clockwise as seen from the front (bottom-left, top-left, top-right, bottom-right).
			public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 nrm, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud, Slot s)
			{
				int i = v.Count;
				v.Add(a); v.Add(b); v.Add(c); v.Add(d);
				n.Add(nrm); n.Add(nrm); n.Add(nrm); n.Add(nrm);
				uv.Add(ua); uv.Add(ub); uv.Add(uc); uv.Add(ud);
				t[(int)s].AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
			}

			// Axis-aligned box: facade UVs on the walls (meters / module + offset), planar UVs on top and bottom.
			public void Box(Vector3 min, Vector3 max, Slot wall, Slot top, Vector2 off, bool bottom = false, Slot bottomSlot = Slot.Metal)
			{
				float y0 = min.y, y1 = max.y;
				Wall(new Vector3(max.x, y0, min.z), new Vector3(max.x, y0, max.z), y1, Vector3.right, wall, off);
				Wall(new Vector3(min.x, y0, max.z), new Vector3(min.x, y0, min.z), y1, Vector3.left, wall, off + new Vector2(ColW * 5f, 0f));
				Wall(new Vector3(max.x, y0, max.z), new Vector3(min.x, y0, max.z), y1, Vector3.forward, wall, off + new Vector2(ColW * 9f, 0f));
				Wall(new Vector3(min.x, y0, min.z), new Vector3(max.x, y0, min.z), y1, Vector3.back, wall, off + new Vector2(ColW * 3f, 0f));
				Flat(min.x, min.z, max.x, max.z, y1, top, 24f);
				if (bottom)
				{
					Quad(new Vector3(max.x, y0, min.z), new Vector3(max.x, y0, max.z), new Vector3(min.x, y0, max.z), new Vector3(min.x, y0, min.z), Vector3.down,
						Vector2.zero, Vector2.up, Vector2.one, Vector2.right, bottomSlot);
				}
			}

			private void Wall(Vector3 bl, Vector3 br, float y1, Vector3 nrm, Slot s, Vector2 off)
			{
				float su = 1f / (ColW * TileCols), sv = 1f / (FloorH * TileFloors);
				float len = Vector3.Distance(bl, br), y0 = bl.y;
				var tl = new Vector3(bl.x, y1, bl.z);
				var tr = new Vector3(br.x, y1, br.z);
				Quad(bl, tl, tr, br, nrm,
					new Vector2(off.x * su, (y0 + off.y) * sv), new Vector2(off.x * su, (y1 + off.y) * sv),
					new Vector2((off.x + len) * su, (y1 + off.y) * sv), new Vector2((off.x + len) * su, (y0 + off.y) * sv), s);
			}

			// Extruded polygon (counter-clockwise seen from above) with facade UVs running around the perimeter and a
			// flat roof; topScale < 1 tapers the walls towards the centre (spires).
			public void Prism(Vector2[] poly, float y0, float y1, Slot wall, Slot top, Vector2 off, float topScale = 1f)
			{
				float su = 1f / (ColW * TileCols), sv = 1f / (FloorH * TileFloors);
				int cnt = poly.Length;
				Vector2 c = Vector2.zero;
				foreach (Vector2 p in poly) c += p;
				c /= cnt;
				var tp = new Vector2[cnt];
				for (int i = 0; i < cnt; i++) tp[i] = c + (poly[i] - c) * topScale;
				float u = off.x;
				for (int i = 0; i < cnt; i++)
				{
					int j = (i + 1) % cnt;
					var a = new Vector3(poly[i].x, y0, poly[i].y);
					var b = new Vector3(poly[j].x, y0, poly[j].y);
					var ta = new Vector3(tp[i].x, y1, tp[i].y);
					var tb = new Vector3(tp[j].x, y1, tp[j].y);
					float len = Vector2.Distance(poly[i], poly[j]);
					Vector3 nrm = Vector3.Cross(ta - a, b - a).normalized;
					Quad(a, ta, tb, b, nrm,
						new Vector2(u * su, (y0 + off.y) * sv), new Vector2(u * su, (y1 + off.y) * sv),
						new Vector2((u + len) * su, (y1 + off.y) * sv), new Vector2((u + len) * su, (y0 + off.y) * sv), wall);
					u += len;
				}
				int k = v.Count;
				for (int i = 0; i < cnt; i++)
				{
					v.Add(new Vector3(tp[i].x, y1, tp[i].y));
					n.Add(Vector3.up);
					uv.Add(tp[i] / 24f);
				}
				for (int i = 1; i < cnt - 1; i++) t[(int)top].AddRange(new[] { k, k + i + 1, k + i });
			}

			// Upward-facing rectangle with planar UVs (meters / scale).
			public void Flat(float x0, float z0, float x1, float z1, float y, Slot s, float scale)
			{
				Quad(new Vector3(x0, y, z0), new Vector3(x0, y, z1), new Vector3(x1, y, z1), new Vector3(x1, y, z0), Vector3.up,
					new Vector2(x0, z0) / scale, new Vector2(x0, z1) / scale, new Vector2(x1, z1) / scale, new Vector2(x1, z0) / scale, s);
			}
		}

		private static MB At(Dictionary<Vector2Int, MB> d, Vector3 p)
		{
			var k = new Vector2Int(Mathf.FloorToInt(p.x / ChunkSize), Mathf.FloorToInt(p.z / ChunkSize));
			if (!d.TryGetValue(k, out MB mb)) d[k] = mb = new MB();
			return mb;
		}

		// Straight neon tube / bar between two points on one axis.
		private static void Tube(MB mb, Vector3 a, Vector3 b, float w, Slot s)
		{
			Vector3 h = Vector3.one * (w * 0.5f);
			mb.Box(Vector3.Min(a, b) - h, Vector3.Max(a, b) + h, s, s, Vector2.zero);
		}

		// Tubes around a rectangle at height y, pushed out (or in, if negative) by `out`.
		private static void Ring(MB mb, Vector3 min, Vector3 max, float y, float o, float w, Slot s)
		{
			float x0 = min.x - o, x1 = max.x + o, z0 = min.z - o, z1 = max.z + o;
			Tube(mb, new Vector3(x0, y, z0), new Vector3(x1, y, z0), w, s);
			Tube(mb, new Vector3(x0, y, z1), new Vector3(x1, y, z1), w, s);
			Tube(mb, new Vector3(x0, y, z0), new Vector3(x0, y, z1), w, s);
			Tube(mb, new Vector3(x1, y, z0), new Vector3(x1, y, z1), w, s);
		}

		// Vertical cylinder (base centre, radius, height) for round towers: capsule colliders.
		private static readonly List<Vector4> Cylinders = new List<Vector4>();

		private static void SolidCylinder(Vector3 baseCenter, float r, float height)
		{
			Cylinders.Add(new Vector4(baseCenter.x, baseCenter.y, baseCenter.z, r));
			Cylinders.Add(new Vector4(height, 0f, 0f, 0f));
		}

		private static void Solid(Vector3 min, Vector3 max)
		{
			var b = new Bounds();
			b.SetMinMax(min, max);
			var k = new Vector2Int(Mathf.FloorToInt(b.center.x / ChunkSize), Mathf.FloorToInt(b.center.z / ChunkSize));
			if (!Solids.TryGetValue(k, out List<Bounds> l)) Solids[k] = l = new List<Bounds>();
			l.Add(b);
		}

		private static void AddLight(Vector3 p, Color c, float lumen, float range, float volumetric)
		{
			Lights.Add(new LightSpec { pos = p, color = c, lumen = lumen, range = range, volumetric = volumetric });
		}

		// ================================================================ route

		private static List<Vector3> MakeRoute(out int[] checkpoints)
		{
			var raw = new List<Vector3> { Waypoints[0] };
			int finishRaw = 0;
			for (int k = 1; k < Waypoints.Length; k++)
			{
				Vector3 prev = Waypoints[k - 1], cur = Waypoints[k];
				bool last = k == Waypoints.Length - 1;
				Vector3 dirIn = Flat(cur - prev).normalized;
				Vector3 dirOut = last ? dirIn : Flat(Waypoints[k + 1] - cur).normalized;
				if (Vector3.Angle(dirIn, dirOut) < 1f)
				{
					Line(raw, cur);
				}
				else
				{
					Vector3 next = Waypoints[k + 1];
					Vector3 a = cur - dirIn * CornerD, b = cur + dirOut * CornerD;
					a.y = Mathf.Lerp(prev.y, cur.y, 1f - CornerD / Flat(cur - prev).magnitude);
					b.y = Mathf.Lerp(cur.y, next.y, CornerD / Flat(next - cur).magnitude);
					Line(raw, a);
					for (int s = 1; s <= 30; s++)
					{
						float t = s / 30f;
						raw.Add((1f - t) * (1f - t) * a + 2f * t * (1f - t) * cur + t * t * b);
					}
				}
				if (k == FinishWaypoint) finishRaw = raw.Count - 1;
			}
			// Resample every 20 m.
			var cum = new float[raw.Count];
			for (int i = 1; i < raw.Count; i++) cum[i] = cum[i - 1] + Vector3.Distance(raw[i - 1], raw[i]);
			var path = new List<Vector3>();
			int j = 0;
			for (float s = 0f; s <= cum[cum.Length - 1]; s += 20f)
			{
				while (j < cum.Length - 2 && cum[j + 1] < s) j++;
				float t = Mathf.InverseLerp(cum[j], cum[j + 1], s);
				path.Add(Vector3.Lerp(raw[j], raw[j + 1], t));
			}
			// Smooth the altitude profile.
			var ys = new float[path.Count];
			for (int i = 0; i < path.Count; i++)
			{
				float sum = 0f;
				int n = 0;
				for (int d = -6; d <= 6; d++)
				{
					int q = Mathf.Clamp(i + d, 0, path.Count - 1);
					sum += path[q].y;
					n++;
				}
				ys[i] = sum / n;
			}
			for (int i = 0; i < path.Count; i++) path[i] = new Vector3(path[i].x, ys[i], path[i].z);

			int finish = Mathf.Clamp(Mathf.RoundToInt(cum[finishRaw] / 20f), 1, path.Count - 1);
			int count = Mathf.Max(6, Mathf.RoundToInt(finish * 20f / 430f));
			checkpoints = new int[count];
			for (int k = 1; k <= count; k++) checkpoints[k - 1] = Mathf.RoundToInt(k * finish / (float)count);
			return path;
		}

		private static void Line(List<Vector3> pts, Vector3 to)
		{
			Vector3 from = pts[pts.Count - 1];
			int n = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(from, to) / 10f));
			for (int i = 1; i <= n; i++) pts.Add(Vector3.Lerp(from, to, i / (float)n));
		}

		private static float RouteDistance(Vector3 p, out Vector3 nearest)
		{
			float best = float.MaxValue;
			nearest = _route[0];
			foreach (Vector3 q in _route)
			{
				float d = (q.x - p.x) * (q.x - p.x) + (q.z - p.z) * (q.z - p.z);
				if (d < best)
				{
					best = d;
					nearest = q;
				}
			}
			return Mathf.Sqrt(best);
		}

		// Buildings the (rounded) race line passes close to are kept well below it.
		private static float HeightCap(Rect lot)
		{
			float cap = 10000f;
			foreach (Vector3 p in _route)
			{
				float dx = Mathf.Max(lot.xMin - p.x, 0f, p.x - lot.xMax), dz = Mathf.Max(lot.yMin - p.z, 0f, p.z - lot.yMax);
				if (dx * dx + dz * dz < 30f * 30f) cap = Mathf.Min(cap, p.y - 45f);
			}
			return cap;
		}

		private static bool NearBridge(Rect r, float margin)
		{
			foreach (Bounds b in Bridges)
			{
				if (r.xMax + margin > b.min.x && r.xMin - margin < b.max.x && r.yMax + margin > b.min.z && r.yMin - margin < b.max.z) return true;
			}
			return false;
		}

		private static float BridgeMin(Rect r)
		{
			float h = 0f;
			foreach (Bounds b in Bridges)
			{
				if (r.xMax + 12f > b.min.x && r.xMin - 12f < b.max.x && r.yMax + 12f > b.min.z && r.yMin - 12f < b.max.z) h = Mathf.Max(h, b.max.y + 25f);
			}
			return h;
		}

		// ================================================================ city

		// Skybridges across avenues: three on the race line (two to fly under, one over) plus random ones at mixed
		// heights elsewhere, kept clear of the race line by 20 m vertically.
		private static void MakeBridges(Dictionary<Vector2Int, MB> core)
		{
			var specs = new[] { (x: 2f * Pitch, z: 7.5f * Pitch, under: true), (x: -2f * Pitch, z: 11.5f * Pitch, under: false), (x: 0f, z: 15.5f * Pitch, under: true) };
			foreach (var s in specs)
			{
				RouteDistance(new Vector3(s.x, 0f, s.z), out Vector3 near);
				float y0 = s.under ? near.y + 24f : near.y - 35f;
				AddBridge(core, s.x, s.z, y0, s.under);
			}
			for (int i = CoreI0 + 1; i <= CoreI1; i++)
			{
				for (int j = 1; j < CoreJ1; j++)
				{
					if (R() > 0.2f) continue;
					float x = i * Pitch, z = (j + 0.5f) * Pitch;
					if (Blocked.Exists(r => r.Contains(new Vector2(x, z)))) continue;
					float y0 = R(25f, Ceiling - 20f);
					bool clash = false;
					foreach (Vector3 p in _route)
					{
						if (Mathf.Abs(p.x - x) < HalfStreet + 20f && Mathf.Abs(p.z - z) < 40f && p.y > y0 - 20f && p.y < y0 + 29f) clash = true;
					}
					foreach (Bounds b in Bridges)
					{
						if (Mathf.Abs(b.center.x - x) < 1f && Mathf.Abs(b.center.z - z) < 1f) clash = true;
					}
					if (!clash) AddBridge(core, x, z, y0, R() < 0.5f);
				}
			}
		}

		private static void AddBridge(Dictionary<Vector2Int, MB> core, float x, float z, float y0, bool lightsBelow)
		{
			float y1 = y0 + 9f;
			var min = new Vector3(x - HalfStreet - 9f, y0, z - 7f);
			var max = new Vector3(x + HalfStreet + 9f, y1, z + 7f);
			var b = new Bounds();
			b.SetMinMax(min, max);
			Bridges.Add(b);
			float under = Mathf.Max(1f, y0 - 30f); // flying under the bridge is a stunt
			var u = new Bounds();
			u.SetMinMax(new Vector3(x - HalfStreet, under, z - 9f), new Vector3(x + HalfStreet, y0 - 0.5f, z + 9f));
			if (y0 > 12f) Tricks.Add((u, TrickZone.Kind.UnderBridge));
			MB mb = At(core, b.center);
			mb.Box(min, max, R() < 0.5f ? Slot.Mirror : Slot.MirrorGold, Slot.Roof, new Vector2(R(0f, 40f), 0f), true, Slot.Metal); // dark underside: chrome right above the neon tubes blew out
			Solid(min, max);
			float ny = lightsBelow ? y0 - 0.25f : y1 + 0.25f;
			Tube(mb, new Vector3(min.x + 9f, ny, min.z + 0.4f), new Vector3(max.x - 9f, ny, min.z + 0.4f), 0.35f, Slot.NeonCyan);
			Tube(mb, new Vector3(min.x + 9f, ny, max.z - 0.4f), new Vector3(max.x - 9f, ny, max.z - 0.4f), 0.35f, Slot.NeonCyan);
			Tube(mb, new Vector3(min.x + 9f, (y0 + y1) * 0.5f, min.z - 0.3f), new Vector3(max.x - 9f, (y0 + y1) * 0.5f, min.z - 0.3f), 0.3f, Slot.NeonPink);
			Tube(mb, new Vector3(min.x + 9f, (y0 + y1) * 0.5f, max.z + 0.3f), new Vector3(max.x - 9f, (y0 + y1) * 0.5f, max.z + 0.3f), 0.3f, Slot.NeonPink);
			if (Bridges.Count <= 3 || R() < 0.3f)
			{
				float ly = lightsBelow ? y0 - 3f : y1 + 3f;
				AddLight(new Vector3(x - 16f, ly, z), NeonColors[1], 9000f, 45f, 1f);
				AddLight(new Vector3(x + 16f, ly, z), NeonColors[1], 9000f, 45f, 1f);
			}
		}

		// ---------------------------------------------------------------- maze

		private static readonly List<Rect> Blocked = new List<Rect>(); // street segments closed by buildings (xz)

		private static bool RouteNear(Rect r, float margin)
		{
			foreach (Vector3 p in _route)
			{
				if (p.x > r.xMin - margin && p.x < r.xMax + margin && p.z > r.yMin - margin && p.z < r.yMax + margin) return true;
			}
			return false;
		}

		private static string _mazeStats = "";

		// Free-route racing would otherwise be "fly up any avenue", so buildings taller than the ceiling close street
		// segments, but never into a dead end. Row j is the cross street at z = j * Pitch; its open stretches join
		// neighbouring avenues into groups, and every group keeps at least one open avenue north (two for groups of five
		// or more), with at least three per row: from every intersection there is a way on without turning back.
		// Straights stay short: an avenue open for two blocks in a row is closed on the next unless a group needs it, so
		// racers keep cutting across. Some closed avenues are gates: a building over the street with a lit tunnel
		// through it, the risky way to keep going straight. The race line (autopilot, respawns), the start row and the
		// waterfront stay open.
		private const int MaxStraight = 2;

		private static void MakeBlockers(Dictionary<Vector2Int, MB> core)
		{
			Blocked.Clear();
			var rng = new Random(7406); // own sequence, so maze tweaks don't reshuffle the rest of the city
			int avenues = CoreI1 + 2 - CoreI0; // avenue k is at x = (CoreI0 + k) * Pitch
			int walls = 0, gates = 0, minNorth = avenues, longest = 0;
			var run = new int[avenues]; // open avenue segments in a row so far (gates don't count as open)
			Rect AvenueSeg(int k, int j)
			{
				float x = (CoreI0 + k) * Pitch;
				return Rect.MinMaxRect(x - HalfStreet - 2f, j * Pitch + HalfStreet + 8f, x + HalfStreet + 2f, (j + 1) * Pitch - HalfStreet - 8f);
			}
			for (int j = 0; j <= CoreJ1; j++)
			{
				// Cross street j: close some stretches between neighbouring avenues (not on the start row).
				var streetOpen = new bool[avenues - 1];
				for (int k = 0; k < avenues - 1; k++)
				{
					float x = (CoreI0 + k) * Pitch;
					Rect r = Rect.MinMaxRect(x + HalfStreet + 8f, j * Pitch - HalfStreet - 2f, x + Pitch - HalfStreet - 8f, j * Pitch + HalfStreet + 2f);
					streetOpen[k] = j == 0 || rng.NextDouble() > 0.3 || RouteNear(r, 25f);
					if (!streetOpen[k])
					{
						Blocker(core, r);
						walls++;
					}
				}
				if (j == CoreJ1) break; // the last blocks open onto the waterfront

				// Avenue segments north of row j: about half close at random, and so does any avenue already open for
				// MaxStraight blocks. Then reopen some (shortest straights first) until every group has its way on.
				var north = new bool[avenues];
				var route = new bool[avenues];
				for (int k = 0; k < avenues; k++)
				{
					route[k] = RouteNear(AvenueSeg(k, j), 25f);
					north[k] = route[k] || (run[k] < MaxStraight && rng.NextDouble() > 0.5);
				}
				int Pick(int from, int size) // a closed avenue in [from, from + size), preferring the shortest straight
				{
					int best = -1;
					for (int q = from; q < from + size; q++)
					{
						if (!north[q] && (best < 0 || run[q] < run[best] || (run[q] == run[best] && rng.NextDouble() < 0.5))) best = q;
					}
					return best;
				}
				for (int g0 = 0, k = 0; k < avenues; k++)
				{
					if (k < avenues - 1 && streetOpen[k]) continue; // the group carries on east
					int size = k - g0 + 1, open = 0;
					for (int q = g0; q <= k; q++) if (north[q]) open++;
					for (int need = Mathf.Min(size >= 5 ? 2 : 1, size); open < need; open++) north[Pick(g0, size)] = true;
					g0 = k + 1;
				}
				int count = 0;
				foreach (bool o in north) if (o) count++;
				for (; count < 3; count++) north[Pick(0, avenues)] = true;
				minNorth = Mathf.Min(minNorth, count);
				for (int k = 0; k < avenues; k++)
				{
					if (north[k])
					{
						run[k]++;
						if (!route[k]) longest = Mathf.Max(longest, run[k]);
						continue;
					}
					run[k] = 0;
					if (rng.NextDouble() < 0.3) { Gate(core, AvenueSeg(k, j)); gates++; }
					else { Blocker(core, AvenueSeg(k, j)); walls++; }
				}
			}
			_mazeStats = "maze: " + walls + " walls, " + gates + " gates, " + minNorth + "+ open avenues north of every row, straights off the race line up to "
				+ longest + " blocks, no dead ends";
		}

		// A building over the avenue with a 44 x 38 m tunnel through it at a random height: neon portal frames, light
		// strips along the tunnel and a polished floor; collides like any other building.
		private static void Gate(Dictionary<Vector2Int, MB> core, Rect r)
		{
			Blocked.Add(r);
			float h = R(175f, 300f), cx = r.center.x, hw = 22f, y0 = R(30f, 85f), y1 = y0 + 38f;
			Slot[] styles = { Slot.Housing, Slot.Dim, Slot.Office, Slot.Concrete };
			Slot wall = styles[_rng.Next(styles.Length)];
			var off = new Vector2(_rng.Next(0, 16) * ColW, _rng.Next(0, 32) * FloorH);
			MB mb = At(core, new Vector3(cx, 0f, r.center.y));
			void Part(Vector3 min, Vector3 max, Slot top, bool bottom)
			{
				mb.Box(min, max, wall, top, off, bottom, Slot.Metal);
				Solid(min, max);
			}
			Part(new Vector3(r.xMin, 0f, r.yMin), new Vector3(r.xMax, y0, r.yMax), Slot.Marble, false); // under the tunnel
			Part(new Vector3(r.xMin, y1, r.yMin), new Vector3(r.xMax, h, r.yMax), Slot.Roof, true);     // over it
			Part(new Vector3(r.xMin, y0, r.yMin), new Vector3(cx - hw, y1, r.yMax), Slot.Roof, false);  // side walls
			Part(new Vector3(cx + hw, y0, r.yMin), new Vector3(r.xMax, y1, r.yMax), Slot.Roof, false);
			var tunnel = new Bounds();
			tunnel.SetMinMax(new Vector3(cx - hw + 1f, y0 + 1f, r.yMin), new Vector3(cx + hw - 1f, y1 - 1f, r.yMax));
			Tricks.Add((tunnel, TrickZone.Kind.Gate));
			int n = _rng.Next(NeonSlots.Length);
			foreach (float z in new[] { r.yMin - 0.4f, r.yMax + 0.4f })
			{
				Tube(mb, new Vector3(cx - hw, y0, z), new Vector3(cx + hw, y0, z), 0.7f, NeonSlots[n]);
				Tube(mb, new Vector3(cx - hw, y1, z), new Vector3(cx + hw, y1, z), 0.7f, NeonSlots[n]);
				Tube(mb, new Vector3(cx - hw, y0, z), new Vector3(cx - hw, y1, z), 0.7f, NeonSlots[n]);
				Tube(mb, new Vector3(cx + hw, y0, z), new Vector3(cx + hw, y1, z), 0.7f, NeonSlots[n]);
			}
			Tube(mb, new Vector3(cx - hw + 0.4f, y1 - 0.4f, r.yMin), new Vector3(cx - hw + 0.4f, y1 - 0.4f, r.yMax), 0.4f, NeonSlots[n]);
			Tube(mb, new Vector3(cx + hw - 0.4f, y1 - 0.4f, r.yMin), new Vector3(cx + hw - 0.4f, y1 - 0.4f, r.yMax), 0.4f, NeonSlots[n]);
			AddLight(new Vector3(cx, y1 - 5f, r.center.y), NeonColors[n], 14000f, 70f, 0.6f);
			Ring(mb, new Vector3(r.xMin, 0f, r.yMin), new Vector3(r.xMax, h, r.yMax), R(y1 + 12f, Ceiling - 5f), 0.3f, 0.5f, NeonSlots[_rng.Next(NeonSlots.Length)]);
		}

		private static void Blocker(Dictionary<Vector2Int, MB> core, Rect r)
		{
			Blocked.Add(r);
			float h = R(175f, 300f);
			var min = new Vector3(r.xMin, 0f, r.yMin);
			var max = new Vector3(r.xMax, h, r.yMax);
			Slot[] styles = { Slot.Housing, Slot.Dim, Slot.Office, Slot.Concrete };
			MB mb = At(core, (min + max) * 0.5f);
			mb.Box(min, max, styles[_rng.Next(styles.Length)], Slot.Roof, new Vector2(_rng.Next(0, 16) * ColW, _rng.Next(0, 32) * FloorH));
			Solid(min, max);
			// A neon band at street level and one higher up, so closed streets read from a distance.
			Ring(mb, min, max, R(12f, 24f), 0.3f, 0.5f, NeonSlots[_rng.Next(NeonSlots.Length)]);
			if (R() < 0.6f) Ring(mb, min, max, R(60f, Ceiling - 10f), 0.3f, 0.5f, NeonSlots[_rng.Next(NeonSlots.Length)]);
			if (R() < 0.5f) Ring(mb, min, max, h + 0.2f, -0.5f, 0.4f, NeonSlots[_rng.Next(NeonSlots.Length)]);
		}

		private static void MakeBlocks(Dictionary<Vector2Int, MB> core, MB[] flicker)
		{
			for (int i = CoreI0; i <= CoreI1; i++)
			{
				for (int j = CoreJ0; j <= CoreJ1; j++)
				{
					float bx0 = i * Pitch + HalfStreet, bx1 = (i + 1) * Pitch - HalfStreet;
					float bz0 = j * Pitch + HalfStreet, bz1 = (j + 1) * Pitch - HalfStreet;
					MB mb = At(core, new Vector3((bx0 + bx1) * 0.5f, 0f, (bz0 + bz1) * 0.5f));
					mb.Box(new Vector3(bx0, 0f, bz0), new Vector3(bx1, 0.25f, bz1), Slot.Roof, Slot.Marble, Vector2.zero);
					var block = Rect.MinMaxRect(bx0, bz0, bx1, bz1);
					foreach (Rect lot in SplitLots(Rect.MinMaxRect(bx0 + 4f, bz0 + 4f, bx1 - 4f, bz1 - 4f), NearBridge(block, 14f))) Tower(mb, lot, flicker);
				}
			}
		}

		private static List<Rect> SplitLots(Rect r, bool single)
		{
			var lots = new List<Rect>();
			float roll = R();
			if (single || roll < 0.3f)
			{
				lots.Add(r);
			}
			else if (roll < 0.72f)
			{
				float f = R(0.4f, 0.6f);
				if (R() < 0.5f)
				{
					float x = Mathf.Lerp(r.xMin, r.xMax, f);
					lots.Add(Rect.MinMaxRect(r.xMin, r.yMin, x - 2f, r.yMax));
					lots.Add(Rect.MinMaxRect(x + 2f, r.yMin, r.xMax, r.yMax));
				}
				else
				{
					float z = Mathf.Lerp(r.yMin, r.yMax, f);
					lots.Add(Rect.MinMaxRect(r.xMin, r.yMin, r.xMax, z - 2f));
					lots.Add(Rect.MinMaxRect(r.xMin, z + 2f, r.xMax, r.yMax));
				}
			}
			else
			{
				float x = Mathf.Lerp(r.xMin, r.xMax, R(0.4f, 0.6f)), z = Mathf.Lerp(r.yMin, r.yMax, R(0.4f, 0.6f));
				lots.Add(Rect.MinMaxRect(r.xMin, r.yMin, x - 2f, z - 2f));
				lots.Add(Rect.MinMaxRect(x + 2f, r.yMin, r.xMax, z - 2f));
				lots.Add(Rect.MinMaxRect(r.xMin, z + 2f, x - 2f, r.yMax));
				lots.Add(Rect.MinMaxRect(x + 2f, z + 2f, r.xMax, r.yMax));
			}
			return lots;
		}

		private static Slot PickStyle(float h)
		{
			float r = R();
			// Mirror-glass skyscrapers dominate the skyline; LED-banded glass, offices and hab blocks fill in.
			if (h > 150f) return r < 0.3f ? Slot.Mirror : r < 0.45f ? Slot.MirrorGold : r < 0.6f ? Slot.Glass : r < 0.71f ? Slot.GlassPink : r < 0.85f ? Slot.Office : r < 0.95f ? Slot.Housing : Slot.Dim;
			return r < 0.16f ? Slot.Mirror : r < 0.24f ? Slot.MirrorGold : r < 0.33f ? Slot.Glass : r < 0.4f ? Slot.GlassPink : r < 0.56f ? Slot.Office : r < 0.85f ? Slot.Housing : Slot.Dim;
		}

		private static void Tower(MB mb, Rect lot, MB[] flicker)
		{
			var center = new Vector3(lot.center.x, 0f, lot.center.y);
			float dRoute = RouteDistance(center, out Vector3 nearest);
			float downtown = WorldNoise.SmoothStep(560f, 140f, dRoute);
			float h = Mathf.Lerp(28f + 110f * Mathf.Pow(R(), 1.6f), 130f + 300f * Mathf.Pow(R(), 1.2f), downtown);
			h = Mathf.Max(Mathf.Max(h, BridgeMin(lot)), MinTower); // taller than the ceiling: no flying over the blocks
			float cap = HeightCap(lot);
			if (h > cap) h = cap;
			if (h < 12f) return; // open plaza on the inside of a corner

			Slot style = PickStyle(h);
			var off = new Vector2(_rng.Next(0, 16) * ColW, _rng.Next(0, 32) * FloorH);
			// Cyberpunk massing: round and chamfered (octagonal) glass towers, ledged housing blocks, chrome-finned curtain walls.
			bool glassy = style == Slot.Glass || style == Slot.GlassPink || style == Slot.Mirror || style == Slot.MirrorGold;
			bool round = h > 120f && glassy && R() < 0.24f;
			bool octagon = !round && h > 90f && style != Slot.Housing && R() < 0.4f;
			float chamferRatio = R(0.14f, 0.24f);
			bool ledges = !octagon && !round && (style == Slot.Housing || style == Slot.Dim || style == Slot.Office) && R() < 0.6f;
			bool fins = !octagon && !round && glassy && R() < 0.35f;
			float x0 = lot.xMin, x1 = lot.xMax, z0 = lot.yMin, z1 = lot.yMax;
			var tiers = new List<Bounds>();
			float y = 0.25f;
			float podium = h > 60f && lot.width > 36f && lot.height > 36f ? R(10f, 22f) : 0f;
			if (podium > 0f)
			{
				tiers.Add(B(x0, y, z0, x1, podium, z1));
				y = podium;
				float ins = R(4f, 9f);
				x0 += ins; x1 -= ins; z0 += ins; z1 -= ins;
			}
			int setbacks = h > 150f ? 2 : h > 70f ? 1 : 0;
			for (int k = 0; k <= setbacks; k++)
			{
				float top = k == setbacks ? h : y + (h - y) * R(0.45f, 0.7f);
				tiers.Add(B(x0, y, z0, x1, top, z1));
				y = top;
				float ins = Mathf.Min(R(3f, 8f), Mathf.Min(x1 - x0, z1 - z0) * 0.18f);
				x0 += ins; x1 -= ins; z0 += ins; z1 -= ins;
			}
			for (int k = 0; k < tiers.Count; k++)
			{
				bool isPodium = podium > 0f && k == 0;
				Slot s = isPodium ? Slot.Concrete : style;
				Bounds t = tiers[k];
				Vector2 o = off + new Vector2(k * 7f * ColW, 0f);
				if (round && !isPodium)
				{
					// Faceted round tower (24 sides) with a capsule collider.
					float r = Mathf.Min(t.size.x, t.size.z) * 0.5f;
					mb.Prism(Circle(t, r, 24), t.min.y, t.max.y, s, Slot.Roof, o);
					SolidCylinder(new Vector3(t.center.x, t.min.y, t.center.z), r, t.size.y);
					continue;
				}
				if (octagon && !isPodium)
				{
					float c = Mathf.Min(t.size.x, t.size.z) * chamferRatio;
					mb.Prism(Octagon(t, c), t.min.y, t.max.y, s, Slot.Roof, o);
					Solid(new Vector3(t.min.x + c, t.min.y, t.min.z), new Vector3(t.max.x - c, t.max.y, t.max.z));
					Solid(new Vector3(t.min.x, t.min.y, t.min.z + c), new Vector3(t.max.x, t.max.y, t.max.z - c));
					continue;
				}
				mb.Box(t.min, t.max, s, isPodium ? Slot.Marble : Slot.Roof, o);
				Solid(t.min, t.max);
				if (isPodium) continue;
				if (ledges)
				{
					for (float ly = t.min.y + 4f * FloorH; ly < t.max.y - 3f; ly += 4f * FloorH) Ledge(mb, t, ly);
				}
				if (fins) Fins(mb, t);
			}
			Bounds topTier = tiers[tiers.Count - 1];

			// Glowing glass spire on some of the tallest towers (away from the corner height caps).
			bool spire = h > 170f && cap > 9000f && (glassy || style == Slot.Office) && R() < 0.4f;
			if (spire)
			{
				float sh = R(25f, 70f), ins = Mathf.Min(topTier.size.x, topTier.size.z) * 0.18f;
				Bounds sb = B(topTier.min.x + ins, topTier.max.y, topTier.min.z + ins, topTier.max.x - ins, topTier.max.y + sh, topTier.max.z - ins);
				Slot spireSkin = R() < 0.5f ? Slot.Chrome : style == Slot.Office ? Slot.GlassPink : style;
				mb.Prism(Octagon(sb, Mathf.Min(sb.size.x, sb.size.z) * 0.3f), sb.min.y, sb.max.y, spireSkin, Slot.Chrome, off, 0.12f);
				Solid(sb.min + new Vector3(sb.size.x * 0.25f, 0f, sb.size.z * 0.25f), new Vector3(sb.max.x - sb.size.x * 0.25f, sb.min.y + sh * 0.4f, sb.max.z - sb.size.z * 0.25f));
			}

			// Roof furniture: mechanical penthouse, AC units, antenna with beacon.
			if (!spire && topTier.size.x > 14f && topTier.size.z > 14f && h + 10f < cap)
			{
				float px = topTier.size.x * R(0.2f, 0.35f), pz = topTier.size.z * R(0.2f, 0.35f);
				Vector3 c = topTier.center;
				var pmin = new Vector3(c.x - px, topTier.max.y, c.z - pz);
				var pmax = new Vector3(c.x + px, topTier.max.y + R(4f, 9f), c.z + pz);
				mb.Box(pmin, pmax, Slot.Concrete, Slot.Roof, off);
				for (int k = _rng.Next(0, 3); k > 0; k--)
				{
					var a = new Vector3(R(topTier.min.x + 2f, topTier.max.x - 5f), topTier.max.y, R(topTier.min.z + 2f, topTier.max.z - 5f));
					mb.Box(a, a + new Vector3(3f, 1.8f, 3f), Slot.Metal, Slot.Metal, Vector2.zero);
				}
				if (h > 200f && R() < 0.4f)
				{
					float ah = R(15f, 45f);
					var am = new Vector3(c.x - 0.35f, pmax.y, c.z - 0.35f);
					mb.Box(am, am + new Vector3(0.7f, ah, 0.7f), Slot.Metal, Slot.Metal, Vector2.zero);
					var bm = new Vector3(c.x - 0.6f, pmax.y + ah, c.z - 0.6f);
					mb.Box(bm, bm + new Vector3(1.2f, 1.2f, 1.2f), Slot.NeonPink, Slot.NeonPink, Vector2.zero);
				}
			}

			if (dRoute > 320f) return;
			// Neon: rooftop outline, horizontal rings, podium band, blade signs, billboards.
			if (!octagon && !round && R() < 0.5f) Ring(mb, topTier.min, topTier.max, topTier.max.y + 0.2f, -0.5f, 0.4f, NeonSlots[_rng.Next(NeonSlots.Length)]);
			if (!octagon && !round && h > 60f && R() < 0.22f)
			{
				Bounds t = tiers[Mathf.Min(tiers.Count - 1, podium > 0f ? 1 : 0)];
				Ring(mb, t.min, t.max, Mathf.Lerp(t.min.y, t.max.y, R(0.3f, 0.9f)), 0.3f, 0.45f, NeonSlots[_rng.Next(NeonSlots.Length)]);
			}
			if (podium > 0f && R() < 0.6f) Ring(mb, tiers[0].min, tiers[0].max, podium - 1.5f, 0.3f, 0.5f, NeonSlots[_rng.Next(NeonSlots.Length)]);
			if (!octagon && !round && h > 100f && R() < 0.25f)
			{
				// Vertical light strips up two opposite corners of the main tier.
				Bounds t = tiers[podium > 0f ? 1 : 0];
				Slot s = NeonSlots[_rng.Next(NeonSlots.Length)];
				Tube(mb, new Vector3(t.min.x - 0.25f, t.min.y + 1f, t.min.z - 0.25f), new Vector3(t.min.x - 0.25f, t.max.y - 0.5f, t.min.z - 0.25f), 0.45f, s);
				Tube(mb, new Vector3(t.max.x + 0.25f, t.min.y + 1f, t.max.z + 0.25f), new Vector3(t.max.x + 0.25f, t.max.y - 0.5f, t.max.z + 0.25f), 0.45f, s);
			}

			// The face of the main tier that looks towards the race line.
			Bounds main = tiers[podium > 0f ? 1 : 0];
			Vector3 toRoute = Flat(nearest - main.center);
			Vector3 n = Mathf.Abs(toRoute.x) > Mathf.Abs(toRoute.z) ? new Vector3(Mathf.Sign(toRoute.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(toRoute.z));
			Vector3 along = Vector3.Cross(Vector3.up, n);
			// Flat part of the face (chamfered towers lose the corners).
			float faceHalf = round ? 0f : Mathf.Abs(Vector3.Dot(main.extents, along)) - (octagon ? Mathf.Min(main.size.x, main.size.z) * chamferRatio : 0f);
			Vector3 faceCenter = new Vector3(main.center.x, 0f, main.center.z) + n * Mathf.Abs(Vector3.Dot(main.extents, n));
			float signSide = R() < 0.5f ? -1f : 1f;
			if (dRoute < 220f && faceHalf > 6f && R() < 0.6f)
			{
				float bottom = podium > 0f ? podium + 2f : R(6f, 14f);
				float height = Mathf.Min(R(14f, 36f), main.max.y - bottom - 3f);
				if (height > 8f)
				{
					float side = signSide;
					Vector3 anchor = faceCenter + along * side * (faceHalf - 3f) + Vector3.up * bottom;
					int ci = _rng.Next(NeonSlots.Length);
					bool flick = R() < 0.15f;
					BladeSign(flick ? flicker[_rng.Next(flicker.Length)] : mb, mb, anchor, n, height, NeonSlots[ci]);
					if (_signLights < 60)
					{
						_signLights++;
						AddLight(anchor + n * 3.5f + Vector3.up * (height * 0.5f), NeonColors[ci], 5000f, 32f, 1f);
					}
				}
			}
			bool boarded = false;
			if (h > 90f && dRoute < 220f && Boards.Count < 40 && R() < 0.3f && faceHalf > 14f)
			{
				boarded = true;
				float w = Mathf.Min(30f, faceHalf * 2f - 8f), bh = w * 0.56f;
				float by = Mathf.Clamp(nearest.y + R(-10f, 25f), Mathf.Max(podium, 10f) + bh * 0.5f + 2f, main.max.y - bh * 0.5f - 4f);
				if (by + bh * 0.5f < main.max.y)
				{
					Vector3 c = faceCenter + Vector3.up * by;
					Vector3 half = along * (w * 0.5f + 0.6f) + Vector3.up * (bh * 0.5f + 0.6f);
					Vector3 a = c - half, b = c + half + n * 0.5f;
					mb.Box(Vector3.Min(a, b), Vector3.Max(a, b), Slot.Metal, Slot.Metal, Vector2.zero);
					Boards.Add(new BoardSpec { center = c + n * 0.56f, normal = n, w = w, h = bh, mat = Boards.Count % 3 });
					AddLight(c + n * 8f, new Color(0.8f, 0.5f, 1f), 7000f, 40f, 0.6f);
				}
			}
			// Tall holographic banner on the other side of the face from the blade sign.
			if (!boarded && h > 80f && dRoute < 260f && Boards.Count < 40 && R() < 0.4f && faceHalf > 9f)
			{
				float bw = R(6f, 9f), bh = Mathf.Min(R(24f, 44f), main.max.y - main.min.y - 10f);
				if (bh > 14f)
				{
					float by = Mathf.Clamp(nearest.y + R(-25f, 15f), main.min.y + bh * 0.5f + 3f, main.max.y - bh * 0.5f - 3f);
					Vector3 c = faceCenter + along * (-signSide * (faceHalf - bw * 0.5f - 2f)) + Vector3.up * by;
					Vector3 half = along * (bw * 0.5f + 0.5f) + Vector3.up * (bh * 0.5f + 0.5f);
					Vector3 a = c - half, b = c + half + n * 0.5f;
					mb.Box(Vector3.Min(a, b), Vector3.Max(a, b), Slot.Metal, Slot.Metal, Vector2.zero);
					Boards.Add(new BoardSpec { center = c + n * 0.56f, normal = n, w = bw, h = bh, mat = 3 + Boards.Count % 2 });
					if (_signLights < 90)
					{
						_signLights++;
						AddLight(c + n * 6f, new Color(0.4f, 0.8f, 1f), 5000f, 32f, 0.6f);
					}
				}
			}
		}

		private static Vector2[] Circle(Bounds t, float r, int seg)
		{
			var p = new Vector2[seg];
			for (int i = 0; i < seg; i++)
			{
				float a = i * Mathf.PI * 2f / seg;
				p[i] = new Vector2(t.center.x + Mathf.Cos(a) * r, t.center.z + Mathf.Sin(a) * r);
			}
			return p;
		}

		private static Vector2[] Octagon(Bounds t, float c)
		{
			float x0 = t.min.x, x1 = t.max.x, z0 = t.min.z, z1 = t.max.z;
			return new[]
			{
				new Vector2(x0 + c, z0), new Vector2(x1 - c, z0), new Vector2(x1, z0 + c), new Vector2(x1, z1 - c),
				new Vector2(x1 - c, z1), new Vector2(x0 + c, z1), new Vector2(x0, z1 - c), new Vector2(x0, z0 + c)
			};
		}

		// Protruding floor slab around a tier.
		private static void Ledge(MB mb, Bounds t, float y)
		{
			const float d = 0.8f, th = 0.4f;
			mb.Box(new Vector3(t.min.x - d, y, t.min.z - d), new Vector3(t.max.x + d, y + th, t.min.z), Slot.Roof, Slot.Roof, Vector2.zero);
			mb.Box(new Vector3(t.min.x - d, y, t.max.z), new Vector3(t.max.x + d, y + th, t.max.z + d), Slot.Roof, Slot.Roof, Vector2.zero);
			mb.Box(new Vector3(t.min.x - d, y, t.min.z), new Vector3(t.min.x, y + th, t.max.z), Slot.Roof, Slot.Roof, Vector2.zero);
			mb.Box(new Vector3(t.max.x, y, t.min.z), new Vector3(t.max.x + d, y + th, t.max.z), Slot.Roof, Slot.Roof, Vector2.zero);
		}

		// Vertical metal fins every 6 m on all four faces of a tier.
		private static void Fins(MB mb, Bounds t)
		{
			const float step = 6f, th = 0.35f, d = 1.1f;
			float y0 = t.min.y + 1f, y1 = t.max.y - 0.5f;
			for (float x = t.min.x + 3f; x < t.max.x - 2f; x += step)
			{
				mb.Box(new Vector3(x - th * 0.5f, y0, t.min.z - d), new Vector3(x + th * 0.5f, y1, t.min.z), Slot.Chrome, Slot.Chrome, Vector2.zero);
				mb.Box(new Vector3(x - th * 0.5f, y0, t.max.z), new Vector3(x + th * 0.5f, y1, t.max.z + d), Slot.Chrome, Slot.Chrome, Vector2.zero);
			}
			for (float z = t.min.z + 3f; z < t.max.z - 2f; z += step)
			{
				mb.Box(new Vector3(t.min.x - d, y0, z - th * 0.5f), new Vector3(t.min.x, y1, z + th * 0.5f), Slot.Chrome, Slot.Chrome, Vector2.zero);
				mb.Box(new Vector3(t.max.x, y0, z - th * 0.5f), new Vector3(t.max.x + d, y1, z + th * 0.5f), Slot.Chrome, Slot.Chrome, Vector2.zero);
			}
		}

		private static Bounds B(float x0, float y0, float z0, float x1, float y1, float z1)
		{
			var b = new Bounds();
			b.SetMinMax(new Vector3(x0, y0, z0), new Vector3(x1, y1, z1));
			return b;
		}

		// Vertical blade sign sticking out of a facade: dark plate with neon glyph strokes and outline on both faces.
		private static void BladeSign(MB neonMb, MB mb, Vector3 anchor, Vector3 n, float height, Slot neon)
		{
			Vector3 along = Vector3.Cross(Vector3.up, n);
			const float depth = 3.4f, thick = 0.7f;
			Vector3 a = anchor + n * 0.3f - along * (thick * 0.5f);
			Vector3 b = anchor + n * (0.3f + depth) + along * (thick * 0.5f) + Vector3.up * height;
			mb.Box(Vector3.Min(a, b), Vector3.Max(a, b), Slot.Metal, Slot.Metal, Vector2.zero, true);
			Solid(Vector3.Min(a, b), Vector3.Max(a, b));
			float cell = depth * 0.72f;
			int rows = Mathf.Max(1, Mathf.FloorToInt((height - 1f) / (cell * 1.12f)));
			for (int s = -1; s <= 1; s += 2)
			{
				Vector3 face = anchor + n * 0.3f + along * (s * (thick * 0.5f + 0.08f));
				// outline
				Tube(neonMb, face + n * 0.2f + Vector3.up * 0.2f, face + n * (depth - 0.2f) + Vector3.up * 0.2f, 0.16f, neon);
				Tube(neonMb, face + n * 0.2f + Vector3.up * (height - 0.2f), face + n * (depth - 0.2f) + Vector3.up * (height - 0.2f), 0.16f, neon);
				Tube(neonMb, face + n * (depth - 0.2f) + Vector3.up * 0.2f, face + n * (depth - 0.2f) + Vector3.up * (height - 0.2f), 0.16f, neon);
				// glyph strokes per cell
				for (int r = 0; r < rows; r++)
				{
					Vector3 o = face + n * ((depth - cell) * 0.5f) + Vector3.up * (0.7f + r * cell * 1.12f);
					int strokes = 2 + _rng.Next(3);
					for (int k = 0; k < strokes; k++)
					{
						float u = R(0.05f, 0.75f), v = R(0.05f, 0.75f), len = R(0.3f, 0.95f);
						Vector3 p0 = o + n * (u * cell) + Vector3.up * (v * cell);
						Vector3 p1 = R() < 0.5f ? p0 + n * ((0.95f - u) * len * cell) : p0 + Vector3.up * ((0.95f - v) * len * cell);
						Tube(neonMb, p0, p1, 0.17f, neon);
					}
				}
			}
		}

		private static void MakeStreets(Dictionary<Vector2Int, MB> core)
		{
			// Ground (wet asphalt) in 180 m cells over the whole land area.
			for (int i = -18; i <= 18; i++)
			{
				for (int j = -14; j <= 23; j++)
				{
					float x0 = i * Pitch, z0 = j * Pitch, x1 = x0 + Pitch, z1 = Mathf.Min(z0 + Pitch, WaterZ);
					if (z0 >= WaterZ) continue;
					At(core, new Vector3(x0 + 90f, 0f, z0 + 90f)).Flat(x0, z0, x1, z1, 0f, Slot.Pavement, 32f);
				}
			}
			// Centre and edge markings on the core streets.
			for (int i = CoreI0; i <= CoreI1 + 1; i++)
			{
				float x = i * Pitch;
				for (float z = CoreJ0 * Pitch; z < WaterZ - 20f; z += 12f)
				{
					float local = Mathf.Repeat(z, Pitch);
					if (local < HalfStreet + 2f || local > Pitch - HalfStreet - 8f) continue;
					At(core, new Vector3(x, 0f, z)).Flat(x - 0.15f, z, x + 0.15f, z + 6f, 0.03f, Slot.Marking, 4f);
					foreach (float e in new[] { -HalfStreet + 3f, HalfStreet - 3f })
						At(core, new Vector3(x, 0f, z)).Flat(x + e - 0.1f, z, x + e + 0.1f, z + 12f, 0.03f, Slot.Marking, 4f);
				}
			}
			for (int j = CoreJ0; j <= CoreJ1 + 1; j++)
			{
				float z = j * Pitch;
				for (float x = CoreI0 * Pitch; x < (CoreI1 + 1) * Pitch; x += 12f)
				{
					float local = Mathf.Repeat(x, Pitch);
					if (local < HalfStreet + 2f || local > Pitch - HalfStreet - 8f) continue;
					At(core, new Vector3(x, 0f, z)).Flat(x, z - 0.15f, x + 6f, z + 0.15f, 0.03f, Slot.Marking, 4f);
				}
			}
			// Street lamps along the race avenues (a real light every 90 m, alternating sides).
			for (int k = 0; k < Waypoints.Length - 1; k++)
			{
				Vector3 a = Flat(Waypoints[k]), b = Flat(Waypoints[k + 1]);
				Vector3 dir = (b - a).normalized, side = Vector3.Cross(Vector3.up, dir);
				float len = Vector3.Distance(a, b);
				int n = 0;
				for (float s = k == 0 ? 0f : 70f; s < len - 70f; s += 45f, n++)
				{
					Vector3 p = a + dir * s;
					if (p.z > WaterZ - 30f) break;
					for (int sg = -1; sg <= 1; sg += 2)
					{
						Vector3 basePos = p + side * (sg * (HalfStreet - 2.5f));
						bool lit = sg > 0 ? n % 4 == 0 : n % 4 == 2;
						Lamp(At(core, basePos), basePos, -side * sg, lit);
					}
				}
			}
		}

		private static void Lamp(MB mb, Vector3 p, Vector3 inward, bool light)
		{
			mb.Box(p + new Vector3(-0.15f, 0f, -0.15f), p + new Vector3(0.15f, 9f, 0.15f), Slot.Metal, Slot.Metal, Vector2.zero);
			Tube(mb, p + Vector3.up * 9f, p + Vector3.up * 9f + inward * 2.6f, 0.18f, Slot.Metal);
			Vector3 h = p + Vector3.up * 8.85f + inward * 2.7f;
			mb.Box(h - new Vector3(0.45f, 0.12f, 0.45f), h + new Vector3(0.45f, 0.12f, 0.45f), Slot.Metal, Slot.Metal, Vector2.zero, true, Slot.Lamp);
			if (light) AddLight(h - Vector3.up * 0.6f, new Color(1f, 0.72f, 0.45f), 14000f, 42f, 0.5f);
		}

		private static void MakeBackdrop(Dictionary<Vector2Int, MB> far)
		{
			for (int i = -18; i <= 18; i++)
			{
				for (int j = -14; j <= 22; j++)
				{
					if (i >= CoreI0 && i <= CoreI1 && j >= CoreJ0 && j <= CoreJ1) continue;
					float bx0 = i * Pitch + HalfStreet, bx1 = (i + 1) * Pitch - HalfStreet;
					float bz0 = j * Pitch + HalfStreet, bz1 = (j + 1) * Pitch - HalfStreet;
					var c = new Vector3((bx0 + bx1) * 0.5f, 0f, (bz0 + bz1) * 0.5f);
					MB mb = At(far, c);
					float dist = new Vector2(c.x - 90f, c.z - 1900f).magnitude;
					float hmax = Mathf.Lerp(190f, 45f, Mathf.InverseLerp(1200f, 3400f, dist));
					FarBuildings(mb, Rect.MinMaxRect(bx0, bz0, bx1, bz1), hmax);
				}
			}
			// The far shore across the harbor: a dense skyline with a cluster of supertall towers.
			for (int i = -24; i <= 24; i++)
			{
				for (int j = 36; j <= 45; j++)
				{
					float bx0 = i * Pitch + HalfStreet, bx1 = (i + 1) * Pitch - HalfStreet;
					float bz0 = j * Pitch + HalfStreet, bz1 = (j + 1) * Pitch - HalfStreet;
					if (bz0 < FarShoreZ + 30f) continue;
					var c = new Vector3((bx0 + bx1) * 0.5f, 0f, (bz0 + bz1) * 0.5f);
					float hmax = 40f + 260f * Mathf.Exp(-(c.x - 400f) * (c.x - 400f) / (1400f * 1400f)) * (j < 40 ? 1f : 0.7f);
					FarBuildings(At(far, c), Rect.MinMaxRect(bx0, bz0, bx1, bz1), hmax);
				}
			}
		}

		private static void FarBuildings(MB mb, Rect block, float hmax)
		{
			int count = R() < 0.5f ? 1 : 2;
			for (int k = 0; k < count; k++)
			{
				Rect r = block;
				if (count == 2)
				{
					float x = Mathf.Lerp(block.xMin, block.xMax, 0.5f);
					r = k == 0 ? Rect.MinMaxRect(block.xMin, block.yMin, x - 3f, block.yMax) : Rect.MinMaxRect(x + 3f, block.yMin, block.xMax, block.yMax);
				}
				float ix = Mathf.Min(R(4f, 16f), r.width * 0.3f), iz = Mathf.Min(R(4f, 16f), r.height * 0.3f);
				float h = hmax * (0.25f + 0.75f * Mathf.Pow(R(), 1.5f));
				var min = new Vector3(r.xMin + ix, 0f, r.yMin + iz);
				var max = new Vector3(r.xMax - ix, h, r.yMax - iz);
				mb.Box(min, max, PickStyle(h), Slot.Roof, new Vector2(_rng.Next(0, 16) * ColW, _rng.Next(0, 32) * FloorH));
				if (h > 110f && R() < 0.3f)
				{
					Vector3 c = (min + max) * 0.5f;
					mb.Box(new Vector3(c.x - 0.8f, h, c.z - 0.8f), new Vector3(c.x + 0.8f, h + 1.6f, c.z + 0.8f), Slot.NeonPink, Slot.NeonPink, Vector2.zero);
				}
			}
		}

		private static void MakeHarbor(Dictionary<Vector2Int, MB> core, Dictionary<Vector2Int, MB> far)
		{
			// Quay wall and promenade lamps along the waterfront, water across the bay.
			for (float x = -18f * Pitch; x < 19f * Pitch; x += 360f)
			{
				MB mb = At(core, new Vector3(x + 180f, 0f, WaterZ));
				mb.Box(new Vector3(x, -3f, WaterZ - 1f), new Vector3(x + 360f, 0f, WaterZ), Slot.Concrete, Slot.Roof, Vector2.zero);
				for (float lx = x + 20f; lx < x + 360f; lx += 40f)
				{
					var p = new Vector3(lx, 0f, WaterZ - 4f);
					mb.Box(p + new Vector3(-0.12f, 0f, -0.12f), p + new Vector3(0.12f, 5f, 0.12f), Slot.Metal, Slot.Metal, Vector2.zero);
					mb.Box(p + new Vector3(-0.35f, 5f, -0.35f), p + new Vector3(0.35f, 5.6f, 0.35f), Slot.Lamp, Slot.Lamp, Vector2.zero);
				}
			}
			for (float x = 180f - 640f; x <= 180f + 640f; x += 160f) AddLight(new Vector3(x, 5f, WaterZ - 4f), new Color(1f, 0.75f, 0.5f), 9000f, 30f, 0.4f);
			for (float x = -7000f; x < 7000f; x += 1000f)
			{
				for (float z = WaterZ; z < FarShoreZ; z += 1000f)
				{
					At(core, new Vector3(x + 500f, 0f, z + 500f)).Flat(x, z, x + 1000f, Mathf.Min(z + 1000f, FarShoreZ), -3f, Slot.Water, 40f);
				}
				// Far shore land and its quay.
				for (float z = FarShoreZ; z < 9000f; z += 1000f)
				{
					At(far, new Vector3(x + 500f, 0f, z + 500f)).Flat(x, z, x + 1000f, Mathf.Min(z + 1000f, 9000f), 0f, Slot.Pavement, 32f);
				}
				At(far, new Vector3(x + 500f, 0f, FarShoreZ)).Box(new Vector3(x, -3f, FarShoreZ), new Vector3(x + 1000f, 0f, FarShoreZ + 1f), Slot.Concrete, Slot.Roof, Vector2.zero);
			}
			// Bay bridge: deck with a row of lamps and two towers with beacons.
			const float bz = 5550f;
			for (float x = -3400f; x < 3400f; x += 400f)
			{
				MB mb = At(far, new Vector3(x + 200f, 0f, bz));
				mb.Box(new Vector3(x, 38f, bz - 12f), new Vector3(x + 400f, 42f, bz + 12f), Slot.Metal, Slot.Roof, Vector2.zero, true);
				for (float lx = x + 10f; lx < x + 400f; lx += 40f)
				{
					foreach (float e in new[] { -11.5f, 11.5f })
						mb.Box(new Vector3(lx - 0.4f, 42f, bz + e - 0.4f), new Vector3(lx + 0.4f, 42.8f, bz + e + 0.4f), Slot.Lamp, Slot.Lamp, Vector2.zero);
				}
			}
			foreach (float tx in new[] { -900f, 900f })
			{
				MB mb = At(far, new Vector3(tx, 0f, bz));
				foreach (float e in new[] { -10f, 10f })
				{
					mb.Box(new Vector3(tx - 4f, -3f, bz + e - 3f), new Vector3(tx + 4f, 190f, bz + e + 3f), Slot.Metal, Slot.Metal, Vector2.zero);
					mb.Box(new Vector3(tx - 1f, 190f, bz + e - 1f), new Vector3(tx + 1f, 192f, bz + e + 1f), Slot.NeonPink, Slot.NeonPink, Vector2.zero);
				}
				mb.Box(new Vector3(tx - 4f, 150f, bz - 10f), new Vector3(tx + 4f, 156f, bz + 10f), Slot.Metal, Slot.Metal, Vector2.zero, true);
			}
		}

		// ================================================================ scene objects

		private static int Emit(Dictionary<Vector2Int, MB> chunks, string prefix, Transform parent, bool shadows)
		{
			int verts = 0;
			foreach (KeyValuePair<Vector2Int, MB> kv in chunks)
			{
				if (EmitOne(kv.Value, prefix + "_" + kv.Key.x + "_" + kv.Key.y, parent, shadows) != null) verts += kv.Value.v.Count;
			}
			return verts;
		}

		private static GameObject EmitOne(MB mb, string name, Transform parent, bool shadows)
		{
			if (mb.v.Count == 0) return null;
			var mesh = new Mesh { name = name };
			if (mb.v.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
			mesh.SetVertices(mb.v);
			mesh.SetNormals(mb.n);
			mesh.SetUVs(0, mb.uv);
			var used = new List<int>();
			for (int s = 0; s < mb.t.Length; s++) if (mb.t[s].Count > 0) used.Add(s);
			mesh.subMeshCount = used.Count;
			var mats = new Material[used.Count];
			for (int k = 0; k < used.Count; k++)
			{
				mesh.SetTriangles(mb.t[used[k]], k);
				mats[k] = _mats[used[k]];
			}
			mesh.RecalculateTangents();
			mesh.RecalculateBounds();
			AssetDatabase.CreateAsset(mesh, Dir + "/Meshes/" + name + ".asset");
			var go = new GameObject(name);
			go.transform.SetParent(parent, false);
			go.AddComponent<MeshFilter>().sharedMesh = mesh;
			var r = go.AddComponent<MeshRenderer>();
			r.sharedMaterials = mats;
			r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
			r.rayTracingMode = UnityEngine.Experimental.Rendering.RayTracingMode.Static;
			return go;
		}

		// Street traffic on every core avenue and street (two directions) plus air-car lanes between the towers.
		private static void MakeTraffic(CityWeather w)
		{
			var from = new List<Vector3>();
			var to = new List<Vector3>();
			var col = new List<Color>();
			var speed = new List<float>();
			var rate = new List<float>();
			void Lane(Vector3 a, Vector3 b, Color c, float s, float r)
			{
				from.Add(a);
				to.Add(b);
				col.Add(c);
				speed.Add(s);
				rate.Add(r);
			}
			var head = new Color(1f, 0.95f, 0.85f);
			var tail = new Color(1f, 0.12f, 0.06f);
			float z0 = CoreJ0 * Pitch, z1 = WaterZ - 30f, x0 = CoreI0 * Pitch, x1 = (CoreI1 + 1) * Pitch;
			for (int i = CoreI0; i <= CoreI1 + 1; i++)
			{
				// Cars heading north show the racers their tail lights, oncoming cars their headlights.
				float x = i * Pitch;
				foreach (Vector2 s in OpenSpans(true, x, z0, z1))
				{
					Lane(new Vector3(x + 6f, 1.1f, s.x), new Vector3(x + 6f, 1.1f, s.y), tail, R(17f, 25f), 0.7f);
					Lane(new Vector3(x - 6f, 1.1f, s.y), new Vector3(x - 6f, 1.1f, s.x), head, R(17f, 25f), 0.7f);
				}
			}
			for (int j = CoreJ0; j <= CoreJ1 + 1; j++)
			{
				float z = j * Pitch;
				foreach (Vector2 s in OpenSpans(false, z, x0, x1))
				{
					Lane(new Vector3(s.x, 1.1f, z + 6f), new Vector3(s.y, 1.1f, z + 6f), tail, R(15f, 22f), 0.45f);
					Lane(new Vector3(s.y, 1.1f, z - 6f), new Vector3(s.x, 1.1f, z - 6f), head, R(15f, 22f), 0.45f);
				}
			}
			Color[] sky = { new Color(0.3f, 0.9f, 1f), new Color(1f, 0.6f, 0.2f), new Color(1f, 0.25f, 0.7f) };
			int[] streets = { 2, 6, 9, 12, 15, 19 };
			for (int k = 0; k < streets.Length; k++)
			{
				bool east = k % 2 == 0;
				float z = streets[k] * Pitch + (east ? 10f : -10f), y = R(120f, 190f);
				Lane(new Vector3(east ? x0 - 400f : x1 + 400f, y, z), new Vector3(east ? x1 + 400f : x0 - 400f, y, z), sky[k % 3], R(40f, 65f), 0.5f);
			}
			w.trafficMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/GliderJam/Art/FX/FX_Spark.mat");
			w.laneFrom = from.ToArray();
			w.laneTo = to.ToArray();
			w.laneColor = col.ToArray();
			w.laneSpeed = speed.ToArray();
			w.laneRate = rate.ToArray();
		}

		// Stretches of a street line (along z at x = coord, or along x at z = coord) not closed by a blocking building.
		private static List<Vector2> OpenSpans(bool alongZ, float coord, float from, float to)
		{
			var cuts = new List<Vector2>();
			foreach (Rect r in Blocked)
			{
				if (alongZ && coord >= r.xMin && coord <= r.xMax) cuts.Add(new Vector2(r.yMin, r.yMax));
				if (!alongZ && coord >= r.yMin && coord <= r.yMax) cuts.Add(new Vector2(r.xMin, r.xMax));
			}
			cuts.Sort((a, b) => a.x.CompareTo(b.x));
			var spans = new List<Vector2>();
			float s = from;
			foreach (Vector2 c in cuts)
			{
				if (c.x - s > 80f) spans.Add(new Vector2(s, c.x - 4f));
				s = Mathf.Max(s, c.y + 4f);
			}
			if (to - s > 80f) spans.Add(new Vector2(s, to));
			return spans;
		}

		// Trigger boxes for the stunts (gate tunnels, under skybridges); RaceManager pays out the boost.
		private static void MakeTricks(Transform parent)
		{
			var root = new GameObject("City Stunts").transform;
			root.SetParent(parent, false);
			foreach (var (box, kind) in Tricks)
			{
				var go = new GameObject(kind == TrickZone.Kind.Gate ? "Gate Tunnel" : "Under Bridge");
				go.transform.SetParent(root, false);
				go.transform.position = box.center;
				var c = go.AddComponent<BoxCollider>();
				c.isTrigger = true;
				c.size = box.size;
				go.AddComponent<TrickZone>().kind = kind;
			}
		}

		private static void MakeColliders(Transform parent)
		{
			var root = new GameObject("Colliders").transform;
			root.SetParent(parent, false);
			foreach (KeyValuePair<Vector2Int, List<Bounds>> kv in Solids)
			{
				var go = new GameObject("Colliders_" + kv.Key.x + "_" + kv.Key.y);
				go.transform.SetParent(root, false);
				foreach (Bounds b in kv.Value)
				{
					var bc = go.AddComponent<BoxCollider>();
					bc.center = b.center;
					bc.size = b.size;
				}
			}
			var round = new GameObject("Round Towers");
			round.transform.SetParent(root, false);
			for (int i = 0; i + 1 < Cylinders.Count; i += 2)
			{
				Vector4 c = Cylinders[i];
				float hgt = Cylinders[i + 1].x;
				var cap = round.AddComponent<CapsuleCollider>();
				cap.direction = 1;
				cap.radius = c.w;
				cap.height = hgt; // rounded ends stay inside the tower (no invisible dome above the roof)
				cap.center = new Vector3(c.x, c.y + hgt * 0.5f, c.z);
			}
			var ground = new GameObject("Ground and Water");
			ground.transform.SetParent(root, false);
			var g = ground.AddComponent<BoxCollider>();
			g.center = new Vector3(0f, -5f, (-14f * Pitch + WaterZ) * 0.5f);
			g.size = new Vector3(40f * Pitch, 10f, WaterZ + 14f * Pitch);
			var w = ground.AddComponent<BoxCollider>();
			w.center = new Vector3(0f, -8f, (WaterZ + FarShoreZ) * 0.5f);
			w.size = new Vector3(14000f, 10f, FarShoreZ - WaterZ);
		}

		private static void MakeLights()
		{
			var root = new GameObject("City Lights").transform;
			PropertyInfo unit = typeof(Light).GetProperty("lightUnit");
			foreach (LightSpec s in Lights)
			{
				var go = new GameObject("Light");
				go.transform.SetParent(root, false);
				go.transform.position = s.pos;
				var l = go.AddComponent<Light>();
				l.type = LightType.Point;
				var hd = go.GetComponent<HDAdditionalLightData>();
				if (hd == null) hd = go.AddComponent<HDAdditionalLightData>();
				if (unit != null) unit.SetValue(l, Enum.Parse(unit.PropertyType, "Lumen"));
				l.intensity = s.lumen;
				l.color = s.color;
				l.range = s.range;
				l.shadows = LightShadows.None;
				hd.volumetricDimmer = s.volumetric;
			}
		}

		private static Renderer[] MakeBillboards(Transform parent, Material[] boards)
		{
			var quad = new Mesh { name = "BillboardQuad" };
			quad.SetVertices(new List<Vector3> { new Vector3(-0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(0.5f, -0.5f, 0f) });
			quad.SetNormals(new List<Vector3> { Vector3.back, Vector3.back, Vector3.back, Vector3.back });
			quad.SetUVs(0, new List<Vector2> { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) });
			quad.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);
			quad.RecalculateTangents();
			quad.RecalculateBounds();
			AssetDatabase.CreateAsset(quad, Dir + "/Meshes/BillboardQuad.asset");
			var root = new GameObject("Billboards").transform;
			root.SetParent(parent, false);
			var list = new List<Renderer>();
			foreach (BoardSpec b in Boards)
			{
				var go = new GameObject("Billboard");
				go.transform.SetParent(root, false);
				go.transform.position = b.center;
				go.transform.rotation = Quaternion.LookRotation(-b.normal);
				go.transform.localScale = new Vector3(b.w, b.h, 1f);
				go.AddComponent<MeshFilter>().sharedMesh = quad;
				var r = go.AddComponent<MeshRenderer>();
				r.sharedMaterial = boards[b.mat];
				r.shadowCastingMode = ShadowCastingMode.Off;
				list.Add(r);
			}
			return list.ToArray();
		}

		private static void Atmosphere()
		{
			var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
			if (profile == null)
			{
				profile = ScriptableObject.CreateInstance<VolumeProfile>();
				AssetDatabase.CreateAsset(profile, ProfilePath);
			}
			var env = AtmosphereSetup.Get<VisualEnvironment>(profile);
			AtmosphereSetup.P(env, "skyType", (int)SkyType.Gradient);
			AtmosphereSetup.P(env, "skyAmbientMode", SkyAmbientMode.Dynamic);
			var sky = AtmosphereSetup.Get<GradientSky>(profile);
			AtmosphereSetup.P(sky, "top", new Color(0.09f, 0.11f, 0.3f));
			AtmosphereSetup.P(sky, "middle", new Color(0.85f, 0.45f, 1.25f)); // mirror towers reflect this city glow
			AtmosphereSetup.P(sky, "bottom", new Color(3.8f, 1.6f, 2.1f));
			AtmosphereSetup.P(sky, "gradientDiffusion", 2.4f);
			AtmosphereSetup.P(sky, "multiplier", 1f);
			var clouds = AtmosphereSetup.Get<VolumetricClouds>(profile);
			AtmosphereSetup.P(clouds, "enable", false);
			var fog = AtmosphereSetup.Get<Fog>(profile);
			AtmosphereSetup.P(fog, "enabled", true);
			AtmosphereSetup.P(fog, "meanFreePath", 900f);
			AtmosphereSetup.P(fog, "baseHeight", 0f);
			AtmosphereSetup.P(fog, "maximumHeight", 280f);
			AtmosphereSetup.P(fog, "maxFogDistance", 9000f);
			AtmosphereSetup.P(fog, "albedo", new Color(0.72f, 0.72f, 0.86f));
			AtmosphereSetup.P(fog, "enableVolumetricFog", true);
			AtmosphereSetup.P(fog, "anisotropy", 0.6f);
			AtmosphereSetup.P(fog, "depthExtent", 900f);
			AtmosphereSetup.P(fog, "globalLightProbeDimmer", 1f);
			var exposure = AtmosphereSetup.Get<Exposure>(profile);
			AtmosphereSetup.P(exposure, "mode", ExposureMode.Fixed);
			AtmosphereSetup.P(exposure, "fixedExposure", 1.4f); // was 3.0: too dark without ray traced GI
			var tone = AtmosphereSetup.Get<Tonemapping>(profile);
			AtmosphereSetup.P(tone, "mode", TonemappingMode.ACES);
			var bloom = AtmosphereSetup.Get<Bloom>(profile);
			AtmosphereSetup.P(bloom, "intensity", 0.5f);
			AtmosphereSetup.P(bloom, "scatter", 0.75f);
			AtmosphereSetup.P(bloom, "threshold", 0.8f);
			var ca = AtmosphereSetup.Get<ColorAdjustments>(profile);
			AtmosphereSetup.P(ca, "contrast", 15f);
			AtmosphereSetup.P(ca, "saturation", 12f);
			var split = AtmosphereSetup.Get<SplitToning>(profile);
			AtmosphereSetup.P(split, "shadows", new Color(0.25f, 0.4f, 0.6f));
			AtmosphereSetup.P(split, "highlights", new Color(1f, 0.6f, 0.75f));
			AtmosphereSetup.P(split, "balance", 10f);
			var vignette = AtmosphereSetup.Get<Vignette>(profile);
			AtmosphereSetup.P(vignette, "intensity", 0.3f);
			AtmosphereSetup.P(vignette, "smoothness", 0.5f);
			var blur = AtmosphereSetup.Get<MotionBlur>(profile);
			AtmosphereSetup.P(blur, "intensity", 0.15f);
			AtmosphereSetup.P(blur, "maximumVelocity", 90f);
			var grain = AtmosphereSetup.Get<FilmGrain>(profile);
			AtmosphereSetup.P(grain, "type", FilmGrainLookup.Thin1);
			AtmosphereSetup.P(grain, "intensity", 0.15f);
			var shadows = AtmosphereSetup.Get<HDShadowSettings>(profile);
			AtmosphereSetup.P(shadows, "maxShadowDistance", 1500f);
			AtmosphereSetup.P(shadows, "cascadeShadowSplitCount", 4);
			var ao = AtmosphereSetup.Get<ScreenSpaceAmbientOcclusion>(profile);
			AtmosphereSetup.P(ao, "intensity", 1f);
			AtmosphereSetup.P(ao, "radius", 2f);
			var ssr = AtmosphereSetup.Get<ScreenSpaceReflection>(profile);
			AtmosphereSetup.P(ssr, "reflectSky", true);
			EditorUtility.SetDirty(profile);
			AssetDatabase.SaveAssets();

			var vol = new GameObject("Night Volume").AddComponent<Volume>();
			vol.isGlobal = true;
			vol.sharedProfile = profile;

			var moonGo = new GameObject("Moon");
			moonGo.transform.rotation = Quaternion.Euler(30f, 215f, 0f);
			var moon = moonGo.AddComponent<Light>();
			moon.type = LightType.Directional;
			var hd = moonGo.GetComponent<HDAdditionalLightData>();
			if (hd == null) hd = moonGo.AddComponent<HDAdditionalLightData>();
			moon.intensity = 14f;
			moon.useColorTemperature = true;
			moon.colorTemperature = 9000f;
			moon.color = new Color(0.8f, 0.85f, 1f);
			moon.shadows = LightShadows.Soft;
			hd.SetShadowResolutionLevel(2);
			hd.volumetricDimmer = 0.4f;
		}

		// Clearance check along the race line (and the split-screen autopilot offsets).
		private static string Verify(RaceRoute rr)
		{
			Physics.SyncTransforms();
			int bad = 0;
			var first = new List<string>();
			int finish = rr.checkpoints[rr.checkpoints.Length - 1];
			for (int i = 0; i <= finish; i++)
			{
				Vector3 p = rr.path[i];
				Vector3 f = Flat(rr.path[Mathf.Min(rr.path.Length - 1, i + 1)] - rr.path[Mathf.Max(0, i - 1)]).normalized;
				Vector3 side = Vector3.Cross(Vector3.up, f);
				foreach (float o in new[] { -10f, 0f, 10f })
				{
					if (!Physics.CheckSphere(p + side * o, 8f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
					bad++;
					if (first.Count < 6) first.Add(i + "@" + (p + side * o).ToString("F0"));
				}
			}
			return bad == 0 ? "route clear" : "route blocked at " + bad + " probes: " + string.Join(" ", first);
		}

		private static void AddToBuild()
		{
			var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
			if (scenes.Exists(s => s.path == ScenePath)) return;
			scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
			EditorBuildSettings.scenes = scenes.ToArray();
		}

		// ================================================================ materials & textures

		private struct FacadeStyle
		{
			public int frame, spandrel, split;   // mullion px, spandrel px, windows per 4 m bay
			public Color glass, frameColor;
			public float glassSmooth, glassMetal, frameSmooth, frameMetal, lit, grime;
			public float paneTilt;               // per-pane normal tilt (mirror glass 'oil-canning')
			public bool bands;                   // ribbon windows (offices)
			public int ledEvery;                 // LED light band every n floors (0 = none)
			public Color led;
			public Color[] palette;              // interior light colours
		}

		private static readonly Color Warm = new Color(1f, 0.72f, 0.44f), Neutral = new Color(1f, 0.9f, 0.78f), Cool = new Color(0.68f, 0.82f, 1f);
		private static readonly Color Pink = new Color(1f, 0.3f, 0.65f), Teal = new Color(0.25f, 1f, 0.85f), Violet = new Color(0.55f, 0.4f, 1f);

		private static Material[] MakeMaterials(out Material rain, out Material[] boards)
		{
			var m = new Material[(int)Slot.Count];
			// Mirror-dark curtain walls with LED floor bands (cyan / magenta), finned or chamfered.
			m[(int)Slot.Glass] = Facade("Glass", new FacadeStyle { frame = 3, spandrel = 6, split = 1, glass = new Color(0.2f, 0.25f, 0.32f), glassMetal = 0.92f, glassSmooth = 0.97f,
				frameColor = new Color(0.07f, 0.08f, 0.09f), frameMetal = 0.8f, frameSmooth = 0.7f, lit = 0.12f, ledEvery = 6, led = new Color(0.1f, 0.85f, 1f), palette = new[] { Cool, Neutral, Teal } }, 11);
			m[(int)Slot.GlassPink] = Facade("GlassPink", new FacadeStyle { frame = 3, spandrel = 6, split = 1, glass = new Color(0.3f, 0.2f, 0.3f), glassMetal = 0.92f, glassSmooth = 0.97f,
				frameColor = new Color(0.08f, 0.07f, 0.09f), frameMetal = 0.8f, frameSmooth = 0.7f, lit = 0.14f, ledEvery = 5, led = new Color(1f, 0.12f, 0.6f), palette = new[] { Warm, Pink, Violet } }, 61);
			// Dense hab blocks: small windows, lots of coloured light, grimy concrete.
			m[(int)Slot.Housing] = Facade("Housing", new FacadeStyle { frame = 10, spandrel = 14, split = 2, glass = new Color(0.02f, 0.025f, 0.03f), glassMetal = 0f, glassSmooth = 0.85f,
				frameColor = new Color(0.2f, 0.19f, 0.18f), frameMetal = 0f, frameSmooth = 0.4f, lit = 0.4f, grime = 0.55f, palette = new[] { Warm, Warm, Neutral, Pink, Teal, Violet } }, 71);
			m[(int)Slot.Concrete] = Facade("Concrete", new FacadeStyle { frame = 18, spandrel = 16, split = 1, glass = new Color(0.025f, 0.03f, 0.035f), glassSmooth = 0.88f,
				frameColor = new Color(0.22f, 0.21f, 0.2f), frameSmooth = 0.42f, lit = 0.3f, grime = 0.5f, palette = new[] { Warm, Neutral, Pink, Teal } }, 23);
			m[(int)Slot.Dim] = Facade("Dim", new FacadeStyle { frame = 10, spandrel = 12, split = 2, glass = new Color(0.02f, 0.025f, 0.03f), glassSmooth = 0.9f,
				frameColor = new Color(0.13f, 0.13f, 0.14f), frameMetal = 0.2f, frameSmooth = 0.5f, lit = 0.05f, grime = 0.4f, palette = new[] { Warm, Neutral } }, 53);
			m[(int)Slot.Office] = Facade("Office", new FacadeStyle { frame = 2, spandrel = 16, split = 1, glass = new Color(0.12f, 0.15f, 0.19f), glassMetal = 0.75f, glassSmooth = 0.95f,
				frameColor = new Color(0.05f, 0.055f, 0.06f), frameMetal = 0.5f, frameSmooth = 0.55f, lit = 0.45f, bands = true, ledEvery = 8, led = new Color(0.6f, 0.9f, 1f), palette = new[] { Neutral, Cool } }, 37);
			// Mirror-glass skyscrapers: coated reflective panes on a fine mullion grid, a few offices lit behind the glass.
			m[(int)Slot.Mirror] = Facade("Mirror", new FacadeStyle { frame = 2, spandrel = 7, split = 2, glass = new Color(0.48f, 0.52f, 0.58f), glassMetal = 1f, glassSmooth = 0.985f,
				frameColor = new Color(0.2f, 0.21f, 0.23f), frameMetal = 0.9f, frameSmooth = 0.82f, lit = 0.08f, paneTilt = 0.035f, ledEvery = 10, led = new Color(0.75f, 0.9f, 1f), palette = new[] { Cool, Neutral } }, 83);
			m[(int)Slot.MirrorGold] = Facade("MirrorGold", new FacadeStyle { frame = 2, spandrel = 7, split = 2, glass = new Color(0.62f, 0.48f, 0.28f), glassMetal = 1f, glassSmooth = 0.985f,
				frameColor = new Color(0.22f, 0.18f, 0.12f), frameMetal = 0.9f, frameSmooth = 0.82f, lit = 0.08f, paneTilt = 0.03f, ledEvery = 12, led = new Color(1f, 0.65f, 0.25f), palette = new[] { Warm, Neutral } }, 89);

			const int RS = 512;
			var rb = new Color[RS * RS];
			var rm = new Color[RS * RS];
			for (int y = 0; y < RS; y++)
			{
				for (int x = 0; x < RS; x++)
				{
					float u = x / (float)RS, v = y / (float)RS;
					float g = Mathf.Lerp(0.06f, 0.12f, TileFbm(u, v, 8, 4, 3)) * Mathf.Lerp(0.9f, 1.05f, TileNoise(u, v, 128, 4));
					float wet = WorldNoise.SmoothStep(0.5f, 0.62f, TileFbm(u, v, 4, 3, 5));
					rb[y * RS + x] = new Color(g, g, g * 1.03f);
					rm[y * RS + x] = new Color(0f, 1f, 0f, Mathf.Lerp(0.35f, 0.9f, wet));
				}
			}
			m[(int)Slot.Roof] = Lit("City_Roof", Color.white, SaveTex("Roof_Base", RS, RS, rb, false), SaveTex("Roof_Mask", RS, RS, rm, true), null, 1f, 0f, null, Color.black);

			const int AS = 1024;
			var ab = new Color[AS * AS];
			var am = new Color[AS * AS];
			var ah = new float[AS * AS];
			for (int y = 0; y < AS; y++)
			{
				for (int x = 0; x < AS; x++)
				{
					float u = x / (float)AS, v = y / (float)AS;
					float grain = TileFbm(u, v, 256, 2, 7);
					float patch = TileFbm(u, v, 6, 3, 9);
					float puddle = WorldNoise.SmoothStep(0.47f, 0.57f, TileFbm(u, v, 5, 4, 13));
					float g = Mathf.Lerp(0.028f, 0.06f, grain) * Mathf.Lerp(0.8f, 1.2f, patch) * Mathf.Lerp(1f, 0.6f, puddle);
					ab[y * AS + x] = new Color(g, g, g * 1.05f);
					am[y * AS + x] = new Color(0f, 1f, 0f, Mathf.Lerp(0.68f + 0.1f * patch, 0.98f, puddle)); // rain-soaked
					ah[y * AS + x] = grain * (1f - puddle);
				}
			}
			m[(int)Slot.Pavement] = Lit("City_Asphalt", Color.white, SaveTex("Asphalt_Base", AS, AS, ab, false), SaveTex("Asphalt_Mask", AS, AS, am, true),
				NormalTex("Asphalt_Normal", AS, ah, 2.5f), 1f, 0f, null, Color.black);

			const int WS = 512;
			var wh = new float[WS * WS];
			for (int y = 0; y < WS; y++)
				for (int x = 0; x < WS; x++)
					wh[y * WS + x] = TileFbm(x / (float)WS, y / (float)WS, 12, 4, 17);
			m[(int)Slot.Water] = Lit("City_Water", new Color(0.006f, 0.009f, 0.014f), null, null, NormalTex("Water_Normal", WS, wh, 3f), 0.965f, 0f, null, Color.black, 0.5f);

			m[(int)Slot.Marking] = Lit("City_Marking", new Color(0.6f, 0.58f, 0.48f), null, null, null, 0.78f, 0f, null, Color.black);
			m[(int)Slot.Metal] = Lit("City_Metal", new Color(0.05f, 0.05f, 0.055f), null, null, null, 0.55f, 0.85f, null, Color.black);
			m[(int)Slot.Chrome] = Lit("City_Chrome", new Color(0.78f, 0.79f, 0.82f), null, null, null, 0.94f, 1f, null, Color.black);
			m[(int)Slot.Marble] = Marble();
			m[(int)Slot.Lamp] = Lit("City_Lamp", new Color(1f, 0.85f, 0.6f), null, null, null, 0.5f, 0f, null, new Color(1f, 0.72f, 0.42f) * 3000f);
			for (int k = 0; k < NeonSlots.Length; k++)
			{
				m[(int)NeonSlots[k]] = Lit("City_Neon_" + NeonSlots[k].ToString().Replace("Neon", ""), NeonColors[k] * 0.25f, null, null, null, 0.6f, 0f, null, NeonColors[k] * 1200f);
			}

			// Rain streak (soft vertical line) on the HDRP particle sample unlit graph.
			var rp = new Color[32 * 128];
			for (int y = 0; y < 128; y++)
			{
				for (int x = 0; x < 32; x++)
				{
					float u = (x + 0.5f) / 32f - 0.5f, v = (y + 0.5f) / 128f;
					float a = Mathf.Exp(-u * u / (2f * 0.12f * 0.12f)) * Mathf.Sin(Mathf.PI * v);
					rp[y * 32 + x] = new Color(1f, 1f, 1f, a);
				}
			}
			Texture2D rainTex = SaveTex("Rain", 32, 128, rp, false, TextureWrapMode.Clamp, true);
			rain = ParticleMaterial("FX_Rain", rainTex);

			boards = new[]
			{
				Billboard("Sun", BoardSun), Billboard("Glyph", BoardGlyph), Billboard("Glider", BoardGlider),
				Billboard("GlyphBanner", (u, v) => Glyphs(u, v, 4, 16, 83, new Color(0.1f, 0.9f, 1f), new Color(1f, 0.85f, 0.3f)), 128, 512),
				Billboard("GlyphBannerPink", (u, v) => Glyphs(u, v, 3, 12, 97, new Color(1f, 0.15f, 0.6f), new Color(0.75f, 0.4f, 1f)), 128, 512)
			};
			return m;
		}

		// Polished dark stone slabs (2 m) with pale veins and grout lines, wet-smooth: plazas and sidewalks.
		private static Material Marble()
		{
			const int S = 1024, slabs = 12;
			var bc = new Color[S * S];
			var mk = new Color[S * S];
			var ht = new float[S * S];
			for (int y = 0; y < S; y++)
			{
				for (int x = 0; x < S; x++)
				{
					float u = x / (float)S, v = y / (float)S;
					float gu = Mathf.Abs(Mathf.Repeat(u * slabs, 1f) - 0.5f), gv = Mathf.Abs(Mathf.Repeat(v * slabs, 1f) - 0.5f);
					bool grout = gu > 0.485f || gv > 0.485f;
					float tint = 0.85f + 0.3f * WorldNoise.Hash(Mathf.FloorToInt(u * slabs), Mathf.FloorToInt(v * slabs), 61);
					float vein = WorldNoise.SmoothStep(0.94f, 0.995f, 1f - Mathf.Abs(TileFbm(u, v, 6, 4, 67) * 2f - 1f));
					Color c = Color.Lerp(new Color(0.028f, 0.028f, 0.034f) * tint, new Color(0.15f, 0.15f, 0.16f), vein * 0.8f);
					bc[y * S + x] = grout ? new Color(0.01f, 0.01f, 0.012f) : c;
					mk[y * S + x] = new Color(0f, 1f, 0f, grout ? 0.45f : 0.93f);
					ht[y * S + x] = grout ? 0f : 1f;
				}
			}
			return Lit("City_Marble", Color.white, SaveTex("Marble_Base", S, S, bc, false), SaveTex("Marble_Mask", S, S, mk, true), NormalTex("Marble_Normal", S, ht, 1.2f), 1f, 0f, null, Color.black);
		}

		private static Material Facade(string name, FacadeStyle s, int seed)
		{
			int w = TileCols * CellPx, h = TileFloors * FloorPx;
			var baseC = new Color[w * h];
			var mask = new Color[w * h];
			var emis = new Color[w * h];
			var height = new float[w * h];
			var tiltX = new float[w * h];
			var tiltY = new float[w * h];
			int split = Mathf.Max(1, s.split), bay = CellPx / split;
			const float windowGlow = 0.15f; // interior light relative to the LED bands (emissive map is LDR)
			for (int fl = 0; fl < TileFloors; fl++)
			{
				float mood = WorldNoise.Hash(fl, seed, 3);
				bool ledFloor = s.ledEvery > 0 && fl % s.ledEvery == s.ledEvery - 1;
				for (int win = 0; win < TileCols * split; win++)
				{
					float chance = s.bands ? (mood < s.lit ? 0.85f : 0.03f) : s.lit * (0.3f + 1.4f * mood);
					bool lit = WorldNoise.Hash(win + 101 * fl, seed, 7) < chance;
					Color light = s.palette[(int)(WorldNoise.Hash(win * 7 + fl * 131, seed, 11) * s.palette.Length) % s.palette.Length];
					float bright = 0.3f + 0.7f * WorldNoise.Hash(win * 13 + fl * 17, seed, 19);
					bool blinds = WorldNoise.Hash(win * 3 + fl * 5, seed, 23) < 0.3f;
					float cover = WorldNoise.Hash(win * 5 + fl * 3, seed, 29) < 0.25f ? 0.3f + 0.4f * WorldNoise.Hash(win, fl, seed) : 0f;
					for (int py = 0; py < FloorPx; py++)
					{
						for (int px = 0; px < bay; px++)
						{
							int x = win * bay + px, y = fl * FloorPx + py, i = y * w + x;
							float u = x / (float)w, v = y / (float)h;
							bool frame = s.bands ? py < s.spandrel || py >= FloorPx - 2 || x % 32 < s.frame
								: px < s.frame / 2 || px >= bay - (s.frame + 1) / 2 || py < s.spandrel || py >= FloorPx - 2;
							float grain = 0.9f + 0.1f * WorldNoise.Hash(x, y, seed);
							float dirt = 1f - s.grime * TileFbm(u, v, 12, 3, seed + 5) * (0.6f + 0.4f * TileNoise(u, v * 0.25f, 48, seed + 9));
							if (ledFloor && py >= 2 && py < 5)
							{
								// LED band in the floor slab.
								baseC[i] = new Color(0.02f, 0.02f, 0.025f);
								mask[i] = new Color(0f, 1f, 0f, 0.85f);
								emis[i] = s.led;
								height[i] = 0.5f;
								continue;
							}
							if (frame)
							{
								baseC[i] = s.frameColor * grain * dirt;
								mask[i] = new Color(s.frameMetal, 1f, 0f, s.frameSmooth * Mathf.Lerp(0.7f, 1f, dirt));
								emis[i] = Color.black;
								height[i] = 1f;
								continue;
							}
							float wy = (py - s.spandrel) / (float)(FloorPx - 2 - s.spandrel);
							float wx = (px - s.frame * 0.5f) / (bay - s.frame);
							baseC[i] = s.glass;
							mask[i] = new Color(s.glassMetal, 1f, 0f, s.glassSmooth);
							height[i] = 0f;
							if (s.paneTilt > 0f)
							{
								// Each pane sits a touch off-plane and bows slightly, so reflections ripple from pane to pane.
								tiltX[i] = (WorldNoise.Hash(win, fl, seed + 41) - 0.5f) * 2f * s.paneTilt + (wx - 0.5f) * s.paneTilt * 0.8f;
								tiltY[i] = (WorldNoise.Hash(win, fl, seed + 43) - 0.5f) * 2f * s.paneTilt + (wy - 0.5f) * s.paneTilt * 0.8f;
							}
							if (!lit)
							{
								emis[i] = Color.black;
								continue;
							}
							// A lit room: brighter towards the ceiling, a ceiling light strip, furniture silhouettes low down.
							float g = Mathf.Lerp(0.55f, 1f, wy);
							if (wy > 0.86f) g *= 1.35f;
							if (wy < 0.32f && WorldNoise.Hash((int)(wx * 7f) + win * 13, fl, seed + 31) < 0.4f) g *= 0.3f;
							if (blinds && py % 4 == 0) g *= 0.35f;
							if (cover > 0f && wx < cover) g *= 0.25f;
							emis[i] = light * (bright * g * windowGlow);
						}
					}
				}
			}
			Texture2D bm = SaveTex("Facade_" + name + "_Base", w, h, baseC, false);
			Texture2D mm = SaveTex("Facade_" + name + "_Mask", w, h, mask, true);
			Texture2D em = SaveTex("Facade_" + name + "_Emissive", w, h, emis, false);
			Texture2D nm = NormalTexRect("Facade_" + name + "_Normal", w, h, height, 1.5f, tiltX, tiltY);
			return Lit("City_Facade_" + name, Color.white, bm, mm, nm, 1f, 0f, em, Color.white * 85f);
		}

		// Normal map from a height field of any size (wrapping).
		private static Texture2D NormalTexRect(string name, int w, int h, float[] height, float strength, float[] offX = null, float[] offY = null)
		{
			var px = new Color[w * h];
			for (int y = 0; y < h; y++)
			{
				for (int x = 0; x < w; x++)
				{
					float dx = (height[y * w + (x + 1) % w] - height[y * w + (x - 1 + w) % w]) * strength;
					float dy = (height[((y + 1) % h) * w + x] - height[((y - 1 + h) % h) * w + x]) * strength;
					Vector3 n = new Vector3(-dx + (offX != null ? offX[y * w + x] : 0f), -dy + (offY != null ? offY[y * w + x] : 0f), 1f).normalized;
					px[y * w + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
				}
			}
			Texture2D t = SaveTex(name, w, h, px, true);
			var ti = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(t));
			ti.textureType = TextureImporterType.NormalMap;
			ti.SaveAndReimport();
			return AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GetAssetPath(t));
		}

		private static Material Billboard(string name, Func<float, float, Color> f, int w = 512, int h = 288)
		{
			var px = new Color[w * h];
			for (int y = 0; y < h; y++)
				for (int x = 0; x < w; x++)
					px[y * w + x] = f((x + 0.5f) / w, (y + 0.5f) / h);
			Texture2D t = SaveTex("Board_" + name, w, h, px, false);
			return Lit("City_Board_" + name, new Color(0.02f, 0.02f, 0.02f), null, null, null, 0.85f, 0f, t, Color.white * 260f);
		}

		// Tall banner: columns of glyphs in two inks on a dark ground with a glowing border (scrolls at runtime).
		private static Color Glyphs(float u, float v, int cols, int rows, int seed, Color inkA, Color inkB)
		{
			if (u < 0.04f || u > 0.96f) return inkA * 0.9f;
			int cx = Mathf.FloorToInt(u * cols), cy = Mathf.FloorToInt(v * rows);
			float lu = u * cols - cx, lv = v * rows - cy;
			var c = new Color(0.01f, 0.012f, 0.03f);
			if (WorldNoise.Hash(cx, cy, seed) < 0.12f) return c;
			Color ink = WorldNoise.Hash(cx, cy / 4, seed + 1) < 0.7f ? inkA : inkB;
			int strokes = 2 + (int)(WorldNoise.Hash(cx, cy, seed + 2) * 3f);
			for (int k = 0; k < strokes; k++)
			{
				float a = 0.15f + 0.6f * WorldNoise.Hash(cx * 7 + k, cy, seed + 3), b = 0.15f + 0.6f * WorldNoise.Hash(cx, cy * 7 + k, seed + 4);
				bool horiz = WorldNoise.Hash(cx + k, cy + k, seed + 5) < 0.5f;
				float len = 0.3f + 0.5f * WorldNoise.Hash(cx + k * 3, cy, seed + 6);
				bool hit = horiz ? Mathf.Abs(lv - b) < 0.07f && lu > a && lu < Mathf.Min(0.9f, a + len)
					: Mathf.Abs(lu - a) < 0.07f && lv > b && lv < Mathf.Min(0.9f, b + len);
				if (hit) return ink;
			}
			return c;
		}

		// Retro sun over a neon grid.
		private static Color BoardSun(float u, float v)
		{
			Color c = Color.Lerp(new Color(0.3f, 0.02f, 0.25f), new Color(0.03f, 0f, 0.1f), v);
			float dx = (u - 0.5f) * 1.78f, dy = v - 0.55f;
			float r = Mathf.Sqrt(dx * dx + dy * dy);
			if (r < 0.3f && v > 0.3f)
			{
				float stripe = v < 0.55f ? Mathf.Repeat((0.55f - v) * 26f, 1f) : 1f;
				if (stripe > Mathf.Lerp(0.15f, 0.6f, Mathf.InverseLerp(0.55f, 0.3f, v)) || v >= 0.55f)
					c = Color.Lerp(new Color(1f, 0.1f, 0.55f), new Color(1f, 0.85f, 0.2f), Mathf.InverseLerp(0.3f, 0.85f, v));
			}
			if (v < 0.3f)
			{
				float depth = 0.3f / Mathf.Max(0.02f, 0.3f - v);
				float gx = Mathf.Abs(Mathf.Repeat((u - 0.5f) * depth * 6f, 1f) - 0.5f);
				float gz = Mathf.Abs(Mathf.Repeat(depth * 1.5f, 1f) - 0.5f);
				if (gx > 0.46f || gz > 0.44f) c = new Color(0.1f, 0.85f, 1f);
				else c = new Color(0.04f, 0f, 0.08f);
			}
			return c;
		}

		// Columns of glowing glyphs (scrolls vertically at runtime).
		private static Color BoardGlyph(float u, float v)
		{
			int cx = Mathf.FloorToInt(u * 16f), cy = Mathf.FloorToInt(v * 9f);
			float lu = u * 16f - cx, lv = v * 9f - cy;
			var c = new Color(0.01f, 0.015f, 0.04f);
			if (WorldNoise.Hash(cx, cy, 41) < 0.15f) return c;
			Color ink = WorldNoise.Hash(cx, 0, 43) < 0.6f ? new Color(0.1f, 0.9f, 1f) : new Color(1f, 0.2f, 0.65f);
			int strokes = 2 + (int)(WorldNoise.Hash(cx, cy, 47) * 3f);
			for (int k = 0; k < strokes; k++)
			{
				float a = 0.15f + 0.6f * WorldNoise.Hash(cx * 7 + k, cy, 53), b = 0.15f + 0.6f * WorldNoise.Hash(cx, cy * 7 + k, 59);
				bool horiz = WorldNoise.Hash(cx + k, cy + k, 61) < 0.5f;
				float len = 0.3f + 0.5f * WorldNoise.Hash(cx + k * 3, cy, 67);
				bool hit = horiz ? Mathf.Abs(lv - b) < 0.06f && lu > a && lu < Mathf.Min(0.9f, a + len)
					: Mathf.Abs(lu - a) < 0.06f && lv > b && lv < Mathf.Min(0.9f, b + len);
				if (hit) return ink;
			}
			return c;
		}

		// A sailplane silhouette with speed lines.
		private static Color BoardGlider(float u, float v)
		{
			Color c = Color.Lerp(new Color(0f, 0.25f, 0.6f), new Color(0.65f, 0f, 0.5f), u * 0.7f + v * 0.3f) * 0.8f;
			float wing = Mathf.Abs(v - (0.55f + (u - 0.5f) * 0.08f)) < Mathf.Lerp(0.025f, 0.008f, Mathf.Abs(u - 0.5f) * 2.4f) && Mathf.Abs(u - 0.5f) < 0.38f ? 1f : 0f;
			float fus = Mathf.Abs(v - 0.53f) < 0.035f * (1f - Mathf.Abs(u - 0.47f) * 5f) && u > 0.3f && u < 0.66f ? 1f : 0f;
			float tail = Mathf.Abs(u - 0.64f) < 0.012f && v > 0.5f && v < 0.68f ? 1f : 0f;
			float lines = u < 0.36f && Mathf.Repeat(v * 22f, 1f) < 0.12f && WorldNoise.Hash(Mathf.FloorToInt(v * 22f), 0, 71) < 0.5f ? 0.8f : 0f;
			return Color.Lerp(c, Color.white, Mathf.Max(Mathf.Max(wing, fus), Mathf.Max(tail, lines)));
		}

		private static Material ParticleMaterial(string name, Texture2D tex)
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
			m.SetFloat("_BlendMode", 0f);
			UnityEditor.Rendering.HighDefinition.HDShaderUtils.ResetMaterialKeywords(m);
			EditorUtility.SetDirty(m);
			return m;
		}

		private static Material Lit(string name, Color color, Texture2D baseMap, Texture2D mask, Texture2D normal, float smooth, float metal, Texture2D emisMap, Color emission, float normalScale = 1f)
		{
			string path = Dir + "/" + name + ".mat";
			var m = AssetDatabase.LoadAssetAtPath<Material>(path);
			if (m == null)
			{
				m = new Material(Shader.Find("HDRP/Lit"));
				AssetDatabase.CreateAsset(m, path);
			}
			m.SetColor("_BaseColor", color);
			m.SetTexture("_BaseColorMap", baseMap);
			m.SetTexture("_MaskMap", mask);
			m.SetTexture("_NormalMap", normal);
			m.SetFloat("_NormalScale", normalScale);
			m.SetFloat("_Smoothness", smooth);
			m.SetFloat("_Metallic", metal);
			m.SetFloat("_SmoothnessRemapMin", 0f);
			m.SetFloat("_SmoothnessRemapMax", 1f);
			m.SetFloat("_MetallicRemapMin", 0f);
			m.SetFloat("_MetallicRemapMax", 1f);
			m.SetFloat("_AORemapMin", 0f);
			m.SetFloat("_AORemapMax", 1f);
			m.SetTexture("_EmissiveColorMap", emisMap);
			m.SetColor("_EmissiveColor", emission);
			m.SetColor("_EmissiveColorLDR", emission.maxColorComponent > 0f ? emission / emission.maxColorComponent : Color.black);
			m.SetFloat("_UseEmissiveIntensity", 0f);
			UnityEditor.Rendering.HighDefinition.HDShaderUtils.ResetMaterialKeywords(m);
			EditorUtility.SetDirty(m);
			return m;
		}

		private static Texture2D SaveTex(string name, int w, int h, Color[] px, bool linear, TextureWrapMode wrap = TextureWrapMode.Repeat, bool alpha = false)
		{
			string path = Dir + "/" + name + ".png";
			var t = new Texture2D(w, h, TextureFormat.RGBA32, true, linear);
			t.SetPixels(px);
			t.Apply();
			File.WriteAllBytes(path, t.EncodeToPNG());
			Object.DestroyImmediate(t);
			AssetDatabase.ImportAsset(path);
			var ti = (TextureImporter)AssetImporter.GetAtPath(path);
			ti.sRGBTexture = !linear;
			ti.wrapMode = wrap;
			ti.anisoLevel = 8;
			ti.alphaIsTransparency = alpha;
			ti.maxTextureSize = 2048;
			ti.SaveAndReimport();
			return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
		}

		private static Texture2D NormalTex(string name, int size, float[] height, float strength)
		{
			var px = new Color[size * size];
			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					float dx = (height[y * size + (x + 1) % size] - height[y * size + (x - 1 + size) % size]) * strength;
					float dy = (height[((y + 1) % size) * size + x] - height[((y - 1 + size) % size) * size + x]) * strength;
					Vector3 n = new Vector3(-dx, -dy, 1f).normalized;
					px[y * size + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
				}
			}
			Texture2D t = SaveTex(name, size, size, px, true);
			var ti = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(t));
			ti.textureType = TextureImporterType.NormalMap;
			ti.SaveAndReimport();
			return AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GetAssetPath(t));
		}

		// Tileable value noise in [0, 1]: `freq` cells across the unit square, wrapping.
		private static float TileNoise(float u, float v, int freq, int seed)
		{
			float x = u * freq, y = v * freq;
			int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
			float fx = x - ix, fy = y - iy;
			fx = fx * fx * (3f - 2f * fx);
			fy = fy * fy * (3f - 2f * fy);
			int x0 = ((ix % freq) + freq) % freq, y0 = ((iy % freq) + freq) % freq, x1 = (x0 + 1) % freq, y1 = (y0 + 1) % freq;
			float a = WorldNoise.Hash(x0, y0, seed), b = WorldNoise.Hash(x1, y0, seed), c = WorldNoise.Hash(x0, y1, seed), d = WorldNoise.Hash(x1, y1, seed);
			return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
		}

		private static float TileFbm(float u, float v, int freq, int octaves, int seed)
		{
			float sum = 0f, amp = 1f, norm = 0f;
			for (int o = 0; o < octaves; o++)
			{
				sum += TileNoise(u, v, freq << o, seed + o * 31) * amp;
				norm += amp;
				amp *= 0.5f;
			}
			return sum / norm;
		}
	}
}
