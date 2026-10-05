# Entrega de MAMPORRO (U6): paquete local reproducible y su verificación.
#   scripts\mamporro.cmd package  -> unity\Builds\Paquete\ (carpeta, ZIP, MANIFIESTO.json, .sha256)
#   scripts\mamporro.cmd verify   -> extrae el ZIP en una carpeta temporal con espacios, comprueba
#                                    huellas y arranca MAMPORRO.exe -u6-smoke con guardado temporal.
# Requiere antes `scripts\u3.cmd build`. No publica nada ni toca el guardado personal.
param([ValidateSet('package','verify')][string]$Action='package')
$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $PSScriptRoot
$unity=Join-Path $repo 'unity'
$build=Join-Path $unity 'Builds\Windows'
$out=Join-Path $unity 'Builds\Paquete'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
# Lo que Unity deja junto a la build y no se distribuye.
$excludedDirs=@('*_BackUpThisFolder_ButDontShipItWithYourGame','*_BurstDebugInformation_DoNotShip')
$excludedFiles=@('*.pdb','*.log')
# Ajustes locales del autor que nunca forman parte de una entrega (no cuentan como cambios del código).
$protected=@('unity/Assets/Mamporro/Generated/RetroPipeline.asset','unity/Assets/UniversalRenderPipelineGlobalSettings.asset','unity/ProjectSettings/GraphicsSettings.asset','unity/ProjectSettings/ProjectAuditorSettings.asset','unity/ProjectSettings/ProjectSettings.asset','unity/ProjectSettings/PackageManagerSettings.asset','unity/ProjectSettings/URPProjectSettings.asset')

function Sha256([string]$path){(Get-FileHash -Algorithm SHA256 -LiteralPath $path).Hash.ToLowerInvariant()}
function Invoke-Git([string[]]$a){$r=& git -C $repo @a 2>$null;if($LASTEXITCODE -ne 0){throw "git $($a -join ' ') falló"};$r}

