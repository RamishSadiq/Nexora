param([string]$NodePath = "node")
$ErrorActionPreference = "Stop"
$repo = Split-Path $PSScriptRoot -Parent
$logs = Join-Path $repo "artifacts/local-runtime"
New-Item -ItemType Directory -Force -Path $logs | Out-Null
foreach ($port in @(3000, 5080)) {
    if (Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction SilentlyContinue) {
        throw "Port $port is already in use. Stop the existing Nexora instance before starting the combined build."
    }
}
$apiDll = Join-Path $repo "backend/src/Nexora.Api/bin/Release/net10.0/Nexora.Api.dll"
if (!(Test-Path -LiteralPath $apiDll)) { throw "Build the backend in Release first." }
if (!(Test-Path -LiteralPath (Join-Path $repo "apps/web/.next/BUILD_ID"))) { throw "Build the web application first." }
$previousEnvironment = $env:ASPNETCORE_ENVIRONMENT
$previousDotnet = $env:DOTNET_ENVIRONMENT
$previousApi = $env:NEXORA_API_URL
$previousSeed = $env:Identity__SeedAdminPassword
try {
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    $env:DOTNET_ENVIRONMENT = "Development"
    $env:NEXORA_API_URL = "http://localhost:5080"
    $env:Identity__SeedAdminPassword = $null
    $quotedApiDll = '"' + $apiDll + '"'
    $api = Start-Process dotnet -ArgumentList @($quotedApiDll, '--urls', 'http://localhost:5080') -WorkingDirectory (Join-Path $repo "backend/src/Nexora.Api") -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $logs "api.log") -RedirectStandardError (Join-Path $logs "api-error.log")
    $web = Start-Process $NodePath -ArgumentList @('node_modules/next/dist/bin/next', 'start', '--port', '3000') -WorkingDirectory (Join-Path $repo "apps/web") -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $logs "web.log") -RedirectStandardError (Join-Path $logs "web-error.log")
    @{ apiProcessId = $api.Id; webProcessId = $web.Id; startedAt = (Get-Date).ToString("o"); url = "http://localhost:3000/membership" } | ConvertTo-Json | Set-Content (Join-Path $logs "processes.json")
    Write-Output "Local Nexora started: http://localhost:3000/membership (API PID $($api.Id), web PID $($web.Id)). Check /health/ready before testing."
} finally {
    $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment
    $env:DOTNET_ENVIRONMENT = $previousDotnet
    $env:NEXORA_API_URL = $previousApi
    $env:Identity__SeedAdminPassword = $previousSeed
}

