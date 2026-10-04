param([ValidateSet('create','edit','play','build','visual','benchmark','devdiag')][string]$Action='edit')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$project=Join-Path $repo 'unity'
$results=Join-Path $project 'TestResults\U3'
New-Item -ItemType Directory -Force -Path $results | Out-Null
if($Action -eq 'edit') {
    # Entradas reales web para la prueba de compatibilidad U4; nunca genera esperados.
    $exportLog=Join-Path $project 'TestResults\U4\step4-export-fixtures.log'
    New-Item -ItemType Directory -Force -Path (Split-Path $exportLog -Parent) | Out-Null
    node (Join-Path $repo 'scripts\u4-export-fixtures.mjs') | Tee-Object -FilePath $exportLog
    if($LASTEXITCODE -ne 0){throw 'Falló la preparación de transferencias web U4.'}
}
if($Action -eq 'devdiag') {
    # Diagnóstico Development (U5): build de desarrollo aparte, una pasada a 1920x1080. Sus FPS
    # no son el rendimiento final; sirve para GC/asignaciones, memoria y GPU si es válida.
    if(Get-Process Unity -ErrorAction SilentlyContinue){throw 'Hay un Editor abierto. Guarda y cierra la instancia antes de ejecutar batch.'}
    $log=Join-Path $results 'devdiag-build.log'
    $process=Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe' -ArgumentList "-batchmode -projectPath `"$project`" -logFile `"$log`" -quit -executeMethod Mamporro.Editor.U3Project.BuildDevelopment" -WorkingDirectory $project -WindowStyle Hidden -PassThru
    $process.WaitForExit();if($process.ExitCode -ne 0){throw "Falló la build Development: $log"}
}
if($Action -in @('benchmark','devdiag')) {
    $dev=$Action -eq 'devdiag'
    $player=Join-Path $project $(if($dev){'Builds\U3Dev\Mamporro-U3.exe'}else{'Builds\U3\Mamporro-U3.exe'})
    if(!(Test-Path -LiteralPath $player)){throw 'Primero genera la build U3.'}
    if($dev){$results=Join-Path $results 'DevDiag';New-Item -ItemType Directory -Force -Path $results | Out-Null}
    $summary=@()
    # Asignación directa: en PowerShell la salida de un bloque desenrolla los arrays anidados.
    $sizes=@(@(1920,1080),@(2560,1440));if($dev){$sizes=@(,$sizes[0])}
    foreach($size in $sizes) {
    foreach($profile in @('horda','armas')) {
        $width=$size[0];$height=$size[1];$started=Get-Date
        $log=Join-Path $results "player-${width}x${height}-$profile.log"
        # Ensayo gráfico explícitamente visible; no ocultar ni usar -nographics. Siempre en el
        # monitor principal (-monitor 1): Unity recuerda el último monitor y en uno de 1080p
        # el punto de 2560x1440 se haría a 1920x1080.
        $process=Start-Process -FilePath $player -WorkingDirectory $project -ArgumentList "-monitor 1 -screen-fullscreen 1 -window-mode exclusive -screen-width $width -screen-height $height -u3-benchmark -u5-profile $profile -u3-output `"$results`" -u4-save-dir `"$(Join-Path $results BenchmarkSave)`" -logFile `"$log`"" -WindowStyle Normal -PassThru
        if(!$process.WaitForExit(900000)){throw "El ensayo no terminó en 15 min: $log"}
        if($process.ExitCode -ne 0){throw "Build terminó con error: $log"}
        $reports=@(Get-ChildItem -LiteralPath $results -Filter "u3-${width}x${height}-$profile-*.json" | Where-Object {$_.LastWriteTime -ge $started})
        if($reports.Count -ne 4){throw 'No se completaron los cuatro puntos (minutos 2, 5, 9 y enjambre).'}
        foreach($report in $reports){
            $data=Get-Content -Raw $report.FullName | ConvertFrom-Json
            if(!$data.validRender -or $data.outputWidth -ne $width -or $data.outputHeight -ne $height){throw "Render/resolución inválidos: $($report.Name)"}
            if($dev -ne [bool]$data.development){throw "Tipo de build inesperado en $($report.Name)"}
            $summary+=[pscustomobject]@{salida="${width}x${height}";perfil=$profile;punto=$data.scenario;fps=[math]::Round($data.meanFps,1);p95ms=[math]::Round($data.p95,2);p99ms=[math]::Round($data.p99,2);max=[math]::Round($data.max,2);lentos=$data.framesOverBudget;enemigos="$($data.minEntities)-$($data.maxEntities)";particulas=$data.particlesMax;voces=$data.voicesMax;sonidos="$($data.soundsPlayed)/$($data.soundsDropped)";gc=$data.gcCollections;gcB=[math]::Round($data.gcBytesPerFrame,0);tickMs=[math]::Round($data.tickMean,3);gpuMs=[math]::Round($data.gpuMean,2)}
        }
    }
    }
    if($dev){Write-Output 'DIAGNÓSTICO DEVELOPMENT: no es el rendimiento de la build final.'}
    $summary | Format-Table -AutoSize | Out-String -Width 400 | Write-Output
} elseif($Action -eq 'visual') {
    $player=Join-Path $project 'Builds\U3\Mamporro-U3.exe'
    if(!(Test-Path -LiteralPath $player)){throw 'Primero genera la build U3.'}
    $visual=Join-Path $results 'Visual'
    $started=Get-Date
    $log=Join-Path $results 'visual.log'
    # La comprobación gráfica solicitada requiere ventana visible: oculta captura negro.
    $process=Start-Process -FilePath $player -WorkingDirectory $project -ArgumentList "-screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -u3-visual-check -u3-output `"$visual`" -u4-save-dir `"$(Join-Path $visual VisualSave)`" -logFile `"$log`"" -WindowStyle Normal -PassThru
    if(!$process.WaitForExit(300000)){throw "La comprobación visual no terminó en 300 s: $log"}
    if($process.ExitCode -ne 0){throw "Build terminó con error: $log"}
    Add-Type -AssemblyName System.Drawing
    foreach($name in @('inicio','tienda','misiones','opciones','opciones-en','importar','vista-alta','sitio-house','sitio-temple','sitio-farm','sitio-well','combate','interactuables','telegrafiado','pausa','pausa-opciones','cartas','resultados','reinicio')) {
        $capture=Get-Item -LiteralPath (Join-Path $visual "$name.png")
        if($capture.LastWriteTime -lt $started -or $capture.Length -eq 0){throw "Captura ausente o antigua: $name"}
        $bitmap=[System.Drawing.Bitmap]::FromFile($capture.FullName)
        try {
            $colors=@{}
            for($y=0;$y -lt $bitmap.Height;$y+=31){for($x=0;$x -lt $bitmap.Width;$x+=31){$colors[$bitmap.GetPixel($x,$y).ToArgb()]=1}}
            if($colors.Count -lt 16){throw "Captura sin imagen útil: $name"}
        } finally {$bitmap.Dispose()}
    }
    $audio=Get-Item -LiteralPath (Join-Path $visual 'audio-report.json')
    if($audio.LastWriteTime -lt $started){throw 'Falta la medida de audio nueva (audio-report.json).'}
    Write-Output ('audio  : '+((Get-Content -Raw $audio.FullName | ConvertFrom-Json) | ConvertTo-Json -Compress))
    $sessions=Get-Item -LiteralPath (Join-Path $visual 'sessions-report.json')
    if($sessions.LastWriteTime -lt $started){throw 'Falta el ensayo de varias partidas (sessions-report.json).'}
    Write-Output ('partidas: '+((Get-Content -Raw $sessions.FullName | ConvertFrom-Json) | ConvertTo-Json -Compress))
    Write-Output 'visual : 19 capturas nuevas (no es un ensayo de rendimiento)'
    # U5: las 11 pantallas en español e inglés a 1280x720 y 1920x1080 (44 capturas).
    foreach($size in @(@(1280,720),@(1920,1080))){foreach($language in @('es','en')){
        $w=$size[0];$h=$size[1];$folder=Join-Path $visual "$language-${w}x$h";$started=Get-Date
        $slog=Join-Path $results "visual-$language-${w}x$h.log"
        $process=Start-Process -FilePath $player -WorkingDirectory $project -ArgumentList "-monitor 1 -screen-fullscreen 0 -screen-width $w -screen-height $h -u3-visual-check -u5-screens $language -u3-output `"$folder`" -u4-save-dir `"$(Join-Path $folder VisualSave)`" -logFile `"$slog`"" -WindowStyle Normal -PassThru
        if(!$process.WaitForExit(180000)){throw "Las capturas $language ${w}x$h no terminaron: $slog"}
        if($process.ExitCode -ne 0){throw "Build terminó con error: $slog"}
        foreach($name in @('inicio','preparacion','tienda','misiones','opciones','partida','horda','jefe','cartas','pausa','resultados')){
            $capture=Get-Item -LiteralPath (Join-Path $folder "$name.png")
            if($capture.LastWriteTime -lt $started -or $capture.Length -eq 0){throw "Captura ausente o antigua: $language ${w}x$h $name"}
            $bitmap=[System.Drawing.Bitmap]::FromFile($capture.FullName)
            try{if($bitmap.Width -ne $w -or $bitmap.Height -ne $h){throw "Tamaño inesperado: $language ${w}x$h $name"}}finally{$bitmap.Dispose()}
        }
    }}
    Write-Output 'visual : 44 capturas ES/EN (1280x720 y 1920x1080)'
} else {
    if(Get-Process Unity -ErrorAction SilentlyContinue){throw 'Hay un Editor abierto. Guarda y cierra la instancia antes de ejecutar batch.'}
    $log=Join-Path $results "$Action.log"
    $args="-batchmode -projectPath `"$project`" -logFile `"$log`""
    switch($Action){
        'create' {$args+=' -quit -executeMethod Mamporro.Editor.U3Project.Create'}
        'build' {$args+=' -quit -executeMethod Mamporro.Editor.U3Project.Build'}
        'edit' {$args+=" -nographics -runTests -testPlatform EditMode -testResults `"$results\edit.xml`""}
        'play' {$args+=" -runTests -testPlatform PlayMode -testResults `"$results\play.xml`""}
    }
    $process=Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe' -ArgumentList $args -WorkingDirectory $project -WindowStyle Hidden -PassThru
    $process.WaitForExit()
    if($process.ExitCode -ne 0){throw "Unity terminó con error: $log"}
    if($Action -in @('edit','play')){[xml]$report=Get-Content -Raw (Join-Path $results "$Action.xml");if($report.'test-run'.result -ne 'Passed'){throw 'Pruebas fallidas'};Write-Output "$Action : $($report.'test-run'.passed) pruebas correctas"}
}
Write-Output "Resultados locales: $results"
