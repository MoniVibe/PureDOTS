[CmdletBinding()]
param(
    [switch]$Once
)

$ErrorActionPreference = "Stop"

$logDir = $null
if (-not $env:TRI_STATE_DIR) {
    throw "TRI_STATE_DIR must be set for startup logging"
}
$logDir = Join-Path $env:TRI_STATE_DIR "ops"
New-Item -ItemType Directory -Path $logDir -Force | Out-Null
$stamp = Get-Date -Format "yyyyMMdd_HHmmss"
$bootstrapLog = Join-Path $logDir ("ps_bootstrap_{0}.log" -f $stamp)
$ingestLog = Join-Path $logDir ("ps_ingest_{0}.log" -f $stamp)

$bootstrap = Join-Path $PSScriptRoot "tri_ps_bootstrap.ps1"
$ingest = Join-Path $PSScriptRoot "tri_ps_ingest.ps1"
if (-not (Test-Path $bootstrap)) {
    throw "tri_ps_bootstrap.ps1 not found: $bootstrap"
}
if (-not (Test-Path $ingest)) {
    throw "tri_ps_ingest.ps1 not found: $ingest"
}

if ($Once) {
    & $bootstrap -Once *>> $bootstrapLog
    $bootstrapExit = $LASTEXITCODE
    & $ingest -Once *>> $ingestLog
    $ingestExit = $LASTEXITCODE
    if ($bootstrapExit -ne 0) { exit $bootstrapExit }
    exit $ingestExit
}

$shellExe = if ($PSVersionTable.PSEdition -eq "Core") { "pwsh.exe" } else { "powershell.exe" }
$args = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $bootstrap)
Start-Process -FilePath $shellExe -ArgumentList $args -WindowStyle Hidden `
    -RedirectStandardOutput $bootstrapLog -RedirectStandardError $bootstrapLog | Out-Null
$ingestArgs = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $ingest)
Start-Process -FilePath $shellExe -ArgumentList $ingestArgs -WindowStyle Hidden `
    -RedirectStandardOutput $ingestLog -RedirectStandardError $ingestLog | Out-Null
