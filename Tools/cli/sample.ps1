# Import a Unity package sample: sample.ps1 -Package com.unity.shadergraph -Name "Terrain Shaders"
param([Parameter(Mandatory = $true)][string]$Package, [Parameter(Mandatory = $true)][string]$Name)
$env:UNITY_NO_PAGER = 1
$tmpl = Get-Content (Join-Path $PSScriptRoot "eval\import_sample.cs") -Raw
$file = "F:\glider_jam\_downloads\tmp\import_sample_run.cs"
if (-not $file.StartsWith("F:\")) { throw "refusing non-F: path" }
Set-Content -Path $file -Value ($tmpl.Replace("/*PKG*/", $Package).Replace("/*NAME*/", $Name)) -Encoding utf8
(unity command eval_file $file --timeout 300 --result-only --no-banner --project-path F:\glider_jam\GliderJam -- --timeout 290000 | ConvertFrom-Json).result
