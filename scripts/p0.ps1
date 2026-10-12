param([ValidateSet('build','devbuild','jugar','edit','play','benchmark','devdiag','evidence','benchmarkb','devdiagb','evidenceb','benchmarkc','devdiagc','evidencec')][string]$Action='jugar')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$project=Join-Path $repo 'unity'
$results=Join-Path $project 'TestResults/P0'
New-Item -ItemType Directory -Force -Path $results | Out-Null
if($Action -eq 'edit'){& (Join-Path $PSScriptRoot 'u3.cmd') edit;exit $LASTEXITCODE}
if($Action -eq 'play'){& (Join-Path $PSScriptRoot 'u3.cmd') play;exit $LASTEXITCODE}
if($Action -in @('benchmark','devdiag','evidence','benchmarkb','devdiagb','evidenceb','benchmarkc','devdiagc','evidencec')){
    if(Get-Process MAMPORRO-P0 -ErrorAction SilentlyContinue){throw 'Cierra la otra instancia P0 antes de medir o capturar.'}
    # P0-B: las variantes *b usan el circuito B (persecución vertical) y comparan heredado/navegación.
    $pursuit=$Action.EndsWith('b')
    # P0-C: las variantes *c usan el circuito C en varias semillas QA dentro de una sola ejecución.
    $procedural=$Action.EndsWith('c')
    $seeds=@('P0QA','COLINAS7','HORDA750')
    $dev=$Action -in @('devdiag','devdiagb','devdiagc')
    $evidence=$Action -in @('evidence','evidenceb','evidencec')
    $folder=if($dev){'P0Dev'}else{'P0'}
    $player=Join-Path $project "Builds/$folder/MAMPORRO-P0.exe"
    if(!(Test-Path -LiteralPath $player)){throw 'Falta la build QA correspondiente.'}
    $kind=if($evidence){'Evidence'}elseif($dev){'Development'}else{'Normal'}
    if($pursuit){$kind='Pursuit'+$kind}
    if($procedural){$kind='Procedural'+$kind}
    $output=Join-Path $results ($kind+'/'+(Get-Date -Format 'yyyyMMddTHHmmss'))
    New-Item -ItemType Directory -Force -Path $output | Out-Null
    $source=Get-Content -LiteralPath (Join-Path (Split-Path $player -Parent) 'P0-source.txt') -Raw
    $source=$source.Trim()
    $mode=if($evidence){'-p0-evidence'}else{'-p0-benchmark'}
    if($pursuit){$mode+=' -p0-circuit B'}
    if($procedural){$mode+=' -p0-circuit C -p0-seeds '+($seeds -join ',')}
    # Ventana QA visible para las capturas y medidas gráficas solicitadas: oculta devuelve negro.
    $process=Start-Process -FilePath $player -WorkingDirectory $project -ArgumentList "-monitor 1 -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -force-d3d11 -p0-qa $mode -p0-source $source -p0-output `"$output`" -u4-save-dir `"$(Join-Path $output Save)`" -logFile `"$(Join-Path $output player.log)`"" -WindowStyle Normal -PassThru
    # C: tres semillas × cuatro cargas × 40 s más la generación de cada mapa.
    $limit=if($procedural){1200000}else{600000}
    if(!$process.WaitForExit($limit)){Stop-Process -Id $process.Id -Force;throw "P0 agotó el tiempo: $output"}
    if($process.ExitCode -ne 0){throw "P0 falló: $output"}
    $expected=if($evidence){@('evidence.json')}elseif($procedural){foreach($s in $seeds){foreach($c in 0,300,500,750){"load-$s-$c.json"}}}elseif($pursuit){foreach($m in 'heredado','navegacion'){foreach($c in 0,300,500,750){"load-$m-$c.json"}}}else{@('load-0.json','load-300.json','load-500.json','load-750.json')}
    foreach($name in $expected){
        $report=Get-Content -LiteralPath (Join-Path $output $name) -Raw | ConvertFrom-Json
        if(!$report.valid){throw "Informe no válido: $name"}
        if(!$evidence -and $report.development -ne $dev){throw 'Tipo de build inesperado.'}
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
