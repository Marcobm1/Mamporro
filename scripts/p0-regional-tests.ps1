param([string]$Filter='Mamporro.Tests.RegionalGraphTests',[string]$Category='')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$project=Join-Path $repo 'unity'
if(Get-Process Unity -ErrorAction SilentlyContinue){throw 'Cierra el Editor antes de ejecutar el diagnóstico regional.'}
if($Filter -notmatch '^[A-Za-z0-9_.]+$' -or $Category -notmatch '^[A-Za-z0-9_-]*$'){throw 'Filtro/categoría no válidos.'}
$output=Join-Path $project ('TestResults\P0\Regional-'+(Get-Date -Format 'yyyyMMddTHHmmss'))
New-Item -ItemType Directory -Path $output | Out-Null
$arguments="-batchmode -nographics -projectPath `"$project`" -runTests -testPlatform EditMode -testFilter $Filter -testResults `"$output\edit.xml`" -logFile `"$output\edit.log`""
if($Category){$arguments+=" -testCategory $Category"}
$process=Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
if($process.ExitCode -ne 0){throw "Unity terminó con código $($process.ExitCode): $output"}
[xml]$report=Get-Content -Raw (Join-Path $output 'edit.xml')
if($report.'test-run'.result -ne 'Passed' -or [int]$report.'test-run'.passed -eq 0){throw "Diagnóstico fallido o vacío: $output"}
Write-Output "Regional: $($report.'test-run'.passed) pruebas; $output"
