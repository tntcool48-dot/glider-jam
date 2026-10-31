using System.Collections.Generic;
using GliderJam.Audio;
using GliderJam.Flight;
using GliderJam.FX;
using GliderJam.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

namespace GliderJam.Race
{
	// Runs a race in Solo or Split-screen mode: spawns gliders, cameras and HUDs, countdown, checkpoints,
	// ranking, crashes and respawns at the last checkpoint, and the finish / result hand-off to the end scene.
	public class RaceManager : MonoBehaviour
	{
		public FxLibrary fx;
		public RaceRoute route;
		public float countdownSeconds = 3f;
		public float missedPenalty = 3f;
		public float respawnDelay = 2.2f;
		public float resultsDelay = 4.5f;
		[Tooltip("Debug / tests: let the autopilot fly these players.")]
		public bool[] autopilot = new bool[2];

		private class Player
		{
			public GliderController glider;
			public GliderVisuals visuals;
			public FollowCamera cam;
			public PlayerHUD hud;
			public int nextCp;
			public int pathHint;
			public float progress;
			public int missed;
			public bool finished;
			public float finishTime;
			public float respawnAt = -1f;
			public Vector3 prevPos;
			public Volume boostVolume;
			public float rumbleUntil;
			public float nextAlarm;
		}

		private readonly List<Player> _players = new List<Player>();
		private float _raceStart = -1f;
		private float _countdownEnd;
		private int _lastCountdown = -1;
		private int _winner = -1;
		private float _endAt = -1f;
		private readonly List<Transform> _rings = new List<Transform>();

		private bool _showcase;
		private readonly List<GliderController> _showcaseGliders = new List<GliderController>();

		// Layers 8 / 9 hold effects only one player's camera may see.
		public static int ViewLayer(int player) => 8 + Mathf.Clamp(player, 0, 1);

		public float RaceTime => _raceStart < 0f ? 0f : Time.time - _raceStart;
		public bool Running => _raceStart >= 0f;

		private void Start()
		{
			DisableLegacyObjects();
			if (GameSettings.Showcase)
			{
				StartShowcase();
				return;
			}
			int count = GameSettings.PlayerCount;
			bool solo = count == 1;
			var canvasGo = new GameObject("Race HUD", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
			var canvas = canvasGo.GetComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 10;
			var scaler = canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>();
			scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1920f, 1080f);
			scaler.matchWidthOrHeight = 1f;

			Vector3 start = route.path[0];
			Vector3 fwd = Vector3.ProjectOnPlane(route.path[3] - route.path[0], Vector3.up).normalized; // level start
			Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
			var cams = new List<FollowCamera>();
			for (int i = 0; i < count; i++)
			{
				var p = new Player();
				Vector3 pos = start + right * (solo ? 0f : (i == 0 ? -16f : 16f));
				GameObject go = Instantiate(fx.gliderPrefabs[i], pos, Quaternion.LookRotation(fwd));
				go.name = "Glider P" + (i + 1);
				p.glider = go.AddComponent<GliderController>();
				p.glider.PlayerIndex = i;
				p.glider.ceiling = route.ceiling;
				p.glider.OnCrash += HandleCrash;
				p.visuals = go.AddComponent<GliderVisuals>();
				p.visuals.trailMaterial = fx.trail;
				p.visuals.vaporMaterial = fx.vapor;
				p.visuals.playerColor = GameSettings.PlayerColors[i];
				if ((i < autopilot.Length && autopilot[i]) || Benchmark.Active)
				{
					var ap = go.AddComponent<AutopilotDriver>();
					ap.route = route;
					ap.lateralOffset = solo ? 0f : (i == 0 ? -10f : 10f);
				}
				else
				{
					var d = go.AddComponent<PlayerDriver>();
					d.player = i;
					d.solo = solo;
				}
				var au = go.AddComponent<GliderAudio>();
				au.windClip = fx.windClip;
				au.masterVolume = solo ? 1f : 0.65f;

				// Camera with its own viewport: P1 top, P2 bottom.
				var camGo = new GameObject("Camera P" + (i + 1));
				camGo.layer = ViewLayer(i);
				var cam = camGo.AddComponent<Camera>();
				cam.nearClipPlane = 0.5f;
				cam.farClipPlane = 20000f;
				var hd = camGo.AddComponent<HDAdditionalCameraData>();
				hd.antialiasing = HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;
				Rect vp = solo ? new Rect(0f, 0f, 1f, 1f) : (i == 0 ? new Rect(0f, 0.5f, 1f, 0.5f) : new Rect(0f, 0f, 1f, 0.5f));
				cam.rect = vp;
				cam.cullingMask = ~(1 << ViewLayer(1 - i)); // never see the other player's private view effects
				hd.volumeLayerMask = 1 | (1 << ViewLayer(i));  // shared volumes + this player's boost effects
				p.boostVolume = MakeBoostVolume(i);
				if (i == 0) camGo.AddComponent<AudioListener>();
				p.cam = camGo.AddComponent<FollowCamera>();
				p.cam.target = p.glider;
				p.cam.Snap();
				cams.Add(p.cam);
				var streaks = camGo.AddComponent<SpeedStreaks>();
				streaks.glider = p.glider;
				streaks.material = fx.spark;

				p.hud = new GameObject("HUD P" + (i + 1)).AddComponent<PlayerHUD>();
				p.hud.Init(canvas, cam, i, count, vp);
				p.prevPos = pos;
				p.glider.Frozen = true;
				_players.Add(p);
			}
			// Gliders pass through each other.
			if (_players.Count == 2)
			{
				foreach (Collider a in _players[0].glider.GetComponentsInChildren<Collider>())
					foreach (Collider b in _players[1].glider.GetComponentsInChildren<Collider>())
						Physics.IgnoreCollision(a, b);
			}
			_cams = cams.ToArray();
			BuildRings();
			_countdownEnd = Time.time + countdownSeconds + 1f;
			_pause = new GameObject("Pause Menu").AddComponent<PauseMenu>();
		}

