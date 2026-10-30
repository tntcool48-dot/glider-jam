// Phase 1: convert the package's URP / Built-in materials to HDRP equivalents in place (GUIDs kept).
var sb = new System.Text.StringBuilder();
string matDir = "Assets/GliderLevel/Material/";
string sampleDir = "Assets/Samples/High Definition Render Pipeline/17.3.0/Particle System Shader Samples/Materials/";
var hdLit = UnityEngine.Shader.Find("HDRP/Lit");
System.Func<string, UnityEngine.Material> load = p => UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(p);
System.Func<string, UnityEngine.Texture2D> tex = p => UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(p);

// --- Opaque / transparent lit surfaces (values read from the original URP / Standard YAML) ---
var lit = new System.Collections.Generic.Dictionary<string, UnityEngine.Vector4>
{
    // name -> (r, g, b, smoothness); alpha handled separately for glass
    { "orange", new UnityEngine.Vector4(1f, 0.531f, 0f, 0.35f) },
    { "white", new UnityEngine.Vector4(1f, 1f, 1f, 0.5f) },
    { "black", new UnityEngine.Vector4(0.02f, 0.02f, 0.02f, 0.5f) },
    { "gray", new UnityEngine.Vector4(0.557f, 0.557f, 0.557f, 0.5f) },
    { "glider_armtex", new UnityEngine.Vector4(0.35f, 0.35f, 0.37f, 0.4f) },
    { "glider_fabric", new UnityEngine.Vector4(0.12f, 0.12f, 0.13f, 0.2f) },
    { "glider_guages", new UnityEngine.Vector4(0.05f, 0.05f, 0.05f, 0.6f) },
    { "glider_interior", new UnityEngine.Vector4(0.25f, 0.25f, 0.27f, 0.3f) },
    { "glider_leather", new UnityEngine.Vector4(0.10f, 0.08f, 0.07f, 0.45f) },
    { "glider_leatherlight", new UnityEngine.Vector4(0.30f, 0.24f, 0.20f, 0.45f) },
    { "glider_speedo", new UnityEngine.Vector4(0.05f, 0.05f, 0.05f, 0.7f) },
};
foreach (var kv in lit)
{
    var m = load(matDir + kv.Key + ".mat");
    if (m == null) { sb.Append("missing ").Append(kv.Key).Append("\n"); continue; }
    m.shader = hdLit;
    m.SetColor("_BaseColor", new UnityEngine.Color(kv.Value.x, kv.Value.y, kv.Value.z, 1f));
    m.SetFloat("_Metallic", 0f);
    m.SetFloat("_Smoothness", kv.Value.w);
    UnityEditor.Rendering.HighDefinition.HDShaderUtils.ResetMaterialKeywords(m);
    UnityEditor.EditorUtility.SetDirty(m);
    sb.Append("lit ").Append(kv.Key).Append("\n");
}
{
    var glass = load(matDir + "glass_0.mat");
    glass.shader = hdLit;
    glass.SetColor("_BaseColor", new UnityEngine.Color(0.43f, 0.97f, 1f, 0.12f));
    glass.SetFloat("_Metallic", 0f);
    glass.SetFloat("_Smoothness", 0.95f);
    glass.SetFloat("_SurfaceType", 1f);
    glass.SetFloat("_BlendMode", 0f);
    glass.SetFloat("_EnableBlendModePreserveSpecularLighting", 1f);
    UnityEditor.Rendering.HighDefinition.HDShaderUtils.ResetMaterialKeywords(glass);
    UnityEditor.EditorUtility.SetDirty(glass);
    sb.Append("transparent glass_0\n");
}

