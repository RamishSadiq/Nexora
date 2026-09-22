param([string]$NodePath = "node", [switch]$Portal)
$ErrorActionPreference = "Stop"
$repo = Split-Path $PSScriptRoot -Parent
$runtime = Join-Path $repo $(if ($Portal) { 'artifacts/portal-local' } else { 'artifacts/website-local' })
$apiPort = if ($Portal) { 5098 } else { 5096 }
$webPort = if ($Portal) { 3018 } else { 3016 }
$configuration = if ($Portal) { 'Portal' } else { 'Website' }
$entry = if ($Portal) { 'portal' } else { 'website' }
$webRoot = Join-Path $runtime 'web'
foreach ($port in @($apiPort, $webPort)) {
    if (Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction SilentlyContinue) { throw "Port $port is occupied; existing processes were left alone." }
}
New-Item -ItemType Directory -Path $webRoot -Force | Out-Null
foreach ($name in @('src','public','package.json','package-lock.json','tsconfig.json','next-env.d.ts','next.config.ts','postcss.config.mjs')) {
    $source = Join-Path $repo "apps/web/$name"
    if (Test-Path -LiteralPath $source) { Copy-Item -LiteralPath $source -Destination $webRoot -Recurse -Force }
}
if (!(Test-Path -LiteralPath (Join-Path $webRoot 'node_modules'))) {
    New-Item -ItemType Junction -Path (Join-Path $webRoot 'node_modules') -Target (Join-Path $repo 'apps/web/node_modules') | Out-Null
}
$previousApi = $env:NEXORA_API_URL
$previousAsp = $env:ASPNETCORE_ENVIRONMENT
$previousDotnet = $env:DOTNET_ENVIRONMENT
$previousSeed = $env:Identity__SeedAdminPassword
$previousOrigin = $env:Web__AllowedOrigins__0
try {
    $env:NEXORA_API_URL = "http://localhost:$apiPort"
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:DOTNET_ENVIRONMENT = 'Development'
    $env:Identity__SeedAdminPassword = $null
    $env:Web__AllowedOrigins__0 = "http://localhost:$webPort"
    $api = Start-Process dotnet -ArgumentList @("bin/$configuration/net10.0/Nexora.Api.dll",'--urls',"http://localhost:$apiPort") -WorkingDirectory (Join-Path $repo 'backend/src/Nexora.Api') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $runtime 'api.log') -RedirectStandardError (Join-Path $runtime 'api-error.log')
    $web = Start-Process $NodePath -ArgumentList @('node_modules/next/dist/bin/next','dev','--webpack','--port',"$webPort") -WorkingDirectory $webRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $runtime 'web.log') -RedirectStandardError (Join-Path $runtime 'web-error.log')
    @{apiProcessId=$api.Id;webProcessId=$web.Id;url="http://localhost:$webPort/$entry"} | ConvertTo-Json | Set-Content (Join-Path $runtime 'processes.json')
    Write-Output "Local preview: http://localhost:$webPort/$entry. Existing development/debug sessions were left alone."
} finally {
    $env:NEXORA_API_URL = $previousApi
    $env:ASPNETCORE_ENVIRONMENT = $previousAsp
    $env:DOTNET_ENVIRONMENT = $previousDotnet
    $env:Identity__SeedAdminPassword = $previousSeed
    $env:Web__AllowedOrigins__0 = $previousOrigin
}
