using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace GliderJam.Flight
{
	// Per-player controls (Input System, read directly from devices).
	// P1: W/S pitch, A/D roll, Space boost, Left Shift brake.  P2: arrows, Right Ctrl boost, Right Shift brake.
	// Optional gamepads: pad 1 -> P1, pad 2 -> P2 (solo: any pad). Left stick (pull back = nose up),
	// RT or A = boost, LT or B = brake. Keyboard W / Up = nose up, as in the original game.
	// Solo mode accepts both keyboard schemes.
	public class GliderInput
	{
		public float Pitch { get; private set; }   // +1 nose up
		public float Roll { get; private set; }    // +1 roll / turn right
		public bool Boost { get; private set; }
		public bool Brake { get; private set; }

		private readonly int _player;
		private readonly bool _solo;
		private float _kbPitch, _kbRoll;

		public GliderInput(int player, bool solo)
		{
			_player = player;
			_solo = solo;
		}

		public void Update(float dt)
		{
			float pitchTarget = 0f, rollTarget = 0f;
			bool boost = false, brake = false;

			// All keyboards (real + virtual test devices).
			foreach (InputDevice d in InputSystem.devices)
			{
				if (!(d is Keyboard k) || !k.enabled)
				{
					continue;
				}
				if (_solo || _player == 0)
				{
					pitchTarget += Key(k.wKey) - Key(k.sKey);
					rollTarget += Key(k.dKey) - Key(k.aKey);
					boost |= k.spaceKey.isPressed;
					brake |= k.leftShiftKey.isPressed;
				}
				if (_solo || _player == 1)
				{
					pitchTarget += Key(k.upArrowKey) - Key(k.downArrowKey);
					rollTarget += Key(k.rightArrowKey) - Key(k.leftArrowKey);
					boost |= k.rightCtrlKey.isPressed;
					brake |= k.rightShiftKey.isPressed;
				}
			}
			// Keys ramp in for smooth digital control.
			_kbPitch = Mathf.MoveTowards(_kbPitch, Mathf.Clamp(pitchTarget, -1f, 1f), dt * 4f);
			_kbRoll = Mathf.MoveTowards(_kbRoll, Mathf.Clamp(rollTarget, -1f, 1f), dt * 5f);
			float pitch = _kbPitch, roll = _kbRoll;

			for (int i = 0; i < Gamepad.all.Count; i++)
			{
				if (!_solo && i != _player)
				{
					continue;
				}
				Gamepad g = Gamepad.all[i];
				Vector2 stick = g.leftStick.ReadValue();
				float sy = -stick.y; // pull back = nose up
				if (Mathf.Abs(sy) > 0.12f && Mathf.Abs(sy) > Mathf.Abs(pitch)) pitch = sy;
				if (Mathf.Abs(stick.x) > 0.12f && Mathf.Abs(stick.x) > Mathf.Abs(roll)) roll = stick.x;
				boost |= g.rightTrigger.ReadValue() > 0.3f || g.buttonSouth.isPressed;
				brake |= g.leftTrigger.ReadValue() > 0.3f || g.buttonEast.isPressed;
			}
			Pitch = Mathf.Clamp(pitch, -1f, 1f);
			Roll = Mathf.Clamp(roll, -1f, 1f);
			Boost = boost;
			Brake = brake && !boost;
		}

		private static float Key(KeyControl k)
		{
			return k.isPressed ? 1f : 0f;
		}
	}
}
