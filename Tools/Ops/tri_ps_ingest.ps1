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

if (-not $env:TRI_STATE_DIR) {
    throw "TRI_STATE_DIR must be set for ingest"
}

$triOpsPath = Join-Path $PSScriptRoot "tri_ops.py"
if (-not (Test-Path $triOpsPath)) {
    throw "tri_ops not found: $triOpsPath"
}

$pythonCmd = Get-PythonCommand
$pollSeconds = if ($env:TRI_INGEST_POLL_SECONDS) { [int]$env:TRI_INGEST_POLL_SECONDS } else { 30 }
$leaseSeconds = if ($env:TRI_OPS_LEASE_SECONDS) { [int]$env:TRI_OPS_LEASE_SECONDS } else { 900 }

function Invoke-TriOps {
    param([string[]]$TriOpsArgs)
    $output = & $pythonCmd.Exe @($pythonCmd.Args + @($triOpsPath) + $TriOpsArgs) 2>&1 | Out-String
    return [pscustomobject]@{
        ExitCode = $LASTEXITCODE
        Output = $output.Trim()
    }
}

function Write-Result([string]$RequestId, [string]$Status, [string]$PublishedPath, [string]$BuildCommit, [string[]]$Logs, [string]$Error) {
    $args = @(
        "write_result", "--id", $RequestId, "--status", $Status,
        "--published-build-path", $PublishedPath, "--build-commit", $BuildCommit
    )
    foreach ($entry in $Logs) {
        $args += @("--log", $entry)
    }
    if ($Error) {
        $args += @("--error", $Error)
    }
    Invoke-TriOps $args | Out-Null
}

function Get-ProjectInfo([string]$Project) {
    switch ($Project.ToLowerInvariant()) {
        "space4x" { return @{ Name = "space4x"; Exe = "Space4X_Headless.x86_64" } }
        "godgame" { return @{ Name = "godgame"; Exe = "Godgame_Headless.x86_64" } }
        default { return $null }
    }
}

function Publish-Project([string]$InboxRoot, [string]$Project, [string]$BuildCommit, [string]$RequestId) {
    $info = Get-ProjectInfo $Project
    if (-not $info) {
        throw "unknown project: $Project"
    }
    $stamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $buildId = "${stamp}_${BuildCommit}"
    $sourceDir = Join-Path $InboxRoot $Project
    if (-not (Test-Path $sourceDir)) {
        throw "missing inbox project dir: $sourceDir"
    }
    $publishRoot = Join-Path $env:TRI_STATE_DIR ("builds\" + $info.Name)
    $publishDir = Join-Path $publishRoot $buildId
    New-Item -ItemType Directory -Path $publishDir -Force | Out-Null
    Copy-Item -Path (Join-Path $sourceDir "*") -Destination $publishDir -Recurse -Force
    $exePath = Join-Path $publishDir $info.Exe
    if (-not (Test-Path $exePath)) {
        throw "executable missing after publish: $exePath"
    }

    Invoke-TriOps @(
        "write_current", "--project", $info.Name,
        "--path", ([System.IO.Path]::GetFullPath($publishDir)),
        "--executable", ([System.IO.Path]::GetFullPath($exePath)),
        "--build-commit", $BuildCommit,
        "--build-id", $buildId,
        "--request-id", $RequestId
    ) | Out-Null

    return @{
        PublishDir = $publishDir
        Executable = $exePath
        BuildId = $buildId
    }
}

function Process-Ready([string]$ReadyPath) {
    $ready = Get-Content $ReadyPath -Raw | ConvertFrom-Json
    if (-not $ready) {
        throw "READY.json invalid: $ReadyPath"
    }
    $requestId = $ready.request_id
    if (-not $requestId) {
        throw "READY.json missing request_id: $ReadyPath"
    }
    $projects = @()
    if ($ready.projects) { $projects = @($ready.projects) }
    if (-not $projects -or $projects.Count -eq 0) {
        throw "READY.json missing projects: $ReadyPath"
    }
    $buildCommit = if ($ready.build_commit) { $ready.build_commit } else { "unknown" }

    $inboxRoot = Split-Path -Parent $ReadyPath
    $folderName = [System.IO.Path]::GetFileName($inboxRoot)
    if ($folderName -ne $requestId) {
        $inboxRoot = Join-Path $inboxRoot $requestId
    }
    if (-not (Test-Path $inboxRoot)) {
        throw "inbox folder missing for request: $requestId"
    }

    $lockResult = Invoke-TriOps @("lock_build", "--owner", "ps-ingest", "--request-id", $requestId, "--lease-seconds", $leaseSeconds)
    if ($lockResult.ExitCode -ne 0) {
        throw "unable to acquire build.lock for ingest"
    }

    $logs = New-Object System.Collections.Generic.List[string]
    $status = "ok"
    $publishedPath = ""
    $errorMessage = ""

    try {
        foreach ($project in $projects) {
            $publish = Publish-Project $inboxRoot $project $buildCommit $requestId
            $logs.Add("publish_path_" + $project + "=" + $publish.PublishDir)
            if (-not $publishedPath) { $publishedPath = $publish.PublishDir }
        }
    } catch {
        $status = "failed"
        $errorMessage = $_.Exception.Message
    } finally {
        if (-not $publishedPath) { $publishedPath = "n/a" }
        Write-Result $requestId $status $publishedPath $buildCommit $logs $errorMessage

        Invoke-TriOps @("unlock_build", "--owner", "ps-ingest", "--request-id", $requestId) | Out-Null
    }

    if ($status -eq "ok") {
        $archiveRoot = Join-Path $env:TRI_STATE_DIR "builds\inbox_archive"
        New-Item -ItemType Directory -Path $archiveRoot -Force | Out-Null
        $stamp = Get-Date -Format "yyyyMMdd_HHmmss"
        $archiveDir = Join-Path $archiveRoot ("{0}_{1}" -f $requestId, $stamp)
        Move-Item -Path $inboxRoot -Destination $archiveDir -Force
    }
}

while ($true) {
    $inboxRoot = Join-Path $env:TRI_STATE_DIR "builds\inbox"
    New-Item -ItemType Directory -Path $inboxRoot -Force | Out-Null
    $readyFiles = Get-ChildItem -Path $inboxRoot -Filter "READY.json" -File -Recurse -ErrorAction SilentlyContinue
    foreach ($ready in $readyFiles) {
        try {
            Process-Ready $ready.FullName
        } catch {
            Write-Result "unknown" "failed" "n/a" "unknown" @() $_.Exception.Message
        }
    }

    if ($Once) { break }
    Start-Sleep -Seconds $pollSeconds
}
