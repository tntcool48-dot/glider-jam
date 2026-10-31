using GliderJam.Flight;
using UnityEngine;

namespace GliderJam.FX
{
	// Faint air streaks rushing past the camera at high speed / boost. Lives on a per-player layer so the
	// other split-screen camera never sees it.
	public class SpeedStreaks : MonoBehaviour
	{
		public GliderController glider;
		public Material material;
		private ParticleSystem _ps;

		private void Start()
		{
			var go = new GameObject("Speed Streaks");
			go.layer = gameObject.layer;
			go.transform.SetParent(transform, false);
			go.transform.localPosition = new Vector3(0f, 0f, 28f);
			_ps = go.AddComponent<ParticleSystem>();
			_ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
			var main = _ps.main;
			main.simulationSpace = ParticleSystemSimulationSpace.Local;
			main.startLifetime = 0.45f;
			main.startSpeed = 0f;
			main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.4f);
			main.maxParticles = 400;
			main.startColor = new Color(1f, 1f, 1f, 0.5f);
			var shape = _ps.shape;
			shape.shapeType = ParticleSystemShapeType.Box;
			shape.scale = new Vector3(44f, 26f, 30f);
			var vel = _ps.velocityOverLifetime;
			vel.enabled = true;
			vel.space = ParticleSystemSimulationSpace.Local;
			vel.z = new ParticleSystem.MinMaxCurve(-160f, -110f);
			vel.x = new ParticleSystem.MinMaxCurve(0f, 0f);
			vel.y = new ParticleSystem.MinMaxCurve(0f, 0f);
			var col = _ps.colorOverLifetime;
			col.enabled = true;
			var g = new Gradient();
			g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
				new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.35f, 0.3f), new GradientAlphaKey(0f, 1f) });
			col.color = g;
			var r = go.GetComponent<ParticleSystemRenderer>();
			r.sharedMaterial = material;
			r.renderMode = ParticleSystemRenderMode.Stretch;
			r.velocityScale = 0.08f;
			r.lengthScale = 2f;
			r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			_ps.Play();
		}

		private void Update()
		{
			if (glider == null || _ps == null) return;
			var em = _ps.emission;
			float k = glider.Crashed || glider.Frozen ? 0f : Mathf.Clamp01((glider.SpeedNormalized - 0.55f) * 2.5f) + (glider.Boosting ? 0.6f : 0f);
			em.rateOverTime = 260f * Mathf.Clamp01(k);
		}
	}
}
