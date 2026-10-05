param([ValidateSet('create','build','devbuild','benchmark','devdiag','summary','edit','play','visual','jugar')][string]$Action='benchmark',[int]$Runs=3,[int]$First=1,[string]$Strategies='base',[string]$Source='',[ValidateSet('both','1080','1440')][string]$Sizes='both',[switch]$Avatar)
# B0 (spike Blender): builds QA separadas y ensayo de presentación de la horda. No toca la
# entrega aprobada (unity\Builds\Windows) ni el progreso personal (guardado propio del ensayo).
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$project=Join-Path $repo 'unity'
$unity='C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe'
$results=Join-Path $project 'TestResults\B0\Benchmark'
New-Item -ItemType Directory -Force -Path $results | Out-Null
function Invoke-Build([string]$method,[string]$log){
    if(Get-Process Unity -ErrorAction SilentlyContinue){throw 'Hay un Editor abierto. Guarda y cierra la instancia antes de ejecutar batch.'}
    $process=Start-Process -FilePath $unity -ArgumentList "-batchmode -projectPath `"$project`" -logFile `"$log`" -quit -executeMethod Mamporro.Editor.$method" -WorkingDirectory $project -WindowStyle Hidden -PassThru
    $process.WaitForExit();if($process.ExitCode -ne 0){throw "Falló la build ($method): $log"}
}
if($Action -in @('visual','jugar')){
    $player=Join-Path $project 'Builds\B0\MAMPORRO-B0.exe'
    if(!(Test-Path -LiteralPath $player)){throw 'Primero genera la build: scripts\b0.cmd build'}
    if($Action -eq 'jugar'){
        # Revisión manual del autor: build QA con el visual B0 y guardado QA propio (nunca el personal).
        $save=Join-Path $project 'TestResults\B0\ManualSave'
        Start-Process -FilePath $player -WorkingDirectory $project -ArgumentList "-screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -b0-visual -u4-save-dir `"$save`" -logFile `"$(Join-Path $project 'TestResults\B0\manual.log')`""
        Write-Output "B0 jugar: build QA con visual B0; progreso QA en $save";return
    }
    $out=Join-Path $project 'TestResults\B0\Visual';New-Item -ItemType Directory -Force -Path $out | Out-Null;$started=Get-Date
    $process=Start-Process -FilePath $player -WorkingDirectory $project -ArgumentList "-monitor 1 -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -b0-visual -b0-visual-check -b0-output `"$out`" -u4-save-dir `"$(Join-Path $out VisualSave)`" -logFile `"$(Join-Path $out 'visual.log')`"" -WindowStyle Normal -PassThru
    if(!$process.WaitForExit(180000)){Stop-Process -Id $process.Id -Force;throw 'La comprobación visual B0 no terminó en 180 s'}
    if($process.ExitCode -ne 0){throw "La build terminó con error $($process.ExitCode): $(Join-Path $out 'visual.log')"}
    foreach($name in @('b0-juego','b0-colliders','b0-u6-mismo-instante','b0-andar-0','b0-andar-5','b0-inactiva','b0-reinicio')){
        $f=Get-Item -LiteralPath (Join-Path $out "$name.png");if($f.LastWriteTime -lt $started -or $f.Length -eq 0){throw "Captura ausente o antigua: $name"}
    }
    Write-Output ('B0 visual: '+((Get-Content -Raw (Join-Path $out 'b0-visual.json') | ConvertFrom-Json) | ConvertTo-Json -Compress));Write-Output "Capturas: $out";return
}
if($Action -eq 'play'){
    # Solo las pruebas Play B0 (integración visual); la batería completa sigue en scripts\u3.cmd play.
    if(Get-Process Unity -ErrorAction SilentlyContinue){throw 'Hay un Editor abierto. Guarda y cierra la instancia antes de ejecutar batch.'}
    $xml=Join-Path $results 'play-b0.xml';$log=Join-Path $results 'play-b0.log';Remove-Item -LiteralPath $xml -ErrorAction SilentlyContinue
    $process=Start-Process -FilePath $unity -ArgumentList "-batchmode -projectPath `"$project`" -logFile `"$log`" -runTests -testPlatform PlayMode -testFilter Mamporro.Tests.B0VisualSceneTests -testResults `"$xml`"" -WorkingDirectory $project -WindowStyle Hidden -PassThru
    $process.WaitForExit()
    if(!(Test-Path -LiteralPath $xml)){throw "Unity no generó resultados: $log"}
    [xml]$report=Get-Content -Raw $xml
    foreach($t in $report.SelectNodes('//test-case')){Write-Output "$($t.result.PadRight(7)) $($t.name)$(if($t.result -ne 'Passed'){' -> '+$t.failure.message.InnerText})"}
    if($report.'test-run'.result -ne 'Passed'){throw "Pruebas Play B0 fallidas: $xml"}
    Write-Output "play B0: $($report.'test-run'.passed)/$($report.'test-run'.total) correctas";return
}
if($Action -eq 'edit'){
    # Solo las pruebas B0 (contrato de importación); la batería completa sigue en scripts\u3.cmd edit.
    if(Get-Process Unity -ErrorAction SilentlyContinue){throw 'Hay un Editor abierto. Guarda y cierra la instancia antes de ejecutar batch.'}
    $xml=Join-Path $results 'edit-b0.xml';$log=Join-Path $results 'edit-b0.log';Remove-Item -LiteralPath $xml -ErrorAction SilentlyContinue
    $process=Start-Process -FilePath $unity -ArgumentList "-batchmode -nographics -projectPath `"$project`" -logFile `"$log`" -runTests -testPlatform EditMode -assemblyNames Mamporro.B0Tests -testResults `"$xml`"" -WorkingDirectory $project -WindowStyle Hidden -PassThru
    $process.WaitForExit()
    if(!(Test-Path -LiteralPath $xml)){throw "Unity no generó resultados: $log"}
    [xml]$report=Get-Content -Raw $xml
    foreach($t in $report.SelectNodes('//test-case')){Write-Output "$($t.result.PadRight(7)) $($t.name)$(if($t.result -ne 'Passed'){' -> '+$t.failure.message.InnerText})"}
    if($report.'test-run'.result -ne 'Passed'){throw "Pruebas B0 fallidas: $xml"}
    Write-Output "edit B0: $($report.'test-run'.passed)/$($report.'test-run'.total) correctas";return
}
if($Action -eq 'create'){Invoke-Build 'B0Project.CreateQa' (Join-Path $results 'create.log');Write-Output 'B0 escena QA: unity\Assets\Mamporro\QA\B0\B0_QA.unity';return}
if($Action -eq 'build'){Invoke-Build 'U3Project.BuildB0' (Join-Path $results 'build.log');Write-Output 'B0 build QA normal: unity\Builds\B0\MAMPORRO-B0.exe';return}
if($Action -eq 'devbuild'){Invoke-Build 'U3Project.BuildB0Development' (Join-Path $results 'devbuild.log');Write-Output 'B0 build QA Development: unity\Builds\B0Dev\MAMPORRO-B0.exe';return}
$dev=$Action -eq 'devdiag'
$out=Join-Path $results $(if($dev){'DevDiag'}else{'Normal'})
New-Item -ItemType Directory -Force -Path $out | Out-Null
$head=(git -C $repo rev-parse --short HEAD).Trim()
$dirty=@(git -C $repo status --porcelain -- 'unity/Assets/Mamporro' 'scripts').Count -gt 0
if(!$Source){$Source=$head+$(if($dirty){'+local'}else{''})}
function Row($file){
    $d=Get-Content -Raw $file.FullName | ConvertFrom-Json
    [pscustomobject]@{estrategia=$d.strategy+$(if($d.avatar){'+avatar'}else{''});entidades=$d.entities;salida="$($d.outputWidth)x$($d.outputHeight)";pasada=$d.run;frames=$d.frames;p50=$d.p50;p95=$d.p95;p99=$d.p99;max=$d.max;media=$d.mean;sobre16_67=$d.framesOver16_67;cpuFrame=$d.cpuFrameMean;envioCamara=$d.submitCpuMean;instancias=$d.instancesMax;setPass=$d.setPassMean;triangulos=$d.trianglesMean;vertices=$d.verticesMean;drawCalls=$d.drawCallsMean;batches=$d.batchesMean;gcColecciones=$d.gcCollections;gcBytesFrame=$d.gcAllocMeanBytes;memoriaUnityMB=[math]::Round($d.memoryEnd/1MB,1);reservadaMB=[math]::Round($d.reserved/1MB,1);sistemaMB=$(if($d.systemMemoryEnd -gt 0){[math]::Round($d.systemMemoryEnd/1MB,1)}else{-1});heapMonoMB=[math]::Round($d.monoHeapEnd/1MB,1);development=$d.development;fuente=$d.source;cpuVisualHorda=$d.hordeCpuMean;instanciasHorda=$d.hordeInstances;vatBytes=$d.vatTextureBytes;posesBytes=$d.poseMeshBytes;informe=$file.Name}
}
if($Action -ne 'summary'){
$player=Join-Path $project $(if($dev){'Builds\B0Dev\MAMPORRO-B0.exe'}else{'Builds\B0\MAMPORRO-B0.exe'})
if(!(Test-Path -LiteralPath $player)){throw "Primero genera la build: scripts\b0.cmd $(if($dev){'devbuild'}else{'build'})"}
$save=Join-Path $out 'BenchmarkSave'
$sizes=@(@(1920,1080),@(2560,1440));if($Sizes -eq '1080'){$sizes=@(,$sizes[0])}elseif($Sizes -eq '1440'){$sizes=@(,$sizes[1])};if($dev){$sizes=@(,@(1920,1080));$Runs=1}
# Repeticiones alternadas: cada pasada recorre todas las condiciones antes de repetir ninguna.
for($run=$First;$run -lt $First+$Runs;$run++){
foreach($size in $sizes){
foreach($count in @(300,500,750)){
foreach($strategy in $Strategies.Split(',')){
    $width=$size[0];$height=$size[1];$started=Get-Date
    $tag=$strategy+$(if($Avatar){'+avatar'}else{''})
    $log=Join-Path $out "player-$tag-$count-${width}x$height-r$run.log"
    $extra=$(if($Avatar){' -b0-avatar'}else{''})+$(if($run -eq 1 -and $width -eq 1920 -and !$dev){' -b0-closeups'}else{''})
    # Ventana visible en exclusiva y monitor principal, como el ensayo U3/U5.
    $process=Start-Process -FilePath $player -WorkingDirectory $project -ArgumentList "-monitor 1 -screen-fullscreen 1 -window-mode exclusive -screen-width $width -screen-height $height -b0-benchmark$extra -b0-strategy $strategy -b0-count $count -b0-run $run -b0-source $Source -b0-output `"$out`" -u4-save-dir `"$save`" -logFile `"$log`"" -WindowStyle Normal -PassThru
    if(!$process.WaitForExit(240000)){Stop-Process -Id $process.Id -Force;throw "El ensayo no terminó en 240 s: $log"}
    if($process.ExitCode -ne 0){throw "La build terminó con error $($process.ExitCode): $log"}
    $report=@(Get-ChildItem -LiteralPath $out -Filter "$tag-$count-${width}x$height-r$run-*.json" | Where-Object {$_.LastWriteTime -ge $started})
    if($report.Count -ne 1){throw "Falta el informe nuevo: $log"}
    $d=Get-Content -Raw $report[0].FullName | ConvertFrom-Json
    if(!$d.validRender -or $d.entities -ne $count -or $d.entitiesInFrustum -ne $count -or $d.outputWidth -ne $width -or $d.outputHeight -ne $height){throw "Render, recuento o resolución inválidos: $($report[0].Name)"}
    if($dev -ne [bool]$d.development){throw "Tipo de build inesperado: $($report[0].Name)"}
    Write-Output "$tag $count ${width}x$height pasada $run`: p95 $([math]::Round($d.p95,3)) ms"
}}}}
}
# Resumen de todos los informes válidos de esta fuente (todas las pasadas ejecutadas).
$wanted=@($Strategies.Split(',') | ForEach-Object {$_+$(if($Avatar){'+avatar'}else{''})})
$rows=@(Get-ChildItem -LiteralPath $out -Filter '*.json' | ForEach-Object {Row $_} | Where-Object {$_.fuente -eq $Source -and $_.pasada -ge 1 -and $wanted -contains $_.estrategia})
if($rows.Count -eq 0){throw "No hay informes de la fuente $Source"}
$stamp=Get-Date -Format 'yyyyMMddTHHmmss'
$rows | Sort-Object estrategia,salida,entidades,pasada | Export-Csv -NoTypeInformation -Encoding UTF8 -Path (Join-Path $out "resumen-$stamp.csv")
function Median($v){$s=@($v|Sort-Object);$s[[int][math]::Floor(($s.Count-1)/2)]}
# Agregado por condición: mediana de las pasadas para p50/p95/p99, peor máximo y rango de p95.
$aggregate=$rows | Group-Object estrategia,entidades,salida | ForEach-Object {
    $g=$_.Group
    [pscustomobject]@{estrategia=$g[0].estrategia;entidades=$g[0].entidades;salida=$g[0].salida;pasadas=$g.Count;p50=[math]::Round((Median $g.p50),3);p95=[math]::Round((Median $g.p95),3);p95min=[math]::Round(($g.p95|Measure-Object -Minimum).Minimum,3);p95max=[math]::Round(($g.p95|Measure-Object -Maximum).Maximum,3);p99=[math]::Round((Median $g.p99),3);max=[math]::Round(($g.max|Measure-Object -Maximum).Maximum,3);sobre16_67=($g.sobre16_67|Measure-Object -Sum).Sum;cpuFrame=[math]::Round((Median $g.cpuFrame),3);envioCamara=[math]::Round((Median $g.envioCamara),3);instancias=$g[0].instancias;setPass=(Median $g.setPass);triangulos=[math]::Round((Median $g.triangulos),0);cpuVisualHorda=[math]::Round((Median $g.cpuVisualHorda),4);instanciasHorda=$g[0].instanciasHorda;gcColecciones=($g.gcColecciones|Measure-Object -Sum).Sum;gcBytesFrame=[math]::Round((Median $g.gcBytesFrame),1);memoriaUnityMB=(Median $g.memoriaUnityMB);sistemaMB=(Median $g.sistemaMB);suelo300=$(if($g[0].entidades -eq 300){if(($g.p95|Measure-Object -Maximum).Maximum -le 16.67){'cumple'}else{'NO cumple'}}else{'-'})}
} | Sort-Object estrategia,salida,entidades
$aggregate | Export-Csv -NoTypeInformation -Encoding UTF8 -Path (Join-Path $out "agregado-$stamp.csv")
if($dev){Write-Output 'DIAGNÓSTICO DEVELOPMENT: no es el rendimiento de la build normal.'}
$aggregate | Format-Table -AutoSize | Out-String -Width 400 | Write-Output
Write-Output "Fuente $Source · resultados locales: $out"
