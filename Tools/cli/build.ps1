# Build the Windows player through the connected Editor and wait for it: build.ps1 [-TimeoutMinutes 40]
param([int]$TimeoutMinutes = 40)
$env:UNITY_NO_PAGER = 1
$out = "F:/glider_jam/Build/GliderJam.exe"
if (-not $out.StartsWith("F:/")) { throw "refusing non-F: path" }
unity status --until-ready --project-path F:\glider_jam\GliderJam --timeout 300 --format json --no-banner | Out-Null
unity command build run --result-only --no-banner --project-path F:\glider_jam\GliderJam -- --target StandaloneWindows64 --outputPath $out --confirm true --timeout 120 | ConvertFrom-Json | Select-Object -ExpandProperty status
$t = Get-Date
do {
    Start-Sleep -Seconds 15
    $s = unity command build status --result-only --no-banner --project-path F:\glider_jam\GliderJam 2>$null | ConvertFrom-Json
} while (-not ($s -and $s.status -eq "completed") -and ((Get-Date) - $t).TotalMinutes -lt $TimeoutMinutes)
"build " + $s.result + " in " + [int]((Get-Date) - $t).TotalSeconds + "s errors=" + $s.totalErrors + " warnings=" + $s.totalWarnings
$s.errors | ForEach-Object { $_.message.Substring(0, [Math]::Min(240, $_.message.Length)) }
