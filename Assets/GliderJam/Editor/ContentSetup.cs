using GliderJam.Audio;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace GliderJam.EditorTools
{
	// Music library (Resources/GliderJamMusic) and the UI font (Resources/GliderJamFont, Rajdhani Bold, OFL).
	public static class ContentSetup
	{
		private const string Res = "Assets/GliderJam/Resources";

		public static string Run()
		{
			System.IO.Directory.CreateDirectory(Res);
			string m = "Assets/GliderJam/Audio/Music/";
			foreach (string clip in new[] { "FloatingCities", "VolatileReaction", "CyborgNinja" })
			{
				var ai = (AudioImporter)AssetImporter.GetAtPath(m + clip + ".mp3");
				AudioImporterSampleSettings s = ai.defaultSampleSettings;
				s.loadType = AudioClipLoadType.Streaming;
				s.compressionFormat = AudioCompressionFormat.Vorbis;
				s.quality = 0.7f;
				ai.defaultSampleSettings = s;
				ai.SaveAndReimport();
			}
			var lib = AssetDatabase.LoadAssetAtPath<MusicLibrary>(Res + "/GliderJamMusic.asset");
			if (lib == null)
			{
				lib = ScriptableObject.CreateInstance<MusicLibrary>();
				AssetDatabase.CreateAsset(lib, Res + "/GliderJamMusic.asset");
			}
			lib.menu = AssetDatabase.LoadAssetAtPath<AudioClip>(m + "FloatingCities.mp3");
			lib.race = AssetDatabase.LoadAssetAtPath<AudioClip>(m + "VolatileReaction.mp3");
			lib.city = AssetDatabase.LoadAssetAtPath<AudioClip>(m + "CyborgNinja.mp3");
			EditorUtility.SetDirty(lib);

			string fontPath = Res + "/GliderJamFont.asset";
			if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath) == null)
			{
				var ttf = AssetDatabase.LoadAssetAtPath<Font>("Assets/GliderJam/Art/UI/Fonts/Rajdhani-Bold.ttf");
				TMP_FontAsset fa = TMP_FontAsset.CreateFontAsset(ttf, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
				fa.name = "GliderJamFont";
				AssetDatabase.CreateAsset(fa, fontPath);
				fa.material.name = "GliderJamFont Material";
				AssetDatabase.AddObjectToAsset(fa.material, fa);
				fa.atlasTextures[0].name = "GliderJamFont Atlas";
				AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
				// Pre-populate the glyphs the game uses; anything else falls back to Liberation Sans.
				fa.TryAddCharacters(" !\"#%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]_abcdefghijklmnopqrstuvwxyz|°", out string missing);
				if (TMP_Settings.defaultFontAsset != null) fa.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> { TMP_Settings.defaultFontAsset };
				EditorUtility.SetDirty(fa);
			}
			AssetDatabase.SaveAssets();
			return "music library + font ok";
		}
	}
}
