using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GliderJam.UI
{
	public class EndScreen : MenuScreen
	{
		protected override void Build()
		{
			GameSettings.Showcase = false;
			int w = GameSettings.Winner;
			bool solo = GameSettings.Mode == GameMode.Solo;
			if (!GameSettings.HasResult)
			{
				Label("RACE OVER", 110f, new Vector2(120f, -170f), Color.white, FontStyles.Bold | FontStyles.Italic);
			}
			else if (solo)
			{
				float t = GameSettings.FinishTimes[0];
				bool record = GameSettings.BestSoloTime <= 0f || t < GameSettings.BestSoloTime;
				if (record) GameSettings.BestSoloTime = t;
				Label("FINISHED", 110f, new Vector2(120f, -170f), GameSettings.PlayerColors[0], FontStyles.Bold | FontStyles.Italic);
				Label(GameSettings.FormatTime(t), 72f, new Vector2(126f, -320f), Color.white, FontStyles.Bold);
				Label(record ? "NEW BEST TIME" : "Best  " + GameSettings.FormatTime(GameSettings.BestSoloTime), 30f, new Vector2(126f, -420f), new Color(1f, 0.85f, 0.5f));
				Label(Missed(0), 26f, new Vector2(126f, -470f), new Color(1f, 1f, 1f, 0.7f), FontStyles.Normal);
			}
			else
			{
				Color c = w >= 0 ? GameSettings.PlayerColors[w] : Color.white;
				Label(w >= 0 ? GameSettings.PlayerNames[w] + " WINS" : "NO WINNER", 110f, new Vector2(120f, -170f), c, FontStyles.Bold | FontStyles.Italic, 1600f);
				for (int i = 0; i < 2; i++)
				{
					string time = GameSettings.Finished[i] ? GameSettings.FormatTime(GameSettings.FinishTimes[i]) : "DNF";
					string tag = "<color=#" + ColorUtility.ToHtmlStringRGB(GameSettings.PlayerColors[i]) + ">" + GameSettings.PlayerNames[i] + "</color>";
					Label(tag + "    " + time + "    <size=70%>" + Missed(i) + "</size>", 40f, new Vector2(126f, -330f - i * 64f), Color.white, FontStyles.Bold, 1600f);
				}
			}
			Label(GameSettings.MapName, 26f, new Vector2(126f, -268f), UiKit.Gold, FontStyles.Bold);
			MakeButton("RESTART", new Vector2(120f, -620f), w >= 0 ? GameSettings.PlayerColors[w] : GameSettings.PlayerColors[0], () => Load(GameSettings.Map));
			MakeButton("MAIN MENU", new Vector2(120f, -720f), new Color(0.5f, 0.55f, 0.6f), () => Load(GameSettings.MenuScene));
		}

		private static string Missed(int i)
		{
			int m = GameSettings.MissedCheckpoints[i];
			return m == 0 ? "all checkpoints" : m + " missed checkpoint" + (m > 1 ? "s" : "") + " (+" + (m * 3) + " s)";
		}
	}
}
