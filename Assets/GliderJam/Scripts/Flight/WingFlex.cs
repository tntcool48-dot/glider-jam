using System.Collections.Generic;
using UnityEngine;

namespace GliderJam.Flight
{
	// Bends the wings up under load: a parabolic offset along the span applied to the wing, tip, aileron and
	// nav-light meshes (per-glider mesh instances), with attached anchors (tip vapor, over-wing vapor) moving along.
	public class WingFlex : MonoBehaviour
	{
		public float restDeflection = 0.14f; // tip rise in level flight (m)
		public float loadDeflection = 0.65f; // extra tip rise at full load (m)
		private const float Root = 0.3f, Semi = 7.35f;

		private class Part
		{
			public Mesh mesh;
			public Vector3[] rest, offset, work;
		}

		private readonly List<Part> _parts = new List<Part>();
		private readonly List<(Transform t, Vector3 rest, float profile)> _anchors = new List<(Transform, Vector3, float)>();
		private GliderController _glider;
		private float _deflection = -1f, _applied = -1f;

		public float Deflection => Mathf.Max(0f, _deflection);

		// 0 at the root, 1 at the tip.
		public static float Profile(float x)
		{
			float s = Mathf.Clamp01((Mathf.Abs(x) - Root) / (Semi - Root));
			return s * s;
		}

		// Call before the control surfaces move so the rest pose is captured.
		public void Init(Transform visual)
		{
			_glider = GetComponent<GliderController>();
			foreach (MeshFilter mf in visual.GetComponentsInChildren<MeshFilter>(true))
			{
				string n = mf.name;
				if (!(n.StartsWith("Wing") || n.StartsWith("Aileron") || n.StartsWith("NavLight"))) continue;
				Mesh m = mf.mesh;
				m.MarkDynamic();
				Matrix4x4 toVisual = visual.worldToLocalMatrix * mf.transform.localToWorldMatrix;
				Vector3 up = toVisual.inverse.MultiplyVector(Vector3.up);
				var p = new Part { mesh = m, rest = m.vertices };
				p.offset = new Vector3[p.rest.Length];
				p.work = new Vector3[p.rest.Length];
				for (int i = 0; i < p.rest.Length; i++) p.offset[i] = up * Profile(toVisual.MultiplyPoint3x4(p.rest[i]).x);
				_parts.Add(p);
				mf.GetComponent<MeshRenderer>().rayTracingMode = UnityEngine.Experimental.Rendering.RayTracingMode.DynamicGeometry;
			}
		}

		public void AddAnchor(Transform t)
		{
			if (t != null) _anchors.Add((t, t.localPosition, Profile(t.localPosition.x)));
		}

		private void LateUpdate()
		{
			if (_glider == null) return;
			float load = _glider.Crashed || _glider.Frozen ? 0f : Mathf.Clamp01(_glider.Load);
			float target = restDeflection + loadDeflection * load;
			_deflection = _deflection < 0f ? target : Mathf.Lerp(_deflection, target, 1f - Mathf.Exp(-5f * Time.deltaTime));
			// Light buffeting when pulling hard at speed.
			float buffet = load > 0.6f ? 0.018f * (load - 0.6f) * Mathf.Sin(Time.time * 31f) * (0.5f + _glider.SpeedNormalized) : 0f;
			float d = _deflection + buffet;
			if (Mathf.Abs(d - _applied) < 0.002f) return;
			_applied = d;
			foreach (Part p in _parts)
			{
				for (int i = 0; i < p.rest.Length; i++) p.work[i] = p.rest[i] + p.offset[i] * d;
				p.mesh.SetVertices(p.work);
			}
			foreach (var a in _anchors) a.t.localPosition = a.rest + Vector3.up * (a.profile * d);
		}

		private void OnDestroy()
		{
			foreach (Part p in _parts) Destroy(p.mesh);
		}
	}
}
