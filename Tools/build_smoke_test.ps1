# Smoke test of the Windows build: menu -> Split-screen -> both players steer -> screenshots (saved on F:).
param([string]$Exe = "F:\glider_jam\Build\GliderJam.exe", [string]$Out = "F:\glider_jam\GliderJam\Screenshots")
if (-not $Out.StartsWith("F:\")) { throw "refusing non-F: path" }
Add-Type -AssemblyName System.Drawing, System.Windows.Forms
Add-Type @"
using System; using System.Runtime.InteropServices;
public static class Kbd {
  [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern uint MapVirtualKey(uint code, uint mapType);
  public static void Down(byte vk, bool ext) { keybd_event(vk, (byte)MapVirtualKey(vk, 0), ext ? 1u : 0u, UIntPtr.Zero); }
  public static void Up(byte vk, bool ext) { keybd_event(vk, (byte)MapVirtualKey(vk, 0), (ext ? 1u : 0u) | 2u, UIntPtr.Zero); }
}
"@
function Shot($name) {
  $b = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
  $bmp = New-Object System.Drawing.Bitmap $b.Width, $b.Height
  $g = [System.Drawing.Graphics]::FromImage($bmp); $g.CopyFromScreen($b.Location, [System.Drawing.Point]::Empty, $b.Size)
  $small = New-Object System.Drawing.Bitmap $bmp, 1280, ([int](1280 * $b.Height / $b.Width))
  $small.Save((Join-Path $Out "$name.png")); $g.Dispose(); $bmp.Dispose(); $small.Dispose(); "$name.png"
}
function Tap([byte]$vk, [bool]$ext = $false, [int]$ms = 80) { [Kbd]::Down($vk, $ext); Start-Sleep -Milliseconds $ms; [Kbd]::Up($vk, $ext) }

$proc = Start-Process -FilePath $Exe -ArgumentList @("-logFile", "F:\glider_jam\_logs\Player_smoke.log", "-screen-fullscreen", "1", "-screen-width", "1920", "-screen-height", "1080") -PassThru
Start-Sleep -Seconds 14
$ws = New-Object -ComObject WScript.Shell; Tap 0x12; $null = $ws.AppActivate($proc.Id); Start-Sleep -Milliseconds 500; [Kbd]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null; Start-Sleep -Milliseconds 500; Add-Type -Name Fg -Namespace W -MemberDefinition '[DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();'; "foreground is game: " + ([W.Fg]::GetForegroundWindow() -eq $proc.MainWindowHandle)
Shot "build_menu"
Tap 0x28 $true          # Down arrow -> SPLIT-SCREEN
Start-Sleep -Milliseconds 400
Tap 0x0D                # Enter
Start-Sleep -Seconds 9  # load + countdown
Shot "build_split_go"
# P1 turns right (D) while P2 turns left (Left arrow) and boosts (Right Ctrl)
[Kbd]::Down(0x44, $false); [Kbd]::Down(0x25, $true); [Kbd]::Down(0xA3, $true)
Start-Sleep -Milliseconds 1500
Shot "build_split_steer"
[Kbd]::Up(0x44, $false); [Kbd]::Up(0x25, $true); [Kbd]::Up(0xA3, $true)
Start-Sleep -Seconds 4
Shot "build_split_later"
Stop-Process -Id $proc.Id
"done"
