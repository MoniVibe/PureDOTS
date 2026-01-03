[CmdletBinding()]
param(
    [switch]$Once
)

$ErrorActionPreference = "Stop"

function Test-WorkspaceRoot([string]$Root) {
    if ([string]::IsNullOrWhiteSpace($Root)) { return $false }
    $full = [System.IO.Path]::GetFullPath($Root)
    return (Test-Path (Join-Path $full "space4x")) -and `
        (Test-Path (Join-Path $full "godgame")) -and `
        (Test-Path (Join-Path $full "Tools"))
}

if (-not $env:TRI_ROOT) {
    throw "TRI_ROOT must be set to the workspace containing: space4x, godgame, Tools"
}
if (-not (Test-WorkspaceRoot $env:TRI_ROOT)) {
    throw "TRI_ROOT must contain: space4x, godgame, Tools"
}

if (-not $env:TRI_STATE_DIR) {
    $distro = if ($env:TRI_WSL_DISTRO) { $env:TRI_WSL_DISTRO } else { "Ubuntu" }
    $env:TRI_STATE_DIR = "\\wsl$\$distro\home\oni\Tri\.tri\state"
}

$builder = Join-Path $PSScriptRoot "tri_ps_builder.ps1"
if (-not (Test-Path $builder)) {
    throw "tri_ps_builder.ps1 not found: $builder"
}

& $builder -Once:$Once
