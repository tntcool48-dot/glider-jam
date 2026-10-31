using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GliderJam.UI
{
	// Shared look for every runtime-built UI: font, palette, panels, buttons and generated sprites.
	public static class UiKit
	{
		public static readonly Color Ink = new Color(0.02f, 0.025f, 0.04f, 0.82f);
		public static readonly Color InkLight = new Color(0.06f, 0.07f, 0.1f, 0.78f);
		public static readonly Color Gold = new Color(1f, 0.8f, 0.48f);
		public static readonly Color Muted = new Color(1f, 1f, 1f, 0.62f);

		private static TMP_FontAsset _font;
		private static Sprite _rounded, _vignette, _triangle, _circle, _ring, _diamond;

		public static TMP_FontAsset Font => _font != null ? _font : (_font = Resources.Load<TMP_FontAsset>("GliderJamFont") ?? TMP_Settings.defaultFontAsset);

		public static Sprite Rounded => _rounded ??= MakeSprite(32, (x, y) =>
		{
			float r = 0.3f;
			float dx = Mathf.Max(Mathf.Abs(x - 0.5f) - (0.5f - r), 0f), dy = Mathf.Max(Mathf.Abs(y - 0.5f) - (0.5f - r), 0f);
			return Mathf.Clamp01((r - Mathf.Sqrt(dx * dx + dy * dy)) * 32f);
		}, new Vector4(12, 12, 12, 12));
		public static Sprite Vignette => _vignette ??= MakeSprite(128, (x, y) =>
		{
			float d = Mathf.Max(Mathf.Abs(x - 0.5f) * 1.15f, Mathf.Abs(y - 0.5f)) * 2f;
			return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d - 0.55f) / 0.45f));
		});
		public static Sprite Triangle => _triangle ??= MakeSprite(64, (x, y) => { float w = (1f - y) * 0.5f; return Mathf.Clamp01((w - Mathf.Abs(x - 0.5f)) * 64f) * Mathf.Clamp01((y - 0.05f) * 64f) * Mathf.Clamp01((0.95f - y) * 64f); });
		public static Sprite Circle => _circle ??= MakeSprite(64, (x, y) => Mathf.Clamp01((0.5f - new Vector2(x - 0.5f, y - 0.5f).magnitude) * 64f));
		public static Sprite Ring => _ring ??= MakeSprite(64, (x, y) => { float d = new Vector2(x - 0.5f, y - 0.5f).magnitude; return Mathf.Clamp01((0.06f - Mathf.Abs(d - 0.4f)) * 64f); });
		public static Sprite Diamond => _diamond ??= MakeSprite(64, (x, y) => Mathf.Clamp01((0.45f - (Mathf.Abs(x - 0.5f) + Mathf.Abs(y - 0.5f))) * 64f));

		public static Canvas Canvas(string name, int order)
		{
			var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
			var c = go.GetComponent<Canvas>();
			c.renderMode = RenderMode.ScreenSpaceOverlay;
			c.sortingOrder = order;
			var s = go.GetComponent<CanvasScaler>();
			s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			s.referenceResolution = new Vector2(1920f, 1080f);
			s.matchWidthOrHeight = 1f;
			return c;
		}

		public static RectTransform Rect(string name, Transform parent)
		{
			var go = new GameObject(name, typeof(RectTransform));
			go.transform.SetParent(parent, false);
			return (RectTransform)go.transform;
		}

		public static RectTransform Stretch(RectTransform rt, float l = 0f, float b = 0f, float r = 0f, float t = 0f)
		{
			rt.anchorMin = Vector2.zero;
			rt.anchorMax = Vector2.one;
			rt.offsetMin = new Vector2(l, b);
			rt.offsetMax = new Vector2(-r, -t);
			return rt;
		}

		public static Image Panel(Transform parent, string name, Color color, bool rounded = true)
		{
			RectTransform rt = Rect(name, parent);
			var img = rt.gameObject.AddComponent<Image>();
			img.color = color;
			if (rounded)
			{
				img.sprite = Rounded;
				img.type = Image.Type.Sliced;
			}
			return img;
		}

		public static TextMeshProUGUI Text(Transform parent, string text, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft, FontStyles style = FontStyles.Bold)
		{
			RectTransform rt = Rect("Text", parent);
			var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
			t.font = Font;
			t.text = text;
			t.fontSize = size;
			t.color = color;
			t.alignment = align;
			t.fontStyle = style;
			t.textWrappingMode = TextWrappingModes.NoWrap;
			t.raycastTarget = false;
			return t;
		}

		public static Button Button(Transform parent, string label, Color accent, UnityAction onClick, float fontSize = 34f)
		{
			Image bg = Panel(parent, label + " Button", Color.white);
			var b = bg.gameObject.AddComponent<Button>();
			Style(b, accent);
			b.onClick.AddListener(() => { Audio.AudioSynth.Play(Audio.AudioSynth.Click, 0.5f); onClick(); });
			Image bar = Panel(bg.transform, "Accent", accent, false);
			RectTransform brt = bar.rectTransform;
			brt.anchorMin = new Vector2(0f, 0f);
			brt.anchorMax = new Vector2(0f, 1f);
			brt.pivot = new Vector2(0f, 0.5f);
			brt.sizeDelta = new Vector2(7f, -18f);
			brt.anchoredPosition = new Vector2(8f, 0f);
			TextMeshProUGUI t = Text(bg.transform, label, fontSize, Color.white, TextAlignmentOptions.MidlineLeft, FontStyles.Bold | FontStyles.Italic);
			Stretch(t.rectTransform, 32f);
			return b;
		}

		public static void Style(Selectable s, Color accent)
		{
			ColorBlock c = s.colors;
			c.normalColor = InkLight;
			c.highlightedColor = new Color(accent.r * 0.8f, accent.g * 0.8f, accent.b * 0.8f, 0.95f);
			c.selectedColor = c.highlightedColor;
			c.pressedColor = accent;
			c.disabledColor = new Color(0.1f, 0.1f, 0.12f, 0.4f);
			c.fadeDuration = 0.08f;
			s.colors = c;
		}

		public static void LinkVertical(System.Collections.Generic.IList<Selectable> items)
		{
			for (int i = 0; i < items.Count; i++)
			{
				Navigation n = items[i].navigation;
				n.mode = Navigation.Mode.Explicit;
				n.selectOnUp = i > 0 ? items[i - 1] : null;
				n.selectOnDown = i < items.Count - 1 ? items[i + 1] : null;
				if (!(items[i] is Slider))
				{
					n.selectOnLeft = null;
					n.selectOnRight = null;
				}
				items[i].navigation = n;
			}
		}

		public static Sprite MakeSprite(int n, System.Func<float, float, float> alpha, Vector4 border = default)
		{
			var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
			var px = new Color[n * n];
			for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) px[y * n + x] = new Color(1f, 1f, 1f, alpha((x + 0.5f) / n, (y + 0.5f) / n));
			t.SetPixels(px);
			t.Apply();
			return Sprite.Create(t, new UnityEngine.Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
		}
	}
}
