using System.Collections.Generic;
using GliderJam.World;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

namespace GliderJam
{
	// Applies GameConfig to the game: HDRP quality level (one HDRP asset per level, incl. ray tracing),
	// a high-priority runtime volume (clouds, fog, shadows, AO, reflections, GI, post), per-camera frame
	// settings / anti-aliasing / dynamic resolution, texture + LOD limits, display mode and the scene's
	// quality hooks (forest, backdrop, water, sun flare, mist). Persistent; re-applies on scene load,
	// on config change, and to any new camera it sees.
	public class QualityManager : MonoBehaviour
	{
		public static QualityManager Instance { get; private set; }

		private VolumeProfile _profile;
		private Volume _volume;
		private readonly HashSet<int> _configured = new HashSet<int>();
#if !UNITY_EDITOR
		private int _appliedResolution = -1;
#endif

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Boot()
		{
			if (Instance != null) return;
			var go = new GameObject("Glider Jam Systems");
			DontDestroyOnLoad(go);
			Instance = go.AddComponent<QualityManager>();
			go.AddComponent<Audio.MusicPlayer>();
			Benchmark.ApplyCommandLineOverrides(GameConfig.Current);
		}

		private void Awake()
		{
			_profile = ScriptableObject.CreateInstance<VolumeProfile>();
			_volume = gameObject.AddComponent<Volume>();
			_volume.isGlobal = true;
			_volume.priority = 100f;
			_volume.sharedProfile = _profile;
			GameConfig.Changed += ApplyAll;
			SceneManager.sceneLoaded += (s, m) => ApplyAll(GameConfig.Current);
		}

		private void Start()
		{
			ApplyAll(GameConfig.Current);
		}

		private void OnDestroy()
		{
			GameConfig.Changed -= ApplyAll;
		}

		private void Update()
		{
			foreach (Camera c in Camera.allCameras)
			{
				if (_configured.Add(c.GetInstanceID())) ConfigureCamera(c, GameConfig.Current);
			}
		}

		public void ApplyAll(GameConfig cfg)
		{
			ApplyDisplay(cfg);
			ApplyQualityLevel(cfg);
			ApplyVolume(cfg);
			QualitySettings.globalTextureMipmapLimit = 2 - Mathf.Clamp(cfg.textureQuality, 0, 2);
			QualitySettings.lodBias = new[] { 0.7f, 1f, 1.6f, 2.6f }[Mathf.Clamp(cfg.terrainDetail, 0, 3)];
			DynamicResolutionHandler.SetDynamicResScaler(() => GameConfig.Current.renderScale, DynamicResScalePolicyType.ReturnsPercentage);
			AudioListener.volume = cfg.masterVolume;
			_configured.Clear();
			foreach (Camera c in Camera.allCameras)
			{
				_configured.Add(c.GetInstanceID());
				ConfigureCamera(c, cfg);
			}
			ApplyScene(cfg);
		}

		private void ApplyDisplay(GameConfig cfg)
		{
			QualitySettings.vSyncCount = cfg.vsync && !Benchmark.Active ? 1 : 0;
			Application.targetFrameRate = cfg.frameCap > 0 && !Benchmark.Active ? cfg.frameCap : -1;
#if !UNITY_EDITOR
			int key = cfg.resolutionWidth * 7919 + cfg.resolutionHeight * 31 + cfg.displayMode;
			if (key != _appliedResolution && !Benchmark.Active)
			{
				_appliedResolution = key;
				int w = cfg.resolutionWidth > 0 ? cfg.resolutionWidth : Screen.currentResolution.width;
				int h = cfg.resolutionHeight > 0 ? cfg.resolutionHeight : Screen.currentResolution.height;
				var mode = cfg.displayMode == 1 ? FullScreenMode.ExclusiveFullScreen : cfg.displayMode == 2 ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow;
				Screen.SetResolution(w, h, mode);
			}
#endif
		}

