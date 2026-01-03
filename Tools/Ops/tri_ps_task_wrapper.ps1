[CmdletBinding()]
param(
    [string]$TriRoot = $env:TRI_ROOT,
    [string]$TriStateDir = $env:TRI_STATE_DIR,
    [string]$TriStateDirWsl = $env:TRI_STATE_DIR_WSL,
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
if (-not $TriStateDirWsl) {
    $prefixes = @(
        "\\wsl$\$Distro\",
        "\\wsl.localhost\$Distro\"
    )
    foreach ($prefix in $prefixes) {
        if ($TriStateDir.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            $suffix = $TriStateDir.Substring($prefix.Length)
            $TriStateDirWsl = "/" + ($suffix -replace "\\", "/")
            break
        }
    }
}
if ($TriStateDirWsl) {
    $env:TRI_STATE_DIR_WSL = $TriStateDirWsl
}

# Enforce Option B (no Unity builds on this machine)
$env:TRI_BUILDER_MODE = $BuilderMode

# Wake the distro so \\wsl$ is alive (important for scheduled tasks)
wsl.exe -d $env:TRI_WSL_DISTRO --exec bash -lc "true" | Out-Null

$opsDir = Join-Path $env:TRI_STATE_DIR "ops"
New-Item -ItemType Directory -Path $opsDir -Force | Out-Null
$superLog = Join-Path $opsDir "ps_supervisor.log"

function Write-Supervisor([string]$Message) {
    $stamp = (Get-Date).ToUniversalTime().ToString("s") + "Z"
    "$stamp $Message" | Out-File -Encoding ascii -Append -FilePath $superLog
}

function Get-ShellExe {
    if ($PSVersionTable.PSEdition -eq "Core") {
        return (Join-Path $PSHOME "pwsh.exe")
    }
    return (Join-Path $PSHOME "powershell.exe")
}

function Start-Worker([string]$ScriptPath, [string]$LogPrefix) {
    $stamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $out = Join-Path $opsDir ("{0}_{1}.out.log" -f $LogPrefix, $stamp)
    $err = Join-Path $opsDir ("{0}_{1}.err.log" -f $LogPrefix, $stamp)
    $args = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $ScriptPath)
    Start-Process -FilePath (Get-ShellExe) -ArgumentList $args -WindowStyle Hidden `
        -RedirectStandardOutput $out -RedirectStandardError $err | Out-Null
}

function Is-Running([string]$ScriptName) {
    $pattern = [Regex]::Escape($ScriptName)
    $procs = Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -match $pattern }
    return ($procs.Count -gt 0)
}

$bootstrapPath = Join-Path $PSScriptRoot "tri_ps_bootstrap.ps1"
$ingestPath = Join-Path $PSScriptRoot "tri_ps_ingest.ps1"

Write-Supervisor "supervisor_start"

$delaySeconds = 30
$cooldownSeconds = 60
$lastStart = @{
    bootstrap = [DateTime]::MinValue
    ingest = [DateTime]::MinValue
}

function Can-Restart([string]$Key) {
    return ((Get-Date) - $lastStart[$Key]).TotalSeconds -ge $cooldownSeconds
}

while ($true) {
    $bootRunning = Is-Running "tri_ps_bootstrap.ps1"
    $ingestRunning = Is-Running "tri_ps_ingest.ps1"

    if (-not $bootRunning -and (Can-Restart "bootstrap")) {
        Start-Worker $bootstrapPath "ps_bootstrap"
        $lastStart["bootstrap"] = Get-Date
        Write-Supervisor "restart bootstrap"
    }
    if (-not $ingestRunning -and (Can-Restart "ingest")) {
        Start-Worker $ingestPath "ps_ingest"
        $lastStart["ingest"] = Get-Date
        Write-Supervisor "restart ingest"
    }

    Start-Sleep -Seconds $delaySeconds
}
