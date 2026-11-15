param([string]$ApiDll)
$ErrorActionPreference='Stop'
# When dot-sourced by security-smoke this branch uses only its isolated host/keys.
if (Test-Path variable:adminToken) {
    function ExpectEqual($actual,$expected,$label) {
        $script:checks++
        if($actual -ne $expected){throw "JWT assertion failed: $label"}
    }
    function Payload($token) {
        $part=$token.Split('.')[1].Replace('-','+').Replace('_','/')
        $part=$part.PadRight([int]([Math]::Ceiling($part.Length/4)*4),'=')
        [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($part))|ConvertFrom-Json -AsHashtable
    }
    function Encode($bytes) { [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+','-').Replace('/','_') }
    function ChangedToken($token,$field,$value,$key) {
        $p=Payload $token;$p[$field]=$value
        $parts=$token.Split('.');$unsigned=$parts[0]+'.'+(Encode ([Text.Encoding]::UTF8.GetBytes(($p|ConvertTo-Json -Compress -Depth 8))))
        $hmac=[Security.Cryptography.HMACSHA256]::new([Text.Encoding]::UTF8.GetBytes($key))
        try { $unsigned+'.'+(Encode ($hmac.ComputeHash([Text.Encoding]::ASCII.GetBytes($unsigned)))) } finally { $hmac.Dispose() }
    }
    $role='http://schemas.microsoft.com/ws/2008/06/identity/claims/role'
    $merchant=Payload $adminToken;$customer=Payload $customerToken
    ExpectEqual $merchant[$role] 'Merchant' 'Merchant role claim retained'
    ExpectEqual $customer[$role] 'Customer' 'Customer role claim retained'
    foreach($p in @($merchant,$customer)) {
        ExpectEqual $p.iss 'OnlineShopping' 'Issuer retained'
        ExpectEqual $p.aud 'OnlineShoppingClient' 'Audience retained'
        ExpectEqual ([string]::IsNullOrEmpty($p.store_id)) $false 'Store claim retained'
        ExpectEqual ($p.ContainsKey('store_security_version')) $true 'Security version claim retained, including the initial empty value'
        $remaining=[long]$p.exp-[DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
        ExpectEqual ($remaining -gt 7100 -and $remaining -le 7200) $true '120 minute lifetime retained'
    }
    ExpectEqual ([string]::IsNullOrEmpty($customer.session_stamp)) $false 'Customer session stamp retained'
    foreach($field in @('iss','aud')) {
        $bad=ChangedToken $customerToken $field 'wrong-test-value' $env:Jwt__SecretKey
        ExpectStatus (Request GET '/api/account/profile' $bad) 401 "Wrong $field rejected"
    }
    $bad=ChangedToken $customerToken 'iss' 'OnlineShopping' ('f'*64)
    ExpectStatus (Request GET '/api/account/profile' $bad) 401 'Wrong signing key rejected'
    $login=Request POST '/api/platform/auth/login' '' @{username='platform-test';password=$env:Platform__BootstrapPassword}
    ExpectStatus $login 200 'Platform token issuance retained'
    $platformToken=($login.Content|ConvertFrom-Json).data.token
    ExpectEqual (Payload $platformToken)[$role] 'PlatformAdmin' 'Platform role retained'
    ExpectStatus (Request GET '/api/platform/stores' $platformToken) 200 'Platform token validates'
    ExpectStatus (Request GET '/api/account/profile' $customerToken) 200 'Valid customer token validates'
    ExpectStatus (Request GET '/api/merchant/products' $adminToken) 200 'Valid merchant token validates'
    return
}
$repo=Split-Path $PSScriptRoot -Parent
$dll=(Resolve-Path -LiteralPath $ApiDll).Path
$root=Join-Path ([IO.Path]::GetTempPath()) ('shop-jwt-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $root|Out-Null
$key=[Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
$cases=@(
    @{name='valid';ok=$true},
    @{name='custom-expiry';ok=$true;field='ExpiryMinutes';value='37'},
    @{name='missing-key';field='SecretKey';value=''},
    @{name='short-key';field='SecretKey';value='test-short'},
    @{name='placeholder';field='SecretKey';value='CHANGE_ME_development_placeholder_secret_key_32plus_chars'},
    @{name='development-placeholder';field='SecretKey';value='dev_only_do_not_use_in_production_secret_key_32plus_chars'},
    @{name='missing-issuer';field='Issuer';value=''},
    @{name='missing-audience';field='Audience';value=''},
    @{name='zero-expiry';field='ExpiryMinutes';value='0'},
    @{name='negative-expiry';field='ExpiryMinutes';value='-1'},
    @{name='overflow-expiry';field='ExpiryMinutes';value='2147483648'},
    @{name='redacted-invalid-expiry';field='ExpiryMinutes';value='PRIVATE_TEST_MARKER'},
    @{name='omitted-expiry';ok=$true;omit=$true},
    @{name='environment-override';ok=$true;override=$true}
)
foreach($case in $cases) {
    $dir=Join-Path $root $case.name;New-Item -ItemType Directory -Path $dir|Out-Null
    $settings=Get-Content (Join-Path $repo 'src/Shop.Api/appsettings.json') -Raw|ConvertFrom-Json -AsHashtable
    $settings.Jwt=@{SecretKey=$key;Issuer='OnlineShopping';Audience='OnlineShoppingClient';ExpiryMinutes='120'}
    if($case.field){$settings.Jwt[$case.field]=$case.value}
    if($case.omit){$settings.Jwt.Remove('ExpiryMinutes')}
    if($case.override){$settings.Jwt.SecretKey='CHANGE_ME_placeholder'}
    $settings|ConvertTo-Json -Depth 20|Set-Content (Join-Path $dir 'appsettings.json')
    '{}'|Set-Content (Join-Path $dir 'appsettings.Development.json')
    $listener=[Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback,0);$listener.Start();$port=$listener.LocalEndpoint.Port;$listener.Stop()
    $info=[Diagnostics.ProcessStartInfo]::new('dotnet');$info.UseShellExecute=$false;$info.CreateNoWindow=$true;$info.WorkingDirectory=$dir
    $info.ArgumentList.Add($dll);$info.ArgumentList.Add('--contentRoot');$info.ArgumentList.Add($dir)
    $info.RedirectStandardOutput=$true;$info.RedirectStandardError=$true
    foreach($name in @($info.Environment.Keys)){if($name -like 'Jwt__*'){$info.Environment.Remove($name)|Out-Null}}
    $info.Environment['ASPNETCORE_ENVIRONMENT']='Development';$info.Environment['ASPNETCORE_URLS']="http://127.0.0.1:$port"
    $info.Environment['ConnectionStrings__SqliteConnection']='Data Source='+ (Join-Path $dir 'store.db')
    $info.Environment['ConnectionStrings__PlatformConnection']='Data Source='+ (Join-Path $dir 'platform.db')
    $info.Environment['Platform__DefaultStoreHost']='127.0.0.1'
    $info.Environment['Platform__BootstrapUsername']='jwt-test';$info.Environment['Platform__BootstrapPassword']='Jwt-test-'+[guid]::NewGuid().ToString('N')
    $info.Environment['Email__Enabled']='false'
    foreach($field in @('SecretKey','Issuer','Audience','ExpiryMinutes')) {
        if($settings.Jwt.ContainsKey($field)){$info.Environment['Jwt__'+$field]=[string]$settings.Jwt[$field]}
    }
    if($case.override){$info.Environment['Jwt__SecretKey']=$key}
    $process=[Diagnostics.Process]::Start($info)
    $stdout=$process.StandardOutput.ReadToEndAsync();$stderr=$process.StandardError.ReadToEndAsync()
    try {
        if($case.ok){
            $ready=$false
            for($i=0;$i -lt 60;$i++){
                if($process.HasExited){break}
                try{$r=Invoke-WebRequest "http://127.0.0.1:$port/swagger/v1/swagger.json" -TimeoutSec 1;if($r.StatusCode -eq 200){$ready=$true;break}}catch{}
                Start-Sleep -Milliseconds 100
            }
            if(!$ready){throw "Valid JWT case failed: $($case.name)"}
            $login=Invoke-RestMethod "http://127.0.0.1:$port/api/merchant/auth/login" -Method POST -ContentType 'application/json' -Body '{"username":"admin","password":"Admin@12345"}'
            $part=$login.data.token.Split('.')[1].Replace('-','+').Replace('_','/')
            $part=$part.PadRight([int]([Math]::Ceiling($part.Length/4)*4),'=')
            $payload=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($part))|ConvertFrom-Json
            $minutes=if($case.name -eq 'custom-expiry'){37}else{120}
            $seconds=[long]$payload.exp-[DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
            if($seconds -lt ($minutes*60-10) -or $seconds -gt $minutes*60){throw 'Configured/default token lifetime changed'}
        } else {
            if(!$process.WaitForExit(10000)){throw "Invalid configuration did not fail: $($case.name)"}
            if($process.ExitCode -eq 0){throw 'Invalid JWT configuration returned success'}
            $diagnostics=$stdout.GetAwaiter().GetResult()+$stderr.GetAwaiter().GetResult()
            if($diagnostics -notmatch 'Jwt:'){throw 'Expected a configuration-specific diagnostic'}
            if($diagnostics.Contains($key) -or $diagnostics.Contains('PRIVATE_TEST_MARKER')){throw 'Configuration value leaked into diagnostics'}
            if(@(Get-ChildItem $dir -Filter '*.db' -Recurse).Count -ne 0 -or (Test-Path (Join-Path $dir 'store-data')) -or (Test-Path (Join-Path $dir 'wwwroot'))){throw 'Invalid JWT configuration reached filesystem/database initialization'}
        }
        Write-Output "PASS JWT configuration: $($case.name)"
    } finally {
        if(!$process.HasExited){$process.Kill();$process.WaitForExit()};$process.Dispose()
    }
}
& pwsh -NoProfile -File (Join-Path $PSScriptRoot 'security-smoke.ps1') -ApiDll $dll -AdditionalChecks $PSCommandPath
if($LASTEXITCODE -ne 0){throw 'JWT HTTP regression checks failed'}

