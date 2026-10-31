using UnityEngine;

namespace GliderJam.Audio
{
	// Small procedural sound kit (no audio assets needed): UI clicks, countdown beeps, checkpoint chime,
	// an air-rush loop and the electric sustainer (FES) motor whine that reacts to boost.
	public static class AudioSynth
	{
		private const int Rate = 44100;
		private static AudioClip _click, _beep, _go, _chime, _air, _motor;

		public static AudioClip Click => _click ??= Tone("ui_click", 0.05f, t => Bell(t, 2400f, 60f) * 0.5f);
		public static AudioClip Beep => _beep ??= Tone("beep", 0.25f, t => (Mathf.Sin(2f * Mathf.PI * 660f * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * 1320f * t)) * Env(t, 0.005f, 0.22f) * 0.45f);
		public static AudioClip Go => _go ??= Tone("go", 0.7f, t => (Mathf.Sin(2f * Mathf.PI * 1046.5f * t) + 0.35f * Mathf.Sin(2f * Mathf.PI * 2093f * t)) * Env(t, 0.005f, 0.65f) * 0.5f);
		public static AudioClip Chime => _chime ??= Tone("checkpoint", 0.9f, t => (Bell(t, 1318.5f, 4.5f) + (t > 0.09f ? Bell(t - 0.09f, 1975.5f, 4f) : 0f)) * 0.35f);

		// Seamless loops
		public static AudioClip Air => _air ??= Loop("air_rush", 2f, (t, rng) => (float)(rng.NextDouble() * 2.0 - 1.0));
		public static AudioClip Motor => _motor ??= Loop("fes_motor", 1f, (t, rng) =>
			0.5f * Mathf.Sin(2f * Mathf.PI * 220f * t) + 0.25f * Mathf.Sin(2f * Mathf.PI * 440f * t) + 0.12f * Mathf.Sin(2f * Mathf.PI * 1320f * t) + 0.06f * (float)(rng.NextDouble() * 2.0 - 1.0));

		private static float Bell(float t, float f, float decay)
		{
			if (t < 0f) return 0f;
			float e = Mathf.Exp(-decay * t);
			return (Mathf.Sin(2f * Mathf.PI * f * t) + 0.4f * Mathf.Sin(2f * Mathf.PI * f * 2.76f * t) * Mathf.Exp(-decay * 2f * t)) * e;
		}

		private static float Env(float t, float attack, float length)
		{
			return Mathf.Clamp01(t / attack) * Mathf.Clamp01((length - t) / (length * 0.6f));
		}

		private static AudioClip Tone(string name, float seconds, System.Func<float, float> f)
		{
			int n = (int)(seconds * Rate);
			var data = new float[n];
			for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f(i / (float)Rate), -1f, 1f);
			var clip = AudioClip.Create(name, n, 1, Rate, false);
			clip.SetData(data, 0);
			return clip;
		}

		// Loops are low-passed and cross-faded at the seam so they repeat without clicks.
		private static AudioClip Loop(string name, float seconds, System.Func<float, System.Random, float> f)
		{
			int n = (int)(seconds * Rate);
			var rng = new System.Random(7);
			var raw = new float[n];
			for (int i = 0; i < n; i++) raw[i] = f(i / (float)Rate, rng);
			var data = new float[n];
			float lp = 0f, lp2 = 0f;
			for (int pass = 0; pass < 2; pass++)
			{
				for (int i = 0; i < n; i++)
				{
					lp += (raw[i] - lp) * 0.08f;
					lp2 += (lp - lp2) * 0.08f;
					if (pass == 1) data[i] = lp2;
				}
			}
			int fade = Rate / 10;
			for (int i = 0; i < fade; i++)
			{
				float k = i / (float)fade;
				data[i] = data[i] * k + data[n - fade + i] * (1f - k);
			}
			float peak = 0.0001f;
			foreach (float s in data) peak = Mathf.Max(peak, Mathf.Abs(s));
			for (int i = 0; i < n; i++) data[i] = data[i] / peak * 0.6f;
			var clip = AudioClip.Create(name, n - fade, 1, Rate, false);
			var trimmed = new float[n - fade];
			System.Array.Copy(data, 0, trimmed, 0, n - fade);
			clip.SetData(trimmed, 0);
			return clip;
		}

		public static void Play(AudioClip clip, float volume = 1f, float pitch = 1f)
		{
			var go = new GameObject("SFX " + clip.name);
			var a = go.AddComponent<AudioSource>();
			a.clip = clip;
			a.volume = volume;
			a.pitch = pitch;
			a.spatialBlend = 0f;
			a.Play();
			Object.Destroy(go, clip.length / Mathf.Max(0.1f, pitch) + 0.1f);
		}
	}
}
