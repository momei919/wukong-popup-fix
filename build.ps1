# Build script for Wukong Popup Fix
# Uses the .NET Framework compiler shipped with Windows, no SDK required.

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$src = Join-Path $root 'src'
$out = Join-Path $root 'WukongPopupFix.exe'

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) {
    $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path $csc)) {
    throw 'csc.exe not found. .NET Framework 4.x is required.'
}

& $csc /nologo /target:winexe /optimize+ `
    "/win32icon:$src\app.ico" `
    "/win32manifest:$src\app.manifest" `
    "/out:$out" `
    /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll `
    "$src\Main.cs"

if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE" }

$info = (Get-Item $out).VersionInfo
Write-Host ("Built: " + $out)
Write-Host ("Version: " + $info.FileVersion)
