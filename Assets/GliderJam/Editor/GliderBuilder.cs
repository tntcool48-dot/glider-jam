using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GliderJam.EditorTools
{
	// Generates the racing sailplane: lofted fuselage, NACA-section wings with winglets, T-tail, hinged control
	// surfaces, canopy, pilot, details and a per-player livery. Output: meshes, textures, materials and
	// one prefab per player color under Assets/GliderJam/Art/Glider.
	public static class GliderBuilder
	{
		private const string Dir = "Assets/GliderJam/Art/Glider";

		public static readonly Color[] PlayerColors = { new Color(1f, 0.18f, 0.30f), new Color(0.12f, 0.78f, 1f) };

		public static string Build()
		{
			Directory.CreateDirectory(Dir);
			var sb = new System.Text.StringBuilder();
			for (int p = 0; p < 2; p++)
			{
				string path = BuildPrefab(p);
				sb.Append(path).Append(" ");
			}
			AssetDatabase.SaveAssets();
			return sb.ToString();
		}

		// ================================================================ prefab

		private static string BuildPrefab(int player)
		{
			Color accent = PlayerColors[player];
			string tag = "P" + (player + 1);
			Texture2D bodyLivery = LiveryTexture(accent, player + 1, false, Dir + "/Livery_Fuselage_" + tag + ".png");
			Texture2D finLivery = LiveryTexture(accent, player + 1, true, Dir + "/Livery_Fin_" + tag + ".png");

			Material white = Lit("Paint_White", new Color(0.93f, 0.93f, 0.93f), 0.86f, 0f, true, null);
			Material bodyMat = Lit("Paint_Fuselage_" + tag, Color.white, 0.86f, 0f, true, bodyLivery);
			Material finMat = Lit("Paint_Fin_" + tag, Color.white, 0.86f, 0f, true, finLivery);
			Material accentMat = Lit("Paint_Accent_" + tag, accent, 0.82f, 0.25f, true, null);
			Material interior = Lit("Cockpit_Interior", new Color(0.035f, 0.037f, 0.04f), 0.35f, 0f, false, null);
			Material suit = Lit("Pilot_Suit", new Color(0.06f, 0.065f, 0.075f), 0.3f, 0f, false, null);
			Material rubber = Lit("Rubber", new Color(0.02f, 0.02f, 0.02f), 0.45f, 0f, false, null);
			Material metal = Lit("Metal", new Color(0.75f, 0.75f, 0.76f), 0.75f, 1f, false, null);
			Material glass = Glass();
			Material navRed = Emissive("Light_Red", new Color(1f, 0.05f, 0.03f), 60000f);
			Material navGreen = Emissive("Light_Green", new Color(0.05f, 1f, 0.25f), 60000f);
			Material strobe = Emissive("Light_Strobe", Color.white, 120000f);

			var root = new GameObject("Glider_" + tag);
			var visual = new GameObject("Visual").transform;
			visual.SetParent(root.transform, false);

			// Fuselage with cockpit opening (submesh 1) and canopy
			Mesh fuselage = Fuselage(out Mesh canopy);
			Add(visual, "Fuselage", Save(fuselage, "Fuselage"), bodyMat, interior);
			Add(visual, "Canopy", Save(canopy, "Canopy"), glass).GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			Add(visual, "Pilot", Save(Pilot(), "Pilot"), suit, accentMat);

			// Wings
			for (int side = -1; side <= 1; side += 2)
			{
				string s = side < 0 ? "L" : "R";
				WingMeshes(side, out Mesh main, out Mesh aileron, out Mesh tip, out Vector3 hingeA, out Vector3 hingeB, out Vector3 tipPoint, out Vector3 navPoint);
				Add(visual, "Wing" + s, Save(main, "Wing" + s), white);
				Add(visual, "WingTip" + s, Save(tip, "WingTip" + s), accentMat);
				Transform pivot = Pivot(visual, "Aileron" + s + "_Pivot", hingeA, hingeB);
				Add(pivot, "Aileron" + s, Save(ToLocal(aileron, pivot), "Aileron" + s), white);
				var tipAnchor = new GameObject("TipTrail" + s).transform;
				tipAnchor.SetParent(visual, false);
				tipAnchor.localPosition = tipPoint;
				var nav = Add(visual, "NavLight" + s, Save(Sphere(0.045f, 10, navPoint), "NavLight" + s), side < 0 ? navRed : navGreen);
				nav.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			}

			// Tail
			FinMeshes(out Mesh fin, out Mesh rudder, out Vector3 rA, out Vector3 rB);
			Add(visual, "Fin", Save(fin, "Fin"), finMat);
			Transform rudderPivot = Pivot(visual, "Rudder_Pivot", rA, rB);
			Add(rudderPivot, "Rudder", Save(ToLocal(rudder, rudderPivot), "Rudder"), accentMat);
			StabMeshes(out Mesh stab, out Mesh elevator, out Vector3 eA, out Vector3 eB);
			Add(visual, "Stabilizer", Save(stab, "Stabilizer"), white);
			Transform elevPivot = Pivot(visual, "Elevator_Pivot", eA, eB);
			Add(elevPivot, "Elevator", Save(ToLocal(elevator, elevPivot), "Elevator"), white);

			// Details
			Add(visual, "Wheel", Save(Wheel(), "Wheel"), rubber, metal);
			Add(visual, "Pitot", Save(Cylinder(new Vector3(0f, 1.36f, -3.98f), new Vector3(0f, 1.36f, -3.62f), 0.012f, 8), "Pitot"), metal);
			var strobeGo = Add(visual, "Strobe", Save(Sphere(0.05f, 10, new Vector3(0f, 1.42f, -4.45f)), "Strobe"), strobe);
			strobeGo.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

			// Simple colliders: fuselage capsule, wing box, tail box
			var col = root.AddComponent<CapsuleCollider>();
			col.direction = 2; col.radius = 0.42f; col.height = 6.6f; col.center = new Vector3(0f, 0.05f, -1.0f);
			var wingCol = root.AddComponent<BoxCollider>();
			wingCol.center = new Vector3(0f, 0.45f, -0.05f); wingCol.size = new Vector3(14.6f, 0.25f, 0.8f);
			var tailCol = root.AddComponent<BoxCollider>();
			tailCol.center = new Vector3(0f, 0.85f, -4.3f); tailCol.size = new Vector3(2.6f, 1.3f, 0.8f);

			string prefabPath = Dir + "/Glider_" + tag + ".prefab";
			PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
			Object.DestroyImmediate(root);
			return prefabPath;
		}

		private static GameObject Add(Transform parent, string name, Mesh mesh, params Material[] mats)
		{
			var go = new GameObject(name);
			go.transform.SetParent(parent, false);
			go.AddComponent<MeshFilter>().sharedMesh = mesh;
			go.AddComponent<MeshRenderer>().sharedMaterials = mats;
			return go;
		}

		private static Transform Pivot(Transform parent, string name, Vector3 a, Vector3 b)
		{
			var t = new GameObject(name).transform;
			t.SetParent(parent, false);
			t.localPosition = a;
			t.localRotation = Quaternion.FromToRotation(Vector3.right, (b - a).normalized);
			return t;
		}

		// Re-expresses a mesh built in glider space in a pivot's local space.
		private static Mesh ToLocal(Mesh m, Transform pivot)
		{
			Quaternion inv = Quaternion.Inverse(pivot.localRotation);
			Vector3 o = pivot.localPosition;
			var v = m.vertices;
			var n = m.normals;
			for (int i = 0; i < v.Length; i++)
			{
				v[i] = inv * (v[i] - o);
				n[i] = inv * n[i];
			}
			m.vertices = v;
			m.normals = n;
			m.RecalculateBounds();
			m.RecalculateTangents();
			return m;
		}

		private static Mesh Save(Mesh m, string name)
		{
			m.name = name;
			if (m.vertexCount > 65000)
			{
				m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
			}
			string path = Dir + "/Meshes/" + name + ".asset";
			Directory.CreateDirectory(Dir + "/Meshes");
			var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
			if (existing != null)
			{
				EditorUtility.CopySerialized(m, existing);
				return existing;
			}
			AssetDatabase.CreateAsset(m, path);
			return m;
		}

		// ================================================================ fuselage

		// (z, half width, half height, center y) keyframes from nose to tail.
		private static readonly float[,] Profile =
		{
			{ 2.62f, 0.000f, 0.000f, -0.06f },
			{ 2.50f, 0.115f, 0.125f, -0.05f },
			{ 2.30f, 0.215f, 0.230f, -0.03f },
			{ 1.95f, 0.295f, 0.330f, -0.01f },
			{ 1.40f, 0.335f, 0.405f, 0.02f },
			{ 0.80f, 0.335f, 0.420f, 0.04f },
			{ 0.20f, 0.300f, 0.390f, 0.06f },
			{ -0.40f, 0.240f, 0.310f, 0.08f },
			{ -1.20f, 0.155f, 0.200f, 0.12f },
			{ -2.30f, 0.105f, 0.135f, 0.16f },
			{ -3.60f, 0.082f, 0.118f, 0.20f },
			{ -4.30f, 0.065f, 0.130f, 0.24f },
			{ -4.62f, 0.000f, 0.020f, 0.26f },
		};

		private const float CanopyFront = 2.02f, CanopyBack = 0.30f, CanopyHalfAngle = 62f;

		private static void Section(float z, out float w, out float h, out float cy)
		{
			int n = Profile.GetLength(0);
			for (int i = 0; i < n - 1; i++)
			{
				float z0 = Profile[i, 0], z1 = Profile[i + 1, 0];
				if (z <= z0 && z >= z1)
				{
					float t = (z0 - z) / (z0 - z1);
					int a = Mathf.Max(0, i - 1), d = Mathf.Min(n - 1, i + 2);
					w = CatmullRom(Profile[a, 1], Profile[i, 1], Profile[i + 1, 1], Profile[d, 1], t);
					h = CatmullRom(Profile[a, 2], Profile[i, 2], Profile[i + 1, 2], Profile[d, 2], t);
					cy = CatmullRom(Profile[a, 3], Profile[i, 3], Profile[i + 1, 3], Profile[d, 3], t);
					// Rounded nose / tail caps
					if (i == 0) { float k = Mathf.Sqrt(Mathf.Clamp01(t)); w = Profile[1, 1] * k; h = Profile[1, 2] * k; }
					w = Mathf.Max(0f, w); h = Mathf.Max(0f, h);
					return;
				}
			}
			w = h = 0f; cy = 0f;
		}

		private static float CatmullRom(float p0, float p1, float p2, float p3, float t)
		{
			return 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t + (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t);
		}

		// Superellipse point; angle 0 = top, increasing towards +x.
		private static Vector3 Ring(float z, float angleDeg, float w, float h, float cy, float exponent = 2.3f)
		{
			float a = angleDeg * Mathf.Deg2Rad;
			float s = Mathf.Sin(a), c = Mathf.Cos(a);
			float x = Mathf.Sign(s) * Mathf.Pow(Mathf.Abs(s), 2f / exponent) * w;
			float y = Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), 2f / exponent) * h;
			return new Vector3(x, cy + y, z);
		}

		private static Mesh Fuselage(out Mesh canopy)
		{
			const int radial = 56;
			var zs = new List<float>();
			float zNose = Profile[0, 0], zTail = Profile[Profile.GetLength(0) - 1, 0];
			for (int i = 0; i <= 160; i++)
			{
				float t = i / 160f;
				t = t * t * (3f - 2f * t) * 0.35f + t * 0.65f; // denser at both ends
				zs.Add(Mathf.Lerp(zNose, zTail, t));
			}
			var v = new List<Vector3>();
			var uv = new List<Vector2>();
			var body = new List<int>();
			var cockpit = new List<int>();
			int rowLen = radial + 1;
			foreach (float z in zs)
			{
				Section(z, out float w, out float h, out float cy);
				float inCockpit = CockpitFactor(z);
				for (int r = 0; r <= radial; r++)
				{
					float ang = r * 360f / radial;
					Vector3 p = Ring(z, ang, w, h, cy);
					// Lower the cockpit opening into a tub so the canopy shows an interior
					float topness = Mathf.Clamp01(1f - Mathf.Abs(Mathf.DeltaAngle(0f, ang)) / CanopyHalfAngle);
					if (inCockpit > 0f && topness > 0f)
					{
						float sink = inCockpit * Mathf.SmoothStep(0f, 1f, topness * 1.6f);
						p.y -= sink * h * 0.55f;
						p.x *= 1f - sink * 0.12f;
					}
					v.Add(p);
					uv.Add(new Vector2(ang / 360f, (z - zTail) / (zNose - zTail)));
				}
			}
			for (int i = 0; i < zs.Count - 1; i++)
			{
				float zc = (zs[i] + zs[i + 1]) * 0.5f;
				bool cockpitRow = CockpitFactor(zc) > 0.35f;
				for (int r = 0; r < radial; r++)
				{
					int a = i * rowLen + r, b = a + 1, c = a + rowLen, d = c + 1;
					float angMid = (r + 0.5f) * 360f / radial;
					bool open = cockpitRow && Mathf.Abs(Mathf.DeltaAngle(0f, angMid)) < CanopyHalfAngle * 0.72f;
					List<int> list = open ? cockpit : body;
					list.AddRange(new[] { a, b, c, b, d, c });
				}
			}
			var m = new Mesh();
			m.SetVertices(v);
			m.SetUVs(0, uv);
			m.subMeshCount = 2;
			m.SetTriangles(body, 0);
			m.SetTriangles(cockpit, 1);
			m.RecalculateNormals();
			m.RecalculateTangents();
			m.RecalculateBounds();

			// Canopy: bubble over the cockpit following the un-sunk fuselage section, slightly bulged.
			var cv = new List<Vector3>();
			var ct = new List<int>();
			int steps = 40, arc = 28;
			for (int i = 0; i <= steps; i++)
			{
				float t = i / (float)steps;
				float z = Mathf.Lerp(CanopyFront, CanopyBack, t);
				Section(z, out float w, out float h, out float cy);
				float bulge = 1f + 0.10f * Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.8f));
				float lift = 0.07f * Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.7f));
				for (int r = 0; r <= arc; r++)
				{
					float ang = Mathf.Lerp(-CanopyHalfAngle, CanopyHalfAngle, r / (float)arc);
					Vector3 p = Ring(z, ang, w * 1.015f, h * 1.015f * bulge, cy);
					p.y += lift * Mathf.Cos(ang * Mathf.Deg2Rad);
					cv.Add(p);
				}
			}
			for (int i = 0; i < steps; i++)
			{
				for (int r = 0; r < arc; r++)
				{
					int a = i * (arc + 1) + r, b = a + 1, c = a + arc + 1, d = c + 1;
					ct.AddRange(new[] { a, b, c, b, d, c });
				}
			}
			canopy = new Mesh();
			canopy.SetVertices(cv);
			canopy.SetTriangles(ct, 0);
			canopy.RecalculateNormals();
			canopy.RecalculateBounds();
			return m;
		}

		private static float CockpitFactor(float z)
		{
			if (z > CanopyFront - 0.05f || z < CanopyBack + 0.05f)
			{
				return 0f;
			}
			float t = Mathf.InverseLerp(CanopyFront - 0.05f, CanopyBack + 0.05f, z);
			return Mathf.SmoothStep(0f, 1f, Mathf.Min(t, 1f - t) * 6f);
		}

		private static Mesh Pilot()
		{
			// Helmet (submesh 1, accent) and shoulders/torso (submesh 0)
			var head = Sphere(0.125f, 16, new Vector3(0f, 0.30f, 0.98f));
			var torso = Ellipsoid(new Vector3(0.21f, 0.16f, 0.17f), 16, new Vector3(0f, 0.06f, 0.82f));
			return Combine(new[] { torso, head }, new[] { 0, 1 });
		}

		// ================================================================ lifting surfaces

		private struct Station
		{
			public Vector3 le;      // leading edge point
			public Vector3 chordDir; // LE -> TE
			public Vector3 upDir;    // thickness direction
			public float chord, thickness, camber;
		}

		private static void Airfoil(float x, float t, float m, float p, out float yu, out float yl)
		{
			float yt = 5f * t * (0.2969f * Mathf.Sqrt(x) - 0.1260f * x - 0.3516f * x * x + 0.2843f * x * x * x - 0.1036f * x * x * x * x);
			float yc = m <= 0f ? 0f : (x < p ? m / (p * p) * (2f * p * x - x * x) : m / ((1f - p) * (1f - p)) * ((1f - 2f * p) + 2f * p * x - x * x));
			yu = yc + yt;
			yl = yc - yt;
		}

		// Lofts stations into a closed surface covering chord fraction [x0, x1]; flat faces at cut lines and end caps.
		private static Mesh Loft(List<Station> st, float x0, float x1, int chordPts, bool capStart, bool capEnd)
		{
			var xs = new List<float>();
			for (int i = 0; i <= chordPts; i++)
			{
				float c = 0.5f - 0.5f * Mathf.Cos(Mathf.PI * i / chordPts); // cosine spacing
				xs.Add(Mathf.Lerp(x0, x1, c));
			}
			// Smooth polyline: lower (x1 -> x0) then upper (x0 -> x1); the leading edge point is shared when x0 == 0.
			var loop = new List<(float x, bool upper)>();
			for (int i = xs.Count - 1; i >= 0; i--) loop.Add((xs[i], false));
			for (int i = (x0 <= 0f ? 1 : 0); i < xs.Count; i++) loop.Add((xs[i], true));

			var v = new List<Vector3>();
			var tris = new List<int>();
			var pts = new Vector3[st.Count, loop.Count];
			for (int s = 0; s < st.Count; s++)
			{
				for (int j = 0; j < loop.Count; j++)
				{
					Airfoil(loop[j].x, st[s].thickness, st[s].camber, 0.4f, out float yu, out float yl);
					float y = loop[j].upper ? yu : yl;
					pts[s, j] = st[s].le + st[s].chordDir * (loop[j].x * st[s].chord) + st[s].upDir * (y * st[s].chord);
				}
			}
			int n = loop.Count;
			for (int s = 0; s < st.Count; s++) for (int j = 0; j < n; j++) v.Add(pts[s, j]);
			for (int s = 0; s < st.Count - 1; s++)
			{
				for (int j = 0; j < n - 1; j++)
				{
					int a = s * n + j, b = a + 1, c = a + n, d = c + 1;
					tris.AddRange(new[] { a, c, b, b, c, d });
				}
			}
			var mesh = new Mesh();
			mesh.SetVertices(v);
			mesh.SetTriangles(tris, 0);
			mesh.RecalculateNormals();
			FixOrientation(mesh, st);
			var parts = new List<Mesh> { mesh };

			// Flat faces: trailing cut (x1 < 1) joins upper(x1) and lower(x1); leading cut (x0 > 0) joins lower(x0) and upper(x0).
			if (x1 < 0.999f) parts.Add(Strip(st, s => pts[s, n - 1], s => pts[s, 0]));
			if (x0 > 0.001f) parts.Add(Strip(st, s => pts[s, xs.Count - 1], s => pts[s, xs.Count]));
			if (capStart) parts.Add(Cap(Enumerable(pts, 0, n)));
			if (capEnd) parts.Add(Cap(Enumerable(pts, st.Count - 1, n)));
			return Combine(parts.ToArray(), new int[parts.Count]);
		}

		private static List<Vector3> Enumerable(Vector3[,] pts, int s, int n)
		{
			var l = new List<Vector3>();
			for (int j = 0; j < n; j++) l.Add(pts[s, j]);
			return l;
		}

		private static Mesh Strip(List<Station> st, System.Func<int, Vector3> a, System.Func<int, Vector3> b)
		{
			var v = new List<Vector3>();
			var t = new List<int>();
			for (int s = 0; s < st.Count; s++) { v.Add(a(s)); v.Add(b(s)); }
			for (int s = 0; s < st.Count - 1; s++)
			{
				int i = s * 2;
				t.AddRange(new[] { i, i + 2, i + 1, i + 1, i + 2, i + 3 });
				t.AddRange(new[] { i, i + 1, i + 2, i + 1, i + 3, i + 2 }); // both sides (thin faces)
			}
			var m = new Mesh();
			m.SetVertices(v);
			m.SetTriangles(t, 0);
			m.RecalculateNormals();
			return m;
		}

		private static Mesh Cap(List<Vector3> ring)
		{
			var v = new List<Vector3>(ring);
			Vector3 c = Vector3.zero;
			foreach (var p in ring) c += p;
			c /= ring.Count;
			v.Add(c);
			var t = new List<int>();
			for (int j = 0; j < ring.Count; j++)
			{
				int k = (j + 1) % ring.Count;
				t.AddRange(new[] { ring.Count, j, k, ring.Count, k, j });
			}
			var m = new Mesh();
			m.SetVertices(v);
			m.SetTriangles(t, 0);
			m.RecalculateNormals();
			return m;
		}

		// Makes the lofted skin face outwards (away from the local chord line).
		private static void FixOrientation(Mesh m, List<Station> st)
		{
			var v = m.vertices;
			var n = m.normals;
			int outward = 0;
			for (int i = 0; i < v.Length; i++)
			{
				Station s = st[Mathf.Min(st.Count - 1, i / Mathf.Max(1, v.Length / st.Count))];
				Vector3 mid = s.le + s.chordDir * s.chord * 0.5f;
				outward += Vector3.Dot(n[i], v[i] - mid) > 0f ? 1 : -1;
			}
			if (outward < 0)
			{
				var t = m.triangles;
				for (int i = 0; i < t.Length; i += 3) (t[i + 1], t[i + 2]) = (t[i + 2], t[i + 1]);
				m.triangles = t;
				m.RecalculateNormals();
			}
		}

		private static void WingMeshes(int side, out Mesh main, out Mesh aileron, out Mesh tip, out Vector3 hingeA, out Vector3 hingeB, out Vector3 tipPoint, out Vector3 navPoint)
		{
			const float semi = 7.35f, kink = 0.55f, rootX = 0.22f, rootY = 0.36f, rootLeZ = 0.40f;
			System.Func<float, Station> wingAt = s =>
			{
				float x = rootX + s * (semi - rootX);
				float chord = s < kink ? Mathf.Lerp(0.95f, 0.72f, s / kink) : Mathf.Lerp(0.72f, 0.40f, (s - kink) / (1f - kink));
				float sweep = s < kink ? 0f : (x - (rootX + kink * (semi - rootX))) * Mathf.Tan(4f * Mathf.Deg2Rad);
				float kinkX = rootX + kink * (semi - rootX);
				float y = rootY + Mathf.Min(x, kinkX) * Mathf.Tan(2.5f * Mathf.Deg2Rad) + Mathf.Max(0f, x - kinkX) * Mathf.Tan(4.5f * Mathf.Deg2Rad);
				float incidence = Mathf.Lerp(2f, -1f, s) * Mathf.Deg2Rad;
				var chordDir = new Vector3(0f, -Mathf.Sin(incidence), -Mathf.Cos(incidence));
				var up = new Vector3(0f, Mathf.Cos(incidence), -Mathf.Sin(incidence));
				return new Station { le = new Vector3(side * x, y, rootLeZ - sweep), chordDir = chordDir, upDir = up, chord = chord, thickness = Mathf.Lerp(0.15f, 0.12f, s), camber = 0.03f };
			};
			const float a0 = 0.50f, a1 = 0.93f, hinge = 0.76f, tipStart = 0.92f;
			var inner = new List<Station>();
			for (int i = 0; i <= 10; i++) inner.Add(wingAt(Mathf.Lerp(0f, a0, i / 10f)));
			var mid = new List<Station>();
			for (int i = 0; i <= 12; i++) mid.Add(wingAt(Mathf.Lerp(a0, a1, i / 12f)));
			var ail = new List<Station>();
			for (int i = 0; i <= 12; i++) ail.Add(wingAt(Mathf.Lerp(a0 + 0.004f, a1 - 0.004f, i / 12f)));

			// Accent-colored tip: from tipStart over the end of the wing, then a blended winglet.
			var outer = new List<Station>();
			for (int i = 0; i <= 4; i++) outer.Add(wingAt(Mathf.Lerp(tipStart, 1f, i / 4f)));
			Station last = outer[outer.Count - 1];
			Vector3 pos = last.le;
			float chordW = last.chord;
			const int bend = 10, rise = 8;
			for (int i = 1; i <= bend + rise; i++)
			{
				float th = Mathf.SmoothStep(0f, 75f, Mathf.Clamp01(i / (float)bend)) * Mathf.Deg2Rad;
				var spanDir = new Vector3(side * Mathf.Cos(th), Mathf.Sin(th), 0f);
				var upDir = new Vector3(-side * Mathf.Sin(th), Mathf.Cos(th), 0f);
				float ds = i <= bend ? 0.032f : 0.055f;
				pos += spanDir * ds + new Vector3(0f, 0f, -(i <= bend ? 0.01f : 0.03f));
				chordW = i <= bend ? Mathf.Lerp(last.chord, last.chord * 0.92f, i / (float)bend) : Mathf.Lerp(last.chord * 0.92f, 0.17f, (i - bend) / (float)rise);
				outer.Add(new Station { le = pos, chordDir = last.chordDir, upDir = upDir, chord = chordW, thickness = 0.11f, camber = 0.015f });
			}

			main = Combine(new[] { Loft(inner, 0f, 1f, 30, false, false), Loft(mid, 0f, hinge, 26, true, true) }, new[] { 0, 0 });
			tip = Loft(outer, 0f, 1f, 30, true, true);
			aileron = Loft(ail, hinge + 0.006f, 1f, 10, true, true);
			if (side < 0) { Flip(main); Flip(tip); Flip(aileron); }

			Station h0 = ail[0], h1 = ail[ail.Count - 1];
			hingeA = h0.le + h0.chordDir * (hinge * h0.chord);
			hingeB = h1.le + h1.chordDir * (hinge * h1.chord);
			if (side < 0) (hingeA, hingeB) = (hingeB, hingeA);
			Station top = outer[outer.Count - 1];
			tipPoint = top.le + top.chordDir * top.chord * 0.7f;
			Station wt = outer[4];
			navPoint = wt.le + wt.chordDir * 0.06f + new Vector3(side * 0.02f, 0f, 0f);
		}

		private static void FinMeshes(out Mesh fin, out Mesh rudder, out Vector3 hingeA, out Vector3 hingeB)
		{
			System.Func<float, Station> finAt = s =>
			{
				float y = Mathf.Lerp(0.20f, 1.34f, s);
				float chord = Mathf.Lerp(1.08f, 0.66f, s);
				float le = Mathf.Lerp(-3.55f, -4.02f, s);
				return new Station { le = new Vector3(0f, y, le), chordDir = Vector3.back, upDir = Vector3.right, chord = chord, thickness = 0.11f, camber = 0f };
			};
			const float hinge = 0.66f;
			var st = new List<Station>();
			for (int i = 0; i <= 12; i++) st.Add(finAt(i / 12f));
			var rs = new List<Station>();
			for (int i = 0; i <= 12; i++) rs.Add(finAt(Mathf.Lerp(0.06f, 0.96f, i / 12f)));
			fin = FinUV(Loft(st, 0f, hinge, 26, true, true), st);
			rudder = Loft(rs, hinge + 0.006f, 1f, 10, true, true);
			hingeA = rs[0].le + Vector3.back * hinge * rs[0].chord;
			hingeB = rs[rs.Count - 1].le + Vector3.back * hinge * rs[rs.Count - 1].chord;
		}

		// Fin UVs for the livery: u along the chord (z), v up the span (y).
		private static Mesh FinUV(Mesh m, List<Station> st)
		{
			var v = m.vertices;
			var uv = new Vector2[v.Length];
			for (int i = 0; i < v.Length; i++)
			{
				float s = Mathf.InverseLerp(0.20f, 1.34f, v[i].y);
				float le = Mathf.Lerp(-3.55f, -4.02f, s), chord = Mathf.Lerp(1.08f, 0.66f, s);
				uv[i] = new Vector2(Mathf.Clamp01((le - v[i].z) / chord) * (v[i].x >= 0f ? 1f : -1f) * 0.5f + 0.5f, s);
			}
			m.uv = uv;
			m.RecalculateTangents();
			return m;
		}

		private static void StabMeshes(out Mesh stab, out Mesh elevator, out Vector3 hingeA, out Vector3 hingeB)
		{
			const float semi = 1.38f, hinge = 0.62f;
			System.Func<float, Station> at = s =>
			{
				float x = s * semi;
				float chord = Mathf.Lerp(0.64f, 0.40f, Mathf.Abs(s));
				float le = -4.10f - Mathf.Abs(s) * 0.14f;
				return new Station { le = new Vector3(x, 1.36f, le), chordDir = Vector3.back, upDir = Vector3.up, chord = chord, thickness = 0.10f, camber = 0f };
			};
			var st = new List<Station>();
			for (int i = 0; i <= 16; i++) st.Add(at(Mathf.Lerp(-1f, 1f, i / 16f)));
			var es = new List<Station>();
			for (int i = 0; i <= 16; i++) es.Add(at(Mathf.Lerp(-0.96f, 0.96f, i / 16f)));
			stab = Loft(st, 0f, hinge, 24, true, true);
			elevator = Loft(es, hinge + 0.006f, 1f, 10, true, true);
			hingeA = es[0].le + Vector3.back * hinge * es[0].chord;
			hingeB = es[es.Count - 1].le + Vector3.back * hinge * es[es.Count - 1].chord;
		}

		private static void Flip(Mesh m)
		{
			var t = m.triangles;
			for (int i = 0; i < t.Length; i += 3) (t[i + 1], t[i + 2]) = (t[i + 2], t[i + 1]);
			m.triangles = t;
			m.RecalculateNormals();
		}

		// ================================================================ primitives

		private static Mesh Wheel()
		{
			var tire = Cylinder(new Vector3(-0.05f, -0.30f, -0.05f), new Vector3(0.05f, -0.30f, -0.05f), 0.19f, 20);
			var hub = Cylinder(new Vector3(-0.055f, -0.30f, -0.05f), new Vector3(0.055f, -0.30f, -0.05f), 0.08f, 12);
			return Combine(new[] { tire, hub }, new[] { 0, 1 });
		}

		private static Mesh Cylinder(Vector3 a, Vector3 b, float r, int seg)
		{
			var axis = (b - a).normalized;
			var u = Vector3.Cross(axis, Mathf.Abs(axis.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
			var w = Vector3.Cross(axis, u);
			var v = new List<Vector3>();
			var t = new List<int>();
			for (int i = 0; i <= seg; i++)
			{
				float ang = i * Mathf.PI * 2f / seg;
				var d = u * Mathf.Cos(ang) + w * Mathf.Sin(ang);
				v.Add(a + d * r);
				v.Add(b + d * r);
			}
			for (int i = 0; i < seg; i++)
			{
				int k = i * 2;
				t.AddRange(new[] { k, k + 2, k + 1, k + 1, k + 2, k + 3 });
			}
			int ca = v.Count; v.Add(a);
			int cb = v.Count; v.Add(b);
			for (int i = 0; i < seg; i++)
			{
				t.AddRange(new[] { ca, i * 2 + 2, i * 2 });
				t.AddRange(new[] { cb, i * 2 + 1, i * 2 + 3 });
			}
			var m = new Mesh();
			m.SetVertices(v);
			m.SetTriangles(t, 0);
			m.RecalculateNormals();
			return m;
		}

		private static Mesh Sphere(float r, int seg, Vector3 c)
		{
			return Ellipsoid(Vector3.one * r, seg, c);
		}

		private static Mesh Ellipsoid(Vector3 r, int seg, Vector3 c)
		{
			var v = new List<Vector3>();
			var n = new List<Vector3>();
			var t = new List<int>();
			for (int i = 0; i <= seg; i++)
			{
				float th = Mathf.PI * i / seg;
				for (int j = 0; j <= seg; j++)
				{
					float ph = 2f * Mathf.PI * j / seg;
					var d = new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph));
					v.Add(c + Vector3.Scale(d, r));
					n.Add(new Vector3(d.x / r.x, d.y / r.y, d.z / r.z).normalized);
				}
			}
			for (int i = 0; i < seg; i++)
			{
				for (int j = 0; j < seg; j++)
				{
					int a = i * (seg + 1) + j, b = a + 1, cc = a + seg + 1, d = cc + 1;
					t.AddRange(new[] { a, b, cc, b, d, cc });
				}
			}
			var m = new Mesh();
			m.SetVertices(v);
			m.SetNormals(n);
			m.SetTriangles(t, 0);
			return m;
		}

		// Combines meshes; submesh[i] selects which output submesh each input lands in.
		private static Mesh Combine(Mesh[] meshes, int[] submesh)
		{
			int subCount = 0;
			foreach (int s in submesh) subCount = Mathf.Max(subCount, s + 1);
			var v = new List<Vector3>();
			var n = new List<Vector3>();
			var uv = new List<Vector2>();
			var tris = new List<int>[subCount];
			for (int i = 0; i < subCount; i++) tris[i] = new List<int>();
			for (int k = 0; k < meshes.Length; k++)
			{
				Mesh m = meshes[k];
				int b = v.Count;
				v.AddRange(m.vertices);
				Vector3[] mn = m.normals;
				if (mn == null || mn.Length != m.vertexCount) { m.RecalculateNormals(); mn = m.normals; }
				n.AddRange(mn);
				Vector2[] muv = m.uv;
				if (muv == null || muv.Length != m.vertexCount) muv = new Vector2[m.vertexCount];
				uv.AddRange(muv);
				for (int s = 0; s < m.subMeshCount; s++)
				{
					foreach (int i in m.GetTriangles(s)) tris[submesh[k]].Add(i + b);
				}
			}
			var o = new Mesh();
			if (v.Count > 65000) o.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
			o.SetVertices(v);
			o.SetNormals(n);
			o.SetUVs(0, uv);
			o.subMeshCount = subCount;
			for (int i = 0; i < subCount; i++) o.SetTriangles(tris[i], i);
			o.RecalculateTangents();
			o.RecalculateBounds();
			return o;
		}

		// ================================================================ materials & livery

		private static Material Lit(string name, Color color, float smoothness, float metallic, bool clearCoat, Texture2D map)
		{
			string path = Dir + "/" + name + ".mat";
			var m = AssetDatabase.LoadAssetAtPath<Material>(path);
			if (m == null)
			{
				m = new Material(Shader.Find("HDRP/Lit"));
				AssetDatabase.CreateAsset(m, path);
			}
			m.SetColor("_BaseColor", color);
			m.SetTexture("_BaseColorMap", map);
			m.SetFloat("_Smoothness", smoothness);
			m.SetFloat("_Metallic", metallic);
			m.SetFloat("_CoatMask", clearCoat ? 1f : 0f);
			UnityEditor.Rendering.HighDefinition.HDShaderUtils.ResetMaterialKeywords(m);
			EditorUtility.SetDirty(m);
			return m;
		}

		private static Material Glass()
		{
			var m = Lit("Canopy_Glass", new Color(0.05f, 0.07f, 0.09f, 0.45f), 0.97f, 0f, false, null);
			m.SetFloat("_SurfaceType", 1f);
			m.SetFloat("_BlendMode", 0f);
			m.SetFloat("_EnableBlendModePreserveSpecularLighting", 1f);
			m.SetFloat("_DoubleSidedEnable", 1f);
			UnityEditor.Rendering.HighDefinition.HDShaderUtils.ResetMaterialKeywords(m);
			return m;
		}

		private static Material Emissive(string name, Color color, float nits)
		{
			var m = Lit(name, color * 0.2f, 0.6f, 0f, false, null);
			m.SetColor("_EmissiveColor", color * nits);
			m.SetColor("_EmissiveColorLDR", color);
			m.SetFloat("_UseEmissiveIntensity", 0f);
			UnityEditor.Rendering.HighDefinition.HDShaderUtils.ResetMaterialKeywords(m);
			return m;
		}

		// Fuselage livery (u = around, v = along, nose at v=1): white with a swoosh stripe and a pinstripe on
		// both sides. Fin livery (u = chord either side, v = span): accent top band and the race number.
		private static Texture2D LiveryTexture(Color accent, int number, bool fin, string path)
		{
			int w = fin ? 1024 : 2048, h = fin ? 1024 : 512;
			var px = new Color[w * h];
			Color white = new Color(0.94f, 0.94f, 0.95f), dark = new Color(0.06f, 0.07f, 0.09f);
			for (int y = 0; y < h; y++)
			{
				for (int x = 0; x < w; x++)
				{
					float u = (x + 0.5f) / w, v = (y + 0.5f) / h;
					Color c = white;
					if (!fin)
					{
						float ang = u * 360f; // 0 top, 90 right side, 180 bottom, 270 left
						float fromTop = Mathf.Abs(Mathf.DeltaAngle(ang, 0f));
						// Swoosh: low on the nose, rising and thinning towards the tail, mirrored on both sides.
						float target = Mathf.Lerp(72f, 120f, GliderJam.World.WorldNoise.SmoothStep(0.2f, 1f, v));
						float width = Mathf.Lerp(4f, 24f, GliderJam.World.WorldNoise.SmoothStep(0.15f, 0.85f, v));
						float d = Mathf.Abs(fromTop - target);
						float stripe = Smooth(width - d);
						float pin = Smooth(1.6f - Mathf.Abs(d - width - 3.5f));
						c = Color.Lerp(c, accent, stripe);
						c = Color.Lerp(c, dark, pin * GliderJam.World.WorldNoise.SmoothStep(0.15f, 0.3f, v));
						// Nose cone and anti-glare panel ahead of the canopy
						c = Color.Lerp(c, accent, GliderJam.World.WorldNoise.SmoothStep(0.965f, 0.975f, v));
						float glare = Smooth(18f - Mathf.Abs(Mathf.DeltaAngle(ang, 0f))) * Smooth((v - 0.885f) * 400f) * Smooth((0.965f - v) * 400f);
						c = Color.Lerp(c, dark, glare * 0.9f);
					}
					else
					{
						float chordU = Mathf.Abs(u - 0.5f) * 2f; // 0 = LE, 1 = hinge line, both sides
						c = Color.Lerp(c, accent, GliderJam.World.WorldNoise.SmoothStep(0.80f, 0.81f, v));
						c = Color.Lerp(c, dark, Smooth((0.012f - Mathf.Abs(v - 0.79f)) * 2000f));
						// Right side (u > 0.5) is seen with the leading edge on the viewer's right: mirror so it reads correctly.
						float dx = (chordU - 0.12f) / 0.44f;
						if (u > 0.5f) dx = 1f - dx;
						float digit = Digit(number, dx, (v - 0.30f) / 0.42f);
						float outline = Digit(number, dx, (v - 0.30f) / 0.42f, 0.055f);
						c = Color.Lerp(c, dark, outline);
						c = Color.Lerp(c, accent, digit);
					}
					px[y * w + x] = c;
				}
			}
			var tex = new Texture2D(w, h, TextureFormat.RGBA32, true);
			tex.SetPixels(px);
			tex.Apply();
			File.WriteAllBytes(path, tex.EncodeToPNG());
			Object.DestroyImmediate(tex);
			AssetDatabase.ImportAsset(path);
			var ti = (TextureImporter)AssetImporter.GetAtPath(path);
			ti.wrapMode = fin ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
			ti.anisoLevel = 8;
			ti.SaveAndReimport();
			return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
		}

		private static float Smooth(float d)
		{
			return Mathf.Clamp01(d * 0.5f + 0.5f);
		}

		// Thick-stroke digits rendered from line segments in a unit box; returns coverage with antialiasing.
		private static float Digit(int number, float x, float y, float extra = 0f)
		{
			if (x < -0.3f || x > 1.3f || y < -0.3f || y > 1.3f) return 0f;
			Vector2[] segs = number == 1
				? new[] { new Vector2(0.3f, 0.78f), new Vector2(0.58f, 1f), new Vector2(0.58f, 1f), new Vector2(0.58f, 0f), new Vector2(0.3f, 0f), new Vector2(0.86f, 0f) }
				: new[] { new Vector2(0.18f, 0.78f), new Vector2(0.35f, 0.97f), new Vector2(0.35f, 0.97f), new Vector2(0.68f, 0.97f), new Vector2(0.68f, 0.97f), new Vector2(0.84f, 0.75f), new Vector2(0.84f, 0.75f), new Vector2(0.80f, 0.55f), new Vector2(0.80f, 0.55f), new Vector2(0.18f, 0f), new Vector2(0.18f, 0f), new Vector2(0.86f, 0f) };
			var p = new Vector2(x, y);
			float best = 10f;
			for (int i = 0; i < segs.Length; i += 2)
			{
				Vector2 a = segs[i], b = segs[i + 1], ab = b - a;
				float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
				best = Mathf.Min(best, (p - (a + ab * t)).magnitude);
			}
			float half = 0.085f + extra;
			return Mathf.Clamp01((half - best) * 300f);
		}
	}
}
