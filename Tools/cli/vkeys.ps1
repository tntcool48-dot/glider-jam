# Hold keys on the virtual AgentKeyboard in the running game. Usage: vkeys.ps1 D,W   |  vkeys.ps1 (release all)
param([string[]]$Keys = @())
$env:UNITY_NO_PAGER = 1
$tmpl = Get-Content (Join-Path $PSScriptRoot "eval\vkeys.cs") -Raw
$list = ($Keys | Where-Object { $_ } | ForEach-Object { "UnityEngine.InputSystem.Key.$_" }) -join ", "
$file = "F:\glider_jam\_downloads\tmp\vkeys_run.cs"
if (-not $file.StartsWith("F:\")) { throw "refusing non-F: path" }
Set-Content -Path $file -Value ($tmpl.Replace("/*KEYS*/", $list)) -Encoding utf8
(unity command eval_file $file --result-only --no-banner --project-path F:\glider_jam\GliderJam | ConvertFrom-Json).result
