# Nightly CI script for scenario benchmark generation
# Generates scenarios with scaling parameters (10K → 1M entities)

param(
    [int]$EntityCount = 10000,
    [string]$OutputPath = "Scenarios/Benchmark_$EntityCount.json"
)

Write-Host "Generating scenario with $EntityCount entities..."

# Run Unity in batch mode to generate scenario
Unity -batchmode -nographics `
    -executeMethod PureDOTS.Runtime.Scenario.ScenarioGeneratorSystem.Generate `
    -entityCount $EntityCount `
    -outputPath $OutputPath `
    -quit

Write-Host "Scenario generated: $OutputPath"

