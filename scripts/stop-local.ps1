$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$marker = Join-Path $root 'src/Shop.Api/obj/local-run/launcher.json'
if (!(Test-Path $marker)) { Write-Host 'No local launcher recorded.'; return }
$record = Get-Content $marker -Raw | ConvertFrom-Json
$process = Get-Process -Id $record.Id -ErrorAction SilentlyContinue
if ($process -and $process.StartTime.ToUniversalTime().Ticks -eq $record.Started) {
    & taskkill.exe /PID $process.Id /T /F
} else { Write-Host 'The recorded launcher is no longer running.' }
