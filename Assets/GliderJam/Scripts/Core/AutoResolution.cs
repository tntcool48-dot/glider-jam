using UnityEngine;

namespace GliderJam
{
	// "Auto resolution": holds 60 fps on slower PCs by lowering the internal render resolution (down to 50%,
	// upscaled to the screen) while the GPU can't keep up, and raising it back when it can. Driven by measured GPU
	// frame time; the Render Scale setting is the ceiling, so a fast PC simply stays at full resolution.
	public class AutoResolution : MonoBehaviour
	{
		public static float Percent { get; private set; } = 100f;

		// The render scale the cameras use right now.
		public static float Scale(GameConfig cfg) => cfg.autoResolution ? Mathf.Min(Percent, cfg.renderScale) : cfg.renderScale;

		private const float Budget = 1000f / 60f * 0.88f; // GPU ms per frame to aim for, with headroom under 16.7 ms
		private const float Min = 50f;
		private readonly FrameTiming[] _timing = new FrameTiming[1];
		private float _avg, _nextStep;

		private void Update()
		{
			GameConfig cfg = GameConfig.Current;
			if (!cfg.autoResolution)
			{
				Percent = cfg.renderScale;
				return;
			}
			// GPU time of a recent frame. Some drivers don't report it: then the whole frame time, which with vsync sits at
			// 16.7 ms on a 60 Hz screen, so that mode only reacts to frames actually missing 60 fps.
			FrameTimingManager.CaptureFrameTimings();
			bool gpu = FrameTimingManager.GetLatestTimings(1, _timing) > 0 && _timing[0].gpuFrameTime > 0.0;
			float ms = gpu ? (float)_timing[0].gpuFrameTime : Time.unscaledDeltaTime * 1000f;
			if (ms <= 0f || ms > 120f) return; // loading hitches say nothing about the steady load
			_avg = _avg <= 0f ? ms : Mathf.Lerp(_avg, ms, 0.08f);
			if (Time.unscaledTime < _nextStep) return;
			_nextStep = Time.unscaledTime + 0.4f;

			// GPU cost goes roughly with the pixel count, i.e. with the square of the scale.
			float budget = gpu ? Budget : 18.5f;
			float max = cfg.renderScale, p = Mathf.Min(Percent, max);
			if (_avg > budget * 1.05f)
			{
				p = Mathf.Max(Min, Mathf.Max(p * Mathf.Sqrt(budget / _avg), p - 15f));
			}
			else if (p < max && _avg * Sq((p + 5f) / p) < (gpu ? Budget * 0.92f : 14f))
			{
				p = Mathf.Min(max, p + 5f);
			}
			if (!Mathf.Approximately(p, Percent))
			{
				_avg *= Sq(p / Mathf.Max(1f, Percent)); // expected time at the new scale, until new frames come in
				Percent = p;
			}
		}

		private static float Sq(float x) => x * x;
	}
}
