param([string]$SqlServerConnection)
$ErrorActionPreference="Stop"
$repo=Split-Path $PSScriptRoot -Parent
$previousSql=$env:NEXORA_TEST_SQLSERVER
Push-Location $repo
try {
    if($SqlServerConnection){$env:NEXORA_TEST_SQLSERVER=$SqlServerConnection}
    dotnet test backend/Nexora.slnx --configuration Release
    if($LASTEXITCODE -ne 0){throw "Backend checks failed"}
    npm --prefix apps/web run lint
    if($LASTEXITCODE -ne 0){throw "Web lint failed"}
    npm --prefix apps/web run build
    if($LASTEXITCODE -ne 0){throw "Web build failed"}
    git diff --check
    if($LASTEXITCODE -ne 0){throw "Diff whitespace checks failed"}
} finally {$env:NEXORA_TEST_SQLSERVER=$previousSql;Pop-Location}