// --- Particles: HDRP particle Shader Graph samples ---
var unlit = load(sampleDir + "Unlit.mat");
var unlitSoft = load(sampleDir + "UnlitSoft.mat");
var unlitDist = load(sampleDir + "UnlitDistortion.mat");
System.Action<string, UnityEngine.Material, string, UnityEngine.Texture2D> toParticle = (name, sample, texProp, t) =>
{
    var m = load(matDir + name + ".mat");
    m.shader = sample.shader;
    m.CopyPropertiesFromMaterial(sample);
    if (t != null) m.SetTexture(texProp, t);
    UnityEditor.Rendering.HighDefinition.HDShaderUtils.ResetMaterialKeywords(m);
    UnityEditor.EditorUtility.SetDirty(m);
    sb.Append("particle ").Append(name).Append(" -> ").Append(sample.shader.name).Append("\n");
};
toParticle("Circle", unlit, "Texture2D_23DD87FD", tex("Assets/GliderLevel/Texture2D/Circle.png"));
toParticle("MagicCircle", unlit, "Texture2D_23DD87FD", tex("Assets/GliderLevel/Texture2D/MagicCircle.png"));
toParticle("Mask1", unlit, "Texture2D_23DD87FD", tex("Assets/GliderLevel/Texture2D/Mask1.png"));
toParticle("Point", unlitSoft, "Texture2D_8FEEBB68", tex("Assets/GliderLevel/Texture2D/Point1.png"));
toParticle("Smoke26", unlitSoft, "Texture2D_8FEEBB68", tex("Assets/GliderLevel/Texture2D/Smoke26.png"));
toParticle("explosion material", unlit, "Texture2D_23DD87FD", null);
{
    var m = load(matDir + "DistortionPoint.mat");
    m.shader = unlitDist.shader;
    m.CopyPropertiesFromMaterial(unlitDist);
    m.SetTexture("Texture2D_23DD87FD", tex("Assets/GliderLevel/Texture2D/Point1.png"));
    m.SetTexture("Texture2D_D1349F11", tex("Assets/GliderLevel/Texture2D/Point1n.png"));
    UnityEditor.Rendering.HighDefinition.HDShaderUtils.ResetMaterialKeywords(m);
    UnityEditor.EditorUtility.SetDirty(m);
    sb.Append("particle DistortionPoint -> ").Append(unlitDist.shader.name).Append("\n");
}
// Replacement for the built-in Default-ParticleSystem material (Particles/Standard Unlit does not render in HDRP)
string defPartPath = matDir + "DefaultParticle_HDRP.mat";
var defPart = load(defPartPath);
if (defPart == null)
{
    defPart = new UnityEngine.Material(unlit);
    defPart.name = "DefaultParticle_HDRP";
    UnityEditor.AssetDatabase.CreateAsset(defPart, defPartPath);
}

// --- Terrain ---
{
    var m = load(matDir + "TerrainLit.mat");
    m.shader = UnityEngine.Shader.Find("HDRP/TerrainLit");
    UnityEditor.Rendering.HighDefinition.HDShaderUtils.ResetMaterialKeywords(m);
    UnityEditor.EditorUtility.SetDirty(m);
    sb.Append("terrain TerrainLit -> HDRP/TerrainLit\n");
}
UnityEditor.AssetDatabase.SaveAssets();

// --- Scene fix-ups: renderers still on non-HDRP shaders, URP leftovers ---
int swapped = 0;
foreach (var r in UnityEngine.Object.FindObjectsByType<UnityEngine.Renderer>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None))
{
    var mats = r.sharedMaterials; bool changed = false;
    for (int i = 0; i < mats.Length; i++)
    {
        if (mats[i] != null && mats[i].shader != null && (mats[i].shader.name.StartsWith("Particles/") || mats[i].shader.name.StartsWith("Legacy Shaders/Particles")))
        { mats[i] = defPart; changed = true; swapped++; }
    }
    if (changed) r.sharedMaterials = mats;
}
sb.Append("swapped built-in particle materials on ").Append(swapped).Append(" renderers\n");
foreach (var path in new[] { "/Directional Light", "/Main Camera" })
{
    var go = UnityEngine.GameObject.Find(path);
    int removed = UnityEditor.GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
    sb.Append("removed ").Append(removed).Append(" missing URP component(s) from ").Append(path).Append("\n");
}
var cam = UnityEngine.GameObject.Find("/Main Camera").GetComponent<UnityEngine.Camera>();
cam.farClipPlane = 12000f;
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

// --- Report anything still not rendering in HDRP ---
foreach (var r in UnityEngine.Object.FindObjectsByType<UnityEngine.Renderer>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None))
    foreach (var m in r.sharedMaterials)
        if (m == null || m.shader == null || m.shader.name == "Hidden/InternalErrorShader" || m.shader.name == "Standard")
            sb.Append("STILL BAD: ").Append(r.name).Append(" -> ").Append(m == null ? "null" : m.name + "<" + m.shader.name + ">").Append(r.enabled ? "" : " (renderer disabled)").Append("\n");
return sb.ToString();
