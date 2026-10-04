# Verifica la base aprobada sin tocar el worktree activo ni modificar guardas/esperados.
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$base='0505b1690656d15188860157612455639820fe1f'
$snapshot=Join-Path $repo ('qa-results\reference-'+[guid]::NewGuid().ToString('N'))
git -C $repo worktree add --detach $snapshot $base
if($LASTEXITCODE -ne 0){throw 'No se pudo crear la instantánea aislada.'}
New-Item -ItemType Junction -Path (Join-Path $snapshot 'node_modules') -Target (Join-Path $repo 'node_modules') | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $snapshot 'unity\Docs\Reference') | Out-Null
Get-ChildItem -LiteralPath $PSScriptRoot -Filter 'unity-reference*.mjs' | Copy-Item -Destination (Join-Path $snapshot 'scripts')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'u4-reference.mjs') -Destination (Join-Path $snapshot 'scripts')
Get-ChildItem -LiteralPath (Join-Path $repo 'unity\Docs\Reference') -File | Copy-Item -Destination (Join-Path $snapshot 'unity\Docs\Reference')
Write-Output "Instantánea conservada para revisión: $snapshot"
Push-Location $snapshot
try {
    foreach($script in @('unity-reference.mjs','unity-reference-u2.mjs','unity-reference-u3.mjs','u4-reference.mjs')) {
        node (Join-Path 'scripts' $script) 2>&1 | Tee-Object -FilePath (Join-Path $snapshot ($script+'.log'))
        if($LASTEXITCODE -ne 0){throw "Falló $script"}
    }
} finally {Pop-Location}
# No borrar la instantánea ni su junction automáticamente.
