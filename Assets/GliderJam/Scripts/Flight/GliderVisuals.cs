using UnityEngine;

namespace GliderJam.Flight
{
	// Control-surface animation, lights, player trail, wingtip and over-wing vapor, wing flex and the respawn
	// blink for a glider prefab.
	public class GliderVisuals : MonoBehaviour
	{
		public Material trailMaterial;
		public Material vaporMaterial;
		public Material puffMaterial;
		public Color playerColor = Color.white;

		private GliderController _glider;
		private Transform _ailL, _ailR, _elev, _rudder, _visual;
		private Quaternion _ailL0, _ailR0, _elev0, _rudder0;
		private Renderer _strobe;
		private Renderer[] _renderers;
		private TrailRenderer _trail, _vaporL, _vaporR;
		private ParticleSystem[] _wingVapor = new ParticleSystem[0];
		private float _strobeT;

		private void Start()
		{
			_glider = GetComponent<GliderController>();
			_visual = transform.Find("Visual");
			var flex = gameObject.AddComponent<WingFlex>();
			flex.Init(_visual);
			_ailL = Find("AileronL_Pivot");
			_ailR = Find("AileronR_Pivot");
			_elev = Find("Elevator_Pivot");
			_rudder = Find("Rudder_Pivot");
			_ailL0 = _ailL.localRotation;
			_ailR0 = _ailR.localRotation;
			_elev0 = _elev.localRotation;
			_rudder0 = _rudder.localRotation;
			_strobe = Find("Strobe").GetComponent<Renderer>();
			_renderers = _visual.GetComponentsInChildren<Renderer>();

			// Player-colored ribbon from the tail, readable from far away by the other player.
			var tail = new GameObject("PlayerTrail").transform;
			tail.SetParent(_visual, false);
			tail.localPosition = new Vector3(0f, 0.25f, -4.7f);
			_trail = MakeTrail(tail, trailMaterial, playerColor, 0.55f, 2.4f, 0.55f);
			_vaporL = MakeTrail(Find("TipTrailL"), vaporMaterial, Color.white, 0.16f, 0.4f, 0.3f);
			_vaporR = MakeTrail(Find("TipTrailR"), vaporMaterial, Color.white, 0.16f, 0.4f, 0.3f);
			flex.AddAnchor(_vaporL.transform);
			flex.AddAnchor(_vaporR.transform);

			// Condensation sheets over the wing upper surface at high load (inner and outer panel per side).
			if (puffMaterial != null && GameConfig.Current.effects > 0)
			{
				_wingVapor = new ParticleSystem[4];
				int k = 0;
				for (int side = -1; side <= 1; side += 2)
				{
					_wingVapor[k++] = MakeWingVapor(side, 0.9f, 3.7f, flex);
					_wingVapor[k++] = MakeWingVapor(side, 3.7f, 6.5f, flex);
				}
			}
		}