		private PauseMenu _pause;

		// Per-player chromatic aberration + lens distortion that swell while boosting (on the player's view layer).
		private static Volume MakeBoostVolume(int player)
		{
			var go = new GameObject("Boost FX P" + (player + 1)) { layer = ViewLayer(player) };
			var profile = ScriptableObject.CreateInstance<VolumeProfile>();
			var ca = profile.Add<ChromaticAberration>(true);
			ca.intensity.value = 0.55f;
			var ld = profile.Add<LensDistortion>(true);
			ld.intensity.value = -0.12f;
			var v = go.AddComponent<Volume>();
			v.isGlobal = true;
			v.priority = 20f;
			v.weight = 0f;
			v.sharedProfile = profile;
			return v;
		}

		private FollowCamera[] _cams;

		private void Update()
		{
			if (_showcase)
			{
				UpdateShowcase();
				return;
			}
			// Countdown
			if (_raceStart < 0f)
			{
				int n = Mathf.CeilToInt(_countdownEnd - Time.time - 0.0001f);
				if (n != _lastCountdown && n <= countdownSeconds && n > 0)
				{
					_lastCountdown = n;
					foreach (Player p in _players) p.hud.ShowMessage(n.ToString(), "", Color.white, 0.9f);
					AudioSynth.Play(AudioSynth.Beep, 0.7f);
				}
				if (Time.time >= _countdownEnd)
				{
					_raceStart = Time.time;
					foreach (Player p in _players)
					{
						p.glider.Frozen = false;
						p.hud.ShowMessage("GO!", "", new Color(1f, 0.85f, 0.4f), 1.1f);
					}
					AudioSynth.Play(AudioSynth.Go, 0.8f);
				}
			}

			for (int i = 0; i < _players.Count; i++)
			{
				Player p = _players[i];
				Vector3 pos = p.glider.transform.position;
				if (Running && !p.finished && !p.glider.Crashed)
				{
					CheckCheckpoint(i, p, p.prevPos, pos);
				}
				p.prevPos = pos;
				if (p.respawnAt > 0f && Time.time >= p.respawnAt)
				{
					Respawn(p);
				}
				p.pathHint = route.ClosestIndex(pos, p.pathHint, 25);
				// Continuous progress: checkpoint count, then path sample, then position along the local tangent.
				int h = Mathf.Min(p.pathHint, route.path.Length - 2);
				Vector3 tangent = route.path[h + 1] - route.path[h];
				float along = Vector3.Dot(pos - route.path[h], tangent.normalized) / Mathf.Max(1f, tangent.magnitude);
				p.progress = p.finished ? 1e6f - p.finishTime : p.nextCp * 10000f + h + along;
				float speed = p.glider.Crashed ? 0f : p.glider.Speed;
				float time = p.finished ? p.finishTime : RaceTime + p.missed * missedPenalty;
				p.hud.SetStats(Rank(i), Mathf.Min(p.nextCp, route.CheckpointCount), route.CheckpointCount, time, speed, p.glider.BoostEnergy);

				int next = Mathf.Min(p.nextCp, route.CheckpointCount - 1);
				Vector3 cp = route.CheckpointPosition(next);
				bool showNext = !p.finished;
				p.hud.UpdateNext(cp, Mathf.RoundToInt(Vector3.Distance(pos, cp)) + " m", showNext);
				if (_players.Count == 2)
				{
					Player o = _players[1 - i];
					Vector3 op = o.glider.transform.position;
					string label = "P" + (2 - i) + "  " + Mathf.RoundToInt(Vector3.Distance(pos, op)) + " m";
					p.hud.UpdateOther(op, label, !o.glider.Crashed);
				}

				// Above the ceiling: warning + alarm, countdown to destruction.
				bool oob = p.glider.OutOfBounds && !p.glider.Crashed;
				p.hud.SetOutOfBounds(oob, p.glider.OutOfBoundsLeft, p.glider.ceilingGrace);
				if (oob && Time.time >= p.nextAlarm)
				{
					p.nextAlarm = Time.time + 0.3f;
					AudioSynth.Play(AudioSynth.Beep, 0.45f, 1.7f);
					Rumble(i, p, 0.15f, 0.45f, 0.12f);
				}

				// Boost: lens effects swell and the pad hums.
				float boostTarget = p.glider.Boosting && GameConfig.Current.chromaticAberration ? 1f : 0f;
				p.boostVolume.weight = Mathf.MoveTowards(p.boostVolume.weight, boostTarget, Time.deltaTime * 3f);
				if (p.glider.Boosting) Rumble(i, p, 0.05f, 0.12f, 0.1f);
				if (p.rumbleUntil > 0f && Time.unscaledTime > p.rumbleUntil)
				{
					p.rumbleUntil = 0f;
					GliderInput.Rumble(i, _players.Count == 1, 0f, 0f);
				}
			}
			UpdateRings();

			if (_endAt > 0f && Time.time >= _endAt)
			{
				FinishRace();
			}
		}

