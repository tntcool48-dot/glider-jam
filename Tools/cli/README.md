# Unity CLI helpers

Scripts used to drive the open Editor through the Unity CLI (`unity command ...`).
This folder is outside `Assets/`, so Unity never compiles or imports it.

- `eval/*.cs`: snippets run with `unity command eval_file <file> --project-path F:\glider_jam\GliderJam`.
  `p1_*` are the Phase 1 conversion steps (materials, terrain placeholders, scenes, volume);
  `build_city.cs`, `canyon_setup.cs`, `rebuild_gliders.cs` run the generators; the rest are inspection probes
  (`ui_rects.cs`, `wing_uv_probe.cs`, `tex_probe.cs`, `floor_probe.cs`, ...).
  Files with `/*NAME*/`-style placeholders are templates filled in by the scripts below.
- `shot.ps1 -Name <name>`: captures the Game view inline and writes `Screenshots/<name>.png`
  (git-ignored). The CLI's own `save_path` always writes into `Assets/Screenshots`, so it is not used.
- `eval/city_photo.cs`, `eval/glider_photo.cs`: render stills from a free camera (edit or play mode).
- `click.ps1 "<Button name>"`: invokes a uGUI button by GameObject name in Play mode (menu testing
  without key auto-repeat problems).
- `vkeys.ps1 D,W`: holds keys on a virtual `AgentKeyboard` device in Play mode; `vkeys.ps1` releases.
  Needed because `unity command simulate_key` does not change `Keyboard.current` while the Editor is unfocused.
  Run `eval/input_focus.cs` once per Play session first.
- `eval/autopilot_on.cs`, `eval/teleport_route.cs`, `eval/crash_count.cs`: play-mode race helpers.
- `build.ps1`: builds the Windows player to `F:\glider_jam\Build` through the connected Editor and waits.
- `bench.ps1 -Name x -Preset High [-Map city] [-Solo] [-Set "backdrop=0"]`: one benchmark run of the
  built player (autopilot race, 1920x1080), log in `F:\glider_jam\Build\Benchmarks`.
- `bench_suite.ps1`: the full preset / map / mode matrix plus the distant-landscape A/B, writes `summary.md`.

PowerShell 5.1 mangles quotes in inline `unity command eval '...'` code, so C# always goes through `eval_file`.
