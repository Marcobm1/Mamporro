param([ValidateSet('verify','author','export','test')][string]$Action='verify',[string]$Asset='',[switch]$Force)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$results=Join-Path $repo $(if($Action -eq 'verify'){'unity\TestResults\B0\Environment'}else{"unity\TestResults\B0\$((Get-Culture).TextInfo.ToTitleCase($Action))"})
New-Item -ItemType Directory -Force -Path $results | Out-Null
# Preferir selección explícita; no instalar ni cambiar PATH/preferencias del usuario.
$candidates=@()
if($env:MAMPORRO_BLENDER){$candidates=@($env:MAMPORRO_BLENDER)}else{
    $command=Get-Command blender.exe -ErrorAction SilentlyContinue
    if($command){$candidates+= $command.Source}
    foreach($key in @('HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall','HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall')){
        Get-ChildItem $key -ErrorAction SilentlyContinue | Get-ItemProperty | Where-Object {$_.DisplayName -eq 'Blender' -and $_.InstallLocation} | ForEach-Object {$candidates+=Join-Path $_.InstallLocation 'blender.exe'}
    }
    $standard=Join-Path $env:ProgramFiles 'Blender Foundation'
    if(Test-Path -LiteralPath $standard){Get-ChildItem -LiteralPath $standard -Directory | ForEach-Object {$candidates+=Join-Path $_.FullName 'blender.exe'}}
}
$candidates=@($candidates | Where-Object {Test-Path -LiteralPath $_ -PathType Leaf} | ForEach-Object {(Resolve-Path -LiteralPath $_).Path} | Sort-Object -Unique)
if($candidates.Count -ne 1){throw 'Selecciona el ejecutable real con MAMPORRO_BLENDER: no hay una instalación única identificada. No se instalará nada automáticamente.'}
$blender=$candidates[0]
$common=@('--background','--factory-startup','--disable-autoexec','--python-exit-code','1')
if($Action -ne 'verify'){
    $manifest=Get-Content -Raw -LiteralPath (Join-Path $repo 'art\blender\b0\manifest.json') | ConvertFrom-Json
    $ids=@($manifest.assets | ForEach-Object {$_.id});if($Asset){if($ids -notcontains $Asset){throw "Asset desconocido: $Asset"};$ids=@($Asset)}
    if($Action -eq 'test'){
        & $blender @common --python (Join-Path $PSScriptRoot 'blender\b0_test.py') -- (Join-Path $results 'test-report.json') | Tee-Object -FilePath (Join-Path $results 'test.log') | Where-Object {$_ -match '^(OK|FALLO|B0 test)'}
        if($LASTEXITCODE -ne 0){throw "Fallaron pruebas del contrato/exportador: $(Join-Path $results 'test.log')"}
        return
    }
    foreach($id in $ids){
        $script=Join-Path $PSScriptRoot $(if($Action -eq 'author'){'blender\b0_author.py'}else{'blender\b0_export.py'})
        $extra=@($id);if($Action -eq 'export'){$extra+=(Join-Path $results "$id.json")};if($Force){$extra+='--force'}
        & $blender @common --python $script -- @extra | Tee-Object -FilePath (Join-Path $results "$id.log") | Where-Object {$_ -match '^(B0|\{)'}
        if($LASTEXITCODE -ne 0){throw "Falló $Action de ${id}: $(Join-Path $results "$id.log")"}
    }
    Write-Output "B0 ${Action}: $($ids -join ', ') correcto. Informes: $results"
    return
}
& $blender --version | Tee-Object -FilePath (Join-Path $results 'version.log')
if($LASTEXITCODE -ne 0){throw 'Falló la consulta de versión Blender.'}
& $blender --background --factory-startup --disable-autoexec --python-exit-code 1 --python (Join-Path $PSScriptRoot 'blender\verify.py') -- (Join-Path $results 'verified.json') | Tee-Object -FilePath (Join-Path $results 'verify.log')
if($LASTEXITCODE -ne 0){throw 'Falló la verificación Blender/Python/FBX. Ver el registro local; no cambiar versión sin diagnóstico.'}
$report=Get-Content -Raw -LiteralPath (Join-Path $results 'verified.json') | ConvertFrom-Json
if($report.version -ne '5.2.2 LTS' -or !$report.background -or !$report.fbx){throw 'El informe de Blender no cumple el entorno validado de B0.'}
Write-Output "B0 verify: Blender $($report.version), Python $($report.python), exportador FBX disponible. Informe: $results"
