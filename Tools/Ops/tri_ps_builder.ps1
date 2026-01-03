[CmdletBinding()]
param(
    [switch]$Once
)

$ErrorActionPreference = "Stop"

function Get-PythonCommand {
    $cmd = Get-Command python -ErrorAction SilentlyContinue
    if ($cmd) { return @{ Exe = $cmd.Source; Args = @() } }
    $cmd = Get-Command python3 -ErrorAction SilentlyContinue
    if ($cmd) { return @{ Exe = $cmd.Source; Args = @() } }
    $cmd = Get-Command py -ErrorAction SilentlyContinue
    if ($cmd) { return @{ Exe = $cmd.Source; Args = @("-3") } }
    throw "python not found"
}

function Test-WorkspaceRoot([string]$Root) {
    if ([string]::IsNullOrWhiteSpace($Root)) { return $false }
    $full = [System.IO.Path]::GetFullPath($Root)
    return (Test-Path (Join-Path $full "space4x")) -and `
        (Test-Path (Join-Path $full "godgame")) -and `
        (Test-Path (Join-Path $full "Tools"))
}

function Resolve-Root {
    if (-not $env:TRI_ROOT) {
        throw "TRI_ROOT must be set to the workspace containing: space4x, godgame, Tools"
    }
    $env:TRI_ROOT = [System.IO.Path]::GetFullPath($env:TRI_ROOT)
    if (-not (Test-WorkspaceRoot $env:TRI_ROOT)) {
        throw "TRI_ROOT must contain: space4x, godgame, Tools"
    }
}

function Resolve-StateDir {
    if (-not $env:TRI_STATE_DIR) {
        $distro = if ($env:TRI_WSL_DISTRO) { $env:TRI_WSL_DISTRO } else { "Ubuntu" }
        $env:TRI_STATE_DIR = "\\wsl$\$distro\home\oni\Tri\.tri\state"
    }
}

$stateDirWin = $null
$stateDirWsl = $null
function Resolve-StateDirWsl {
    $script:stateDirWin = $env:TRI_STATE_DIR
    if ($env:TRI_STATE_DIR_WSL) {
        $script:stateDirWsl = $env:TRI_STATE_DIR_WSL
        return
    }
    if ($script:stateDirWin -match "^\\\\\\\\wsl(\\.localhost)?\\\\[^\\\\]+\\\\") {
        $script:stateDirWsl = $script:stateDirWin -replace "^\\\\\\\\wsl(\\.localhost)?\\\\[^\\\\]+\\\\", ""
        $script:stateDirWsl = "/" + ($script:stateDirWsl -replace "\\\\", "/")
    }
}

function Get-ShellExe {
    if ($PSVersionTable.PSEdition -eq "Core") {
        return (Join-Path $PSHOME "pwsh.exe")
    }
    return (Join-Path $PSHOME "powershell.exe")
}

function Archive-File([string]$SourcePath, [string]$ArchiveDir) {
    if (-not (Test-Path $SourcePath)) {
        return
    }
    New-Item -ItemType Directory -Path $ArchiveDir -Force | Out-Null
    $base = [System.IO.Path]::GetFileNameWithoutExtension($SourcePath)
    $ext = [System.IO.Path]::GetExtension($SourcePath)
    $stamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $dest = Join-Path $ArchiveDir ("{0}_{1}{2}" -f $base, $stamp, $ext)
    try {
        Move-Item -Path $SourcePath -Destination $dest -Force -ErrorAction Stop
    } catch {
        Remove-Item -Path $SourcePath -Force -ErrorAction SilentlyContinue
    }
}

