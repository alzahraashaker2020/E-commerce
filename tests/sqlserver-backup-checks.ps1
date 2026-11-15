# Run through security-smoke.ps1 -SqlServerTests. Only disposable fixture databases are used.
. (Join-Path $PSScriptRoot 'checkout-integrity-checks.ps1')
$login=Request POST '/api/platform/auth/login' '' @{username='platform-test';password=$envValues.Platform__BootstrapPassword}
ExpectStatus $login 200 'SQL Server platform sign in'
$platformToken=($login.Content|ConvertFrom-Json).data.token
function BackupRequest($path) {
    $response=Invoke-WebRequest -Uri ($base+'/api/platform/operations/'+$path) -Method POST -Headers @{Authorization=('Bearer '+$platformToken)} -ContentType 'application/json' -Body '{}' -SkipHttpErrorCheck -TimeoutSec 120
    ExpectStatus $response 200 $path
    return ($response.Content|ConvertFrom-Json).data
}
foreach($store in @('default','platform')) {
    $snapshot=BackupRequest ('backups/'+$store)
    $manifestPath=Join-Path $runDir ('store-data/operations/backups/'+$snapshot.id+'/manifest.json')
    $manifest=Get-Content $manifestPath -Raw|ConvertFrom-Json
    ExpectEqual $manifest.provider 'SqlServer' 'Native SQL Server provider recorded'
    ExpectEqual $manifest.databaseFile $(if($SharedSqlServerTests -and $store-ne'platform'){'database.schema.json'}else{'database.bak'}) 'Native backup payload recorded'
    $verified=BackupRequest ('backups/'+$snapshot.id+'/verify')
    ExpectEqual $verified.lastVerificationState 'Passed' 'Actual isolated restore and CHECKDB pass'
    ExpectEqual ($verified.tables -gt 0) $true 'Restored tables are readable'
}
Write-Output 'SQL Server merchant and platform native backup, actual isolated restore and integrity checks passed.'
