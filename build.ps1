# WinTotal build script — uses the csc.exe built into Windows (nothing to install)
# Usage:  .\build.ps1        → release (requireAdministrator manifest, WinTotal.exe)
#         .\build.ps1 -Dbg   → debug   (no UAC, WinTotal_dbg.exe)
param([switch]$Dbg)

$src = Join-Path $PSScriptRoot "WinTotal.cs"
$icon = Join-Path $PSScriptRoot "icon.ico"
if ($Dbg) {
    $out = Join-Path $PSScriptRoot "WinTotal_dbg.exe"
    $manifest = Join-Path $PSScriptRoot "app.debug.manifest"
} else {
    $out = Join-Path $PSScriptRoot "WinTotal.exe"
    $manifest = Join-Path $PSScriptRoot "app.manifest"
}

$fw = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319"
$wpf = Join-Path $fw "WPF"
$csc = Join-Path $fw "csc.exe"

$iconArg = @()
if (Test-Path $icon) { $iconArg = @("/win32icon:$icon") }

& $csc /nologo /target:winexe /optimize+ /platform:anycpu /codepage:65001 `
    /out:$out `
    /win32manifest:$manifest `
    @iconArg `
    /r:System.dll /r:System.Core.dll /r:System.Management.dll /r:System.Xaml.dll /r:System.Drawing.dll `
    /r:"$wpf\WindowsBase.dll" /r:"$wpf\PresentationCore.dll" /r:"$wpf\PresentationFramework.dll" `
    $src

if ($LASTEXITCODE -eq 0) {
    $size = (Get-Item $out).Length / 1KB
    Write-Host ("Build OK: {0} ({1:F0} KB)" -f $out, $size)
} else {
    Write-Host "Build failed" -ForegroundColor Red
    exit 1
}
