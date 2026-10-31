# Run one player benchmark and print its summary.
# bench.ps1 -Name split_high [-Preset High] [-Map canyon|city] [-Solo] [-Seconds 45] [-Set "backdrop=0","water=1"]
param(
    [Parameter(Mandatory = $true)][string]$Name,
    [string]$Preset = "High",
    [string]$Map = "canyon",
    [switch]$Solo,
    [int]$Seconds = 45,
    [string[]]$Set = @()
)
$exe = "F:\glider_jam\Build\GliderJam.exe"
$logDir = "F:\glider_jam\Build\Benchmarks"
if (-not $logDir.StartsWith("F:\")) { throw "refusing non-F: path" }
New-Item -ItemType Directory -Force $logDir | Out-Null
$log = Join-Path $logDir "$Name.txt"
Remove-Item $log -ErrorAction SilentlyContinue
$a = @("-benchmark", "-benchmarkLog", $log, "-benchmarkSeconds", $Seconds, "-benchmarkMap", $Map, "-preset", $Preset,
       "-screen-fullscreen", "1", "-screen-width", "1920", "-screen-height", "1080", "-logFile", (Join-Path $logDir "$Name.log"))
if ($Solo) { $a += "-benchmarkSolo" }
foreach ($kv in $Set) { $a += @("-set", $kv) }
# A new player started while the previous one is still shutting down can hang at load.
Get-Process GliderJam -ErrorAction SilentlyContinue | ForEach-Object { if (-not $_.WaitForExit(20000)) { $_.Kill() } }
Start-Sleep -Seconds 3
for ($try = 1; $try -le 2 -and -not (Test-Path $log); $try++) {
    # The player occasionally hangs right after loading (rare, canyon only): kill it and try once more.
    $p = Start-Process -FilePath $exe -ArgumentList $a -PassThru
    if (-not $p.WaitForExit(($Seconds + 120) * 1000))
    {
        $p.Kill(); "timed out (attempt $try)"; Start-Sleep -Seconds 5
        # Keep the hung run's player log (the retry would overwrite it).
        Move-Item -Force (Join-Path $logDir "$Name.log") (Join-Path $logDir "$Name.hang$try.log") -ErrorAction SilentlyContinue
    }
}
if (Test-Path $log) { Get-Content $log } else { "no log written" }
