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
    if ($script:stateDirWin -match '^\\\\\\\\wsl(\\.localhost)?\\$?\\\\[^\\\\]+\\\\') {
        $script:stateDirWsl = $script:stateDirWin -replace '^\\\\\\\\wsl(\\.localhost)?\\$?\\\\[^\\\\]+\\\\', ""
        $script:stateDirWsl = "/" + ($script:stateDirWsl -replace "\\\\", "/")
    }
}

function Get-ShellExe {
    if ($PSVersionTable.PSEdition -eq "Core") {
        return (Join-Path $PSHOME "pwsh.exe")
    }
    return (Join-Path $PSHOME "powershell.exe")
}

function Invoke-Git {
    param(
        [string]$ProjectRoot,
        [Parameter(ValueFromRemainingArguments = $true)]
        [string[]]$Args
    )
    $prev = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $output = & git -C $ProjectRoot @Args 2>&1 | Out-String
        $exitCode = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $prev
    }
    return @{
        ExitCode = $exitCode
        Output = $output.Trim()
    }
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

function Get-NotesRaw($reqObj) {
    if (-not $reqObj -or -not $reqObj.notes) {
        return ""
    }
    if ($reqObj.notes -is [System.Array]) {
        return ($reqObj.notes -join ";")
    }
    return [string]$reqObj.notes
}

function Get-NoteValue([string]$NotesRaw, [string]$Key) {
    if ([string]::IsNullOrWhiteSpace($NotesRaw)) {
        return $null
    }
    $pattern = [regex]::Escape($Key) + "=([^;]+)"
    $match = [regex]::Match($NotesRaw, $pattern)
    if (-not $match.Success) {
        return $null
    }
    $value = $match.Groups[1].Value
    $value = $value.Trim().Trim('"').Trim("'")
    $value = ($value -replace '[\s\u00AD]+', '')
    if ([string]::IsNullOrWhiteSpace($value)) {
        return $null
    }
    return $value
}

function Normalize-Path([string]$Path) {
    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $Path
    }
    return [System.IO.Path]::GetFullPath($Path)
}

function Set-LogValue([System.Collections.Generic.List[string]]$Logs, [string]$Key, [string]$Value) {
    for ($i = $Logs.Count - 1; $i -ge 0; $i--) {
        if ($Logs[$i] -like "$Key=*") {
            $Logs.RemoveAt($i)
        }
    }
    $Logs.Add("$Key=$Value")
}

function Get-UnityProjectPathFromLog([string]$LogPath) {
    if (-not (Test-Path $LogPath)) {
        return $null
    }
    $line = Select-String -Path $LogPath -Pattern '-projectPath\s+"?([^"]+)"?' -ErrorAction SilentlyContinue | Select-Object -Last 1
    if ($line -and $line.Matches.Count -gt 0) {
        return $line.Matches[0].Groups[1].Value.Trim()
    }
    $line = Select-String -Path $LogPath -Pattern 'Using project path\s*[:=]\s*(.+)$' -ErrorAction SilentlyContinue | Select-Object -Last 1
    if ($line -and $line.Matches.Count -gt 0) {
        return $line.Matches[0].Groups[1].Value.Trim()
    }
    return $null
}

function Get-ManifestPureDots([string]$ProjectPath) {
    $manifestPath = Join-Path $ProjectPath "Packages\manifest.json"
    if (-not (Test-Path $manifestPath)) {
        return @{ Value = ""; Path = $manifestPath; Error = "manifest_missing" }
    }
    try {
        $manifest = Get-Content -Path $manifestPath -Raw | ConvertFrom-Json
        $value = $manifest.dependencies."com.moni.puredots"
        return @{ Value = $value; Path = $manifestPath; Error = "" }
    } catch {
        return @{ Value = ""; Path = $manifestPath; Error = $_.Exception.Message }
    }
}

function Get-PackagesLockPureDots([string]$ProjectPath) {
    $lockPath = Join-Path $ProjectPath "Packages\packages-lock.json"
    if (-not (Test-Path $lockPath)) {
        return @{ Version = ""; Source = ""; Path = $lockPath; Error = "lock_missing" }
    }
    try {
        $lock = Get-Content -Path $lockPath -Raw | ConvertFrom-Json
        $dep = $null
        if ($lock -and $lock.dependencies) {
            $dep = $lock.dependencies."com.moni.puredots"
        }
        $version = if ($dep -and $dep.version) { $dep.version } else { "" }
        $source = if ($dep -and $dep.source) { $dep.source } else { "" }
        return @{ Version = $version; Source = $source; Path = $lockPath; Error = "" }
    } catch {
        return @{ Version = ""; Source = ""; Path = $lockPath; Error = $_.Exception.Message }
    }
}

