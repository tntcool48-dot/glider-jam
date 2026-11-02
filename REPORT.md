# Glider Jam: report for the "go a little crazy" round and the city / canyon polish pass

Everything lives on F: (project `F:\glider_jam\GliderJam`, player build `F:\glider_jam\Build\GliderJam.exe`,
benchmark logs `F:\glider_jam\Build\Benchmarks`). Unity 6000.3.18f1, HDRP 17.3, D3D12, tested on an
RX 7900 XT / Ryzen 7 9700X.

## After the first playtests (latest)

- Respawns follow your own path. Each glider remembers where it flew in the last 20 s (only spots with nothing
  within 14 m). After a crash it comes back at the newest of those at least 1.2 s before the crash, never ahead
  of it. It faces whichever of its old heading and turns of 20 or 40 degrees has the most open air, with at least
  200 m of it. Respawning on the race line let a crash skip the maze.
- No respawn traps: crashing again at the same spot soon after a respawn sends you further back each time (more
  open air required, and behind the previous respawn), up to four times. Tested in the canyon with a glider that
  can't steer: it kept getting past the pillar it had hit instead of looping.
- No more flying through walls after a respawn: the glider used to be a ghost for 2.5 s (colliders as triggers)
  and passed through pillars. It's now a 0.4 s blink, and respawn spots are checked for room. The city's round
  towers also got exact 24-sided colliders; the capsules left their lowest stretch only partly solid.