function Archive-RequestFiles([string]$RequestId) {
    $opsDir = Join-Path $env:TRI_STATE_DIR "ops"
    $requestsDir = Join-Path $opsDir "requests"
    $claimsDir = Join-Path $opsDir "claims"
    $archiveReqDir = Join-Path $opsDir "archive\requests"
    $archiveClaimDir = Join-Path $opsDir "archive\claims"
    $requestPath = Join-Path $requestsDir ("{0}.json" -f $RequestId)
    $claimPath = Join-Path $claimsDir ("{0}.json" -f $RequestId)
    Archive-File $requestPath $archiveReqDir
    Archive-File $claimPath $archiveClaimDir
}

Resolve-Root
Resolve-StateDir
Resolve-StateDirWsl

$triOpsPath = Join-Path $PSScriptRoot "tri_ops.py"
if (-not (Test-Path $triOpsPath)) {
    throw "tri_ops not found: $triOpsPath"
}

$builderMode = if ($env:TRI_BUILDER_MODE) { $env:TRI_BUILDER_MODE } else { "orchestrator" }
$builderMode = $builderMode.ToLowerInvariant()
if ($builderMode -notin @("build", "orchestrator")) {
    throw "TRI_BUILDER_MODE must be 'build' or 'orchestrator'"
}

$pythonCmd = Get-PythonCommand
$shellExe = Get-ShellExe
$heartbeatSeconds = if ($env:TRI_OPS_HEARTBEAT_SECONDS) { [int]$env:TRI_OPS_HEARTBEAT_SECONDS } else { 45 }
$pollSeconds = if ($env:TRI_OPS_POLL_SECONDS) { [int]$env:TRI_OPS_POLL_SECONDS } else { 30 }
$leaseSeconds = if ($env:TRI_OPS_LEASE_SECONDS) { [int]$env:TRI_OPS_LEASE_SECONDS } else { 900 }

function Invoke-TriOps {
    param([string[]]$TriOpsArgs)
    $output = & $pythonCmd.Exe @($pythonCmd.Args + @($triOpsPath) + $TriOpsArgs) 2>&1 | Out-String
    return [pscustomobject]@{
        ExitCode = $LASTEXITCODE
        Output = $output.Trim()
    }
}

function Write-Heartbeat([string]$Phase, [string]$Task, [int]$Cycle) {
    Invoke-TriOps @(
        "heartbeat", "--agent", "ps", "--phase", $Phase,
        "--current-task", $Task, "--cycle", $Cycle
    ) | Out-Null
}

function Renew-Leases([string]$RequestId) {
    Invoke-TriOps @(
        "renew_lock", "--owner", "ps", "--request-id", $RequestId,
        "--lease-seconds", $leaseSeconds
    ) | Out-Null
    Invoke-TriOps @(
        "renew_claim", "--id", $RequestId, "--agent", "ps",
        "--lease-seconds", $leaseSeconds
    ) | Out-Null
}

function Get-Projects($reqObj) {
    if ($reqObj -and $reqObj.projects) {
        return @($reqObj.projects)
    }
    return @("space4x", "godgame")
}

function Get-GitCommit([string]$ProjectRoot) {
    try {
        $output = & git -C $ProjectRoot rev-parse --short=10 HEAD 2>$null
        if ($LASTEXITCODE -eq 0) {
            return $output.Trim()
        }
    } catch {
    }
    return "unknown"
}

function Get-ProjectInfo([string]$Project) {
    switch ($Project.ToLowerInvariant()) {
        "space4x" {
            return @{
                Name = "space4x"
                BuildRootName = "Space4X_headless"
                ExecutableName = "Space4X_Headless.x86_64"
                BuildScript = (Join-Path $env:TRI_ROOT "Tools\build_space4x_windows.ps1")
            }
        }
        "godgame" {
            return @{
                Name = "godgame"
                BuildRootName = "Godgame_headless"
                ExecutableName = "Godgame_Headless.x86_64"
                BuildScript = (Join-Path $env:TRI_ROOT "Tools\build_godgame_windows.ps1")
            }
        }
        default { return $null }
    }
}

