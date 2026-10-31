// Play mode: put player 1's glider at a position with a heading (yaw degrees, 0 = +Z), level flight.
GliderJam.Flight.GliderController g = null;
foreach (var c in UnityEngine.Object.FindObjectsByType<GliderJam.Flight.GliderController>(UnityEngine.FindObjectsSortMode.None)) if (c.PlayerIndex == 0) g = c;
var pd = g.GetComponent<GliderJam.Flight.PlayerDriver>(); if (pd != null) pd.enabled = false;
g.inputTurn = 0f; g.inputPitch = 0f;
var pos = new UnityEngine.Vector3(/*POS*/);
var rot = UnityEngine.Quaternion.Euler(0f, /*YAW*/, 0f);
var rb = g.GetComponent<UnityEngine.Rigidbody>();
rb.position = pos; g.transform.position = pos; rb.rotation = rot; g.transform.rotation = rot; g.SetOrientation(rot);
rb.linearVelocity = rot * UnityEngine.Vector3.forward * g.Speed;
var cam = UnityEngine.Object.FindFirstObjectByType<GliderJam.Flight.FollowCamera>(); if (cam != null) cam.Snap();
return "at " + pos + " yaw " + /*YAW*/;
