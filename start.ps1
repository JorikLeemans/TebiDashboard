# Starts Tebbi Dashboard API + Blazor Web in separate terminal windows.
# Usage: .\start.cmd          (recommended — works when script execution is disabled)
#        .\start.ps1          (requires RemoteSigned or Bypass for CurrentUser)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$apiProject = Join-Path $root "src\TebbiDashboard.Api"
$webProject = Join-Path $root "src\TebbiDashboard.Web"
$apiHealthUrl = "https://localhost:7119/health"
$webUrl = "https://localhost:7274"

Write-Host "TEBI Dashboard - starting..." -ForegroundColor Cyan
Write-Host "Project root: $root" -ForegroundColor DarkGray

# Ensure the local SQLite folder exists (see appsettings.json connection string).
$dataDir = Join-Path $root "data"
New-Item -ItemType Directory -Force -Path $dataDir | Out-Null

# Trust HTTPS dev certificate (safe to run multiple times).
Write-Host "Checking HTTPS dev certificate..." -ForegroundColor DarkGray
dotnet dev-certs https --trust 2>$null | Out-Null

# Launch API in a new window.
Write-Host "Starting Web API (https://localhost:7119)..." -ForegroundColor Green
Start-Process powershell -ArgumentList @(
    "-NoExit",
    "-Command",
    "Set-Location '$root'; Write-Host '=== TebbiDashboard.Api ===' -ForegroundColor Cyan; dotnet run --project '$apiProject'"
)

# Wait until the API health endpoint responds.
Write-Host "Waiting for API to be ready..." -ForegroundColor DarkGray
$maxAttempts = 30
$ready = $false

for ($i = 1; $i -le $maxAttempts; $i++) {
  # curl -k works on Windows PowerShell 5.1 and PowerShell 7 (local dev cert).
  $statusCode = & curl.exe -k -s -o $null -w "%{http_code}" $apiHealthUrl 2>$null
  if ($statusCode -eq "200") {
    $ready = $true
    break
  }
  Start-Sleep -Seconds 1
}

if (-not $ready) {
    Write-Host "API did not respond in time. Check the API window for errors." -ForegroundColor Yellow
}
else {
    Write-Host "API is ready." -ForegroundColor Green
}

# Launch Blazor Web in a new window.
Write-Host "Starting Blazor Web ($webUrl)..." -ForegroundColor Green
Start-Process powershell -ArgumentList @(
    "-NoExit",
    "-Command",
    "Set-Location '$root'; Write-Host '=== TebbiDashboard.Web ===' -ForegroundColor Cyan; dotnet run --project '$webProject'"
)

Write-Host ""
Write-Host "Done. Two terminal windows should be open:" -ForegroundColor Cyan
Write-Host "  API:  https://localhost:7119" -ForegroundColor White
Write-Host "  Web:  $webUrl" -ForegroundColor White
Write-Host ""
Write-Host "Close those windows to stop the applications." -ForegroundColor DarkGray
