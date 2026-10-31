# Download CC0 texture sets from Poly Haven (https://polyhaven.com, license CC0) to F:\glider_jam\_downloads\polyhaven\<id>\
# Usage: polyhaven_fetch.ps1 -Ids cliff_side,mossy_rock [-Res 2k]
param([Parameter(Mandatory = $true)][string[]]$Ids, [string]$Res = "2k")
$ProgressPreference = 'SilentlyContinue'
$root = "F:\glider_jam\_downloads\polyhaven"
if (-not $root.StartsWith("F:\")) { throw "refusing non-F: path" }
$maps = @{ "Diffuse" = "diff"; "nor_gl" = "nor_gl"; "Rough" = "rough"; "AO" = "ao"; "Displacement" = "disp" }
foreach ($id in $Ids) {
    $dir = Join-Path $root $id
    New-Item -ItemType Directory -Force $dir | Out-Null
    $files = Invoke-RestMethod -Uri "https://api.polyhaven.com/files/$id" -TimeoutSec 60 -Headers @{ "User-Agent" = "GliderJam-dev" }
    foreach ($k in $maps.Keys) {
        $entry = $files.$k
        if (-not $entry) { "  $id has no $k"; continue }
        $f = $entry.$Res.jpg
        if (-not $f) { $f = $entry.$Res.png }
        if (-not $f) { "  $id $k has no $Res jpg/png"; continue }
        $ext = [IO.Path]::GetExtension($f.url)
        $out = Join-Path $dir ("{0}_{1}_{2}{3}" -f $id, $maps[$k], $Res, $ext)
        if (-not (Test-Path $out)) { Invoke-WebRequest -Uri $f.url -OutFile $out -TimeoutSec 300 -Headers @{ "User-Agent" = "GliderJam-dev" } }
        "{0}  {1:N1} MB  {2}" -f (Split-Path $out -Leaf), ((Get-Item $out).Length / 1MB), $f.url
    }
}
