// Hold keys on a virtual "AgentKeyboard" device. Edit KEYS below (empty = release all).
var KEYS = new UnityEngine.InputSystem.Key[] { /*KEYS*/ };
UnityEngine.InputSystem.Keyboard kb = null;
foreach (var d in UnityEngine.InputSystem.InputSystem.devices) if (d.name == "AgentKeyboard") kb = (UnityEngine.InputSystem.Keyboard)d;
if (kb == null) kb = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>("AgentKeyboard");
var state = new UnityEngine.InputSystem.LowLevel.KeyboardState(KEYS);
kb.MakeCurrent();
// Processed by the next frame's input update, so wasPressedThisFrame works in game code.
UnityEngine.InputSystem.InputSystem.QueueStateEvent(kb, state);
return "AgentKeyboard holds [" + string.Join(",", KEYS) + "] current=" + (UnityEngine.InputSystem.Keyboard.current == kb);
