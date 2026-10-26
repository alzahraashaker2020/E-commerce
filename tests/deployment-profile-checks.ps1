# Run via security-smoke.ps1; all databases and processes are isolated.
. (Join-Path $PSScriptRoot 'tenant-isolation-checks.ps1')
$deploymentKeys=@('Deployment__Mode','Deployment__StandaloneStoreId','Deployment__BusinessMode')
foreach($key in $deploymentKeys){$previous[$key]=[Environment]::GetEnvironmentVariable($key,'Process')}
function RestartProfile($mode,$storeId='default',$business=$null,$expectFailure=$false){
    Stop-Process -Id $apiProcess.Id
    [Environment]::SetEnvironmentVariable('Deployment__Mode',$mode,'Process')
    [Environment]::SetEnvironmentVariable('Deployment__StandaloneStoreId',$storeId,'Process')
    [Environment]::SetEnvironmentVariable('Deployment__BusinessMode',$business,'Process')
    $script:apiProcess=Start-Process dotnet -ArgumentList @($dll,'--contentRoot',('"'+$runDir+'"')) -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $runDir 'api.log') -RedirectStandardError (Join-Path $runDir 'api-error.log')
    $ready=$false
    for($i=0;$i -lt 80;$i++){
        if($apiProcess.HasExited){if($expectFailure){return};throw 'Profile startup failed. Inspect isolated API logs.'}
        try{$probe=Request GET '/api/deployment';if([int]$probe.StatusCode -eq 200){$ready=$true;break}}catch{}
        Start-Sleep -Milliseconds 200
    }
    if(!$ready){throw 'Profile did not become ready.'};if($expectFailure){throw 'Invalid configuration unexpectedly started the API.'}
}
$alpha=$stores.Alpha;$beta=$stores.Beta
$alphaToken=(StoreJson 'alpha.localhost' POST '/api/merchant/auth/login' '' @{username='merchant';password='Merchant-Isolation-12345'}).token
$business=StoreJson 'alpha.localhost' GET '/api/merchant/storefront' $alphaToken
$business.businessMode='Catalog';$business=StoreJson 'alpha.localhost' PUT '/api/merchant/storefront' $alphaToken $business
AssertValue (!(StoreJson 'alpha.localhost' GET '/api/store').commerceEnabled) 'SaaS merchant can use catalog independently'
AssertValue ((StoreJson 'beta.localhost' GET '/api/store').commerceEnabled) 'Catalog choice does not affect another SaaS merchant'
ExpectStatus (StoreRequest 'alpha.localhost' POST '/api/checkout' '' @{}) 403 'SaaS catalog blocks direct purchasing'
$business.businessMode='Commerce';$null=StoreJson 'alpha.localhost' PUT '/api/merchant/storefront' $alphaToken $business
$sub=StoreJson 'alpha.localhost' GET '/api/merchant/subscription' $alphaToken
ExpectStatus (Request POST "/api/platform/stores/$($alpha.id)/subscription" $platformToken @{action='Cancel';version=$sub.subscription.version;reference='Profile regression test'}) 200 'Cancel SaaS fixture subscription'
ExpectStatus (StoreRequest 'alpha.localhost' PUT '/api/merchant/settings' $alphaToken @{currencyCode='EGP';currencySymbol='EGP'}) 403 'SaaS still enforces subscription'
ExpectStatus (Request GET '/api/merchant/operations/backups' $adminToken) 404 'SaaS owner operations are unavailable'
$beforeProducts=@(StoreJson 'alpha.localhost' GET '/api/merchant/products' $alphaToken).Count
$betaBackup=StoreJson 'beta.localhost' POST "/api/platform/operations/backups/$($beta.id)" $platformToken @{}
RestartProfile 'Standalone' $alpha.id
$deployment=(Request GET '/api/deployment').Content|ConvertFrom-Json
AssertValue (!$deployment.data.saasEnabled) 'Standalone capabilities omit SaaS'
$info=StoreJson 'alpha.localhost' GET '/api/store'
AssertValue ($info.id -eq $alpha.id -and $info.operationsEnabled -and $info.commerceEnabled) 'Standalone resolves same store identity and commerce'
ExpectStatus (Request GET '/api/store') 404 'Standalone rejects former default store host'
ExpectStatus (StoreRequest 'beta.localhost' GET '/api/store') 404 'Standalone rejects unrelated tenant host'
ExpectStatus (StoreRequest 'alpha.localhost' GET '/api/merchant/products' $tokens.Beta) 401 'Other-store token stays invalid'
foreach($path in @('/api/platform/stores','/api/platform/plans','/api/platform/operations/backups','/api/merchant/subscription')){ExpectStatus (StoreRequest 'alpha.localhost' GET $path $platformToken) 404 'Platform and subscription routes disabled server-side'}
ExpectStatus (StoreRequest 'alpha.localhost' POST '/api/platform/auth/login' '' @{username='platform-test';password=$envValues.Platform__BootstrapPassword}) 404 'Standalone disables platform sign in'
ExpectStatus (StoreRequest 'alpha.localhost' PUT '/api/merchant/settings' $alphaToken @{currencyCode='EGP';currencySymbol='EGP'}) 200 'Standalone writes work with cancelled SaaS snapshot'
$category=(StoreJson 'alpha.localhost' GET '/api/merchant/categories' $alphaToken)[0]
for($i=0;$i -lt 2;$i++){$extra=StoreJson 'alpha.localhost' POST '/api/merchant/products' $alphaToken @{name="Standalone product $i";categoryId=$category.id;basePrice=120;stockQuantity=10;hasVariants=$false;isActive=$true}}
AssertValue (@(StoreJson 'alpha.localhost' GET '/api/merchant/products' $alphaToken).Count -eq $beforeProducts+2) 'Standalone does not inherit SaaS product quotas'
$gov=StoreJson 'alpha.localhost' POST '/api/merchant/delivery/governorates' $alphaToken @{name='Profile delivery';isActive=$true}
$area=StoreJson 'alpha.localhost' POST '/api/merchant/delivery/areas' $alphaToken @{name='Profile area';governorateId=$gov.id;currencyCode='EGP';shippingFee=0;isActive=$true}
$orderBody=@{idempotencyKey=[guid]::NewGuid().ToString();areaId=$area.id;guestName='Profile Test';guestEmail='profile@example.invalid';addressLine1='Test';city='Cairo';stateRegion='Cairo';postalCode='11511';country='Egypt';contactPhone='01000000000';paymentMethod='Cash';lines=@(@{productId=$extra.id;quantity=1})}
$order=StoreJson 'alpha.localhost' POST '/api/checkout' '' $orderBody
AssertValue ($order.total -eq 120) 'Standalone still calculates real checkout prices'
$ordersBefore=(StoreJson 'alpha.localhost' GET '/api/merchant/orders?pageSize=100' $alphaToken).totalCount
ExpectStatus (StoreRequest 'alpha.localhost' GET '/api/merchant/operations/backups') 401 'Standalone backups require owner'
$alphaCustomer=(StoreJson 'alpha.localhost' POST '/api/account/login' '' @{email='same-customer@example.invalid';password='Customer-Test-12345'}).token
ExpectStatus (StoreRequest 'alpha.localhost' GET '/api/merchant/operations/backups' $alphaCustomer) 403 'Customer cannot back up store'
$backup=StoreJson 'alpha.localhost' POST "/api/merchant/operations/backups/$($alpha.id)" $alphaToken @{}
$verified=StoreJson 'alpha.localhost' POST "/api/merchant/operations/backups/$($backup.id)/verify" $alphaToken @{}
AssertValue ($verified.lastVerificationState -eq 'Passed') 'Standalone restore rehearsal works'
ExpectStatus (StoreRequest 'alpha.localhost' POST '/api/merchant/operations/backups/platform' $alphaToken @{}) 404 'Owner cannot back up platform'
ExpectStatus (StoreRequest 'alpha.localhost' POST "/api/merchant/operations/backups/$($beta.id)" $alphaToken @{}) 404 'Owner cannot back up other store'
ExpectStatus (StoreRequest 'alpha.localhost' POST "/api/merchant/operations/backups/$($betaBackup.id)/verify" $alphaToken @{}) 404 'Owner cannot verify other-store backup'
$rows=StoreJson 'alpha.localhost' GET '/api/merchant/operations/backups?storeId=platform' $alphaToken
AssertValue (@($rows|Where-Object storeId -NE $alpha.id).Count -eq 0) 'Query parameters cannot widen backup scope'
$events=StoreJson 'alpha.localhost' GET '/api/merchant/operations/events?storeId=platform' $alphaToken
AssertValue (@($events|Where-Object storeId -NE $alpha.id).Count -eq 0) 'Query parameters cannot widen activity scope'
$null=StoreJson 'alpha.localhost' PUT '/api/merchant/operations/backup-policy' $alphaToken @{enabled=$false;intervalHours=24}
$config=StoreJson 'alpha.localhost' GET '/api/merchant/storefront' $alphaToken
$config.businessMode='Catalog';$config=StoreJson 'alpha.localhost' PUT '/api/merchant/storefront' $alphaToken $config
$info=StoreJson 'alpha.localhost' GET '/api/store'
AssertValue (!$info.commerceEnabled -and $info.businessMode -eq 'Catalog') 'Catalog capabilities reflect saved merchant setting'
ExpectStatus (StoreRequest 'alpha.localhost' GET '/api/catalog') 200 'Catalog browsing retained'
ExpectStatus (StoreRequest 'alpha.localhost' POST '/api/cart/validate' '' $orderBody.lines) 403 'Catalog blocks cart at server'
$orderBody.idempotencyKey=[guid]::NewGuid().ToString()
ExpectStatus (StoreRequest 'alpha.localhost' POST '/api/checkout' '' $orderBody) 403 'Catalog blocks direct checkout'
AssertValue ((StoreJson 'alpha.localhost' GET '/api/merchant/orders?pageSize=100' $alphaToken).totalCount -eq $ordersBefore) 'Catalog rejection never creates order'
ExpectStatus (StoreRequest 'alpha.localhost' GET '/api/merchant/returns' $alphaToken) 200 'Historical returns management remains available'
RestartProfile 'Standalone' $alpha.id
AssertValue (!(StoreJson 'alpha.localhost' GET '/api/store').commerceEnabled) 'Catalog mode survives restart'
$config=StoreJson 'alpha.localhost' GET '/api/merchant/storefront' $alphaToken
$config.businessMode='Commerce';$config=StoreJson 'alpha.localhost' PUT '/api/merchant/storefront' $alphaToken $config
$null=StoreJson 'alpha.localhost' POST '/api/checkout' '' $orderBody
RestartProfile 'Standalone' $alpha.id 'Catalog'
$info=StoreJson 'alpha.localhost' GET '/api/store'
AssertValue (!$info.commerceEnabled -and $info.businessModeLocked) 'Operator override locks activity mode independently'
ExpectStatus (StoreRequest 'alpha.localhost' POST '/api/checkout' '' $orderBody) 403 'Server override cannot be bypassed by stored Commerce setting'
RestartProfile 'Saas'
AssertValue (((Request GET '/api/deployment').Content|ConvertFrom-Json).data.saasEnabled) 'SaaS can be restored without converting data'
ExpectStatus (StoreRequest 'alpha.localhost' PUT '/api/merchant/settings' $alphaToken @{currencyCode='EGP';currencySymbol='EGP'}) 403 'Original cancelled subscription still applies after returning to SaaS'
ExpectStatus (StoreRequest 'beta.localhost' GET '/api/merchant/products' $tokens.Beta) 200 'Other tenant preserved after standalone runs'
ExpectStatus (Request GET '/api/platform/stores' $platformToken) 200 'Platform access restored in SaaS'
Write-Output 'Deployment profiles, independent catalog mode, standalone owner operations and unchanged SaaS isolation passed.'

