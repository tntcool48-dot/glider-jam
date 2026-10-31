using UnityEngine;

namespace GliderJam.Audio
{
	// Music tracks (see Resources/GliderJamMusic.asset and MUSIC_CREDITS.md for sources / licenses).
	[CreateAssetMenu(menuName = "Glider Jam/Music Library")]
	public class MusicLibrary : ScriptableObject
	{
		public AudioClip menu;
		public AudioClip race;
		public AudioClip city;
	}
}
