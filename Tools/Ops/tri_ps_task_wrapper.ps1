[CmdletBinding()]
param(
    [string]$TriRoot = $env:TRI_ROOT,
    [string]$TriStateDir = $env:TRI_STATE_DIR,
    [string]$Distro = "Ubuntu",
    [string]$BuilderMode = "orchestrator"
)

$ErrorActionPreference = "Stop"

if (-not $TriRoot) {
    throw "TriRoot is required (pass -TriRoot or set TRI_ROOT)"
}
if (-not $TriStateDir) {
    throw "TriStateDir is required (pass -TriStateDir or set TRI_STATE_DIR)"
}

# REQUIRED: workspace root that contains space4x/, godgame/, Tools/
$env:TRI_ROOT = $TriRoot

# REQUIRED: shared state dir (WSL ext4 via \\wsl$)
$env:TRI_WSL_DISTRO = $Distro
$env:TRI_STATE_DIR = $TriStateDir

# Enforce Option B (no Unity builds on this machine)
$env:TRI_BUILDER_MODE = $BuilderMode

# Wake the distro so \\wsl$ is alive (important for scheduled tasks)
wsl.exe -d $env:TRI_WSL_DISTRO --exec bash -lc "true" | Out-Null

# Start the PS lane (this script already writes logs under TRI_STATE_DIR/ops/)
& (Join-Path $PSScriptRoot "tri_ps_startup.ps1")
