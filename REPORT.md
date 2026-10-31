# Glider Jam: report for the "go a little crazy" round

Everything lives on F: (project `F:\glider_jam\GliderJam`, player build `F:\glider_jam\Build\GliderJam.exe`,
benchmark logs `F:\glider_jam\Build\Benchmarks`). Unity 6000.3.18f1, HDRP 17.3, D3D12.

## What changed, by request

| You asked for | Status | Where |
|---|---|---|
| Settings for everything, volume to graphics | Done: Graphics / Display / Audio / Controls tabs, live apply, saved to `Build\Settings\settings.json` (Editor: `UserSettings/GliderJamSettings.json`) | `Scripts/Core/GameConfig.cs`, `QualityManager.cs`, `UI/SettingsPanel.cs` |
| Presets Low / Medium / High (+ more) | Done: Low, Medium, High (default), Ultra, Ultra + Ray Tracing; touching any graphics row switches to Custom | `GameConfig.ApplyPreset` |
| Ray tracing | Done: ray traced AO, reflections and GI per setting, own HDRP asset "Ultra RT". Works in split-screen and solo | `Settings/HDRP Ultra RT.asset`, `QualityManager` |
| Cranked settings that look better than before | Ultra: 4 cascades at 2.6 km, ultra clouds (80 steps), ultra fog, high TAA, denser and farther vegetation, full terrain LODs; Ultra RT adds the ray traced effects | `GameConfig` table |
| Two modes, solo and split | Main menu: SOLO RACE, SPLIT-SCREEN, MAP choice | `UI/MainMenuScreen.cs` |
| Your control scheme (W/S pitch, A/D turn with roll to a 90 degree cap; arrows mirror for P2) | Done, constant speed; boost/brake on Space/LShift and RCtrl/RShift | `Flight/GliderController.cs`, `GliderInput.cs` |
| Rebindable keys, invert pitch, FOV per player, controllers | Done: rebinding with swap on conflict, per-player invert / FOV / vibration; pad 1 = P1, pad 2 = P2 (solo: any pad); left stick fly, RT boost, LT brake, Start pause; rumble on checkpoints, crashes, boost and the ceiling alarm | `GliderInput.cs`, `SettingsPanel.cs` |
| Ceiling: don't raise it, explode after 1-2 s above it | Done: 2 s countdown with red vignette, "PULL DOWN", alarm beeps and rumble, then the glider breaks up ("OUT OF BOUNDS") | `GliderController`, `PlayerHUD`, `RaceManager` |
| Night city with neon, wet streets, a bit of rain | Done: "Neon City", see below | `Editor/CityBuilder.cs`, `Scenes/City.unity`, `World/CityWeather.cs` |
| Glider: wing flex, vapor at high G, canopy reflections, panel lines (no decals, no instruments) | Done | `Flight/WingFlex.cs`, `GliderVisuals.cs`, `Editor/GliderBuilder.cs` |
| Free music | 3 tracks by Kevin MacLeod (CC BY 4.0), cross-faded: menu, canyon, city | `Audio/`, `MUSIC_CREDITS.md` |
| Great-looking UI | New UI kit (Rajdhani font, rounded panels, accent bars), settings screen, pause menu, out-of-bounds warning, full-route minimap, map name on the results | `Scripts/UI/` |
| Outer terrain: keep it, optimize, measure | Kept. Outer chunks never cast shadows and have no colliders; "Distant landscape" setting (Off / Low / High). Measured below | `World/MeshTerrain.cs` |
| Make the canyon look better | Done: river over the valley floor, seven waterfalls spilling off pillar lips beside the race line, circling bird flocks, sunlit dust motes, drifting 3D-noise mist, sharper cumulus. Not done: vines, denser pillar-top forest, second rock texture (see "Not done") | `World/CanyonAmbience.cs`, `Editor/CanyonSetup.cs` |

## Neon City

- Procedural 11 x 26 block downtown (towers with podiums, setbacks, roof furniture and antennas), four
  generated facade sets with lit windows (blinds, curtains, warm / cool / colored light), mask and emissive maps.
