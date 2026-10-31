using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace GliderJam.Flight
{
	// Per-player controls (Input System, read directly from devices; keys are rebindable in GameConfig).
	// Defaults: P1 W/S pitch, A/D turn, Space boost, L-Shift brake. P2 arrows, R-Ctrl boost, R-Shift brake.
	// Gamepads: pad 1 -> P1, pad 2 -> P2 (solo: any pad). Left stick (pull back = nose up), RT/A boost, LT/B brake.
	// Solo mode also accepts player 2's keys.
	public class GliderInput
	{
		public float Pitch { get; private set; }   // +1 nose up
		public float Turn { get; private set; }    // +1 turn right
		public bool Boost { get; private set; }
		public bool Brake { get; private set; }

		private readonly int _player;
		private readonly bool _solo;
		private float _kbPitch, _kbTurn;

		public GliderInput(int player, bool solo)
		{
			_player = player;
			_solo = solo;
		}

		public static Gamepad PadFor(int player, bool solo)
		{
			if (Gamepad.all.Count == 0) return null;
			if (solo) return Gamepad.current;
			return player < Gamepad.all.Count ? Gamepad.all[player] : null;
		}

		public void Update(float dt)
		{
			GameConfig cfg = GameConfig.Current;
			float pitchTarget = 0f, turnTarget = 0f;
			bool boost = false, brake = false;
			foreach (InputDevice d in InputSystem.devices)
			{
				if (!(d is Keyboard k) || !k.enabled) continue;
				for (int p = 0; p < 2; p++)
				{
					if (!_solo && p != _player) continue;
					Key[] keys = cfg.players[p].keys;
					pitchTarget += Down(k, keys[(int)ControlAction.PitchUp]) - Down(k, keys[(int)ControlAction.PitchDown]);
					turnTarget += Down(k, keys[(int)ControlAction.TurnRight]) - Down(k, keys[(int)ControlAction.TurnLeft]);
					boost |= Down(k, keys[(int)ControlAction.Boost]) > 0f;
					brake |= Down(k, keys[(int)ControlAction.Brake]) > 0f;
				}
			}
			// Keys ramp in for smooth digital control.
			_kbPitch = Mathf.MoveTowards(_kbPitch, Mathf.Clamp(pitchTarget, -1f, 1f), dt * 4.5f);
			_kbTurn = Mathf.MoveTowards(_kbTurn, Mathf.Clamp(turnTarget, -1f, 1f), dt * 5f);
			float pitch = _kbPitch, turn = _kbTurn;

			Gamepad g = PadFor(_player, _solo);
			if (g != null)
			{
				Vector2 stick = g.leftStick.ReadValue();
				float sy = -stick.y; // pull back = nose up
				if (Mathf.Abs(sy) > 0.12f && Mathf.Abs(sy) > Mathf.Abs(pitch)) pitch = sy;
				if (Mathf.Abs(stick.x) > 0.12f && Mathf.Abs(stick.x) > Mathf.Abs(turn)) turn = stick.x;
				boost |= g.rightTrigger.ReadValue() > 0.3f || g.buttonSouth.isPressed;
				brake |= g.leftTrigger.ReadValue() > 0.3f || g.buttonEast.isPressed;
			}
			if (cfg.players[Mathf.Clamp(_player, 0, 1)].invertPitch) pitch = -pitch;
			Pitch = Mathf.Clamp(pitch, -1f, 1f);
			Turn = Mathf.Clamp(turn, -1f, 1f);
			Boost = boost;
			Brake = brake && !boost;
		}

		private static float Down(Keyboard k, Key key)
		{
			if (key == Key.None) return 0f;
			KeyControl c = k[key];
			return c != null && c.isPressed ? 1f : 0f;
		}

		// Esc on any keyboard or Start on any gamepad.
		public static bool PausePressed()
		{
			foreach (InputDevice d in InputSystem.devices)
			{
				if (d is Keyboard k && k.escapeKey.wasPressedThisFrame) return true;
				if (d is Gamepad g && g.startButton.wasPressedThisFrame) return true;
			}
			return false;
		}

		public static void Rumble(int player, bool solo, float low, float high)
		{
			Gamepad g = PadFor(player, solo);
			if (g == null) return;
			float v = GameConfig.Current.players[Mathf.Clamp(player, 0, 1)].vibration;
			g.SetMotorSpeeds(low * v, high * v);
		}
	}
}
