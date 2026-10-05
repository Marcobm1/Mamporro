@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0blender.ps1" %*
exit /b %ERRORLEVEL%
