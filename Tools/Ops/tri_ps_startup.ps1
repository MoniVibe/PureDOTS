[CmdletBinding()]
param(
    [switch]$Once
)

$ErrorActionPreference = "Stop"

$bootstrap = Join-Path $PSScriptRoot "tri_ps_bootstrap.ps1"
if (-not (Test-Path $bootstrap)) {
    throw "tri_ps_bootstrap.ps1 not found: $bootstrap"
}

if ($Once) {
    & $bootstrap -Once
    exit $LASTEXITCODE
}

$shellExe = if ($PSVersionTable.PSEdition -eq "Core") { "pwsh.exe" } else { "powershell.exe" }
$args = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $bootstrap)
Start-Process -FilePath $shellExe -ArgumentList $args -WindowStyle Hidden | Out-Null
