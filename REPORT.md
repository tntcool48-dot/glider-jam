# Glider Jam: report for the "go a little crazy" round

Everything lives on F: (project `F:\glider_jam\GliderJam`, player build `F:\glider_jam\Build\GliderJam.exe`,
benchmark logs `F:\glider_jam\Build\Benchmarks`). Unity 6000.3.18f1, HDRP 17.3, D3D12, tested on an
RX 7900 XT / Ryzen 7 9700X.

## What changed, by request

| You asked for | Status | Where |
|---|---|---|
| Settings for everything, volume to graphics | Done: Graphics / Display / Audio / Controls tabs, live apply, saved to `Build\Settings\settings.json` (Editor: `UserSettings/GliderJamSettings.json`) | `Scripts/Core/GameConfig.cs`, `QualityManager.cs`, `UI/SettingsPanel.cs` |
| Presets Low / Medium / High (+ more) | Done: Low, Medium, High (default), Ultra, Ultra + Ray Tracing; touching any graphics row switches to Custom | `GameConfig.ApplyPreset` |
| Ray tracing | Done, own HDRP asset "Ultra RT". The Ultra + Ray Tracing preset now ray traces reflections only (see "Ray tracing" below); ray traced AO and GI stay selectable under Custom | `Settings/HDRP Ultra RT.asset`, `QualityManager` |
| Two modes, solo and split | Main menu: SOLO RACE, SPLIT-SCREEN, MAP choice | `UI/MainMenuScreen.cs` |
| Your control scheme (W/S pitch, A/D turn with roll to a 90 degree cap; arrows mirror for P2) | Done, constant speed. Boost / brake: P1 Space / Left Shift, P2 Right Shift / Right Ctrl (next to the arrows); each player's keys show under the race countdown; all rebindable | `Flight/GliderController.cs`, `GliderInput.cs`, `GameConfig.DefaultPlayers` |
| Rebindable keys, invert pitch, FOV per player, controllers | Done: rebinding with swap on conflict, per-player invert / FOV / vibration; pad 1 = P1, pad 2 = P2 (solo: any pad); left stick fly, RT boost, LT brake, Start pause; rumble on crashes, boost, the finish and warnings | `GliderInput.cs`, `SettingsPanel.cs` |
| Ceiling: don't raise it, explode after 1-2 s above it | Done: 2 s countdown with red vignette, "PULL DOWN", alarm and rumble, then the glider breaks up | `GliderController`, `PlayerHUD`, `RaceManager` |
| No map, no checkpoints: a straight race, everyone finds their own way, warn when going the wrong way | Done, see "Free-route racing" | `Race/RaceManager.cs`, `RaceRoute.cs` |
| Night city with neon, wet streets, rain; then cyberpunk buildings with reflections | Done, see "Neon City" | `Editor/CityBuilder.cs`, `Scenes/City.unity`, `World/CityWeather.cs` |
| Make both maps harder (city was a straight line) | Done: city maze under a lower ceiling, extra spires and fins in the canyon | `CityBuilder`, `Editor/CanyonSetup.cs` |
| Bug: the glider clips into terrain and only dies on the way out | Fixed (see "Crash fix") | `Flight/GliderController.cs` |
| Glider: wing flex, vapor at high G, canopy reflections, panel lines (no decals, no instruments) | Done | `Flight/WingFlex.cs`, `GliderVisuals.cs`, `Editor/GliderBuilder.cs` |
| Free music | 3 tracks by Kevin MacLeod (CC BY 4.0), cross-faded: menu, canyon, city | `Audio/`, `MUSIC_CREDITS.md` |
| Great-looking UI | UI kit (Rajdhani font, rounded panels, accent bars), settings screen, pause menu, warnings, finish marker, map name on the results | `Scripts/UI/` |
| FPS counter, top left | Settings > Display > FPS COUNTER: frame rate and frame time, green 60+ / yellow 30+ / red below, in menus and races | `UI/FpsCounter.cs` |
| Outer terrain: keep it, optimize, measure | Kept. No shadows, no colliders, one LOD in the ray tracing scene; "Distant landscape" setting (Off / Low / High). Costs nothing measurable (below) | `World/MeshTerrain.cs` |
| Make the canyon look better | Done: river over the valley floor, seven waterfalls, circling bird flocks, sunlit dust motes, drifting 3D-noise mist, sharper cumulus, 42 extra spires / fins. Not done: vines, denser pillar-top forest, second rock texture | `World/CanyonAmbience.cs`, `Editor/CanyonSetup.cs` |

## Free-route racing

