# Click a uGUI button by name in Play mode: click.ps1 "SETTINGS Button"
param([Parameter(Mandatory = $true)][string]$Name)
$env:UNITY_NO_PAGER = 1
$file = "F:\glider_jam\_downloads\tmp\ui_click_run.cs"
(Get-Content (Join-Path $PSScriptRoot "eval\ui_click.cs") -Raw).Replace("/*NAME*/", $Name) | Set-Content $file -Encoding utf8
(unity command eval_file $file --result-only --no-banner --project-path F:\glider_jam\GliderJam | ConvertFrom-Json).result
