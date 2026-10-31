using GliderJam.Race;
using UnityEngine;

namespace GliderJam.Flight
{
	// Follows the race line (used for automated tests, AI opponents in screenshots and the menu flyover).
	public class AutopilotDriver : MonoBehaviour
	{
		public RaceRoute route;
		public float lookAheadMeters = 90f;
		public float lateralOffset;
		public bool useBoost = true;
		private GliderController _glider;
		private int _index;

		private void Start()
		{
			_glider = GetComponent<GliderController>();
			_index = route.ClosestIndex(transform.position, 0, route.path.Length);
		}

		private void Update()
		{
			if (_glider.Crashed)
			{
				return;
			}
			_index = route.ClosestIndex(transform.position, _index, 30);
			int ahead = Mathf.Min(route.path.Length - 1, _index + Mathf.Max(2, (int)(lookAheadMeters / 20f)));
			Vector3 p = route.path[ahead];
			Vector3 side = Vector3.Cross(Vector3.up, (route.path[Mathf.Min(route.path.Length - 1, ahead + 1)] - route.path[Mathf.Max(0, ahead - 1)]).normalized);
			p += side * lateralOffset;
			Vector3 local = transform.InverseTransformPoint(p);
			float yawErr = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
			Vector3 to = p - transform.position;
			float pitchWant = Mathf.Atan2(to.y, new Vector2(to.x, to.z).magnitude) * Mathf.Rad2Deg;
			_glider.inputRoll = Mathf.Clamp(yawErr / 18f, -1f, 1f);
			_glider.inputPitch = Mathf.Clamp((pitchWant - _glider.PitchAngle) / 10f, -1f, 1f);
			_glider.inputBoost = useBoost && Mathf.Abs(yawErr) < 6f && _glider.BoostEnergy > 0.4f;
			_glider.inputBrake = Mathf.Abs(yawErr) > 40f;
		}
	}
}
