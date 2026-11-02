using TMPro;
using UnityEngine;

namespace GliderJam.UI
{
	// Optional frame-rate readout in the top-left corner of the screen (Settings > Display > FPS COUNTER), above every
	// other UI, in menus and races. Averages over a quarter second; green at 60+, yellow at 30+, red below.
	public class FpsCounter : MonoBehaviour
	{
		private GameObject _canvas;
		private TextMeshProUGUI _text;
		private float _time;
		private int _frames;

		private void Start()
		{
			_canvas = new GameObject("FPS Counter", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
			_canvas.transform.SetParent(transform, false);
			var canvas = _canvas.GetComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 1000;
			var scaler = _canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
			scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1920f, 1080f);
			scaler.matchWidthOrHeight = 1f;
			_text = UiKit.Text(_canvas.transform, "", 18f, Color.white, TextAlignmentOptions.TopLeft, FontStyles.Bold);
			RectTransform rt = _text.rectTransform;
			rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
			rt.anchoredPosition = new Vector2(8f, -2f);
			rt.sizeDelta = new Vector2(240f, 24f);
			_text.raycastTarget = false;
			_text.outlineWidth = 0.2f;
			_text.outlineColor = new Color32(0, 0, 0, 220);
		}

		private void Update()
		{
			bool on = GameConfig.Current.showFps;
			if (_canvas.activeSelf != on) _canvas.SetActive(on);
			if (!on) return;
			_frames++;
			_time += Time.unscaledDeltaTime;
			if (_time < 0.25f) return;
			float fps = _frames / _time;
			float scale = AutoResolution.Scale(GameConfig.Current);
			_text.text = Mathf.RoundToInt(fps) + " FPS  <size=75%>" + (1000f / fps).ToString("0.0") + " ms" + (scale < 99.5f ? "  " + Mathf.RoundToInt(scale) + "% RES" : "") + "</size>";
			_text.color = fps >= 60f ? new Color(0.45f, 1f, 0.5f) : fps >= 30f ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.35f, 0.3f);
			_frames = 0;
			_time = 0f;
		}
	}
}
