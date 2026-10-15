$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$repository = Split-Path $PSScriptRoot -Parent
$scannerDirectory = Join-Path $repository 'artifacts/local-document-scanner'
$databaseDirectory = Join-Path $scannerDirectory 'database'
$packageDirectory = Join-Path $scannerDirectory 'package'
$version = '1.5.4'
# SHA-256 published by the official Cisco-Talos/clamav GitHub release.
$expectedHash = '0d9e0228b2674137ea1a2853566c98a0278ad52ab2582c3d6dbd75373848c395'
$archive = Join-Path $scannerDirectory "clamav-$version.win.x64.zip"

function Test-ScannerReady {
    $client = [System.Net.Sockets.TcpClient]::new()
    try {
        if (!$client.ConnectAsync('127.0.0.1', 3310).Wait(1000)) { return $false }
        $stream = $client.GetStream()
        $stream.ReadTimeout = 1000
        $message = [Text.Encoding]::ASCII.GetBytes("zPING`0")
        $stream.Write($message, 0, $message.Length)
        $reply = [byte[]]::new(32)
        $length = $stream.Read($reply, 0, $reply.Length)
        return [Text.Encoding]::ASCII.GetString($reply, 0, $length).Trim([char]0) -eq 'PONG'
    } catch { return $false }
    finally { $client.Dispose() }
}

$mutex = [System.Threading.Mutex]::new($false, 'Local\NexoraDevDocumentScanner3310')
$locked = $false
try {
try { $locked = $mutex.WaitOne(240000) }
catch [System.Threading.AbandonedMutexException] { $locked = $true }
if (!$locked) { throw 'Timed out waiting for another local scanner startup.' }
if (Test-ScannerReady) { Write-Output 'Local document scanner is ready on 127.0.0.1:3310.'; return }
New-Item -ItemType Directory -Path $scannerDirectory, $databaseDirectory, $packageDirectory -Force | Out-Null
$daemon = Get-ChildItem -LiteralPath $packageDirectory -Filter clamd.exe -Recurse -File | Select-Object -First 1
if (!$daemon) {
    if (!(Test-Path -LiteralPath $archive) -or (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expectedHash) {
        Write-Output "Downloading official ClamAV $version for the local Nexora scanner."
        Invoke-WebRequest -Uri "https://www.clamav.net/downloads/production/clamav-$version.win.x64.zip" -OutFile $archive
    }
    if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expectedHash) {
        throw 'The ClamAV package checksum does not match the official release. Nothing was started.'
    }
    Expand-Archive -LiteralPath $archive -DestinationPath $packageDirectory -Force
    $daemon = Get-ChildItem -LiteralPath $packageDirectory -Filter clamd.exe -Recurse -File | Select-Object -First 1
    if (!$daemon) { throw 'The verified ClamAV package does not contain clamd.exe.' }
}
$freshclam = Join-Path $daemon.DirectoryName 'freshclam.exe'
$daemonConfig = Join-Path $scannerDirectory 'clamd.conf'
$updateConfig = Join-Path $scannerDirectory 'freshclam.conf'
@"
TCPAddr 127.0.0.1
TCPSocket 3310
DatabaseDirectory "$databaseDirectory"
LogFile "$(Join-Path $scannerDirectory 'clamd.log')"
LogTime yes
Foreground yes
MaxThreads 2
StreamMaxLength 6M
AlertExceedsMax yes
"@ | Set-Content -LiteralPath $daemonConfig -Encoding ascii
@"
DatabaseDirectory "$databaseDirectory"
DatabaseMirror database.clamav.net
UpdateLogFile "$(Join-Path $scannerDirectory 'freshclam.log')"
LogTime yes
Checks 6
"@ | Set-Content -LiteralPath $updateConfig -Encoding ascii
if (!(Get-ChildItem -LiteralPath $databaseDirectory -Filter 'daily.*' -File)) {
    Write-Output 'Updating and verifying official antivirus signatures.'
    & $freshclam "--config-file=$updateConfig" --quiet *> (Join-Path $scannerDirectory 'definition-update.log')
    if ($LASTEXITCODE -ne 0) { throw 'Antivirus signatures could not be updated. Image uploads remain blocked.' }
}
$started = Start-Process -FilePath $daemon.FullName -ArgumentList "--config-file=`"$daemonConfig`"" -WorkingDirectory $daemon.DirectoryName -WindowStyle Hidden -PassThru
$deadline = (Get-Date).AddSeconds(120)
while (!(Test-ScannerReady)) {
    if ($started.HasExited) { throw 'The local scanner exited. Inspect artifacts/local-document-scanner/clamd.log.' }
    if ((Get-Date) -gt $deadline) { throw 'The local scanner did not become ready. Inspect artifacts/local-document-scanner/clamd.log.' }
    Start-Sleep -Milliseconds 500
}
$started.Id | Set-Content -LiteralPath (Join-Path $scannerDirectory 'clamd.pid')
$updaterPidFile = Join-Path $scannerDirectory 'freshclam.pid'
$updater = $null
if (Test-Path -LiteralPath $updaterPidFile) {
    $recordedUpdaterId = (Get-Content -LiteralPath $updaterPidFile -Raw).Trim()
    if ($recordedUpdaterId -match '^\d+$') {
        $candidate = Get-CimInstance Win32_Process -Filter "ProcessId=$recordedUpdaterId"
        if ($candidate.ExecutablePath -eq $freshclam -and $candidate.CommandLine.Contains($updateConfig)) { $updater = $candidate }
    }
}
if (!$updater) {
    $startedUpdater = Start-Process -FilePath $freshclam -ArgumentList "--daemon --config-file=`"$updateConfig`"" -WorkingDirectory $daemon.DirectoryName -WindowStyle Hidden -PassThru
    $startedUpdater.Id | Set-Content -LiteralPath $updaterPidFile
}
Write-Output 'Local document scanner is ready on 127.0.0.1:3310.'
} finally {
    if ($locked) { $mutex.ReleaseMutex() }
    $mutex.Dispose()
}
