param([ValidateSet('create','edit','play','build','visual')][string]$Action='edit')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$project=Join-Path $repo 'unity'
$results=Join-Path $project 'TestResults\U3'
New-Item -ItemType Directory -Force -Path $results | Out-Null
if($Action -eq 'visual') {
    $player=Join-Path $project 'Builds\U3\Mamporro-U3.exe'
    if(!(Test-Path -LiteralPath $player)){throw 'Primero genera la build U3.'}
    $visual=Join-Path $results 'Visual'
    $started=Get-Date
    $log=Join-Path $results 'visual.log'
    # La comprobación gráfica solicitada requiere ventana visible: oculta captura negro.
    $process=Start-Process -FilePath $player -WorkingDirectory $project -ArgumentList "-screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -u3-visual-check -u3-output `"$visual`" -logFile `"$log`"" -WindowStyle Normal -PassThru
    if(!$process.WaitForExit(120000)){throw "La comprobación visual no terminó en 120 s: $log"}
    if($process.ExitCode -ne 0){throw "Build terminó con error: $log"}
    Add-Type -AssemblyName System.Drawing
    foreach($name in @('inicio','vista-alta','sitio-house','sitio-temple','sitio-farm','sitio-well','combate','cartas','reinicio')) {
        $capture=Get-Item -LiteralPath (Join-Path $visual "$name.png")
        if($capture.LastWriteTime -lt $started -or $capture.Length -eq 0){throw "Captura ausente o antigua: $name"}
        $bitmap=[System.Drawing.Bitmap]::FromFile($capture.FullName)
        try {
            $colors=@{}
            for($y=0;$y -lt $bitmap.Height;$y+=31){for($x=0;$x -lt $bitmap.Width;$x+=31){$colors[$bitmap.GetPixel($x,$y).ToArgb()]=1}}
            if($colors.Count -lt 16){throw "Captura sin imagen útil: $name"}
        } finally {$bitmap.Dispose()}
    }
    Write-Output 'visual : 9 capturas nuevas (no es un ensayo de rendimiento)'
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
