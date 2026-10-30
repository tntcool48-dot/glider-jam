var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var cam = UnityEngine.GameObject.Find("/Main Camera");
var light = UnityEngine.GameObject.Find("/Directional Light");
int missingCam = UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(cam);
int missingLight = UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(light);
var s = "scene=" + scene.path + " dirty=" + scene.isDirty + " camFar=" + cam.GetComponent<UnityEngine.Camera>().farClipPlane + " missing(cam,light)=" + missingCam + "," + missingLight;
if (missingCam > 0 || missingLight > 0)
{
    UnityEditor.GameObjectUtility.RemoveMonoBehavioursWithMissingScript(cam);
    UnityEditor.GameObjectUtility.RemoveMonoBehavioursWithMissingScript(light);
}
cam.GetComponent<UnityEngine.Camera>().farClipPlane = 12000f;
bool saved = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return s + " saved=" + saved;
