using UnityEngine;

namespace GliderJam.Flight
{
	// Smooth chase camera: trails the glider, leans with the bank, widens FOV with speed, avoids clipping into
	// rock, and shakes on impacts. After a crash it holds position and watches the wreck.
	[RequireComponent(typeof(Camera))]
	public class FollowCamera : MonoBehaviour
	{
		public GliderController target;
		public float distance = 17f;
		public float height = 5.2f;
		public float lookAhead = 40f;
		public float followSharpness = 9f;
		public float rotationSharpness = 7f;
		public float baseFov = 60f;
		public float boostFov = 74f;
		public float bankLean = 0.35f;
		public LayerMask collisionMask = ~0;

		private Camera _cam;
		private float _shake;
		private Vector3 _crashPoint;
		private Vector3 _smoothedUp = Vector3.up;
		private Vector3 _dir;

		private void Awake()
		{
			_cam = GetComponent<Camera>();
		}

		public void Snap()
		{
			if (target == null) return;
			Transform t = target.transform;
			transform.position = t.position - t.forward * distance + Vector3.up * height;
			transform.rotation = Quaternion.LookRotation(t.position + t.forward * lookAhead - transform.position);
			_smoothedUp = Vector3.up;
			_dir = t.forward;
		}

		public void AddShake(float amount)
		{
			_shake = Mathf.Max(_shake, amount);
		}

		public void OnTargetCrashed(Vector3 point)
		{
			_crashPoint = point;
			AddShake(2.2f);
		}

		private void LateUpdate()
		{
			if (target == null) return;
			float dt = Time.deltaTime;
			Transform t = target.transform;
			if (target.Crashed)
			{
				// Hold back and watch the wreck.
				Vector3 look = _crashPoint - transform.position;
				if (look.sqrMagnitude > 1f)
				{
					transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), 1f - Mathf.Exp(-3f * dt));
				}
				ApplyShake(dt);
				return;
			}

			// Smooth the viewing direction (not the position) so the distance stays constant at any speed.
			// The glider's forward is partly flattened so steep pitch doesn't swing the camera wildly.
			Vector3 fwd = t.forward;
			Vector3 flat = Vector3.ProjectOnPlane(fwd, Vector3.up).normalized;
			Vector3 wantDir = Vector3.Slerp(flat, fwd, 0.55f).normalized;
			_dir = _dir == Vector3.zero ? wantDir : Vector3.Slerp(_dir, wantDir, 1f - Mathf.Exp(-followSharpness * 0.6f * dt)).normalized;
			float speedK = target.SpeedNormalized;
			float dist = distance * Mathf.Lerp(0.95f, 1.12f, speedK);
			Vector3 desired = t.position - _dir * dist + Vector3.up * height;

			// Keep line of sight: pull in if rock is between the glider and the camera.
			Vector3 toCam = desired - t.position;
			if (Physics.SphereCast(t.position, 1.5f, toCam.normalized, out RaycastHit hit, toCam.magnitude, collisionMask, QueryTriggerInteraction.Ignore)
				&& hit.collider.GetComponentInParent<GliderController>() == null)
			{
				desired = t.position + toCam.normalized * Mathf.Max(3f, hit.distance - 0.5f);
			}

			// Small residual smoothing on top of the glider's own motion (relative, so no speed-dependent lag).
			Vector3 rel = Vector3.Lerp(transform.position - t.position, desired - t.position, 1f - Mathf.Exp(-followSharpness * 2f * dt));
			transform.position = t.position + rel;
			_smoothedUp = Vector3.Slerp(_smoothedUp, Vector3.Slerp(Vector3.up, t.up, bankLean), 1f - Mathf.Exp(-4f * dt));
			Vector3 lookPoint = t.position + fwd * lookAhead;
			Quaternion want = Quaternion.LookRotation(lookPoint - transform.position, _smoothedUp);
			transform.rotation = Quaternion.Slerp(transform.rotation, want, 1f - Mathf.Exp(-rotationSharpness * dt));

			// Per-player field of view from the settings; boost widens it by ~12 degrees.
			float playerFov = GameConfig.Current.players[Mathf.Clamp(target.PlayerIndex, 0, 1)].fieldOfView;
			float fov = Mathf.Lerp(playerFov - 2f, playerFov + 12f, Mathf.SmoothStep(0f, 1f, speedK));
			_cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, fov, 1f - Mathf.Exp(-3f * dt));
			if (target.Boosting) _shake = Mathf.Max(_shake, 0.12f);
			ApplyShake(dt);
		}

		private void ApplyShake(float dt)
		{
			if (_shake <= 0.001f) return;
			if (!GameConfig.Current.cameraShake)
			{
				_shake = 0f;
				return;
			}
			float t = Time.time * 25f;
			Vector3 offset = new Vector3(Mathf.PerlinNoise(t, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, t) - 0.5f, 0f) * _shake;
			transform.position += transform.rotation * offset;
			transform.rotation *= Quaternion.Euler(offset.y * 2f, offset.x * 2f, offset.x * 3f);
			_shake = Mathf.MoveTowards(_shake, 0f, dt * 1.6f);
		}
	}
}
