@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0u1.ps1" %*
exit /b %errorlevel%