function Get-LatestBuildPath([string]$ProjectRoot, [string]$BuildRootName, [string]$ExeName) {
    $buildRoot = Join-Path $ProjectRoot ("Builds\" + $BuildRootName)
    if (-not (Test-Path $buildRoot)) {
        throw "Build root missing: $buildRoot"
    }
    $latest = Get-ChildItem -Path $buildRoot -Directory -Filter "Linux_*" -ErrorAction SilentlyContinue |
        Sort-Object Name -Descending |
        Select-Object -First 1
    $buildDir = if ($latest) { $latest.FullName } else { Join-Path $buildRoot "Linux" }
    $exePath = Join-Path $buildDir $ExeName
    if (-not (Test-Path $exePath)) {
        throw "Headless binary not found: $exePath"
    }
    return $buildDir
}

function Invoke-BuildScript([string]$ScriptPath, [string]$LogPath, [string]$Phase, [string]$RequestId, [int]$Cycle) {
    $unityExe = if ($env:TRI_UNITY_EXE) { $env:TRI_UNITY_EXE } else { $env:UNITY_WIN }
    $args = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $ScriptPath, "-TriRoot", $env:TRI_ROOT, "-LogPath", $LogPath)
    if ($unityExe) {
        $args += @("-UnityExe", $unityExe)
    }
    $proc = Start-Process -FilePath $shellExe -ArgumentList $args -PassThru
    while (-not $proc.HasExited) {
        Write-Heartbeat $Phase "req=$RequestId" $Cycle
        Renew-Leases $RequestId
        Start-Sleep -Seconds $heartbeatSeconds
    }
    return $proc.ExitCode
}

