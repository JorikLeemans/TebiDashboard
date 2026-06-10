@echo off
REM Starts Tebbi Dashboard without changing system PowerShell execution policy.
REM Usage: start.cmd   (double-click or run from cmd/PowerShell)
REM -NoExit keeps this launcher window open after startup finishes.

powershell.exe -NoProfile -ExecutionPolicy Bypass -NoExit -File "%~dp0start.ps1"