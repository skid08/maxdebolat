#requires -Version 5.1
<#
    MAX DEBLOAT - build script
    Produces a single self-contained portable EXE. Run on Windows with the .NET 8 SDK installed.
    Output: .\publish\MaxDebloat.exe
#>

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Definition
Set-Location $root

Write-Host "== MAX DEBLOAT build ==" -ForegroundColor Cyan

# Verify SDK
$sdk = (& dotnet --version) 2>$null
if (-not $sdk) {
    Write-Error "The .NET 8 SDK was not found. Install it from https://dotnet.microsoft.com/download/dotnet/8.0 (SDK, not just runtime)."
}
Write-Host "Using .NET SDK $sdk"

# Clean
if (Test-Path .\publish) { Remove-Item .\publish -Recurse -Force }

# Publish: self-contained, single file, win-x64
dotnet publish .\MaxDebloat.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o .\publish

$exe = Join-Path $root 'publish\MaxDebloat.exe'
if (Test-Path $exe) {
    $size = [math]::Round((Get-Item $exe).Length / 1MB, 1)
    Write-Host ""
    Write-Host "BUILD OK -> $exe  (${size} MB)" -ForegroundColor Green
    Write-Host "Double-click it on the target Windows 11 VM (it will request administrator rights)."
} else {
    Write-Error "Build finished but MaxDebloat.exe was not produced."
}
