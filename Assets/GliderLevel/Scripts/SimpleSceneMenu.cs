using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace yajaber
{
	// Placeholder menu logic for the "mainUI" and "end" scenes (Phase 1).
	// Buttons call LoadScene / Quit; Enter triggers the default scene, Escape quits.
	public class SimpleSceneMenu : MonoBehaviour
	{
		[Tooltip("Scene loaded when Enter is pressed.")]
		public string defaultScene = "yazan scene";

		private void Start()
		{
			Time.timeScale = 1f;
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
		}

		private void Update()
		{
			Keyboard keyboard = Keyboard.current;
			if (keyboard == null)
			{
				return;
			}
			if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
			{
				LoadScene(defaultScene);
			}
			else if (keyboard.escapeKey.wasPressedThisFrame)
			{
				Quit();
			}
		}

		public void LoadScene(string sceneName)
		{
			SceneManager.LoadScene(sceneName);
		}

		public void Quit()
		{
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#else
			Application.Quit();
#endif
		}
	}
}
