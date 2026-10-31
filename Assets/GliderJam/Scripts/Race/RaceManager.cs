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
			public float progress;
			public bool finished;
			public float finishTime;
			public float respawnAt = -1f;
			public Vector3 prevPos;
			public Volume boostVolume;
			public float rumbleUntil;
			public float nextAlarm;
			public float wrongWay;
			public Vector3 crashPoint;
		}

		private readonly List<Player> _players = new List<Player>();
		private float _raceStart = -1f;
		private float _countdownEnd;
		private int _lastCountdown = -1;
		private int _winner = -1;
		private float _endAt = -1f;

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
				p.glider.minX = route.minX;
				p.glider.maxX = route.maxX;
				p.glider.minZ = route.MinZ;
				p.glider.OnCrash += HandleCrash;
				p.visuals = go.AddComponent<GliderVisuals>();
				p.visuals.trailMaterial = fx.trail;
				p.visuals.vaporMaterial = fx.vapor;
				p.visuals.puffMaterial = fx.wingVapor != null ? fx.wingVapor : fx.smoke;
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
			BuildFinishLine();
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

			float finishZ = route.FinishZ;
			for (int i = 0; i < _players.Count; i++)
			{
				Player p = _players[i];
				Vector3 pos = p.glider.transform.position;
				// Free route: the race is won by crossing the finish line anywhere across the course.
				if (Running && !p.finished && !p.glider.Crashed && p.prevPos.z < finishZ && pos.z >= finishZ)
				{
					Finish(i, p);
				}
				p.prevPos = pos;
				if (p.respawnAt > 0f && Time.time >= p.respawnAt)
				{
					Respawn(p);
				}
				p.progress = p.finished ? 1e6f - p.finishTime : pos.z;
				float speed = p.glider.Crashed ? 0f : p.glider.Speed;
				float time = p.finished ? p.finishTime : RaceTime;
				float toGo = Mathf.Max(0f, finishZ - pos.z);
				p.hud.SetStats(Rank(i), toGo, time, speed, p.glider.BoostEnergy);

				// The finish line straight ahead, at the glider's own side position and height.
				var goal = new Vector3(Mathf.Clamp(pos.x, route.minX, route.maxX), pos.y, finishZ);
				p.hud.UpdateNext(goal, "FINISH  " + Distance(toGo), !p.finished);
				if (_players.Count == 2)
				{
					Player o = _players[1 - i];
					Vector3 op = o.glider.transform.position;
					string label = "P" + (2 - i) + "  " + Mathf.RoundToInt(Vector3.Distance(pos, op)) + " m";
					p.hud.UpdateOther(op, label, !o.glider.Crashed);
				}

				// Wrong way: heading back towards the start. Warn, then turn the glider around.
				Vector3 v = Vector3.ProjectOnPlane(p.glider.Velocity, Vector3.up);
				bool backwards = Running && !p.finished && !p.glider.Crashed && v.sqrMagnitude > 100f && v.normalized.z < -0.25f;
				p.wrongWay = backwards ? p.wrongWay + Time.deltaTime : Mathf.Max(0f, p.wrongWay - 2f * Time.deltaTime);
				bool wrong = p.wrongWay > WrongWayWarn;
				if (p.wrongWay > WrongWayWarn + WrongWayGrace) TurnAround(p);

				// Above the ceiling / off the course: warning + alarm, countdown to destruction.
				bool oob = p.glider.OutOfBounds && !p.glider.Crashed;
				if (oob)
				{
					bool ceiling = p.glider.AboveCeiling;
					p.hud.SetWarning(true, ceiling ? "PULL DOWN" : "OFF COURSE", ceiling ? "RETURN BELOW THE CLOUDS" : "HEAD BACK INTO THE COURSE", p.glider.OutOfBoundsLeft, p.glider.ceilingGrace);
				}
				else
				{
					p.hud.SetWarning(wrong, "WRONG WAY", "TURN AROUND", WrongWayWarn + WrongWayGrace - p.wrongWay, WrongWayGrace);
				}
				if ((oob || wrong) && Time.time >= p.nextAlarm)
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

		private static string Distance(float meters)
		{
			return meters >= 1000f ? (meters / 1000f).ToString("0.0") + " km" : Mathf.RoundToInt(meters / 10f) * 10 + " m";
		}

		// Seconds of heading backwards before the warning shows, then before the glider is turned around.
		private const float WrongWayWarn = 0.6f, WrongWayGrace = 3f;

		private void TurnAround(Player p)
		{
			p.wrongWay = 0f;
			Vector3 pos = p.glider.transform.position;
			p.glider.Respawn(pos, Quaternion.LookRotation(Vector3.forward), 1.5f);
			p.prevPos = pos;
			p.visuals.ClearTrails();
			p.cam.Snap();
			p.hud.ShowMessage("", "TURNED AROUND", new Color(1f, 0.7f, 0.35f), 1.2f);
		}

		private void Finish(int i, Player p)
		{
			p.finished = true;
			AudioSynth.Play(AudioSynth.Go, 0.9f, 0.8f);
			Rumble(i, p, 0.4f, 0.4f, 0.3f);
			p.finishTime = RaceTime;
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
			p.hud.SetWarning(false, "", "", 0f, 1f);
			if (p.finished) return; // after the line: keep the result on screen, no respawn
			bool outOfBounds = point.y > route.ceiling || !route.InsideCourse(point);
			p.hud.ShowMessage(outOfBounds ? "OUT OF BOUNDS" : "CRASHED", "respawning", new Color(1f, 0.4f, 0.3f), respawnDelay);
			Rumble(i, p, 1f, 1f, 0.55f);
			p.crashPoint = point;
			p.respawnAt = Time.time + respawnDelay;
		}

		// Back on the (known-clear) race line a little behind the crash.
		private void Respawn(Player p)
		{
			p.respawnAt = -1f;
			p.wrongWay = 0f;
			int pathIndex = Mathf.Min(route.IndexAtZ(p.crashPoint.z - 120f), route.checkpoints[route.CheckpointCount - 1] - 4);
			pathIndex = Mathf.Max(0, pathIndex);
			Vector3 pos = route.path[pathIndex];
			Vector3 fwd = Vector3.ProjectOnPlane(route.path[Mathf.Min(route.path.Length - 1, pathIndex + 3)] - pos, Vector3.up).normalized;
			p.glider.Respawn(pos, Quaternion.LookRotation(fwd));
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
				GameSettings.MissedCheckpoints[i] = 0;
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
				v.puffMaterial = fx.wingVapor != null ? fx.wingVapor : fx.smoke;
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
			BuildFinishLine();
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
		}

		// ---------------------------------------------------------------- finish line

		// Free route: a line of glowing gold pillars and bars across the whole course at the finish.
		private void BuildFinishLine()
		{
			var root = new GameObject("Finish Line").transform;
			float z = route.FinishZ, top = route.ceiling;
			float x0 = float.IsInfinity(route.minX) ? route.CheckpointPosition(route.CheckpointCount - 1).x - 500f : route.minX;
			float x1 = float.IsInfinity(route.maxX) ? x0 + 1000f : route.maxX;
			int n = Mathf.Max(2, Mathf.RoundToInt((x1 - x0) / 45f));
			for (int k = 0; k <= n; k++)
			{
				float x = Mathf.Lerp(x0, x1, k / (float)n);
				Beam(root, new Vector3(x, top * 0.5f - 10f, z), new Vector3(1.4f, top + 20f, 1.4f));
			}
			for (float y = 40f; y <= top; y += 70f) Beam(root, new Vector3((x0 + x1) * 0.5f, y, z), new Vector3(x1 - x0, 0.7f, 0.7f));
			Beam(root, new Vector3((x0 + x1) * 0.5f, top, z), new Vector3(x1 - x0, 1.2f, 1.2f));
		}

		private void Beam(Transform parent, Vector3 center, Vector3 size)
		{
			GameObject b = GameObject.CreatePrimitive(PrimitiveType.Cube);
			Destroy(b.GetComponent<Collider>());
			b.name = "Finish Beam";
			b.transform.SetParent(parent, false);
			b.transform.position = center;
			b.transform.localScale = size;
			var mr = b.GetComponent<MeshRenderer>();
			mr.sharedMaterial = fx.ringFinish;
			mr.shadowCastingMode = ShadowCastingMode.Off;
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
