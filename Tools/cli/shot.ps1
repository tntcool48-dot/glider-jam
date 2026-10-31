# Capture the Unity Game view (or one camera) through the CLI and save it outside Assets/.
# Usage: shot.ps1 -Name p1_flight [-Source screen|camera] [-Camera "Main Camera"] [-Width 1920 -Height 1080]
param(
    [Parameter(Mandatory = $true)][string]$Name,
    [string]$Source = "screen",
    [string]$Camera = "",
    [int]$Width = 0,
    [int]$Height = 0
)
$env:UNITY_NO_PAGER = 1
$outDir = "F:\glider_jam\GliderJam\Screenshots"
if (-not $outDir.StartsWith("F:\")) { throw "refusing non-F: path" }
New-Item -ItemType Directory -Force $outDir | Out-Null
$extra = @("--source", $Source)
if ($Camera) { $extra += @("--camera", $Camera) }
if ($Width -gt 0) { $extra += @("--width", $Width, "--height", $Height) }
$r = unity command capture game_view --result-only --no-banner --project-path F:\glider_jam\GliderJam -- @extra | ConvertFrom-Json
if (-not $r.base64) { throw "capture failed: $($r | ConvertTo-Json -Compress)" }
$path = Join-Path $outDir "$Name.png"
[IO.File]::WriteAllBytes($path, [Convert]::FromBase64String($r.base64))
"$path ($($r.width)x$($r.height))"
