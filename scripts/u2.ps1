param([ValidateSet('create','edit','play','build','benchmark')][string]$Action='edit')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$project=Join-Path $repo 'unity'
$results=Join-Path $project 'TestResults\U2'
New-Item -ItemType Directory -Force -Path $results | Out-Null
if($Action -eq 'benchmark') {
    $player=Join-Path $project 'Builds\U2\Mamporro-U2.exe'
    if(!(Test-Path -LiteralPath $player)){throw 'Primero genera la build U2.'}
    foreach($size in @(@(1920,1080),@(2560,1440))) {
        $width=$size[0];$height=$size[1];$started=Get-Date
        $log=Join-Path $results "player-${width}x${height}.log"
        # Ensayo gráfico explícitamente visible; no ocultar ni usar -nographics.
        $process=Start-Process -FilePath $player -WorkingDirectory $project -ArgumentList "-screen-fullscreen 1 -window-mode exclusive -screen-width $width -screen-height $height -u2-benchmark -u2-output `"$results`" -logFile `"$log`"" -WindowStyle Normal -PassThru
        $process.WaitForExit()
        if($process.ExitCode -ne 0){throw "Build terminó con error: $log"}
        $reports=@(Get-ChildItem -LiteralPath $results -Filter "u2-${width}x${height}-*.json" | Where-Object {$_.LastWriteTime -ge $started})
        if($reports.Count -ne 4){throw 'No se completaron las cuatro cargas.'}
        foreach($report in $reports){$data=Get-Content -Raw $report.FullName | ConvertFrom-Json;if(!$data.validRender -or $data.outputWidth -ne $width -or $data.outputHeight -ne $height){throw "Render/resolución inválidos: $($report.Name)"}}
    }
} else {
    if(Get-Process Unity -ErrorAction SilentlyContinue){throw 'Hay un Editor abierto. Guarda y cierra la instancia antes de ejecutar batch.'}
    $log=Join-Path $results "$Action.log"
    $args="-batchmode -projectPath `"$project`" -logFile `"$log`""
    switch($Action){
        'create' {$args+=' -quit -executeMethod Mamporro.Editor.U2Project.Create'}
        'build' {$args+=' -quit -executeMethod Mamporro.Editor.U2Project.Build'}
        'edit' {$args+=" -nographics -runTests -testPlatform EditMode -testResults `"$results\edit.xml`""}
        'play' {$args+=" -runTests -testPlatform PlayMode -testResults `"$results\play.xml`""}
    }
    $process=Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe' -ArgumentList $args -WorkingDirectory $project -WindowStyle Hidden -PassThru
    $process.WaitForExit()
    if($process.ExitCode -ne 0){throw "Unity terminó con error: $log"}
    if($Action -in @('edit','play')){[xml]$report=Get-Content -Raw (Join-Path $results "$Action.xml");if($report.'test-run'.result -ne 'Passed'){throw 'Pruebas fallidas'};Write-Output "$Action : $($report.'test-run'.passed) pruebas correctas"}
}
Write-Output "Resultados locales: $results"
