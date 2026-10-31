using System.Collections.Generic;
using GliderJam.Flight;
using UnityEngine;
using UnityEngine.Rendering;

namespace GliderJam.World
{
	// Golden Pillars ambience: a still river / lake surface over the flat valley floor (trees below it are
	// skipped by the forest), flocks of birds circling between the pillars, and sunlit dust motes drifting
	// around each player camera (on that player's view layer). Follows the Water / Effects settings.
	public class CanyonAmbience : MonoBehaviour
	{
		public Material waterMaterial;
		public Material birdMaterial;
		public Material dustMaterial;
		public Material waterfallMaterial;
		public GameObject vines;
		public Vector4[] waterfalls = new Vector4[0]; // top edge (xyz) and the yaw (degrees) it falls towards
		public float waterLevel = 2.5f;
		public Rect waterArea = new Rect(0f, 0f, 1000f, 7000f);
		public Vector3[] flocks = new Vector3[0];

		// Trees whose base is below this height are not drawn (water covers the valley floor).
		public static float TreeLine = float.NegativeInfinity;

		private GameObject _water;
		private readonly List<ParticleSystem> _birds = new List<ParticleSystem>();
		private readonly Dictionary<Camera, ParticleSystem> _dust = new Dictionary<Camera, ParticleSystem>();
		private float _dustRate = 1f;
		private bool _birdsOn = true;

		public static void ApplyQuality(GameConfig cfg)
		{
			foreach (CanyonAmbience a in FindObjectsByType<CanyonAmbience>(FindObjectsSortMode.None)) a.Apply(cfg);
		}

		private void Awake()
		{
			if (waterMaterial != null) _water = MakeWater();
			if (birdMaterial != null)
			{
				for (int i = 0; i < flocks.Length; i++) _birds.Add(MakeFlock(flocks[i], i));
			}
			if (waterfallMaterial != null)
			{
				foreach (Vector4 w in waterfalls) MakeWaterfall(new Vector3(w.x, w.y, w.z), w.w);
			}
		}

		private readonly List<GameObject> _falls = new List<GameObject>();

