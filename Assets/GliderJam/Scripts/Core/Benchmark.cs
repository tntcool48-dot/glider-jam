using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GliderJam
{
	// Player benchmark:  -benchmark [-benchmarkLog <file>] [-benchmarkSeconds 75] [-benchmarkMap canyon|city]
	//                    [-benchmarkSolo] [-preset Low|Medium|High|Ultra|UltraRayTracing] [-set field=value ...]
	// Starts a race with every glider on autopilot (vsync off), records CPU / GPU frame times after a warm-up,
	// writes a summary and quits. -preset / -set also work without -benchmark (they are not saved).
	public class Benchmark : MonoBehaviour
	{
		private static bool? _active;
		public static bool Active => _active ??= Array.IndexOf(Environment.GetCommandLineArgs(), "-benchmark") >= 0;

		private string _logPath = "benchmark.txt";
		private float _seconds = 75f;
		private readonly float _warmup = 6f;
		private readonly List<float> _frames = new List<float>(20000);
		private readonly List<float> _gpu = new List<float>(20000);
		private readonly List<float> _cpu = new List<float>(20000);
		private readonly List<string> _notes = new List<string>();
		private float _raceStartedAt = -1f;
		private readonly FrameTiming[] _timing = new FrameTiming[1];
		private static string _raceScene = GameSettings.RaceScene;

		// -preset X  and  -set field=value  (any public GameConfig field), applied before the first scene.
		public static void ApplyCommandLineOverrides(GameConfig cfg)
		{
			string[] args = Environment.GetCommandLineArgs();
			for (int i = 0; i < args.Length - 1; i++)
			{
				if (args[i] == "-preset" && Enum.TryParse(args[i + 1], true, out QualityPreset p))
				{
					cfg.ApplyPreset(p);
				}
				if (args[i] == "-set")
				{
					string[] kv = args[i + 1].Split('=');
					FieldInfo f = typeof(GameConfig).GetField(kv[0]);
					if (f == null || kv.Length != 2) continue;
					object v = f.FieldType == typeof(bool) ? (object)(kv[1] == "1" || kv[1].ToLower() == "true")
						: f.FieldType == typeof(float) ? float.Parse(kv[1], System.Globalization.CultureInfo.InvariantCulture)
						: f.FieldType.IsEnum ? Enum.Parse(f.FieldType, kv[1], true)
						: (object)int.Parse(kv[1]);
					f.SetValue(cfg, v);
					cfg.preset = f.Name == "preset" ? cfg.preset : QualityPreset.Custom;
				}
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Init()
		{
			if (!Active) return;
			string[] args = Environment.GetCommandLineArgs();
			var go = new GameObject("Benchmark");
			DontDestroyOnLoad(go);
			var b = go.AddComponent<Benchmark>();
			GameSettings.Mode = GameMode.SplitScreen;
			for (int i = 0; i < args.Length; i++)
			{
				if (i + 1 < args.Length && args[i] == "-benchmarkLog") b._logPath = args[i + 1];
				if (i + 1 < args.Length && args[i] == "-benchmarkSeconds") float.TryParse(args[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out b._seconds);
				if (i + 1 < args.Length && args[i] == "-benchmarkMap" && args[i + 1].ToLower() == "city") _raceScene = GameSettings.CityScene;
				if (args[i] == "-benchmarkSolo") GameSettings.Mode = GameMode.Solo;
			}
			GameSettings.Map = _raceScene;
			Application.runInBackground = true; // a focus change must not pause the measurement
			GameSettings.Showcase = false;
			SceneManager.LoadScene(_raceScene);
		}

		private void OnApplicationFocus(bool focus)
		{
			_notes.Add("focus " + (focus ? "gained" : "lost") + " at frame " + Time.frameCount);
		}

		private void Update()
		{
			string scene = SceneManager.GetActiveScene().name;
			if (scene == _raceScene && _raceStartedAt < 0f) _raceStartedAt = Time.unscaledTime;
			if (_raceStartedAt < 0f) return;
			float t = Time.unscaledTime - _raceStartedAt;
			FrameTimingManager.CaptureFrameTimings();
			if (t > _warmup && scene == _raceScene)
			{
				_frames.Add(Time.unscaledDeltaTime);
				if (FrameTimingManager.GetLatestTimings(1, _timing) > 0)
				{
					_gpu.Add((float)_timing[0].gpuFrameTime);
					_cpu.Add((float)_timing[0].cpuMainThreadFrameTime);
				}
				if (Time.unscaledDeltaTime > 0.05f) _notes.Add("hitch " + (Time.unscaledDeltaTime * 1000f).ToString("F0") + " ms at race t=" + t.ToString("F1") + " s");
			}
			if (t > _warmup + _seconds || scene == GameSettings.EndScene)
			{
				Write();
				Application.Quit();
				enabled = false;
			}
		}

		private static float Avg(List<float> l)
		{
			if (l.Count == 0) return 0f;
			float s = 0f;
			foreach (float f in l) s += f;
			return s / l.Count;
		}

		private void Write()
		{
			GameConfig cfg = GameConfig.Current;
			var sorted = new List<float>(_frames);
			sorted.Sort();
			int n = sorted.Count;
			var sb = new StringBuilder();
			sb.AppendLine("Glider Jam benchmark  map=" + _raceScene + "  mode=" + GameSettings.Mode + "  preset=" + cfg.preset + (cfg.UsesRayTracing ? " (ray tracing)" : ""));
			sb.AppendLine("resolution: " + Screen.width + "x" + Screen.height + " " + Screen.fullScreenMode + "  render scale " + cfg.renderScale + "%  quality level " + QualitySettings.names[QualitySettings.GetQualityLevel()]);
			sb.AppendLine("gpu: " + SystemInfo.graphicsDeviceName + " (" + SystemInfo.graphicsDeviceType + ")  cpu: " + SystemInfo.processorType);
			sb.AppendLine("frames: " + n);
			if (n > 0)
			{
				float avg = Avg(_frames);
				int low = Mathf.Max(1, n / 100);
				float lowSum = 0f;
				for (int i = n - low; i < n; i++) lowSum += sorted[i];
				sb.AppendLine("average fps: " + (1f / avg).ToString("F1") + "  (" + (avg * 1000f).ToString("F2") + " ms)   1% low fps: " + (low / lowSum).ToString("F1"));
				sb.AppendLine("99th pct frame: " + (sorted[Mathf.Min(n - 1, (int)(n * 0.99f))] * 1000f).ToString("F2") + " ms   worst: " + (sorted[n - 1] * 1000f).ToString("F2") + " ms");
				sb.AppendLine("gpu frame: " + Avg(_gpu).ToString("F2") + " ms   cpu main thread: " + Avg(_cpu).ToString("F2") + " ms");
				int over = 0;
				foreach (float f in sorted) if (f > 1f / 60f) over++;
				sb.AppendLine("frames slower than 60 fps: " + (100f * over / n).ToString("F1") + "%");
			}
			int shown = 0;
			foreach (string s in _notes) if (shown++ < 12) sb.AppendLine(s);
			Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_logPath)));
			File.WriteAllText(_logPath, sb.ToString());
			Debug.Log(sb.ToString());
		}
	}
}
