return System.Environment.GetEnvironmentVariable("UPM_CACHE_ROOT")
    + " | ASSETSTORE=" + System.Environment.GetEnvironmentVariable("ASSETSTORE_CACHE_PATH")
    + " | TEMP=" + System.IO.Path.GetTempPath()
    + " | GI=" + UnityEditor.EditorPrefs.GetBool("GICacheEnableCustomPath") + " " + UnityEditor.EditorPrefs.GetString("GICacheFolder")
    + " | log=" + UnityEngine.Application.consoleLogPath
    + " | pipeline=" + UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