		// Water spilling over a pillar edge: a widening plume falling under gravity, spray mist at the foot.
		private void MakeWaterfall(Vector3 top, float yaw)
		{
			var root = new GameObject("Waterfall");
			root.transform.SetParent(transform, false);
			root.transform.SetPositionAndRotation(top, Quaternion.Euler(0f, yaw, 0f));
			_falls.Add(root);
			float drop = Mathf.Max(20f, top.y - waterLevel);
			float fallTime = Mathf.Sqrt(2f * drop / 9.81f);

			var ps = root.AddComponent<ParticleSystem>();
			ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
			var main = ps.main;
			main.loop = true;
			main.duration = 3f;
			main.prewarm = true;
			main.startLifetime = fallTime;
			main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 5f);
			main.startSize = new ParticleSystem.MinMaxCurve(1.6f, 2.6f);
			main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
			main.startColor = new Color(0.95f, 0.97f, 1f, 0.42f);
			main.gravityModifier = 1f;
			main.simulationSpace = ParticleSystemSimulationSpace.World;
			main.maxParticles = 2500;
			var e = ps.emission;
			e.rateOverTime = 260f;
			var shape = ps.shape;
			shape.shapeType = ParticleSystemShapeType.Box;
			shape.scale = new Vector3(9f, 0.5f, 0.5f);
			var size = ps.sizeOverLifetime;
			size.enabled = true;
			size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 2.6f)));
			var col = ps.colorOverLifetime;
			col.enabled = true;
			var g = new Gradient();
			g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
				new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.6f, 0.7f), new GradientAlphaKey(0f, 1f) });
			col.color = g;
			var r = root.GetComponent<ParticleSystemRenderer>();
			r.renderMode = ParticleSystemRenderMode.Stretch; // falling streaks
			r.velocityScale = 0.06f;
			r.lengthScale = 1.2f;
			r.sharedMaterial = waterfallMaterial;
			r.shadowCastingMode = ShadowCastingMode.Off;
			r.receiveShadows = false;
			ps.Play();

			var spray = new GameObject("Spray");
			spray.transform.SetParent(root.transform, false);
			spray.transform.position = new Vector3(top.x, waterLevel + 4f, top.z) + root.transform.forward * (4f + 2.2f * fallTime);
			var sp = spray.AddComponent<ParticleSystem>();
			sp.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
			var sm = sp.main;
			sm.loop = true;
			sm.prewarm = true;
			sm.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f);
			sm.startSpeed = new ParticleSystem.MinMaxCurve(1f, 4f);
			sm.startSize = new ParticleSystem.MinMaxCurve(12f, 22f);
			sm.startColor = new Color(0.92f, 0.95f, 1f, 0.18f);
			sm.simulationSpace = ParticleSystemSimulationSpace.World;
			sm.maxParticles = 80;
			var se = sp.emission;
			se.rateOverTime = 14f;
			var ss = sp.shape;
			ss.shapeType = ParticleSystemShapeType.Hemisphere;
			ss.radius = 6f;
			var scol = sp.colorOverLifetime;
			scol.enabled = true;
			scol.color = g;
			var sr = spray.GetComponent<ParticleSystemRenderer>();
			sr.sharedMaterial = waterfallMaterial;
			sr.shadowCastingMode = ShadowCastingMode.Off;
			sr.receiveShadows = false;
			sp.Play();
		}

		private void Start()
		{
			Apply(GameConfig.Current);
		}

		private void OnDestroy()
		{
			TreeLine = float.NegativeInfinity;
		}

		private void Apply(GameConfig cfg)
		{
			bool water = cfg.water > 0 && _water != null;
			if (_water != null) _water.SetActive(water);
			float line = water ? waterLevel + 0.8f : float.NegativeInfinity;
			if (!Mathf.Approximately(line, TreeLine) && !(float.IsNegativeInfinity(line) && float.IsNegativeInfinity(TreeLine)))
			{
				TreeLine = line;
				foreach (InstancedForest f in FindObjectsByType<InstancedForest>(FindObjectsSortMode.None)) f.Build();
			}
			_birdsOn = cfg.effects > 0;
			foreach (ParticleSystem b in _birds) b.gameObject.SetActive(_birdsOn);
			foreach (GameObject f in _falls) f.SetActive(cfg.effects > 0);
			if (vines != null) vines.SetActive(cfg.vegetation > 0);
			_dustRate = new[] { 0f, 0.5f, 1f }[Mathf.Clamp(cfg.effects, 0, 2)];
			foreach (ParticleSystem d in _dust.Values)
			{
				if (d == null) continue;
				var e = d.emission;
				e.rateOverTime = 260f * _dustRate;
			}
		}

		private void Update()
		{
			if (dustMaterial == null) return;
			foreach (Camera cam in Camera.allCameras)
			{
				if (_dust.ContainsKey(cam) || cam.GetComponent<FollowCamera>() == null && cam.name != "Showcase Camera") continue;
				_dust[cam] = MakeDust(cam);
			}
		}

		private GameObject MakeWater()
		{
			// Grid of 250 m tiles (keeps precision and lets tiles cull).
			var go = new GameObject("Valley Water");
			go.transform.SetParent(transform, false);
			const float tile = 250f;
			var verts = new List<Vector3>();
			var uvs = new List<Vector2>();
			var tris = new List<int>();
			for (float z = waterArea.yMin; z < waterArea.yMax; z += tile)
			{
				for (float x = waterArea.xMin; x < waterArea.xMax; x += tile)
				{
					int i = verts.Count;
					float x1 = Mathf.Min(x + tile, waterArea.xMax), z1 = Mathf.Min(z + tile, waterArea.yMax);
					verts.Add(new Vector3(x, waterLevel, z));
					verts.Add(new Vector3(x, waterLevel, z1));
					verts.Add(new Vector3(x1, waterLevel, z1));
					verts.Add(new Vector3(x1, waterLevel, z));
					foreach (Vector3 v in verts.GetRange(i, 4)) uvs.Add(new Vector2(v.x, v.z) / 40f);
					tris.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
				}
			}
			var mesh = new Mesh { name = "Valley Water" };
			mesh.SetVertices(verts);
			mesh.SetUVs(0, uvs);
			mesh.SetTriangles(tris, 0);
			mesh.RecalculateNormals();
			mesh.RecalculateTangents();
			mesh.RecalculateBounds();
			go.AddComponent<MeshFilter>().sharedMesh = mesh;
			var r = go.AddComponent<MeshRenderer>();
			r.sharedMaterial = waterMaterial;
			r.shadowCastingMode = ShadowCastingMode.Off;
			return go;
		}

		// A loose flock circling a point: billboards with a 4-frame wing-flap sheet, orbiting around Y.
		private ParticleSystem MakeFlock(Vector3 center, int seed)
		{
			var go = new GameObject("Bird Flock " + seed);
			go.transform.SetParent(transform, false);
			go.transform.position = center;
			var ps = go.AddComponent<ParticleSystem>();
			ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
			var main = ps.main;
			main.loop = true;
			main.duration = 5f;
			main.startLifetime = 100000f;
			main.startSpeed = 0f;
			main.startSize = new ParticleSystem.MinMaxCurve(1.6f, 2.4f);
			main.startColor = new Color(0.12f, 0.1f, 0.09f, 1f);
			main.simulationSpace = ParticleSystemSimulationSpace.Local;
			main.maxParticles = 18;
			main.prewarm = false;
			var emission = ps.emission;
			emission.rateOverTime = 0f;
			emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)12, (short)18) });
			var shape = ps.shape;
			shape.shapeType = ParticleSystemShapeType.Donut;
			shape.radius = 45f + 15f * (seed % 3);
			shape.donutRadius = 12f;
			shape.rotation = new Vector3(90f, 0f, 0f);
			var vel = ps.velocityOverLifetime;
			vel.enabled = true;
			vel.space = ParticleSystemSimulationSpace.Local;
			vel.orbitalX = new ParticleSystem.MinMaxCurve(0f, 0f);
			vel.orbitalY = new ParticleSystem.MinMaxCurve(0.22f + 0.04f * (seed % 4), 0.3f + 0.04f * (seed % 4));
			vel.orbitalZ = new ParticleSystem.MinMaxCurve(0f, 0f);
			vel.x = new ParticleSystem.MinMaxCurve(0f, 0f);
			vel.y = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);
			vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
			var noise = ps.noise;
			noise.enabled = true;
			noise.strength = 1.2f;
			noise.frequency = 0.15f;
			var sheet = ps.textureSheetAnimation;
			sheet.enabled = true;
			sheet.numTilesX = 4;
			sheet.numTilesY = 1;
			sheet.cycleCount = 0;
			sheet.fps = 7f + seed % 3;
			sheet.timeMode = ParticleSystemAnimationTimeMode.FPS;
			sheet.startFrame = new ParticleSystem.MinMaxCurve(0f, 3.99f);
			var r = go.GetComponent<ParticleSystemRenderer>();
			r.sharedMaterial = birdMaterial;
			r.shadowCastingMode = ShadowCastingMode.Off;
			r.receiveShadows = false;
			ps.Play();
			return ps;
		}

		private ParticleSystem MakeDust(Camera cam)
		{
			var go = new GameObject("Dust Motes");
			go.layer = cam.gameObject.layer;
			go.transform.SetParent(cam.transform, false);
			go.transform.localPosition = new Vector3(0f, 0f, 30f);
			var ps = go.AddComponent<ParticleSystem>();
			ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
			var main = ps.main;
			main.loop = true;
			main.duration = 2f;
			main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
			main.startSpeed = 0f;
			main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
			main.startColor = new Color(1f, 0.85f, 0.6f, 0.5f);
			main.simulationSpace = ParticleSystemSimulationSpace.World;
			main.maxParticles = 2500;
			var e = ps.emission;
			e.rateOverTime = 260f * _dustRate;
			var shape = ps.shape;
			shape.shapeType = ParticleSystemShapeType.Box;
			shape.scale = new Vector3(70f, 40f, 70f);
			var noise = ps.noise;
			noise.enabled = true;
			noise.strength = 0.6f;
			noise.frequency = 0.3f;
			var col = ps.colorOverLifetime;
			col.enabled = true;
			var g = new Gradient();
			g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
				new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
			col.color = g;
			var r = go.GetComponent<ParticleSystemRenderer>();
			r.sharedMaterial = dustMaterial;
			r.shadowCastingMode = ShadowCastingMode.Off;
			r.receiveShadows = false;
			ps.Play();
			return ps;
		}
	}
}
