param([switch]$WaitForApi)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
function Wait-NexoraApi {
    param($StartedProcess)
    $deadline = (Get-Date).AddSeconds(180)
    $lastFailure = 'No readiness response received.'
    do {
        if ($StartedProcess -and $StartedProcess.HasExited) {
            throw 'The shared API exited during startup. Inspect artifacts/local-shared-api.log and local-shared-api-error.log.'
        }
        try {
            $response = Invoke-WebRequest 'http://localhost:5097/health/ready' -UseBasicParsing -TimeoutSec 45
            if ($response.StatusCode -eq 200) { Write-Output 'Nexora API is ready.'; return }
        } catch { $lastFailure = $_.Exception.Message }
        Start-Sleep -Seconds 1
    } while ((Get-Date) -lt $deadline)
    throw "API did not become ready on 5097: $lastFailure. Inspect API logs and resume any paused API debugger."
}
if ($WaitForApi) { Wait-NexoraApi; exit 0 }
if (!$WaitForApi) {
    $mutex = New-Object System.Threading.Mutex($false, 'Local\NexoraDevApi5097')
    $locked = $false
    try {
        try { $locked = $mutex.WaitOne(240000) }
        catch [System.Threading.AbandonedMutexException] { $locked = $true }
        if (!$locked) { throw 'Timed out waiting for another API startup.' }
        $listeners = @(Get-NetTCPConnection -State Listen -LocalPort 5097 -ErrorAction SilentlyContinue)
        foreach ($listener in $listeners) {
            $owner = Get-CimInstance Win32_Process -Filter "ProcessId=$($listener.OwningProcess)"
            if (!$owner -or $owner.CommandLine -notmatch 'Nexora\.Api(?:\.dll|\.exe)') {
                throw "Port 5097 belongs to another process (PID $($listener.OwningProcess)); it was not stopped."
            }
        }
        if (!$listeners.Count) {
            dotnet build "$root/backend/src/Nexora.Api/Nexora.Api.csproj" --configuration LocalDebug -p:DebugSymbols=true -p:DebugType=portable -p:Optimize=false
            if ($LASTEXITCODE -ne 0) { throw 'API build failed.' }
            $config = (Get-Content "$root/.vscode/launch.json" -Raw | ConvertFrom-Json).configurations | Where-Object name -eq 'Nexora shared API'
            foreach ($property in $config.env.PSObject.Properties) { [Environment]::SetEnvironmentVariable($property.Name, $property.Value, 'Process') }
            New-Item -ItemType Directory "$root/artifacts" -Force | Out-Null
            $api = Start-Process dotnet -ArgumentList 'bin/LocalDebug/net10.0/Nexora.Api.dll', '--urls', 'http://localhost:5097' -WorkingDirectory "$root/backend/src/Nexora.Api" -WindowStyle Hidden -RedirectStandardOutput "$root/artifacts/local-shared-api.log" -RedirectStandardError "$root/artifacts/local-shared-api-error.log" -PassThru
            $api.Id | Set-Content "$root/artifacts/local-shared-api.pid"
        }
        Wait-NexoraApi -StartedProcess $api
    } finally {
        if ($locked) { $mutex.ReleaseMutex() }
        $mutex.Dispose()
    }
    exit 0
}
