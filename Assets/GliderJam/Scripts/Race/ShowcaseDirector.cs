using GliderJam.Flight;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace GliderJam.Race
{
	// Menu background: films the autopilot gliders with a cycle of cinematic shots (chase, side tracking,
	// fly-by, long-lens wide) and per-frame depth of field focused on the subject.
	[RequireComponent(typeof(Camera))]
	public class ShowcaseDirector : MonoBehaviour
	{
		public GliderController[] subjects;
		public RaceRoute route;
		public float shotLength = 6.5f;

		private Camera _cam;
		private DepthOfField _dof;
		private int _shot = -1;
		private float _shotEnd;
		private GliderController _subject;
		private Vector3 _anchor, _offset;
		private Vector3 _velocity;

		private void Start()
		{
			_cam = GetComponent<Camera>();
			var profile = ScriptableObject.CreateInstance<VolumeProfile>();
			_dof = profile.Add<DepthOfField>(true);
			_dof.focusMode.value = DepthOfFieldMode.Manual;
			_dof.quality.value = 2;
			var mb = profile.Add<MotionBlur>(true);
			mb.intensity.value = 0.1f;
			var vol = gameObject.AddComponent<Volume>();
			vol.isGlobal = true;
			vol.priority = 50f;
			vol.sharedProfile = profile;
			NextShot();
		}

		private void NextShot()
		{
			_shot = (_shot + 1) % 4;
			_shotEnd = Time.time + shotLength;
			_subject = subjects[_shot % subjects.Length];
			Transform t = _subject.transform;
			Vector3 fwd = Vector3.ProjectOnPlane(t.forward, Vector3.up).normalized;
			Vector3 right = Vector3.Cross(Vector3.up, fwd);
			switch (_shot)
			{
				case 0: // chase
					_offset = -fwd * 24f + Vector3.up * 6f;
					_cam.fieldOfView = 55f;
					break;
				case 1: // side tracking
					_offset = right * 32f + Vector3.up * 5f + fwd * 6f;
					_cam.fieldOfView = 45f;
					break;
				case 2: // fly-by: fixed point ahead on the route, off to the side
					int i = route.ClosestIndex(t.position, 0, route.path.Length);
					int ahead = Mathf.Min(route.path.Length - 1, i + 14);
					_anchor = route.path[ahead] + Vector3.Cross(Vector3.up, (route.path[Mathf.Min(route.path.Length - 1, ahead + 1)] - route.path[ahead]).normalized) * 28f - Vector3.up * 6f;
					_cam.fieldOfView = 50f;
					break;
				default: // long lens, high and behind
					_offset = -fwd * 160f + Vector3.up * 70f + right * 40f;
					_cam.fieldOfView = 22f;
					break;
			}
			if (_shot != 2)
			{
				transform.position = t.position + _offset;
			}
			else
			{
				transform.position = _anchor;
			}
			transform.rotation = Quaternion.LookRotation(t.position - transform.position);
			_velocity = Vector3.zero;
		}

		private void LateUpdate()
		{
			if (_subject == null) return;
			if (Time.time >= _shotEnd || _subject.Crashed) NextShot();
			Transform t = _subject.transform;
			if (_shot != 2)
			{
				Vector3 fwd = Vector3.ProjectOnPlane(t.forward, Vector3.up).normalized;
				Vector3 right = Vector3.Cross(Vector3.up, fwd);
				Vector3 want = t.position + (_shot == 0 ? -fwd * 24f + Vector3.up * 6f : _shot == 1 ? right * 32f + Vector3.up * 5f + fwd * 6f : -fwd * 160f + Vector3.up * 70f + right * 40f);
				transform.position = Vector3.SmoothDamp(transform.position, want, ref _velocity, 0.35f);
			}
			Quaternion look = Quaternion.LookRotation(t.position + t.forward * 6f - transform.position);
			transform.rotation = Quaternion.Slerp(transform.rotation, look, 1f - Mathf.Exp(-6f * Time.deltaTime));
			float d = Vector3.Distance(transform.position, t.position);
			_dof.nearFocusStart.value = 0f;
			_dof.nearFocusEnd.value = d * 0.45f;
			_dof.farFocusStart.value = d * 1.8f;
			_dof.farFocusEnd.value = d * 6f;
		}
	}
}