function Resolve-PureDotsPath([string]$ProjectPath, [string]$ManifestValue) {
    if ([string]::IsNullOrWhiteSpace($ManifestValue)) {
        return $null
    }
    if ($ManifestValue -notmatch "^file:") {
        return $null
    }
    $rel = $ManifestValue -replace "^file:(//)?", ""
    $rel = $rel.TrimStart("/")
    $rel = $rel.Replace("/", "\")
    if ([System.IO.Path]::IsPathRooted($rel)) {
        return Normalize-Path $rel
    }
    return Normalize-Path (Join-Path $ProjectPath $rel)
}

function Get-SourceSentinelInfo([string]$ResolvedPath, [string[]]$Sentinels) {
    $info = @{ Present = $false; Hash = ""; Path = "" }
    if ([string]::IsNullOrWhiteSpace($ResolvedPath)) {
        return $info
    }
    $path = Join-Path $ResolvedPath "Runtime\Systems\Telemetry\TelemetryExportSystem.cs"
    $info.Path = $path
    if (-not (Test-Path $path)) {
        return $info
    }
    $present = $false
    foreach ($sentinel in $Sentinels) {
        if (Select-String -Path $path -Pattern $sentinel -SimpleMatch -Quiet) {
            $present = $true
            break
        }
    }
    $info.Present = $present
    $info.Hash = (Get-FileHash -Path $path -Algorithm SHA256).Hash
    return $info
}

function Test-ByteSequence([byte[]]$Data, [byte[]]$Pattern) {
    if (-not $Data -or -not $Pattern -or $Pattern.Length -eq 0 -or $Data.Length -lt $Pattern.Length) {
        return $false
    }
    for ($i = 0; $i -le $Data.Length - $Pattern.Length; $i++) {
        $match = $true
        for ($j = 0; $j -lt $Pattern.Length; $j++) {
            if ($Data[$i + $j] -ne $Pattern[$j]) {
                $match = $false
                break
            }
        }
        if ($match) {
            return $true
        }
    }
    return $false
}

function Test-BinaryContains([string]$Path, [string]$Text) {
    if (-not (Test-Path $Path)) {
        return $false
    }
    try {
        $bytes = [System.IO.File]::ReadAllBytes($Path)
    } catch {
        return $false
    }
    $ascii = [System.Text.Encoding]::ASCII.GetBytes($Text)
    if (Test-ByteSequence $bytes $ascii) {
        return $true
    }
    $utf16 = [System.Text.Encoding]::Unicode.GetBytes($Text)
    return (Test-ByteSequence $bytes $utf16)
}

function Trim-LogValue([string]$Value, [int]$MaxLen = 1500) {
    if ($Value -eq $null) {
        return ""
    }
    if ($Value.Length -le $MaxLen) {
        return $Value
    }
    return $Value.Substring(0, $MaxLen)
}

function Format-LogSnippet([string[]]$Lines, [int]$MaxLen = 1500) {
    if (-not $Lines) {
        return ""
    }
    $clean = $Lines | ForEach-Object {
        ($_ -replace "[\r\n]+", " ").Trim()
    } | Where-Object { $_ }
    $joined = ($clean -join " | ")
    return (Trim-LogValue $joined $MaxLen)
}

function Get-UnityLogDiagnostics([string]$LogPath) {
    if (-not (Test-Path $LogPath)) {
        return @{ Tail = ""; Errors = "" }
    }
    $tailLines = Get-Content -Path $LogPath -Tail 30 -ErrorAction SilentlyContinue
    $errorLines = Select-String -Path $LogPath -Pattern "(?i)(error|exception|aborting batchmode)" -ErrorAction SilentlyContinue |
        Select-Object -First 10 | ForEach-Object { $_.Line }
    return @{
        Tail = (Format-LogSnippet $tailLines)
        Errors = (Format-LogSnippet $errorLines)
    }
}

function Get-UnityLicensingLines([string]$LogPath) {
    if (-not (Test-Path $LogPath)) {
        return @()
    }
    $lines = Select-String -Path $LogPath -Pattern "\[Licensing::Module\]\s*Error:|Access token is unavailable" -ErrorAction SilentlyContinue |
        Select-Object -First 10 | ForEach-Object { $_.Line }
    return $lines
}

function Get-DllCandidates([string]$ProjectRoot, [int]$Minutes = 60, [int]$Max = 10) {
    $roots = @(
        (Join-Path $ProjectRoot "Library"),
        (Join-Path $ProjectRoot "Temp"),
        (Join-Path $ProjectRoot "Builds")
    )
    $cutoff = (Get-Date).AddMinutes(-$Minutes)
    $candidates = New-Object System.Collections.Generic.List[object]
    foreach ($root in $roots) {
        if (-not (Test-Path $root)) {
            continue
        }
        $dlls = Get-ChildItem -Path $root -Recurse -Filter "*.dll" -File -ErrorAction SilentlyContinue
        foreach ($dll in $dlls) {
            if ($dll.Name -like "PureDOTS*.dll" -or $dll.LastWriteTime -ge $cutoff) {
                $candidates.Add($dll)
            }
        }
    }
    $unique = $candidates | Sort-Object FullName -Unique
    $ordered = $unique | Sort-Object LastWriteTime -Descending | Select-Object -First $Max
    $entries = $ordered | ForEach-Object {
        "{0}@{1}" -f $_.FullName, $_.LastWriteTime.ToString("s")
    }
    return (Trim-LogValue ($entries -join ","))
}

function Add-CompileDiagnostics(
    [string]$ProjectRoot,
    [string]$LogPath,
    [int]$ExitCode,
    [string]$CmdProjectPath,
    [System.Collections.Generic.List[string]]$Logs
) {
    Set-LogValue $Logs "unity_exit_code" $ExitCode
    Set-LogValue $Logs "unity_log_path" $LogPath
    Set-LogValue $Logs "unity_cmdline_projectPath" $CmdProjectPath

    $diag = Get-UnityLogDiagnostics $LogPath
    Set-LogValue $Logs "unity_log_tail" $diag.Tail
    Set-LogValue $Logs "unity_error_lines" $diag.Errors

    $libraryPath = Join-Path $ProjectRoot "Library"
    $libraryExists = Test-Path $libraryPath
    Set-LogValue $Logs "library_exists" ([int]$libraryExists)
    $libraryDirsTop = ""
    if ($libraryExists) {
        $dirs = Get-ChildItem -Path $libraryPath -Directory -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Name
        $libraryDirsTop = Trim-LogValue ($dirs -join ",")
    }
    Set-LogValue $Logs "library_dirs_top" $libraryDirsTop

    $playerDirs = Get-ChildItem -Path $ProjectRoot -Directory -Recurse -Filter "PlayerScriptAssemblies" -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty FullName
    $scriptDirs = Get-ChildItem -Path $ProjectRoot -Directory -Recurse -Filter "ScriptAssemblies" -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty FullName
    $beeDirs = Get-ChildItem -Path $ProjectRoot -Directory -Recurse -Filter "Bee" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 3 -ExpandProperty FullName

    Set-LogValue $Logs "found_PlayerScriptAssemblies_dirs" (Trim-LogValue ($playerDirs -join ","))
    Set-LogValue $Logs "found_ScriptAssemblies_dirs" (Trim-LogValue ($scriptDirs -join ","))
    Set-LogValue $Logs "found_Bee_dirs" (Trim-LogValue ($beeDirs -join ","))

    $candidates = Get-DllCandidates $ProjectRoot 60 10
    Set-LogValue $Logs "discovered_dll_candidates" $candidates
}

function Test-BinaryContainsAny([string]$Path, [string[]]$Texts) {
    foreach ($text in $Texts) {
        if (Test-BinaryContains $Path $text) {
            return $true
        }
    }
    return $false
}

function Get-CompileHits(
    [string]$ProjectPath,
    [string[]]$Sentinels,
    [System.Collections.Generic.List[string]]$Logs,
    [int]$MaxBeeDirs = 5
) {
    $playerDir = Join-Path $ProjectPath "Library\PlayerScriptAssemblies"
    $scriptDir = Join-Path $ProjectPath "Library\ScriptAssemblies"
    $beeRoot = Join-Path $ProjectPath "Library\Bee"

    $playerExists = Test-Path $playerDir
    $scriptExists = Test-Path $scriptDir
    Set-LogValue $Logs "player_script_assemblies_dir" ("{0} exists={1}" -f $playerDir, [int]$playerExists)
    Set-LogValue $Logs "script_assemblies_dir" ("{0} exists={1}" -f $scriptDir, [int]$scriptExists)

    $beeDirs = @()
    if (Test-Path $beeRoot) {
        $beeDirs = Get-ChildItem -Path $beeRoot -Directory -Recurse -Filter "ManagedStripped" -ErrorAction SilentlyContinue
    }
    Set-LogValue $Logs "bee_managed_dirs_count" $beeDirs.Count

    function ScanDir([string]$DirPath) {
        $hits = New-Object System.Collections.Generic.List[string]
        if (-not (Test-Path $DirPath)) {
            return $hits
        }
        $dlls = Get-ChildItem -Path $DirPath -Filter "*.dll" -File -ErrorAction SilentlyContinue
        foreach ($dll in $dlls) {
            if (Test-BinaryContainsAny $dll.FullName $Sentinels) {
                $hits.Add($dll.FullName)
            }
        }
        return $hits
    }

    $hits = ScanDir $playerDir
    if ($hits.Count -gt 0) {
        return @{ Hits = $hits; Source = "PlayerScriptAssemblies"; PlayerDir = $playerDir; PlayerExists = $playerExists; ScriptExists = $scriptExists; BeeCount = $beeDirs.Count }
    }

    $hits = ScanDir $scriptDir
    if ($hits.Count -gt 0) {
        return @{ Hits = $hits; Source = "ScriptAssemblies"; PlayerDir = $playerDir; PlayerExists = $playerExists; ScriptExists = $scriptExists; BeeCount = $beeDirs.Count }
    }

    if ($beeDirs.Count -gt 0) {
        $beeDirs = $beeDirs | Sort-Object LastWriteTime -Descending | Select-Object -First $MaxBeeDirs
        $beeHits = New-Object System.Collections.Generic.List[string]
        foreach ($dir in $beeDirs) {
            $dirHits = ScanDir $dir.FullName
            foreach ($hit in $dirHits) {
                $beeHits.Add($hit)
            }
        }
        if ($beeHits.Count -gt 0) {
            return @{ Hits = $beeHits; Source = "Bee"; PlayerDir = $playerDir; PlayerExists = $playerExists; ScriptExists = $scriptExists; BeeCount = $beeDirs.Count }
        }
    }

    return @{ Hits = @(); Source = ""; PlayerDir = $playerDir; PlayerExists = $playerExists; ScriptExists = $scriptExists; BeeCount = $beeDirs.Count }
}

function Get-ManagedDir([string]$BuildDir) {
    if (-not (Test-Path $BuildDir)) {
        return $null
    }
    $dataDir = Get-ChildItem -Path $BuildDir -Directory -Filter "*_Data" -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $dataDir) {
        return $null
    }
    $managedDir = Join-Path $dataDir.FullName "Managed"
    if (Test-Path $managedDir) {
        return $managedDir
    }
    return $null
}

