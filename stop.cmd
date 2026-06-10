@echo off
REM Stops Tebbi Dashboard without changing PowerShell execution policy.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0stop.ps1"
pause
