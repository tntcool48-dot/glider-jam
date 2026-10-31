using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GliderJam.World
{
	// Draws the terrain's tree instances with GPU instancing (Graphics.RenderMeshInstanced) grouped into spatial
	// cells, with distance LOD and shadow cut-off. Unity's per-tree terrain drawing cost ~90k draw calls with
	// two cameras; this keeps it to a few hundred. Tree data stays in TerrainData (the source of truth).
	[ExecuteAlways]
	public class InstancedForest : MonoBehaviour
	{
		public float cellSize = 250f;
		public float lod0Distance = 320f;
		public float shadowDistance = 650f;
		public float cullDistance = 3500f;
		[Range(0.05f, 1f)] public float density = 1f;

		// Vegetation setting 0 low .. 3 ultra.
		public void ApplyQuality(int level)
		{
			level = Mathf.Clamp(level, 0, 3);
			float d = new[] { 0.35f, 0.65f, 1f, 1f }[level];
			lod0Distance = new[] { 140f, 240f, 320f, 560f }[level];
			shadowDistance = new[] { 0f, 280f, 650f, 1200f }[level];
			cullDistance = new[] { 1500f, 2500f, 3500f, 6000f }[level];
			if (!Mathf.Approximately(d, density))
			{
				density = d;
				Build();
			}
		}

		private class Proto
		{
			public Mesh lod0, lod1;
			public Material[] materials;
		}

		private class Cell
		{
			public Bounds bounds;
			public List<Matrix4x4>[] perProto;
			public Matrix4x4[][] arrays;
		}

		private readonly List<Proto> _protos = new List<Proto>();
		private readonly List<Cell> _cells = new List<Cell>();
		private readonly List<Terrain> _terrains = new List<Terrain>();
		private readonly List<Vector3> _viewers = new List<Vector3>();
		private static readonly Matrix4x4[] Scratch = new Matrix4x4[1023];

		private void OnEnable()
		{
			Build();
		}

		private void OnDisable()
		{
			foreach (Terrain t in _terrains)
			{
				if (t != null) t.drawTreesAndFoliage = true;
			}
			_cells.Clear();
			_protos.Clear();
		}

		public void Build()
		{
			_cells.Clear();
			_protos.Clear();
			_terrains.Clear();
			var protoIndex = new Dictionary<GameObject, int>();
			var cells = new Dictionary<Vector2Int, Cell>();
			foreach (Terrain t in FindObjectsByType<Terrain>(FindObjectsSortMode.None))
			{
				TerrainData d = t.terrainData;
				if (d.treeInstanceCount == 0) continue;
				_terrains.Add(t);
				var map = new int[d.treePrototypes.Length];
				for (int i = 0; i < map.Length; i++)
				{
					GameObject prefab = d.treePrototypes[i].prefab;
					if (!protoIndex.TryGetValue(prefab, out int pi))
					{
						pi = _protos.Count;
						protoIndex[prefab] = pi;
						_protos.Add(MakeProto(prefab));
					}
					map[i] = pi;
				}
				Vector3 o = t.transform.position, size = d.size;
				int n = 0;
				foreach (TreeInstance ti in d.treeInstances)
				{
					// Deterministic thinning for lower vegetation settings.
					if (density < 1f && WorldNoise.Hash(n++, 17, 5) > density) continue;
					Vector3 p = o + Vector3.Scale(ti.position, size);
					p.y = o.y + t.SampleHeight(p);
					if (p.y < CanyonAmbience.TreeLine) continue; // under the valley water
					var m = Matrix4x4.TRS(p, Quaternion.Euler(0f, ti.rotation * Mathf.Rad2Deg, 0f), new Vector3(ti.widthScale, ti.heightScale, ti.widthScale));
					var key = new Vector2Int(Mathf.FloorToInt(p.x / cellSize), Mathf.FloorToInt(p.z / cellSize));
					if (!cells.TryGetValue(key, out Cell c))
					{
						c = new Cell { bounds = new Bounds(p, Vector3.one), perProto = new List<Matrix4x4>[_protos.Count + 8] };
						cells[key] = c;
					}
					c.bounds.Encapsulate(p + Vector3.up * 30f);
					c.bounds.Encapsulate(p - Vector3.up * 2f);
					int idx = map[ti.prototypeIndex];
					(c.perProto[idx] ??= new List<Matrix4x4>()).Add(m);
				}
				t.drawTreesAndFoliage = false;
			}
			foreach (Cell c in cells.Values)
			{
				c.bounds.Expand(new Vector3(15f, 0f, 15f));
				c.arrays = new Matrix4x4[_protos.Count][];
				for (int i = 0; i < _protos.Count; i++) c.arrays[i] = c.perProto[i]?.ToArray() ?? new Matrix4x4[0];
				c.perProto = null;
				_cells.Add(c);
			}
		}

		private static Proto MakeProto(GameObject prefab)
		{
			var lod = prefab.GetComponent<LODGroup>();
			var p = new Proto();
			Renderer r0 = lod != null ? lod.GetLODs()[0].renderers[0] : prefab.GetComponentInChildren<Renderer>();
			Renderer r1 = lod != null && lod.lodCount > 1 ? lod.GetLODs()[1].renderers[0] : r0;
			p.lod0 = r0.GetComponent<MeshFilter>().sharedMesh;
			p.lod1 = r1.GetComponent<MeshFilter>().sharedMesh;
			p.materials = r0.sharedMaterials;
			return p;
		}

		private void Update()
		{
			if (_cells.Count == 0) return;
			_viewers.Clear();
			foreach (Camera c in Camera.allCameras) _viewers.Add(c.transform.position);
#if UNITY_EDITOR
			foreach (UnityEditor.SceneView sv in UnityEditor.SceneView.sceneViews)
			{
				if (sv.camera != null) _viewers.Add(sv.camera.transform.position);
			}
#endif
			if (_viewers.Count == 0) return;
			foreach (Cell cell in _cells)
			{
				float d = float.MaxValue;
				foreach (Vector3 v in _viewers) d = Mathf.Min(d, Mathf.Sqrt(cell.bounds.SqrDistance(v)));
				if (d > cullDistance) continue;
				bool near = d < lod0Distance;
				ShadowCastingMode shadows = d < shadowDistance ? ShadowCastingMode.On : ShadowCastingMode.Off;
				for (int i = 0; i < _protos.Count; i++)
				{
					Matrix4x4[] arr = cell.arrays[i];
					if (arr.Length == 0) continue;
					Proto p = _protos[i];
					Mesh mesh = near ? p.lod0 : p.lod1;
					for (int sub = 0; sub < mesh.subMeshCount && sub < p.materials.Length; sub++)
					{
						var rp = new RenderParams(p.materials[sub])
						{
							worldBounds = cell.bounds,
							shadowCastingMode = shadows,
							receiveShadows = true,
							layer = gameObject.layer
						};
						for (int start = 0; start < arr.Length; start += 1023)
						{
							int n = Mathf.Min(1023, arr.Length - start);
							System.Array.Copy(arr, start, Scratch, 0, n);
							Graphics.RenderMeshInstanced(rp, mesh, sub, Scratch, n);
						}
					}
				}
			}
		}
	}
}
