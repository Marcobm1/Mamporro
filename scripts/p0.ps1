param([ValidateSet('build','devbuild','jugar','edit','play','benchmark','devdiag','evidence')][string]$Action='jugar')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$project=Join-Path $repo 'unity'
$results=Join-Path $project 'TestResults/P0'
New-Item -ItemType Directory -Force -Path $results | Out-Null
if($Action -eq 'edit'){& (Join-Path $PSScriptRoot 'u3.cmd') edit;exit $LASTEXITCODE}
if($Action -eq 'play'){& (Join-Path $PSScriptRoot 'u3.cmd') play;exit $LASTEXITCODE}
if($Action -in @('benchmark','devdiag','evidence')){
    if(Get-Process MAMPORRO-P0 -ErrorAction SilentlyContinue){throw 'Cierra la otra instancia P0 antes de medir o capturar.'}
    $dev=$Action -eq 'devdiag'
    $folder=if($dev){'P0Dev'}else{'P0'}
    $player=Join-Path $project "Builds/$folder/MAMPORRO-P0.exe"
    if(!(Test-Path -LiteralPath $player)){throw 'Falta la build QA correspondiente.'}
    $kind=if($Action -eq 'evidence'){'Evidence'}elseif($dev){'Development'}else{'Normal'}
    $output=Join-Path $results ($kind+'/'+(Get-Date -Format 'yyyyMMddTHHmmss'))
    New-Item -ItemType Directory -Force -Path $output | Out-Null
    $source=Get-Content -LiteralPath (Join-Path (Split-Path $player -Parent) 'P0-source.txt') -Raw
    $source=$source.Trim()
    $mode=if($Action -eq 'evidence'){'-p0-evidence'}else{'-p0-benchmark'}
    # Ventana QA visible para las capturas y medidas gráficas solicitadas: oculta devuelve negro.
    $process=Start-Process -FilePath $player -WorkingDirectory $project -ArgumentList "-monitor 1 -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -force-d3d11 -p0-qa $mode -p0-source $source -p0-output `"$output`" -u4-save-dir `"$(Join-Path $output Save)`" -logFile `"$(Join-Path $output player.log)`"" -WindowStyle Normal -PassThru
    if(!$process.WaitForExit(600000)){Stop-Process -Id $process.Id -Force;throw "P0 agotó 10 minutos: $output"}
    if($process.ExitCode -ne 0){throw "P0 falló: $output"}
    $expected=if($Action -eq 'evidence'){@('evidence.json')}else{@('load-0.json','load-300.json','load-500.json','load-750.json')}
    foreach($name in $expected){
        $report=Get-Content -LiteralPath (Join-Path $output $name) -Raw | ConvertFrom-Json
        if(!$report.valid){throw "Informe no válido: $name"}
        if($Action -ne 'evidence' -and $report.development -ne $dev){throw 'Tipo de build inesperado.'}
    }
    Write-Output "P0 $Action verificado: $output"
    return
}
if($Action -eq 'jugar'){
    $player=Join-Path $project 'Builds/P0/MAMPORRO-P0.exe'
    if(!(Test-Path -LiteralPath $player)){throw 'Falta build QA: scripts\p0.cmd build'}
    # Ventana visible para la prueba jugable solicitada, progreso exclusivamente QA.
    Start-Process -FilePath $player -WorkingDirectory $project -ArgumentList "-monitor 1 -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -p0-qa -u4-save-dir `"$(Join-Path $results ManualSave)`" -logFile `"$(Join-Path $results manual.log)`"" -WindowStyle Normal
    return
}
if(Get-Process Unity -ErrorAction SilentlyContinue){throw 'Cierra el Editor antes de generar la build QA.'}
$editor='C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe'
$method=if($Action -eq 'devbuild'){'Development'}else{'Build'}
$process=Start-Process -FilePath $editor -ArgumentList "-batchmode -projectPath `"$project`" -logFile `"$(Join-Path $results ($Action+'.log'))`" -quit -executeMethod Mamporro.Editor.P0Project.$method" -WorkingDirectory $project -WindowStyle Hidden -PassThru
$process.WaitForExit()
if($process.ExitCode -ne 0){throw "P0 $Action falló; revisar $results"}
$buildFolder=if($Action -eq 'devbuild'){'P0Dev'}else{'P0'}
$source=(& git -C $repo rev-parse HEAD).Trim()
$pending=& git -C $repo status --porcelain -- unity/Assets/Mamporro/Core unity/Assets/Mamporro/U3 unity/Assets/Mamporro/Editor/P0Project.cs
if($pending){$source+='+P0-WIP'}
Set-Content -LiteralPath (Join-Path $project "Builds/$buildFolder/P0-source.txt") -Value $source -Encoding ASCII
Write-Output "P0 $Action correcta. Entrega Windows U6 no modificada."
