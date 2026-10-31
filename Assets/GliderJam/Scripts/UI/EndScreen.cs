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
			// City races end over a city backdrop (rendered from the game into Resources/CityBackground).
			if (GameSettings.Map == GameSettings.CityScene && BackgroundImage != null)
			{
				var city = Resources.Load<Texture2D>("CityBackground");
				if (city != null)
				{
					BackgroundImage.texture = city;
					BackgroundImage.uvRect = FitCover(city);
				}
			}
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
				Label(GameSettings.FormatTime(t), 72f, new Vector2(126f, -335f), Color.white, FontStyles.Bold);
				Label(record ? "NEW BEST TIME" : "Best  " + GameSettings.FormatTime(GameSettings.BestSoloTime), 30f, new Vector2(126f, -435f), new Color(1f, 0.85f, 0.5f));
			}
			else
			{
				Color c = w >= 0 ? GameSettings.PlayerColors[w] : Color.white;
				Label(w >= 0 ? GameSettings.PlayerNames[w] + " WINS" : "NO WINNER", 110f, new Vector2(120f, -170f), c, FontStyles.Bold | FontStyles.Italic, 1600f);
				for (int i = 0; i < 2; i++)
				{
					string time = GameSettings.Finished[i] ? GameSettings.FormatTime(GameSettings.FinishTimes[i]) : "DNF";
					string tag = "<color=#" + ColorUtility.ToHtmlStringRGB(GameSettings.PlayerColors[i]) + ">" + GameSettings.PlayerNames[i] + "</color>";
					Label(tag + "    " + time, 40f, new Vector2(126f, -350f - i * 64f), Color.white, FontStyles.Bold, 1600f);
				}
			}
			Label(GameSettings.MapName, 26f, new Vector2(126f, -292f), UiKit.Gold, FontStyles.Bold);
			MakeButton("RESTART", new Vector2(120f, -620f), w >= 0 ? GameSettings.PlayerColors[w] : GameSettings.PlayerColors[0], () => Load(GameSettings.Map));
			MakeButton("MAIN MENU", new Vector2(120f, -720f), new Color(0.5f, 0.55f, 0.6f), () => Load(GameSettings.MenuScene));
		}
}
}
