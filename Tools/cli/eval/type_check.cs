var t = System.Type.GetType("yajaber.SimpleSceneMenu, Assembly-CSharp");
var src = System.IO.File.ReadAllText("Assets/GliderLevel/Scripts/Singleton.cs").Contains("FindFirstObjectByType");
return "SimpleSceneMenu type: " + (t != null) + " | Singleton fixed on disk: " + src + " | asset known: " + (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.MonoScript>("Assets/GliderLevel/Scripts/SimpleSceneMenu.cs") != null);
