param([switch]$NoBrowser)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$api = Join-Path $root 'src/Shop.Api'
$client = Join-Path $root 'client'
$children = @()
function Wait-Local($url, $hostName, $process) {
    $deadline = (Get-Date).AddMinutes(3)
    while ((Get-Date) -lt $deadline) {
        if ($process.HasExited) { throw "Startup failed. See logs in src/Shop.Api/obj/local-run." }
        try {
            $response = Invoke-WebRequest $url -Headers @{ Host = $hostName } -UseBasicParsing -TimeoutSec 2
            if ($response.StatusCode -eq 200) { return }
        } catch { }
        Start-Sleep -Milliseconds 500
    }
    throw "Timed out waiting for $url. See logs in src/Shop.Api/obj/local-run."
}
try {
    foreach ($port in @(5000, 4300)) {
        $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, $port)
        try { $listener.Start() } finally { $listener.Stop() }
    }
    if (!(Test-Path (Join-Path $client 'node_modules/@angular/cli/bin/ng.js'))) {
        throw 'Install frontend dependencies first: cd client; npm ci'
    }
    # Persist a private local key; never print it or replace an existing valid key.
    $secretPath = Join-Path $api 'email-secrets.json'
    $secrets = if (Test-Path $secretPath) { Get-Content $secretPath -Raw | ConvertFrom-Json } else { [pscustomobject]@{} }
    if (!$secrets.Jwt) { $secrets | Add-Member NoteProperty Jwt ([pscustomobject]@{}) }
    $key = [string]$secrets.Jwt.SecretKey
    if (!$env:Jwt__SecretKey -and ($key.Length -lt 32 -or $key -match 'CHANGE_ME|REPLACE_WITH|placeholder|dev_only|do_not_use_in_production')) {
        $bytes = New-Object byte[] 48
        $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
        try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
        $secrets.Jwt | Add-Member NoteProperty SecretKey ([Convert]::ToBase64String($bytes)) -Force
        $secrets | ConvertTo-Json -Depth 32 | Set-Content $secretPath -Encoding UTF8
    }
    $logs = Join-Path $api 'obj/local-run'
    New-Item $logs -ItemType Directory -Force | Out-Null
    @{ Id = $PID; Started = (Get-Process -Id $PID).StartTime.ToUniversalTime().Ticks } | ConvertTo-Json | Set-Content (Join-Path $logs 'launcher.json')
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:Logging__LogLevel__Default = 'Information'
    $output = Join-Path $logs 'api-output'
    & dotnet build (Join-Path $api 'Shop.Api.csproj') -o $output > (Join-Path $logs 'build.log') 2>&1
    if ($LASTEXITCODE -ne 0) { throw 'API build failed. See src/Shop.Api/obj/local-run/build.log' }
    $backend = Start-Process dotnet -ArgumentList @(('"' + (Join-Path $output 'Shop.Api.dll') + '"'),'--urls','http://localhost:5000') -WorkingDirectory $api -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $logs 'api.log') -RedirectStandardError (Join-Path $logs 'api-error.log')
    $children += $backend
    $frontend = Start-Process node -ArgumentList @('node_modules/@angular/cli/bin/ng.js','serve','--host','127.0.0.1','--port','4300') -WorkingDirectory $client -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $logs 'ui.log') -RedirectStandardError (Join-Path $logs 'ui-error.log')
    $children += $frontend
    Wait-Local 'http://127.0.0.1:5000/api/settings' 'kids.localhost:5000' $backend
    Wait-Local 'http://127.0.0.1:4300/' 'platform.localhost:4300' $frontend
    Write-Host 'Ready: http://platform.localhost:4300/platform'
    Write-Host 'Store: http://kids.localhost:4300'
    Write-Host 'Keep this process running. Close it to stop the site.'
    if (!$NoBrowser) { Start-Process 'http://platform.localhost:4300/platform' }
    while (!$backend.HasExited -and !$frontend.HasExited) { Start-Sleep -Seconds 1 }
    throw 'A server stopped. See src/Shop.Api/obj/local-run logs.'
} finally {
    foreach ($child in $children) {
        if (!$child.HasExited) { & taskkill.exe /PID $child.Id /T /F | Out-Null }
    }
}

