@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0b0.ps1" %*
exit /b %ERRORLEVEL%
