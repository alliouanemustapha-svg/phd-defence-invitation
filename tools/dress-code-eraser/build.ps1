# Builds erase.exe with the C# compiler that ships with Windows (.NET Framework 4.x).
$ErrorActionPreference = "Stop"
$csc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) { throw "csc.exe not found at $csc. Install .NET Framework 4.x." }
& $csc /nologo /optimize /out:"$PSScriptRoot\erase.exe" "$PSScriptRoot\erase.cs"
if ($LASTEXITCODE -ne 0) { throw "Build failed." }
Write-Host "Built $PSScriptRoot\erase.exe"
