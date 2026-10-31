using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GliderJam
{
	// Player benchmark: launch the build with  -benchmark [-benchmarkLog <file>] [-benchmarkSeconds 90]
	// It starts a split-screen race with both gliders on autopilot (vsync off), records frame times after a
	// warm-up, writes a summary to the log file and quits.
	public class Benchmark : MonoBehaviour
	{
		public static bool Active { get; private set; }

		private string _logPath = "benchmark.txt";
		private float _seconds = 90f;
		private float _warmup = 6f;
		private readonly List<float> _frames = new List<float>(20000);
		private float _raceStartedAt = -1f;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Init()
		{
			string[] args = System.Environment.GetCommandLineArgs();
			if (System.Array.IndexOf(args, "-benchmark") < 0) return;
			Active = true;
			var go = new GameObject("Benchmark");
			DontDestroyOnLoad(go);
			var b = go.AddComponent<Benchmark>();
			for (int i = 0; i < args.Length - 1; i++)
			{
				if (args[i] == "-benchmarkLog") b._logPath = args[i + 1];
				if (args[i] == "-benchmarkSeconds") float.TryParse(args[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out b._seconds);
			}
			QualitySettings.vSyncCount = 0;
			Application.targetFrameRate = -1;
			GameSettings.Mode = GameMode.SplitScreen;
			GameSettings.Showcase = false;
			SceneManager.LoadScene(GameSettings.RaceScene);
		}

		private void Update()
		{
			string scene = SceneManager.GetActiveScene().name;
			if (scene == GameSettings.RaceScene && _raceStartedAt < 0f)
			{
				_raceStartedAt = Time.unscaledTime;
			}
			if (_raceStartedAt < 0f) return;
			float t = Time.unscaledTime - _raceStartedAt;
			if (t > _warmup && scene == GameSettings.RaceScene)
			{
				_frames.Add(Time.unscaledDeltaTime);
			}
			if (t > _warmup + _seconds || scene == GameSettings.EndScene)
			{
				Write();
				Application.Quit();
				enabled = false;
			}
		}

		private void Write()
		{
			var sorted = new List<float>(_frames);
			sorted.Sort();
			int n = sorted.Count;
			var sb = new StringBuilder();
			sb.AppendLine("Glider Jam benchmark (split-screen, both players on autopilot)");
			sb.AppendLine("resolution: " + Screen.width + "x" + Screen.height + " fullscreen=" + Screen.fullScreenMode);
			sb.AppendLine("gpu: " + SystemInfo.graphicsDeviceName + " (" + SystemInfo.graphicsDeviceType + ")");
			sb.AppendLine("cpu: " + SystemInfo.processorType);
			sb.AppendLine("frames: " + n);
			if (n > 0)
			{
				float total = 0f;
				foreach (float f in sorted) total += f;
				float avg = total / n;
				float p99 = sorted[Mathf.Min(n - 1, (int)(n * 0.99f))];
				float p95 = sorted[Mathf.Min(n - 1, (int)(n * 0.95f))];
				float onePercentLow = 0f;
				int lowCount = Mathf.Max(1, n / 100);
				for (int i = n - lowCount; i < n; i++) onePercentLow += sorted[i];
				onePercentLow /= lowCount;
				sb.AppendLine("average fps: " + (1f / avg).ToString("F1") + "  (" + (avg * 1000f).ToString("F2") + " ms)");
				sb.AppendLine("1% low fps: " + (1f / onePercentLow).ToString("F1"));
				sb.AppendLine("95th pct frame: " + (p95 * 1000f).ToString("F2") + " ms   99th: " + (p99 * 1000f).ToString("F2") + " ms   worst: " + (sorted[n - 1] * 1000f).ToString("F2") + " ms");
				int over = 0;
				foreach (float f in sorted) if (f > 1f / 60f) over++;
				sb.AppendLine("frames slower than 60 fps: " + (100f * over / n).ToString("F1") + "%");
			}
			File.WriteAllText(_logPath, sb.ToString());
			Debug.Log(sb.ToString());
		}
	}
}
