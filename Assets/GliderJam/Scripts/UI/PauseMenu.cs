using GliderJam.Flight;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GliderJam.UI
{
	// In-race pause (Esc / Start): Resume, Settings, Restart, Main menu. Freezes time and game audio.
	public class PauseMenu : MonoBehaviour
	{
		private Canvas _canvas;
		private RectTransform _menu;
		private Button _resume;
		private SettingsPanel _settings;
		public bool Paused { get; private set; }

		private void Start()
		{
			if (EventSystem.current == null)
			{
				new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
			}
			_canvas = UiKit.Canvas("Pause Canvas", 50);
			_canvas.transform.SetParent(transform, false);
			_menu = UiKit.Stretch(UiKit.Rect("Pause", _canvas.transform));
			_menu.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
			TextMeshPro(UiKit.Text(_menu, "PAUSED", 110f, Color.white, TMPro.TextAlignmentOptions.Left, TMPro.FontStyles.Bold | TMPro.FontStyles.Italic), new Vector2(120f, -200f), new Vector2(900f, 140f));
			_resume = Item("RESUME", 0, UiKit.Gold, Resume);
			Button settings = Item("SETTINGS", 1, new Color(0.6f, 0.7f, 0.8f), OpenSettings);
			Button restart = Item("RESTART", 2, GameSettings.PlayerColors[0], () => Leave(GameSettings.Map));
			Button menu = Item("MAIN MENU", 3, new Color(0.5f, 0.55f, 0.6f), () => Leave(GameSettings.MenuScene));
			UiKit.LinkVertical(new Selectable[] { _resume, settings, restart, menu });
			_menu.gameObject.SetActive(false);
		}

		private static void TextMeshPro(TMPro.TextMeshProUGUI t, Vector2 pos, Vector2 size)
		{
			t.rectTransform.anchorMin = t.rectTransform.anchorMax = t.rectTransform.pivot = new Vector2(0f, 1f);
			t.rectTransform.anchoredPosition = pos;
			t.rectTransform.sizeDelta = size;
		}

		private Button Item(string label, int i, Color accent, UnityEngine.Events.UnityAction action)
		{
			Button b = UiKit.Button(_menu, label, accent, action, 36f);
			var rt = (RectTransform)b.transform;
			rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
			rt.anchoredPosition = new Vector2(120f, -400f - i * 100f);
			rt.sizeDelta = new Vector2(560f, 82f);
			return b;
		}

		private int _settingsClosedFrame = -1;

		private void Update()
		{
			if (_settings != null || Time.frameCount <= _settingsClosedFrame + 1) return;
			if (GliderInput.PausePressed())
			{
				if (Paused) Resume(); else Pause();
			}
		}

		public void Pause()
		{
			Paused = true;
			Time.timeScale = 0f;
			AudioListener.pause = true;
			InputSystem.ResetHaptics();
			_menu.gameObject.SetActive(true);
			EventSystem.current?.SetSelectedGameObject(_resume.gameObject);
		}

		public void Resume()
		{
			Paused = false;
			Time.timeScale = 1f;
			AudioListener.pause = false;
			_menu.gameObject.SetActive(false);
		}

		private void OpenSettings()
		{
			_menu.gameObject.SetActive(false);
			_settings = SettingsPanel.Open(_canvas.transform, () =>
			{
				_settings = null;
				_settingsClosedFrame = Time.frameCount;
				_menu.gameObject.SetActive(true);
				EventSystem.current?.SetSelectedGameObject(_resume.gameObject);
			});
		}

		private static void Leave(string scene)
		{
			Time.timeScale = 1f;
			AudioListener.pause = false;
			InputSystem.ResetHaptics();
			GameSettings.Showcase = false;
			SceneLoader.Load(scene);
		}
	}
}
