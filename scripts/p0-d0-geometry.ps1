$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$project=Join-Path $repo 'unity'
if(Get-Process Unity -ErrorAction SilentlyContinue){throw 'Cierra el Editor antes de ejecutar el diagnóstico.'}
$output=Join-Path $project ('TestResults\P0\D0\Geometry-'+(Get-Date -Format 'yyyyMMddTHHmmss'))
New-Item -ItemType Directory -Path $output | Out-Null
$arguments="-batchmode -nographics -projectPath `"$project`" -runTests -testPlatform EditMode -testFilter Mamporro.Tests.RegionalGeometryDiagnosisTests -testResults `"$output\edit.xml`" -logFile `"$output\edit.log`""
$process=Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
if($process.ExitCode -ne 0){throw "Unity terminó con código $($process.ExitCode): $output"}
[xml]$report=Get-Content -Raw (Join-Path $output 'edit.xml')
if($report.'test-run'.result -ne 'Passed'){throw "Diagnóstico fallido: $output"}
Write-Output "Geometría D0: $($report.'test-run'.passed) pruebas; $output"
