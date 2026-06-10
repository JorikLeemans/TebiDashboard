# Starts Tebi Dashboard API + Blazor Web in separate terminal windows.
# Usage: .\start.cmd          (recommended — works when script execution is disabled)
#        .\start.ps1          (requires RemoteSigned or Bypass for CurrentUser)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$apiProject = Join-Path $root "src\TebiDashboard.Api"
$webProject = Join-Path $root "src\TebiDashboard.Web"
$apiHealthUrl = "https://localhost:7119/health"
$webUrl = "https://localhost:7274"

Write-Host "TEBI Dashboard - starting..." -ForegroundColor Cyan
Write-Host "Project root: $root" -ForegroundColor DarkGray

# Local development uses the same PostgreSQL stack as Railway (database in Docker).
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Host ""
    Write-Host "Docker is required for local PostgreSQL." -ForegroundColor Red
    Write-Host "Install Docker Desktop: https://www.docker.com/products/docker-desktop/" -ForegroundColor Yellow
    Write-Host "Then run .\start.cmd again." -ForegroundColor Yellow
    exit 1
}

Write-Host "Starting PostgreSQL (Docker)..." -ForegroundColor Green
Push-Location $root
try {
    docker compose up postgres -d --wait
    if ($LASTEXITCODE -ne 0) {
        throw "docker compose could not start PostgreSQL."
    }
}
finally {
    Pop-Location
}

Write-Host "PostgreSQL is ready on localhost:5432 (Tebi / Tebi_dev_password)." -ForegroundColor Green

# Trust HTTPS dev certificate (safe to run multiple times).
Write-Host "Checking HTTPS dev certificate..." -ForegroundColor DarkGray
dotnet dev-certs https --trust 2>$null | Out-Null

# Launch API in a new window.
Write-Host "Starting Web API (https://localhost:7119)..." -ForegroundColor Green
Start-Process powershell -ArgumentList @(
    "-NoExit",
    "-Command",
    "Set-Location '$root'; Write-Host '=== TebiDashboard.Api ===' -ForegroundColor Cyan; dotnet run --project '$apiProject'"
)

# Wait until the API health endpoint responds (migrations may take longer on first run).
Write-Host "Waiting for API to be ready..." -ForegroundColor DarkGray
$maxAttempts = 60
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
    Write-Host "Tip: ensure PostgreSQL is running with 'docker compose ps'." -ForegroundColor Yellow
}
else {
    Write-Host "API is ready." -ForegroundColor Green
}

# Launch Blazor Web in a new window.
Write-Host "Starting Blazor Web ($webUrl)..." -ForegroundColor Green
Start-Process powershell -ArgumentList @(
    "-NoExit",
    "-Command",
    "Set-Location '$root'; Write-Host '=== TebiDashboard.Web ===' -ForegroundColor Cyan; dotnet run --project '$webProject'"
)

Write-Host ""
Write-Host "Done. Two terminal windows should be open:" -ForegroundColor Cyan
Write-Host "  PostgreSQL: localhost:5432 (Docker volume Tebi_pg_data)" -ForegroundColor White
Write-Host "  API:        https://localhost:7119" -ForegroundColor White
Write-Host "  Web:        $webUrl" -ForegroundColor White
Write-Host ""
Write-Host "Close the API/Web windows to stop the apps. PostgreSQL keeps running in Docker." -ForegroundColor DarkGray
Write-Host "Stop PostgreSQL with: docker compose stop postgres" -ForegroundColor DarkGray
