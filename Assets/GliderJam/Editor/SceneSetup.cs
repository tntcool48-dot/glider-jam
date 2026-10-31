using GliderJam.Race;
using GliderJam.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace GliderJam.EditorTools
{
	// Wires the race into the level scene and rebuilds the menu / end scenes.
	public static class SceneSetup
	{
		private const string Scenes = "Assets/GliderLevel/Scenes/";
		public const string BackgroundPath = "Assets/GliderJam/Art/UI/menu_background.png";

		public static string InstallRace()
		{
			var go = GameObject.Find("/Race Manager") ?? new GameObject("Race Manager");
			var rm = go.GetComponent<RaceManager>();
			if (rm == null) rm = go.AddComponent<RaceManager>();
			rm.fx = AssetDatabase.LoadAssetAtPath<FxLibrary>(FxBuilder.LibraryPath);
			rm.route = Object.FindFirstObjectByType<RaceRoute>();
			EditorUtility.SetDirty(rm);
			EditorSceneManager.MarkSceneDirty(go.scene);
			EditorSceneManager.SaveOpenScenes();
			return "race manager: fx=" + (rm.fx != null) + " route=" + (rm.route != null);
		}

		public static string BuildFrontEnd()
		{
			string active = EditorSceneManager.GetActiveScene().path;
			EditorSceneManager.SaveOpenScenes();
			var bg = AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundPath);
			Build("mainUI", typeof(MainMenuScreen), bg);
			Build("end", typeof(EndScreen), bg);
			EditorSceneManager.OpenScene(active);
			return "front end rebuilt (background " + (bg != null) + ")";
		}

		private static void Build(string name, System.Type screen, Texture2D bg)
		{
			var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
			var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
			camGo.AddComponent<Camera>();
			camGo.AddComponent<AudioListener>();
			var hd = camGo.AddComponent<HDAdditionalCameraData>();
			hd.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
			hd.backgroundColorHDR = new Color(0.02f, 0.025f, 0.04f);
			new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
			var ui = new GameObject(name == "end" ? "End Screen" : "Main Menu");
			var s = (MenuScreen)ui.AddComponent(screen);
			s.background = bg;
			EditorSceneManager.SaveScene(scene, Scenes + name + ".unity");
		}
	}
}
