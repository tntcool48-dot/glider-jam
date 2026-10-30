// Phase 1 placeholder scenes: "mainUI" (title + Fly/Quit) and "end" (finish + Restart/Menu).
var sb = new System.Text.StringBuilder();
string sceneDir = "Assets/GliderLevel/Scenes/";
var uiSprite = UnityEditor.AssetDatabase.GetBuiltinExtraResource<UnityEngine.Sprite>("UI/Skin/UISprite.psd");
var font = TMPro.TMP_Settings.defaultFontAsset;

System.Func<string, UnityEngine.Transform, UnityEngine.RectTransform> rect = (name, parent) =>
{
    var go = new UnityEngine.GameObject(name, typeof(UnityEngine.RectTransform));
    go.transform.SetParent(parent, false);
    return (UnityEngine.RectTransform)go.transform;
};
System.Func<UnityEngine.Transform, string, float, UnityEngine.Vector2, UnityEngine.Color, TMPro.TextMeshProUGUI> label = (parent, text, size, pos, col) =>
{
    var rt = rect("Text", parent);
    rt.anchorMin = rt.anchorMax = new UnityEngine.Vector2(0.5f, 0.5f);
    rt.sizeDelta = new UnityEngine.Vector2(1600, size * 1.4f);
    rt.anchoredPosition = pos;
    var t = rt.gameObject.AddComponent<TMPro.TextMeshProUGUI>();
    t.text = text; t.fontSize = size; t.color = col; t.alignment = TMPro.TextAlignmentOptions.Center;
    if (font != null) t.font = font;
    rt.gameObject.name = text.Length > 20 ? text.Substring(0, 20) : text;
    return t;
};

System.Func<string, string, string, string, string, string, string, bool> build = (sceneName, title, subtitle, b1Text, b1Scene, b2Text, b2Scene) =>
{
    var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);

    var camGO = new UnityEngine.GameObject("Main Camera");
    camGO.tag = "MainCamera";
    var cam = camGO.AddComponent<UnityEngine.Camera>();
    camGO.AddComponent<UnityEngine.AudioListener>();
    var hdCam = camGO.AddComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>();
    hdCam.clearColorMode = UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData.ClearColorMode.Color;
    hdCam.backgroundColorHDR = new UnityEngine.Color(0.02f, 0.03f, 0.05f);

    var es = new UnityEngine.GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));

    var canvasGO = new UnityEngine.GameObject("Canvas", typeof(UnityEngine.RectTransform), typeof(UnityEngine.Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
    canvasGO.GetComponent<UnityEngine.Canvas>().renderMode = UnityEngine.RenderMode.ScreenSpaceOverlay;
    var scaler = canvasGO.GetComponent<UnityEngine.UI.CanvasScaler>();
    scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
    scaler.referenceResolution = new UnityEngine.Vector2(1920, 1080);
    scaler.matchWidthOrHeight = 0.5f;

    var bg = rect("Background", canvasGO.transform);
    bg.anchorMin = UnityEngine.Vector2.zero; bg.anchorMax = UnityEngine.Vector2.one; bg.offsetMin = bg.offsetMax = UnityEngine.Vector2.zero;
    bg.gameObject.AddComponent<UnityEngine.UI.Image>().color = new UnityEngine.Color(0.03f, 0.05f, 0.08f, 1f);

    label(canvasGO.transform, title, 120, new UnityEngine.Vector2(0, 220), UnityEngine.Color.white);
    label(canvasGO.transform, subtitle, 40, new UnityEngine.Vector2(0, 110), new UnityEngine.Color(0.7f, 0.78f, 0.85f));

    var menu = canvasGO.AddComponent<yajaber.SimpleSceneMenu>();
    menu.defaultScene = b1Scene;

    var res = new TMPro.TMP_DefaultControls.Resources { standard = uiSprite };
    string[] texts = { b1Text, b2Text };
    string[] targets = { b1Scene, b2Scene };
    for (int i = 0; i < 2; i++)
    {
        var bGO = TMPro.TMP_DefaultControls.CreateButton(res);
        bGO.name = texts[i] + " Button";
        var brt = (UnityEngine.RectTransform)bGO.transform;
        brt.SetParent(canvasGO.transform, false);
        brt.anchorMin = brt.anchorMax = new UnityEngine.Vector2(0.5f, 0.5f);
        brt.sizeDelta = new UnityEngine.Vector2(420, 90);
        brt.anchoredPosition = new UnityEngine.Vector2(0, -60 - i * 120);
        bGO.GetComponent<UnityEngine.UI.Image>().color = i == 0 ? new UnityEngine.Color(0.95f, 0.55f, 0.15f) : new UnityEngine.Color(0.2f, 0.25f, 0.32f);
        var txt = bGO.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        txt.text = texts[i]; txt.fontSize = 40; txt.color = UnityEngine.Color.white;
        if (font != null) txt.font = font;
        var btn = bGO.GetComponent<UnityEngine.UI.Button>();
        if (targets[i] == "QUIT")
            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(btn.onClick, new UnityEngine.Events.UnityAction(menu.Quit));
        else
            UnityEditor.Events.UnityEventTools.AddStringPersistentListener(btn.onClick, new UnityEngine.Events.UnityAction<string>(menu.LoadScene), targets[i]);
    }
    bool ok = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, sceneDir + sceneName + ".unity");
    sb.Append(sceneName).Append(" saved=").Append(ok).Append("\n");
    return ok;
};

build("mainUI", "GLIDER JAM", "Placeholder menu. Press Enter or click Fly", "FLY", "yazan scene", "QUIT", "QUIT");
build("end", "FINISH!", "You flew through the finish gate", "RESTART", "yazan scene", "MENU", "mainUI");

UnityEditor.EditorBuildSettings.scenes = new[]
{
    new UnityEditor.EditorBuildSettingsScene(sceneDir + "mainUI.unity", true),
    new UnityEditor.EditorBuildSettingsScene(sceneDir + "yazan scene.unity", true),
    new UnityEditor.EditorBuildSettingsScene(sceneDir + "end.unity", true),
};
UnityEditor.SceneManagement.EditorSceneManager.OpenScene(sceneDir + "yazan scene.unity");
foreach (var s in UnityEditor.EditorBuildSettings.scenes) sb.Append("build: ").Append(s.path).Append("\n");
return sb.ToString();
