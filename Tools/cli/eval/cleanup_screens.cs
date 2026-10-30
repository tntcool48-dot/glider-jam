bool removed = UnityEditor.AssetDatabase.IsValidFolder("Assets/Screenshots") && UnityEditor.AssetDatabase.DeleteAsset("Assets/Screenshots");
return "removed Assets/Screenshots: " + removed;
