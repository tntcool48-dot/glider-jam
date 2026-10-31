using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace GliderJam.UI
{
	// Settings screen (main menu and pause menu): Graphics, Display, Audio and Controls tabs.
	// Every change applies immediately and is saved (GameConfig.Commit). Keyboard, mouse and gamepad navigable.
	public class SettingsPanel : MonoBehaviour
	{
		private static readonly string[] Tabs = { "GRAPHICS", "DISPLAY", "AUDIO", "CONTROLS" };
		private static readonly string[] OffOn = { "OFF", "ON" };
		private static readonly string[] ActionNames = { "PITCH UP", "PITCH DOWN", "TURN LEFT", "TURN RIGHT", "BOOST", "BRAKE" };

		private Action _onClose;
		private RectTransform _content;
		private ScrollRect _scroll;
		private readonly List<Selectable> _rows = new List<Selectable>();
		private Button[] _tabButtons;
		private Button _reset, _back;
		private int _tab;
		private int _bindPlayer = -1;
		private ControlAction _bindAction;
		private TextMeshProUGUI _bindLabel;
		private float _bindStarted;
		private List<Vector2Int> _resolutions;

		public static SettingsPanel Open(Transform canvas, Action onClose)
		{
			RectTransform root = UiKit.Stretch(UiKit.Rect("Settings", canvas));
			var p = root.gameObject.AddComponent<SettingsPanel>();
			p._onClose = onClose;
			p.Build(root);
			return p;
		}

		private GameConfig Cfg => GameConfig.Current;

		private void Build(RectTransform root)
		{
			Image dim = root.gameObject.AddComponent<Image>();
			dim.color = new Color(0f, 0f, 0f, 0.55f);
			Image window = UiKit.Panel(root, "Window", new Color(0.03f, 0.035f, 0.05f, 0.94f));
			RectTransform w = window.rectTransform;
			w.anchorMin = w.anchorMax = new Vector2(0.5f, 0.5f);
			w.sizeDelta = new Vector2(1380f, 900f);
			TextMeshProUGUI title = UiKit.Text(w, "SETTINGS", 56f, Color.white, TextAlignmentOptions.TopLeft, FontStyles.Bold | FontStyles.Italic);
			title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0f, 1f);
			title.rectTransform.pivot = new Vector2(0f, 1f);
			title.rectTransform.anchoredPosition = new Vector2(48f, -30f);
			title.rectTransform.sizeDelta = new Vector2(500f, 70f);

			_tabButtons = new Button[Tabs.Length];
			for (int i = 0; i < Tabs.Length; i++)
			{
				int tab = i;
				Button b = UiKit.Button(w, Tabs[i], UiKit.Gold, () => ShowTab(tab), 28f);
				var rt = (RectTransform)b.transform;
				rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
				rt.pivot = new Vector2(0f, 1f);
				rt.anchoredPosition = new Vector2(48f + i * 290f, -112f);
				rt.sizeDelta = new Vector2(270f, 60f);
				_tabButtons[i] = b;
			}

			// Scrollable rows
			RectTransform view = UiKit.Rect("Viewport", w);
			UiKit.Stretch(view, 48f, 110f, 48f, 196f);
			view.gameObject.AddComponent<RectMask2D>();
			_content = UiKit.Rect("Content", view);
			_content.anchorMin = new Vector2(0f, 1f);
			_content.anchorMax = new Vector2(1f, 1f);
			_content.pivot = new Vector2(0.5f, 1f);
			_content.sizeDelta = Vector2.zero;
			_content.anchoredPosition = Vector2.zero;
			var layout = _content.gameObject.AddComponent<VerticalLayoutGroup>();
			layout.padding = new RectOffset(4, 12, 4, 12);
			layout.spacing = 6f;
			layout.childControlHeight = true;
			layout.childControlWidth = true;
			layout.childForceExpandHeight = false;
			_content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
			_scroll = view.gameObject.AddComponent<ScrollRect>();
			_scroll.content = _content;
			_scroll.horizontal = false;
			_scroll.movementType = ScrollRect.MovementType.Clamped;
			_scroll.scrollSensitivity = 40f;

			_reset = UiKit.Button(w, "RESET DEFAULTS", new Color(0.55f, 0.6f, 0.66f), () => { GameConfig.ResetToDefaults(); ShowTab(_tab); }, 26f);
			Place(_reset, new Vector2(0f, 0f), new Vector2(48f, 30f), new Vector2(340f, 60f));
			_back = UiKit.Button(w, "BACK", UiKit.Gold, Close, 28f);
			Place(_back, new Vector2(1f, 0f), new Vector2(-48f, 30f), new Vector2(260f, 60f));
			ShowTab(0);
		}

		private static void Place(Button b, Vector2 anchor, Vector2 pos, Vector2 size)
		{
			var rt = (RectTransform)b.transform;
			rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
			rt.anchoredPosition = pos;
			rt.sizeDelta = size;
		}

		private void ShowTab(int tab)
		{
			_tab = tab;
			_bindPlayer = -1;
			foreach (Transform c in _content) Destroy(c.gameObject);
			_rows.Clear();
			for (int i = 0; i < _tabButtons.Length; i++) _tabButtons[i].GetComponent<Image>().color = i == tab ? new Color(0.35f, 0.3f, 0.22f, 1f) : Color.white;
			switch (tab)
			{
				case 0: BuildGraphics(); break;
				case 1: BuildDisplay(); break;
				case 2: BuildAudio(); break;
				default: BuildControls(); break;
			}
			// Navigation: tabs <-> first row, rows vertically, last row -> footer.
			var nav = new List<Selectable>(_rows);
			UiKit.LinkVertical(nav);
			for (int i = 0; i < _tabButtons.Length; i++)
			{
				Navigation n = _tabButtons[i].navigation;
				n.mode = Navigation.Mode.Explicit;
				n.selectOnLeft = i > 0 ? _tabButtons[i - 1] : null;
				n.selectOnRight = i < _tabButtons.Length - 1 ? _tabButtons[i + 1] : null;
				n.selectOnDown = _rows.Count > 0 ? _rows[0] : _back;
				_tabButtons[i].navigation = n;
			}
			if (_rows.Count > 0)
			{
				Navigation first = _rows[0].navigation;
				first.selectOnUp = _tabButtons[tab];
				_rows[0].navigation = first;
				Navigation last = _rows[_rows.Count - 1].navigation;
				last.selectOnDown = _back;
				_rows[_rows.Count - 1].navigation = last;
			}
			Navigation bn = _back.navigation;
			bn.mode = Navigation.Mode.Explicit;
			bn.selectOnUp = _rows.Count > 0 ? _rows[_rows.Count - 1] : _tabButtons[tab];
			bn.selectOnLeft = _reset;
			_back.navigation = bn;
			Navigation rn = _reset.navigation;
			rn.mode = Navigation.Mode.Explicit;
			rn.selectOnUp = bn.selectOnUp;
			rn.selectOnRight = _back;
			_reset.navigation = rn;
			_scroll.verticalNormalizedPosition = 1f;
			EventSystem.current?.SetSelectedGameObject(_tabButtons[tab].gameObject);
		}

		// ---------------------------------------------------------------- tabs

		private void BuildGraphics()
		{
			string[] presets = GameConfig.PresetNames;
			Choice("QUALITY PRESET", presets, () => (int)Cfg.preset, i =>
			{
				if (i == (int)QualityPreset.Custom) i = (int)QualityPreset.Low;
				if (i == (int)QualityPreset.UltraRayTracing && !SystemInfo.supportsRayTracing) i = (int)QualityPreset.Ultra;
				Cfg.ApplyPreset((QualityPreset)i);
				Cfg.Commit();
				ShowTab(0);
			});
			Header("LIGHTING AND ATMOSPHERE");
			Graphic("SHADOWS", new[] { "OFF", "LOW", "MEDIUM", "HIGH", "ULTRA" }, () => Cfg.shadows, v => Cfg.shadows = v);
			Graphic("VOLUMETRIC CLOUDS", new[] { "OFF", "LOW", "HIGH", "ULTRA" }, () => Cfg.clouds, v => Cfg.clouds = v);
			Graphic("VOLUMETRIC FOG", new[] { "OFF", "LOW", "HIGH", "ULTRA" }, () => Cfg.fog, v => Cfg.fog = v);
			string[] rt = SystemInfo.supportsRayTracing ? new[] { "OFF", "SCREEN SPACE", "RAY TRACED" } : new[] { "OFF", "SCREEN SPACE" };
			Graphic("AMBIENT OCCLUSION", rt, () => Cfg.ambientOcclusion, v => Cfg.ambientOcclusion = v);
			Graphic("REFLECTIONS", rt, () => Cfg.reflections, v => Cfg.reflections = v);
			Graphic("GLOBAL ILLUMINATION", rt, () => Cfg.globalIllumination, v => Cfg.globalIllumination = v);
			Header("WORLD");
			Graphic("TEXTURES", new[] { "LOW", "MEDIUM", "HIGH" }, () => Cfg.textureQuality, v => Cfg.textureQuality = v);
			Graphic("VEGETATION", new[] { "LOW", "MEDIUM", "HIGH", "ULTRA" }, () => Cfg.vegetation, v => Cfg.vegetation = v);
			Graphic("TERRAIN DETAIL", new[] { "LOW", "MEDIUM", "HIGH", "ULTRA" }, () => Cfg.terrainDetail, v => Cfg.terrainDetail = v);
			Graphic("DISTANT LANDSCAPE", new[] { "OFF", "LOW", "HIGH" }, () => Cfg.backdrop, v => Cfg.backdrop = v);
			Graphic("WATER", OffOn, () => Cfg.water, v => Cfg.water = v);
			Graphic("EFFECTS DETAIL", new[] { "LOW", "MEDIUM", "HIGH" }, () => Cfg.effects, v => Cfg.effects = v);
			Header("POST PROCESSING");
			Graphic("ANTI-ALIASING", new[] { "OFF", "FXAA", "SMAA", "TAA" }, () => Cfg.antiAliasing, v => Cfg.antiAliasing = v);
			Toggle("MOTION BLUR", () => Cfg.motionBlur, v => Cfg.motionBlur = v);
			Toggle("DEPTH OF FIELD (MENUS)", () => Cfg.depthOfField, v => Cfg.depthOfField = v);
			Toggle("FILM GRAIN", () => Cfg.filmGrain, v => Cfg.filmGrain = v);
			Toggle("LENS FLARE", () => Cfg.lensFlare, v => Cfg.lensFlare = v);
			Toggle("CHROMATIC ABERRATION (BOOST)", () => Cfg.chromaticAberration, v => Cfg.chromaticAberration = v);
			Toggle("SPEED EFFECTS", () => Cfg.speedEffects, v => Cfg.speedEffects = v);
			Toggle("CAMERA SHAKE", () => Cfg.cameraShake, v => Cfg.cameraShake = v);
		}

		private void BuildDisplay()
		{
			_resolutions = Screen.resolutions.Select(r => new Vector2Int(r.width, r.height)).Distinct().OrderBy(r => r.x * 10000 + r.y).ToList();
			var res = new List<string> { "NATIVE" };
			res.AddRange(_resolutions.Select(r => r.x + " x " + r.y));
			Choice("RESOLUTION", res.ToArray(), () =>
			{
				int i = _resolutions.FindIndex(r => r.x == Cfg.resolutionWidth && r.y == Cfg.resolutionHeight);
				return i < 0 ? 0 : i + 1;
			}, i =>
			{
				Cfg.resolutionWidth = i == 0 ? 0 : _resolutions[i - 1].x;
				Cfg.resolutionHeight = i == 0 ? 0 : _resolutions[i - 1].y;
				Cfg.Commit();
			});
			Choice("DISPLAY MODE", new[] { "BORDERLESS", "FULLSCREEN", "WINDOWED" }, () => Cfg.displayMode, i => { Cfg.displayMode = i; Cfg.Commit(); });
			Choice("VSYNC", OffOn, () => Cfg.vsync ? 1 : 0, i => { Cfg.vsync = i == 1; Cfg.Commit(); });
			int[] caps = { 0, 30, 60, 90, 120, 144, 165, 240 };
			Choice("FRAME LIMIT", caps.Select(c => c == 0 ? "UNLIMITED" : c + " FPS").ToArray(), () => Mathf.Max(0, Array.IndexOf(caps, Cfg.frameCap)), i => { Cfg.frameCap = caps[i]; Cfg.Commit(); });
			Slider("RENDER SCALE", 50f, 100f, () => Cfg.renderScale, v => { Cfg.renderScale = Mathf.RoundToInt(v); Cfg.preset = QualityPreset.Custom; Cfg.Commit(); }, v => Mathf.RoundToInt(v) + "%", true);
			Choice("UPSCALER", new[] { "CATMULL-ROM", "TAA UPSCALE", "AMD FSR 1" }, () => Cfg.upscaler, i => { Cfg.upscaler = i; Cfg.Commit(); });
		}

		private void BuildAudio()
		{
			Slider("MASTER", 0f, 1f, () => Cfg.masterVolume, v => { Cfg.masterVolume = v; Cfg.Commit(); }, Pct);
			Slider("MUSIC", 0f, 1f, () => Cfg.musicVolume, v => { Cfg.musicVolume = v; Cfg.Commit(); }, Pct);
			Slider("EFFECTS", 0f, 1f, () => Cfg.effectsVolume, v => { Cfg.effectsVolume = v; Cfg.Commit(); }, Pct);
			Slider("WIND AND AIRFLOW", 0f, 1f, () => Cfg.windVolume, v => { Cfg.windVolume = v; Cfg.Commit(); }, Pct);
		}

		private void BuildControls()
		{
			for (int p = 0; p < 2; p++)
			{
				int player = p;
				Header(GameSettings.PlayerNames[p], GameSettings.PlayerColors[p]);
				for (int a = 0; a < 6; a++) KeyRow(player, (ControlAction)a);
				Choice("INVERT PITCH", OffOn, () => Cfg.players[player].invertPitch ? 1 : 0, i => { Cfg.players[player].invertPitch = i == 1; Cfg.Commit(); });
				Slider("FIELD OF VIEW", 50f, 100f, () => Cfg.players[player].fieldOfView, v => { Cfg.players[player].fieldOfView = Mathf.Round(v); Cfg.Commit(); }, v => Mathf.RoundToInt(v) + "°", true);
				Slider("CONTROLLER VIBRATION", 0f, 1f, () => Cfg.players[player].vibration, v => { Cfg.players[player].vibration = v; Cfg.Commit(); }, Pct);
			}
			Header("CONTROLLERS: PAD 1 = PLAYER 1, PAD 2 = PLAYER 2 (SOLO: ANY PAD)", UiKit.Muted);
			Header("LEFT STICK FLY   RT / A BOOST   LT / B BRAKE   START PAUSE", UiKit.Muted);
		}

		private static string Pct(float v)
		{
			return Mathf.RoundToInt(v * 100f) + "%";
		}

		// ---------------------------------------------------------------- rows

		private RectTransform Row(string label, Color? color = null)
		{
			RectTransform row = UiKit.Rect(label, _content);
			row.gameObject.AddComponent<LayoutElement>().preferredHeight = 58f;
			TextMeshProUGUI t = UiKit.Text(row, label, 26f, color ?? new Color(1f, 1f, 1f, 0.9f));
			t.rectTransform.anchorMin = new Vector2(0f, 0f);
			t.rectTransform.anchorMax = new Vector2(0.5f, 1f);
			t.rectTransform.offsetMin = new Vector2(16f, 0f);
			t.rectTransform.offsetMax = Vector2.zero;
			return row;
		}

		private static RectTransform ControlArea(RectTransform row)
		{
			RectTransform c = UiKit.Rect("Control", row);
			c.anchorMin = new Vector2(0.5f, 0.08f);
			c.anchorMax = new Vector2(1f, 0.92f);
			c.offsetMin = c.offsetMax = Vector2.zero;
			return c;
		}

		private void Header(string text, Color? color = null)
		{
			RectTransform row = UiKit.Rect(text, _content);
			row.gameObject.AddComponent<LayoutElement>().preferredHeight = 54f;
			TextMeshProUGUI t = UiKit.Text(row, text, 22f, color ?? UiKit.Gold, TextAlignmentOptions.BottomLeft);
			UiKit.Stretch(t.rectTransform, 16f, 6f);
		}

		private void Graphic(string label, string[] options, Func<int> get, Action<int> set)
		{
			Choice(label, options, () => Mathf.Clamp(get(), 0, options.Length - 1), i => { set(i); Cfg.preset = QualityPreset.Custom; Cfg.Commit(); RefreshPreset(); });
		}

		private void Toggle(string label, Func<bool> get, Action<bool> set)
		{
			Graphic(label, OffOn, () => get() ? 1 : 0, i => set(i == 1));
		}

		private ChoiceSelectable _presetChoice;

		private void RefreshPreset()
		{
			if (_presetChoice != null) _presetChoice.SetIndex((int)Cfg.preset, false);
		}

		private void Choice(string label, string[] options, Func<int> get, Action<int> set)
		{
			RectTransform area = ControlArea(Row(label));
			var img = area.gameObject.AddComponent<Image>();
			img.sprite = UiKit.Rounded;
			img.type = Image.Type.Sliced;
			var c = area.gameObject.AddComponent<ChoiceSelectable>();
			UiKit.Style(c, UiKit.Gold);
			c.targetGraphic = img;
			c.options = options;
			TextMeshProUGUI left = UiKit.Text(area, "<", 30f, UiKit.Gold, TextAlignmentOptions.MidlineLeft);
			UiKit.Stretch(left.rectTransform, 20f);
			TextMeshProUGUI right = UiKit.Text(area, ">", 30f, UiKit.Gold, TextAlignmentOptions.MidlineRight);
			UiKit.Stretch(right.rectTransform, 0f, 0f, 20f);
			c.valueText = UiKit.Text(area, "", 26f, Color.white, TextAlignmentOptions.Center);
			UiKit.Stretch(c.valueText.rectTransform, 50f, 0f, 50f);
			c.SetIndex(get(), false);
			c.onChanged = set;
			if (label == "QUALITY PRESET") _presetChoice = c;
			_rows.Add(c);
		}

		private void Slider(string label, float min, float max, Func<float> get, Action<float> set, Func<float, string> fmt, bool whole = false)
		{
			RectTransform area = ControlArea(Row(label));
			RectTransform sliderRt = UiKit.Rect("Slider", area);
			sliderRt.anchorMin = new Vector2(0f, 0.2f);
			sliderRt.anchorMax = new Vector2(0.76f, 0.8f);
			sliderRt.offsetMin = sliderRt.offsetMax = Vector2.zero;
			Image bg = UiKit.Panel(sliderRt, "Background", new Color(1f, 1f, 1f, 0.12f));
			UiKit.Stretch(bg.rectTransform, 0f, 8f, 0f, 8f);
			RectTransform fillArea = UiKit.Stretch(UiKit.Rect("Fill Area", sliderRt), 0f, 8f, 0f, 8f);
			Image fill = UiKit.Panel(fillArea, "Fill", UiKit.Gold);
			fill.rectTransform.sizeDelta = Vector2.zero;
			RectTransform handleArea = UiKit.Stretch(UiKit.Rect("Handle Area", sliderRt), 10f, 0f, 10f, 0f);
			Image handle = UiKit.Panel(handleArea, "Handle", Color.white);
			handle.sprite = UiKit.Circle;
			handle.type = Image.Type.Simple;
			handle.rectTransform.sizeDelta = new Vector2(26f, 0f);
			var s = sliderRt.gameObject.AddComponent<UnityEngine.UI.Slider>();
			s.fillRect = fill.rectTransform;
			s.handleRect = handle.rectTransform;
			s.targetGraphic = handle;
			s.minValue = min;
			s.maxValue = max;
			s.wholeNumbers = whole;
			UiKit.Style(s, UiKit.Gold);
			ColorBlock cb = s.colors;
			cb.normalColor = Color.white;
			s.colors = cb;
			TextMeshProUGUI value = UiKit.Text(area, "", 26f, Color.white, TextAlignmentOptions.MidlineRight);
			value.rectTransform.anchorMin = new Vector2(0.78f, 0f);
			value.rectTransform.anchorMax = Vector2.one;
			value.rectTransform.offsetMin = value.rectTransform.offsetMax = Vector2.zero;
			s.SetValueWithoutNotify(get());
			value.text = fmt(s.value);
			if (!whole) s.onValueChanged.AddListener(v => s.SetValueWithoutNotify(Mathf.Round(v * 20f) / 20f));
			s.onValueChanged.AddListener(v => { value.text = fmt(s.value); set(s.value); });
			_rows.Add(s);
		}

		private void KeyRow(int player, ControlAction action)
		{
			RectTransform area = ControlArea(Row(ActionNames[(int)action]));
			var img = area.gameObject.AddComponent<Image>();
			img.sprite = UiKit.Rounded;
			img.type = Image.Type.Sliced;
			var b = area.gameObject.AddComponent<Button>();
			UiKit.Style(b, GameSettings.PlayerColors[player]);
			b.targetGraphic = img;
			TextMeshProUGUI t = UiKit.Text(area, KeyName(Cfg.players[player].keys[(int)action]), 26f, Color.white, TextAlignmentOptions.Center);
			UiKit.Stretch(t.rectTransform);
			b.onClick.AddListener(() =>
			{
				_bindPlayer = player;
				_bindAction = action;
				_bindLabel = t;
				_bindStarted = Time.unscaledTime;
				t.text = "PRESS A KEY  (ESC CANCELS)";
				t.color = UiKit.Gold;
			});
			_rows.Add(b);
		}

		private static string KeyName(Key k)
		{
			if (k == Key.None) return "UNBOUND";
			KeyControl c = Keyboard.current?[k];
			string n = c != null ? c.displayName : k.ToString();
			return string.IsNullOrEmpty(n) ? k.ToString().ToUpper() : n.ToUpper();
		}

		// ---------------------------------------------------------------- update

		private void Update()
		{
			if (_bindPlayer >= 0)
			{
				PollBinding();
				return;
			}
			bool back = false;
			foreach (InputDevice d in InputSystem.devices)
			{
				if (d is Keyboard k && k.escapeKey.wasPressedThisFrame) back = true;
				if (d is Gamepad g && (g.buttonEast.wasPressedThisFrame || g.startButton.wasPressedThisFrame)) back = true;
			}
			if (back)
			{
				Close();
				return;
			}
			// Keep the selected row visible.
			GameObject sel = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
			if (sel != null && sel.transform.IsChildOf(_content))
			{
				int idx = _rows.FindIndex(r => r != null && r.gameObject == sel);
				if (idx >= 0 && _rows.Count > 1)
				{
					float target = 1f - idx / (float)(_rows.Count - 1);
					_scroll.verticalNormalizedPosition = Mathf.Lerp(_scroll.verticalNormalizedPosition, target, 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
				}
			}
		}

		private void PollBinding()
		{
			if (Time.unscaledTime - _bindStarted < 0.15f) return;
			foreach (InputDevice d in InputSystem.devices)
			{
				if (!(d is Keyboard k)) continue;
				foreach (KeyControl key in k.allKeys)
				{
					if (key == null || !key.wasPressedThisFrame) continue;
					Key[] keys = Cfg.players[_bindPlayer].keys;
					if (key.keyCode != Key.Escape)
					{
						// Swap if this player already uses the key for another action.
						int existing = Array.IndexOf(keys, key.keyCode);
						if (existing >= 0) keys[existing] = keys[(int)_bindAction];
						keys[(int)_bindAction] = key.keyCode;
						Cfg.Commit();
					}
					_bindPlayer = -1;
					ShowTab(3);
					return;
				}
			}
		}

		public void Close()
		{
			_onClose?.Invoke();
			Destroy(gameObject);
		}
	}
}
