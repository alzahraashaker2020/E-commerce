. (Join-Path $PSScriptRoot 'email-account-security-checks.ps1')
function OutboxRows { @((JsonRequest GET '/api/merchant/notifications/messages')) }
function WaitEmailState($id,$state){
    for($wait=0;$wait -lt 150;$wait++){
        $row=OutboxRows | Where-Object id -EQ $id
        if($row.state -eq $state){return $row}
        Start-Sleep -Milliseconds 100
    }
    throw "Expected outbox state $state, got $($row.state)"
}
function RestartEmailApi {
    Stop-Process -Id $apiProcess.Id;$apiProcess.WaitForExit()
    $script:apiProcess=Start-Process dotnet -ArgumentList @($dll,'--contentRoot',('"'+$runDir+'"')) -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $runDir ('outbox-'+[guid]::NewGuid().ToString('N')+'.log')) -RedirectStandardError (Join-Path $runDir ('outbox-error-'+[guid]::NewGuid().ToString('N')+'.log'))
    for($wait=0;$wait -lt 100;$wait++){try{if((Request GET '/api/settings').StatusCode -eq 200){return}}catch{};Start-Sleep -Milliseconds 100}
    throw 'Outbox test API did not restart'
}
$delivered=OutboxRows | Where-Object subject -EQ "New order $number" | Select-Object -First 1
$null=WaitEmailState $delivered.id 'DevelopmentOnly'
ExpectStatus (Request POST "/api/merchant/notifications/messages/$($delivered.id)/retry" $adminToken) 409 'Delivered messages cannot be resent manually'
ExpectStatus (Request GET '/api/merchant/notifications/messages' $customerToken) 403 'Customer cannot inspect merchant email history'
ExpectStatus (Request GET '/api/merchant/notifications/messages') 401 'Anonymous email history denied'
$savedEnv=@{}
function RestoreEmailEnvironment{foreach($key in $savedEnv.Keys){if($null -eq $savedEnv[$key]){Remove-Item -LiteralPath ('Env:'+ $key) -ErrorAction SilentlyContinue}else{[Environment]::SetEnvironmentVariable($key,$savedEnv[$key],'Process')}}}
try{
    $settings=@{Email__Enabled='true';Email__SmtpHost='127.0.0.1';Email__SmtpPort='1';Email__SmtpUser='isolated';Email__SmtpPassword='isolated';Email__From='isolated@example.invalid'}
    foreach($key in $settings.Keys){$savedEnv[$key]=[Environment]::GetEnvironmentVariable($key,'Process');[Environment]::SetEnvironmentVariable($key,$settings[$key],'Process')}
    RestartEmailApi
    $failureOrder=@{}+$guest;$failureOrder.idempotencyKey=[guid]::NewGuid().ToString()
    $failure=Request POST '/api/checkout' '' $failureOrder
    ExpectStatus $failure 200 'SMTP failure does not block saved order'
    $failureNumber=($failure.Content|ConvertFrom-Json).data.orderNumber
    $entry=OutboxRows | Where-Object subject -EQ "Order $failureNumber confirmation" | Select-Object -First 1
    for($attempt=1;$attempt -le 5;$attempt++){
        for($wait=0;$wait -lt 150;$wait++){
            $entry=OutboxRows | Where-Object id -EQ $entry.id
            if($entry.attempts -ge $attempt -and $entry.state -ne 'Sending'){break}
            Start-Sleep -Milliseconds 100
        }
        ExpectEqual $entry.attempts $attempt 'Automatic attempt recorded'
        if($attempt -lt 5){& dotnet (Join-Path $repoRoot 'tests/Shop.SchemaChecks/bin/Debug/net7.0/Shop.SchemaChecks.dll') --due-test-email (Join-Path $runDir 'test.db') $entry.id;if($LASTEXITCODE -ne 0){throw 'Cannot make isolated retry due'}}
    }
    ExpectEqual $entry.state 'Failed' 'Retries stop after five failed attempts'
    $history=JsonRequest GET "/api/merchant/notifications/messages/$($entry.id)/attempts"
    ExpectEqual @($history).Count 5 'Attempt history is durable'
    RestartEmailApi
    ExpectEqual (OutboxRows|Where-Object id -EQ $entry.id).state 'Failed' 'Failed message survives process restart'
    RestoreEmailEnvironment
    RestartEmailApi
    ExpectStatus (Request POST "/api/merchant/notifications/messages/$($entry.id)/retry" $adminToken) 200 'Merchant can retry failed message after configuration repair'
    $null=WaitEmailState $entry.id 'DevelopmentOnly'
    ExpectEqual @(JsonRequest GET "/api/merchant/notifications/messages/$($entry.id)/attempts").Count 6 'Manual retry preserves previous attempt history'
    ExpectEqual (OutboxRows|Where-Object id -EQ $entry.id).PSObject.Properties.Name.Contains('payload') $false 'Email bodies and recovery secrets are excluded from history API'
}finally{RestoreEmailEnvironment}
Write-Host 'Durable encrypted outbox, restart recovery, SMTP failure, automatic retries, manual retry and permissions passed.'
