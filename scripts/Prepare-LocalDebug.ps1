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
        if ($listeners.Count) {
            $sourceFiles = Get-ChildItem "$root/backend/src" -Recurse -File | Where-Object {
                $_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and $_.Extension -in '.cs', '.csproj', '.props', '.targets', '.json'
            }
            $latestSource = $sourceFiles | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
            $ownerIds = @($listeners.OwningProcess | Select-Object -Unique)
            $recordedApiId = if (Test-Path "$root/artifacts/local-shared-api.pid") { (Get-Content "$root/artifacts/local-shared-api.pid" -Raw).Trim() } else { '' }
            foreach ($ownerId in $ownerIds) {
                $owner = Get-CimInstance Win32_Process -Filter "ProcessId=$ownerId"
                if ($owner -and $latestSource -and $latestSource.LastWriteTimeUtc -gt $owner.CreationDate.ToUniversalTime()) {
                    if ($recordedApiId -ne [string]$ownerId -or $owner.CommandLine -notmatch 'bin[/\\]LocalDebug[/\\]net10\.0[/\\]Nexora\.Api\.dll') {
                        throw "The API on 5097 predates backend changes and was started outside this preparation task. Stop that API debug session, then launch CRM, Portal or CMS again."
                    }
                    Write-Output 'Backend changes detected. Rebuilding the shared local API.'
                    Stop-Process -Id $ownerId
                    Wait-Process -Id $ownerId -Timeout 15 -ErrorAction SilentlyContinue
                    $listeners = @()
                }
            }
        }
        if (!$listeners.Count) {
            dotnet build "$root/backend/src/Nexora.Api/Nexora.Api.csproj" --configuration LocalDebug -m:1 -nr:false -p:UseSharedCompilation=false -p:DebugSymbols=true -p:DebugType=portable -p:Optimize=false
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
