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

		[Header("Close calls")]
		public float closeCallRadius = 14f;     // skimming a wall, building or the ground this close...
		public float closeCallRecharge = 0.55f; // ...refills boost this much faster per second

		[Header("Out of bounds")]
		public float ceiling = 288f;
		public float ceilingGrace = 2f;
		// Course sides: outside them counts like the ceiling.
		public float minX = float.NegativeInfinity, maxX = float.PositiveInfinity;

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
		public bool CloseCall { get; private set; }
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

			// Constant speed; boost and brake just shift it. A burst (a stunt reward) boosts for free and kicks in faster.
			_burst = Mathf.Max(0f, _burst - dt);
			bool free = _burst > 0f;
			Boosting = free || (inputBoost && BoostEnergy > 0.02f);
			BoostEnergy = Mathf.Clamp01(BoostEnergy + (Boosting && !free ? -boostDrain : boostRecharge) * dt);
			float target = Boosting ? boostSpeed : inputBrake ? brakeSpeed : cruiseSpeed;
			Speed = Mathf.MoveTowards(Speed, target, speedChange * (free ? 2f : 1f) * dt);

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

			// At 110-145 m/s the glider moves 2-3 m per physics step and can slip under a terrain heightfield's surface
			// (it then only "hits" on the way out). Sweep the fuselage along this step, and treat being below the
			// terrain surface (e.g. still inside a pillar when respawn invulnerability ends) as a crash.
			Vector3 at = _rb.position;
			if (Ghost <= 0f)
			{
				Vector3 step = _rb.linearVelocity * dt;
				float dist = step.magnitude;
				if (dist > 0.01f && Physics.SphereCast(at, 0.45f, step / dist, out RaycastHit hit, dist + 0.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
					&& hit.collider.attachedRigidbody == null) // static world only (not gliders or crash debris)
				{
					Crash(hit.point);
					return;
				}
				if (BelowTerrain(at))
				{
					Crash(at);
					return;
				}
			}

			// Close call: static world within reach (not while ghosted after a respawn). Checked every other physics step.
			if (Ghost > 0f)
			{
				CloseCall = false;
			}
			else if ((++_nearStep & 1) == 0)
			{
				CloseCall = false;
				int n = Physics.OverlapSphereNonAlloc(at, closeCallRadius, Near, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
				for (int k = 0; k < n && !CloseCall; k++) CloseCall = Near[k].attachedRigidbody == null;
			}
			if (CloseCall) AddBoost(closeCallRecharge * dt);

			// Above the ceiling or off the course: short countdown, then the glider is destroyed.
			AboveCeiling = at.y > ceiling;
			if ((AboveCeiling || at.x < minX || at.x > maxX) && Ghost <= 0f)
			{
				OutOfBoundsTime += dt;
				if (OutOfBoundsTime >= ceilingGrace) Crash(_rb.position);
			}
			else
			{
				OutOfBoundsTime = 0f;
			}
		}

		private static readonly Collider[] Near = new Collider[8];
		private int _nearStep;

		// Extra boost energy (close calls, slipstream, stunts).
		public void AddBoost(float amount)
		{
			BoostEnergy = Mathf.Clamp01(BoostEnergy + amount);
		}

		// Free boost for a few seconds (stunt reward).
		public void Burst(float seconds)
		{
			_burst = Mathf.Max(_burst, seconds);
		}

		private float _burst;

		private static Terrain[] _terrains = new Terrain[0];
		private static int _terrainFrame = -1000;

		private static bool BelowTerrain(Vector3 p)
		{
			if (Time.frameCount - _terrainFrame > 300) // refresh the terrain list now and then (scene changes)
			{
				_terrains = Terrain.activeTerrains;
				_terrainFrame = Time.frameCount;
			}
			foreach (Terrain t in _terrains)
			{
				if (t == null) continue;
				Vector3 o = t.transform.position, s = t.terrainData.size;
				if (p.x < o.x || p.x > o.x + s.x || p.z < o.z || p.z > o.z + s.z) continue;
				return p.y < o.y + t.SampleHeight(p) - 0.5f;
			}
			return false;
		}

		private void OnCollisionEnter(Collision collision)
		{
			if (Crashed || Ghost > 0f || collision.rigidbody != null) return; // other gliders and crash debris don't count
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

		// Respawn points are checked to be clear (RaceManager), so the ghost time is only a short blink: a long one let
		// the glider fly straight through walls and pillars after a respawn.
		public void Respawn(Vector3 position, Quaternion rotation, float ghostSeconds = 0.4f)
		{
			_rb.isKinematic = false;
			_rb.position = position;
			_rb.rotation = rotation;
			transform.SetPositionAndRotation(position, rotation);
			SetOrientation(rotation);
			Roll = 0f;
			Speed = cruiseSpeed;
			OutOfBoundsTime = 0f;
			_burst = 0f;
			_rb.linearVelocity = rotation * Vector3.forward * Speed;
			foreach (Collider c in _colliders) c.enabled = true;
			Crashed = false;
			BoostEnergy = Mathf.Max(BoostEnergy, 0.5f);
			Ghost = ghostSeconds;
			SetGhost(ghostSeconds > 0f);
		}

		// While ghosted the glider can't crash (colliders become triggers).
		private void SetGhost(bool on)
		{
			foreach (Collider c in _colliders) c.isTrigger = on;
		}
	}
}
