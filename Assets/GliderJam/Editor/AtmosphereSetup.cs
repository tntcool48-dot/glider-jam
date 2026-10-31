using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace GliderJam.EditorTools
{
	// Builds the "Golden Pillars" look: golden-hour sun, physically based sky, volumetric clouds near the
	// flight ceiling, volumetric fog with valley mist, and the post-processing stack.
	public static class AtmosphereSetup
	{
		public const string ProfilePath = "Assets/GliderJam/Settings/GoldenHour.asset";
		private static readonly List<string> Missing = new List<string>();

		public static string Run()
		{
			Missing.Clear();
			System.IO.Directory.CreateDirectory("Assets/GliderJam/Settings");
			var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
			if (profile == null)
			{
				profile = ScriptableObject.CreateInstance<VolumeProfile>();
				AssetDatabase.CreateAsset(profile, ProfilePath);
			}

			var env = Get<VisualEnvironment>(profile);
			P(env, "skyType", (int)SkyType.PhysicallyBased);
			P(env, "skyAmbientMode", SkyAmbientMode.Dynamic);

			var sky = Get<PhysicallyBasedSky>(profile);
			P(sky, "aerosolDensity", 0.22f);
			P(sky, "aerosolTint", new Color(0.92f, 0.92f, 0.94f));
			P(sky, "horizonTint", new Color(1f, 0.97f, 0.93f));
			P(sky, "zenithTint", new Color(0.92f, 0.96f, 1f));
			P(sky, "groundTint", new Color(0.22f, 0.18f, 0.14f));

			var clouds = Get<VolumetricClouds>(profile);
			P(clouds, "enable", true);
			P(clouds, "cloudControl", "Simple");
			// cloudPreset is a property that rewrites the shape parameters, so set it first; then a broken,
			// warm-lit cumulus layer with its base just above the flight ceiling.
			PropertyInfo preset = typeof(VolumetricClouds).GetProperty("cloudPreset");
			preset.SetValue(clouds, Enum.Parse(preset.PropertyType, "Custom"));
			P(clouds, "densityMultiplier", 0.35f);
			P(clouds, "shapeFactor", 0.9f);
			P(clouds, "shapeScale", 2.4f);
			P(clouds, "erosionFactor", 0.8f);
			P(clouds, "erosionScale", 90f);
			P(clouds, "bottomAltitude", 330f);
			P(clouds, "altitudeRange", 1200f);
			P(clouds, "shadows", true);
			P(clouds, "shadowOpacity", 0.7f);
			P(clouds, "shadowDistance", 6000f);
			P(clouds, "globalWindSpeed", new WindParameter.WindParamaterValue { mode = WindParameter.WindOverrideMode.Custom, customValue = 25f });

			var fog = Get<Fog>(profile);
			P(fog, "enabled", true);
			P(fog, "meanFreePath", 5500f);
			P(fog, "baseHeight", 0f);
			P(fog, "maximumHeight", 260f);
			P(fog, "maxFogDistance", 15000f);
			P(fog, "albedo", new Color(0.92f, 0.92f, 0.95f));
			P(fog, "enableVolumetricFog", true);
			P(fog, "anisotropy", 0.72f);
			P(fog, "depthExtent", 1600f);
			P(fog, "globalLightProbeDimmer", 0.8f);

			var exposure = Get<Exposure>(profile);
			P(exposure, "mode", ExposureMode.AutomaticHistogram);
			P(exposure, "meteringMode", MeteringMode.CenterWeighted);
			P(exposure, "limitMin", 10.5f);
			P(exposure, "limitMax", 14.5f);
			P(exposure, "compensation", 0.8f);
			P(exposure, "adaptationSpeedDarkToLight", 2.5f);
			P(exposure, "adaptationSpeedLightToDark", 1.5f);

			var tone = Get<Tonemapping>(profile);
			P(tone, "mode", TonemappingMode.ACES);

			var ca = Get<ColorAdjustments>(profile);
			P(ca, "contrast", 12f);
			P(ca, "saturation", 0f);
			P(ca, "colorFilter", Color.white);

			var wb = Get<WhiteBalance>(profile);
			P(wb, "temperature", 0f);

			var split = Get<SplitToning>(profile);
			P(split, "shadows", new Color(0.29f, 0.39f, 0.52f));
			P(split, "highlights", new Color(0.95f, 0.71f, 0.42f));
			P(split, "balance", -15f);

			var bloom = Get<Bloom>(profile);
			P(bloom, "intensity", 0.22f);
			P(bloom, "scatter", 0.7f);
			P(bloom, "threshold", 1.1f);

			var vignette = Get<Vignette>(profile);
			P(vignette, "intensity", 0.22f);
			P(vignette, "smoothness", 0.5f);

			var blur = Get<MotionBlur>(profile);
			P(blur, "intensity", 0.35f);
			P(blur, "maximumVelocity", 160f);

			var grain = Get<FilmGrain>(profile);
			P(grain, "type", FilmGrainLookup.Thin1);
			P(grain, "intensity", 0.12f);

			var shadows = Get<HDShadowSettings>(profile);
			P(shadows, "maxShadowDistance", 2500f);
			P(shadows, "cascadeShadowSplitCount", 4);

			var contact = Get<ContactShadows>(profile);
			P(contact, "enable", true);

			var ao = Get<ScreenSpaceAmbientOcclusion>(profile);
			P(ao, "intensity", 1.2f);
			P(ao, "radius", 3f);

			EditorUtility.SetDirty(profile);
			AssetDatabase.SaveAssets();

			// ---- Scene: global volume, sun, valley mist ----
			var volGo = GameObject.Find("/Sky and Fog Volume") ?? new GameObject("Sky and Fog Volume");
			var vol = volGo.GetComponent<Volume>();
			if (vol == null)
			{
				vol = volGo.AddComponent<Volume>();
			}
			vol.isGlobal = true;
			vol.sharedProfile = profile;

			var sun = GameObject.Find("/Directional Light") ?? GameObject.Find("/Sun");
			sun.name = "Sun";
			// Low sun from the left and slightly behind the racing direction (+Z): lit faces, long shadows ahead.
			sun.transform.rotation = Quaternion.Euler(15f, 70f, 0f);
			var light = sun.GetComponent<Light>();
			light.intensity = 100000f;
			light.useColorTemperature = true;
			light.colorTemperature = 5600f;
			light.color = Color.white;
			light.shadows = LightShadows.Soft;
			var hd = sun.GetComponent<HDAdditionalLightData>();
			if (hd != null)
			{
				hd.SetShadowResolutionLevel(3);
				hd.volumetricDimmer = 1.2f;
			}

			BuildMist();
			ConfigurePipelineAsset();
			UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sun.scene);
			return "profile ok" + (Missing.Count > 0 ? "; missing: " + string.Join(", ", Missing) : "");
		}

		private static void BuildMist()
		{
			var root = GameObject.Find("/Valley Mist") ?? new GameObject("Valley Mist");
			for (int i = root.transform.childCount - 1; i >= 0; i--)
			{
				UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
			}
			// Dense low mist along the corridor floor, broken into overlapping boxes for some variation.
			for (int i = 0; i < 8; i++)
			{
				var go = new GameObject("Mist " + i);
				go.transform.SetParent(root.transform, false);
				go.transform.position = new Vector3(500f, 15f, 450f + i * 880f);
				var f = go.AddComponent<LocalVolumetricFog>();
				var p = f.parameters;
				p.albedo = new Color(0.86f, 0.88f, 0.92f);
				p.meanFreePath = 700f + 250f * (i % 3);
				p.size = new Vector3(1100f, 110f, 1000f);
				p.positiveFade = new Vector3(0.05f, 0.75f, 0.1f);
				p.negativeFade = new Vector3(0.05f, 0.05f, 0.1f);
				p.distanceFadeStart = 9000f;
				p.distanceFadeEnd = 12000f;
				f.parameters = p;
			}
		}

		private static void ConfigurePipelineAsset()
		{
			foreach (var asset in new[] { GraphicsSettings.defaultRenderPipeline, QualitySettings.renderPipeline })
			{
				if (asset == null)
				{
					continue;
				}
				var so = new SerializedObject(asset);
				Set(so, "m_RenderPipelineSettings.supportVolumetrics", true);
				Set(so, "m_RenderPipelineSettings.supportVolumetricClouds", true);
				Set(so, "m_RenderPipelineSettings.supportSSAO", true);
				Set(so, "m_RenderPipelineSettings.supportSSR", true);
				Set(so, "m_RenderPipelineSettings.supportMotionVectors", true);
				so.ApplyModifiedPropertiesWithoutUndo();
			}
		}

		private static void Set(SerializedObject so, string path, bool v)
		{
			var p = so.FindProperty(path);
			if (p == null)
			{
				Missing.Add(path);
				return;
			}
			p.boolValue = v;
		}

		private static T Get<T>(VolumeProfile profile) where T : VolumeComponent
		{
			if (!profile.TryGet(out T c))
			{
				c = profile.Add<T>(false);
				// Components must be sub-assets of the profile or they vanish on the next domain reload.
				c.name = typeof(T).Name;
				c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
				AssetDatabase.AddObjectToAsset(c, profile);
			}
			c.active = true;
			return c;
		}

		// Sets a VolumeParameter field by name (with override) so a renamed field is reported, not a compile error.
		private static void P(VolumeComponent c, string field, object value)
		{
			FieldInfo f = c.GetType().GetField(field, BindingFlags.Public | BindingFlags.Instance);
			if (f == null)
			{
				Missing.Add(c.GetType().Name + "." + field);
				return;
			}
			var param = (VolumeParameter)f.GetValue(c);
			PropertyInfo vp = param.GetType().GetProperty("value");
			Type t = vp.PropertyType;
			object v = value;
			if (t.IsEnum)
			{
				v = value is string s ? Enum.Parse(t, s) : Enum.ToObject(t, Convert.ToInt32(value));
			}
			else if (t == typeof(float))
			{
				v = Convert.ToSingle(value);
			}
			else if (t == typeof(int))
			{
				v = Convert.ToInt32(value);
			}
			try
			{
				vp.SetValue(param, v);
				param.overrideState = true;
			}
			catch (Exception e)
			{
				Missing.Add(c.GetType().Name + "." + field + " (" + e.GetType().Name + ")");
			}
		}
	}
}
