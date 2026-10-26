# Run through security-smoke.ps1; isolated SQLite registry and tenant databases only.
. (Join-Path $PSScriptRoot 'tenant-isolation-checks.ps1')
$plans=(Request GET '/api/platform/onboarding/plans').Content|ConvertFrom-Json
$free=@($plans.data | Where-Object name -eq 'Free Trial')[0]
AssertValue ($free.trialDays -eq 14 -and $free.maxProducts -eq 50) 'Default free trial is available with explicit limits'
$private=Request POST '/api/platform/plans' $platformToken @{name='Private zero-price demo';priceMinor=0;currency='EGP';periodMonths=1;trialDays=90;isActive=$true;allowMerchantApplications=$false}
ExpectStatus $private 200 'Platform may retain private demo plans'
$offered=(Request GET '/api/platform/onboarding/plans').Content|ConvertFrom-Json
AssertValue (@($offered.data).Count -eq 1) 'Private zero-price demo never appears on public registration'
$body=@{fullName='Onboarding Owner';email='new-merchant@example.invalid';phone='01000000000';username='new-merchant';password='Merchant-Applicant-12345';storeName='Applied shop';businessCategory='Kids clothes';businessDescription='An isolated test shop';businessMode='Catalog';planId=$free.id;planVersion=$free.version;acceptTerms=$true;role='PlatformAdmin';state='Approved'}
$bad=@{}+$body;$bad.acceptTerms=$false
ExpectStatus (Request POST '/api/platform/onboarding/applications' '' $bad) 400 'Consent is required'
$bad=@{}+$body;$bad.planId=$isolationPlan.id
ExpectStatus (Request POST '/api/platform/onboarding/applications' '' $bad) 400 'Paid plans cannot be requested as free trials'
$applied=Request POST '/api/platform/onboarding/applications' '' $body
ExpectStatus $applied 200 'Merchant can submit application without a store account'
$app=($applied.Content|ConvertFrom-Json).data
AssertValue ($app.state -eq 'Pending' -and !$app.token) 'Public role/state input cannot grant merchant or platform credentials'
ExpectStatus (Request POST '/api/platform/onboarding/applications' '' $body) 409 'Duplicate email does not create another application'
ExpectStatus (Request POST '/api/platform/onboarding/status' '' @{email=$body.email;password='wrong'}) 401 'Application details require password'
$status=(Request POST '/api/platform/onboarding/status' '' @{email=$body.email;password=$body.password}).Content|ConvertFrom-Json
AssertValue ($status.data.state -eq 'Pending' -and !$status.data.host) 'Pending applicant sees own pending state only'
ExpectStatus (Request POST '/api/merchant/auth/login' '' @{username=$body.username;password=$body.password}) 401 'Pending applicant is not a merchant of current store'
foreach($token in @('',$customerToken,$adminToken)){
 ExpectStatus (Request GET '/api/platform/onboarding/applications' $token) 401 'Application list requires platform role'
 ExpectStatus (Request POST "/api/platform/onboarding/applications/$($app.id)/approve" $token @{version=1;host='onboard.localhost';connectionKey='Onboard'}) 401 'Approval requires platform role'
}
$list=(Request GET '/api/platform/onboarding/applications' $platformToken).Content|ConvertFrom-Json
$row=@($list.data|Where-Object id -eq $app.id)[0]
AssertValue (!$row.PSObject.Properties['passwordHash'] -and !$row.PSObject.Properties['password']) 'Application review never discloses password or hash'
ExpectStatus (Request POST "/api/platform/onboarding/applications/$($app.id)/approve" $platformToken @{version=99;host='onboard.localhost';connectionKey='Onboard'}) 409 'Stale approval cannot act'
ExpectStatus (Request POST "/api/platform/onboarding/applications/$($app.id)/approve" $platformToken @{version=$row.version;host='onboard.localhost';connectionKey='Missing'}) 400 'Unconfigured database cannot be provisioned'
$list=(Request GET '/api/platform/onboarding/applications' $platformToken).Content|ConvertFrom-Json
$row=@($list.data|Where-Object id -eq $app.id)[0]
AssertValue ($row.state -eq 'Pending') 'Validation failure returns request to pending'
# Existing applications retain accepted terms after the public offer changes.
$edited=Request PUT "/api/platform/plans/$($free.id)" $platformToken @{name='Revised free trial';priceMinor=0;currency='EGP';periodMonths=1;trialDays=7;maxProducts=5;isActive=$true;allowMerchantApplications=$true;version=$free.version}
ExpectStatus $edited 200 'Edit trial offer for future applications'
$newFree=($edited.Content|ConvertFrom-Json).data
$bad=@{}+$body;$bad.email='stale-offer@example.invalid'
ExpectStatus (Request POST '/api/platform/onboarding/applications' '' $bad) 400 'Stale displayed offer must be refreshed before consent'
# Add a fresh configured reference and restart the isolated API so configuration is visible.
$key='StoreConnections__Onboard__Provider';$previous[$key]=[Environment]::GetEnvironmentVariable($key,'Process');[Environment]::SetEnvironmentVariable($key,'Sqlite','Process')
$key='StoreConnections__Onboard__ConnectionString';$previous[$key]=[Environment]::GetEnvironmentVariable($key,'Process');[Environment]::SetEnvironmentVariable($key,('Data Source='+(Join-Path $runDir 'onboard.db')),'Process')
Stop-Process -Id $apiProcess.Id
$script:apiProcess=Start-Process dotnet -ArgumentList @($dll,'--contentRoot',('"'+$runDir+'"')) -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $runDir 'onboarding-api.log') -RedirectStandardError (Join-Path $runDir 'onboarding-api-error.log')
for($i=0;$i -lt 80;$i++){try{$null=Request GET '/api/deployment';break}catch{Start-Sleep -Milliseconds 200}}
$offers=(Request GET '/api/platform/onboarding/plans').Content|ConvertFrom-Json
AssertValue (@($offers.data).Count -eq 1 -and $offers.data[0].name -eq 'Revised free trial') 'Restart never recreates renamed trial offer'
$before=Get-Date
$approved=Request POST "/api/platform/onboarding/applications/$($app.id)/approve" $platformToken @{version=$row.version;host='onboard.localhost';connectionKey='Onboard'}
ExpectStatus $approved 200 'Admin approval provisions isolated store'
ExpectStatus (Request POST "/api/platform/onboarding/applications/$($app.id)/approve" $platformToken @{version=$row.version;host='duplicate.localhost';connectionKey='Onboard'}) 409 'Approval cannot run twice'
$status=(Request POST '/api/platform/onboarding/status' '' @{email=$body.email;password=$body.password}).Content|ConvertFrom-Json
AssertValue ($status.data.state -eq 'Approved' -and $status.data.host -eq 'onboard.localhost') 'Applicant receives own store address after approval'
$owner=StoreJson 'onboard.localhost' POST '/api/merchant/auth/login' '' @{username=$body.username;password=$body.password}
AssertValue ($owner.role -eq 'Merchant') 'Approved merchant uses original chosen password'
$sub=StoreJson 'onboard.localhost' GET '/api/merchant/subscription' $owner.token
AssertValue ($sub.canSell -and $sub.subscription.isTrial -and $sub.subscription.maxProducts -eq 50 -and $sub.subscription.planName -eq 'Free Trial' -and (([datetime]$sub.subscription.endsAtUtc)-([datetime]$sub.subscription.startsAtUtc)).TotalDays -eq 14) 'Approval grants free trial snapshot and quota'
AssertValue (([datetime]$sub.subscription.startsAtUtc) -ge $before.ToUniversalTime().AddSeconds(-2)) 'Trial starts at approval rather than application'
AssertValue (!(StoreJson 'onboard.localhost' GET '/api/store').commerceEnabled) 'Requested catalog mode is honored'
AssertValue (@(StoreJson 'onboard.localhost' GET '/api/merchant/products' $owner.token).Count -eq 0) 'New merchant never inherits original storefront products'
ExpectStatus (StoreRequest 'alpha.localhost' GET '/api/merchant/products' $owner.token) 401 'Approved merchant cannot access another store'
ExpectStatus (StoreRequest 'onboard.localhost' GET '/api/account/orders' $owner.token) 403 'Merchant does not impersonate customer portal'
$body.email='rejected-merchant@example.invalid';$body.username='rejected';$body.planVersion=$newFree.version
$rejected=(Request POST '/api/platform/onboarding/applications' '' $body).Content|ConvertFrom-Json
ExpectStatus (Request POST "/api/platform/onboarding/applications/$($rejected.data.id)/reject" $platformToken @{version=1;note='Business details need clarification.'}) 200 'Admin may reject with explanation'
ExpectStatus (Request POST "/api/platform/onboarding/applications/$($rejected.data.id)/approve" $platformToken @{version=2;host='rejected.localhost';connectionKey='Onboard'}) 409 'Rejected applicant cannot become active'
$status=(Request POST '/api/platform/onboarding/status' '' @{email=$body.email;password=$body.password}).Content|ConvertFrom-Json
AssertValue ($status.data.state -eq 'Rejected' -and $status.data.reviewNote -eq 'Business details need clarification.' -and !$status.data.host) 'Rejected applicant sees reason and receives no store access'

Stop-Process -Id $apiProcess.Id
$standaloneSettings=@{Deployment__Mode='Standalone';Deployment__StandaloneStoreId=($approved.Content|ConvertFrom-Json).data.id}
foreach($setting in $standaloneSettings.GetEnumerator()){
 $previous[$setting.Key]=[Environment]::GetEnvironmentVariable($setting.Key,'Process');[Environment]::SetEnvironmentVariable($setting.Key,$setting.Value,'Process')
}
$script:apiProcess=Start-Process dotnet -ArgumentList @($dll,'--contentRoot',('"'+$runDir+'"')) -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $runDir 'standalone-onboarding.log') -RedirectStandardError (Join-Path $runDir 'standalone-onboarding-error.log')
for($i=0;$i -lt 80;$i++){try{$null=Request GET '/api/deployment';break}catch{Start-Sleep -Milliseconds 200}}
ExpectStatus (Request GET '/api/platform/onboarding/plans') 404 'Standalone does not advertise trial offers'
ExpectStatus (Request POST '/api/platform/onboarding/applications' '' $body) 404 'Standalone refuses SaaS application writes'
ExpectStatus (Request GET '/api/platform/onboarding/applications' $platformToken) 404 'Standalone refuses platform application review'
