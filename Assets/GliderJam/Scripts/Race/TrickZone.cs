using System;
using System.Collections.Generic;
using GliderJam.Flight;
using UnityEngine;

namespace GliderJam.Race
{
	// A stunt that pays out boost: a trigger box (a gate tunnel, the space under a skybridge) that counts when a glider
	// flies all the way through it in the race direction (+Z). RaceManager hands out the reward.
	[RequireComponent(typeof(BoxCollider))]
	public class TrickZone : MonoBehaviour
	{
		public enum Kind { Gate, UnderBridge }

		public Kind kind;

		public static event Action<GliderController, TrickZone> Passed;

		private BoxCollider _box;
		private readonly Dictionary<GliderController, float> _enteredAtZ = new Dictionary<GliderController, float>();

		private void Awake()
		{
			_box = GetComponent<BoxCollider>();
			_box.isTrigger = true;
		}

		private static GliderController Glider(Collider c)
		{
			return c.attachedRigidbody != null ? c.attachedRigidbody.GetComponent<GliderController>() : null;
		}

		private void OnTriggerEnter(Collider other)
		{
			GliderController g = Glider(other);
			if (g != null && !_enteredAtZ.ContainsKey(g)) _enteredAtZ[g] = g.transform.position.z;
		}

		private void OnTriggerExit(Collider other)
		{
			GliderController g = Glider(other);
			if (g == null || !_enteredAtZ.TryGetValue(g, out float z0)) return;
			Bounds b = _box.bounds;
			Vector3 p = g.transform.position;
			if (b.Contains(p)) return; // a wing tip left, the glider hasn't (one exit per collider)
			_enteredAtZ.Remove(g);
			if (!g.Crashed && z0 < b.center.z && p.z > b.max.z - 1f) Passed?.Invoke(g, this);
		}
	}
}
