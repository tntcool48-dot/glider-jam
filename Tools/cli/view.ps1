# Render the open scene from a viewpoint (edit or play mode) into Screenshots/<Name>.png
# view.ps1 -Name v1 -Pos "342,242,213" -Look "500,180,1200" [-Fov 60] [-Width 1600 -Height 900] [-Frames 24]
param([Parameter(Mandatory = $true)][string]$Name, [string]$Pos, [string]$Look, [int]$Fov = 60,
      [int]$Width = 1600, [int]$Height = 900, [int]$Frames = 24)
$env:UNITY_NO_PAGER = 1
unity status --until-ready --project-path F:\glider_jam\GliderJam --timeout 180 --format json --no-banner | Out-Null
$tmpl = Get-Content (Join-Path $PSScriptRoot "eval\preview_cam.cs") -Raw
$file = "F:\glider_jam\_downloads\tmp\preview_cam_run.cs"
if (-not $file.StartsWith("F:\")) { throw "refusing non-F: path" }
$code = $tmpl.Replace("/*POS*/", ($Pos -replace ",", "f,") + "f").Replace("/*LOOK*/", ($Look -replace ",", "f,") + "f")
$code = $code.Replace("/*FOV*/", "$Fov").Replace("/*W*/", "$Width").Replace("/*H*/", "$Height").Replace("/*FRAMES*/", "$Frames").Replace("/*NAME*/", $Name)
Set-Content -Path $file -Value $code -Encoding utf8
$r = unity command eval_file $file --timeout 200 --result-only --no-banner --project-path F:\glider_jam\GliderJam -- --timeout 190000 | ConvertFrom-Json
if ($r.success) { "F:\glider_jam\GliderJam\" + $r.result } else { "FAILED: " + ($r | ConvertTo-Json -Compress -Depth 5) }
