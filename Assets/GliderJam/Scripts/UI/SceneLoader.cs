using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GliderJam.UI
{
	// Loading screen: a full-screen card (the map's picture, its name, a progress bar and a tip) that stays up while the
	// next scene loads in the background and through its first heavy frames (terrain build, shader warm-up), then
	// fades out. Replaces SceneManager.LoadScene, which froze the game with no feedback.
	public class SceneLoader : MonoBehaviour
	{
		private static readonly string[] Tips =
		{
			"THREAD THE LIT GATE TUNNELS FOR A FREE SPEED BURST",
			"SKIM WALLS AND FLY KNIFE-EDGE TO REFILL BOOST",
			"RIDE THE OTHER GLIDER'S SLIPSTREAM TO REFILL BOOST",
			"FLY UNDER THE SKYBRIDGES FOR EXTRA BOOST",
			"STAY BELOW THE CEILING: ABOVE IT YOU HAVE TWO SECONDS",
			"NO CHECKPOINTS: ANY ROUTE TO THE FINISH COUNTS"
		};

		public static bool Busy { get; private set; }

		private string _scene;
		private CanvasGroup _group;
		private Image _bar;

		public static void Load(string scene)
		{
			if (Busy) return;
			Busy = true;
			var go = new GameObject("Loading Screen");
			DontDestroyOnLoad(go);
			var loader = go.AddComponent<SceneLoader>();
			loader._scene = scene;
			loader.Build();
			loader.StartCoroutine(loader.Run());
		}

		private void Build()
		{
			var canvas = gameObject.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 5000;
			var scaler = gameObject.AddComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1920f, 1080f);
			scaler.matchWidthOrHeight = 1f;
			gameObject.AddComponent<GraphicRaycaster>(); // swallows clicks while loading
			_group = gameObject.AddComponent<CanvasGroup>();
			Transform root = transform;

			Image black = UiKit.Panel(root, "Black", Color.black, false);
			UiKit.Stretch(black.rectTransform);
			bool city = _scene == GameSettings.CityScene, race = city || _scene == GameSettings.RaceScene;
			Texture2D pic = race ? Resources.Load<Texture2D>(city ? "CityBackground" : "CanyonBackground") : null;
			if (pic != null)
			{
				var img = UiKit.Rect("Picture", root).gameObject.AddComponent<RawImage>();
				UiKit.Stretch(img.rectTransform);
				img.texture = pic;
				img.color = new Color(0.55f, 0.55f, 0.6f);
				float screen = 16f / 9f, aspect = (float)pic.width / pic.height;
				img.uvRect = aspect > screen ? new Rect((1f - screen / aspect) * 0.5f, 0f, screen / aspect, 1f) : new Rect(0f, (1f - aspect / screen) * 0.5f, 1f, aspect / screen);
			}
			string title = race ? (city ? "NEON CITY" : "GOLDEN PILLARS") : "";
			TextMeshProUGUI loading = UiKit.Text(root, "LOADING", 28f, UiKit.Gold, TextAlignmentOptions.BottomLeft);
			UiKit.Stretch(loading.rectTransform, 120f, 250f, 120f, 0f);
			if (title.Length > 0)
			{
				TextMeshProUGUI t = UiKit.Text(root, title, 96f, Color.white, TextAlignmentOptions.BottomLeft, FontStyles.Bold | FontStyles.Italic);
				UiKit.Stretch(t.rectTransform, 114f, 130f, 120f, 0f);
			}
			Image track = UiKit.Panel(root, "Bar", new Color(1f, 1f, 1f, 0.15f), false);
			track.rectTransform.anchorMin = new Vector2(0f, 0f);
			track.rectTransform.anchorMax = new Vector2(1f, 0f);
			track.rectTransform.offsetMin = new Vector2(120f, 96f);
			track.rectTransform.offsetMax = new Vector2(-120f, 104f);
			_bar = UiKit.Panel(track.transform, "Fill", UiKit.Gold, false);
			_bar.rectTransform.anchorMin = Vector2.zero;
			_bar.rectTransform.anchorMax = new Vector2(0f, 1f);
			_bar.rectTransform.offsetMin = _bar.rectTransform.offsetMax = Vector2.zero;
			if (race)
			{
				TextMeshProUGUI tip = UiKit.Text(root, "TIP   " + Tips[Random.Range(0, Tips.Length)], 22f, new Color(1f, 1f, 1f, 0.75f), TextAlignmentOptions.BottomLeft);
				UiKit.Stretch(tip.rectTransform, 120f, 40f, 120f, 0f);
			}
		}

		private void SetProgress(float p)
		{
			_bar.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(p), 1f);
		}

		private IEnumerator Run()
		{
			yield return null; // show the card before any heavy work starts
			yield return null;
			Application.backgroundLoadingPriority = ThreadPriority.High; // nothing to keep smooth behind the card: load fast
			AsyncOperation op = SceneManager.LoadSceneAsync(_scene);
			op.allowSceneActivation = false;
			while (op.progress < 0.9f)
			{
				SetProgress(op.progress / 0.9f * 0.8f);
				yield return null;
			}
			SetProgress(0.85f);
			yield return null;
			op.allowSceneActivation = true;
			while (!op.isDone) yield return null;
			// The first frames of a race scene are the heavy ones; keep the card up through them.
			SetProgress(1f);
			Application.backgroundLoadingPriority = ThreadPriority.BelowNormal;
			for (int i = 0; i < 4; i++) yield return null;
			yield return new WaitForSecondsRealtime(0.25f);
			for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.3f)
			{
				_group.alpha = 1f - t;
				yield return null;
			}
			Busy = false;
			Destroy(gameObject);
		}
	}
}