		private void Rumble(int i, Player p, float low, float high, float seconds)
		{
			GliderInput.Rumble(i, _players.Count == 1, low, high);
			p.rumbleUntil = Mathf.Max(p.rumbleUntil, Time.unscaledTime + seconds);
		}

		private void OnDestroy()
		{
			UnityEngine.InputSystem.InputSystem.ResetHaptics();
		}

		private int Rank(int i)
		{
			int r = 1;
			for (int j = 0; j < _players.Count; j++)
			{
				if (j != i && _players[j].progress > _players[i].progress) r++;
			}
			return r;
		}

		// A checkpoint counts when the glider crosses the ring's plane inside the ring.
		// Crossing the plane outside the ring, or flying well past it, counts as missed (+penalty).
		private void CheckCheckpoint(int i, Player p, Vector3 a, Vector3 b)
		{
			if (p.nextCp >= route.CheckpointCount) return;
			Vector3 c = route.CheckpointPosition(p.nextCp);
			Vector3 f = route.CheckpointForward(p.nextCp);
			float s0 = Vector3.Dot(a - c, f), s1 = Vector3.Dot(b - c, f);
			bool last = p.nextCp == route.CheckpointCount - 1;
			bool crossed = s0 < 0f && s1 >= 0f;
			int cpPath = route.checkpoints[p.nextCp];
			if (crossed)
			{
				Vector3 hit = a + (b - a) * (s0 / (s0 - s1));
				float d = Vector3.Distance(hit, c);
				if (d <= route.Radius(p.nextCp))
				{
					Pass(i, p, last, false);
					return;
				}
				if (d <= route.Radius(p.nextCp) * 3f && !last)
				{
					Pass(i, p, false, true);
					return;
				}
			}
			if (!last && p.pathHint > cpPath + 6)
			{
				Pass(i, p, false, true);
			}
		}

