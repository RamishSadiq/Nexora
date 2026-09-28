param([switch]$WaitForApi)
$ErrorActionPreference = 'Stop'
if (!$WaitForApi) {
    $root = Split-Path $PSScriptRoot -Parent
    $mutex = New-Object System.Threading.Mutex($false, 'Local\NexoraDevApi5097')
    $locked = $false
    try {
        $locked = $mutex.WaitOne(240000)
        if (!$locked) { throw 'Timed out waiting for another API startup.' }
        if (!(Get-NetTCPConnection -State Listen -LocalPort 5097 -ErrorAction SilentlyContinue)) {
            dotnet build "$root/backend/src/Nexora.Api/Nexora.Api.csproj" --configuration LocalDebug -p:DebugSymbols=true -p:DebugType=portable -p:Optimize=false
            if ($LASTEXITCODE -ne 0) { throw 'API build failed.' }
            $config = (Get-Content "$root/.vscode/launch.json" -Raw | ConvertFrom-Json).configurations | Where-Object name -eq 'Nexora shared API'
            foreach ($property in $config.env.PSObject.Properties) { [Environment]::SetEnvironmentVariable($property.Name, $property.Value, 'Process') }
            New-Item -ItemType Directory "$root/artifacts" -Force | Out-Null
            $api = Start-Process dotnet -ArgumentList 'bin/LocalDebug/net10.0/Nexora.Api.dll', '--urls', 'http://localhost:5097' -WorkingDirectory "$root/backend/src/Nexora.Api" -WindowStyle Hidden -RedirectStandardOutput "$root/artifacts/local-shared-api.log" -RedirectStandardError "$root/artifacts/local-shared-api-error.log" -PassThru
            $api.Id | Set-Content "$root/artifacts/local-shared-api.pid"
        }
        & $PSCommandPath -WaitForApi
        if ($LASTEXITCODE -ne 0) { throw 'Shared API readiness failed.' }
    } finally {
        if ($locked) { $mutex.ReleaseMutex() }
        $mutex.Dispose()
    }
    exit 0
}
if ($WaitForApi) {
    # The API has a 30s total readiness budget. A 2s client timeout used to abort
    # LocalDB cold starts inside EF, producing repeated TaskCanceledExceptions.
    $deadline = (Get-Date).AddSeconds(180)
    $lastFailure = 'No readiness response received.'
    do {
        try {
            $response = Invoke-WebRequest 'http://localhost:5097/health/ready' -UseBasicParsing -TimeoutSec 45
            if ($response.StatusCode -eq 200) { Write-Output 'Nexora API is ready.'; exit 0 }
        } catch {
            $lastFailure = $_.Exception.Message
            Write-Output "API not ready yet: $lastFailure"
        }
        Start-Sleep -Seconds 1
    } while ((Get-Date) -lt $deadline)
    throw "API did not become ready on port 5097. Last failure: $lastFailure. Inspect the API Readiness logs for DbContext, server, database and phase. CRM, Portal and CMS share this API. A debugger paused on an exception must be resumed before readiness can finish."
}
