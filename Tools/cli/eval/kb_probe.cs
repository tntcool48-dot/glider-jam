var kb = UnityEngine.InputSystem.Keyboard.current;
var sb = new System.Text.StringBuilder();
sb.Append("kb=").Append(kb == null ? "null" : kb.name + "#" + kb.deviceId + " enabled=" + kb.enabled + " canRunInBackground=" + kb.canRunInBackground);
if (kb != null) sb.Append(" d=").Append(kb.dKey.isPressed).Append(" s=").Append(kb.sKey.isPressed).Append(" anyKey=").Append(kb.anyKey.isPressed);
sb.Append(" | devices:");
foreach (var d in UnityEngine.InputSystem.InputSystem.devices) sb.Append(" ").Append(d.name).Append("#").Append(d.deviceId).Append(d.enabled ? "" : "(off)");
sb.Append(" | legacy D=").Append(UnityEngine.Input.GetKey(UnityEngine.KeyCode.D));
return sb.ToString();