# A fresh standalone install boots without platform credentials or a subscription.
[Environment]::SetEnvironmentVariable('ConnectionStrings__PlatformConnection',('Data Source='+(Join-Path $runDir 'fresh-registry.db')),'Process')
[Environment]::SetEnvironmentVariable('ConnectionStrings__SqliteConnection',('Data Source='+(Join-Path $runDir 'fresh-store.db')),'Process')
RestartProfile 'Standalone'
$fresh=(Request GET '/api/store').Content|ConvertFrom-Json
AssertValue ($fresh.data.id -eq 'default' -and $fresh.data.operationsEnabled) 'Fresh standalone initializes local store identity'
$freshLogin=Request POST '/api/merchant/auth/login' '' @{username='admin';password='Admin@12345'}
ExpectStatus $freshLogin 200 'Fresh standalone merchant login needs no platform setup'
$freshToken=($freshLogin.Content|ConvertFrom-Json).data.token
ExpectStatus (Request GET '/api/merchant/operations/status' $freshToken) 200 'Fresh owner operations work without SaaS plans'
ExpectStatus (Request GET '/api/merchant/subscription' $freshToken) 404 'Fresh standalone has no subscription surface'
RestartProfile 'Invalid' 'default' $null $true
AssertValue ((Get-Content (Join-Path $runDir 'api-error.log') -Raw) -match 'Deployment:Mode must be Saas or Standalone') 'Invalid deployment mode fails startup clearly'
Write-Output 'Fresh standalone startup and invalid-configuration rejection passed.'