- City finish line at neon brightness (it was tuned for the canyon's daylight, about 13x too bright at night).
- Menu and loading:
  - The title screen no longer loads the whole canyon behind it for a live flyover (seconds of loading freezes,
    a busy GPU, laggy input). It shows rendered stills of the maps with a slow zoom and drift, crossfading when
    you switch maps; about 390 fps in the editor.
  - Every scene change goes through a loading screen (the map picture, its name, a progress bar, a tip). It loads
    in the background at high priority and stays up through the new scene's first heavy frames, then fades out.
    Before, the game simply froze with no feedback.
  - Opening settings takes about 40 ms and applying a setting under 1 ms; the lag came from the flyover.

## Meetup polish

- City routes: no dead ends, but no long straights either.
  - Each cross street splits the avenues into groups that are connected along it, and every group keeps at least
    one open avenue ahead (two for groups of five or more), with at least 3 per row. From any intersection there
    is a way forward without turning back.
  - An avenue that has been open for two blocks closes on the next unless a group needs it, so you keep cutting
    across. Off the race line no straight is longer than 3 blocks.
  - 147 blocking buildings plus 37 gates: a building over the street with a 44 x 38 m tunnel through it, neon
    portal frames and a lit interior. Gates are the risky way to keep going straight. Tested: straight through a
    tunnel is clean, the wall next to it is a crash.
- Stunts pay out boost (boost is speed):
  - Threading a gate tunnel all the way through: +50% boost and a 2 s free speed burst ("THREADED IT!").
  - Flying under a skybridge: +25% boost and a short burst ("UNDER THE BRIDGE").
  - Knife edge: a close call with the wings past 65 degrees refills boost faster still.
  - Tested in a live race: both callouts fire and the burst takes the glider to boost speed with no input.
- City side limits moved out to the far kerb of the two outer avenues. Before, flying in the outer half of those
  streets set off "OFF COURSE".
- Wrong way only warns: an amber "WRONG WAY / THE FINISH IS BEHIND YOU" with a soft beep, no countdown, no forced
  turn, no crash. The only limit is 250 m behind the start line, where the course ends: the glider is turned around
  there (it never breaks up for flying backwards).
- Boost you earn:
  - Close calls: within 14 m of a wall, building or the ground, boost refills about 6x faster.
  - Slipstream (split-screen): within 80 m behind the other glider, nearly dead ahead, refills it too.
  - "CLOSE CALL / SLIPSTREAM +BOOST" shows over the boost bar, which glows. The countdown hint tells players how to
    earn boost.
- City brightness: fixed exposure EV 3.0 -> 1.4. Building surfaces, lit windows and the city-glow sky now show
  without ray tracing; the neon still reads as neon, and ray tracing on top isn't blown out.
- Weaker PCs:
  - Auto resolution (Settings > Display, on by default): measures GPU frame time and lowers the internal
    resolution (down to 50%, upscaled with TAA upscale) when the GPU can't hold 60 fps, raising it again when it
    can. Render Scale is the ceiling; a fast PC stays at 100%. The FPS counter shows the current resolution when
    it is below 100%.
  - First launch picks a preset from the graphics card: integrated graphics or under 3 GB of video memory get Low,
    under 7 GB Medium, the rest High. This only happens without a settings file. To test that on another PC, copy
    the build without `Build\Settings\` (it holds your own settings).

## What changed, by request

| You asked for | Status | Where |
|---|---|---|
| Settings for everything, volume to graphics | Done: Graphics / Display / Audio / Controls tabs, live apply, saved to `Build\Settings\settings.json` (Editor: `UserSettings/GliderJamSettings.json`) | `Scripts/Core/GameConfig.cs`, `QualityManager.cs`, `UI/SettingsPanel.cs` |
| Presets Low / Medium / High (+ more) | Done: Low, Medium, High (default), Ultra, Ultra + Ray Tracing; touching any graphics row switches to Custom | `GameConfig.ApplyPreset` |
| Ray tracing | Done, own HDRP asset "Ultra RT". Ultra + Ray Tracing ray traces reflections, GI and AO in the city; the canyon keeps ray traced reflections only, and none in split-screen (see "Ray tracing") | `Settings/HDRP Ultra RT.asset`, `QualityManager` |
| Two modes, solo and split | Main menu: SOLO RACE, SPLIT-SCREEN, MAP choice | `UI/MainMenuScreen.cs` |
| Your control scheme (W/S pitch, A/D turn with roll to a 90 degree cap; arrows mirror for P2) | Done, constant speed. Boost / brake: P1 Space / Left Shift, P2 Right Shift / Right Ctrl (next to the arrows); each player's keys show under the race countdown; all rebindable | `Flight/GliderController.cs`, `GliderInput.cs`, `GameConfig.DefaultPlayers` |
| Rebindable keys, invert pitch, FOV per player, controllers | Done: rebinding with swap on conflict, per-player invert / FOV / vibration; pad 1 = P1, pad 2 = P2 (solo: any pad); left stick fly, RT boost, LT brake, Start pause; rumble on crashes, boost, the finish and warnings | `GliderInput.cs`, `SettingsPanel.cs` |
| Ceiling: don't raise it, explode after 1-2 s above it | Done: 2 s countdown with red vignette, "PULL DOWN", alarm and rumble, then the glider breaks up | `GliderController`, `PlayerHUD`, `RaceManager` |
| No map, no checkpoints: a straight race, everyone finds their own way, warn when going the wrong way | Done, see "Free-route racing" | `Race/RaceManager.cs`, `RaceRoute.cs` |
| Night city with neon, wet streets, rain; then cyberpunk buildings with reflections | Done, see "Neon City" | `Editor/CityBuilder.cs`, `Scenes/City.unity`, `World/CityWeather.cs` |
| City: more reflective, higher-quality buildings, windows, mirror skyscrapers, ray tracing that shows off | Done: mirror-glass towers (silver-blue and bronze) are now the most common skyscraper, round glass towers, chrome fins and spires, polished stone plazas, wetter streets, lit windows with interiors; ray tracing tuned so reflections keep the neon colour (see "Neon City" and "Ray tracing") | `Editor/CityBuilder.cs`, `QualityManager`, `Editor/QualitySetup.cs` |
| Make both maps harder (city was a straight line) | Done: city maze under a lower ceiling, extra spires and fins in the canyon | `CityBuilder`, `Editor/CanyonSetup.cs` |
| Bug: the glider clips into terrain and only dies on the way out | Fixed (see "Crash fix") | `Flight/GliderController.cs` |
| Glider: wing flex, vapor at high G, canopy reflections, panel lines (no decals, no instruments) | Done | `Flight/WingFlex.cs`, `GliderVisuals.cs`, `Editor/GliderBuilder.cs` |
| Free music | 3 tracks by Kevin MacLeod (CC BY 4.0), cross-faded: menu, canyon, city | `Audio/`, `MUSIC_CREDITS.md` |
| Great-looking UI | UI kit (Rajdhani font, rounded panels, accent bars), settings screen, pause menu, warnings, finish marker, map name on the results; the results screen shows the city after a city race (a ray traced render of the game) | `Scripts/UI/`, `Resources/CityBackground.png` |
| FPS counter, top left | Settings > Display > FPS COUNTER: frame rate and frame time, green 60+ / yellow 30+ / red below, in menus and races | `UI/FpsCounter.cs` |
| Outer terrain: keep it, optimize, measure | Kept. No shadows, no colliders, one LOD in the ray tracing scene; "Distant landscape" setting (Off / Low / High). Costs nothing measurable (below) | `World/MeshTerrain.cs` |
| Make the canyon look better | Done: river over the valley floor, seven waterfalls, circling bird flocks, sunlit dust motes, drifting 3D-noise mist, 42 extra spires / fins; then ivy curtains on the cliffs, a denser forest on the high ground, a second (darker) rock in bands, and separate cumulus clouds instead of a flat overcast | `World/CanyonAmbience.cs`, `Editor/CanyonSetup.cs`, `Editor/TerrainArtSetup.cs` |

## Free-route racing

- Each race goes one way (+Z): start at one end, finish by crossing a line of gold light that spans the whole
  course. No checkpoint rings and no minimap; take any line you like.
- HUD: distance to go, a finish marker straight ahead (with an arrow when it's off screen), the other player.
- Wrong way: heading back towards the start for 0.6 s shows an amber "WRONG WAY" notice, nothing more. Only
  250 m behind the start line, where the course ends, is the glider turned around.
- Off course: the sides of each course (canyon corridor, city core; the scenery beyond has no collision) count
  like the ceiling: "OFF COURSE", 2 s, then the glider breaks up.
- Crash: respawn on the known-clear race line a little behind the crash. After you finish, a crash just plays out.
- The old race line is kept for the autopilot (benchmarks, menu flyover) and respawns.

## Neon City

- 11 x 26 block downtown: towers with podiums and setbacks, a third of the glass towers chamfered (octagonal),
  floor ledges on hab blocks, vertical fins on curtain walls, glowing glass spires, roof furniture and antennas.
- Mirror skyscrapers (the most common tall tower now): silver-blue and bronze mirror glass, fully metallic and
  98.5% smooth, on a fine mullion grid. Each pane is tilted a little in the normal map, so reflections ripple from
  pane to pane like real curtain walls instead of one flat mirror; faint offices and a light band every 10-12
  floors show through the glass.
- Round glass towers (faceted, with capsule colliders), octagonal towers, chrome fins and chrome spires, polished
  stone (veined slabs) on plazas, podium tops and sidewalks, wetter asphalt with bigger puddles.
- Other facades: tinted glass with LED bands every 5-8 floors (cyan / magenta), offices and dense hab blocks;
  lit windows now have a ceiling light and furniture silhouettes; normal maps on all of them.
- Neon blade signs (some flicker), rooftop outlines, rings, podium bands, corner strips, 40 holograms incl. tall
  scrolling glyph banners, about 170 point lights.
- Street traffic (tail lights going your way, headlights coming at you) and six lanes of air cars crossing between
  the towers, all as light streaks from one particle system.
- Wet asphalt with puddles, lane markings, lamps, skybridges, the finish over the harbor, far-shore skyline and
  bay bridge (Distant landscape setting), night sky with city glow, volumetric fog, rain per player view.
- Harder: ceiling 150 m and every tower at least 165 m, so you can't fly over the blocks. Buildings spanning the
  street close 147 street segments and 37 more are fly-through gates, plus skybridges at mixed heights. There are
  no dead ends and no long straights (see "Meetup polish"). The race line stays open and is checked for clearance.
- Regenerate with `Tools/cli/eval/build_city.cs` (about 45 s).

## Canyon

- Ivy: 1,135 curtains (about 12,000 strands) hanging from cliff lips along the corridor. Each curtain faces down the
  slope and each strand is re-fitted to the wall every 2.5 m, so it drapes over the rock instead of sticking out or
  sinking in. Generated leaf texture, alpha cut, double-sided, no shadows, not in the ray tracing scene; hidden when
  Vegetation is at its lowest setting.
- Forest: 7,077 more trees in clumps on flat high ground (above 110 m), the same tree types as before, so they
  collide like the rest. Re-running the canyon setup replaces them rather than adding more.
- Rock: a second, darker rock (Poly Haven "dark_rock") in strata bands on the cliffs, pillars and spires, mixed
  with the sandstone, rubble and moss layers.
- Clouds: the cloud base sat at 330 m, just above the 288 m ceiling, and from the canyon floor it read as one flat
  purple overcast. The base is now at 800 m with more shape and erosion: separate cumulus with sky between them.
- Regenerate with `Tools/cli/eval/canyon_setup.cs` (about a minute).

## Crash fix

At 110-145 m/s the glider moves 2-3 m per physics step, and Unity's terrain (heightfield) collider can let a fast
body slip under its surface; from inside, the terrain is invisible and the glider only "hit" on the way out. Now
every step sweeps the fuselage along its motion and crashes on the first surface, and a glider below the terrain
surface (e.g. still inside a pillar when respawn invulnerability ends) crashes too. Tested: level into a cliff,
shallow dive into a slope, vertical dive: all stop at the surface. Crash debris and the other glider never count.

## Ray tracing

- Most of the canyon is lit by the sun and sky, which look the same either way, so ray tracing showed little
  there. The city is where it pays off: mirror towers reflect each other, the neon and the wet streets, including
  what's off screen, which screen-space reflections can't do.
- Per map:
  - City: ray traced reflections, GI and AO.
  - Canyon: ray traced reflections only. Ray traced GI / AO on the four-layer terrain material were very heavy
    (single digits in split-screen, and twice the AMD driver failed to build the ray tracing pipeline: out of
    memory, `0x8007000E`). Terrain chunks put one LOD each into the ray tracing scene.
  - Canyon in split-screen: falls back to screen-space effects automatically (reflections alone ran at 4.6 fps).
- Reflection tuning, chosen from side-by-side renders of the same city views:
  - Performance mode, full resolution, 1 bounce, rays up to 2 km, down to wet-asphalt smoothness (0.45).
    Quality mode looked darker and noisier for the same cost.
  - The firefly clamp is raised from 1 to 10: at 1, reflected neon came out grey. At 30, a chrome bridge underside
    over neon flared into an orange hot spot, so the underside is plain metal now.
- Ultra without ray tracing is still the recommended top setting for split-screen.

## Benchmarks (1920x1080 fullscreen, autopilot race, vsync off)

Meetup build (tighter city maze with gates and stunt zones, brighter city, close calls and slipstream). Logs
`Build\Benchmarks\v6_*`, `v7_*`:

| run | avg fps | 1% low | GPU ms | CPU ms | frames < 60 fps |
|---|---|---|---|---|---|
| city split Low | 376.5 | 52.6 | 2.06 | 2.31 | 0.8% |
| city split Medium | 217.1 | 87.7 | 3.80 | 4.34 | 0.1% |
| city split High | 145.0 | 35.7 | 6.17 | 4.77 | 1.8% |
| city split Ultra RT, fixed resolution | 62.4 | 33.9 | 15.04 | 8.85 | 28.2% |
| city split Ultra RT, auto resolution (average 83%) | 69.6 | 31.4 | 13.24 | 9.01 | 14.0% |
| canyon split Low | 289.6 | 40.4 | 2.57 | 2.89 | 0.9% |
| canyon split Medium | 167.4 | 47.5 | 4.65 | 5.78 | 0.4% |
| canyon split High | 131.6 | 52.3 | 6.44 | 7.19 | 0.3% |

- Auto resolution does its job on the heaviest case: city split-screen with ray tracing went from 62 to 70 fps
  and the frames under 60 halved. It can't help where the CPU is the limit (canyon solo with ray tracing: 13 ms
  of CPU, so it stayed at about 100%).
- The close-call check first cost the canyon about 0.7 ms of CPU; checking every other physics step and touching
  the HUD only when it changes brought it back to within 2%.
- Benchmarks run at a fixed resolution unless `-set autoResolution=1` is passed, and the summary reports the
  average and lowest scale when it is on.

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

Harder maps, Ultra RT = ray traced reflections only (before the mirror-glass city):

| run | avg fps | 1% low | GPU ms | CPU ms | frames < 60 fps |
|---|---|---|---|---|---|
| canyon split High | 139.5 | 29.2 | 6.09 | 6.58 | 0.8% |
| city split High (maze) | 151.6 | 56.0 | 5.87 | 4.58 | 1.1% |
| canyon split Ultra RT (automatic screen-space fallback) | 88.4 | 71.6 | 11.17 | 7.44 | 0.2% |
| city split Ultra RT (RT reflections) | 74.2 | 34.6 | 12.67 | 7.67 | 12.8% |
| canyon solo Ultra RT (RT reflections) | 58.8 | 11.9 | 11.36 | 10.63 | 35.7% |

Latest build (mirror-glass city with all ray traced effects in Ultra RT; canyon with ivy, extra trees, dark rock
and the new clouds). Logs `Build\Benchmarks\v4_*` and `v5_*`:

| run | avg fps | 1% low | GPU ms | CPU ms | frames < 60 fps |
|---|---|---|---|---|---|
| canyon split Low | 304.9 | 48.5 | 2.39 | 2.74 | 0.6% |
| canyon split Medium | 176.7 | 60.8 | 4.35 | 5.38 | 0.3% |
| canyon split High | 133.4 | 31.9 | 6.09 | 6.71 | 0.7% |
| canyon split Ultra | 85.5 | 77.4 | 11.60 | 7.67 | 0.0% |
| canyon split Ultra RT (automatic screen-space fallback) | 85.3 | 77.5 | 11.59 | 7.77 | 0.0% |
| canyon solo Ultra | 107.9 | 96.6 | 9.18 | 4.41 | 0.0% |
| canyon solo Ultra RT (RT reflections) | 62.8 | 13.3 | 11.46 | 10.68 | 30.1% |
| city split High | 157.0 | 55.5 | 5.84 | 4.12 | 0.2% |
| city split Ultra | 89.3 | 38.9 | 11.12 | 4.40 | 9.0% |
| city split Ultra RT (all RT effects) | 64.3 | 32.7 | 15.15 | 7.97 | 17.4% |
| city solo Ultra | 123.7 | 84.0 | 8.00 | 2.47 | 0.0% |
| city solo Ultra RT (all RT effects) | 86.2 | 61.3 | 11.48 | 4.33 | 0.1% |

- The canyon additions cost 3-5% (split High 139.5 -> 133.4, split Ultra 90.3 -> 85.5).
- Split-screen 1080p holds 60 fps on High on both maps, which is the default. On Ultra both maps average 85+, but the
  city has dips (9% of frames under 60, mostly in the densest blocks).
- Ray tracing: the city in solo is the showcase and stays above 60 (86 fps with all three ray traced effects).
  City split-screen with ray tracing averages 64 with dips. The canyon solo run averages 63 but stalls while the ray
  tracing scene warms up in the first seconds (1% low 13).

## Downloads (this round)

- Music: Kevin MacLeod / incompetech.com, "Floating Cities", "Volatile Reaction", "Cyborg Ninja" (MP3, about 16 MB,
  CC BY 4.0, credited in the main menu and `MUSIC_CREDITS.md`).
- Font: Rajdhani Bold / SemiBold (SIL Open Font License, `Art/UI/Fonts/OFL.txt`).
- Rock textures from Poly Haven (CC0), 2K sets in `F:\glider_jam\_downloads\polyhaven`:
  - "dark_rock" (10.7 MB): used as the second rock.
  - "rock_face_03" (13.7 MB) and "rock_wall_08" (diffuse only, about 2.9 MB): tried and dropped. The first was
    too brown next to the sandstone, the second looked like brickwork.
- Everything else (city textures, glider panels, bird sheet, mist noise, rain, spires, ivy) is generated by code.

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
- The player occasionally hangs right after loading the canyon (see "Not done"); `bench.ps1` kills and retries
  such a run and keeps the hung run's log.
- Editor screenshots exaggerate motion blur (low, uneven Editor frame rate); judge blur in the build.
- `capture game_view --save_path` is relative to `Assets/` and rejects `..`, so saved captures land inside the
  project (an `Assets/Screenshots` folder that then has to be moved out and deleted).
- Right after `recompile` the Editor reloads scripts and the next command can fail with "No Pipeline instance
  found"; `unity status --until-ready` before continuing avoids it.
- In an eval, `VolumeManager.instance.stack` shows HDRP's default values, not what a camera renders with, which
  briefly made the canyon clouds look switched off. Comparing renders with a setting on and off was reliable.

Ideas for the CLI
- A `capture camera` that renders a given position / rotation without writing a C# snippet.
- An input command that targets the Input System device the game reads, with focus handling.
- Play-mode evals that can wait N frames / seconds inside one call.

## Not done / known issues

- The rare hang right after the canyon loads in the player is not fixed. The terrain meshes now build on their own
  threads instead of .NET's thread pool, and each load logs its timing. Since then there was 1 hang in 20 canyon
  launches (8 benchmark runs plus 12 quick launches), against 2 in about 35 before. The hung run's log was
  overwritten by the retry; `bench.ps1` now keeps it (`<run>.hang1.log`) for the next time it happens.
- "A physically bigger canyon" was left as is: the free-route course is already 1 km wide and 7 km long, and the
  new spires, ivy and trees fill it. Making it bigger means regenerating the terrain and the race line.
- City split-screen on Ultra / Ultra RT dips under 60 in the densest blocks (see Benchmarks); High is smooth.
- Builds log one harmless TMP error: the dynamic UI font's glyph cache is cleared for the build
  (`MissingReferenceException ... m_AtlasTextures`); text renders normally in the player.
