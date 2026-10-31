// Regenerate the glider prefabs (meshes, panel textures, materials) and enable frame timing stats for benchmarks.
UnityEditor.PlayerSettings.enableFrameTimingStats = true;
var r = GliderJam.EditorTools.GliderBuilder.Build();
UnityEditor.AssetDatabase.SaveAssets();
return r + " frameTimingStats=" + UnityEditor.PlayerSettings.enableFrameTimingStats;