function Get-BackendGuess([string]$BuildDir) {
    if (Test-Path (Join-Path $BuildDir "GameAssembly.so")) {
        return "il2cpp"
    }
    return "mono"
}

function Apply-PureDotsManifestOverride([string]$ProjectPath, [string]$AbsolutePureDotsPath) {
    $manifestPath = Join-Path $ProjectPath "Packages\manifest.json"
    $lockPath = Join-Path $ProjectPath "Packages\packages-lock.json"
    $restore = @{
        Applied = $false
        ManifestPath = $manifestPath
        LockPath = $lockPath
        ManifestContent = $null
        LockContent = $null
        Error = ""
    }
    if (Test-Path $manifestPath) {
        $restore.ManifestContent = Get-Content -Path $manifestPath -Raw
    }
    if (Test-Path $lockPath) {
        $restore.LockContent = Get-Content -Path $lockPath -Raw
    }
    try {
        $manifest = Get-Content -Path $manifestPath -Raw | ConvertFrom-Json
        if (-not $manifest.dependencies) {
            $manifest | Add-Member -MemberType NoteProperty -Name dependencies -Value @{}
        }
        $manifest.dependencies."com.moni.puredots" = ("file:" + $AbsolutePureDotsPath.Replace("\", "/"))
        $manifest | ConvertTo-Json -Depth 100 | Set-Content -Path $manifestPath -Encoding ASCII
        if (Test-Path $lockPath) {
            Remove-Item -Path $lockPath -Force -ErrorAction SilentlyContinue
        }
        $restore.Applied = $true
    } catch {
        $restore.Error = $_.Exception.Message
    }
    return $restore
}

function Restore-PureDotsManifestOverride($RestoreInfo) {
    if (-not $RestoreInfo -or -not $RestoreInfo.Applied) {
        return
    }
    if ($RestoreInfo.ManifestContent -ne $null) {
        Set-Content -Path $RestoreInfo.ManifestPath -Value $RestoreInfo.ManifestContent -Encoding ASCII
    }
    if ($RestoreInfo.LockContent -ne $null) {
        Set-Content -Path $RestoreInfo.LockPath -Value $RestoreInfo.LockContent -Encoding ASCII
    }
}

function Resolve-FilePackagePath([string]$Value, [string]$BaseDir) {
    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $null
    }
    if ($Value -notmatch "^file:") {
        return $null
    }
    $rel = $Value -replace "^file:(//)?", ""
    $rel = $rel.TrimStart("/")
    $rel = $rel.Replace("/", "\")
    if ([System.IO.Path]::IsPathRooted($rel)) {
        return Normalize-Path $rel
    }
    if ([string]::IsNullOrWhiteSpace($BaseDir)) {
        return Normalize-Path $rel
    }
    return Normalize-Path (Join-Path $BaseDir $rel)
}

function Test-PureDotsPackage([string]$Path) {
    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $false
    }
    $pkgPath = Join-Path $Path "package.json"
    if (-not (Test-Path $pkgPath)) {
        return $false
    }
    try {
        $pkg = Get-Content -Path $pkgPath -Raw | ConvertFrom-Json
        return ($pkg -and $pkg.name -eq "com.moni.puredots")
    } catch {
        return $false
    }
}

function Invoke-HeadlessManifestSwap(
    [string]$ProjectName,
    [string]$ProjectRoot,
    [System.Collections.Generic.List[string]]$Logs
) {
    $scriptPath = Join-Path $env:TRI_ROOT "Tools\Tools\use_headless_manifest_windows.ps1"
    $key = $ProjectName.ToLowerInvariant()
    $Logs.Add("headless_swap_script_path=" + $scriptPath)
    if (-not (Test-Path $scriptPath)) {
        $Logs.Add("headless_swap_exit_code_" + $key + "=missing")
        $Logs.Add("headless_swap_error_" + $key + "=missing_script")
        $Logs.Add("headless_manifest_swap_" + $key + "=0")
        return @{ Ok = $false; Error = "HEADLESS_MANIFEST_SWAP_FAILED" }
    }

    $stderrPath = Join-Path $env:TEMP ("headless_swap_{0}_{1}.err.log" -f $key, [Guid]::NewGuid().ToString("N"))
    $args = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $scriptPath, "-ProjectPath", $ProjectRoot)
    try {
        $proc = Start-Process -FilePath (Get-ShellExe) -ArgumentList $args -PassThru -Wait -RedirectStandardError $stderrPath
        $exitCode = $proc.ExitCode
    } catch {
        $Logs.Add("headless_swap_exit_code_" + $key + "=exception")
        $Logs.Add("headless_swap_error_" + $key + "=" + (Trim-LogValue $_.Exception.Message))
        $Logs.Add("headless_manifest_swap_" + $key + "=0")
        return @{ Ok = $false; Error = "HEADLESS_MANIFEST_SWAP_FAILED" }
    }

    $Logs.Add("headless_swap_exit_code_" + $key + "=" + $exitCode)
    $Logs.Add("headless_manifest_swap_" + $key + "=" + ($(if ($exitCode -eq 0) { "1" } else { "0" })))
    if (Test-Path $stderrPath) {
        $stderrText = Get-Content -Path $stderrPath -ErrorAction SilentlyContinue | Out-String
        if (-not [string]::IsNullOrWhiteSpace($stderrText)) {
            $Logs.Add("headless_swap_error_" + $key + "=" + (Trim-LogValue $stderrText))
        }
        Remove-Item -Path $stderrPath -Force -ErrorAction SilentlyContinue
    }

    if ($exitCode -ne 0) {
        $existing = $Logs | Where-Object { $_ -like ("headless_swap_error_" + $key + "=*") }
        if (-not $existing) {
            $Logs.Add("headless_swap_error_" + $key + "=exit_code=" + $exitCode)
        }
        return @{ Ok = $false; Error = "HEADLESS_MANIFEST_SWAP_FAILED" }
    }
    return @{ Ok = $true; Error = "" }
}