		// Low -> Performant, Medium -> Balanced, High/Ultra/Custom -> High Fidelity, any ray traced option -> Ultra RT.
		private static void ApplyQualityLevel(GameConfig cfg)
		{
			string want = cfg.UsesRayTracing ? "Ultra RT"
				: cfg.preset == QualityPreset.Low ? "Performant"
				: cfg.preset == QualityPreset.Medium ? "Balanced"
				: "High Fidelity";
			int index = System.Array.IndexOf(QualitySettings.names, want);
			if (index < 0) index = System.Array.IndexOf(QualitySettings.names, "High Fidelity");
			if (index >= 0 && QualitySettings.GetQualityLevel() != index)
			{
				QualitySettings.SetQualityLevel(index, true);
			}
		}

		private T Get<T>() where T : VolumeComponent
		{
			if (!_profile.TryGet(out T c)) c = _profile.Add<T>(false);
			return c;
		}

		private void ApplyVolume(GameConfig cfg)
		{
			var clouds = Get<VolumetricClouds>();
			if (cfg.clouds == 0)
			{
				clouds.enable.Override(false);
			}
			else
			{
				clouds.enable.overrideState = false; // keep the level's own setting (on)
				clouds.numPrimarySteps.Override(new[] { 0, 24, 48, 80 }[cfg.clouds]);
				clouds.numLightSteps.Override(new[] { 0, 4, 6, 12 }[cfg.clouds]);
				clouds.shadows.Override(cfg.clouds >= 2);
			}

			var fog = Get<Fog>();
			fog.enableVolumetricFog.Override(cfg.fog > 0);
			if (cfg.fog > 0) fog.quality.Override(Mathf.Clamp(cfg.fog - 1, 0, 2));

			var shadows = Get<HDShadowSettings>();
			shadows.maxShadowDistance.Override(new[] { 250f, 700f, 1200f, 1600f, 2600f }[Mathf.Clamp(cfg.shadows, 0, 4)]);
			shadows.cascadeShadowSplitCount.Override(new[] { 1, 2, 3, 4, 4 }[Mathf.Clamp(cfg.shadows, 0, 4)]);
			// Contact shadows speckle on the thin wings at low sample counts: Ultra only, with more samples.
			var contact = Get<ContactShadows>();
			contact.enable.Override(cfg.shadows >= 4);
			contact.quality.Override(2);

			var ao = Get<ScreenSpaceAmbientOcclusion>();
			ao.rayTracing.Override(cfg.ambientOcclusion == 2);
			ao.quality.Override(cfg.ambientOcclusion == 2 ? 2 : 1);

			var ssr = Get<ScreenSpaceReflection>();
			ssr.enabled.Override(cfg.reflections > 0);
			ssr.enabledTransparent.Override(cfg.reflections > 0);
			ssr.tracing.Override(cfg.reflections == 2 ? RayCastingMode.RayTracing : RayCastingMode.RayMarching);
			ssr.quality.Override(cfg.reflections == 2 ? 2 : 1);

			var gi = Get<GlobalIllumination>();
			gi.enable.Override(cfg.globalIllumination > 0);
			gi.tracing.Override(cfg.globalIllumination == 2 ? RayCastingMode.RayTracing : RayCastingMode.RayMarching);
			gi.quality.Override(cfg.globalIllumination == 2 ? 1 : 2);

			var mb = Get<MotionBlur>();
			if (cfg.motionBlur) mb.intensity.overrideState = false; else mb.intensity.Override(0f);
			var grain = Get<FilmGrain>();
			if (cfg.filmGrain) grain.intensity.overrideState = false; else grain.intensity.Override(0f);
		}