- Neon blade signs with glyph strokes (some flicker), rooftop outlines, rings, podium bands, corner strips,
  three animated billboards, about 150 point lights (street lamps, signs, under the skybridges).
- Wet asphalt with puddles (smoothness mask + normal map) that reflect the neon through SSR or ray tracing,
  lane markings, street lamps.
- Three skybridges across the race line (two to fly under, one to fly over), the finish over the harbor,
  a far-shore skyline and a bay bridge (part of the Distant landscape setting).
- Gradient night sky with city glow, moonlight, volumetric fog, bloom. Rain is spawned per player camera on that
  player's view layer, so split-screen views never see each other's rain; density follows Effects.
- 6.7 km race line with 14 checkpoints. The generator checks the line for clearance (spheres at the centre and
  at the split-screen offsets); both autopilots finish it without crashing.
- Regenerate with `Tools/cli/eval/build_city.cs` (about 40 s).

## Benchmarks (1920x1080 fullscreen, autopilot race, vsync off)

The full matrix is in `F:\glider_jam\Build\Benchmarks\summary.md` (written when the suite finishes; per-run logs alongside).
First results: canyon split-screen Low 312.6 fps avg (1% low 50.8), Medium 182.5 fps (1% low 52.9).
The canyon split High run wrote no log (check `canyon_split_high.log`; rerun with `Tools\cli\bench.ps1 -Name canyon_split_high -Preset High`).
Earlier build, split-screen High: about 130 fps average.

## Downloads (this round)

- Music: Kevin MacLeod / incompetech.com, "Floating Cities", "Volatile Reaction", "Cyborg Ninja" (MP3, about 16 MB,
  CC BY 4.0, credited in the main menu and `MUSIC_CREDITS.md`).
- Font: Rajdhani Bold / SemiBold (SIL Open Font License, `Art/UI/Fonts/OFL.txt`).
- Everything else (city textures, glider panels, bird sheet, mist noise, rain) is generated by code.

## C: drive

Nothing new was written to C: beyond what you already accepted (Unity / Hub prefs and licensing, CLI logs,
the GPU shader cache, this app's session files). Builds, logs, benchmarks, temp files: all on F:.

## Unity CLI notes

What worked well
- `unity command eval_file` is the workhorse: generators (city, glider, canyon pass), probes and play-mode
  helpers all run as C# files, results come back as JSON.
- `capture game_view` (base64 inline) for screenshots of the running game, `editor.playmode start/stop`,
  `console get/status/clear` (with `groundTruth.compilationFailed`), `recompile`, `scene open`, `build run` +
  `build status` for player builds (about 2-3 minutes).

What didn't / workarounds
- PowerShell 5.1 mangles quotes in inline `unity command eval '...'`, so all C# goes through files.
- `simulate_key` does not reach `Keyboard.current` with the Editor unfocused: a virtual keyboard device is used,
  and UI is tested by invoking buttons by name (`click.ps1`) because held keys auto-repeat in menus.
- Builder meshes updated with `EditorUtility.CopySerialized` keep a stale GPU copy in the Editor (the build is
  fine): this hid the new wing UVs until meshes were copied through the Mesh API. Worth knowing for any tool that
  rewrites mesh assets in place.
- A new player started while the previous one is still shutting down can hang at load: `bench.ps1` waits for
  the old process first.
- Editor screenshots exaggerate motion blur (low, uneven Editor frame rate); judge blur in the build.

Ideas for the CLI
- A `capture camera` that renders a given position / rotation without writing a C# snippet.
- An input command that targets the Input System device the game reads, with focus handling.
- Play-mode evals that can wait N frames / seconds inside one call.

## Not done / known issues

- Canyon: vines, a denser pillar-top forest and a second rock texture were not built this round
  (river, waterfalls, birds, dust, mist and clouds came first).
- Ray tracing does not include the instanced trees or volumetric clouds in the acceleration structure; shadows stay
  raster (PCSS).
- The results screen always flies over the canyon, also after a city race.
- The editor still logs one harmless TMP message during builds (fallback atlas of the default TMP font).
