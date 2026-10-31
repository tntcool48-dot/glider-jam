# Evaluate one C# expression in the open Editor and print its result.
# Usage: call.ps1 "GliderJam.EditorTools.TerrainSculpt.Run()" [-TimeoutSec 300]
param([Parameter(Mandatory = $true)][string]$Expr, [int]$TimeoutSec = 300)
$env:UNITY_NO_PAGER = 1
# After a recompile the Pipeline server is briefly gone and eval fails without retrying: wait for it.
unity status --until-ready --project-path F:\glider_jam\GliderJam --timeout 180 --format json --no-banner | Out-Null
$file = "F:\glider_jam\_downloads\tmp\call_run.cs"
if (-not $file.StartsWith("F:\")) { throw "refusing non-F: path" }
Set-Content -Path $file -Value ("return " + $Expr + ";") -Encoding utf8
$r = unity command eval_file $file --timeout ($TimeoutSec + 10) --result-only --no-banner --project-path F:\glider_jam\GliderJam -- --timeout ($TimeoutSec * 1000) | ConvertFrom-Json
if ($r.success) { $r.result } else { "FAILED: " + ($r | ConvertTo-Json -Depth 5 -Compress) }
