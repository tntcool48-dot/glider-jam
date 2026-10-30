var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
var p = UnityEngine.GameObject.Find("/player");
if (p == null) return "scene=" + scene + " frame=" + UnityEngine.Time.frameCount + " (no player)";
var rb = p.GetComponent<UnityEngine.Rigidbody>();
var ctl = p.GetComponent<yajaber.GliderSurface_Controller>();
var rend = p.GetComponentInChildren<UnityEngine.Renderer>();
int explosions = 0;
foreach (var ps in UnityEngine.Object.FindObjectsByType<UnityEngine.ParticleSystem>(UnityEngine.FindObjectsSortMode.None)) if (ps.name.StartsWith("Particle System")) explosions++;
return "scene=" + scene + " frame=" + UnityEngine.Time.frameCount + " t=" + UnityEngine.Time.time.ToString("F2")
    + " pos=" + p.transform.position.ToString("F0") + " euler=" + p.transform.eulerAngles.ToString("F1")
    + " speed=" + rb.linearVelocity.magnitude.ToString("F1") + " kinematic=" + rb.isKinematic
    + " pitchIn=" + ctl.PitchInput.ToString("F2") + " yawIn=" + ctl.YawInput.ToString("F2")
    + " rendererOn=" + rend.enabled + " explosions=" + explosions
    + " fps=" + (1f / UnityEngine.Time.smoothDeltaTime).ToString("F0");
