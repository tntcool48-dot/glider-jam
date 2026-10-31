using System.Collections.Generic;
using GliderJam.Flight;
using UnityEngine;

namespace GliderJam.World
{
	// Night city runtime: rain around every player camera (on that player's view layer, so split-screen views
	// don't see each other's rain), scrolling billboards, flickering neon and the city's quality hooks
	// (distant skyline, rain density).
	public class CityWeather : MonoBehaviour
	{
		public Material rainMaterial;
		public GameObject backdrop;
		public Renderer[] billboards = new Renderer[0];
		public Renderer[] flicker = new Renderer[0];

		private readonly Dictionary<Camera, ParticleSystem> _rain = new Dictionary<Camera, ParticleSystem>();
		private Material[] _billboardMats = new Material[0];
		private float _density = 1f;

		public static void ApplyQuality(GameConfig cfg)
		{
			foreach (CityWeather w in FindObjectsByType<CityWeather>(FindObjectsSortMode.None)) w.Apply(cfg);
		}

		private void Apply(GameConfig cfg)
		{
			if (backdrop != null) backdrop.SetActive(cfg.backdrop > 0);
			_density = new[] { 0.35f, 0.7f, 1f }[Mathf.Clamp(cfg.effects, 0, 2)];
			foreach (ParticleSystem ps in _rain.Values) if (ps != null) SetRate(ps);
		}

		private void Start()
		{
			Apply(GameConfig.Current);
			_billboardMats = new Material[billboards.Length];
			for (int i = 0; i < billboards.Length; i++) _billboardMats[i] = billboards[i].material;
		}

		private void OnDestroy()
		{
			foreach (Material m in _billboardMats) Destroy(m);
		}

		private void Update()
		{
			foreach (Camera cam in Camera.allCameras)
			{
				if (_rain.ContainsKey(cam) || cam.GetComponent<FollowCamera>() == null) continue;
				_rain[cam] = MakeRain(cam);
			}

			float t = Time.time;
			for (int i = 0; i < _billboardMats.Length; i++)
			{
				// Glyph columns scroll; every board glitches sideways for a moment now and then.
				float glitch = Mathf.Repeat(t * 0.37f + i * 0.31f, 1f) < 0.02f ? 0.13f : 0f;
				float scroll = _billboardMats[i].name.Contains("Glyph") ? Mathf.Repeat(-t * (0.04f + 0.02f * (i % 3)), 1f) : 0f;
				var st = new Vector4(1f, 1f, glitch, scroll);
				_billboardMats[i].SetVector("_BaseColorMap_ST", st);
				_billboardMats[i].SetVector("_EmissiveColorMap_ST", st);
			}
			for (int i = 0; i < flicker.Length; i++)
			{
				// Mostly on, with bursts of fast stutter like a failing transformer.
				float n = Mathf.PerlinNoise(t * 0.9f, i * 7.3f);
				flicker[i].enabled = n < 0.62f || Mathf.PerlinNoise(t * 23f, i * 3.1f) > 0.45f;
			}
		}

		private ParticleSystem MakeRain(Camera cam)
		{
			var go = new GameObject("Rain");
			go.layer = cam.gameObject.layer;
			go.transform.SetParent(cam.transform, false);
			go.transform.localPosition = new Vector3(0f, 12f, 50f);
			var ps = go.AddComponent<ParticleSystem>();
			ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
			var main = ps.main;
			main.loop = true;
			main.duration = 1f;
			main.startLifetime = 1.3f;
			main.startSpeed = 0f;
			main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.1f);
			main.startColor = new Color(0.8f, 0.85f, 1f, 0.5f);
			main.simulationSpace = ParticleSystemSimulationSpace.World;
			main.maxParticles = 12000;
			var shape = ps.shape;
			shape.shapeType = ParticleSystemShapeType.Box;
			shape.scale = new Vector3(120f, 60f, 120f);
			var vel = ps.velocityOverLifetime;
			vel.enabled = true;
			vel.space = ParticleSystemSimulationSpace.World;
			vel.x = new ParticleSystem.MinMaxCurve(3f, 5f);
			vel.y = new ParticleSystem.MinMaxCurve(-36f, -28f);
			vel.z = new ParticleSystem.MinMaxCurve(1f, 2.5f);
			var r = go.GetComponent<ParticleSystemRenderer>();
			r.renderMode = ParticleSystemRenderMode.Stretch;
			r.velocityScale = 0.03f;
			r.lengthScale = 2f;
			r.cameraVelocityScale = 0.01f;
			r.sharedMaterial = rainMaterial;
			r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			r.receiveShadows = false;
			SetRate(ps);
			ps.Play();
			return ps;
		}

		private void SetRate(ParticleSystem ps)
		{
			var e = ps.emission;
			e.rateOverTime = 7000f * _density;
		}
	}
}
