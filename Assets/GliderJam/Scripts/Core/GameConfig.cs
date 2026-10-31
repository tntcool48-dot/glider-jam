using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GliderJam
{
	public enum QualityPreset
	{
		Low = 0,
		Medium = 1,
		High = 2,
		Ultra = 3,
		UltraRayTracing = 4,
		Custom = 5
	}

	public enum ControlAction
	{
		PitchUp = 0,
		PitchDown = 1,
		TurnLeft = 2,
		TurnRight = 3,
		Boost = 4,
		Brake = 5
	}

	[Serializable]
	public class PlayerControls
	{
		public Key[] keys = new Key[6];
		public bool invertPitch;
		public float fieldOfView = 62f;
		public float vibration = 1f;
	}

	// Every user setting. Saved as JSON next to the game (never in the registry or AppData):
	// build: <game folder>/Settings/settings.json, editor: <project>/UserSettings/GliderJamSettings.json.
	[Serializable]
	public class GameConfig
	{
		// ---- audio (0..1)
		public float masterVolume = 1f;
		public float musicVolume = 0.55f;
		public float effectsVolume = 1f;
		public float windVolume = 0.8f;

		// ---- display
		public int resolutionWidth;
		public int resolutionHeight;
		public int displayMode;            // 0 borderless, 1 exclusive fullscreen, 2 windowed
		public bool vsync = true;
		public int frameCap;               // 0 = unlimited
		public int renderScale = 100;      // 50..100 %
		public int upscaler = 1;           // 0 Catmull-Rom, 1 TAA upscale, 2 FSR 1 (edge adaptive)

		// ---- graphics
		public QualityPreset preset = QualityPreset.High;
		public int shadows = 3;            // 0 off, 1 low, 2 medium, 3 high, 4 ultra
		public int clouds = 2;             // 0 off, 1 low, 2 high, 3 ultra
		public int fog = 2;                // 0 off (distance fog only), 1 low, 2 high, 3 ultra
		public int ambientOcclusion = 1;   // 0 off, 1 screen space, 2 ray traced
		public int reflections = 1;        // 0 off, 1 screen space, 2 ray traced
		public int globalIllumination = 0; // 0 off, 1 screen space, 2 ray traced
		public int antiAliasing = 3;       // 0 off, 1 FXAA, 2 SMAA, 3 TAA
		public int textureQuality = 2;     // 0 quarter, 1 half, 2 full
		public int vegetation = 2;         // 0 low .. 3 ultra
		public int terrainDetail = 2;      // 0 low .. 3 ultra
		public int backdrop = 2;           // surrounding landscape: 0 off, 1 low, 2 high
		public int water = 1;              // 0 off, 1 on
		public int effects = 2;            // particle detail 0 low .. 2 high
		public bool motionBlur = true;
		public bool depthOfField = true;
		public bool filmGrain = true;
		public bool lensFlare = true;
		public bool chromaticAberration = true;
		public bool cameraShake = true;
		public bool speedEffects = true;

		// ---- controls
		public PlayerControls[] players = DefaultPlayers();
		public int controlsVersion; // 2: player 2 boosts with Right Shift (next to the arrows), brakes with Right Ctrl

		// ---- records
		public float bestSoloCanyon;
		public float bestSoloCity;

		// ================================================================ presets

		public static readonly string[] PresetNames = { "LOW", "MEDIUM", "HIGH", "ULTRA", "ULTRA + RAY TRACING", "CUSTOM" };

		public void ApplyPreset(QualityPreset p)
		{
			preset = p;
			switch (p)
			{
				case QualityPreset.Low:
					shadows = 1; clouds = 0; fog = 0; ambientOcclusion = 0; reflections = 0; globalIllumination = 0;
					antiAliasing = 1; textureQuality = 1; vegetation = 0; terrainDetail = 0; backdrop = 0; water = 0; effects = 0;
					motionBlur = false; filmGrain = false; renderScale = 75; break;
				case QualityPreset.Medium:
					shadows = 2; clouds = 1; fog = 1; ambientOcclusion = 1; reflections = 0; globalIllumination = 0;
					antiAliasing = 2; textureQuality = 2; vegetation = 1; terrainDetail = 1; backdrop = 1; water = 1; effects = 1;
					motionBlur = true; filmGrain = true; renderScale = 100; break;
				case QualityPreset.High:
					shadows = 3; clouds = 2; fog = 2; ambientOcclusion = 1; reflections = 1; globalIllumination = 0;
					antiAliasing = 3; textureQuality = 2; vegetation = 2; terrainDetail = 2; backdrop = 2; water = 1; effects = 2;
					motionBlur = true; filmGrain = true; renderScale = 100; break;
				case QualityPreset.Ultra:
					shadows = 4; clouds = 3; fog = 3; ambientOcclusion = 1; reflections = 1; globalIllumination = 1;
					antiAliasing = 3; textureQuality = 2; vegetation = 3; terrainDetail = 3; backdrop = 2; water = 1; effects = 2;
					motionBlur = true; filmGrain = true; renderScale = 100; break;
				case QualityPreset.UltraRayTracing:
					// Ray traced reflections only: RT AO / GI cost most and change least (still selectable under Custom).
					shadows = 4; clouds = 3; fog = 3; ambientOcclusion = 1; reflections = 2; globalIllumination = 1;
					antiAliasing = 3; textureQuality = 2; vegetation = 3; terrainDetail = 3; backdrop = 2; water = 1; effects = 2;
					motionBlur = true; filmGrain = true; renderScale = 100; break;
			}
		}

		public bool UsesRayTracing => ambientOcclusion == 2 || reflections == 2 || globalIllumination == 2;

		public static PlayerControls[] DefaultPlayers()
		{
			return new[]
			{
				new PlayerControls { keys = new[] { Key.W, Key.S, Key.A, Key.D, Key.Space, Key.LeftShift } },
				new PlayerControls { keys = new[] { Key.UpArrow, Key.DownArrow, Key.LeftArrow, Key.RightArrow, Key.RightShift, Key.RightCtrl } }
			};
		}

		// ================================================================ storage

		private static GameConfig _current;
		public static event Action<GameConfig> Changed;

		public static GameConfig Current
		{
			get
			{
				if (_current == null) _current = Load();
				return _current;
			}
		}

		public static string FilePath
		{
			get
			{
#if UNITY_EDITOR
				string dir = Path.Combine(Path.GetDirectoryName(Application.dataPath), "UserSettings");
				return Path.Combine(dir, "GliderJamSettings.json");
#else
				string dir = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Settings");
				return Path.Combine(dir, "settings.json");
#endif
			}
		}

		private static GameConfig Load()
		{
			try
			{
				if (File.Exists(FilePath))
				{
					var c = JsonUtility.FromJson<GameConfig>(File.ReadAllText(FilePath));
					if (c != null)
					{
						c.Validate();
						return c;
					}
				}
			}
			catch (Exception e)
			{
				Debug.LogWarning("GliderJam: could not read settings, using defaults (" + e.Message + ")");
			}
			var d = new GameConfig();
			d.ApplyPreset(QualityPreset.High);
			return d;
		}

		private void Validate()
		{
			if (players == null || players.Length < 2) players = DefaultPlayers();
			foreach (PlayerControls p in players)
			{
				if (p.keys == null || p.keys.Length < 6) p.keys = DefaultPlayers()[0].keys;
				p.fieldOfView = Mathf.Clamp(p.fieldOfView <= 1f ? 62f : p.fieldOfView, 50f, 100f);
			}
			renderScale = Mathf.Clamp(renderScale == 0 ? 100 : renderScale, 50, 100);
			if (controlsVersion < 2)
			{
				// Saved before the swap and still on the old defaults: move them to the new layout.
				Key[] k = players[1].keys;
				if (k[(int)ControlAction.Boost] == Key.RightCtrl && k[(int)ControlAction.Brake] == Key.RightShift)
				{
					k[(int)ControlAction.Boost] = Key.RightShift;
					k[(int)ControlAction.Brake] = Key.RightCtrl;
				}
				controlsVersion = 2;
			}
		}

		public void Save()
		{
			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
				File.WriteAllText(FilePath, JsonUtility.ToJson(this, true));
			}
			catch (Exception e)
			{
				Debug.LogWarning("GliderJam: could not save settings (" + e.Message + ")");
			}
		}

		// Call after changing values: applies them everywhere and saves.
		public void Commit()
		{
			Save();
			Changed?.Invoke(this);
		}

		public static void ResetToDefaults()
		{
			var keepBest = _current;
			_current = new GameConfig();
			_current.ApplyPreset(QualityPreset.High);
			if (keepBest != null)
			{
				_current.bestSoloCanyon = keepBest.bestSoloCanyon;
				_current.bestSoloCity = keepBest.bestSoloCity;
			}
			_current.Commit();
		}
	}
}
