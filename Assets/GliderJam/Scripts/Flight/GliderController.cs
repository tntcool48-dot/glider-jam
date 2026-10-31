using System;
using UnityEngine;

namespace GliderJam.Flight
{
	// Flight model in the style of the original game: constant cruise speed (boost / brake only nudge it),
	// W/S pitch the nose, A/D turn the nose and roll the glider into the turn. Holding a turn keeps rolling
	// further, up to a hard 90 degree cap, and the turn tightens as it banks; releasing levels it out.
	// Flying above the ceiling starts a short countdown; staying up there ends in an explosion.
	[RequireComponent(typeof(Rigidbody))]
	public class GliderController : MonoBehaviour
	{
		[Header("Speed (m/s)")]
		public float cruiseSpeed = 110f;
		public float boostSpeed = 145f;
		public float brakeSpeed = 80f;
		public float speedChange = 30f;

		[Header("Handling")]
		public float pitchRate = 55f;
		public float maxPitch = 80f;
		public float turnRate = 32f;       // nose turn at full input, wings level
		public float bankTurnRate = 44f;   // extra turn at a 90 degree roll
		public float maxRoll = 90f;
		public float rollRate = 80f;       // how fast the roll builds while turning
		public float rollReturnRate = 120f;

		[Header("Boost")]
		public float boostDrain = 0.28f;
		public float boostRecharge = 0.11f;

		[Header("Out of bounds")]
		public float ceiling = 288f;
		public float ceilingGrace = 2f;
		// Course limits (x sides, z behind the start): outside them counts like the ceiling.
		public float minX = float.NegativeInfinity, maxX = float.PositiveInfinity, minZ = float.NegativeInfinity;

		// Inputs (set by the driver)
		[NonSerialized] public float inputPitch, inputTurn;
		[NonSerialized] public bool inputBoost, inputBrake;

		public int PlayerIndex { get; set; }
		public float Speed { get; private set; }
		public float Roll { get; private set; }      // degrees, + = right wing down
		public float PitchAngle { get; private set; }
		public float Yaw { get; private set; }
		public float BoostEnergy { get; private set; } = 1f;
		public bool Boosting { get; private set; }
		public bool Crashed { get; private set; }
		public bool Frozen { get; set; }
		public float Ghost { get; private set; }
		public float OutOfBoundsTime { get; private set; }
		public float OutOfBoundsLeft => Mathf.Max(0f, ceilingGrace - OutOfBoundsTime);
		public bool OutOfBounds => OutOfBoundsTime > 0f;
		public bool AboveCeiling { get; private set; }
		public Vector3 Velocity => _rb != null ? _rb.linearVelocity : Vector3.zero;
		public float SpeedNormalized => Mathf.InverseLerp(brakeSpeed, boostSpeed, Speed);
		public float Load => Mathf.Abs(Roll) / maxRoll * 0.6f + Mathf.Abs(inputPitch) * 0.5f;

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
			Roll = -Mathf.DeltaAngle(0f, e.z);
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
				if (Frozen && !Crashed) _rb.linearVelocity = Vector3.zero;
				return;
			}

			// Constant speed; boost and brake just shift it.
			Boosting = inputBoost && BoostEnergy > 0.02f;
			BoostEnergy = Mathf.Clamp01(BoostEnergy + (Boosting ? -boostDrain : boostRecharge) * dt);
			float target = Boosting ? boostSpeed : inputBrake ? brakeSpeed : cruiseSpeed;
			Speed = Mathf.MoveTowards(Speed, target, speedChange * dt);

			// Turning rolls the glider (building up to the 90 degree cap); the nose turns faster as it banks.
			float rollTarget = inputTurn * maxRoll;
			bool building = Mathf.Abs(rollTarget) > Mathf.Abs(Roll) && Mathf.Sign(rollTarget) == Mathf.Sign(Roll == 0f ? rollTarget : Roll);
			Roll = Mathf.MoveTowards(Roll, rollTarget, (building ? rollRate : rollReturnRate) * dt);
			Roll = Mathf.Clamp(Roll, -maxRoll, maxRoll);
			Yaw += (turnRate * inputTurn + bankTurnRate * Mathf.Sin(Roll * Mathf.Deg2Rad)) * dt;
			PitchAngle = Mathf.Clamp(PitchAngle + inputPitch * pitchRate * dt, -maxPitch, maxPitch);

			Quaternion rot = Quaternion.Euler(-PitchAngle, Yaw, -Roll);
			_rb.MoveRotation(rot);
			_rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, rot * Vector3.forward * Speed, 1f - Mathf.Exp(-20f * dt));

			// Above the ceiling or off the course: short countdown, then the glider is destroyed.
			Vector3 at = _rb.position;
			AboveCeiling = at.y > ceiling;
			if ((AboveCeiling || at.x < minX || at.x > maxX || at.z < minZ) && Ghost <= 0f)
			{
				OutOfBoundsTime += dt;
				if (OutOfBoundsTime >= ceilingGrace) Crash(_rb.position);
			}
			else
			{
				OutOfBoundsTime = 0f;
			}
		}

		private void OnCollisionEnter(Collision collision)
		{
			if (Crashed || Ghost > 0f || collision.collider.GetComponentInParent<GliderController>() != null) return;
			Crash(collision.GetContact(0).point);
		}

		public void Crash(Vector3 point)
		{
			if (Crashed) return;
			Crashed = true;
			OutOfBoundsTime = 0f;
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
			Roll = 0f;
			Speed = cruiseSpeed;
			OutOfBoundsTime = 0f;
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
