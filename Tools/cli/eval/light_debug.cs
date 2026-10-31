var sun = UnityEngine.GameObject.Find("/Sun");
var l = sun.GetComponent<UnityEngine.Light>();
var hd = sun.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalLightData>();
var prof = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>("Assets/GliderJam/Settings/GoldenHour.asset");
prof.TryGet(out UnityEngine.Rendering.HighDefinition.VolumetricClouds clouds);
clouds.enable.value = /*CLOUDS*/;
var vols = UnityEngine.Object.FindObjectsByType<UnityEngine.Rendering.Volume>(UnityEngine.FindObjectsSortMode.None);
var names = new System.Collections.Generic.List<string>();
foreach (var v in vols) names.Add(v.name + (v.isGlobal ? "(global," : "(local,") + v.priority + "," + (v.sharedProfile ? v.sharedProfile.name : "null") + ")");
return "sun enabled=" + l.enabled + " active=" + sun.activeInHierarchy + " intensity=" + l.intensity + " unit=" + l.lightUnit + " fwd=" + sun.transform.forward
    + " temp=" + l.colorTemperature + " hd=" + (hd != null) + " | clouds=" + clouds.enable.value + " | volumes: " + string.Join("; ", names)
    + " | RenderSettings.sun=" + (UnityEngine.RenderSettings.sun ? UnityEngine.RenderSettings.sun.name : "null");
