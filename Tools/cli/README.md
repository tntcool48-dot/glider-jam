# Unity CLI helpers

Scripts used to drive the open Editor through the Unity CLI (`unity command ...`).
This folder is outside `Assets/`, so Unity never compiles or imports it.

- `eval/*.cs` — snippets run with `unity command eval_file <file> --project-path F:\glider_jam\GliderJam`.
  `p1_*` are the Phase 1 conversion steps (materials, terrain placeholders, scenes, volume);
  the rest are inspection probes.
- `shot.ps1 -Name <name>` — captures the Game view inline and writes `Screenshots/<name>.png`
  (git-ignored). The CLI's own `save_path` always writes into `Assets/Screenshots`, so it is not used.
- `vkeys.ps1 D,W` — holds keys on a virtual `AgentKeyboard` device in Play mode; `vkeys.ps1` releases.
  Needed because `unity command simulate_key` does not change `Keyboard.current` while the Editor is unfocused.
  Run `eval/input_focus.cs` once per Play session first.

PowerShell 5.1 mangles quotes in inline `unity command eval '...'` code, so C# always goes through `eval_file`.
