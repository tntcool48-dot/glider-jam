using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GliderJam.UI
{
	public class MainMenuScreen : MenuScreen
	{
		protected override void Build()
		{
			Label("GLIDER JAM", 120f, new Vector2(120f, -150f), Color.white, FontStyles.Bold | FontStyles.Italic);
			Label("GOLDEN PILLARS  /  SAILPLANE RACING", 30f, new Vector2(126f, -290f), new Color(1f, 0.82f, 0.55f));
			MakeButton("SOLO RACE", new Vector2(120f, -420f), GameSettings.PlayerColors[0], () => Start(GameMode.Solo));
			MakeButton("SPLIT-SCREEN  -  2 PLAYERS", new Vector2(120f, -520f), GameSettings.PlayerColors[1], () => Start(GameMode.SplitScreen));
			MakeButton("QUIT", new Vector2(120f, -620f), new Color(0.5f, 0.55f, 0.6f), Quit);
			Label("<color=#FF2E4D>PLAYER 1</color>   W/S pitch   A/D turn   SPACE boost   L-SHIFT brake", 24f, new Vector2(120f, -800f), new Color(1f, 1f, 1f, 0.85f), FontStyles.Normal, 1600f);
			Label("<color=#1EC8FF>PLAYER 2</color>   UP/DOWN pitch   LEFT/RIGHT turn   R-CTRL boost   R-SHIFT brake", 24f, new Vector2(120f, -840f), new Color(1f, 1f, 1f, 0.85f), FontStyles.Normal, 1600f);
			Label("Gamepads: left stick to fly, RT boost, LT brake (pad 1 = P1, pad 2 = P2)", 22f, new Vector2(120f, -880f), new Color(1f, 1f, 1f, 0.6f), FontStyles.Normal, 1600f);
			string best = GameSettings.BestSoloTime > 0f ? "Best solo time  " + GameSettings.FormatTime(GameSettings.BestSoloTime) : "";
			Label(best, 24f, new Vector2(120f, -950f), new Color(1f, 0.85f, 0.5f), FontStyles.Bold);
		}

		private static void Start(GameMode mode)
		{
			GameSettings.Mode = mode;
			GameSettings.HasResult = false;
			Load(GameSettings.RaceScene);
		}
	}
}
