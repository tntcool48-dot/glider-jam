using UnityEngine;
using UnityEngine.SceneManagement;

namespace GliderJam.Audio
{
	// Persistent music: menu track in the front end, race tracks in the race scenes, cross-faded.
	// Clips come from Resources/GliderJamMusic (a MusicLibrary asset); without it the game is simply silent.
	public class MusicPlayer : MonoBehaviour
	{
		private AudioSource _a, _b;
		private MusicLibrary _lib;
		private AudioClip _wanted;
		private float _fade = 1f;
		private bool _aActive = true;

		private void Awake()
		{
			_lib = Resources.Load<MusicLibrary>("GliderJamMusic");
			_a = Make();
			_b = Make();
			SceneManager.activeSceneChanged += (from, to) => Choose(to.name);
		}

		private AudioSource Make()
		{
			var s = gameObject.AddComponent<AudioSource>();
			s.loop = true;
			s.spatialBlend = 0f;
			s.playOnAwake = false;
			s.ignoreListenerPause = true;
			s.volume = 0f;
			return s;
		}

		private void Start()
		{
			Choose(SceneManager.GetActiveScene().name);
		}

		private void Choose(string scene)
		{
			if (_lib == null) return;
			bool race = scene == GameSettings.RaceScene || scene == GameSettings.CityScene;
			AudioClip clip;
			if (GameSettings.Showcase || scene == GameSettings.MenuScene || scene == GameSettings.EndScene) clip = _lib.menu;
			else if (scene == GameSettings.CityScene && _lib.city != null) clip = _lib.city;
			else clip = race ? _lib.race : _lib.menu;
			if (clip == null || clip == _wanted) return;
			_wanted = clip;
			_aActive = !_aActive;
			AudioSource next = _aActive ? _a : _b;
			next.clip = clip;
			next.time = 0f;
			next.Play();
			_fade = 0f;
		}

		private void Update()
		{
			float target = GameConfig.Current.musicVolume * 0.6f;
			_fade = Mathf.MoveTowards(_fade, 1f, Time.unscaledDeltaTime / 2f);
			AudioSource on = _aActive ? _a : _b, off = _aActive ? _b : _a;
			on.volume = target * _fade;
			off.volume = target * (1f - _fade);
			if (_fade >= 1f && off.isPlaying) off.Stop();
		}
	}
}
