using System;
using UnityEngine;

namespace GliderJam.Flight
{
	// Arcade sailplane flight: bank-to-turn, pitch, speed that rises in dives and falls in climbs,
	// boost (limited energy) and air brake, a soft flight ceiling, crash on terrain / obstacles.
	// Inputs are set each frame by a driver (player input or autopilot).
	[RequireComponent(typeof(Rigidbody))]
	public class GliderController : MonoBehaviour
	{
		[Header("Speeds (m/s)")]
		public float cruiseSpeed = 100f;
		public float boostSpeed = 150f;
		public float brakeSpeed = 62f;
		public float acceleration = 22f;
		public float deceleration = 30f;

		[Header("Handling")]
		public float pitchRate = 62f;
		public float maxPitch = 70f;
		public float maxBank = 70f;
		public float bankResponse = 4.5f;
		public float turnRate = 62f;
		public float rudderRate = 12f;

		[Header("Boost")]
		public float boostDrain = 0.28f;
		public float boostRecharge = 0.11f;

		[Header("Limits")]
		public float ceiling = 288f;

		// Inputs (set by the driver)
		[NonSerialized] public float inputPitch, inputRoll;
		[NonSerialized] public bool inputBoost, inputBrake;

		public int PlayerIndex { get; set; }
		public float Speed { get; private set; }
		public float Bank { get; private set; }
		public float PitchAngle { get; private set; }
		public float Yaw { get; private set; }
		public float BoostEnergy { get; private set; } = 1f;
		public bool Boosting { get; private set; }
		public bool Crashed { get; private set; }
		public bool Frozen { get; set; }
		public float Ghost { get; private set; }
		public float SpeedNormalized => Mathf.InverseLerp(brakeSpeed, boostSpeed, Speed);

		public event Action<GliderController, Vector3, Vector3> OnCrash;

		private Rigidbody _rb;
		private Collider[] _colliders;

		private void Awake()
		{
			_rb = GetComponent<Rigidbody>();
			_rb.useGravity = false;
			_rb.mass = 450f;
			_rb.linearDamping = 0f;
			_rb.angularDamping = 0f;
			_rb.interpolation = RigidbodyInterpolation.Interpolate;
			_rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
			_rb.constraints = RigidbodyConstraints.FreezeRotation;
			_colliders = GetComponentsInChildren<Collider>();
			Speed = cruiseSpeed;
			SetOrientation(transform.rotation);
		}

		public void SetOrientation(Quaternion rot)
		{
			Vector3 e = rot.eulerAngles;
			Yaw = e.y;
			PitchAngle = -Mathf.DeltaAngle(0f, e.x);
			Bank = -Mathf.DeltaAngle(0f, e.z);
		}

		private void FixedUpdate()
		{
			float dt = Time.fixedDeltaTime;
			if (Ghost > 0f)
			{
				Ghost -= dt;
				if (Ghost <= 0f) SetGhost(false);
			}
			if (Crashed || Frozen)
			{
				if (Frozen && !Crashed)
				{
					_rb.linearVelocity = Vector3.zero;
				}
				return;
			}

			// Speed: boost / brake / cruise, plus gravity trade (dive faster, climb slower).
			Boosting = inputBoost && BoostEnergy > 0.02f;
			BoostEnergy = Mathf.Clamp01(BoostEnergy + (Boosting ? -boostDrain : boostRecharge) * dt);
			float target = Boosting ? boostSpeed : inputBrake ? brakeSpeed : cruiseSpeed;
			target += -PitchAngle * 0.45f;
			Speed = Mathf.MoveTowards(Speed, target, (target > Speed ? acceleration : deceleration) * dt);

			// Attitude: roll input sets the bank target, bank turns the glider; pitch input changes pitch angle.
			float bankTarget = inputRoll * maxBank;
			Bank = Mathf.Lerp(Bank, bankTarget, 1f - Mathf.Exp(-bankResponse * dt));
			Yaw += (turnRate * (Bank / maxBank) + rudderRate * inputRoll) * dt;
			float pitchTarget = PitchAngle + inputPitch * pitchRate * dt;
			if (Mathf.Abs(inputPitch) < 0.05f)
			{
				pitchTarget = Mathf.MoveTowards(pitchTarget, 0f, 20f * dt); // hands-off: return to level flight
			}
			PitchAngle = Mathf.Clamp(pitchTarget, -maxPitch, maxPitch);

			// Soft ceiling: nose is pushed down near the cloud base, altitude is clamped.
			float y = _rb.position.y;
			float over = Mathf.InverseLerp(ceiling - 25f, ceiling, y);
			if (over > 0f)
			{
				PitchAngle = Mathf.Min(PitchAngle, Mathf.Lerp(maxPitch, -8f, over));
			}

			Quaternion rot = Quaternion.Euler(-PitchAngle, Yaw, -Bank);
			_rb.MoveRotation(rot);
			Vector3 vel = rot * Vector3.forward * Speed;
			if (y > ceiling && vel.y > 0f)
			{
				vel.y = 0f;
			}
			_rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, vel, 1f - Mathf.Exp(-9f * dt));
		}

		private void OnCollisionEnter(Collision collision)
		{
			if (Crashed || Ghost > 0f || collision.collider.GetComponentInParent<GliderController>() != null)
			{
				return;
			}
			Crash(collision.GetContact(0).point);
		}

		public void Crash(Vector3 point)
		{
			if (Crashed)
			{
				return;
			}
			Crashed = true;
			Vector3 v = _rb.linearVelocity;
			_rb.isKinematic = true;
			foreach (Collider c in _colliders) c.enabled = false;
			OnCrash?.Invoke(this, point, v);
		}

		public void Respawn(Vector3 position, Quaternion rotation, float ghostSeconds = 2.5f)
		{
			_rb.isKinematic = false;
			_rb.position = position;
			_rb.rotation = rotation;
			transform.SetPositionAndRotation(position, rotation);
			SetOrientation(rotation);
			Bank = 0f;
			Speed = cruiseSpeed;
			_rb.linearVelocity = rotation * Vector3.forward * Speed;
			foreach (Collider c in _colliders) c.enabled = true;
			Crashed = false;
			BoostEnergy = Mathf.Max(BoostEnergy, 0.5f);
			Ghost = ghostSeconds;
			SetGhost(true);
		}

		// While ghosted the glider can't crash (colliders become triggers).
		private void SetGhost(bool on)
		{
			foreach (Collider c in _colliders) c.isTrigger = on;
		}
	}
}
