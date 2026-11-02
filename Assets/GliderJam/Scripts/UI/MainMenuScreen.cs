using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GliderJam.UI
{
	// Main menu over rendered stills of the maps, slowly zooming and drifting, crossfading to the selected map. (It used
	// to load the whole canyon behind the menu for a live flyover: seconds of loading freezes and a busy GPU made the
	// menu laggy.)
	public class MainMenuScreen : MenuScreen
	{
		private RawImage _bgNext;
		private Texture2D _canyonBg, _cityBg;
		private float _fadeStart = -1f;
		private SettingsPanel _settings;
		private Button _solo;
		private ChoiceSelectable _map;
		private TextMeshProUGUI _best;
		private TextMeshProUGUI _controls;
		private GameObject _main;

		protected override void Start()
		{
			base.Start();
			GameSettings.Showcase = false;
			_canyonBg = Resources.Load<Texture2D>("CanyonBackground") ?? background;
			_cityBg = Resources.Load<Texture2D>("CityBackground");
			if (BackgroundImage == null) return;
			BackgroundImage.gameObject.AddComponent<Canvas>(); // own canvas: the slow zoom doesn't re-batch the whole menu
			_bgNext = Instantiate(BackgroundImage, BackgroundImage.transform.parent);
			_bgNext.name = "Background Next";
			_bgNext.transform.SetSiblingIndex(BackgroundImage.transform.GetSiblingIndex() + 1);
			_bgNext.color = new Color(1f, 1f, 1f, 0f);
			ShowMap(false);
		}

		// The selected map's picture: crossfade to it (or set it straight away).
		private void ShowMap(bool fade)
		{
			Texture2D tex = GameSettings.Map == GameSettings.CityScene && _cityBg != null ? _cityBg : _canyonBg;
			if (BackgroundImage == null || tex == null || BackgroundImage.texture == tex) return;
			if (!fade)
			{
				BackgroundImage.texture = tex;
				BackgroundImage.uvRect = FitCover(tex);
				return;
			}
			_bgNext.texture = tex;
			_bgNext.uvRect = FitCover(tex);
			_fadeStart = Time.unscaledTime;
		}

		private void Update()
		{
			if (BackgroundImage == null) return;
			// Slow zoom and drift (scaled up a little so the edges never show).
			float t = Time.unscaledTime;
			float scale = 1.06f + 0.04f * Mathf.Sin(t * 0.13f);
			var drift = new Vector2(Mathf.Sin(t * 0.07f) * 16f, Mathf.Cos(t * 0.05f) * 9f);
			foreach (RawImage img in new[] { BackgroundImage, _bgNext })
			{
				img.rectTransform.localScale = new Vector3(scale, scale, 1f);
				img.rectTransform.anchoredPosition = drift;
			}
			if (_fadeStart < 0f) return;
			float a = Mathf.Clamp01((Time.unscaledTime - _fadeStart) / 0.6f);
			_bgNext.color = new Color(1f, 1f, 1f, a);
			if (a < 1f) return;
			BackgroundImage.texture = _bgNext.texture;
			BackgroundImage.uvRect = _bgNext.uvRect;
			_bgNext.color = new Color(1f, 1f, 1f, 0f);
			_fadeStart = -1f;
		}

		protected override void Build()
		{
			_main = UiKit.Stretch(UiKit.Rect("Main", Root)).gameObject;
			Transform m = _main.transform;
			Label("GLIDER JAM", 120f, new Vector2(120f, -130f), Color.white, FontStyles.Bold | FontStyles.Italic).transform.SetParent(m, true);
			Label("SAILPLANE RACING", 30f, new Vector2(126f, -270f), UiKit.Gold).transform.SetParent(m, true);
			_solo = Item(m, "SOLO RACE", 0, GameSettings.PlayerColors[0], () => Begin(GameMode.Solo));
			Button split = Item(m, "SPLIT-SCREEN  -  2 PLAYERS", 1, GameSettings.PlayerColors[1], () => Begin(GameMode.SplitScreen));
			_map = MapChoice(m, 2);
			Button settings = Item(m, "SETTINGS", 3, new Color(0.6f, 0.7f, 0.8f), OpenSettings);
			Button quit = Item(m, "QUIT", 4, new Color(0.5f, 0.55f, 0.6f), Quit);
			UiKit.LinkVertical(new Selectable[] { _solo, split, _map, settings, quit });
			_controls = Label("", 22f, new Vector2(120f, -880f), new Color(1f, 1f, 1f, 0.8f), FontStyles.Normal, 1700f);
			_controls.transform.SetParent(m, true);
			_best = Label("", 24f, new Vector2(120f, -960f), UiKit.Gold, FontStyles.Bold, 900f);
			_best.transform.SetParent(m, true);
			TextMeshProUGUI credit = UiKit.Text(m, "Music: Kevin MacLeod (incompetech.com), CC BY 4.0   |   Font: Rajdhani (SIL OFL)   |   Textures: Poly Haven (CC0)", 18f, new Color(1f, 1f, 1f, 0.45f), TextAlignmentOptions.BottomRight, FontStyles.Normal);
			UiKit.Stretch(credit.rectTransform, 0f, 24f, 40f, 0f);
			Refresh();
			SelectFirst(_solo);
		}

		private Button Item(Transform parent, string label, int i, Color accent, UnityEngine.Events.UnityAction action)
		{
			Button b = UiKit.Button(parent, label, accent, action, 34f);
			var rt = (RectTransform)b.transform;
			rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
			rt.anchoredPosition = new Vector2(120f, -360f - i * 92f);
			rt.sizeDelta = new Vector2(640f, 78f);
			return b;
		}

		private ChoiceSelectable MapChoice(Transform parent, int i)
		{
			Image img = UiKit.Panel(parent, "Map", Color.white);
			var rt = img.rectTransform;
			rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
			rt.anchoredPosition = new Vector2(120f, -360f - i * 92f);
			rt.sizeDelta = new Vector2(640f, 78f);
			var c = img.gameObject.AddComponent<ChoiceSelectable>();
			UiKit.Style(c, UiKit.Gold);
			c.targetGraphic = img;
			bool city = Application.CanStreamedLevelBeLoaded(GameSettings.CityScene);
			c.options = city ? new[] { "MAP:  GOLDEN PILLARS", "MAP:  NEON CITY" } : new[] { "MAP:  GOLDEN PILLARS" };
			c.valueText = UiKit.Text(img.transform, "", 30f, Color.white, TextAlignmentOptions.Center, FontStyles.Bold | FontStyles.Italic);
			UiKit.Stretch(c.valueText.rectTransform, 50f, 0f, 50f);
			TextMeshProUGUI l = UiKit.Text(img.transform, "<", 32f, UiKit.Gold, TextAlignmentOptions.MidlineLeft);
			UiKit.Stretch(l.rectTransform, 22f);
			TextMeshProUGUI r = UiKit.Text(img.transform, ">", 32f, UiKit.Gold, TextAlignmentOptions.MidlineRight);
			UiKit.Stretch(r.rectTransform, 0f, 0f, 22f);
			c.SetIndex(GameSettings.Map == GameSettings.CityScene && city ? 1 : 0, false);
			c.onChanged = idx => { GameSettings.Map = idx == 1 ? GameSettings.CityScene : GameSettings.RaceScene; Refresh(); ShowMap(true); };
			return c;
		}

		private void Refresh()
		{
			Key[] p1 = GameConfig.Current.players[0].keys, p2 = GameConfig.Current.players[1].keys;
			_controls.text = "<color=#FF2E4D>PLAYER 1</color>   " + K(p1[0]) + "/" + K(p1[1]) + " pitch   " + K(p1[2]) + "/" + K(p1[3]) + " turn   " + K(p1[4]) + " boost   " + K(p1[5]) + " brake\n"
				+ "<color=#1EC8FF>PLAYER 2</color>   " + K(p2[0]) + "/" + K(p2[1]) + " pitch   " + K(p2[2]) + "/" + K(p2[3]) + " turn   " + K(p2[4]) + " boost   " + K(p2[5]) + " brake\n"
				+ "<color=#FFFFFF99>Gamepads: left stick fly, RT boost, LT brake, START pause</color>";
			float best = GameSettings.BestSoloTime;
			_best.text = best > 0f ? "BEST SOLO TIME  " + GameSettings.FormatTime(best) + "  (" + GameSettings.MapName + ")" : "";
		}

		private static string K(Key k)
		{
			var c = Keyboard.current?[k];
			return (c != null ? c.displayName : k.ToString()).ToUpper();
		}

		private void OpenSettings()
		{
			_main.SetActive(false);
			_settings = SettingsPanel.Open(Root, () =>
			{
				_settings = null;
				_main.SetActive(true);
				Refresh();
				SelectFirst(_solo);
			});
		}

		private static void SelectFirst(Selectable s)
		{
			if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(s.gameObject);
		}

		private static void Begin(GameMode mode)
		{
			GameSettings.Mode = mode;
			GameSettings.HasResult = false;
			GameSettings.Showcase = false;
			Load(GameSettings.Map);
		}
	}
}
