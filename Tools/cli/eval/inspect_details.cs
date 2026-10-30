var sb = new System.Text.StringBuilder();
System.Func<string, UnityEngine.GameObject> find = p => UnityEngine.GameObject.Find(p);
var ceiling = find("/ceiling");
sb.Append("ceiling renderer enabled=").Append(ceiling.GetComponent<UnityEngine.MeshRenderer>().enabled).Append(" mesh=").Append(ceiling.GetComponent<UnityEngine.MeshFilter>().sharedMesh?.name).Append("\n");
var sphere = find("/portal/Sphere");
sb.Append("finish sphere renderer enabled=").Append(sphere.GetComponent<UnityEngine.MeshRenderer>().enabled).Append("\n");
var light = find("/Directional Light").GetComponent<UnityEngine.Light>();
sb.Append("light intensity=").Append(light.intensity).Append(" color=").Append(light.color).Append(" rot=").Append(light.transform.eulerAngles).Append(" shadows=").Append(light.shadows).Append(" useColorTemp=").Append(light.useColorTemperature).Append(" lightUnit=").Append(light.lightUnit).Append("\n");
var cam = find("/Main Camera").GetComponent<UnityEngine.Camera>();
sb.Append("camera fov=").Append(cam.fieldOfView).Append(" near=").Append(cam.nearClipPlane).Append(" far=").Append(cam.farClipPlane).Append(" clear=").Append(cam.clearFlags).Append("\n");
var player = find("/player");
var rb = player.GetComponent<UnityEngine.Rigidbody>();
sb.Append("player rb mass=").Append(rb.mass).Append(" kinematic=").Append(rb.isKinematic).Append(" gravity=").Append(rb.useGravity).Append(" interp=").Append(rb.interpolation).Append(" collision=").Append(rb.collisionDetectionMode).Append(" rot=").Append(player.transform.eulerAngles).Append("\n");
foreach (var mb in player.GetComponents<UnityEngine.MonoBehaviour>())
{
    var so = new UnityEditor.SerializedObject(mb);
    var it = so.GetIterator(); it.NextVisible(true);
    sb.Append("  ").Append(mb.GetType().Name).Append(": ");
    while (it.NextVisible(false))
    {
        string v = it.propertyType == UnityEditor.SerializedPropertyType.ObjectReference ? (it.objectReferenceValue == null ? "null" : it.objectReferenceValue.name)
                 : it.propertyType == UnityEditor.SerializedPropertyType.Float ? it.floatValue.ToString()
                 : it.propertyType == UnityEditor.SerializedPropertyType.Enum ? it.enumDisplayNames[it.enumValueIndex]
                 : it.propertyType == UnityEditor.SerializedPropertyType.Boolean ? it.boolValue.ToString() : it.propertyType.ToString();
        sb.Append(it.name).Append("=").Append(v).Append("; ");
    }
    sb.Append("\n");
}
foreach (var src in player.GetComponents<UnityEngine.AudioSource>())
    sb.Append("  audio clip=").Append(src.clip == null ? "null" : src.clip.name).Append(" loop=").Append(src.loop).Append(" playOnAwake=").Append(src.playOnAwake).Append(" vol=").Append(src.volume).Append("\n");
foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.Terrain>(UnityEngine.FindObjectsSortMode.None))
{
    var d = t.terrainData;
    var layers = new System.Collections.Generic.List<string>();
    foreach (var l in d.terrainLayers) layers.Add(l == null ? "null" : l.name);
    sb.Append(t.name).Append(" data=").Append(d.name).Append(" layers=[").Append(string.Join(",", layers)).Append("] alphamapLayers=").Append(d.alphamapLayers).Append(" alphaRes=").Append(d.alphamapResolution).Append(" pixelError=").Append(t.heightmapPixelError).Append(" drawInstanced=").Append(t.drawInstanced).Append("\n");
}
var prof = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>("Assets/Settings/SkyandFogSettingsProfile.asset");
if (prof != null) { sb.Append("SkyandFog components: "); foreach (var c in prof.components) sb.Append(c.GetType().Name).Append(c.active ? "" : "(off)").Append(", "); sb.Append("\n"); }
var def = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>("Assets/Settings/HDRPDefaultResources/DefaultSettingsVolumeProfile.asset");
if (def != null) { sb.Append("Default profile components: "); foreach (var c in def.components) sb.Append(c.GetType().Name).Append(c.active ? "" : "(off)").Append(", "); sb.Append("\n"); }
var ps = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/GliderLevel/GameObject/Particle System.prefab");
if (ps != null) { sb.Append("explosion prefab children: "); foreach (var r in ps.GetComponentsInChildren<UnityEngine.Renderer>(true)) sb.Append(r.name).Append("<").Append(r.sharedMaterial == null ? "null" : r.sharedMaterial.name).Append(">, "); sb.Append("\n"); }
return sb.ToString();
