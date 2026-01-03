[CmdletBinding()]
param(
    [switch]$Once
)

$ErrorActionPreference = "Stop"

$bootstrap = Join-Path $PSScriptRoot "tri_ps_bootstrap.ps1"
$ingest = Join-Path $PSScriptRoot "tri_ps_ingest.ps1"
if (-not (Test-Path $bootstrap)) {
    throw "tri_ps_bootstrap.ps1 not found: $bootstrap"
}
if (-not (Test-Path $ingest)) {
    throw "tri_ps_ingest.ps1 not found: $ingest"
}

if ($Once) {
    & $bootstrap -Once
    $bootstrapExit = $LASTEXITCODE
    & $ingest -Once
    $ingestExit = $LASTEXITCODE
    if ($bootstrapExit -ne 0) { exit $bootstrapExit }
    exit $ingestExit
}

$shellExe = if ($PSVersionTable.PSEdition -eq "Core") { "pwsh.exe" } else { "powershell.exe" }
$args = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $bootstrap)
Start-Process -FilePath $shellExe -ArgumentList $args -WindowStyle Hidden | Out-Null
$ingestArgs = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $ingest)
Start-Process -FilePath $shellExe -ArgumentList $ingestArgs -WindowStyle Hidden | Out-Null