		public static void ConfigureCamera(Camera cam, GameConfig cfg)
		{
			var hd = cam.GetComponent<HDAdditionalCameraData>();
			if (hd == null) return;
			hd.customRenderingSettings = true;
			Set(hd, FrameSettingsField.Volumetrics, cfg.fog > 0);
			Set(hd, FrameSettingsField.VolumetricClouds, cfg.clouds > 0);
			Set(hd, FrameSettingsField.SSAO, cfg.ambientOcclusion > 0);
			Set(hd, FrameSettingsField.SSR, cfg.reflections > 0);
			Set(hd, FrameSettingsField.TransparentSSR, cfg.reflections > 0);
			Set(hd, FrameSettingsField.SSGI, cfg.globalIllumination > 0);
			Set(hd, FrameSettingsField.ContactShadows, cfg.shadows >= 4);
			Set(hd, FrameSettingsField.MotionBlur, cfg.motionBlur);
			Set(hd, FrameSettingsField.FilmGrain, cfg.filmGrain);
			Set(hd, FrameSettingsField.DepthOfField, cfg.depthOfField);
			Set(hd, FrameSettingsField.LensFlareDataDriven, cfg.lensFlare);
			Set(hd, FrameSettingsField.Water, cfg.water > 0);
			Set(hd, FrameSettingsField.RayTracing, cfg.UsesRayTracing);
			hd.antialiasing = cfg.antiAliasing == 0 ? HDAdditionalCameraData.AntialiasingMode.None
				: cfg.antiAliasing == 1 ? HDAdditionalCameraData.AntialiasingMode.FastApproximateAntialiasing
				: cfg.antiAliasing == 2 ? HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing
				: HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;
			hd.TAAQuality = cfg.preset >= QualityPreset.Ultra ? HDAdditionalCameraData.TAAQualityLevel.High : HDAdditionalCameraData.TAAQualityLevel.Medium;
			bool dynamic = cfg.renderScale < 100;
			cam.allowDynamicResolution = dynamic;
			hd.allowDynamicResolution = dynamic;
			DynamicResolutionHandler.SetUpscaleFilter(cam, cfg.upscaler == 0 ? DynamicResUpscaleFilter.CatmullRom : cfg.upscaler == 2 ? DynamicResUpscaleFilter.EdgeAdaptiveScalingUpres : DynamicResUpscaleFilter.TAAU);
		}

		private static void Set(HDAdditionalCameraData hd, FrameSettingsField field, bool on)
		{
			FrameSettingsOverrideMask mask = hd.renderingPathCustomFrameSettingsOverrideMask;
			mask.mask[(uint)field] = true;
			hd.renderingPathCustomFrameSettingsOverrideMask = mask;
			hd.renderingPathCustomFrameSettings.SetEnabled(field, on);
		}

		private static void ApplyScene(GameConfig cfg)
		{
			foreach (InstancedForest f in FindObjectsByType<InstancedForest>(FindObjectsSortMode.None)) f.ApplyQuality(cfg.vegetation);
			foreach (MeshTerrain t in FindObjectsByType<MeshTerrain>(FindObjectsSortMode.None)) t.ApplyQuality(cfg.backdrop, cfg.terrainDetail);
			CityWeather.ApplyQuality(cfg);
			CanyonAmbience.ApplyQuality(cfg);
			foreach (WaterSurface w in FindObjectsByType<WaterSurface>(FindObjectsInactive.Include, FindObjectsSortMode.None)) w.enabled = cfg.water > 0;
			foreach (LensFlareComponentSRP lf in FindObjectsByType<LensFlareComponentSRP>(FindObjectsSortMode.None)) lf.enabled = cfg.lensFlare;
			foreach (LocalVolumetricFog lv in FindObjectsByType<LocalVolumetricFog>(FindObjectsInactive.Include, FindObjectsSortMode.None)) lv.enabled = cfg.fog > 0;
			foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
			{
				if (l.type != LightType.Directional) continue;
				var hd = l.GetComponent<HDAdditionalLightData>();
				if (hd == null) continue;
				l.shadows = cfg.shadows == 0 ? LightShadows.None : LightShadows.Soft;
				hd.SetShadowResolutionLevel(Mathf.Clamp(cfg.shadows - 1, 0, 3));
			}
		}
	}
}