		private void Pass(int i, Player p, bool finish, bool missed)
		{
			p.nextCp++;
			if (missed)
			{
				p.missed++;
				p.hud.ShowMessage("MISSED", "+" + missedPenalty.ToString("0") + " s penalty", new Color(1f, 0.45f, 0.35f), 1.2f);
				AudioSynth.Play(AudioSynth.Beep, 0.5f, 0.6f);
			}
			else if (!finish)
			{
				p.hud.ShowMessage("", "CHECKPOINT " + p.nextCp + "/" + route.CheckpointCount, new Color(1f, 0.9f, 0.7f), 1f);
				AudioSynth.Play(AudioSynth.Chime, 0.55f, i == 0 ? 1f : 1.12f);
				Rumble(i, p, 0.25f, 0.2f, 0.12f);
			}
			if (!finish) return;

			p.finished = true;
			AudioSynth.Play(AudioSynth.Go, 0.9f, 0.8f);
			p.finishTime = RaceTime + p.missed * missedPenalty;
			if (_winner < 0)
			{
				_winner = i;
				_endAt = Time.time + resultsDelay;
				if (_players.Count == 1)
				{
					p.hud.ShowMessage("FINISHED!", GameSettings.FormatTime(p.finishTime), GameSettings.PlayerColors[i], resultsDelay);
				}
				else
				{
					foreach (Player q in _players)
					{
						bool me = q == p;
						q.hud.ShowMessage(me ? "YOU WIN!" : GameSettings.PlayerNames[i] + " WINS", GameSettings.FormatTime(p.finishTime), GameSettings.PlayerColors[i], resultsDelay);
					}
				}
			}
			else
			{
				p.hud.ShowMessage("FINISHED 2ND", GameSettings.FormatTime(p.finishTime), GameSettings.PlayerColors[i], resultsDelay);
			}
			if (_players.TrueForAll(q => q.finished))
			{
				_endAt = Mathf.Min(_endAt, Time.time + 2.5f);
			}
		}

		private void HandleCrash(GliderController g, Vector3 point, Vector3 velocity)
		{
			Player p = _players.Find(x => x.glider == g);
			int i = _players.IndexOf(p);
			CrashFX.Spawn(fx, point, velocity, g.transform.Find("Visual"), _cams);
			p.cam.OnTargetCrashed(point);
			p.hud.SetOutOfBounds(false, 0f, 1f);
			bool outOfBounds = point.y > route.ceiling;
			p.hud.ShowMessage(outOfBounds ? "OUT OF BOUNDS" : "CRASHED", "respawning at last checkpoint", new Color(1f, 0.4f, 0.3f), respawnDelay);
			Rumble(i, p, 1f, 1f, 0.55f);
			p.respawnAt = Time.time + respawnDelay;
		}

		private void Respawn(Player p)
		{
			p.respawnAt = -1f;
			int pathIndex = p.nextCp == 0 ? 0 : route.checkpoints[p.nextCp - 1];
			Vector3 pos = route.path[pathIndex];
			Vector3 fwd = Vector3.ProjectOnPlane(route.path[Mathf.Min(route.path.Length - 1, pathIndex + 3)] - pos, Vector3.up).normalized;
			p.glider.Respawn(pos, Quaternion.LookRotation(fwd));
			p.pathHint = pathIndex;
			p.prevPos = pos;
			p.visuals.ClearTrails();
			p.cam.Snap();
		}

		private void FinishRace()
		{
			_endAt = -1f;
			GameSettings.HasResult = true;
			GameSettings.Winner = _winner;
			for (int i = 0; i < 2; i++)
			{
				bool has = i < _players.Count;
				GameSettings.Finished[i] = has && _players[i].finished;
				GameSettings.FinishTimes[i] = has ? _players[i].finishTime : 0f;
				GameSettings.MissedCheckpoints[i] = has ? _players[i].missed : 0;
			}
			SceneManager.LoadScene(GameSettings.EndScene);
		}

		// ---------------------------------------------------------------- menu showcase

		private void StartShowcase()
		{
			_showcase = true;
			Vector3 start = route.path[0];
			Vector3 fwd = Vector3.ProjectOnPlane(route.path[3] - route.path[0], Vector3.up).normalized;
			Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
			for (int i = 0; i < 2; i++)
			{
				GameObject go = Instantiate(fx.gliderPrefabs[i], start + right * (i == 0 ? -12f : 12f) - fwd * i * 30f, Quaternion.LookRotation(fwd));
				var g = go.AddComponent<GliderController>();
				g.PlayerIndex = i;
				g.ceiling = route.ceiling;
				var v = go.AddComponent<GliderVisuals>();
				v.trailMaterial = fx.trail;
				v.vaporMaterial = fx.vapor;
				v.playerColor = GameSettings.PlayerColors[i];
				var ap = go.AddComponent<AutopilotDriver>();
				ap.route = route;
				ap.lateralOffset = i == 0 ? -10f : 10f;
				g.OnCrash += (glider, point, vel) => glider.Respawn(route.path[Mathf.Max(0, route.ClosestIndex(point, 0, route.path.Length) - 3)], Quaternion.LookRotation(Vector3.ProjectOnPlane(fwd, Vector3.up)), 1f);
				_showcaseGliders.Add(g);
			}
			var camGo = new GameObject("Showcase Camera");
			var cam = camGo.AddComponent<Camera>();
			cam.nearClipPlane = 0.5f;
			cam.farClipPlane = 20000f;
			cam.depth = 5f;
			camGo.AddComponent<HDAdditionalCameraData>().antialiasing = HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;
			var dir = camGo.AddComponent<ShowcaseDirector>();
			dir.subjects = _showcaseGliders.ToArray();
			dir.route = route;
			BuildRings();
		}

