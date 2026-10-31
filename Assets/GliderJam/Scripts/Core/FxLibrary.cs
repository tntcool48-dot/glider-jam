using UnityEngine;

namespace GliderJam
{
	// Assets the race needs at runtime (built by the FxBuilder editor tool).
	[CreateAssetMenu(menuName = "Glider Jam/Fx Library")]
	public class FxLibrary : ScriptableObject
	{
		public GameObject[] gliderPrefabs = new GameObject[2];
		public Material trail;
		public Material vapor;
		public Material wingVapor;
		public Material fire;
		public Material smoke;
		public Material spark;
		public Material ring;
		public Material ringFinish;
		public Material ringGlow;
		public AudioClip explosionClip;
		public AudioClip windClip;
		public AudioClip engineClip;
	}
}
