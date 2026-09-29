param(
    [ValidateSet('create','edit','play','build','benchmark')][string]$Action = 'edit'
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$project = Join-Path $repo 'unity'
$editor = 'C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe'
$results = Join-Path $project 'TestResults'
New-Item -ItemType Directory -Force -Path $results | Out-Null
if ($Action -eq 'benchmark') {
    $player = Join-Path $project 'Builds\U1\Mamporro-U1.exe'
    if (!(Test-Path -LiteralPath $player)) { throw 'Primero genera la build.' }
    foreach ($size in @(@(1920,1080), @(2560,1440))) {
        $width = $size[0]; $height = $size[1]
        $log = Join-Path $results "player-${width}x${height}.log"
        $arguments = "-screen-fullscreen 1 -window-mode exclusive -screen-width $width -screen-height $height -u1-benchmark -u1-output `"$results`" -logFile `"$log`""
        $runStarted = Get-Date
        # La build es una prueba gráfica interactiva: debe tener una ventana visible.
        $process = Start-Process -FilePath $player -ArgumentList $arguments -WorkingDirectory $project -PassThru
        $process.WaitForExit()
        if ($process.ExitCode -ne 0) { throw "Build terminada con código $($process.ExitCode); consulta $log" }
        $reports = @(Get-ChildItem -LiteralPath $results -Filter "u1-player-${width}x${height}-*.json" | Where-Object { $_.LastWriteTime -ge $runStarted })
        if ($reports.Count -ne 4) { throw 'No se completaron los cuatro escenarios.' }
        foreach ($file in $reports) {
            $data = Get-Content -Raw -LiteralPath $file.FullName | ConvertFrom-Json
            if (!$data.validRender) { throw "Ensayo sin render válido o buffer agotado: $($file.Name)" }
        }
    }
} else {
    if (Get-Process Unity -ErrorAction SilentlyContinue) { throw 'Hay un Editor Unity abierto. No se iniciará otra instancia; cierra tu proyecto cuando te convenga.' }
    $log = Join-Path $results "$Action.log"
    $arguments = "-batchmode -projectPath `"$project`" -logFile `"$log`""
    switch ($Action) {
        'create' { $arguments += ' -quit -executeMethod Mamporro.Editor.U1Project.Create' }
        'build' { $arguments += ' -quit -executeMethod Mamporro.Editor.U1Project.Build' }
        'edit' { $arguments += " -nographics -runTests -testPlatform EditMode -testResults `"$results\edit.xml`"" }
        'play' { $arguments += " -runTests -testPlatform PlayMode -testResults `"$results\play.xml`"" }
    }
    $process = Start-Process -FilePath $editor -ArgumentList $arguments -WorkingDirectory $project -WindowStyle Hidden -PassThru
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) { throw "Unity terminó con código $($process.ExitCode); consulta $log" }
    if ($Action -in @('edit','play')) {
        [xml]$report = Get-Content -Raw -LiteralPath (Join-Path $results "$Action.xml")
        if ($report.'test-run'.result -ne 'Passed') { throw 'Las pruebas no han pasado.' }
        Write-Output "$Action : $($report.'test-run'.passed) pruebas correctas"
    }
}
Write-Output "Resultados locales: $results"
