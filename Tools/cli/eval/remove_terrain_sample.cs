// Removes the Shader Graph "Terrain Shaders" sample (imported for evaluation; its graphs fail to import in 6.3).
bool a = UnityEditor.AssetDatabase.DeleteAsset("Assets/Samples/Shader Graph");
return "deleted Shader Graph samples: " + a;
