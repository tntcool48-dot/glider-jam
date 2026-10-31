using UnityEngine;

namespace GliderJam.Flight
{
	// Control-surface animation, lights, player trail, wingtip vapor and the respawn blink for a glider prefab.
	public class GliderVisuals : MonoBehaviour
	{
		public Material trailMaterial;
		public Material vaporMaterial;
		public Color playerColor = Color.white;

		private GliderController _glider;
		private Transform _ailL, _ailR, _elev, _rudder, _visual;
		private Quaternion _ailL0, _ailR0, _elev0, _rudder0;
		private Renderer _strobe;
		private Renderer[] _renderers;
		private TrailRenderer _trail, _vaporL, _vaporR;
		private float _strobeT;

		private void Start()
		{
			_glider = GetComponent<GliderController>();
			_visual = transform.Find("Visual");
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
			_vaporL = MakeTrail(Find("TipTrailL"), vaporMaterial, Color.white, 0.18f, 0.7f, 0.35f);
			_vaporR = MakeTrail(Find("TipTrailR"), vaporMaterial, Color.white, 0.18f, 0.7f, 0.35f);
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
			tr.widthCurve = new AnimationCurve(new Keyframe(0f, width * 0.3f), new Keyframe(0.25f, width), new Keyframe(1f, width * 2.2f));
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
			float roll = _glider.inputRoll, pitch = _glider.inputPitch;
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
			float load = Mathf.Abs(_glider.Bank) / _glider.maxBank * 0.7f + Mathf.Abs(_glider.inputPitch) * 0.5f + _glider.SpeedNormalized * 0.5f;
			bool vapor = !_glider.Crashed && !_glider.Frozen && load > 0.75f;
			_vaporL.emitting = vapor;
			_vaporR.emitting = vapor;
		}

		public void ClearTrails()
		{
			_trail.Clear();
			_vaporL.Clear();
			_vaporR.Clear();
		}
	}
}
