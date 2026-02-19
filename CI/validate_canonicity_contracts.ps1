param(
    [string]$WorkspaceRoot = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Add-Error {
    param(
        [System.Collections.Generic.List[string]]$Errors,
        [string]$Message
    )

    $Errors.Add($Message) | Out-Null
}

function Has-Property {
    param(
        [object]$Object,
        [string]$Name
    )

    if ($null -eq $Object) {
        return $false
    }

    return $null -ne $Object.PSObject.Properties[$Name]
}

function Load-Json {
    param(
        [string]$Path
    )

    $raw = Get-Content -Path $Path -Raw
    return $raw | ConvertFrom-Json
}

function Try-Load-Json {
    param(
        [string]$Path,
        [System.Collections.Generic.List[string]]$Errors
    )

    try {
        return Load-Json -Path $Path
    } catch {
        Add-Error -Errors $Errors -Message ("JSON parse failed: {0} ({1})" -f $Path, $_.Exception.Message)
        return $null
    }
}

function Resolve-RegistryEntryPath {
    param(
        [string]$RawPath,
        [hashtable]$RepoRoots,
        [string]$WorkspaceRoot
    )

    if ([string]::IsNullOrWhiteSpace($RawPath)) {
        return $null
    }

    $normalized = $RawPath -replace "/", "\"
    $parts = $normalized.Split("\", 2)
    if ($parts.Length -eq 2) {
        $prefix = $parts[0]
        $suffix = $parts[1]
        if ($RepoRoots.ContainsKey($prefix)) {
            return Join-Path $RepoRoots[$prefix] $suffix
        }
    }

    return Join-Path $WorkspaceRoot $normalized
}

function Validate-Scenario-Envelope {
    param(
        [object]$Envelope,
        [string]$Path,
        [System.Collections.Generic.List[string]]$Errors,
        [hashtable]$PayloadIndexByContractId = @{}
    )

    if (-not (Has-Property -Object $Envelope -Name "schemaId")) {
        return
    }

    if ($Envelope.schemaId -ne "contract.scenario_envelope.v0") {
        return
    }

    foreach ($required in @("scenarioId", "seed", "duration_s", "contracts")) {
        if (-not (Has-Property -Object $Envelope -Name $required)) {
            Add-Error -Errors $Errors -Message ("Envelope missing required field '{0}': {1}" -f $required, $Path)
        }
    }

    if (Has-Property -Object $Envelope -Name "contracts") {
        $contracts = $Envelope.contracts
        $fieldToContractId = @{
            "mining" = "contract.mining.v0";
            "combat" = "contract.combat.v0";
            "roomProfiles" = "contract.room_profile.v0";
            "missionObjectives" = "contract.mission_objective.v0";
            "lootCaches" = "contract.loot_cache.v0";
            "encounters" = "contract.encounter_profile.v0";
        }

        foreach ($field in $fieldToContractId.Keys) {
            if (-not (Has-Property -Object $contracts -Name $field)) {
                continue
            }

            $items = $contracts.$field
            if ($items -isnot [System.Array]) {
                Add-Error -Errors $Errors -Message ("Envelope contracts.{0} must be an array: {1}" -f $field, $Path)
                continue
            }

            foreach ($item in $items) {
                if ($item -isnot [string] -or [string]::IsNullOrWhiteSpace($item)) {
                    Add-Error -Errors $Errors -Message ("Envelope contracts.{0} contains invalid id: {1}" -f $field, $Path)
                    break
                }

                $expectedContractId = $fieldToContractId[$field]
                if ($PayloadIndexByContractId.ContainsKey($expectedContractId)) {
                    $knownPayloadIds = $PayloadIndexByContractId[$expectedContractId]
                    if (-not $knownPayloadIds.ContainsKey($item)) {
                        Add-Error -Errors $Errors -Message ("Envelope contracts.{0} references unknown payload id '{1}' in {2}" -f $field, $item, $Path)
                    }
                }
            }
        }
    }
}

function Validate-Room-Profile-References {
    param(
        [object]$RoomProfile,
        [string]$Path,
        [System.Collections.Generic.List[string]]$Errors,
        [hashtable]$PayloadIndexByContractId
    )

    if (-not (Has-Property -Object $RoomProfile -Name "contractId")) {
        return
    }

    if ($RoomProfile.contractId -ne "contract.room_profile.v0") {
        return
    }

    if (-not (Has-Property -Object $RoomProfile -Name "contractRefs")) {
        return
    }

    if ($RoomProfile.contractRefs -isnot [System.Array]) {
        Add-Error -Errors $Errors -Message ("Room profile contractRefs must be an array: {0}" -f $Path)
        return
    }

    $refTypeToContractId = @{
        "mining" = "contract.mining.v0";
        "combat" = "contract.combat.v0";
        "mission" = "contract.mission_objective.v0";
        "cache" = "contract.loot_cache.v0";
        "encounter" = "contract.encounter_profile.v0";
    }

    foreach ($contractRef in $RoomProfile.contractRefs) {
        if (-not (Has-Property -Object $contractRef -Name "type") -or -not (Has-Property -Object $contractRef -Name "id")) {
            Add-Error -Errors $Errors -Message ("Room profile contractRef missing type or id: {0}" -f $Path)
            continue
        }

        $refType = [string]$contractRef.type
        $refId = [string]$contractRef.id
        if (-not $refTypeToContractId.ContainsKey($refType)) {
            Add-Error -Errors $Errors -Message ("Room profile contractRef has unsupported type '{0}': {1}" -f $refType, $Path)
            continue
        }

        $expectedContractId = $refTypeToContractId[$refType]
        if ($PayloadIndexByContractId.ContainsKey($expectedContractId)) {
            $knownPayloadIds = $PayloadIndexByContractId[$expectedContractId]
            if (-not $knownPayloadIds.ContainsKey($refId)) {
                Add-Error -Errors $Errors -Message ("Room profile contractRef '{0}' references unknown payload id '{1}' in {2}" -f $refType, $refId, $Path)
            }
        }
    }
}

if ([string]::IsNullOrWhiteSpace($WorkspaceRoot)) {
    $WorkspaceRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
} else {
    $WorkspaceRoot = (Resolve-Path $WorkspaceRoot).Path
}

$hasTriLayout = Test-Path (Join-Path $WorkspaceRoot "puredots\Docs")
$hasPureDotsRepoLayout = Test-Path (Join-Path $WorkspaceRoot "Docs")

if (-not $hasTriLayout -and -not $hasPureDotsRepoLayout) {
    Write-Host ("[ERROR] Could not resolve layout from workspace root: {0}" -f $WorkspaceRoot) -ForegroundColor Red
    exit 1
}

$repoRoots = @{}
if ($hasTriLayout) {
    $repoRoots["puredots"] = Join-Path $WorkspaceRoot "puredots"
    if (Test-Path (Join-Path $WorkspaceRoot "space4x")) {
        $repoRoots["space4x"] = Join-Path $WorkspaceRoot "space4x"
    }
    if (Test-Path (Join-Path $WorkspaceRoot "godgame")) {
        $repoRoots["godgame"] = Join-Path $WorkspaceRoot "godgame"
    }
} else {
    $repoRoots["puredots"] = $WorkspaceRoot
    $parent = Split-Path -Parent $WorkspaceRoot
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        $candidateSpace4x = Join-Path $parent "space4x"
        $candidateGodgame = Join-Path $parent "godgame"
        if (Test-Path $candidateSpace4x) {
            $repoRoots["space4x"] = $candidateSpace4x
        }
        if (Test-Path $candidateGodgame) {
            $repoRoots["godgame"] = $candidateGodgame
        }
    }
}

$strictCrossProject = $hasTriLayout

$errors = New-Object System.Collections.Generic.List[string]
$warnings = New-Object System.Collections.Generic.List[string]
$canonicalContractIds = @{}
$payloadIndexByContractId = @{}
$payloadDocuments = New-Object System.Collections.Generic.List[object]
$canonicalContractRegistryAbsolutePath = Join-Path $repoRoots["puredots"] "Docs\Canonicity\canonical_contracts.v0.json"
if (-not (Test-Path $canonicalContractRegistryAbsolutePath)) {
    Add-Error -Errors $errors -Message ("Missing canonical contract registry: {0}" -f $canonicalContractRegistryAbsolutePath)
} else {
    $contractRegistry = Try-Load-Json -Path $canonicalContractRegistryAbsolutePath -Errors $errors
    if ($null -ne $contractRegistry) {
        if (-not (Has-Property -Object $contractRegistry -Name "entries")) {
            Add-Error -Errors $errors -Message ("Contract registry missing 'entries': {0}" -f $canonicalContractRegistryAbsolutePath)
        } elseif ($contractRegistry.entries -isnot [System.Array]) {
            Add-Error -Errors $errors -Message ("Contract registry 'entries' must be an array: {0}" -f $canonicalContractRegistryAbsolutePath)
        } else {
            $allowedOwners = @("puredots")
            $allowedScopes = @("shared")
            $allowedStatuses = @("draft-active", "scaffold", "planned", "legacy")
            $seenContractCanonicalKeys = @{}
            $seenContractIds = @{}
            $seenContractSchemas = @{}

            for ($i = 0; $i -lt $contractRegistry.entries.Count; $i++) {
                $entry = $contractRegistry.entries[$i]
                $entryPrefix = "contractRegistry.entries[$i]"

                foreach ($required in @("canonicalKey", "contractId", "owner", "scope", "status", "schemaPath", "docPath", "purpose")) {
                    if (-not (Has-Property -Object $entry -Name $required)) {
                        Add-Error -Errors $errors -Message ("{0} missing '{1}'" -f $entryPrefix, $required)
                    }
                }

                if (-not (Has-Property -Object $entry -Name "canonicalKey") -or
                    -not (Has-Property -Object $entry -Name "contractId") -or
                    -not (Has-Property -Object $entry -Name "owner") -or
                    -not (Has-Property -Object $entry -Name "scope") -or
                    -not (Has-Property -Object $entry -Name "status") -or
                    -not (Has-Property -Object $entry -Name "schemaPath") -or
                    -not (Has-Property -Object $entry -Name "docPath")) {
                    continue
                }

                $canonicalKey = [string]$entry.canonicalKey
                $contractId = [string]$entry.contractId
                $owner = [string]$entry.owner
                $scope = [string]$entry.scope
                $status = [string]$entry.status
                $schemaPath = [string]$entry.schemaPath
                $docPath = [string]$entry.docPath

                if ([string]::IsNullOrWhiteSpace($canonicalKey)) {
                    Add-Error -Errors $errors -Message ("{0} canonicalKey must be non-empty" -f $entryPrefix)
                } elseif ($seenContractCanonicalKeys.ContainsKey($canonicalKey)) {
                    Add-Error -Errors $errors -Message ("Duplicate contract canonicalKey '{0}' in {1}" -f $canonicalKey, $canonicalContractRegistryAbsolutePath)
                } else {
                    $seenContractCanonicalKeys[$canonicalKey] = $true
                }

                if ([string]::IsNullOrWhiteSpace($contractId)) {
                    Add-Error -Errors $errors -Message ("{0} contractId must be non-empty" -f $entryPrefix)
                } elseif ($seenContractIds.ContainsKey($contractId)) {
                    Add-Error -Errors $errors -Message ("Duplicate contractId '{0}' in {1}" -f $contractId, $canonicalContractRegistryAbsolutePath)
                } else {
                    $seenContractIds[$contractId] = $true
                    $canonicalContractIds[$contractId] = $true
                }

                if (-not [string]::IsNullOrWhiteSpace($contractId) -and $contractId -notmatch "^contract\.[a-z0-9_\.\-]+\.v[0-9]+$") {
                    Add-Error -Errors $errors -Message ("{0} contractId '{1}' does not match expected pattern 'contract.<name>.vN'" -f $entryPrefix, $contractId)
                }

                if (-not $allowedOwners.Contains($owner)) {
                    Add-Error -Errors $errors -Message ("{0} has unsupported owner '{1}'" -f $entryPrefix, $owner)
                }

                if (-not $allowedScopes.Contains($scope)) {
                    Add-Error -Errors $errors -Message ("{0} has unsupported scope '{1}'" -f $entryPrefix, $scope)
                }

                if (-not $allowedStatuses.Contains($status)) {
                    Add-Error -Errors $errors -Message ("{0} has unsupported status '{1}'" -f $entryPrefix, $status)
                }

                $schemaAbsolutePath = Resolve-RegistryEntryPath -RawPath $schemaPath -RepoRoots $repoRoots -WorkspaceRoot $WorkspaceRoot
                if ([string]::IsNullOrWhiteSpace($schemaAbsolutePath) -or -not (Test-Path $schemaAbsolutePath)) {
                    Add-Error -Errors $errors -Message ("Contract schema path not found: {0}" -f $schemaPath)
                } else {
                    if ($seenContractSchemas.ContainsKey($schemaAbsolutePath)) {
                        Add-Error -Errors $errors -Message ("Duplicate contract schema path '{0}' in {1}" -f $schemaPath, $canonicalContractRegistryAbsolutePath)
                    } else {
                        $seenContractSchemas[$schemaAbsolutePath] = $true
                    }

                    $schemaJson = Try-Load-Json -Path $schemaAbsolutePath -Errors $errors
                    if ($null -ne $schemaJson -and (Has-Property -Object $schemaJson -Name "properties") -and (Has-Property -Object $schemaJson.properties -Name "contractId")) {
                        $contractIdProperty = $schemaJson.properties.contractId
                        if (Has-Property -Object $contractIdProperty -Name "const") {
                            $schemaContractIdConst = [string]$contractIdProperty.const
                            if ($schemaContractIdConst -ne $contractId) {
                                Add-Error -Errors $errors -Message ("ContractId mismatch for {0}: registry='{1}' schema='{2}'" -f $schemaPath, $contractId, $schemaContractIdConst)
                            }
                        } else {
                            $warnings.Add(("Schema does not define properties.contractId.const (treated as envelope-like contract): {0}" -f $schemaPath)) | Out-Null
                        }
                    }
                }

                $docAbsolutePath = Resolve-RegistryEntryPath -RawPath $docPath -RepoRoots $repoRoots -WorkspaceRoot $WorkspaceRoot
                if ([string]::IsNullOrWhiteSpace($docAbsolutePath) -or -not (Test-Path $docAbsolutePath)) {
                    Add-Error -Errors $errors -Message ("Contract doc path not found: {0}" -f $docPath)
                }
            }
        }
    }
}

$payloadRegistryAbsolutePath = Join-Path $repoRoots["puredots"] "Docs\Canonicity\canonical_contract_payloads.v0.json"
if (-not (Test-Path $payloadRegistryAbsolutePath)) {
    Add-Error -Errors $errors -Message ("Missing canonical contract payload registry: {0}" -f $payloadRegistryAbsolutePath)
} else {
    $payloadRegistry = Try-Load-Json -Path $payloadRegistryAbsolutePath -Errors $errors
    if ($null -ne $payloadRegistry) {
        if (-not (Has-Property -Object $payloadRegistry -Name "entries")) {
            Add-Error -Errors $errors -Message ("Contract payload registry missing 'entries': {0}" -f $payloadRegistryAbsolutePath)
        } elseif ($payloadRegistry.entries -isnot [System.Array]) {
            Add-Error -Errors $errors -Message ("Contract payload registry 'entries' must be an array: {0}" -f $payloadRegistryAbsolutePath)
        } else {
            $allowedPayloadStatuses = @("draft-active", "scaffold", "planned", "legacy")
            $seenPayloadIds = @{}
            $seenPayloadPaths = @{}

            for ($i = 0; $i -lt $payloadRegistry.entries.Count; $i++) {
                $entry = $payloadRegistry.entries[$i]
                $entryPrefix = "contractPayloadRegistry.entries[$i]"

                foreach ($required in @("payloadId", "contractId", "status", "path", "purpose")) {
                    if (-not (Has-Property -Object $entry -Name $required)) {
                        Add-Error -Errors $errors -Message ("{0} missing '{1}'" -f $entryPrefix, $required)
                    }
                }

                if (-not (Has-Property -Object $entry -Name "payloadId") -or
                    -not (Has-Property -Object $entry -Name "contractId") -or
                    -not (Has-Property -Object $entry -Name "status") -or
                    -not (Has-Property -Object $entry -Name "path")) {
                    continue
                }

                $payloadId = [string]$entry.payloadId
                $contractId = [string]$entry.contractId
                $status = [string]$entry.status
                $path = [string]$entry.path

                if ([string]::IsNullOrWhiteSpace($payloadId)) {
                    Add-Error -Errors $errors -Message ("{0} payloadId must be non-empty" -f $entryPrefix)
                    continue
                }

                if ($seenPayloadIds.ContainsKey($payloadId)) {
                    Add-Error -Errors $errors -Message ("Duplicate payloadId '{0}' in {1}" -f $payloadId, $payloadRegistryAbsolutePath)
                } else {
                    $seenPayloadIds[$payloadId] = $true
                }

                if ($seenPayloadPaths.ContainsKey($path)) {
                    Add-Error -Errors $errors -Message ("Duplicate payload path '{0}' in {1}" -f $path, $payloadRegistryAbsolutePath)
                } else {
                    $seenPayloadPaths[$path] = $true
                }

                if (-not $allowedPayloadStatuses.Contains($status)) {
                    Add-Error -Errors $errors -Message ("{0} has unsupported status '{1}'" -f $entryPrefix, $status)
                }

                if (-not $canonicalContractIds.ContainsKey($contractId)) {
                    Add-Error -Errors $errors -Message ("{0} references unknown canonical contractId '{1}'" -f $entryPrefix, $contractId)
                }

                $payloadAbsolutePath = Resolve-RegistryEntryPath -RawPath $path -RepoRoots $repoRoots -WorkspaceRoot $WorkspaceRoot
                if ([string]::IsNullOrWhiteSpace($payloadAbsolutePath) -or -not (Test-Path $payloadAbsolutePath)) {
                    Add-Error -Errors $errors -Message ("Contract payload path not found: {0}" -f $path)
                    continue
                }

                $payloadJson = Try-Load-Json -Path $payloadAbsolutePath -Errors $errors
                if ($null -eq $payloadJson) {
                    continue
                }

                if (-not $payloadIndexByContractId.ContainsKey($contractId)) {
                    $payloadIndexByContractId[$contractId] = @{}
                }

                if ($payloadIndexByContractId[$contractId].ContainsKey($payloadId)) {
                    Add-Error -Errors $errors -Message ("Duplicate payloadId '{0}' under contract '{1}'" -f $payloadId, $contractId)
                } else {
                    $payloadIndexByContractId[$contractId][$payloadId] = $path
                }

                if ($contractId -eq "contract.scenario_envelope.v0") {
                    if (-not (Has-Property -Object $payloadJson -Name "schemaId")) {
                        Add-Error -Errors $errors -Message ("Scenario envelope payload missing schemaId: {0}" -f $path)
                    } elseif ([string]$payloadJson.schemaId -ne "contract.scenario_envelope.v0") {
                        Add-Error -Errors $errors -Message ("Scenario envelope schemaId mismatch for {0}: expected 'contract.scenario_envelope.v0'" -f $path)
                    }
                } else {
                    if (-not (Has-Property -Object $payloadJson -Name "contractId")) {
                        Add-Error -Errors $errors -Message ("Contract payload missing contractId: {0}" -f $path)
                    } else {
                        $payloadContractId = [string]$payloadJson.contractId
                        if ($payloadContractId -ne $contractId) {
                            Add-Error -Errors $errors -Message ("Contract payload contractId mismatch for {0}: registry='{1}' file='{2}'" -f $path, $contractId, $payloadContractId)
                        }
                    }
                }

                if ($contractId -eq "contract.scenario_envelope.v0") {
                    if (-not (Has-Property -Object $payloadJson -Name "scenarioId")) {
                        Add-Error -Errors $errors -Message ("Scenario envelope payload missing scenarioId: {0}" -f $path)
                    } else {
                        $payloadScenarioId = [string]$payloadJson.scenarioId
                        if ($payloadScenarioId -ne $payloadId) {
                            Add-Error -Errors $errors -Message ("Scenario payloadId mismatch for {0}: registry='{1}' file='{2}'" -f $path, $payloadId, $payloadScenarioId)
                        }
                    }
                } else {
                    if (-not (Has-Property -Object $payloadJson -Name "id")) {
                        Add-Error -Errors $errors -Message ("Contract payload missing id: {0}" -f $path)
                    } else {
                        $payloadDocumentId = [string]$payloadJson.id
                        if ($payloadDocumentId -ne $payloadId) {
                            Add-Error -Errors $errors -Message ("PayloadId mismatch for {0}: registry='{1}' file='{2}'" -f $path, $payloadId, $payloadDocumentId)
                        }
                    }
                }

                $payloadDocuments.Add(
                    @{
                        "contractId" = $contractId;
                        "payloadId" = $payloadId;
                        "path" = $path;
                        "json" = $payloadJson
                    }
                ) | Out-Null
            }
        }
    }
}

foreach ($payloadDocument in $payloadDocuments) {
    $contractId = [string]$payloadDocument.contractId
    $path = [string]$payloadDocument.path
    $json = $payloadDocument.json

    if ($contractId -eq "contract.scenario_envelope.v0") {
        Validate-Scenario-Envelope -Envelope $json -Path $path -Errors $errors -PayloadIndexByContractId $payloadIndexByContractId
    }

    if ($contractId -eq "contract.room_profile.v0") {
        Validate-Room-Profile-References -RoomProfile $json -Path $path -Errors $errors -PayloadIndexByContractId $payloadIndexByContractId
    }

    if ($contractId -eq "contract.loot_cache.v0" -and (Has-Property -Object $json -Name "triggerPolicy")) {
        $triggerPolicy = $json.triggerPolicy
        if (Has-Property -Object $triggerPolicy -Name "spawnEncounterId") {
            $encounterId = [string]$triggerPolicy.spawnEncounterId
            if (-not [string]::IsNullOrWhiteSpace($encounterId) -and $payloadIndexByContractId.ContainsKey("contract.encounter_profile.v0")) {
                if (-not $payloadIndexByContractId["contract.encounter_profile.v0"].ContainsKey($encounterId)) {
                    Add-Error -Errors $errors -Message ("Loot cache triggerPolicy.spawnEncounterId references unknown encounter payload '{0}' in {1}" -f $encounterId, $path)
                }
            }
        }
    }
}

$registryAbsolutePath = Join-Path $repoRoots["puredots"] "Docs\Canonicity\canonical_scenarios.v0.json"
if (-not (Test-Path $registryAbsolutePath)) {
    Add-Error -Errors $errors -Message ("Missing canonical scenario registry: {0}" -f $registryAbsolutePath)
} else {
    $registry = Try-Load-Json -Path $registryAbsolutePath -Errors $errors

    if ($null -ne $registry) {
        if (-not (Has-Property -Object $registry -Name "entries")) {
            Add-Error -Errors $errors -Message ("Registry missing 'entries': {0}" -f $registryAbsolutePath)
        } elseif ($registry.entries -isnot [System.Array]) {
            Add-Error -Errors $errors -Message ("Registry 'entries' must be an array: {0}" -f $registryAbsolutePath)
        } else {
            $seenCanonicalKeys = @{}
            $seenPaths = @{}
            $seenProjectScenario = @{}
            $allowedProjects = @("space4x", "godgame", "puredots")

            for ($i = 0; $i -lt $registry.entries.Count; $i++) {
                $entry = $registry.entries[$i]
                $entryPrefix = "registry.entries[$i]"

                foreach ($required in @("canonicalKey", "project", "scenarioId", "path", "tier", "purpose")) {
                    if (-not (Has-Property -Object $entry -Name $required)) {
                        Add-Error -Errors $errors -Message ("{0} missing '{1}'" -f $entryPrefix, $required)
                    }
                }

                if (-not (Has-Property -Object $entry -Name "canonicalKey") -or
                    -not (Has-Property -Object $entry -Name "project") -or
                    -not (Has-Property -Object $entry -Name "scenarioId") -or
                    -not (Has-Property -Object $entry -Name "path")) {
                    continue
                }

                $canonicalKey = [string]$entry.canonicalKey
                $project = [string]$entry.project
                $scenarioId = [string]$entry.scenarioId
                $path = [string]$entry.path

                if ([string]::IsNullOrWhiteSpace($canonicalKey)) {
                    Add-Error -Errors $errors -Message ("{0} canonicalKey must be non-empty" -f $entryPrefix)
                } elseif ($seenCanonicalKeys.ContainsKey($canonicalKey)) {
                    Add-Error -Errors $errors -Message ("Duplicate canonicalKey '{0}' in {1}" -f $canonicalKey, $registryAbsolutePath)
                } else {
                    $seenCanonicalKeys[$canonicalKey] = $true
                }

                if (-not $allowedProjects.Contains($project)) {
                    Add-Error -Errors $errors -Message ("{0} has unknown project '{1}'" -f $entryPrefix, $project)
                }

                if ([string]::IsNullOrWhiteSpace($path)) {
                    Add-Error -Errors $errors -Message ("{0} path must be non-empty" -f $entryPrefix)
                } elseif ($seenPaths.ContainsKey($path)) {
                    Add-Error -Errors $errors -Message ("Duplicate registry path '{0}' in {1}" -f $path, $registryAbsolutePath)
                } else {
                    $seenPaths[$path] = $true
                }

                $projectScenarioKey = "{0}|{1}" -f $project, $scenarioId
                if ($seenProjectScenario.ContainsKey($projectScenarioKey)) {
                    Add-Error -Errors $errors -Message ("Duplicate (project, scenarioId)=({0}, {1}) in {2}" -f $project, $scenarioId, $registryAbsolutePath)
                } else {
                    $seenProjectScenario[$projectScenarioKey] = $true
                }

                $scenarioAbsolutePath = Resolve-RegistryEntryPath -RawPath $path -RepoRoots $repoRoots -WorkspaceRoot $WorkspaceRoot
                if (-not (Test-Path $scenarioAbsolutePath)) {
                    $expectedProjectRoot = $null
                    if ($repoRoots.ContainsKey($project)) {
                        $expectedProjectRoot = $repoRoots[$project]
                    }

                    if (-not $strictCrossProject -and $project -ne "puredots" -and $null -eq $expectedProjectRoot) {
                        $warnings.Add(("Skipping registry path for missing sibling repo '{0}': {1}" -f $project, $path)) | Out-Null
                    } else {
                        Add-Error -Errors $errors -Message ("Registry path not found: {0}" -f $path)
                    }
                    continue
                }

                $scenarioJson = Try-Load-Json -Path $scenarioAbsolutePath -Errors $errors
                if ($null -eq $scenarioJson) {
                    continue
                }

                if (-not (Has-Property -Object $scenarioJson -Name "scenarioId")) {
                    Add-Error -Errors $errors -Message ("Scenario missing scenarioId: {0}" -f $path)
                } else {
                    $scenarioScenarioId = [string]$scenarioJson.scenarioId
                    if ($scenarioScenarioId -ne $scenarioId) {
                        Add-Error -Errors $errors -Message ("ScenarioId mismatch for {0}: registry='{1}' file='{2}'" -f $path, $scenarioId, $scenarioScenarioId)
                    }
                }

                Validate-Scenario-Envelope -Envelope $scenarioJson -Path $path -Errors $errors -PayloadIndexByContractId $payloadIndexByContractId
            }
        }
    }
}

$scenarioRoots = @(
    @{
        Project = "space4x";
        RelativeRoot = if ($repoRoots.ContainsKey("space4x")) { Join-Path $repoRoots["space4x"] "Assets\Scenarios" } else { $null };
        ExcludeTemplates = $true;
    },
    @{
        Project = "godgame";
        RelativeRoot = if ($repoRoots.ContainsKey("godgame")) { Join-Path $repoRoots["godgame"] "Assets\Scenarios" } else { $null };
        ExcludeTemplates = $false;
    },
    @{
        Project = "puredots";
        RelativeRoot = Join-Path $repoRoots["puredots"] "Assets\Scenarios";
        ExcludeTemplates = $false;
    }
)

foreach ($scenarioRoot in $scenarioRoots) {
    $project = $scenarioRoot.Project
    $rootAbsolute = $scenarioRoot.RelativeRoot
    if ([string]::IsNullOrWhiteSpace($rootAbsolute) -or -not (Test-Path $rootAbsolute)) {
        $warnings.Add(("Skipping missing scenario root for '{0}'." -f $project)) | Out-Null
        continue
    }

    $files = Get-ChildItem -Path $rootAbsolute -Filter *.json -Recurse
    if ($scenarioRoot.ExcludeTemplates) {
        $files = $files | Where-Object {
            $_.FullName -notmatch "\\Templates\\" -and $_.Name -ne "README.json"
        }
    }

    $scenarioIdOwners = @{}
    foreach ($file in $files) {
        $fileRelative = $file.FullName.Substring($WorkspaceRoot.Length + 1)
        $json = Try-Load-Json -Path $file.FullName -Errors $errors
        if ($null -eq $json) {
            continue
        }

        if (-not (Has-Property -Object $json -Name "scenarioId")) {
            Add-Error -Errors $errors -Message ("[{0}] missing scenarioId: {1}" -f $project, $fileRelative)
            continue
        }

        $scenarioId = [string]$json.scenarioId
        if ([string]::IsNullOrWhiteSpace($scenarioId)) {
            Add-Error -Errors $errors -Message ("[{0}] empty scenarioId: {1}" -f $project, $fileRelative)
            continue
        }

        if ($scenarioIdOwners.ContainsKey($scenarioId)) {
            Add-Error -Errors $errors -Message ("[{0}] duplicate scenarioId '{1}': {2} and {3}" -f $project, $scenarioId, $scenarioIdOwners[$scenarioId], $fileRelative)
        } else {
            $scenarioIdOwners[$scenarioId] = $fileRelative
        }

        Validate-Scenario-Envelope -Envelope $json -Path $fileRelative -Errors $errors -PayloadIndexByContractId $payloadIndexByContractId
    }
}

if ($warnings.Count -gt 0) {
    foreach ($warning in $warnings) {
        Write-Host ("[WARN] {0}" -f $warning) -ForegroundColor Yellow
    }
}

if ($errors.Count -gt 0) {
    foreach ($errorMessage in $errors) {
        Write-Host ("[ERROR] {0}" -f $errorMessage) -ForegroundColor Red
    }

    Write-Host ("Validation failed with {0} error(s)." -f $errors.Count) -ForegroundColor Red
    exit 1
}

Write-Host "Canonicity contract validation passed." -ForegroundColor Green
exit 0
