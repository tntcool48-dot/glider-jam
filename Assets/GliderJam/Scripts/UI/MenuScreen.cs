using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GliderJam.UI
{
	// Shared look for the front-end screens (built at runtime so both scenes stay in sync).
	public abstract class MenuScreen : MonoBehaviour
	{
		public Texture2D background;
		protected RectTransform Root;
		protected RawImage BackgroundImage;
		private Button _first;

		protected virtual void Start()
		{
			Time.timeScale = 1f;
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
			var canvasGo = new GameObject("Menu Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
			var canvas = canvasGo.GetComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			var scaler = canvasGo.GetComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1920f, 1080f);
			scaler.matchWidthOrHeight = 1f;
			Root = (RectTransform)canvasGo.transform;
			if (background != null)
			{
				var bg = Full("Background");
				var raw = bg.gameObject.AddComponent<RawImage>();
				raw.texture = background;
				raw.uvRect = FitCover(background);
				BackgroundImage = raw;
			}
			// Left-side darkening gradient for legibility.
			var shade = Full("Shade");
			var shadeImg = shade.gameObject.AddComponent<RawImage>();
			shadeImg.texture = Gradient();
			Build();
			if (_first != null) EventSystem.current?.SetSelectedGameObject(_first.gameObject);
		}

		protected abstract void Build();

		protected Rect FitCover(Texture t)
		{
			float screen = 16f / 9f, img = (float)t.width / t.height;
			return img > screen ? new Rect((1f - screen / img) * 0.5f, 0f, screen / img, 1f) : new Rect(0f, (1f - img / screen) * 0.5f, 1f, img / screen);
		}

		private static Texture2D Gradient()
		{
			var t = new Texture2D(256, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
			for (int x = 0; x < 256; x++)
			{
				float a = Mathf.Lerp(0.82f, 0.05f, Mathf.SmoothStep(0f, 1f, x / 255f / 0.75f));
				t.SetPixel(x, 0, new Color(0.02f, 0.025f, 0.04f, a));
			}
			t.Apply();
			return t;
		}

		protected RectTransform Full(string name)
		{
			var go = new GameObject(name, typeof(RectTransform));
			go.transform.SetParent(Root, false);
			var rt = (RectTransform)go.transform;
			rt.anchorMin = Vector2.zero;
			rt.anchorMax = Vector2.one;
			rt.offsetMin = rt.offsetMax = Vector2.zero;
			return rt;
		}

		protected TextMeshProUGUI Label(string text, float size, Vector2 pos, Color color, FontStyles style = FontStyles.Bold, float width = 1100f)
		{
			var go = new GameObject("Label", typeof(RectTransform));
			go.transform.SetParent(Root, false);
			var rt = (RectTransform)go.transform;
			rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
			rt.anchoredPosition = pos;
			rt.sizeDelta = new Vector2(width, size * 1.25f);
			var t = go.AddComponent<TextMeshProUGUI>();
			t.font = UiKit.Font;
			t.text = text;
			t.fontSize = size;
			t.color = color;
			t.fontStyle = style;
			t.textWrappingMode = TextWrappingModes.NoWrap;
			t.raycastTarget = false;
			return t;
		}

		protected Button MakeButton(string text, Vector2 pos, Color accent, UnityEngine.Events.UnityAction onClick)
		{
			var go = new GameObject(text + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
			go.transform.SetParent(Root, false);
			var rt = (RectTransform)go.transform;
			rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
			rt.anchoredPosition = pos;
			rt.sizeDelta = new Vector2(640f, 82f);
			var img = go.GetComponent<Image>();
			img.color = Color.white;
			var b = go.GetComponent<Button>();
			var colors = b.colors;
			colors.normalColor = new Color(0.05f, 0.06f, 0.09f, 0.72f);
			colors.highlightedColor = new Color(accent.r * 0.85f, accent.g * 0.85f, accent.b * 0.85f, 0.95f);
			colors.selectedColor = colors.highlightedColor;
			colors.pressedColor = accent;
			colors.fadeDuration = 0.08f;
			b.colors = colors;
			b.onClick.AddListener(onClick);
			var bar = new GameObject("Accent", typeof(RectTransform), typeof(Image));
			bar.transform.SetParent(rt, false);
			var brt = (RectTransform)bar.transform;
			brt.anchorMin = new Vector2(0f, 0f);
			brt.anchorMax = new Vector2(0f, 1f);
			brt.pivot = new Vector2(0f, 0.5f);
			brt.sizeDelta = new Vector2(8f, 0f);
			bar.GetComponent<Image>().color = accent;
			var label = new GameObject("Text", typeof(RectTransform));
			label.transform.SetParent(rt, false);
			var lrt = (RectTransform)label.transform;
			lrt.anchorMin = Vector2.zero;
			lrt.anchorMax = Vector2.one;
			lrt.offsetMin = new Vector2(34f, 0f);
			lrt.offsetMax = Vector2.zero;
			var t = label.AddComponent<TextMeshProUGUI>();
			t.text = text;
			t.fontSize = 36f;
			t.fontStyle = FontStyles.Bold | FontStyles.Italic;
			t.alignment = TextAlignmentOptions.MidlineLeft;
			t.textWrappingMode = TextWrappingModes.NoWrap;
			t.color = Color.white;
			if (_first == null) _first = b;
			return b;
		}

		protected static void Load(string scene)
		{
			SceneManager.LoadScene(scene);
		}

		protected static void Quit()
		{
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#else
			Application.Quit();
#endif
		}
	}
}
