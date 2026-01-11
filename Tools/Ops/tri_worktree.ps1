param(
    [Parameter(Mandatory = $true)]
    [string]$RepoPath,
    [Parameter(Mandatory = $true)]
    [string]$SessionId,
    [Parameter(Mandatory = $true)]
    [string]$AgentId,
    [Parameter(Mandatory = $true)]
    [string]$TaskId
)

$resolvedRepoPath = (Resolve-Path -Path $RepoPath).Path
$repoName = [System.IO.Path]::GetFileName($resolvedRepoPath.TrimEnd('\', '/'))
$wtRoot = "C:\polish\dev\worktrees"

$wtPath = Join-Path $wtRoot $repoName
$wtPath = Join-Path $wtPath $SessionId
$wtPath = Join-Path $wtPath $AgentId
$wtPath = Join-Path $wtPath $TaskId

$branch = "workblock/${SessionId}_${repoName}_${TaskId}_${AgentId}"

& git -C $resolvedRepoPath fetch -q origin
$base = (& git -C $resolvedRepoPath remote show origin | Select-String "HEAD branch").ToString().Split(":")[-1].Trim()

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $wtPath) | Out-Null
& git -C $resolvedRepoPath worktree add -q -b $branch $wtPath "origin/$base"

Write-Output $wtPath
Write-Output $branch
