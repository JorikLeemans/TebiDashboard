# Starts Tebbi Dashboard API + Blazor Web in this window (no extra terminals).
# Usage: .\start.cmd          (recommended — works when script execution is disabled)
#        .\start.ps1          (requires RemoteSigned or Bypass for CurrentUser)

$ErrorActionPreference = "Stop"
$root = (Resolve-Path $PSScriptRoot).Path
$apiProject = Join-Path $root "src\TebbiDashboard.Api\TebbiDashboard.Api.csproj"
$webProject = Join-Path $root "src\TebbiDashboard.Web\TebbiDashboard.Web.csproj"
$logDir = Join-Path $root ".logs"
$apiHealthUrls = @(
  "https://localhost:7119/health",
  "http://localhost:5233/health"
)
$apiUrl = "https://localhost:7119"
$webUrl = "https://localhost:7274"
$webPort = 7274

$script:ApiProcess = $null
$script:WebProcess = $null

# Stops a process and its child tree (dotnet/Kestrel).
function Stop-DotnetApp {
  param([System.Diagnostics.Process]$Process)

  if ($null -eq $Process -or $Process.HasExited) {
    return
  }

  & taskkill.exe /PID $Process.Id /T /F 2>$null | Out-Null
}

# Shows the last lines of a log file when startup fails.
function Show-LogTail {
  param(
    [string]$Path,
    [int]$Lines = 12
  )

  if (-not (Test-Path $Path)) {
    return
  }

  Write-Host "--- tail $Path ---" -ForegroundColor DarkGray
  Get-Content $Path -Tail $Lines -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Host $_ -ForegroundColor DarkGray
  }
}

# Starts dotnet run hidden. Paths with spaces are quoted for cmd.exe.
function Start-DotnetApp {
  param(
    [string]$Name,
    [string]$ProjectFile,
    [string]$Profile
  )

  $stdout = Join-Path $logDir "$Name.out.log"
  $stderr = Join-Path $logDir "$Name.err.log"

  # Clear old logs so failures are easy to spot.
  "" | Set-Content $stdout -Encoding UTF8
  "" | Set-Content $stderr -Encoding UTF8

  # cmd.exe keeps quoted paths intact; redirect avoids Start-Process pipe deadlocks.
  $dotnetCmd = "dotnet run --no-build --project `"$ProjectFile`" --launch-profile $Profile >> `"$stdout`" 2>> `"$stderr`""
  $cmdArgs = "/c $dotnetCmd"

  return Start-Process -FilePath "cmd.exe" `
    -ArgumentList $cmdArgs `
    -WorkingDirectory $root `
    -PassThru `
    -WindowStyle Hidden
}

function Test-TcpPortOpen {
  param([int]$Port)

  try {
    $tcp = New-Object System.Net.Sockets.TcpClient
    $null = $tcp.Connect("127.0.0.1", $Port)
    $tcp.Close()
    return $true
  }
  catch {
    return $false
  }
}

function Test-ApiHealth {
  param([string[]]$Urls)

  foreach ($url in $Urls) {
    $statusCode = (& curl.exe -k -s -L -o NUL -w "%{http_code}" $url 2>$null | Out-String).Trim()
    if ($statusCode -eq "200") {
      return $true
    }
  }

  return (Test-TcpPortOpen -Port 7119)
}

function Wait-UntilReady {
  param(
    [scriptblock]$Condition,
    [string]$Label,
    [System.Diagnostics.Process]$Process,
    [string]$ErrorLogPath,
    [int]$MaxAttempts = 120
  )

  Write-Host "Waiting for $Label..." -ForegroundColor DarkGray

  for ($i = 1; $i -le $MaxAttempts; $i++) {
    if ($null -ne $Process -and $Process.HasExited) {
      Write-Host ""
      Write-Host "$Label process exited early (exit code $($Process.ExitCode))." -ForegroundColor Yellow
      Show-LogTail -Path $ErrorLogPath
      return $false
    }

    if (& $Condition) {
      Write-Host "$Label is ready." -ForegroundColor Green
      return $true
    }

    if ($i % 10 -eq 0) {
      Write-Host " ${i}s" -NoNewline -ForegroundColor DarkGray
    }
    else {
      Write-Host "." -NoNewline -ForegroundColor DarkGray
    }

    Start-Sleep -Seconds 1
  }

  Write-Host ""
  Write-Host "$Label did not respond in time. See .logs\ for details." -ForegroundColor Yellow
  Show-LogTail -Path $ErrorLogPath
  return $false
}

