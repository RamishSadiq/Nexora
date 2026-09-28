param([switch]$WaitForApi)
& "$PSScriptRoot/Prepare-LocalDebug.ps1" -WaitForApi:$WaitForApi
exit $LASTEXITCODE
