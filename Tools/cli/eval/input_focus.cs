var s = UnityEngine.InputSystem.InputSystem.settings;
var before = s.editorInputBehaviorInPlayMode + "/" + s.backgroundBehavior;
s.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
s.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
return "before=" + before + " after=" + s.editorInputBehaviorInPlayMode + "/" + s.backgroundBehavior + " settingsAsset=" + UnityEditor.AssetDatabase.GetAssetPath(s);
