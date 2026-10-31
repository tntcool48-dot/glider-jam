using GliderJam.Race;
using UnityEngine;

namespace GliderJam.Flight
{
	// Feeds keyboard / gamepad input into a glider.
	public class PlayerDriver : MonoBehaviour
	{
		public int player;
		public bool solo;
		private GliderInput _input;
		private GliderController _glider;

		private void Start()
		{
			_glider = GetComponent<GliderController>();
			_input = new GliderInput(player, solo);
		}

		private void Update()
		{
			_input.Update(Time.deltaTime);
			_glider.inputPitch = _input.Pitch;
			_glider.inputTurn = _input.Turn;
			_glider.inputBoost = _input.Boost;
			_glider.inputBrake = _input.Brake;
		}
	}
}