try {
  Write-Host "TEBI Dashboard - launcher" -ForegroundColor Cyan
  Write-Host "Project root: $root" -ForegroundColor DarkGray
  Write-Host "Runs API + Web in the background (one window only)." -ForegroundColor DarkGray
  Write-Host ""

  New-Item -ItemType Directory -Force -Path $logDir | Out-Null

  Write-Host "Checking HTTPS dev certificate..." -ForegroundColor DarkGray
  dotnet dev-certs https --trust 2>$null | Out-Null

  # Build first in this window (fixes space-in-path issues and shows compile errors).
  Write-Host "Building API and Web..." -ForegroundColor DarkGray
  dotnet build $apiProject --verbosity minimal
  if ($LASTEXITCODE -ne 0) {
    throw "API build failed."
  }

  dotnet build $webProject --verbosity minimal
  if ($LASTEXITCODE -ne 0) {
    throw "Web build failed."
  }

  Write-Host "Starting Web API ($apiUrl)..." -ForegroundColor Green
  $apiErrorLog = Join-Path $logDir "api.err.log"
  $script:ApiProcess = Start-DotnetApp -Name "api" -ProjectFile $apiProject -Profile "https"
  Write-Host ""

  $apiReady = Wait-UntilReady `
    -Label "API" `
    -Condition { Test-ApiHealth -Urls $apiHealthUrls } `
    -Process $script:ApiProcess `
    -ErrorLogPath $apiErrorLog
  Write-Host ""

  if (-not $apiReady) {
    Write-Host "Web will not start because the API is not ready." -ForegroundColor Yellow
    return
  }

  Write-Host "Starting Blazor Web ($webUrl)..." -ForegroundColor Green
  $webErrorLog = Join-Path $logDir "web.err.log"
  $script:WebProcess = Start-DotnetApp -Name "web" -ProjectFile $webProject -Profile "https"
  Write-Host ""

  $webReady = Wait-UntilReady `
    -Label "Web app" `
    -Condition { Test-TcpPortOpen -Port $webPort } `
    -Process $script:WebProcess `
    -ErrorLogPath $webErrorLog
  Write-Host ""

  if ($webReady) {
    Write-Host "Opening browser: $webUrl" -ForegroundColor Green
    Start-Process $webUrl
  }

  Write-Host ""
  Write-Host "Running in background:" -ForegroundColor Cyan
  Write-Host "  API:  $apiUrl" -ForegroundColor White
  Write-Host "  Web:  $webUrl" -ForegroundColor White
  Write-Host "  Logs: $logDir" -ForegroundColor DarkGray
  Write-Host ""
  Write-Host "Press Ctrl+C in this window to stop API and Web." -ForegroundColor DarkGray

  while ($true) {
    if ($script:ApiProcess.HasExited) {
      Write-Host "API process stopped unexpectedly." -ForegroundColor Yellow
      Show-LogTail -Path $apiErrorLog
      break
    }

    if ($script:WebProcess.HasExited) {
      Write-Host "Web process stopped unexpectedly." -ForegroundColor Yellow
      Show-LogTail -Path $webErrorLog
      break
    }

    Start-Sleep -Seconds 2
  }
}
finally {
  Write-Host ""
  Write-Host "Stopping Tebbi Dashboard..." -ForegroundColor DarkGray
  Stop-DotnetApp -Process $script:WebProcess
  Stop-DotnetApp -Process $script:ApiProcess
  Write-Host "Stopped." -ForegroundColor DarkGray
}
