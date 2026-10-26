# Isolated registry and tenant data; run through security-smoke.ps1.
. (Join-Path $PSScriptRoot 'tenant-isolation-checks.ps1')
$alphaToken=(StoreJson 'alpha.localhost' POST '/api/merchant/auth/login' '' @{username='merchant';password='Merchant-Isolation-12345'}).token
$fullPlanResponse=Request POST '/api/platform/plans' $platformToken @{name='Published business';description='Business subscription';nameAr='باقة المشاريع';descriptionAr='اشتراك المشاريع';priceMinor=120000;currency='EGP';periodMonths=3;trialDays=0;maxProducts=25;isActive=$true;allowMerchantApplications=$true;features=@('Discounts','Newsletter','StorefrontDesign','Marketplace')}
ExpectStatus $fullPlanResponse 200 'Platform publishes a paid plan with duration and entitlements'
$fullPlan=($fullPlanResponse.Content|ConvertFrom-Json).data
$published=(Request GET '/api/platform/onboarding/plans').Content|ConvertFrom-Json
AssertValue (@($published.data|Where-Object id -eq $fullPlan.id).Count -eq 1 -and @($published.data|Where-Object id -eq $fullPlan.id)[0].nameAr -eq 'باقة المشاريع') 'Published paid plans are visible before registration'
$body=@{fullName='Paid applicant';email='paid-applicant@example.invalid';phone='01000000000';username='paid-owner';password='Applicant-Test-12345';storeName='Paid store';businessCategory='Homewear';businessDescription='An independent homewear shop';businessMode='Commerce';planId=$fullPlan.id;planVersion=$fullPlan.version;acceptTerms=$true}
$submitted=Request POST '/api/platform/onboarding/applications' '' $body
ExpectStatus $submitted 200 'Merchant may select a published paid plan'
$app=($submitted.Content|ConvertFrom-Json).data
ExpectStatus (Request POST "/api/platform/onboarding/applications/$($app.id)/approve" $platformToken @{version=1;host='paid.localhost';connectionKey='Paid'}) 400 'Paid plan without trial requires manual payment verification'
$state=(Request POST '/api/platform/onboarding/status' '' @{email=$body.email;password=$body.password}).Content|ConvertFrom-Json
AssertValue ($state.data.state -eq 'Pending' -and $state.data.priceMinor -eq 120000 -and $state.data.periodMonths -eq 3) 'Paid applicant retains snapshotted price and duration without a token'
# Explicitly activate the test tenants under the full plan.
foreach($store in @($stores.Alpha,$stores.Beta)){
 $current=(Request GET '/api/platform/subscriptions' $platformToken).Content|ConvertFrom-Json
 $sub=@($current.data|Where-Object id -eq $store.id)[0].subscription
 ExpectStatus (Request POST "/api/platform/stores/$($store.id)/subscription" $platformToken @{action='Assign';planId=$fullPlan.id;version=$sub.version;reference='Isolated test activation'}) 200 'Activate full plan for marketplace fixture'
}
$market=(Request GET "/api/platform/marketplace?storeId=$($stores.Alpha.id)").Content|ConvertFrom-Json
AssertValue ($market.data.totalCount -eq 1 -and $market.data.items[0].storeId -eq $stores.Alpha.id -and $market.data.items[0].nameAr -eq 'منتج متجر ألفا') 'Marketplace projects public fields and tenant translation'
AssertValue (!$market.data.items[0].PSObject.Properties['customerId'] -and !$market.data.items[0].PSObject.Properties['connectionKey']) 'Marketplace never publishes account or connection fields'
$design=StoreJson 'alpha.localhost' GET '/api/merchant/storefront' $alphaToken
$design.includeInMarketplace=$false
$design=StoreJson 'alpha.localhost' PUT '/api/merchant/storefront' $alphaToken $design
$market=(Request GET "/api/platform/marketplace?storeId=$($stores.Alpha.id)").Content|ConvertFrom-Json
AssertValue ($market.data.totalCount -eq 0) 'Merchant may opt out of marketplace'
$design.includeInMarketplace=$true;$design=StoreJson 'alpha.localhost' PUT '/api/merchant/storefront' $alphaToken $design
ExpectStatus (Request PUT "/api/platform/stores/$($stores.Beta.id)/state" $platformToken @{state='Suspended'}) 200 'Suspend marketplace fixture'
$market=(Request GET "/api/platform/marketplace?storeId=$($stores.Beta.id)").Content|ConvertFrom-Json
AssertValue ($market.data.totalCount -eq 0) 'Suspended store products disappear from marketplace'
ExpectStatus (Request PUT "/api/platform/stores/$($stores.Beta.id)/state" $platformToken @{state='Active'}) 200 'Reactivate marketplace fixture'
$siteResponse=Request GET '/api/platform/site'
ExpectStatus $siteResponse 200 'Public website has default bilingual content'
$site=($siteResponse.Content|ConvertFrom-Json).data
$site.brandAr='منصة الاختبار';$site.headlineEn='Independent ideas, together.'
ExpectStatus (Request PUT '/api/platform/site' $alphaToken $site) 401 'Merchant cannot edit platform website'
ExpectStatus (Request PUT '/api/platform/site' $customerToken $site) 401 'Customer cannot edit platform website'
ExpectStatus (Request PUT '/api/platform/site' $platformToken $site) 200 'Platform admin edits website without source changes'
ExpectStatus (Request PUT '/api/platform/site' $platformToken $site) 409 'Website content rejects stale edits'
$siteAfter=(Request GET '/api/platform/site').Content|ConvertFrom-Json
AssertValue ($siteAfter.data.brandAr -eq 'منصة الاختبار') 'Public website uses persisted bilingual content'
$restrictedResponse=Request POST '/api/platform/plans' $platformToken @{name='Core only';priceMinor=0;currency='EGP';periodMonths=1;trialDays=14;isActive=$true;features=@()}
ExpectStatus $restrictedResponse 200 'Plan can explicitly omit optional features'
$restricted=($restrictedResponse.Content|ConvertFrom-Json).data
$current=StoreJson 'alpha.localhost' GET '/api/merchant/subscription' $alphaToken
ExpectStatus (Request POST "/api/platform/stores/$($stores.Alpha.id)/subscription" $platformToken @{action='Assign';planId=$restricted.id;version=$current.subscription.version;reference='Entitlement test'}) 200 'Apply limited features explicitly'
ExpectStatus (StoreRequest 'alpha.localhost' GET '/api/merchant/discounts' $alphaToken) 403 'Discounts require plan entitlement'
ExpectStatus (StoreRequest 'alpha.localhost' PUT '/api/merchant/storefront' $alphaToken $design) 403 'Storefront writes require customization entitlement'
ExpectStatus (StoreRequest 'alpha.localhost' POST '/api/storefront/subscribe' '' @{email='plan@example.invalid';consent=$true}) 403 'Newsletter requests require entitlement server-side'
$public=StoreJson 'alpha.localhost' GET '/api/storefront'
AssertValue (!$public.newsletterEnabled) 'Public storefront hides unavailable newsletter'
$market=(Request GET "/api/platform/marketplace?storeId=$($stores.Alpha.id)").Content|ConvertFrom-Json
AssertValue ($market.data.totalCount -eq 0) 'Marketplace visibility is enforced by plan'
$info=StoreJson 'alpha.localhost' GET '/api/store'
AssertValue (@($info.features).Count -eq 0) 'Frontend capabilities match enforced entitlements'
# Configure a fresh database for approved paid applicant and verify activation snapshot.
foreach($setting in @{StoreConnections__Paid__Provider='Sqlite';StoreConnections__Paid__ConnectionString=('Data Source='+(Join-Path $runDir 'paid.db'))}.GetEnumerator()){
 $previous[$setting.Key]=[Environment]::GetEnvironmentVariable($setting.Key,'Process');[Environment]::SetEnvironmentVariable($setting.Key,$setting.Value,'Process')
}
Stop-Process -Id $apiProcess.Id
$script:apiProcess=Start-Process dotnet -ArgumentList @($dll,'--contentRoot',('"'+$runDir+'"')) -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $runDir 'marketplace-api.log') -RedirectStandardError (Join-Path $runDir 'marketplace-api-error.log')
for($i=0;$i -lt 80;$i++){try{$null=Request GET '/api/deployment';break}catch{Start-Sleep -Milliseconds 200}}
$approved=Request POST "/api/platform/onboarding/applications/$($app.id)/approve" $platformToken @{version=1;host='paid.localhost';connectionKey='Paid';paymentVerified=$true;paymentReference='TEST-OFFLINE-ONLY'}
ExpectStatus $approved 200 'Verified offline payment allows requested paid subscription'
$owner=StoreJson 'paid.localhost' POST '/api/merchant/auth/login' '' @{username=$body.username;password=$body.password}
$paidSub=StoreJson 'paid.localhost' GET '/api/merchant/subscription' $owner.token
AssertValue (!$paidSub.subscription.isTrial -and $paidSub.subscription.priceMinor -eq 120000 -and $paidSub.subscription.periodMonths -eq 3 -and $paidSub.canSell) 'Paid activation honors price and duration without pretending to process payment'
$deployment=(StoreRequest 'platform.localhost' GET '/api/deployment').Content|ConvertFrom-Json
AssertValue $deployment.data.isPlatformHost 'Platform host is distinct from merchant hosts'
$deployment=(StoreRequest 'alpha.localhost' GET '/api/deployment').Content|ConvertFrom-Json
AssertValue (!$deployment.data.isPlatformHost) 'Merchant host retains storefront entry'
