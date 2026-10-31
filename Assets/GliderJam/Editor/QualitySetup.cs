using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace GliderJam.EditorTools
{
	// Quality levels: Performant (Low), Balanced (Medium), High Fidelity (High/Ultra) and a new "Ultra RT"
	// level whose HDRP asset has ray tracing enabled. Turns on the HDRP features the settings menu toggles.
	public static class QualitySetup
	{
		private const string RtAsset = "Assets/GliderJam/Settings/HDRP Ultra RT.asset";
		private static readonly List<string> Report = new List<string>();

		public static string Run()
		{
			Report.Clear();
			if (AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(RtAsset) == null)
			{
				AssetDatabase.CopyAsset("Assets/Settings/HDRP High Fidelity.asset", RtAsset);
			}
			foreach (string path in new[] { "Assets/Settings/HDRP Performant.asset", "Assets/Settings/HDRP Balanced.asset", "Assets/Settings/HDRP High Fidelity.asset", RtAsset })
			{
				var asset = AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(path);
				var so = new SerializedObject(asset);
				Bool(so, "m_RenderPipelineSettings.dynamicResolutionSettings.enabled", true);
				Float(so, "m_RenderPipelineSettings.dynamicResolutionSettings.minPercentage", 50f);
				Float(so, "m_RenderPipelineSettings.dynamicResolutionSettings.maxPercentage", 100f);
				Bool(so, "m_RenderPipelineSettings.supportSSGI", true);
				Bool(so, "m_RenderPipelineSettings.supportSSR", true);
				Bool(so, "m_RenderPipelineSettings.supportSSRTransparent", true);
				Bool(so, "m_RenderPipelineSettings.supportSSAO", true);
				Bool(so, "m_RenderPipelineSettings.supportVolumetrics", true);
				Bool(so, "m_RenderPipelineSettings.supportVolumetricClouds", true);
				Bool(so, "m_RenderPipelineSettings.supportWater", true);
				Bool(so, "m_RenderPipelineSettings.supportMotionVectors", true);
				bool rt = path == RtAsset;
				Bool(so, "m_RenderPipelineSettings.supportRayTracing", rt);
				if (rt)
				{
					// Top tier (index 2, used by the Ultra + Ray Tracing preset) tuned to show the reflections off:
					// full resolution, long rays, down to wet-asphalt smoothness, gentle denoising.
					const string q = "m_RenderPipelineSettings.lightingQualitySettings.";
					Tier(so, q + "RTRMinSmoothness", 0.45f);
					Tier(so, q + "RTRSmoothnessFadeStart", 0.55f);
					Tier(so, q + "RTRRayLength", 2000f);
					Tier(so, q + "RTRFullResolution", true);
					Tier(so, q + "RTRRayMaxIterations", 64);
					Tier(so, q + "RTRDenoise", true);
					Tier(so, q + "RTRDenoiserRadiusDimmer", 0.6f);
					Tier(so, q + "RTRDenoiserAntiFlicker", 1f);
					Tier(so, q + "RTGIRayLength", 300f);
					Tier(so, q + "RTGIDenoise", true);
					Tier(so, q + "RTAORayLength", 4f);
					Tier(so, q + "RTAOSampleCount", 2);
				}
				so.ApplyModifiedPropertiesWithoutUndo();
				EditorUtility.SetDirty(asset);
			}

			// Quality level "Ultra RT" using the ray tracing asset.
			var qs = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0]);
			SerializedProperty levels = qs.FindProperty("m_QualitySettings");
			int found = -1;
			for (int i = 0; i < levels.arraySize; i++)
			{
				if (levels.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == "Ultra RT") found = i;
			}
			if (found < 0)
			{
				int hf = 0;
				for (int i = 0; i < levels.arraySize; i++) if (levels.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == "High Fidelity") hf = i;
				levels.InsertArrayElementAtIndex(hf);
				found = hf + 1;
				levels.MoveArrayElement(hf + 1, levels.arraySize - 1);
				found = levels.arraySize - 1;
				levels.GetArrayElementAtIndex(found).FindPropertyRelative("name").stringValue = "Ultra RT";
			}
			SerializedProperty level = levels.GetArrayElementAtIndex(found);
			SerializedProperty rp = level.FindPropertyRelative("customRenderPipeline");
			if (rp != null) rp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(RtAsset);
			else Report.Add("customRenderPipeline missing");
			// Make sure the Windows player includes every level.
			qs.ApplyModifiedPropertiesWithoutUndo();
			AssetDatabase.SaveAssets();
			var names = string.Join(", ", QualitySettings.names);
			return "quality levels: " + names + (Report.Count > 0 ? " | missing: " + string.Join(", ", Report) : "");
		}

		private static void Bool(SerializedObject so, string path, bool v)
		{
			SerializedProperty p = so.FindProperty(path);
			if (p == null) { Report.Add(path); return; }
			p.boolValue = v;
		}

		// Sets the High entry (index 2) of a per-quality-tier array.
		private static void Tier(SerializedObject so, string path, object v)
		{
			SerializedProperty p = so.FindProperty(path);
			if (p == null || !p.isArray || p.arraySize < 3) { Report.Add(path); return; }
			SerializedProperty e = p.GetArrayElementAtIndex(2);
			if (v is bool b) e.boolValue = b;
			else if (v is int i) e.intValue = i;
			else e.floatValue = (float)v;
		}

		private static void Float(SerializedObject so, string path, float v)
		{
			SerializedProperty p = so.FindProperty(path);
			if (p == null) { Report.Add(path); return; }
			p.floatValue = v;
		}
	}
}