function Publish-Build([string]$Project, [string]$BuildDir, [string]$ExeName, [string]$RequestId) {
    $commit = Get-GitCommit (Join-Path $env:TRI_ROOT $Project)
    $stamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $buildId = "${stamp}_${commit}"
    $publishRoot = Join-Path $env:TRI_STATE_DIR ("builds\" + $Project)
    $publishDir = Join-Path $publishRoot $buildId

    New-Item -ItemType Directory -Path $publishDir -Force | Out-Null
    Copy-Item -Path (Join-Path $BuildDir "*") -Destination $publishDir -Recurse -Force

    $exePath = Join-Path $publishDir $ExeName
    $fullPublishDir = [System.IO.Path]::GetFullPath($publishDir)
    $fullExePath = [System.IO.Path]::GetFullPath($exePath)
    $publishDirForOps = $fullPublishDir
    $exePathForOps = $fullExePath
    if ($stateDirWsl) {
        $prefix = $stateDirWin.TrimEnd("\")
        if ($fullPublishDir.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            $suffix = $fullPublishDir.Substring($prefix.Length).TrimStart("\")
            $suffix = $suffix.Replace("\", "/")
            $publishDirForOps = $stateDirWsl.TrimEnd("/") + "/" + $suffix
        }
        if ($fullExePath.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            $suffix = $fullExePath.Substring($prefix.Length).TrimStart("\")
            $suffix = $suffix.Replace("\", "/")
            $exePathForOps = $stateDirWsl.TrimEnd("/") + "/" + $suffix
        }
    }

    Invoke-TriOps @(
        "write_current", "--project", $Project, "--path", $publishDirForOps,
        "--executable", $exePathForOps, "--build-commit", $commit,
        "--build-id", $buildId, "--request-id", $RequestId
    ) | Out-Null

    return @{
        BuildCommit = $commit
        PublishedPath = $publishDirForOps
        ExecutablePath = $exePathForOps
        BuildId = $buildId
    }
}

Invoke-TriOps @("init") | Out-Null

$cycle = 0
while ($true) {
    $cycle++
    Write-Heartbeat "watching" "polling" $cycle

    $claimResult = Invoke-TriOps @("claim_next", "--agent", "ps", "--lease-seconds", $leaseSeconds, "--json")
    if ($claimResult.ExitCode -ne 0 -or [string]::IsNullOrWhiteSpace($claimResult.Output)) {
        Start-Sleep -Seconds $pollSeconds
        if ($Once) { break }
        continue
    }

    $claimObj = $claimResult.Output | ConvertFrom-Json
    $reqObj = $claimObj.request
    $requestId = if ($reqObj.id) { $reqObj.id } else { $claimObj.id }
    if (-not $requestId) {
        Start-Sleep -Seconds $pollSeconds
        continue
    }

    $projects = Get-Projects $reqObj

    while ($true) {
        $lockResult = Invoke-TriOps @("lock_build", "--owner", "ps", "--request-id", $requestId, "--lease-seconds", $leaseSeconds)
        if ($lockResult.ExitCode -eq 0) { break }
        Write-Heartbeat "waiting_lock" "req=$requestId" $cycle
        Invoke-TriOps @("renew_claim", "--id", $requestId, "--agent", "ps", "--lease-seconds", $leaseSeconds) | Out-Null
        Start-Sleep -Seconds $pollSeconds
    }

    Write-Heartbeat "locked" "req=$requestId" $cycle

    $overallStatus = "ok"
    $publishedPath = ""
    $buildCommit = ""
    $logs = New-Object System.Collections.Generic.List[string]
    $errorMessage = ""

    try {
        if ($builderMode -eq "orchestrator") {
            Write-Heartbeat "queued_external" "req=$requestId" $cycle
            $overallStatus = "queued_external"
            $publishedPath = "n/a"
            $buildCommit = "unknown"
            $logs.Add("mode=orchestrator")
        } else {
            foreach ($project in $projects) {
                $info = Get-ProjectInfo $project
                if (-not $info) {
                    $overallStatus = "failed"
                    $errorMessage = "unknown project: $project"
                    continue
                }

                $projectRoot = Join-Path $env:TRI_ROOT $info.Name
                $buildLogDir = Join-Path $env:TRI_STATE_DIR "builds\logs"
                New-Item -ItemType Directory -Path $buildLogDir -Force | Out-Null
                $logPath = Join-Path $buildLogDir ("{0}_{1}.log" -f $info.Name, (Get-Date -Format "yyyyMMdd_HHmmss"))

                $exitCode = Invoke-BuildScript $info.BuildScript $logPath ("building_" + $info.Name) $requestId $cycle
                $logs.Add("build_log=" + $logPath)

                if ($exitCode -ne 0) {
                    $overallStatus = "failed"
                    $errorMessage = "build failed for $project (exit $exitCode)"
                    continue
                }

                Write-Heartbeat ("publishing_" + $info.Name) "req=$requestId" $cycle
                Renew-Leases $requestId

                $buildDir = Get-LatestBuildPath $projectRoot $info.BuildRootName $info.ExecutableName
                $publish = Publish-Build $info.Name $buildDir $info.ExecutableName $requestId
                $logs.Add("publish_path=" + $publish.PublishedPath)
                $publishedPath = $publish.PublishedPath
                $buildCommit = $publish.BuildCommit
            }
        }
    } catch {
        $overallStatus = "failed"
        $errorMessage = $_.Exception.Message
    } finally {
        if (-not $publishedPath) { $publishedPath = "n/a" }
        if (-not $buildCommit) { $buildCommit = "unknown" }

        $resultArgs = @(
            "write_result", "--id", $requestId, "--status", $overallStatus,
            "--published-build-path", $publishedPath, "--build-commit", $buildCommit
        )
        foreach ($entry in $logs) {
            $resultArgs += @("--log", $entry)
        }
        if ($errorMessage) {
            $resultArgs += @("--error", $errorMessage)
        }
        Invoke-TriOps $resultArgs | Out-Null

        Invoke-TriOps @("unlock_build", "--owner", "ps", "--request-id", $requestId) | Out-Null
        Write-Heartbeat "watching" "unlocked" $cycle
        Archive-RequestFiles $requestId
    }

    if ($Once) { break }
    Start-Sleep -Seconds 5
}
