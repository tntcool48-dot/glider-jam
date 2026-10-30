using UnityEngine;

namespace yajaber
{
	public class PhysicOnGlider : MonoBehaviour
	{
		[Header("Level Settings")]
		public float constantSpeed = 120f;

		[Tooltip("How fast the plane changes direction. 20 = Instant/Arcade.")]
		public float agility = 20f;

		[HideInInspector]
		public Rigidbody _rb;

		private void Start()
		{
			_rb = GetComponent<Rigidbody>();
			_rb.useGravity = false;
			_rb.linearDamping = 0f;
			_rb.angularDamping = 0f;
			_rb.interpolation = RigidbodyInterpolation.Interpolate;
			_rb.isKinematic = false;
		}

		public float TrueAirSpeed()
		{
			return constantSpeed;
		}

		public float AirSpeed()
		{
			return constantSpeed;
		}

		public float CalculateLift()
		{
			return 0f;
		}

		public float CalculateDrag()
		{
			return 0f;
		}

		private void FixedUpdate()
		{
			// Crashed gliders are kinematic; setting velocity on them only spams warnings.
			if (_rb.isKinematic)
			{
				return;
			}
			Vector3 b = base.transform.forward * constantSpeed;
			_rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, b, agility * Time.fixedDeltaTime);
		}
	}
}
