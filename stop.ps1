# Stops background Tebbi Dashboard API/Web processes started by start.ps1.
# Usage: .\stop.ps1

$ErrorActionPreference = "SilentlyContinue"
$root = $PSScriptRoot

Write-Host "Stopping Tebbi Dashboard processes..." -ForegroundColor Cyan

# Kill dotnet processes that run our Api/Web projects (dev helper only).
Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe'" |
  Where-Object { $_.CommandLine -match "TebbiDashboard\.(Api|Web)" } |
  ForEach-Object {
    & taskkill.exe /PID $_.ProcessId /T /F 2>$null | Out-Null
    Write-Host "  Stopped PID $($_.ProcessId)" -ForegroundColor DarkGray
  }

Write-Host "Done." -ForegroundColor Green
