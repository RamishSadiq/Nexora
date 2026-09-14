param([string]$OutputDirectory = "artifacts/migrations")
$ErrorActionPreference = "Stop"
$repo = Split-Path $PSScriptRoot -Parent
Push-Location $repo
try {
    New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw "Tool restore failed" }
    dotnet build backend/Nexora.slnx --configuration Release
    if ($LASTEXITCODE -ne 0) { throw "Build failed" }
    $contexts = [ordered]@{Identity="NexoraIdentityDbContext"; Crm="CrmDbContext"; Membership="MembershipDbContext"; Events="EventsDbContext"; Finance="FinanceDbContext"; Engagement="EngagementDbContext"; Work="WorkDbContext"}
    foreach ($module in $contexts.Keys) {
        dotnet ef migrations script --idempotent --configuration Release --no-build --project "backend/src/Modules/Nexora.Modules.$module" --startup-project backend/src/Nexora.Api --context $contexts[$module] --output (Join-Path $OutputDirectory "$module.sql")
        if ($LASTEXITCODE -ne 0) { throw "Migration export failed for $module" }
    }
    Get-ChildItem -LiteralPath $OutputDirectory -Filter *.sql | Get-FileHash -Algorithm SHA256 | Select-Object Hash,Path | ConvertTo-Json | Set-Content (Join-Path $OutputDirectory "manifest.json")
} finally { Pop-Location }
