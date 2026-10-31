# Run the benchmark matrix against the built player and write Build/Benchmarks/summary.md.
param([int]$Seconds = 40)
$dir = "F:\glider_jam\Build\Benchmarks"
if (-not $dir.StartsWith("F:\")) { throw "refusing non-F: path" }
$runs = @(
    @{ n = "canyon_split_low"; p = "Low"; m = "canyon" },
    @{ n = "canyon_split_medium"; p = "Medium"; m = "canyon" },
    @{ n = "canyon_split_high"; p = "High"; m = "canyon" },
    @{ n = "canyon_split_ultra"; p = "Ultra"; m = "canyon" },
    @{ n = "canyon_split_ultra_rt"; p = "UltraRayTracing"; m = "canyon" },
    @{ n = "city_split_low"; p = "Low"; m = "city" },
    @{ n = "city_split_medium"; p = "Medium"; m = "city" },
    @{ n = "city_split_high"; p = "High"; m = "city" },
    @{ n = "city_split_ultra"; p = "Ultra"; m = "city" },
    @{ n = "city_split_ultra_rt"; p = "UltraRayTracing"; m = "city" },
    @{ n = "canyon_solo_ultra"; p = "Ultra"; m = "canyon"; solo = $true },
    @{ n = "canyon_solo_ultra_rt"; p = "UltraRayTracing"; m = "canyon"; solo = $true },
    @{ n = "city_solo_ultra_rt"; p = "UltraRayTracing"; m = "city"; solo = $true },
    @{ n = "ab_canyon_high_backdrop2_a"; p = "High"; m = "canyon"; set = @("backdrop=2") },
    @{ n = "ab_canyon_high_backdrop0_a"; p = "High"; m = "canyon"; set = @("backdrop=0") },
    @{ n = "ab_canyon_high_backdrop2_b"; p = "High"; m = "canyon"; set = @("backdrop=2") },
    @{ n = "ab_canyon_high_backdrop0_b"; p = "High"; m = "canyon"; set = @("backdrop=0") }
)
$rows = @("| run | preset | avg fps | 1% low | 99th pct ms | GPU ms | CPU ms | frames < 60 fps |", "|---|---|---|---|---|---|---|---|")
foreach ($r in $runs) {
    $a = @{ Name = $r.n; Preset = $r.p; Map = $r.m; Seconds = $Seconds }
    if ($r.solo) { $a.Solo = $true }
    if ($r.set) { $a.Set = $r.set }
    $out = & (Join-Path $PSScriptRoot "bench.ps1") @a | Out-String
    $avg = if ($out -match "average fps: ([\d.]+)") { $Matches[1] } else { "-" }
    $low = if ($out -match "1% low fps: ([\d.]+)") { $Matches[1] } else { "-" }
    $p99 = if ($out -match "99th pct frame: ([\d.]+)") { $Matches[1] } else { "-" }
    $gpu = if ($out -match "gpu frame: ([\d.]+)") { $Matches[1] } else { "-" }
    $cpu = if ($out -match "cpu main thread: ([\d.]+)") { $Matches[1] } else { "-" }
    $slow = if ($out -match "slower than 60 fps: ([\d.]+%)") { $Matches[1] } else { "-" }
    $line = "| $($r.n) | $($r.p) | $avg | $low | $p99 | $gpu | $cpu | $slow |"
    $rows += $line
    $line
}
$rows | Set-Content (Join-Path $dir "summary.md") -Encoding utf8