		private ParticleSystem MakeWingVapor(int side, float x0, float x1, WingFlex flex)
		{
			float xc = (x0 + x1) * 0.5f;
			var go = new GameObject("WingVapor");
			go.transform.SetParent(_visual, false);
			// Upper surface height and the front half of the chord at the panel center.
			go.transform.localPosition = new Vector3(side * xc, 0.40f + xc * 0.05f, 0.40f - 0.25f * Mathf.Lerp(0.9f, 0.5f, xc / 7f));
			var ps = go.AddComponent<ParticleSystem>();
			ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
			var main = ps.main;
			main.duration = 1f;
			main.loop = true;
			main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.2f);
			main.startSpeed = 0f;
			main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.65f);
			main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
			main.startColor = new Color(1f, 1f, 1f, 0.32f);
			main.simulationSpace = ParticleSystemSimulationSpace.Local;
			main.scalingMode = ParticleSystemScalingMode.Hierarchy;
			main.maxParticles = 160;
			var emission = ps.emission;
			emission.rateOverTime = 0f;
			var shape = ps.shape;
			shape.shapeType = ParticleSystemShapeType.Box;
			shape.scale = new Vector3(x1 - x0, 0.03f, 0.22f);
			var vel = ps.velocityOverLifetime;
			vel.enabled = true;
			vel.space = ParticleSystemSimulationSpace.Local;
			vel.x = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f); // all axes must share the curve mode
			vel.y = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
			vel.z = new ParticleSystem.MinMaxCurve(-7f, -4f);
			var col = ps.colorOverLifetime;
			col.enabled = true;
			var g = new Gradient();
			g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
				new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) });
			col.color = g;
			var size = ps.sizeOverLifetime;
			size.enabled = true;
			size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(1f, 1.4f)));
			var r = go.GetComponent<ParticleSystemRenderer>();
			r.sharedMaterial = puffMaterial;
			r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			r.receiveShadows = false;
			ps.Play();
			flex.AddAnchor(go.transform);
			return ps;
		}

		private Transform Find(string name)
		{
			foreach (Transform t in GetComponentsInChildren<Transform>(true))
			{
				if (t.name == name) return t;
			}
			return null;
		}

		private static TrailRenderer MakeTrail(Transform anchor, Material mat, Color color, float width, float time, float alpha)
		{
			var tr = anchor.gameObject.AddComponent<TrailRenderer>();
			tr.sharedMaterial = mat;
			tr.time = time;
			tr.minVertexDistance = 1.5f;
			// Thin at the glider, widening and fading as it dissipates.
			tr.widthCurve = new AnimationCurve(new Keyframe(0f, width * 0.3f), new Keyframe(0.25f, width), new Keyframe(1f, width * 1.4f));
			var g = new Gradient();
			g.SetKeys(new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
				new[] { new GradientAlphaKey(alpha, 0f), new GradientAlphaKey(alpha * 0.45f, 0.4f), new GradientAlphaKey(0f, 1f) });
			tr.colorGradient = g;
			tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			tr.receiveShadows = false;
			tr.textureMode = LineTextureMode.Stretch;
			tr.alignment = LineAlignment.View;
			return tr;
		}

		private void LateUpdate()
		{
			if (_glider == null) return;
			float roll = _glider.inputTurn, pitch = _glider.inputPitch;
			_ailL.localRotation = _ailL0 * Quaternion.AngleAxis(roll * 20f, Vector3.right);
			_ailR.localRotation = _ailR0 * Quaternion.AngleAxis(-roll * 20f, Vector3.right);
			_elev.localRotation = _elev0 * Quaternion.AngleAxis(-pitch * 18f, Vector3.right);
			_rudder.localRotation = _rudder0 * Quaternion.AngleAxis(roll * 14f, Vector3.right);

			bool visible = !_glider.Crashed && !(_glider.Ghost > 0f && Mathf.Repeat(Time.time * 8f, 1f) < 0.35f);
			foreach (Renderer r in _renderers) r.enabled = visible;
			_strobeT += Time.deltaTime;
			_strobe.enabled = visible && Mathf.Repeat(_strobeT, 1.1f) < 0.07f;

			_trail.emitting = !_glider.Crashed && !_glider.Frozen;
			// Vapor at the wingtips in hard turns or at high speed.
			float load = Mathf.Abs(_glider.Roll) / _glider.maxRoll * 0.7f + Mathf.Abs(_glider.inputPitch) * 0.5f + _glider.SpeedNormalized * 0.5f;
			bool vapor = !_glider.Crashed && !_glider.Frozen && load > 0.75f;
			_vaporL.emitting = vapor;
			_vaporR.emitting = vapor;
			// Over-wing condensation grows with load (high G) and speed.
			float sheet = _glider.Crashed || _glider.Frozen || !visible ? 0f : Mathf.Clamp01((_glider.Load - 0.55f) / 0.45f) * (0.45f + 0.55f * _glider.SpeedNormalized);
			foreach (ParticleSystem ps in _wingVapor)
			{
				var e = ps.emission;
				e.rateOverTime = sheet * 340f;
			}
		}

		public void ClearTrails()
		{
			_trail.Clear();
			_vaporL.Clear();
			_vaporR.Clear();
			foreach (ParticleSystem ps in _wingVapor) ps.Clear();
		}
	}
}
