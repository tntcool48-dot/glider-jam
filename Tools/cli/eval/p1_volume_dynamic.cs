var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>("Assets/GliderLevel/Settings/YazanSkyAndFog.asset");
UnityEngine.Rendering.HighDefinition.VisualEnvironment ve;
profile.TryGet(out ve);
ve.skyAmbientMode.overrideState = true;
ve.skyAmbientMode.value = UnityEngine.Rendering.HighDefinition.SkyAmbientMode.Dynamic;
UnityEditor.EditorUtility.SetDirty(profile);
UnityEditor.AssetDatabase.SaveAssets();
return "skyAmbientMode=" + ve.skyAmbientMode.value;