- Each race goes one way (+Z): start at one end, finish by crossing a line of gold light that spans the whole
  course. No checkpoint rings and no minimap; take any line you like.
- HUD: distance to go, a finish marker straight ahead (with an arrow when it's off screen), the other player.
- Wrong way: heading back towards the start for 0.6 s shows "WRONG WAY / TURN AROUND" with a 3 s countdown,
  then the glider is turned around. Sideways is fine.
- Off course: the sides of each course (canyon corridor, city core; the scenery beyond has no collision) and the
  area behind the start count like the ceiling: "OFF COURSE", 2 s, then the glider breaks up.
- Crash: respawn on the known-clear race line a little behind the crash. After you finish, a crash just plays out.
- The old race line is kept for the autopilot (benchmarks, menu flyover) and respawns.

## Neon City

- 11 x 26 block downtown: towers with podiums and setbacks, a third of the glass towers chamfered (octagonal),
  floor ledges on hab blocks, vertical fins on curtain walls, glowing glass spires, roof furniture and antennas.
- Facades: mirror glass (light metallic, 97% smooth) that reflects the neon and the city glow, LED light bands
  every 5-8 floors (cyan / magenta), dense hab blocks with coloured windows and grime, normal maps on all of them.
- Neon blade signs (some flicker), rooftop outlines, rings, podium bands, corner strips, 40 holograms incl. tall
  scrolling glyph banners, about 170 point lights.
- Street traffic (tail lights going your way, headlights coming at you) and six lanes of air cars crossing between
  the towers, all as light streaks from one particle system.
- Wet asphalt with puddles, lane markings, lamps, skybridges, the finish over the harbor, far-shore skyline and
  bay bridge (Distant landscape setting), night sky with city glow, volumetric fog, rain per player view.
- Harder: ceiling 150 m and every tower at least 165 m, so you can't fly over the blocks; about a third of the
  avenue segments and a quarter of the cross streets are closed by buildings that span the street, plus
  skybridges at mixed heights. Wherever an avenue closes ahead, a side street stays open (a dead end would need a
  U-turn, which the wrong-way rule would undo). The race line stays open and is checked for clearance.
- Regenerate with `Tools/cli/eval/build_city.cs` (about 45 s).

## Crash fix

At 110-145 m/s the glider moves 2-3 m per physics step, and Unity's terrain (heightfield) collider can let a fast
body slip under its surface; from inside, the terrain is invisible and the glider only "hit" on the way out. Now
every step sweeps the fuselage along its motion and crashes on the first surface, and a glider below the terrain
surface (e.g. still inside a pillar when respawn invulnerability ends) crashes too. Tested: level into a cliff,
shallow dive into a slope, vertical dive: all stop at the surface. Crash debris and the other glider never count.

## Ray tracing

- Your observation matches the numbers: most of the scene is lit by the sun and sky, which look the same either
  way; ray tracing mainly sharpens reflections (river, wet streets, glass, canopy).
- On the canyon, ray traced GI / AO were very heavy: every pixel shoots rays that hit the terrain's four-layer
  material. Split-screen with all three ray traced effects fell to single digits, and twice the AMD driver failed to
  build the ray tracing pipeline (out of memory, `0x8007000E`). The city, with simpler materials, was fine.
- So Ultra + Ray Tracing now ray traces reflections only; terrain chunks put one LOD each into the ray tracing scene.
- Even reflections only were too much for the canyon in split-screen (4.6 fps), so that one combination falls back
  to screen-space effects automatically; solo races, the city and the menu flyover keep ray tracing.
  Ultra without ray tracing is the recommended top setting.

## Benchmarks (1920x1080 fullscreen, autopilot race, vsync off)

Full matrix, before the ray tracing change and the harder maps (`Build\Benchmarks\summary.md`):

| run | avg fps | 1% low | GPU ms | CPU ms | frames < 60 fps |
|---|---|---|---|---|---|
| canyon split Low | 312.6 | 50.8 | 2.32 | 2.67 | 0.5% |
| canyon split Medium | 182.5 | 52.9 | 4.16 | 5.19 | 0.3% |
| canyon split High (re-run) | 137.8 | 33.4 | 5.92 | 6.69 | 0.7% |
| canyon split Ultra | 90.3 | 77.7 | 10.82 | 7.36 | 0.0% |
| city split Low | 446.4 | 48.7 | 1.78 | 1.76 | 1.0% |
| city split Medium | 288.7 | 135.3 | 3.03 | 2.29 | 0.0% |
| city split High | 171.8 | 75.3 | 5.38 | 3.81 | 0.0% |
| city split Ultra | 94.5 | 46.4 | 10.50 | 4.33 | 4.5% |
| city split Ultra RT (all RT effects) | 70.1 | 38.7 | 14.09 | 6.89 | 16.7% |
| canyon solo Ultra | 109.8 | 73.5 | 9.00 | 4.14 | 0.1% |
| canyon solo Ultra RT (all RT effects) | 45.8 | 5.4 | 18.13 | 14.46 | 84.4% |
| city solo Ultra RT (all RT effects) | 97.9 | 74.0 | 10.10 | 3.50 | 0.0% |

Distant landscape A/B (canyon, split, High), two runs each: High 147.5 / 147.2 fps, Off 144.7 / 149.3 fps,
GPU 5.58 / 5.52 ms vs 5.54 / 5.33 ms: within noise. After the optimization the outer terrain costs nothing measurable.

Final build (harder maps, Ultra RT = ray traced reflections only):

| run | avg fps | 1% low | GPU ms | CPU ms | frames < 60 fps |
|---|---|---|---|---|---|
| canyon split High | 139.5 | 29.2 | 6.09 | 6.58 | 0.8% |
| city split High (maze) | 151.6 | 56.0 | 5.87 | 4.58 | 1.1% |
| canyon split Ultra RT (automatic screen-space fallback) | 88.4 | 71.6 | 11.17 | 7.44 | 0.2% |
| city split Ultra RT (RT reflections) | 74.2 | 34.6 | 12.67 | 7.67 | 12.8% |
| canyon solo Ultra RT (RT reflections) | 58.8 | 11.9 | 11.36 | 10.63 | 35.7% |

Split-screen 1080p holds 60 fps on High (and Ultra) on both maps. With ray tracing, only the city in solo stays
reliably above 60; the canyon solo run hitches while the ray tracing scene warms up (an 0.8 s stall in the first
seconds) and stays borderline.

## Downloads (this round)

- Music: Kevin MacLeod / incompetech.com, "Floating Cities", "Volatile Reaction", "Cyborg Ninja" (MP3, about 16 MB,
  CC BY 4.0, credited in the main menu and `MUSIC_CREDITS.md`).
- Font: Rajdhani Bold / SemiBold (SIL Open Font License, `Art/UI/Fonts/OFL.txt`).
- Everything else (city textures, glider panels, bird sheet, mist noise, rain, spires) is generated by code.

## C: drive

Nothing new was written to C: beyond what you already accepted (Unity / Hub prefs and licensing, CLI logs,
the GPU shader cache, this app's session files). Builds, logs, benchmarks, temp files: all on F:.

## Unity CLI notes

What worked well
- `unity command eval_file` is the workhorse: generators (city, glider, canyon pass), probes and play-mode
  helpers all run as C# files, results come back as JSON.
- `capture game_view` (base64 inline) for screenshots of the running game, `editor.playmode start/stop`,
  `console get/status/clear` (with `groundTruth.compilationFailed`), `recompile`, `scene open`, `build run` +
  `build status` for player builds (30 s to 3 min), `unity open --args` to reopen the Editor with its log on F:.

What didn't / workarounds
- PowerShell 5.1 mangles quotes in inline `unity command eval '...'`, so all C# goes through files.
- `simulate_key` does not reach `Keyboard.current` with the Editor unfocused: a virtual keyboard device is used,
  and UI is tested by invoking buttons by name (`click.ps1`) because held keys auto-repeat in menus.
- Builder meshes updated with `EditorUtility.CopySerialized` keep a stale GPU copy in the Editor (the build is
  fine): this hid the new wing UVs until meshes were copied through the Mesh API.
- `FindObjectsByType` skips objects with `HideFlags.DontSave` (the generated terrain chunks), so census evals
  can't see them.
- The player occasionally hangs right after loading the canyon (2 of ~35 benchmark launches, no error in the log);
  `bench.ps1` now kills and retries such a run.
- Editor screenshots exaggerate motion blur (low, uneven Editor frame rate); judge blur in the build.

Ideas for the CLI
- A `capture camera` that renders a given position / rotation without writing a C# snippet.
- An input command that targets the Input System device the game reads, with focus handling.
- Play-mode evals that can wait N frames / seconds inside one call.

## Not done / known issues

- Canyon: vines, a denser pillar-top forest and a second rock texture.
- The rare hang right after loading the canyon in the player (see above) is not diagnosed yet.
- The results screen always flies over the canyon, also after a city race.
- The editor logs one harmless TMP message during builds (fallback atlas of the default TMP font).
