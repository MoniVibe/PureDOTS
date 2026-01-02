[CmdletBinding()]
param(
    [switch]$Once
)

$ErrorActionPreference = "Stop"

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\..\.."))
if (-not $env:TRI_ROOT) {
    $env:TRI_ROOT = $repoRoot
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
