. (Join-Path $PSScriptRoot 'checkout-integrity-checks.ps1')
$login=Request POST '/api/platform/auth/login' '' @{username=$envValues.Platform__BootstrapUsername;password=$envValues.Platform__BootstrapPassword}
ExpectStatus $login 200 'Platform login for operations'
$platformToken=($login.Content|ConvertFrom-Json).data.token
function Ops($method,$path,$body=$null,$expected=200){$r=Request $method ('/api/platform/operations/'+$path) $platformToken $body;if($r.StatusCode -ne $expected){$diagnostic=Request GET '/api/platform/operations/events?errorsOnly=true' $platformToken;Write-Host $diagnostic.Content};ExpectStatus $r $expected "$method operations/$path";if($expected -eq 200){return ($r.Content|ConvertFrom-Json).data}}
ExpectStatus (Request GET '/api/platform/operations/backups' $adminToken) 401 'Merchant token cannot enter platform scope'
ExpectStatus (Request POST '/api/platform/operations/backups/default' $customerToken) 401 'Customer token cannot enter platform scope'
ExpectStatus (Request GET '/api/platform/operations/events') 401 'Anonymous audit access denied'
$before=@(JsonRequest GET '/api/merchant/products').Count
ExpectStatus (Request POST '/api/account/forgot-password' '' @{email='jordan@demo.local'}) 200 'Prepare account action before snapshot'
$oldAction=''
for($wait=0;$wait -lt 100;$wait++){
    foreach($file in Get-ChildItem (Join-Path $runDir 'store-data/default/sent-emails') -Filter 'message-*.txt'){$text=[IO.File]::ReadAllText($file.FullName);if($text.Contains('To: jordan@demo.local') -and $text.Contains('Subject: Reset your store password')){$oldAction=[regex]::Match($text,'#token=([A-Za-z0-9_-]{43})').Groups[1].Value;break}}
    if($oldAction){break};Start-Sleep -Milliseconds 100
}
ExpectEqual $oldAction.Length 43 'Snapshot contains an unused account recovery action'
$backup=Ops POST 'backups/default'
ExpectEqual ($backup.bytes -gt 0) $true 'Backup has actual database bytes'
ExpectEqual ($backup.fileCount -ge 2) $true 'Backup includes assets and encryption keys'
$afterBackupProduct=JsonRequest POST '/api/merchant/products' @{name='Product after backup';categoryId=$category.id;basePrice=10;stockQuantity=3;isActive=$true;hasVariants=$false;showStockQuantity=$true}
$verified=Ops POST ("backups/$($backup.id)/verify")
ExpectEqual $verified.products $before 'Isolated restore retains snapshot product count'
ExpectEqual @(JsonRequest GET '/api/merchant/products').Count ($before+1) 'Restore rehearsal does not overwrite live products'
ExpectEqual ([string]::IsNullOrEmpty($verified.lastVerifiedAtUtc)) $false 'Successful restore check is recorded'
$snapshotRoot=Join-Path $runDir "store-data/operations/backups/$($backup.id)"
$manifest=Get-Content (Join-Path $snapshotRoot 'manifest.json') -Raw|ConvertFrom-Json
foreach($file in $manifest.files|Where-Object path -Like 'uploads/*'){
    $restored=Join-Path $runDir "store-data/operations/restore-checks/$($verified.restoreCheckId)/$($file.path)"
    ExpectEqual (Get-FileHash -LiteralPath $restored -Algorithm SHA256).Hash $file.sha256 'Restored image hash matches snapshot'
}
$registry=Ops POST 'backups/platform';$registryCheck=Ops POST ("backups/$($registry.id)/verify")
ExpectEqual ($registryCheck.tables -gt 0) $true 'Central registry can also be restored separately'
$null=Ops POST 'backups/unknown-store' $null 409
$null=Ops POST 'backups/not-a-backup/verify' $null 409
ExpectStatus (Request GET ("/store-data/operations/backups/$($backup.id)/database.sqlite")) 404 'Private snapshots are not served as public files'
$damaged=Ops POST 'backups/default'
$database=Join-Path $runDir "store-data/operations/backups/$($damaged.id)/database.sqlite"
$stream=[IO.File]::Open($database,[IO.FileMode]::Append,[IO.FileAccess]::Write);try{$stream.WriteByte(42)}finally{$stream.Dispose()}
$null=Ops POST ("backups/$($damaged.id)/verify") $null 409
ExpectEqual @(Ops GET 'backups'|Where-Object id -EQ $damaged.id)[0].lastVerificationState 'Failed' 'Damaged snapshot is marked failed in the backup list'
$damagedManifest=Join-Path $runDir "store-data/operations/backups/$($damaged.id)/manifest.json"
$originalManifest=[IO.File]::ReadAllText($damagedManifest)
try{[IO.File]::WriteAllText($damagedManifest,'{broken');$listed=Ops GET 'backups';ExpectEqual @($listed|Where-Object id -EQ $damaged.id)[0].lastVerificationState 'Failed' 'Corrupt manifest is visible without hiding valid backups';ExpectEqual @($listed|Where-Object id -EQ $backup.id).Count 1 'Good backups remain listed'}finally{[IO.File]::WriteAllText($damagedManifest,$originalManifest)}
ExpectEqual @(JsonRequest GET '/api/merchant/products').Count ($before+1) 'Failed restore check leaves source unchanged'
$errors=Ops GET 'events?errorsOnly=true'
$failed=@($errors|Where-Object action -EQ 'RestoreRehearsalFailed')[0]
ExpectEqual ([string]::IsNullOrEmpty($failed.id)) $false 'Failed restore appears in error monitoring'
$null=Ops POST ("events/$($failed.id)/acknowledge")
$null=Ops POST ("events/$($failed.id)/acknowledge") $null 409
$secret='NEVER-LOG-THIS-'+[guid]::NewGuid().ToString('N')
ExpectStatus (Request POST ("/api/merchant/auth/login?secret=$secret") '' @{username='missing';password=$secret}) 401 'Failed login response'
$events=Ops GET 'events';$json=$events|ConvertTo-Json -Depth 5
ExpectEqual $json.Contains($secret) $false 'Audit excludes request bodies and URL queries'
$activity=Request GET '/api/merchant/activity' $adminToken
ExpectStatus $activity 200 'Merchant can read their activity'
ExpectEqual (($activity.Content|ConvertFrom-Json).data.Count -gt 0) $true 'Merchant writes produce activity events'
$null=Ops PUT 'backup-policy' @{enabled=$false;intervalHours=24}
$null=Ops PUT 'backup-policy' @{enabled=$true;intervalHours=0} 400
# Age only metadata in this disposable test directory to make scheduled snapshots due.
foreach($folder in Get-ChildItem (Join-Path $runDir 'store-data/operations/backups') -Directory){$path=Join-Path $folder.FullName 'manifest.json';if(Test-Path $path){$m=Get-Content $path -Raw|ConvertFrom-Json;$m.createdAtUtc='2000-01-01T00:00:00Z';$m|ConvertTo-Json -Depth 8|Set-Content $path}}
$null=Ops PUT 'backup-policy' @{enabled=$true;intervalHours=1}
for($wait=0;$wait -lt 150;$wait++){$snapshots=Ops GET 'backups';$fresh=@($snapshots|Where-Object {([datetime]$_.createdAtUtc).Year -gt 2000});if(@($fresh|Where-Object storeId -EQ 'default').Count -gt 0 -and @($fresh|Where-Object storeId -EQ 'platform').Count -gt 0){break};Start-Sleep -Milliseconds 100}
ExpectEqual @($fresh|Where-Object storeId -EQ 'default').Count 1 'Scheduled tenant backup created'
ExpectEqual @($fresh|Where-Object storeId -EQ 'platform').Count 1 'Scheduled platform backup created'
$null=Ops PUT 'backup-policy' @{enabled=$false;intervalHours=24}
# Actual replacement runs only in this disposable store, with the API stopped.
$blocked=Start-Process dotnet -ArgumentList @($dll,'--contentRoot',('"'+$runDir+'"'),'--restore-backup',$backup.id,'--confirm-store','default') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $runDir 'blocked-recovery.log') -RedirectStandardError (Join-Path $runDir 'blocked-recovery-error.log')
if(!$blocked.WaitForExit(15000)){throw 'Blocked recovery process did not exit'}
ExpectEqual ($blocked.ExitCode -ne 0) $true 'Actual recovery refuses to run while API holds the instance lock'
Stop-Process -Id $apiProcess.Id;$apiProcess.WaitForExit()
$recovery=Start-Process dotnet -ArgumentList @($dll,'--contentRoot',('"'+$runDir+'"'),'--restore-backup',$backup.id,'--confirm-store','default') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $runDir 'recovery.log') -RedirectStandardError (Join-Path $runDir 'recovery-error.log')
if(!$recovery.WaitForExit(30000)){throw 'Offline recovery did not finish'}
if($recovery.ExitCode -ne 0){throw ('Offline recovery failed: '+(Get-Content (Join-Path $runDir 'recovery-error.log') -Raw))}
$apiProcess=Start-Process dotnet -ArgumentList @($dll,'--contentRoot',('"'+$runDir+'"')) -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $runDir 'recovered-api.log') -RedirectStandardError (Join-Path $runDir 'recovered-api-error.log')
for($wait=0;$wait -lt 100;$wait++){try{if((Request GET '/api/settings').StatusCode -eq 200){break}}catch{};Start-Sleep -Milliseconds 100}
ExpectStatus (Request GET '/api/merchant/products' $adminToken) 401 'Recovery revokes previous merchant sessions'
ExpectStatus (Request GET '/api/account/profile' $newToken) 401 'Recovery revokes previous customer sessions'
ExpectStatus (Request POST '/api/account/reset-password' '' @{token=$oldAction;password='Must-Not-Be-Applied-12345'}) 400 'Historical unused password reset links are invalid after recovery'
$login=Request POST '/api/merchant/auth/login' '' @{username='admin';password='Admin@12345'};ExpectStatus $login 200 'Merchant can sign in after recovery';$adminToken=($login.Content|ConvertFrom-Json).data.token
ExpectEqual @(JsonRequest GET '/api/merchant/products').Count $before 'Offline replacement restores the original product snapshot'
ExpectEqual @(Get-ChildItem $runDir -Filter 'test.db.before-restore-*.db').Count 1 'Previous database retained for rollback'
$recorded=Ops GET 'events';ExpectEqual @($recorded|Where-Object action -EQ 'StoreRestoredOffline').Count 1 'Actual recovery is audited centrally'
Write-Host 'Snapshots, restore rehearsals, offline replacement, session revocation, corruption rejection, schedule, private journals and error review passed.'
