param([switch]$WaitForApi)
$ErrorActionPreference = 'Stop'
if ($WaitForApi) {
    # The API has a 30s total readiness budget. A 2s client timeout used to abort
    # LocalDB cold starts inside EF, producing repeated TaskCanceledExceptions.
    $deadline = (Get-Date).AddSeconds(180)
    $lastFailure = 'No readiness response received.'
    do {
        try {
            $response = Invoke-WebRequest 'http://localhost:5097/health/ready' -UseBasicParsing -TimeoutSec 45
            if ($response.StatusCode -eq 200) { Write-Output 'Website API is ready.'; exit 0 }
        } catch {
            $lastFailure = $_.Exception.Message
            Write-Output "API not ready yet: $lastFailure"
        }
        Start-Sleep -Seconds 1
    } while ((Get-Date) -lt $deadline)
    throw "API did not become ready on port 5097. Last failure: $lastFailure. Inspect the API Readiness logs for DbContext, server, database and phase. Both Website and Portal compounds use this API. A debugger paused on an exception must be resumed before readiness can finish."
}
foreach ($port in @(3017, 5097)) {
    if (Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction SilentlyContinue) {
        throw "Port $port is already occupied. Stop the previous website debug session before pressing F5. No process was stopped."
    }
}
Write-Output 'Website debug ports are available.'
