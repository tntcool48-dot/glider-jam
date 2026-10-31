// Click a uGUI Button by GameObject name (e.g. "SETTINGS Button") in the running game.
foreach (var b in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(UnityEngine.FindObjectsSortMode.None))
    if (b.name == "/*NAME*/") { b.onClick.Invoke(); return "clicked " + b.name; }
return "not found: /*NAME*/";