function Get-PureDotsResolutionSnapshot(
    [string]$ProjectName,
    [string]$UnityProjectPath,
    [string]$ExpectedFull,
    [string[]]$Sentinels,
    [System.Collections.Generic.List[string]]$Logs
) {
    $prefix = $ProjectName.ToLowerInvariant()
    $packagesDir = Join-Path $UnityProjectPath "Packages"
    $packagesLockPath = Join-Path $packagesDir "packages-lock.json"
    $packagesLockExists = Test-Path $packagesLockPath
    $lockContainsPureDots = $false
    if ($packagesLockExists) {
        $lockContainsPureDots = Select-String -Path $packagesLockPath -SimpleMatch -Pattern '"com.moni.puredots"' -Quiet
    }
    Set-LogValue $Logs "${prefix}_packages_lock_exists" ([int]$packagesLockExists)
    Set-LogValue $Logs "packages_lock_exists" ([int]$packagesLockExists)
    Set-LogValue $Logs "${prefix}_lock_contains_puredots" ([int]$lockContainsPureDots)
    Set-LogValue $Logs "lock_contains_puredots" ([int]$lockContainsPureDots)

    $manifestInfo = Get-ManifestPureDots $UnityProjectPath
    Set-LogValue $Logs "${prefix}_manifest_puredots" $manifestInfo.Value
    Set-LogValue $Logs "manifest_puredots" $manifestInfo.Value
    Set-LogValue $Logs "${prefix}_manifest_puredots_raw" $manifestInfo.Value
    Set-LogValue $Logs "manifest_puredots_raw" $manifestInfo.Value
    if ($manifestInfo.Error) {
        $Logs.Add("${prefix}_manifest_puredots_error=" + $manifestInfo.Error)
    }

    $lockInfo = Get-PackagesLockPureDots $UnityProjectPath
    Set-LogValue $Logs "${prefix}_lock_puredots_version" $lockInfo.Version
    Set-LogValue $Logs "${prefix}_lock_puredots_source" $lockInfo.Source
    Set-LogValue $Logs "lock_puredots_version" $lockInfo.Version
    Set-LogValue $Logs "lock_puredots_source" $lockInfo.Source
    Set-LogValue $Logs "${prefix}_lock_puredots_raw" $lockInfo.Version
    Set-LogValue $Logs "lock_puredots_raw" $lockInfo.Version
    if ($lockInfo.Error) {
        $Logs.Add("${prefix}_lock_puredots_error=" + $lockInfo.Error)
    }

    $manifestFull = Resolve-FilePackagePath $manifestInfo.Value $packagesDir
    $lockFull = Resolve-FilePackagePath $lockInfo.Version $packagesDir
    Set-LogValue $Logs "${prefix}_manifest_puredots_full" $manifestFull
    Set-LogValue $Logs "manifest_puredots_full" $manifestFull
    Set-LogValue $Logs "${prefix}_lock_puredots_full" $lockFull
    Set-LogValue $Logs "lock_puredots_full" $lockFull
    Set-LogValue $Logs "${prefix}_expected_puredots_full" $ExpectedFull
    Set-LogValue $Logs "expected_puredots_full" $ExpectedFull

    $packageJsonPresent = Test-PureDotsPackage $ExpectedFull
    Set-LogValue $Logs "${prefix}_package_json_present" ([int]$packageJsonPresent)
    Set-LogValue $Logs "package_json_present" ([int]$packageJsonPresent)

    $resolvedFull = $manifestFull
    $resolvedExists = $resolvedFull -and (Test-Path $resolvedFull)
    Set-LogValue $Logs "${prefix}_resolved_puredots_path" $resolvedFull
    Set-LogValue $Logs "resolved_puredots_path" $resolvedFull

    $sourceInfo = Get-SourceSentinelInfo $resolvedFull $Sentinels
    Set-LogValue $Logs "${prefix}_resolved_source_sentinel_present" ([int]$sourceInfo.Present)
    Set-LogValue $Logs "resolved_source_sentinel_present" ([int]$sourceInfo.Present)
    if ($sourceInfo.Hash) {
        Set-LogValue $Logs "${prefix}_resolved_source_file_hash" $sourceInfo.Hash
        Set-LogValue $Logs "resolved_source_file_hash" $sourceInfo.Hash
    }

    $manifestMatches = $manifestFull -and ($manifestFull.TrimEnd('\') -ieq $ExpectedFull.TrimEnd('\'))
    $lockMatches = $lockFull -and ($lockFull.TrimEnd('\') -ieq $ExpectedFull.TrimEnd('\'))
    $resolvedMatches = $manifestMatches -and $lockMatches -and $packageJsonPresent -and $resolvedExists -and $sourceInfo.Present -and $packagesLockExists -and $lockContainsPureDots

    return @{
        ManifestFull = $manifestFull
        LockFull = $lockFull
        ResolvedFull = $resolvedFull
        ResolvedExists = $resolvedExists
        SourceInfo = $sourceInfo
        ManifestMatches = $manifestMatches
        LockMatches = $lockMatches
        PackageJsonPresent = $packageJsonPresent
        ResolvedMatches = $resolvedMatches
    }
}

function Log-HeadlessPackagePaths(
    [string]$ProjectName,
    [string]$ProjectRoot,
    [System.Collections.Generic.List[string]]$Logs
) {
    $key = $ProjectName.ToLowerInvariant()
    $packagesDir = Join-Path $ProjectRoot "Packages"
    $lockPath = Join-Path $packagesDir "packages-lock.json"
    $lockHeadlessPath = Join-Path $packagesDir "packages-lock.headless.json"
    $manifestPath = Join-Path $packagesDir "manifest.json"
    $manifestHeadlessPath = Join-Path $packagesDir "manifest.headless.json"

    $Logs.Add("${key}_packages_dir=" + $packagesDir)
    $Logs.Add("${key}_lock_path=" + $lockPath)
    $Logs.Add("${key}_lock_headless_path=" + $lockHeadlessPath)
    $Logs.Add("${key}_manifest_path=" + $manifestPath)
    $Logs.Add("${key}_manifest_headless_path=" + $manifestHeadlessPath)

    return @{
        PackagesDir = $packagesDir
        LockPath = $lockPath
        LockHeadlessPath = $lockHeadlessPath
        ManifestPath = $manifestPath
        ManifestHeadlessPath = $manifestHeadlessPath
    }
}

function Validate-HeadlessLockPostSwap(
    [string]$ProjectName,
    [string]$ProjectRoot,
    [System.Collections.Generic.List[string]]$Logs
) {
    $key = $ProjectName.ToLowerInvariant()
    $paths = Log-HeadlessPackagePaths $ProjectName $ProjectRoot $Logs
    $packagesDir = $paths.PackagesDir
    $lockPath = $paths.LockPath
    $lockHeadlessPath = $paths.LockHeadlessPath

    $lockExists = Test-Path $lockPath
    if (-not $lockExists) {
        if (Test-Path $lockHeadlessPath) {
            Copy-Item -Path $lockHeadlessPath -Destination $lockPath -Force -ErrorAction SilentlyContinue
        }
        $lockExists = Test-Path $lockPath
    }
    $Logs.Add("${key}_packages_lock_exists_postswap=" + ([int]$lockExists))

    if (-not $lockExists) {
        $listing = Get-ChildItem -Path $packagesDir -Name -ErrorAction SilentlyContinue | Select-Object -First 40
        $listingText = Trim-LogValue ($listing -join ",")
        $Logs.Add("${key}_packages_dir_listing_top=" + $listingText)
        return @{ Ok = $false; Error = "HEADLESS_SWAP_LOCK_NOT_CREATED" }
    }

    $raw = $null
    $lockData = $null
    try {
        $raw = Get-Content -Path $lockPath -Raw -ErrorAction Stop
        $lockData = $raw | ConvertFrom-Json -ErrorAction Stop
    } catch {
        $size = ""
        try {
            $size = (Get-Item -Path $lockPath -ErrorAction SilentlyContinue).Length
        } catch { }
        $lines = Get-Content -Path $lockPath -TotalCount 20 -ErrorAction SilentlyContinue
        $Logs.Add("${key}_lock_size_bytes=" + $size)
        $Logs.Add("${key}_lock_first20_lines=" + (Trim-LogValue ($lines -join " | ")))
        return @{ Ok = $false; Error = "HEADLESS_SWAP_LOCK_PARSE_ERROR" }
    }

    $hasPureDots = $false
    if ($lockData) {
        if ($lockData.dependencies -and $lockData.dependencies."com.moni.puredots") {
            $hasPureDots = $true
        } elseif ($lockData.packages -and $lockData.packages."com.moni.puredots") {
            $hasPureDots = $true
        }
    }
    $Logs.Add("${key}_lock_contains_puredots=" + ([int]$hasPureDots))
    if (-not $hasPureDots) {
        return @{ Ok = $false; Error = "HEADLESS_SWAP_LOCK_MISSING_PUREDOTS_KEY" }
    }

    return @{ Ok = $true; Error = "" }
}

function Sanitize-NoteValue([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ""
    }
    $clean = $Value -replace "[\r\n;]+", " "
    return $clean.Trim()
}

function Prepare-RequestBuildDir(
    [string]$ProjectRoot,
    [string]$BuildRootName,
    [string]$BuildDir,
    [string]$RequestId
) {
    $buildRoot = Join-Path $ProjectRoot ("Builds\" + $BuildRootName)
    $dest = Join-Path $buildRoot ("Linux_req_" + $RequestId)
    if (Test-Path $dest) {
        Remove-Item -Path $dest -Recurse -Force -ErrorAction SilentlyContinue
    }
    New-Item -ItemType Directory -Path $dest -Force | Out-Null
    Copy-Item -Path (Join-Path $BuildDir "*") -Destination $dest -Recurse -Force
    return $dest
}

function Get-PlayerSentinelInfo([string]$BuildDir, [string[]]$Sentinels) {
    $backend = Get-BackendGuess $BuildDir
    $target = ""
    $found = $false
    if ($backend -eq "il2cpp") {
        $gameAsm = Join-Path $BuildDir "GameAssembly.so"
        if (Test-Path $gameAsm) {
            $target = $gameAsm
            $found = Test-BinaryContainsAny $gameAsm $Sentinels
        }
        if (-not $found) {
            $dataDir = Get-ChildItem -Path $BuildDir -Directory -Filter "*_Data" -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($dataDir) {
                $metaPath = Join-Path $dataDir.FullName "il2cpp_data\Metadata\global-metadata.dat"
                if (Test-Path $metaPath) {
                    $target = $metaPath
                    $found = Test-BinaryContainsAny $metaPath $Sentinels
                }
            }
        }
        return @{ Backend = $backend; Target = $target; Found = $found }
    }
    $managedDir = Get-ManagedDir $BuildDir
    if ($managedDir) {
        $targets = @()
        foreach ($name in @("PureDOTS.Systems.dll", "PureDOTS.Runtime.dll")) {
            $path = Join-Path $managedDir $name
            if (Test-Path $path) {
                $targets += $path
            }
        }
        if (-not $targets) {
            $targets = Get-ChildItem -Path $managedDir -Filter "PureDOTS*.dll" -File -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName
        }
        foreach ($path in $targets) {
            $target = $path
            if (Test-BinaryContainsAny $path $Sentinels) {
                $found = $true
                break
            }
        }
    }
    return @{ Backend = $backend; Target = $target; Found = $found }
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

function Get-PureDotsInfo {
    $root = Join-Path $env:TRI_ROOT "puredots"
    $shaResult = Invoke-Git $root rev-parse HEAD
    $branchResult = Invoke-Git $root rev-parse --abbrev-ref HEAD
    $sha = if ($shaResult.ExitCode -eq 0 -and $shaResult.Output) { $shaResult.Output } else { "unknown" }
    $branch = if ($branchResult.ExitCode -eq 0 -and $branchResult.Output) { $branchResult.Output } else { "unknown" }
    return @{
        Root = $root
        Sha = $sha
        Branch = $branch
    }
}

function Sync-PureDots([string]$DesiredRef) {
    if (-not $DesiredRef) {
        return @{ Ok = $true; Error = ""; OriginalBranch = "" }
    }
    $root = Join-Path $env:TRI_ROOT "puredots"
    if (-not (Test-Path $root)) {
        return @{ Ok = $false; Error = "puredots root missing: $root"; OriginalBranch = "" }
    }
    $branchResult = Invoke-Git $root rev-parse --abbrev-ref HEAD
    if ($branchResult.ExitCode -ne 0) {
        return @{ Ok = $false; Error = "git rev-parse failed for puredots: $($branchResult.Output)"; OriginalBranch = "" }
    }
    $originalBranch = $branchResult.Output
    $fetchResult = Invoke-Git $root fetch --prune
    if ($fetchResult.ExitCode -ne 0) {
        return @{ Ok = $false; Error = "git fetch failed for puredots: $($fetchResult.Output)"; OriginalBranch = "" }
    }
    $checkoutResult = Invoke-Git $root checkout --quiet $DesiredRef
    if ($checkoutResult.ExitCode -ne 0) {
        return @{ Ok = $false; Error = "git checkout failed for puredots (${DesiredRef}): $($checkoutResult.Output)"; OriginalBranch = "" }
    }
    $resetResult = Invoke-Git $root reset --hard --quiet $DesiredRef
    if ($resetResult.ExitCode -ne 0) {
        return @{ Ok = $false; Error = "git reset --hard failed for puredots (${DesiredRef}): $($resetResult.Output)"; OriginalBranch = "" }
    }
    return @{ Ok = $true; Error = ""; OriginalBranch = $originalBranch }
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

function Clean-Worktree([string]$ProjectRoot, [string]$ProjectName, [bool]$AutoClean) {
    $statusResult = Invoke-Git $ProjectRoot status --porcelain
    if ($statusResult.ExitCode -ne 0) {
        return @{
            Ok = $false
            Error = "git status failed for ${ProjectName}: $($statusResult.Output)"
        }
    }
    $dirtyLines = $statusResult.Output
    if (-not $dirtyLines) {
        return @{ Ok = $true; Error = ""; OriginalBranch = "" }
    }
    if (-not $AutoClean) {
        return @{
            Ok = $false
            Error = "dirty worktree for ${ProjectName}; set TRI_GIT_AUTOCLEAN=1 or supply desired_build_commit. Dirty files: $dirtyLines"
        }
    }
    $resetResult = Invoke-Git $ProjectRoot reset --hard --quiet
    if ($resetResult.ExitCode -ne 0) {
        return @{
            Ok = $false
            Error = "git reset --hard failed for ${ProjectName}: $($resetResult.Output)"
        }
    }
    $cleanResult = Invoke-Git $ProjectRoot clean -fd --quiet
    if ($cleanResult.ExitCode -ne 0) {
        return @{
            Ok = $false
            Error = "git clean -fd failed for ${ProjectName}: $($cleanResult.Output)"
        }
    }
    $statusResult = Invoke-Git $ProjectRoot status --porcelain
    if ($statusResult.ExitCode -ne 0) {
        return @{
            Ok = $false
            Error = "git status failed for ${ProjectName} after clean: $($statusResult.Output)"
        }
    }
    $dirtyLines = $statusResult.Output
    if ($dirtyLines) {
        return @{
            Ok = $false
            Error = "worktree still dirty after clean for ${ProjectName}: $dirtyLines"
        }
    }
    return @{ Ok = $true; Error = ""; OriginalBranch = "" }
}

function Sync-Project([string]$ProjectRoot, [string]$ProjectName, [string]$DesiredCommit) {
    $autoCleanEnabled = $false
    if ($env:TRI_GIT_AUTOCLEAN -eq "1") {
        $autoCleanEnabled = $true
    }
    $forceClean = [bool]$DesiredCommit
    $cleanResult = Clean-Worktree $ProjectRoot $ProjectName ($autoCleanEnabled -or $forceClean)
    if (-not $cleanResult.Ok) {
        return $cleanResult
    }

    $fetchResult = Invoke-Git $ProjectRoot fetch --prune
    if ($fetchResult.ExitCode -ne 0) {
        return @{
            Ok = $false
            Error = "git fetch failed for ${ProjectName}: $($fetchResult.Output)"
        }
    }
    $branchResult = Invoke-Git $ProjectRoot rev-parse --abbrev-ref HEAD
    if ($branchResult.ExitCode -ne 0) {
        return @{
            Ok = $false
            Error = "git rev-parse failed for ${ProjectName}: $($branchResult.Output)"
        }
    }
    $currentBranch = $branchResult.Output
    if ($DesiredCommit) {
        $checkoutResult = Invoke-Git $ProjectRoot checkout --quiet $DesiredCommit
        if ($checkoutResult.ExitCode -ne 0) {
            return @{
                Ok = $false
                Error = "git checkout failed for ${ProjectName} (${DesiredCommit}): $($checkoutResult.Output)"
            }
        }
        return @{ Ok = $true; Error = ""; OriginalBranch = $currentBranch }
    }
    $upstreamResult = Invoke-Git $ProjectRoot rev-parse --abbrev-ref --symbolic-full-name "@{u}"
    $upstream = if ($upstreamResult.ExitCode -eq 0) { $upstreamResult.Output } else { "origin/$currentBranch" }
    $pullResult = Invoke-Git $ProjectRoot merge --ff-only --quiet $upstream
    if ($pullResult.ExitCode -ne 0) {
        return @{
            Ok = $false
            Error = "git merge --ff-only failed for ${ProjectName} ($upstream): $($pullResult.Output)"
        }
    }
    return @{ Ok = $true; Error = ""; OriginalBranch = "" }
}

function Test-ProjectPin([string]$ProjectRoot, [string]$ProjectName, [string]$DesiredCommit, [System.Collections.Generic.List[string]]$Logs) {
    if (-not $DesiredCommit) {
        return @{ Ok = $true; Error = "" }
    }
    $fetchResult = Invoke-Git $ProjectRoot fetch --prune
    if ($fetchResult.ExitCode -ne 0) {
        return @{ Ok = $false; Error = "git fetch failed for ${ProjectName}: $($fetchResult.Output)" }
    }
    $verifyResult = Invoke-Git $ProjectRoot rev-parse --verify --quiet $DesiredCommit
    if ($verifyResult.ExitCode -ne 0) {
        $Logs.Add("bad_project_pin=$DesiredCommit;repo=$ProjectName")
        return @{ Ok = $false; Error = "BAD_PROJECT_PIN" }
    }
    return @{ Ok = $true; Error = "" }
}

function Get-LatestBuildPath([string]$ProjectRoot, [string]$BuildRootName, [string]$ExeName) {
    $buildRoot = Join-Path $ProjectRoot ("Builds\" + $BuildRootName)
    if (-not (Test-Path $buildRoot)) {
        throw "Build root missing: $buildRoot"
    }
    $latest = Get-ChildItem -Path $buildRoot -Directory -Filter "Linux_*" -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -notlike "Linux_req_*" } |
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

function Publish-Build(
    [string]$Project,
    [string]$BuildDir,
    [string]$ExeName,
    [string]$RequestId,
    [string]$PureDotsBranch,
    [string]$PureDotsSha,
    [string]$ExtraNotes
) {
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
    if ($publishDirForOps -match '^\\\\\\\\wsl(\\.localhost)?\\$?\\\\[^\\\\]+\\\\') {
        $suffix = $publishDirForOps -replace '^\\\\\\\\wsl(\\.localhost)?\\$?\\\\[^\\\\]+\\\\', ""
        $publishDirForOps = "/" + ($suffix -replace "\\\\", "/")
    }
    if ($exePathForOps -match '^\\\\\\\\wsl(\\.localhost)?\\$?\\\\[^\\\\]+\\\\') {
        $suffix = $exePathForOps -replace '^\\\\\\\\wsl(\\.localhost)?\\$?\\\\[^\\\\]+\\\\', ""
        $exePathForOps = "/" + ($suffix -replace "\\\\", "/")
    }

    $notes = "puredots_branch=$PureDotsBranch;puredots_sha=$PureDotsSha"
    if ($ExtraNotes) {
        $notes = $notes + ";" + $ExtraNotes
    }
    Invoke-TriOps @(
        "write_current", "--project", $Project, "--path", $publishDirForOps,
        "--executable", $exePathForOps, "--build-commit", $commit,
        "--build-id", $buildId, "--request-id", $RequestId,
        "--notes", $notes
    ) | Out-Null

    return @{
        BuildCommit = $commit
        PublishedPath = $publishDirForOps
        ExecutablePath = $exePathForOps
        BuildId = $buildId
    }
}

function Ensure-PureDotsResolution(
    [string]$ProjectName,
    [string]$UnityProjectPath,
    [string]$ExpectedPureDotsPath,
    [string[]]$Sentinels,
    [System.Collections.Generic.List[string]]$Logs
) {
    $prefix = $ProjectName.ToLowerInvariant()
    $expectedFull = Normalize-Path $ExpectedPureDotsPath

    Set-LogValue $Logs "${prefix}_unity_project_path" $UnityProjectPath
    Set-LogValue $Logs "unity_project_path" $UnityProjectPath
    $snapshot = Get-PureDotsResolutionSnapshot $ProjectName $UnityProjectPath $expectedFull $Sentinels $Logs
    $resolvedFull = $snapshot.ResolvedFull
    $resolvedExists = $snapshot.ResolvedExists
    $sourceInfo = $snapshot.SourceInfo
    $resolvedMatches = $snapshot.ResolvedMatches

    $restore = $null
    if (-not $resolvedMatches) {
        $Logs.Add("${prefix}_manifest_override_reason=resolved_mismatch_or_missing")
        $restore = Apply-PureDotsManifestOverride $UnityProjectPath $expectedFull
        if (-not $restore.Applied) {
            $err = if ($restore.Error) { $restore.Error } else { "manifest_override_failed" }
            return @{ Ok = $false; Error = "PACKAGE_RESOLUTION_MISMATCH_PUREDOTS $err"; Restore = $restore; ResolvedPath = $resolvedFull; UnityProjectPath = $UnityProjectPath }
        }
        $Logs.Add("${prefix}_manifest_override_applied=1")

        $libraryPath = Join-Path $UnityProjectPath "Library"
        if (Test-Path $libraryPath) {
            Remove-Item -Path $libraryPath -Recurse -Force -ErrorAction SilentlyContinue
            $Logs.Add("${prefix}_full_library_wipe=1")
        }

        $snapshot = Get-PureDotsResolutionSnapshot $ProjectName $UnityProjectPath $expectedFull $Sentinels $Logs
        $resolvedFull = $snapshot.ResolvedFull
        $resolvedExists = $snapshot.ResolvedExists
        $sourceInfo = $snapshot.SourceInfo
        $resolvedMatches = $snapshot.ResolvedMatches

        if (-not $resolvedMatches) {
            return @{ Ok = $false; Error = "PACKAGE_RESOLUTION_MISMATCH_PUREDOTS"; Restore = $restore; ResolvedPath = $resolvedFull; UnityProjectPath = $UnityProjectPath }
        }
    }

    return @{ Ok = $true; Error = ""; Restore = $restore; ResolvedPath = $resolvedFull; UnityProjectPath = $UnityProjectPath }
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
    $desiredCommit = $null
    if ($reqObj -and $reqObj.desired_build_commit) {
        $desiredCommit = [string]$reqObj.desired_build_commit
    }
    $notesRaw = Get-NotesRaw $reqObj
    $requestedPureDotsRef = Get-NoteValue $notesRaw "puredots_ref"

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
    $originalBranches = @{}
    $scriptPath = $PSCommandPath
    $scriptHash = (Get-FileHash -Path $scriptPath -Algorithm SHA256).Hash
    $logs.Add("builder_script_path=" + $scriptPath)
    $logs.Add("builder_script_hash=" + $scriptHash)
    $logs.Add("builder_boot_utc=" + (Get-Date).ToUniversalTime().ToString("o"))
    $logs.Add("state_dir_wsl=" + $stateDirWsl)
    $buildUser = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
    $sessionType = if ([Environment]::UserInteractive) { "interactive" } else { "non-interactive" }
    $unityEditorPath = if ($env:TRI_UNITY_EXE) { $env:TRI_UNITY_EXE } elseif ($env:UNITY_WIN) { $env:UNITY_WIN } else { "" }
    $logs.Add("build_user=" + $buildUser)
    $logs.Add("session_type=" + $sessionType)
    if ($unityEditorPath) {
        $logs.Add("unity_editor_path=" + $unityEditorPath)
    }
    if ($notesRaw) {
        $logs.Add("notes_raw=" + ($notesRaw -replace "[\r\n]+", " "))
    }
    if ($requestedPureDotsRef) {
        $logs.Add("puredots_ref_parsed=" + $requestedPureDotsRef + " len=" + $requestedPureDotsRef.Length)
    }
    Set-LogValue $logs "backend_guess" "unknown"
    Set-LogValue $logs "build_dir_selected" "unknown"
    Set-LogValue $logs "sentinel_target" "unknown"
    if ($requestedPureDotsRef) {
        Set-LogValue $logs "unity_project_path" "unknown"
        Set-LogValue $logs "manifest_puredots" "unknown"
        Set-LogValue $logs "lock_puredots_source" "unknown"
        Set-LogValue $logs "lock_puredots_version" "unknown"
        Set-LogValue $logs "resolved_puredots_path" "unknown"
        Set-LogValue $logs "resolved_source_sentinel_present" "unknown"
        Set-LogValue $logs "resolved_source_file_hash" "unknown"
        Set-LogValue $logs "unity_exit_code" "unknown"
        Set-LogValue $logs "unity_log_path" "unknown"
        Set-LogValue $logs "unity_cmdline_projectPath" "unknown"
        Set-LogValue $logs "unity_log_tail" ""
        Set-LogValue $logs "unity_error_lines" ""
        Set-LogValue $logs "unity_license_error" "0"
        Set-LogValue $logs "licensing_lines" ""
        Set-LogValue $logs "library_exists" "unknown"
        Set-LogValue $logs "library_dirs_top" ""
        Set-LogValue $logs "found_PlayerScriptAssemblies_dirs" ""
        Set-LogValue $logs "found_ScriptAssemblies_dirs" ""
        Set-LogValue $logs "found_Bee_dirs" ""
        Set-LogValue $logs "discovered_dll_candidates" ""
        Set-LogValue $logs "player_script_assemblies_dir" "unknown"
        Set-LogValue $logs "script_assemblies_dir" "unknown"
        Set-LogValue $logs "bee_managed_dirs_count" "unknown"
        Set-LogValue $logs "compile_hits" ""
        Set-LogValue $logs "compile_hits_source" ""
    }

    try {
        $puredotsInfo = $null
        if ($requestedPureDotsRef) {
            $syncPureDots = Sync-PureDots $requestedPureDotsRef
            if (-not $syncPureDots.Ok) {
                $overallStatus = "failed"
                $errorMessage = $syncPureDots.Error
            } elseif ($syncPureDots.OriginalBranch) {
                $originalBranches["puredots"] = $syncPureDots.OriginalBranch
            }
        }

        if ($overallStatus -eq "ok") {
            $puredotsInfo = Get-PureDotsInfo
            $logs.Add("puredots_branch=" + $puredotsInfo.Branch)
            $logs.Add("puredots_sha=" + $puredotsInfo.Sha)
        }

        if ($overallStatus -ne "ok") {
        } elseif ($builderMode -eq "orchestrator") {
            Write-Heartbeat "queued_external" "req=$requestId" $cycle
            $overallStatus = "queued_external"
            $publishedPath = "n/a"
            $buildCommit = "unknown"
            $logs.Add("mode=orchestrator")
        } else {
            $probeBuild = [bool]$requestedPureDotsRef
            $sentinels = @("oracle_probe_v1", "probeVersion")
            $expectedPureDotsPath = Join-Path $env:TRI_ROOT "puredots\Packages\com.moni.puredots"
            $projectInfos = New-Object System.Collections.Generic.List[object]
            foreach ($project in $projects) {
                $info = Get-ProjectInfo $project
                if (-not $info) {
                    $overallStatus = "failed"
                    $errorMessage = "unknown project: $project"
                    break
                }
                $projectInfos.Add($info)
            }

            if ($overallStatus -eq "ok") {
                foreach ($info in $projectInfos) {
                    $projectRoot = Join-Path $env:TRI_ROOT $info.Name
                    if ($desiredCommit) {
                        $pinCheck = Test-ProjectPin $projectRoot $info.Name $desiredCommit $logs
                        if (-not $pinCheck.Ok) {
                            $overallStatus = "failed"
                            $errorMessage = $pinCheck.Error
                            break
                        }
                    }
                    $syncResult = Sync-Project $projectRoot $info.Name $desiredCommit
                    if (-not $syncResult.Ok) {
                        $overallStatus = "failed"
                        $errorMessage = $syncResult.Error
                        break
                    }
                    if ($syncResult.OriginalBranch) {
                        $originalBranches[$info.Name] = $syncResult.OriginalBranch
                    }
                }
            }

            if ($overallStatus -eq "ok") {
                foreach ($info in $projectInfos) {
                    $projectRoot = Join-Path $env:TRI_ROOT $info.Name
                    $unityProjectPath = $projectRoot
                    $resolvedPureDotsPath = ""
                    $prefix = $info.Name.ToLowerInvariant()
                    $overrideInfo = $null
                    $compileInfo = $null
                    $buildLogDir = Join-Path $env:TRI_STATE_DIR "builds\logs"
                    New-Item -ItemType Directory -Path $buildLogDir -Force | Out-Null
                    $logPath = Join-Path $buildLogDir ("{0}_{1}.log" -f $info.Name, (Get-Date -Format "yyyyMMdd_HHmmss"))

                    try {
                        $swapResult = Invoke-HeadlessManifestSwap $info.Name $projectRoot $logs
                        if (-not $swapResult.Ok) {
                            $overallStatus = "failed"
                            $errorMessage = $swapResult.Error
                            break
                        }

                        $lockGuard = Validate-HeadlessLockPostSwap $info.Name $projectRoot $logs
                        if (-not $lockGuard.Ok) {
                            $overallStatus = "failed"
                            $errorMessage = $lockGuard.Error
                            break
                        }

                        if ($probeBuild) {
                            $resolution = Ensure-PureDotsResolution $info.Name $unityProjectPath $expectedPureDotsPath $sentinels $logs
                            $overrideInfo = $resolution.Restore
                            $unityProjectPath = $resolution.UnityProjectPath
                            $resolvedPureDotsPath = $resolution.ResolvedPath
                            if (-not $resolution.Ok) {
                                $overallStatus = "failed"
                                $errorMessage = $resolution.Error
                                break
                            }
                        }

                        $exitCode = Invoke-BuildScript $info.BuildScript $logPath ("building_" + $info.Name) $requestId $cycle
                        $logs.Add("build_log_" + $info.Name + "=" + $logPath)

                        if ($probeBuild) {
                            Add-CompileDiagnostics $projectRoot $logPath $exitCode $projectRoot $logs
                            $licensingLines = Get-UnityLicensingLines $logPath
                            if ($licensingLines.Count -gt 0) {
                                Set-LogValue $logs "unity_license_error" "1"
                                Set-LogValue $logs "licensing_lines" (Format-LogSnippet $licensingLines)
                                $overallStatus = "failed"
                                $errorMessage = "UNITY_LICENSE_ERROR"
                                break
                            }
                        }

                        if ($exitCode -ne 0) {
                            if ($probeBuild) {
                                $compileInfo = Get-CompileHits $unityProjectPath $sentinels $logs
                                $hitsText = if ($compileInfo.Hits.Count -gt 0) { ($compileInfo.Hits -join ",") } else { "" }
                                Set-LogValue $logs "compile_hits" $hitsText
                                Set-LogValue $logs "compile_hits_source" $compileInfo.Source
                            }
                            $overallStatus = "failed"
                            $errorMessage = "build failed for $($info.Name) (exit $exitCode)"
                            break
                        }

                        $logProjectPath = Get-UnityProjectPathFromLog $logPath
                        if ($logProjectPath) {
                            $unityProjectPath = $logProjectPath
                            Set-LogValue $logs "${prefix}_unity_project_path" $logProjectPath
                            Set-LogValue $logs "unity_project_path" $logProjectPath
                        }

                        $compileInfo = $null
                        if ($probeBuild) {
                            $compileInfo = Get-CompileHits $unityProjectPath $sentinels $logs
                            $hitsText = if ($compileInfo.Hits.Count -gt 0) { ($compileInfo.Hits -join ",") } else { "" }
                            Set-LogValue $logs "compile_hits" $hitsText
                            Set-LogValue $logs "compile_hits_source" $compileInfo.Source
                            if ($compileInfo.Hits.Count -eq 0) {
                                $outputsMissing = (-not $compileInfo.PlayerExists) -and (-not $compileInfo.ScriptExists) -and ($compileInfo.BeeCount -eq 0)
                                $overallStatus = "failed"
                                if ($outputsMissing) {
                                    $errorMessage = "BUILD_OUTPUTS_MISSING"
                                } else {
                                    $errorMessage = "SCRIPTASSEMBLIES_MISSING_PROBEVERSION"
                                }
                                break
                            }
                        }

                        Write-Heartbeat ("publishing_" + $info.Name) "req=$requestId" $cycle
                        Renew-Leases $requestId

                        $buildDir = Get-LatestBuildPath $projectRoot $info.BuildRootName $info.ExecutableName
                        if ($probeBuild) {
                            $stagedDir = Prepare-RequestBuildDir $projectRoot $info.BuildRootName $buildDir $requestId
                            $logs.Add("${prefix}_staged_from=" + $buildDir)
                            $buildDir = $stagedDir
                        }

                        if ($probeBuild) {
                            $sentinelInfo = Get-PlayerSentinelInfo $buildDir $sentinels
                            Set-LogValue $logs "${prefix}_backend_guess" $sentinelInfo.Backend
                            Set-LogValue $logs "backend_guess" $sentinelInfo.Backend
                            Set-LogValue $logs "${prefix}_build_dir_selected" $buildDir
                            Set-LogValue $logs "build_dir_selected" $buildDir
                            Set-LogValue $logs "${prefix}_sentinel_target" $sentinelInfo.Target
                            Set-LogValue $logs "sentinel_target" $sentinelInfo.Target

                            if (-not $sentinelInfo.Found -and $compileInfo -and $compileInfo.Hits.Count -gt 0 -and $sentinelInfo.Backend -eq "mono" -and $compileInfo.Source -eq "PlayerScriptAssemblies") {
                                $managedDir = Get-ManagedDir $buildDir
                                if ($managedDir) {
                                    $patched = $false
                                    foreach ($hit in $compileInfo.Hits) {
                                        $name = Split-Path -Leaf $hit
                                        if ($name -like "PureDOTS*.dll") {
                                            Copy-Item -Path $hit -Destination (Join-Path $managedDir $name) -Force
                                            $patched = $true
                                        }
                                    }
                                    if ($patched) {
                                        $logs.Add("${prefix}_patched_managed_dlls=1")
                                        $sentinelInfo = Get-PlayerSentinelInfo $buildDir $sentinels
                                        Set-LogValue $logs "${prefix}_sentinel_target" $sentinelInfo.Target
                                        Set-LogValue $logs "sentinel_target" $sentinelInfo.Target
                                    } else {
                                        $logs.Add("${prefix}_patched_managed_dlls=0")
                                    }
                                }
                            }

                            if (-not $sentinelInfo.Found) {
                                $overallStatus = "failed"
                                $errorMessage = "BUILD_MISMATCH_PUREDOTS_CACHE sentinel_missing=probeVersion"
                                break
                            }
                            $logs.Add("${prefix}_sentinel_found=probeVersion")
                        }

                        $extraNotes = ""
                        if ($probeBuild) {
                            $unityNote = Sanitize-NoteValue $unityProjectPath
                            $resolvedNote = Sanitize-NoteValue $resolvedPureDotsPath
                            $extraNotes = "unity_project_path=$unityNote;resolved_puredots_path=$resolvedNote"
                        }
                        $publish = Publish-Build $info.Name $buildDir $info.ExecutableName $requestId $puredotsInfo.Branch $puredotsInfo.Sha $extraNotes
                        $logs.Add("publish_path_" + $info.Name + "=" + $publish.PublishedPath)
                        $publishedPath = $publish.PublishedPath
                        $buildCommit = $publish.BuildCommit
                    } finally {
                        Restore-PureDotsManifestOverride $overrideInfo
                    }
                    if ($overallStatus -ne "ok") {
                        break
                    }
                }
            }
        }
    } catch {
        $overallStatus = "failed"
        $errorMessage = $_.Exception.Message
    } finally {
        if ($desiredCommit -and $originalBranches.Count -gt 0) {
            foreach ($entry in $originalBranches.GetEnumerator()) {
                $projectRoot = Join-Path $env:TRI_ROOT $entry.Key
                $restoreResult = Invoke-Git $projectRoot checkout --quiet $entry.Value
                if ($restoreResult.ExitCode -ne 0 -and $overallStatus -eq "ok") {
                    $overallStatus = "failed"
                    $errorMessage = "git checkout restore failed for $($entry.Key): $($restoreResult.Output)"
                }
                $cleanResult = Clean-Worktree $projectRoot $entry.Key $true
                if (-not $cleanResult.Ok -and $overallStatus -eq "ok") {
                    $overallStatus = "failed"
                    $errorMessage = $cleanResult.Error
                }
            }
        }
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
