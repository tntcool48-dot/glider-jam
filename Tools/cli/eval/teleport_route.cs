// Play mode: put player 1's glider on the race line at path index /*I*/, level, heading along the line.
var route = UnityEngine.Object.FindFirstObjectByType<GliderJam.Race.RaceRoute>();
GliderJam.Flight.GliderController g = null;
foreach (var c in UnityEngine.Object.FindObjectsByType<GliderJam.Flight.GliderController>(UnityEngine.FindObjectsSortMode.None)) if (c.PlayerIndex == 0) g = c;
int i = UnityEngine.Mathf.Clamp(/*I*/, 0, route.path.Length - 4);
var pos = route.path[i];
var fwd = UnityEngine.Vector3.ProjectOnPlane(route.path[i + 3] - pos, UnityEngine.Vector3.up).normalized;
var rb = g.GetComponent<UnityEngine.Rigidbody>();
rb.position = pos; g.transform.position = pos;
var rot = UnityEngine.Quaternion.LookRotation(fwd);
rb.rotation = rot; g.transform.rotation = rot; g.SetOrientation(rot);
rb.linearVelocity = fwd * g.Speed;
var cam = UnityEngine.Object.FindFirstObjectByType<GliderJam.Flight.FollowCamera>();
if (cam != null) cam.Snap();
return "teleported to " + pos + " of " + route.path.Length;