if($Action -eq 'package'){
    $exe=Join-Path $build 'MAMPORRO.exe'
    if(!(Test-Path -LiteralPath $exe)){throw 'Falta unity\Builds\Windows\MAMPORRO.exe: ejecuta antes scripts\u3.cmd build.'}
    $commit=(Invoke-Git @('rev-parse','HEAD')).Trim();$short=$commit.Substring(0,7)
    $branch=(Invoke-Git @('rev-parse','--abbrev-ref','HEAD')).Trim()
    $commitDate=(Invoke-Git @('log','-1','--format=%cI',$commit)).Trim()
    $editor=((Get-Content (Join-Path $unity 'ProjectSettings\ProjectVersion.txt') | Where-Object {$_ -like 'm_EditorVersion:*'}) -replace 'm_EditorVersion:\s*','').Trim()
    $changes=@(Invoke-Git @('status','--porcelain','--untracked-files=all') | ForEach-Object {$_.Substring(3).Trim('"')} | Where-Object {$protected -notcontains $_})
    if($changes.Count -gt 0){Write-Warning ("Hay cambios locales sin publicar además de los ajustes protegidos: "+($changes -join ', '))}
    $name="MAMPORRO-Windows-x64-$short"
    $stage=Join-Path $out $name
    if(Test-Path -LiteralPath $out){Remove-Item -LiteralPath $out -Recurse -Force}
    New-Item -ItemType Directory -Force -Path $stage | Out-Null
    # Copia de la build sin lo que no se distribuye.
    Get-ChildItem -LiteralPath $build -Recurse -File | ForEach-Object {
        $relative=$_.FullName.Substring($build.Length+1)
        $top=$relative.Split('\')[0]
        foreach($pattern in $excludedDirs){if($top -like $pattern){return}}
        foreach($pattern in $excludedFiles){if($_.Name -like $pattern){return}}
        $target=Join-Path $stage $relative
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $target) | Out-Null
        Copy-Item -LiteralPath $_.FullName -Destination $target
    }
    $readme=@"
MAMPORRO — build Windows x64 (Unity $editor, Mono)
Commit $commit ($branch)

ARRANCAR
  Descomprime la carpeta entera donde quieras (vale una ruta con espacios) y ejecuta MAMPORRO.exe.
  Conserva todos los archivos y carpetas junto al ejecutable (MAMPORRO_Data, MonoBleedingEdge, DLL).
  No necesita Unity, Node ni conexión.

PROGRESO
  Se guarda en %USERPROFILE%\AppData\LocalLow\Mamporro\MAMPORRO\Progress (progress.json y copia progress.backup.json).
  Si existe progreso de una versión anterior (...\Mamporro\Mamporro U1\Progress), el juego lo ofrece para revisarlo
  y copiarlo tras tu confirmación; el original no se modifica ni se borra y nunca se fusionan progresos.
  Progreso de la versión web: Opciones -> Importar progreso web (archivo mamporro-progreso.json).

PROBAR SIN TOCAR TU PROGRESO
  MAMPORRO.exe -u4-save-dir "%TEMP%\MamporroPrueba"
  Con -u4-save-dir no se lee ni se ofrece el progreso personal.

CONTROLES
  WASD moverse, ratón mirar, Espacio saltar, Mayús/C deslizarse, E usar, Esc pausa. F3 panel de depuración
  (usar sus acciones invalida la recompensa de esa partida). Alt+F4 cierra.

LIMITACIONES CONOCIDAS
  Presentación técnica provisional (modelos de cajas, avatar provisional, fuente integrada); compresor de audio
  aproximado; sin foco el juego se pausa y se silencia. Detalle en docs/EQUIVALENCIA_U5.md y docs/PROGRESO_U6.md
  del repositorio. MANIFIESTO.json lista cada archivo con su tamaño y SHA-256.
"@
    [IO.File]::WriteAllText((Join-Path $stage 'LEEME.txt'),$readme.Replace("`r`n","`n").Replace("`n","`r`n"),(New-Object Text.UTF8Encoding $true))
    # Manifiesto: archivos ordenados (ordinal), tamaño y SHA-256; fecha = la del commit (reproducible).
    $files=@(Get-ChildItem -LiteralPath $stage -Recurse -File | ForEach-Object {
        [pscustomobject]@{path=$_.FullName.Substring($stage.Length+1).Replace('\','/');size=$_.Length;sha256=(Sha256 $_.FullName)}
    })
    [Array]::Sort($files,[Comparison[object]]{param($a,$b)[string]::CompareOrdinal($a.path,$b.path)})
    $manifest=[ordered]@{
        product='MAMPORRO';company='Mamporro';version='1.0';executable='MAMPORRO.exe';platform='Windows x64';backend='Mono';
        buildType='normal (no Development)';unityEditor=$editor;commit=$commit;branch=$branch;commitDate=$commitDate;
        localChangesBesidesProtected=$changes;excluded=@($excludedDirs+$excludedFiles);
        fileCount=$files.Count;totalBytes=($files | Measure-Object -Property size -Sum).Sum;files=$files
    }
    $json=($manifest | ConvertTo-Json -Depth 5)
    [IO.File]::WriteAllText((Join-Path $stage 'MANIFIESTO.json'),$json.Replace("`r`n","`n"),(New-Object Text.UTF8Encoding $false))
    # ZIP determinista: entradas en orden ordinal, fecha fija (la del commit) y misma compresión.
    $zip=Join-Path $out "$name.zip"
    $stamp=[DateTimeOffset]::Parse($commitDate).ToUniversalTime().DateTime
    $entries=@(Get-ChildItem -LiteralPath $stage -Recurse -File | ForEach-Object {$_.FullName.Substring($stage.Length+1).Replace('\','/')})
    [Array]::Sort($entries,[StringComparer]::Ordinal)
    $stream=[IO.File]::Open($zip,[IO.FileMode]::CreateNew)
    try{
        $archive=New-Object IO.Compression.ZipArchive($stream,[IO.Compression.ZipArchiveMode]::Create)
        try{
            foreach($entry in $entries){
                $e=$archive.CreateEntry("$name/$entry",[IO.Compression.CompressionLevel]::Optimal);$e.LastWriteTime=$stamp
                $w=$e.Open();try{$bytes=[IO.File]::ReadAllBytes((Join-Path $stage $entry.Replace('/','\')));$w.Write($bytes,0,$bytes.Length)}finally{$w.Dispose()}
            }
        }finally{$archive.Dispose()}
    }finally{$stream.Dispose()}
    $hash=Sha256 $zip
    [IO.File]::WriteAllText("$zip.sha256","$hash  $name.zip`n",(New-Object Text.UTF8Encoding $false))
    Write-Output "paquete : $stage"
    Write-Output "zip     : $zip ($([math]::Round((Get-Item -LiteralPath $zip).Length/1MB,1)) MB)"
    Write-Output "sha256  : $hash"
    Write-Output "archivos: $($files.Count) ($([math]::Round($manifest.totalBytes/1MB,1)) MB sin comprimir)"
}
else{
    $zip=@(Get-ChildItem -LiteralPath $out -Filter 'MAMPORRO-Windows-x64-*.zip' -ErrorAction SilentlyContinue)
    if($zip.Count -ne 1){throw 'Falta el paquete: ejecuta antes scripts\mamporro.cmd package.'}
    $zip=$zip[0].FullName
    $expected=((Get-Content -LiteralPath "$zip.sha256" -Raw).Split(' ')[0]).Trim()
    if((Sha256 $zip) -ne $expected){throw 'El SHA-256 del ZIP no coincide con su .sha256.'}
    $root=Join-Path $env:TEMP ("MAMPORRO entrega "+[guid]::NewGuid().ToString('N').Substring(0,8))
    $target=Join-Path $root 'Carpeta con espacios'
    New-Item -ItemType Directory -Force -Path $target | Out-Null
    [IO.Compression.ZipFile]::ExtractToDirectory($zip,$target)
    $folder=@(Get-ChildItem -LiteralPath $target -Directory)[0].FullName
    $manifest=Get-Content -LiteralPath (Join-Path $folder 'MANIFIESTO.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $listed=@{};foreach($f in $manifest.files){$listed[$f.path]=$f}
    $present=@(Get-ChildItem -LiteralPath $folder -Recurse -File | ForEach-Object {$_.FullName.Substring($folder.Length+1).Replace('\','/')} | Where-Object {$_ -ne 'MANIFIESTO.json'})
    foreach($p in $present){
        if(!$listed.ContainsKey($p)){throw "Archivo no listado en el manifiesto: $p"}
        $f=$listed[$p];$full=Join-Path $folder $p.Replace('/','\')
        if((Get-Item -LiteralPath $full).Length -ne $f.size -or (Sha256 $full) -ne $f.sha256){throw "Huella distinta: $p"}
    }
    if($present.Count -ne $manifest.files.Count){throw 'Faltan archivos listados en el manifiesto.'}
    foreach($bad in @('Progress','progress.json','Player.log')){if(@(Get-ChildItem -LiteralPath $folder -Recurse -Filter $bad).Count -gt 0){throw "El paquete contiene $bad"}}
    $smoke=Join-Path $root 'humo';$save=Join-Path $root 'guardado de prueba';$log=Join-Path $root 'player.log'
    $p=Start-Process -FilePath (Join-Path $folder 'MAMPORRO.exe') -ArgumentList "-monitor 1 -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -u6-smoke -u3-output `"$smoke`" -u4-save-dir `"$save`" -logFile `"$log`"" -PassThru
    if(!$p.WaitForExit(120000)){throw "MAMPORRO.exe no terminó la comprobación: $log"}
    if($p.ExitCode -ne 0){throw "La comprobación de humo falló (código $($p.ExitCode)): $log"}
    $report=Get-Content -LiteralPath (Join-Path $smoke 'smoke-report.json') -Raw | ConvertFrom-Json
    if(!$report.exe.StartsWith($folder,[StringComparison]::OrdinalIgnoreCase)){throw "Se ejecutó otro MAMPORRO.exe: $($report.exe)"}
    if($report.productName -ne 'MAMPORRO' -or $report.development -or !$report.played){throw 'Identidad o partida inesperadas en la comprobación de humo.'}
    Write-Output "verify  : $($present.Count) archivos con su huella, ZIP $expected"
    Write-Output "humo    : $($report.productName) $($report.version) desde '$folder', partida $([math]::Round($report.runTime,1)) s, progreso en '$($report.saveDirectory)'"
    Remove-Item -LiteralPath $root -Recurse -Force
    Write-Output 'verify  : carpeta temporal eliminada'
}
