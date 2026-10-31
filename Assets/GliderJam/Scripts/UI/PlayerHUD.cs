using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GliderJam.UI
{
	// One player's HUD, laid out inside that player's viewport (split-screen aware).
	public class PlayerHUD : MonoBehaviour
	{
		private static Sprite _circle, _ring, _triangle, _diamond, _pill;

		private RectTransform _root;
		private Camera _cam;
		private Color _color;
		private TextMeshProUGUI _position, _positionOf, _cp, _timer, _speed, _message, _sub, _otherLabel, _nextLabel;
		private Image _boostFill, _otherMarker, _otherArrow, _nextMarker, _nextArrow;
		private RectTransform _otherGroup, _nextGroup;
		private float _messageUntil;

		public void Init(Canvas canvas, Camera cam, int player, int playerCount, Rect viewport)
		{
			BuildSprites();
			_cam = cam;
			_color = GameSettings.PlayerColors[player];
			_root = New("HUD P" + (player + 1), canvas.transform);
			_root.anchorMin = viewport.min;
			_root.anchorMax = viewport.max;
			_root.offsetMin = _root.offsetMax = Vector2.zero;
			float s = playerCount > 1 ? 0.8f : 1f; // tighter layout per half screen

			// Top-left: position and checkpoints
			RectTransform tl = Panel(_root, new Vector2(0f, 1f), new Vector2(28f, -24f), new Vector2(250f, 120f) * s);
			_position = Text(tl, "1ST", 64 * s, new Vector2(18f * s, -6f * s), TextAlignmentOptions.TopLeft, Color.white, FontStyles.Bold | FontStyles.Italic);
			_positionOf = Text(tl, playerCount > 1 ? "/ 2" : "SOLO", 24 * s, new Vector2(150f * s, -40f * s), TextAlignmentOptions.TopLeft, new Color(1f, 1f, 1f, 0.6f), FontStyles.Bold);
			_cp = Text(tl, "CP 0/0", 24 * s, new Vector2(18f * s, -80f * s), TextAlignmentOptions.TopLeft, new Color(1f, 1f, 1f, 0.85f), FontStyles.Bold);
			Accent(tl, _color);

			// Top-right: timer
			RectTransform tr = Panel(_root, new Vector2(1f, 1f), new Vector2(-28f, -24f), new Vector2(260f, 84f) * s);
			Text(tr, "TIME", 18 * s, new Vector2(18f * s, -8f * s), TextAlignmentOptions.TopLeft, new Color(1f, 1f, 1f, 0.6f), FontStyles.Bold);
			_timer = Text(tr, "00:00.00", 44 * s, new Vector2(18f * s, -26f * s), TextAlignmentOptions.TopLeft, Color.white, FontStyles.Bold);
			Accent(tr, _color);

			// Bottom-left: speed and boost
			RectTransform bl = Panel(_root, new Vector2(0f, 0f), new Vector2(28f, 24f), new Vector2(300f, 92f) * s);
			_speed = Text(bl, "0", 52 * s, new Vector2(18f * s, -6f * s), TextAlignmentOptions.TopLeft, Color.white, FontStyles.Bold | FontStyles.Italic);
			Text(bl, "KM/H", 20 * s, new Vector2(150f * s, -30f * s), TextAlignmentOptions.TopLeft, new Color(1f, 1f, 1f, 0.6f), FontStyles.Bold);
			RectTransform barBg = New("BoostBg", bl);
			barBg.anchorMin = barBg.anchorMax = new Vector2(0f, 1f);
			barBg.pivot = new Vector2(0f, 1f);
			barBg.anchoredPosition = new Vector2(18f * s, -70f * s);
			barBg.sizeDelta = new Vector2(264f * s, 10f * s);
			barBg.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);
			RectTransform fill = New("BoostFill", barBg);
			fill.anchorMin = Vector2.zero;
			fill.anchorMax = Vector2.one;
			fill.offsetMin = fill.offsetMax = Vector2.zero;
			_boostFill = fill.gameObject.AddComponent<Image>();
			_boostFill.color = _color;
			_boostFill.type = Image.Type.Filled;
			_boostFill.fillMethod = Image.FillMethod.Horizontal;
			_boostFill.sprite = _pill;
			Accent(bl, _color);

			// Player tag (split-screen only)
			if (playerCount > 1)
			{
				RectTransform tag = Panel(_root, new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(170f, 40f) * s);
				tag.pivot = new Vector2(0.5f, 1f);
				tag.GetComponent<Image>().color = new Color(_color.r, _color.g, _color.b, 0.85f);
				Text(tag, GameSettings.PlayerNames[player], 22 * s, new Vector2(0f, -7f * s), TextAlignmentOptions.Top, Color.white, FontStyles.Bold).rectTransform.sizeDelta = new Vector2(170f * s, 30f);
			}

			// Center messages
			_message = Text(_root, "", 110 * s, Vector2.zero, TextAlignmentOptions.Center, Color.white, FontStyles.Bold | FontStyles.Italic);
			Center(_message.rectTransform, new Vector2(0f, 60f * s), new Vector2(1400f, 160f));
			_sub = Text(_root, "", 30 * s, Vector2.zero, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.9f), FontStyles.Bold);
			Center(_sub.rectTransform, new Vector2(0f, -30f * s), new Vector2(1400f, 50f));

			// Out-of-bounds warning (above the ceiling)
			_oob = New("OutOfBounds", _root);
			_oob.anchorMin = Vector2.zero;
			_oob.anchorMax = Vector2.one;
			_oob.offsetMin = _oob.offsetMax = Vector2.zero;
			_oobVignette = _oob.gameObject.AddComponent<Image>();
			_oobVignette.sprite = UiKit.Vignette;
			_oobVignette.color = new Color(1f, 0.08f, 0.05f, 0.7f);
			_oobVignette.raycastTarget = false;
			_oobTitle = Text(_oob, "PULL DOWN", 64 * s, Vector2.zero, TextAlignmentOptions.Center, new Color(1f, 0.35f, 0.3f), FontStyles.Bold | FontStyles.Italic);
			Center(_oobTitle.rectTransform, new Vector2(0f, 150f * s), new Vector2(1400f, 90f));
			_oobSub = Text(_oob, "RETURN BELOW THE CLOUDS", 26 * s, Vector2.zero, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
			Center(_oobSub.rectTransform, new Vector2(0f, 95f * s), new Vector2(1400f, 40f));
			_oobCount = Text(_oob, "", 120 * s, Vector2.zero, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
			Center(_oobCount.rectTransform, new Vector2(0f, -40f * s), new Vector2(600f, 160f));
			_oob.gameObject.SetActive(false);

			// Markers
			// The other player's marker and arrow use the other player's color.
			Color other = GameSettings.PlayerColors[1 - Mathf.Clamp(player, 0, 1)];
			_otherGroup = New("OtherMarker", _root);
			_otherMarker = Icon(_otherGroup, _diamond, 26f, other);
			_otherLabel = Text(_otherGroup, "", 20, new Vector2(0f, 34f), TextAlignmentOptions.Bottom, other, FontStyles.Bold);
			_otherLabel.rectTransform.anchorMin = _otherLabel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
			_otherLabel.rectTransform.pivot = new Vector2(0.5f, 0f);
			_otherLabel.rectTransform.anchoredPosition = new Vector2(0f, 16f);
			_otherLabel.outlineWidth = 0.25f;
			_otherLabel.outlineColor = new Color32(0, 0, 0, 200);
			_otherArrow = Icon(_root, _triangle, 34f, other);
			_nextGroup = New("NextCheckpoint", _root);
			_nextMarker = Icon(_nextGroup, _ring, 34f, new Color(1f, 0.9f, 0.7f, 0.9f));
			_nextLabel = Text(_nextGroup, "", 18, Vector2.zero, TextAlignmentOptions.Top, new Color(1f, 0.95f, 0.85f), FontStyles.Bold);
			_nextLabel.rectTransform.anchorMin = _nextLabel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
			_nextLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
			_nextLabel.rectTransform.anchoredPosition = new Vector2(0f, -22f);
			_nextArrow = Icon(_root, _triangle, 26f, new Color(1f, 0.9f, 0.7f, 0.9f));
			_otherGroup.gameObject.SetActive(playerCount > 1);
			_otherArrow.gameObject.SetActive(false);
			_nextArrow.gameObject.SetActive(false);
		}

		// ---------------------------------------------------------------- minimap

		private RectTransform _map;
		private Image _mapMe, _mapOther, _mapNext;
		private Vector2 _mapMin;
		private float _mapScale;

		// North-up overview of the whole race line on the right edge: line and checkpoints drawn once into a
		// texture, live markers for this player (heading arrow), the other player and the next checkpoint.
		public void SetRoute(Race.RaceRoute route, int player, int playerCount)
		{
			float s = playerCount > 1 ? 0.8f : 1f;
			Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
			foreach (Vector3 p in route.path)
			{
				min = Vector2.Min(min, new Vector2(p.x, p.z));
				max = Vector2.Max(max, new Vector2(p.x, p.z));
			}
			var size = new Vector2(104f, 340f) * s;
			if (playerCount > 1) size.y = Mathf.Min(size.y, 260f);
			const float pad = 12f;
			_mapScale = Mathf.Min((size.x - 2f * pad) / Mathf.Max(1f, max.x - min.x), (size.y - 2f * pad) / Mathf.Max(1f, max.y - min.y));
			// Centre the route in the panel.
			Vector2 used = (max - min) * _mapScale;
			_mapMin = min - new Vector2((size.x - used.x) * 0.5f, (size.y - used.y) * 0.5f) / _mapScale;

			_map = Panel(_root, new Vector2(1f, 0.5f), new Vector2(-28f, 0f), size);
			_map.pivot = new Vector2(1f, 0.5f);
			_map.anchoredPosition = new Vector2(-28f, -20f * s);
			Accent(_map, _color);
			const int k = 3; // texture supersampling
			int tw = Mathf.CeilToInt(size.x * k), th = Mathf.CeilToInt(size.y * k);
			var px = new Color32[tw * th];
			Vector2 ToTex(Vector3 w) => (new Vector2(w.x, w.z) - _mapMin) * _mapScale * k;
			for (int i = 1; i < route.path.Length; i++) Line(px, tw, th, ToTex(route.path[i - 1]), ToTex(route.path[i]), 2.2f * k * s, new Color32(255, 255, 255, 120));
			for (int c = 0; c < route.CheckpointCount; c++)
			{
				bool finish = c == route.CheckpointCount - 1;
				Vector2 q = ToTex(route.CheckpointPosition(c));
				Line(px, tw, th, q, q, (finish ? 5f : 3.2f) * k * s, finish ? new Color32(255, 196, 80, 255) : new Color32(255, 235, 200, 200));
			}
			var tex = new Texture2D(tw, th, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
			tex.SetPixels32(px);
			tex.Apply(false, true);
			RectTransform img = New("Route", _map);
			img.anchorMin = Vector2.zero;
			img.anchorMax = Vector2.one;
			img.offsetMin = img.offsetMax = Vector2.zero;
			img.gameObject.AddComponent<RawImage>().texture = tex;

			_mapNext = Icon(_map, _ring, 14f * s, new Color(1f, 0.9f, 0.7f));
			if (playerCount > 1) _mapOther = Icon(_map, _circle, 11f * s, GameSettings.PlayerColors[1 - Mathf.Clamp(player, 0, 1)]);
			_mapMe = Icon(_map, _triangle, 18f * s, _color);
			foreach (Image i in new[] { _mapNext, _mapOther, _mapMe })
			{
				if (i == null) continue;
				i.rectTransform.anchorMin = i.rectTransform.anchorMax = Vector2.zero;
			}
		}

		public void UpdateMap(Vector3 me, float yaw, Vector3 other, bool showOther, Vector3 next)
		{
			if (_map == null) return;
			_mapMe.rectTransform.anchoredPosition = (new Vector2(me.x, me.z) - _mapMin) * _mapScale;
			_mapMe.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -yaw);
			_mapNext.rectTransform.anchoredPosition = (new Vector2(next.x, next.z) - _mapMin) * _mapScale;
			if (_mapOther != null)
			{
				_mapOther.enabled = showOther;
				_mapOther.rectTransform.anchoredPosition = (new Vector2(other.x, other.z) - _mapMin) * _mapScale;
			}
		}

		// Anti-aliased thick line (round caps) blended into a pixel buffer.
		private static void Line(Color32[] px, int w, int h, Vector2 a, Vector2 b, float radius, Color32 c)
		{
			int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x) - radius - 1)), x1 = Mathf.Min(w - 1, Mathf.CeilToInt(Mathf.Max(a.x, b.x) + radius + 1));
			int y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y) - radius - 1)), y1 = Mathf.Min(h - 1, Mathf.CeilToInt(Mathf.Max(a.y, b.y) + radius + 1));
			Vector2 ab = b - a;
			float len2 = Mathf.Max(1e-4f, ab.sqrMagnitude);
			for (int y = y0; y <= y1; y++)
			{
				for (int x = x0; x <= x1; x++)
				{
					var p = new Vector2(x + 0.5f, y + 0.5f);
					float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
					float d = (p - (a + ab * t)).magnitude;
					float cov = Mathf.Clamp01(radius - d + 0.5f) * c.a / 255f;
					if (cov <= 0f) continue;
					Color32 o = px[y * w + x];
					float oa = o.a / 255f, na = cov + oa * (1f - cov);
					if (na <= 0f) continue;
					px[y * w + x] = new Color32(
						(byte)((c.r * cov + o.r * oa * (1f - cov)) / na), (byte)((c.g * cov + o.g * oa * (1f - cov)) / na),
						(byte)((c.b * cov + o.b * oa * (1f - cov)) / na), (byte)(na * 255f));
				}
			}
		}

		public void SetStats(int rank, int cp, int cpTotal, float time, float speedMs, float boost)
		{
			string[] suffix = { "ST", "ND", "RD", "TH" };
			_position.text = rank + "<size=55%>" + suffix[Mathf.Clamp(rank - 1, 0, 3)] + "</size>";
			_cp.text = "CP " + cp + "/" + cpTotal;
			_timer.text = GameSettings.FormatTime(Mathf.Max(0f, time)).Replace("--:--.--", "00:00.00");
			_speed.text = Mathf.RoundToInt(speedMs * 3.6f).ToString();
			_boostFill.fillAmount = boost;
		}

		private RectTransform _oob;
		private Image _oobVignette;
		private TextMeshProUGUI _oobTitle, _oobSub, _oobCount;

		public void SetOutOfBounds(bool on, float secondsLeft, float grace)
		{
			if (_oob == null) return;
			if (_oob.gameObject.activeSelf != on) _oob.gameObject.SetActive(on);
			if (!on) return;
			float urgency = 1f - Mathf.Clamp01(secondsLeft / Mathf.Max(0.01f, grace));
			float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * (10f + urgency * 14f));
			_oobVignette.color = new Color(1f, 0.08f, 0.05f, Mathf.Lerp(0.45f, 0.9f, urgency) * (0.75f + 0.25f * pulse));
			_oobCount.text = secondsLeft.ToString("0.0");
			_oobCount.color = Color.Lerp(Color.white, new Color(1f, 0.3f, 0.25f), urgency);
		}

		public void ShowMessage(string text, string sub, Color color, float seconds)
		{
			_message.text = text;
			_message.color = color;
			_sub.text = sub;
			_messageUntil = Time.time + seconds;
		}

		private void Update()
		{
			if (_message == null) return;
			float left = _messageUntil - Time.time;
			float a = Mathf.Clamp01(left * 3f);
			_message.alpha = a;
			_sub.alpha = a;
		}

		// Marker over the other glider, or an edge arrow when it's off this player's screen.
		public void UpdateOther(Vector3 world, string label, bool active)
		{
			Place(world + Vector3.up * 5f, active, _otherGroup, _otherArrow, label, _otherLabel);
		}

		public void UpdateNext(Vector3 world, string label, bool active)
		{
			Place(world, active, _nextGroup, _nextArrow, label, _nextLabel);
		}

		private void Place(Vector3 world, bool active, RectTransform group, Image arrow, string label, TextMeshProUGUI text)
		{
			if (!active)
			{
				group.gameObject.SetActive(false);
				arrow.gameObject.SetActive(false);
				return;
			}
			Rect px = _cam.pixelRect;
			Vector3 sp = _cam.WorldToScreenPoint(world);
			bool behind = sp.z < 0f;
			float margin = 40f;
			bool inside = !behind && sp.x > px.xMin + margin && sp.x < px.xMax - margin && sp.y > px.yMin + margin && sp.y < px.yMax - margin;
			group.gameObject.SetActive(inside);
			arrow.gameObject.SetActive(!inside);
			text.text = label;
			Vector2 center = px.center;
			Vector2 p = new Vector2(sp.x, sp.y);
			if (behind)
			{
				p = center - (p - center);
			}
			if (inside)
			{
				RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, p, null, out Vector2 local);
				group.anchoredPosition = local;
				return;
			}
			Vector2 dir = (p - center);
			if (dir.sqrMagnitude < 1f) dir = Vector2.down;
			Vector2 half = new Vector2(px.width * 0.5f - margin * 1.5f, px.height * 0.5f - margin * 1.5f);
			float k = Mathf.Min(half.x / Mathf.Max(0.001f, Mathf.Abs(dir.x)), half.y / Mathf.Max(0.001f, Mathf.Abs(dir.y)));
			Vector2 edge = center + dir * k;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, edge, null, out Vector2 l2);
			arrow.rectTransform.anchoredPosition = l2;
			arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
		}

		// ---------------------------------------------------------------- building blocks

		private static RectTransform New(string name, Transform parent)
		{
			var go = new GameObject(name, typeof(RectTransform));
			go.transform.SetParent(parent, false);
			var rt = (RectTransform)go.transform;
			rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
			return rt;
		}

		private static RectTransform Panel(RectTransform parent, Vector2 anchor, Vector2 pos, Vector2 size)
		{
			RectTransform rt = New("Panel", parent);
			rt.anchorMin = rt.anchorMax = anchor;
			rt.pivot = anchor;
			rt.anchoredPosition = pos;
			rt.sizeDelta = size;
			var img = rt.gameObject.AddComponent<Image>();
			img.sprite = _pill;
			img.type = Image.Type.Sliced;
			img.color = new Color(0.02f, 0.03f, 0.05f, 0.55f);
			return rt;
		}

		private static void Accent(RectTransform panel, Color c)
		{
			RectTransform a = New("Accent", panel);
			a.anchorMin = new Vector2(0f, 0f);
			a.anchorMax = new Vector2(0f, 1f);
			a.pivot = new Vector2(0f, 0.5f);
			a.sizeDelta = new Vector2(5f, -16f);
			a.anchoredPosition = new Vector2(6f, 0f);
			a.gameObject.AddComponent<Image>().color = c;
		}

		private static TextMeshProUGUI Text(RectTransform parent, string text, float size, Vector2 pos, TextAlignmentOptions align, Color color, FontStyles style)
		{
			RectTransform rt = New("Text", parent);
			rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
			rt.pivot = new Vector2(0f, 1f);
			rt.anchoredPosition = pos;
			rt.sizeDelta = new Vector2(600f, size * 1.3f);
			var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
			t.font = UiKit.Font;
			t.text = text;
			t.fontSize = size;
			t.color = color;
			t.alignment = align;
			t.fontStyle = style;
			t.textWrappingMode = TextWrappingModes.NoWrap;
			t.raycastTarget = false;
			return t;
		}

		private static void Center(RectTransform rt, Vector2 pos, Vector2 size)
		{
			rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
			rt.pivot = new Vector2(0.5f, 0.5f);
			rt.anchoredPosition = pos;
			rt.sizeDelta = size;
		}

		private static Image Icon(RectTransform parent, Sprite sprite, float size, Color c)
		{
			RectTransform rt = New("Icon", parent);
			rt.sizeDelta = new Vector2(size, size);
			var img = rt.gameObject.AddComponent<Image>();
			img.sprite = sprite;
			img.color = c;
			img.raycastTarget = false;
			return img;
		}

		private static void BuildSprites()
		{
			if (_circle != null) return;
			_circle = MakeSprite(64, (x, y) => Mathf.Clamp01((0.5f - new Vector2(x - 0.5f, y - 0.5f).magnitude) * 64f));
			_ring = MakeSprite(64, (x, y) => { float d = new Vector2(x - 0.5f, y - 0.5f).magnitude; return Mathf.Clamp01((0.06f - Mathf.Abs(d - 0.4f)) * 64f); });
			_triangle = MakeSprite(64, (x, y) => { float w = (1f - y) * 0.5f; return Mathf.Clamp01((w - Mathf.Abs(x - 0.5f)) * 64f) * Mathf.Clamp01((y - 0.05f) * 64f) * Mathf.Clamp01((0.95f - y) * 64f); });
			_diamond = MakeSprite(64, (x, y) => Mathf.Clamp01((0.45f - (Mathf.Abs(x - 0.5f) + Mathf.Abs(y - 0.5f))) * 64f));
			_pill = MakeSprite(32, (x, y) =>
			{
				float r = 0.3f;
				float dx = Mathf.Max(Mathf.Abs(x - 0.5f) - (0.5f - r), 0f), dy = Mathf.Max(Mathf.Abs(y - 0.5f) - (0.5f - r), 0f);
				return Mathf.Clamp01((r - Mathf.Sqrt(dx * dx + dy * dy)) * 32f);
			}, new Vector4(12, 12, 12, 12));
		}

		private static Sprite MakeSprite(int n, System.Func<float, float, float> alpha, Vector4 border = default)
		{
			var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
			var px = new Color[n * n];
			for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) px[y * n + x] = new Color(1f, 1f, 1f, alpha((x + 0.5f) / n, (y + 0.5f) / n));
			t.SetPixels(px);
			t.Apply();
			t.wrapMode = TextureWrapMode.Clamp;
			return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
		}
	}
}
