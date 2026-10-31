using UnityEngine;

namespace GliderJam
{
	public enum GameMode
	{
		Solo = 0,
		SplitScreen = 1
	}

	// Cross-scene state: chosen mode (menu -> race) and the race result (race -> end screen).
	public static class GameSettings
	{
		public static GameMode Mode = GameMode.SplitScreen;

		public static readonly Color[] PlayerColors =
		{
			new Color(1f, 0.18f, 0.30f), // P1 signal red
			new Color(0.12f, 0.78f, 1f)  // P2 electric cyan
		};

		public static readonly string[] PlayerNames = { "PLAYER 1", "PLAYER 2" };

		public static int PlayerCount => Mode == GameMode.SplitScreen ? 2 : 1;

		// Race result handed to the end scene.
		public static int Winner = -1;
		public static float[] FinishTimes = new float[2];
		public static bool[] Finished = new bool[2];
		public static int[] MissedCheckpoints = new int[2];
		public static bool HasResult;

		public const string RaceScene = "yazan scene";
		public const string MenuScene = "mainUI";
		public const string EndScene = "end";

		public static float BestSoloTime
		{
			get => PlayerPrefs.GetFloat("GliderJam.BestSolo", 0f);
			set { PlayerPrefs.SetFloat("GliderJam.BestSolo", value); PlayerPrefs.Save(); }
		}

		public static string FormatTime(float t)
		{
			if (t <= 0f)
			{
				return "--:--.--";
			}
			int m = (int)(t / 60f);
			float s = t - m * 60f;
			return m.ToString("00") + ":" + s.ToString("00.00");
		}
	}
}
