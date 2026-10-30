using UnityEngine;

namespace yajaber
{
	public class GliderEngine_Controller : MonoBehaviour
	{
		public float Thrust => 120f;

		private void Update()
		{
			if (Singleton<GliderSurface_Controller>.Instance != null)
			{
				Singleton<GliderSurface_Controller>.Instance.GetInputs();
				Singleton<GliderSurface_Controller>.Instance.PlaneRotations();
			}
		}
	}
}
