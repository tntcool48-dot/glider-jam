// The original package's TMP font copies live in a Resources folder (always built) but are unused; move them out.
string err = UnityEditor.AssetDatabase.MoveAsset("Assets/GliderLevel/Resources", "Assets/GliderLevel/Fonts");
return string.IsNullOrEmpty(err) ? "moved Assets/GliderLevel/Resources -> Assets/GliderLevel/Fonts" : "error: " + err;