		private void UpdateShowcase()
		{
			foreach (GliderController g in _showcaseGliders)
			{
				// Loop the flyover: back to the start after the finish.
				if (!g.Crashed && (g.transform.position - route.path[route.path.Length - 1]).sqrMagnitude < 120f * 120f)
				{
					Vector3 fwd = Vector3.ProjectOnPlane(route.path[3] - route.path[0], Vector3.up).normalized;
					g.Respawn(route.path[0], Quaternion.LookRotation(fwd), 0.5f);
					g.GetComponent<GliderVisuals>().ClearTrails();
				}
			}
			UpdateRings();
		}

		// ---------------------------------------------------------------- rings

		private void BuildRings()
		{
			var root = new GameObject("Checkpoint Rings").transform;
			Mesh ring = Torus(1f, 0.026f, 72, 10);
			for (int i = 0; i < route.CheckpointCount; i++)
			{
				bool last = i == route.CheckpointCount - 1;
				var go = new GameObject(last ? "Finish Gate" : "Checkpoint " + (i + 1));
				go.transform.SetParent(root, false);
				go.transform.position = route.CheckpointPosition(i);
				go.transform.rotation = Quaternion.LookRotation(route.CheckpointForward(i));
				go.transform.localScale = Vector3.one * route.Radius(i);
				go.AddComponent<MeshFilter>().sharedMesh = ring;
				var mr = go.AddComponent<MeshRenderer>();
				mr.sharedMaterial = last ? fx.ringFinish : fx.ring;
				mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
				if (last)
				{
					var inner = Instantiate(go, go.transform.position, go.transform.rotation * Quaternion.Euler(0f, 0f, 45f), root);
					inner.transform.localScale = Vector3.one * route.Radius(i) * 0.86f;
				}
				_rings.Add(go.transform);
			}
		}

		private void UpdateRings()
		{
			int lowest = int.MaxValue;
			foreach (Player p in _players) lowest = Mathf.Min(lowest, p.nextCp);
			for (int i = 0; i < _rings.Count; i++)
			{
				// Rings everyone has passed shrink away; the rest slowly spin.
				bool passed = i < lowest;
				Transform t = _rings[i];
				float target = passed ? 0.01f : route.Radius(i);
				t.localScale = Vector3.one * Mathf.Lerp(t.localScale.x, target, 1f - Mathf.Exp(-4f * Time.deltaTime));
				t.Rotate(0f, 0f, 12f * Time.deltaTime, Space.Self);
			}
		}

		private static Mesh Torus(float radius, float tube, int seg, int sides)
		{
			var v = new List<Vector3>();
			var n = new List<Vector3>();
			var t = new List<int>();
			for (int i = 0; i <= seg; i++)
			{
				float a = i * Mathf.PI * 2f / seg;
				var c = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius;
				for (int j = 0; j <= sides; j++)
				{
					float b = j * Mathf.PI * 2f / sides;
					Vector3 dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * Mathf.Cos(b) + Vector3.forward * Mathf.Sin(b);
					v.Add(c + dir * tube);
					n.Add(dir);
				}
			}
			for (int i = 0; i < seg; i++)
			{
				for (int j = 0; j < sides; j++)
				{
					int a = i * (sides + 1) + j, b = a + sides + 1;
					t.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
				}
			}
			var m = new Mesh { name = "Ring" };
			m.SetVertices(v);
			m.SetNormals(n);
			m.SetTriangles(t, 0);
			m.RecalculateBounds();
			return m;
		}

		// The original scene's player, camera, HUD and portal effects are replaced by this race system.
		private static void DisableLegacyObjects()
		{
			foreach (string path in new[] { "/player", "/Main Camera", "/[UI]", "/portal/Portal yellow", "/portal/Portal green", "/portal/Portal red", "/portal/Portal blue", "/portal/Magic circle" })
			{
				GameObject go = GameObject.Find(path);
				if (go != null) go.SetActive(false);
			}
			GameObject ceiling = GameObject.Find("/ceiling");
			if (ceiling != null)
			{
				foreach (Collider c in ceiling.GetComponents<Collider>()) c.enabled = false; // soft ceiling in GliderController
			}
		}
	}
}
