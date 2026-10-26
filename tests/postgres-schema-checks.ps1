param([Parameter(Mandatory=$true)][string]$PostgresBin, [switch]$ApiIsolation, [string]$AdditionalChecks)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$PostgresBin = (Resolve-Path $PostgresBin).Path
$cluster = Join-Path $repo ('src/Shop.Api/obj/postgres-check-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $cluster | Out-Null
$secret = [guid]::NewGuid().ToString('N')
$passfile = Join-Path $cluster 'password.txt'
Set-Content -LiteralPath $passfile -Value $secret -NoNewline
& "$PostgresBin/initdb.exe" -D "$cluster/data" --username=shop_test "--pwfile=$passfile" --auth=scram-sha-256 --encoding=UTF8 --locale=C | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL initialization failed' }
$listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
$listener.Start(); $port = $listener.LocalEndpoint.Port; $listener.Stop()
$oldConnection = $env:SHOP_TEST_POSTGRES
try {
    $pg = Start-Process -FilePath "$PostgresBin/pg_ctl.exe" -ArgumentList @('-D', "`"$cluster/data`"", '-l', "`"$cluster/server.log`"", '-o', "`"-h 127.0.0.1 -p $port`"", '-w', 'start') -WindowStyle Hidden -PassThru
    if (!$pg.WaitForExit(60000) -or $pg.ExitCode -ne 0) { throw 'PostgreSQL startup failed' }
    $env:SHOP_TEST_POSTGRES = "Host=127.0.0.1;Port=$port;Database=postgres;Username=shop_test;Password=$secret"
    & dotnet run --project "$repo/tests/Shop.SchemaChecks/Shop.SchemaChecks.csproj" --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false -- --postgres
    if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL schema checks failed' }
    if ($ApiIsolation) {
        if (!$AdditionalChecks) { $AdditionalChecks = "$repo/tests/tenant-isolation-checks.ps1" }
        & pwsh -NoProfile -File "$repo/tests/security-smoke.ps1" -ApiDll "$repo/src/Shop.Api/obj/verification-output/Shop.Api.dll" -AdditionalChecks $AdditionalChecks -PostgresTenants
        if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL tenant API isolation checks failed' }
    }
} finally {
    if (Test-Path "$cluster/data/postmaster.pid") { & "$PostgresBin/pg_ctl.exe" -D "$cluster/data" -m fast -w stop }
    $env:SHOP_TEST_POSTGRES = $oldConnection
    Remove-Item -LiteralPath $passfile -ErrorAction SilentlyContinue
}
