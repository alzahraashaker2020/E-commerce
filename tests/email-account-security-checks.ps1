. (Join-Path $PSScriptRoot 'upload-tracking-security-checks.ps1')
$mailDirectory=Join-Path $runDir 'store-data/default/sent-emails'
function SecurityToken($email,$purpose){
    $subject=if($purpose -eq 'VerifyEmail'){'Verify your store email'}else{'Reset your store password'}
    for($attempt=0;$attempt -lt 40;$attempt++){
    foreach($file in (Get-ChildItem $mailDirectory -Filter 'message-*.txt'|Sort-Object LastWriteTime -Descending)){
        $text=[IO.File]::ReadAllText($file.FullName)
        if($text.Contains("To: $email") -and $text.Contains("Subject: $subject")){
            return [regex]::Match($text,'#token=([A-Za-z0-9_-]{43})').Groups[1].Value
        }
    }
    Start-Sleep -Milliseconds 100
    }
    throw 'Expected a development email with an account action token'
}
$email='mail-security-'+[guid]::NewGuid().ToString('N')+'@example.invalid'
$register=Request POST '/api/account/register' '' @{fullName='Email Security';email=$email;password='Original-Password-12345'}
ExpectStatus $register 200 'Register with verification email'
$session=($register.Content|ConvertFrom-Json).data
$token=SecurityToken $email 'VerifyEmail'
$status=Request GET '/api/account/email-status' $session.token
ExpectStatus $status 200 'Read email verification status'
ExpectEqual ($status.Content|ConvertFrom-Json).data.verified $false 'New email is not automatically verified'
ExpectStatus (Request POST '/api/account/verify-email' '' @{token=$token}) 200 'Email proof verifies account'
ExpectStatus (Request POST '/api/account/verify-email' '' @{token=$token}) 400 'Verification token is single use'
$status=Request GET '/api/account/email-status' $session.token
ExpectEqual ($status.Content|ConvertFrom-Json).data.verified $true 'Verification persists'
$known=Request POST '/api/account/forgot-password' '' @{email=$email}
$unknown=Request POST '/api/account/forgot-password' '' @{email='unknown-'+[guid]::NewGuid().ToString('N')+'@example.invalid'}
ExpectStatus $known 200 'Known email recovery response'
ExpectStatus $unknown 200 'Unknown email recovery response'
ExpectEqual $known.Content $unknown.Content 'Recovery response does not disclose account existence'
$reset=SecurityToken $email 'ResetPassword'
ExpectStatus (Request POST '/api/account/verify-email' '' @{token=$reset}) 400 'Reset token cannot verify email'
ExpectStatus (Request POST '/api/account/reset-password' '' @{token=$reset;password='short'}) 400 'Weak replacement password rejected'
ExpectStatus (Request POST '/api/account/reset-password' '' @{token=$reset;password='Replacement-Password-12345'}) 200 'Password reset succeeds'
ExpectStatus (Request POST '/api/account/reset-password' '' @{token=$reset;password='Other-Password-12345'}) 400 'Reset token cannot be replayed'
ExpectStatus (Request GET '/api/account/profile' $session.token) 401 'Password reset revokes existing JWT'
ExpectStatus (Request POST '/api/account/login' '' @{email=$email;password='Original-Password-12345'}) 401 'Old password no longer works'
ExpectStatus (Request POST '/api/account/login' '' @{email=$email;password='Replacement-Password-12345'}) 200 'New password works'
$expiredEmail='expired-'+[guid]::NewGuid().ToString('N')+'@example.invalid'
$registered=Request POST '/api/account/register' '' @{fullName='Expiry Test';email=$expiredEmail;password='Expiry-Password-12345'}
ExpectStatus $registered 200 'Create expiry test account'
$expirySession=($registered.Content|ConvertFrom-Json).data
$expiryToken=SecurityToken $expiredEmail 'VerifyEmail'
& dotnet (Join-Path $repoRoot 'tests/Shop.SchemaChecks/bin/Debug/net7.0/Shop.SchemaChecks.dll') --expire-test-account-actions (Join-Path $runDir 'test.db') $expirySession.customerId
if($LASTEXITCODE -ne 0){throw 'Could not expire isolated account action'}
ExpectStatus (Request POST '/api/account/verify-email' '' @{token=$expiryToken}) 400 'Expired email action rejected'
$prefs=@{merchantEmail='merchant-notifications@example.invalid';customerOrdersEnabled=$true;customerStatusEnabled=$true;merchantOrdersEnabled=$true}
$saved=Request PUT '/api/merchant/notifications' $adminToken $prefs
ExpectStatus $saved 200 'Merchant email notification preferences saved'
ExpectEqual ($saved.Content|ConvertFrom-Json).data.ready $false 'Unconfigured Gmail is not reported as connected'
ExpectEqual ($saved.Content.Contains('smtpPassword')) $false 'SMTP secrets are not returned by API'
$testOrder=@{}+$guest;$testOrder.idempotencyKey=[guid]::NewGuid().ToString()
$created=Request POST '/api/checkout' '' $testOrder
ExpectStatus $created 200 'Order with customer and merchant notifications'
$number=($created.Content|ConvertFrom-Json).data.orderNumber
for($wait=0;$wait -lt 100 -and !(Test-Path (Join-Path $mailDirectory ('merchant-'+$number+'.txt')));$wait++){Start-Sleep -Milliseconds 100}
ExpectEqual (Test-Path (Join-Path $mailDirectory ($number+'.txt'))) $true 'Customer receives confirmation message'
ExpectEqual (Test-Path (Join-Path $mailDirectory ('merchant-'+$number+'.txt'))) $true 'Merchant receives separate new-order message'
ExpectStatus (Request POST '/api/checkout' '' $testOrder) 200 'Replay with notification settings enabled'
ExpectEqual @([regex]::Matches([IO.File]::ReadAllText((Join-Path $mailDirectory ('merchant-'+$number+'.txt'))),'Subject:')).Count 1 'Replayed checkout does not notify merchant twice'
$logs=Get-Content -LiteralPath (Join-Path $runDir 'restart-checkout.log') -Raw
ExpectEqual ($logs.Contains($reset)) $false 'Reset secret is not written in normal API logs'
Write-Host 'Customer email verification, reset revocation, private development messages and merchant notification preferences passed.'
