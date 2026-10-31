using GliderJam.Audio;
using UnityEngine;

namespace GliderJam.Flight
{
	// Per-glider sound: recorded wind loop + synthesized air rush (both follow airspeed), and the electric
	// sustainer motor whine that spins up while boosting. 2D (split-screen has one listener); volume is shared
	// between players.
	public class GliderAudio : MonoBehaviour
	{
		public AudioClip windClip;
		public float masterVolume = 1f;

		private GliderController _glider;
		private AudioSource _wind, _air, _motor;
		private float _motorLevel;

		private void Start()
		{
			_glider = GetComponent<GliderController>();
			_wind = Source(windClip);
			_air = Source(AudioSynth.Air);
			_motor = Source(AudioSynth.Motor);
		}

		private AudioSource Source(AudioClip clip)
		{
			var a = gameObject.AddComponent<AudioSource>();
			a.clip = clip;
			a.loop = true;
			a.spatialBlend = 0f;
			a.volume = 0f;
			a.playOnAwake = false;
			if (clip != null) a.Play();
			return a;
		}

		private void Update()
		{
			bool alive = !_glider.Crashed && !_glider.Frozen;
			float s = _glider.SpeedNormalized;
			float dt = Time.deltaTime;
			_wind.volume = alive ? Mathf.Lerp(0.12f, 0.45f, s) * masterVolume : Mathf.MoveTowards(_wind.volume, 0f, dt);
			_wind.pitch = Mathf.Lerp(0.85f, 1.25f, s);
			float turn = Mathf.Abs(_glider.Bank) / _glider.maxBank;
			_air.volume = alive ? Mathf.Lerp(0.04f, 0.3f, s * 0.7f + turn * 0.3f) * masterVolume : 0f;
			_air.pitch = Mathf.Lerp(0.7f, 1.35f, s);
			_motorLevel = Mathf.MoveTowards(_motorLevel, alive && _glider.Boosting ? 1f : 0f, dt * (_glider.Boosting ? 2.5f : 1.2f));
			_motor.volume = _motorLevel * 0.22f * masterVolume;
			_motor.pitch = Mathf.Lerp(0.55f, 1.35f, _motorLevel) * Mathf.Lerp(0.95f, 1.1f, s);
		}
	}
}
