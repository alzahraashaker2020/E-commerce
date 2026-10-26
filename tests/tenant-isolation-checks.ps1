# Run through security-smoke.ps1 -AdditionalChecks tests/tenant-isolation-checks.ps1.
function StoreRequest($hostName, $method, $path, $token = '', $body = $null) {
    $headers = @{Host=$hostName}
    if ($token) { $headers.Authorization = 'Bearer ' + $token }
    $arguments = @{ Uri=$base+$path; Method=$method; Headers=$headers; SkipHttpErrorCheck=$true; TimeoutSec=20 }
    if ($null -ne $body) { $arguments.ContentType='application/json'; $arguments.Body=$body | ConvertTo-Json -Depth 12 -Compress }
    Invoke-WebRequest @arguments
}
function StoreJson($hostName, $method, $path, $token = '', $body = $null, $status = 200) {
    $response = StoreRequest $hostName $method $path $token $body
    ExpectStatus $response $status "$hostName $method $path"
    if ($status -eq 200) { return ($response.Content | ConvertFrom-Json).data }
}
function AssertValue($condition, $label) { if (!$condition) { throw $label }; $script:checks++ }

$platformLogin = Request POST '/api/platform/auth/login' '' @{username='platform-test';password=$envValues.Platform__BootstrapPassword}
ExpectStatus $platformLogin 200 'Platform sign in'
$platformToken = ($platformLogin.Content | ConvertFrom-Json).data.token
$planResponse = Request POST '/api/platform/plans' $platformToken @{name='Isolation plan';priceMinor=10000;currency='EGP';periodMonths=1;trialDays=7;isActive=$true}
ExpectStatus $planResponse 200 'Create isolation plan'
$isolationPlan = ($planResponse.Content | ConvertFrom-Json).data
ExpectStatus (Request GET '/api/platform/stores' $adminToken) 401 'Merchant cannot access platform registry'
ExpectStatus (Request GET '/api/platform/stores' $customerToken) 401 'Customer cannot access platform registry'
ExpectStatus (Request GET '/api/merchant/products' $platformToken) 401 'Platform operator cannot read merchant products'
ExpectStatus (Request GET '/api/platform/stores') 401 'Anonymous cannot access registry'

$stores = @{}
$tokens = @{}
foreach ($key in @('Alpha','Beta')) {
    $hostName=$key.ToLowerInvariant()+'.localhost'
    $payload=@{name=$key;host=$hostName;connectionKey=$key;ownerUsername='merchant';ownerEmail='owner@example.invalid';ownerPassword='Merchant-Isolation-12345'}
    $created = Request POST '/api/platform/stores' $platformToken $payload
    ExpectStatus $created 200 "Provision $key"
    $stores[$key]=($created.Content | ConvertFrom-Json).data
    AssertValue ($stores[$key].state -eq 'Active') 'Provisioned store must be ready'
    $firstAction=if($key -eq 'Alpha'){'Trial'}else{'Assign'}
    ExpectStatus (Request POST "/api/platform/stores/$($stores[$key].id)/subscription" $platformToken @{action=$firstAction;planId=$isolationPlan.id;version=0}) 200 'Activate store subscription'
    $login=StoreJson $hostName POST '/api/merchant/auth/login' '' @{username='merchant';password='Merchant-Isolation-12345'}
    AssertValue ($login.role -eq 'Merchant') 'Store role is Merchant'
    $tokens[$key]=$login.token
    $products=StoreJson $hostName GET '/api/merchant/products' $login.token
    AssertValue (@($products).Count -eq 0) 'New merchant starts without demo products'
    $null=StoreJson $hostName POST '/api/merchant/auth/login' '' @{username='admin';password='Admin@12345'} 401
    $category=StoreJson $hostName POST '/api/merchant/categories' $login.token @{name='Own category';isActive=$true}
    $product=StoreJson $hostName POST '/api/merchant/products' $login.token @{name="$key product";categoryId=$category.id;basePrice=100;stockQuantity=37;isActive=$true;showStockQuantity=$true;hasVariants=$false}
    $stores[$key] | Add-Member -NotePropertyName ProductId -NotePropertyValue $product.id
    $customer=StoreJson $hostName POST '/api/account/register' '' @{email='same-customer@example.invalid';fullName="$key customer";password='Customer-Test-12345'}
    $tokens[$key+'Customer']=$customer.token
}
AssertValue ($stores.Alpha.ProductId -eq $stores.Beta.ProductId) 'Independent databases allow overlapping product IDs'
foreach ($key in @('Alpha','Beta')) {
    $other=if($key -eq 'Alpha'){'Beta'}else{'Alpha'}
    $hostName=$key.ToLowerInvariant()+'.localhost'
    $null=StoreJson $hostName GET '/api/merchant/products' $tokens[$other] $null 401
    $null=StoreJson $hostName PUT "/api/merchant/products/$($stores[$key].ProductId)" $tokens[$other] @{name='Cross-store change'} 401
    $null=StoreJson $hostName GET '/api/account/profile' $tokens[$other+'Customer'] $null 401
    $null=StoreJson $hostName GET '/api/merchant/products' $tokens[$key+'Customer'] $null 403
    $null=StoreJson $hostName GET '/api/platform/stores' $tokens[$key] $null 401
    $profile=StoreJson $hostName GET '/api/account/profile' $tokens[$key+'Customer']
    AssertValue ($profile.fullName -eq "$key customer") 'Customer IDs do not cross stores'
    $product=StoreJson $hostName GET "/api/catalog/$($stores[$key].ProductId)"
    AssertValue ($product.name -eq "$key product" -and $product.stockQuantity -eq 37) 'Product data stays in its store'
}
$alias=Request POST '/api/platform/stores' $platformToken @{name='Alias';host='alias.localhost';connectionKey='Alias';ownerUsername='m';ownerEmail='m@example.invalid';ownerPassword='Merchant-Isolation-12345'}
ExpectStatus $alias 409 'Different references cannot share the same database'
$unknown=StoreRequest 'unknown.localhost' GET '/api/catalog'
ExpectStatus $unknown 404 'Unregistered host has no default-store fallback'

# A store upload must be served only from its own host.
$http=[Net.Http.HttpClient]::new()
$http.DefaultRequestHeaders.Host='alpha.localhost'
$http.DefaultRequestHeaders.Authorization=[Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer',$tokens.Alpha)
$multipart=[Net.Http.MultipartFormDataContent]::new()
$file=[Net.Http.ByteArrayContent]::new([Convert]::FromBase64String('iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAYAAABytg0kAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAQSURBVBhXY/jPwPCfARkAAB7zAf+x9MCaAAAAAElFTkSuQmCC'))
$file.Headers.ContentType=[Net.Http.Headers.MediaTypeHeaderValue]::new('image/png')
$multipart.Add($file,'file','sample.png')
try {
    $response=$http.PostAsync("$base/api/merchant/uploads",$multipart).GetAwaiter().GetResult()
    AssertValue ([int]$response.StatusCode -eq 200) 'Merchant upload succeeds'
    $path=($response.Content.ReadAsStringAsync().GetAwaiter().GetResult() | ConvertFrom-Json).data
} finally { $multipart.Dispose(); $http.Dispose() }
$null=StoreJson 'alpha.localhost' GET $path
$null=StoreJson 'beta.localhost' GET $path '' $null 404
$null=StoreJson 'beta.localhost' GET ($path.Replace('/uploads/', '/uploads%5C')) '' $null 404
$null=StoreJson '127.0.0.1' GET $path '' $null 404
$suspend=Request PUT "/api/platform/stores/$($stores.Alpha.id)/state" $platformToken @{state='Suspended'}
ExpectStatus $suspend 200 'Suspend store'
$null=StoreJson 'alpha.localhost' GET '/api/catalog' '' $null 403
$null=StoreJson 'alpha.localhost' GET '/api/merchant/products' $tokens.Alpha $null 403
$null=StoreJson 'beta.localhost' GET '/api/merchant/products' $tokens.Beta
ExpectStatus (Request PUT "/api/platform/stores/$($stores.Alpha.id)/state" $platformToken @{state='Active'}) 200 'Resume store'
$null=StoreJson 'alpha.localhost' GET '/api/merchant/products' $tokens.Alpha
ExpectStatus (Request GET '/api/admin/products' $adminToken) 404 'Obsolete API cannot bypass merchant route'
Write-Host 'Tenant isolation and platform/merchant permissions passed.'

# Subscription terms, quota enforcement, trial abuse, expiry and manual renewal.
$alphaId=$stores.Alpha.id
$sub=StoreJson 'alpha.localhost' GET '/api/merchant/subscription' $tokens.Alpha
AssertValue ($sub.status -eq 'Trialing' -and $sub.canSell) 'Trial enables selling'
AssertValue ((StoreRequest 'alpha.localhost' GET '/api/merchant/subscription' $tokens.Alpha).Content -match '"endsAtUtc"\s*:\s*"[^"]+Z"') 'Subscription dates explicitly use UTC'
$drafts=Request GET '/api/platform/plans' $platformToken
ExpectStatus $drafts 200 'List plans'
$draftPlan=(($drafts.Content | ConvertFrom-Json).data | Where-Object name -EQ 'Starter')
AssertValue (!$draftPlan.isActive) 'Starter draft is not enabled automatically'
ExpectStatus (Request POST "/api/platform/stores/$alphaId/subscription" $platformToken @{action='Assign';planId=$draftPlan.id;version=$sub.subscription.version}) 400 'Disabled plan cannot be assigned'
ExpectStatus (Request POST '/api/platform/plans' $tokens.Alpha @{name='Attack';priceMinor=0;currency='EGP';periodMonths=1}) 401 'Merchant cannot change plans'
ExpectStatus (Request POST '/api/platform/plans' $platformToken @{name='Bad';priceMinor=-1;currency='EGP';periodMonths=1}) 400 'Negative plan price rejected'
ExpectStatus (Request POST '/api/platform/plans' $platformToken @{name='Bad';priceMinor=0;currency='EGP';periodMonths=0}) 400 'Invalid billing period rejected'
$changed=Request PUT "/api/platform/plans/$($isolationPlan.id)" $platformToken @{name='Updated isolation plan';priceMinor=25000;currency='USD';periodMonths=2;trialDays=10;maxProducts=1;isActive=$true;version=$isolationPlan.version}
ExpectStatus $changed 200 'Edit plan for future activations'
$newPlan=($changed.Content | ConvertFrom-Json).data
$sub=StoreJson 'alpha.localhost' GET '/api/merchant/subscription' $tokens.Alpha
AssertValue ($sub.subscription.priceMinor -eq 10000 -and $sub.subscription.currency -eq 'EGP' -and $null -eq $sub.subscription.maxProducts) 'Plan edits preserve existing subscription snapshot'
ExpectStatus (Request POST "/api/platform/stores/$alphaId/subscription" $platformToken @{action='Trial';planId=$newPlan.id;version=$sub.subscription.version}) 409 'Trial cannot be repeated'
$assigned=Request POST "/api/platform/stores/$alphaId/subscription" $platformToken @{action='Assign';planId=$newPlan.id;version=$sub.subscription.version;reference='Manual approval test'}
ExpectStatus $assigned 200 'Assign current plan terms'
$current=($assigned.Content | ConvertFrom-Json).data.subscription
$null=StoreJson 'alpha.localhost' POST '/api/merchant/products' $tokens.Alpha @{name='Over quota';categoryId=1;basePrice=100;isActive=$true} 403
AssertValue (@(StoreJson 'alpha.localhost' GET '/api/merchant/products' $tokens.Alpha).Count -eq 1) 'Quota rejection preserves existing product'
ExpectStatus (Request POST "/api/platform/stores/$alphaId/subscription" $platformToken @{action='Renew';planId=$newPlan.id;version=1}) 409 'Stale renew cannot extend subscription twice'
$renewed=Request POST "/api/platform/stores/$alphaId/subscription" $platformToken @{action='Renew';planId=$newPlan.id;version=$current.version}
ExpectStatus $renewed 200 'Manual renewal succeeds'
$renewedSub=($renewed.Content | ConvertFrom-Json).data.subscription
AssertValue ([datetime]$renewedSub.endsAtUtc -eq ([datetime]$current.endsAtUtc).AddMonths(2)) 'Paid renewal extends from the existing expiry'
ExpectStatus (Request POST "/api/platform/stores/$alphaId/subscription" $platformToken @{action='Cancel';version=$renewedSub.version}) 200 'Cancel subscription'
$null=StoreJson 'alpha.localhost' POST '/api/checkout' '' @{} 403
$null=StoreJson 'alpha.localhost' POST '/api/merchant/categories' $tokens.Alpha @{name='Blocked';isActive=$true} 403
$cancelled=StoreJson 'alpha.localhost' GET '/api/merchant/subscription' $tokens.Alpha
AssertValue (!$cancelled.canSell -and $cancelled.status -eq 'Cancelled') 'Cancelled merchant can view subscription'
$null=StoreJson 'alpha.localhost' GET "/api/catalog/$($stores.Alpha.ProductId)"
$null=StoreJson 'beta.localhost' GET '/api/merchant/subscription' $tokens.Beta
$resume=Request POST "/api/platform/stores/$alphaId/subscription" $platformToken @{action='Renew';planId=$newPlan.id;version=$cancelled.subscription.version}
ExpectStatus $resume 200 'Renew cancelled subscription from now'
& dotnet (Join-Path $repoRoot 'tests/Shop.SchemaChecks/bin/Debug/net7.0/Shop.SchemaChecks.dll') --expire-test-subscription (Join-Path $runDir 'platform.db') $alphaId
if($LASTEXITCODE -ne 0){throw 'Unable to expire isolated test subscription'}
$expired=StoreJson 'alpha.localhost' GET '/api/merchant/subscription' $tokens.Alpha
AssertValue ($expired.status -eq 'Expired' -and !$expired.canSell) 'Expiry is enforced at request time without a background job'
$null=StoreJson 'alpha.localhost' POST '/api/checkout' '' @{} 403
$null=StoreJson 'alpha.localhost' POST '/api/merchant/categories' $tokens.Alpha @{name='Still blocked';isActive=$true} 403
ExpectStatus (Request POST "/api/platform/stores/$alphaId/subscription" $platformToken @{action='Renew';planId=$newPlan.id;version=$expired.subscription.version}) 200 'Expired subscription can be renewed'
$history=Request GET "/api/platform/stores/$alphaId/subscription/history" $platformToken
ExpectStatus $history 200 'Subscription history available to platform'
AssertValue (@(($history.Content | ConvertFrom-Json).data).Count -ge 6) 'Each accepted subscription action is audited'
$all=Request GET '/api/platform/subscriptions' $platformToken
ExpectStatus $all 200 'Platform can review all subscriptions'
Write-Host 'Subscription plans, snapshots, quotas, trials, cancellation, expiry and renewal passed.'

# Exercise checkout against the selected tenant provider (SQLite or PostgreSQL).
$currency=(StoreJson 'alpha.localhost' GET '/api/settings').currencyCode
$gov=StoreJson 'alpha.localhost' POST '/api/merchant/delivery/governorates' $tokens.Alpha @{name='Checkout area';isActive=$true}
$area=StoreJson 'alpha.localhost' POST '/api/merchant/delivery/areas' $tokens.Alpha @{name='Checkout street';governorateId=$gov.id;currencyCode=$currency;shippingFee=0;isActive=$true}
$checkoutBody=@{idempotencyKey=[guid]::NewGuid().ToString();areaId=$area.id;guestName='Untrusted';guestEmail='fake@example.invalid';addressLine1='Checkout street';city='Cairo';stateRegion='Cairo';postalCode='11511';country='Egypt';contactPhone='01000000000';paymentMethod='Cash';lines=@(@{productId=$stores.Alpha.ProductId;quantity=1})}
$placed=StoreJson 'alpha.localhost' POST '/api/checkout' $tokens.AlphaCustomer $checkoutBody
$again=StoreJson 'alpha.localhost' POST '/api/checkout' $tokens.AlphaCustomer $checkoutBody
AssertValue ($placed.orderNumber -eq $again.orderNumber -and $placed.paymentStatus -eq 'CashOnDelivery') 'Tenant checkout replays the original cash order'
$own=StoreJson 'alpha.localhost' GET '/api/account/orders' $tokens.AlphaCustomer
AssertValue (@($own|Where-Object orderNumber -EQ $placed.orderNumber).Count -eq 1) 'Tenant customer owns their checkout'
$null=StoreJson 'alpha.localhost' POST '/api/checkout' $tokens.BetaCustomer $checkoutBody 401
$raceBody=@{}+$checkoutBody; $raceBody.idempotencyKey=[guid]::NewGuid().ToString()
$raceJson=$raceBody|ConvertTo-Json -Depth 10 -Compress
$raceResults=1..4|ForEach-Object -Parallel {
    $r=Invoke-WebRequest -Uri ($using:base+'/api/checkout') -Headers @{Host='alpha.localhost'} -Method POST -ContentType application/json -Body $using:raceJson -SkipHttpErrorCheck -TimeoutSec 30
    [pscustomobject]@{StatusCode=[int]$r.StatusCode;Number=($r.Content|ConvertFrom-Json).data.orderNumber}
} -ThrottleLimit 4
foreach($r in $raceResults){ExpectStatus $r 200 'Concurrent tenant checkout retry'}
AssertValue (@($raceResults.Number|Select-Object -Unique).Count -eq 1) 'Tenant concurrent retries create one order'
Stop-Process -Id $apiProcess.Id; $apiProcess.WaitForExit()
$apiProcess=Start-Process dotnet -ArgumentList @($dll,'--contentRoot',('"'+$runDir+'"')) -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $runDir 'tenant-restart.log') -RedirectStandardError (Join-Path $runDir 'tenant-restart-error.log')
for($i=0;$i -lt 60;$i++){try{if((Request GET '/api/settings').StatusCode -eq 200){break}}catch{};Start-Sleep -Milliseconds 200}
$afterRestart=StoreJson 'alpha.localhost' POST '/api/checkout' $tokens.AlphaCustomer $checkoutBody
AssertValue ($afterRestart.orderNumber -eq $placed.orderNumber) 'Tenant checkout receipt persists across restart'
$withoutProof=StoreRequest 'alpha.localhost' GET ("/api/ordertracking/"+$placed.orderNumber)
ExpectStatus $withoutProof 404 'Tenant order number alone is private'
$withProof=Invoke-WebRequest -Uri ($base+'/api/ordertracking/'+$placed.orderNumber) -Headers @{Host='alpha.localhost';'X-Order-Tracking-Token'=$placed.trackingToken} -SkipHttpErrorCheck
ExpectStatus $withProof 200 'Private code works in its own store'
$otherStoreProof=Invoke-WebRequest -Uri ($base+'/api/ordertracking/'+$placed.orderNumber) -Headers @{Host='beta.localhost';'X-Order-Tracking-Token'=$placed.trackingToken} -SkipHttpErrorCheck
ExpectStatus $otherStoreProof 404 'Tracking code cannot cross stores'
$ownerTracking=StoreRequest 'alpha.localhost' GET ("/api/ordertracking/"+$placed.orderNumber) $tokens.AlphaCustomer
ExpectStatus $ownerTracking 200 'Authenticated tenant customer can track their own order'
Write-Host 'Tenant checkout ownership and concurrency checks passed.'

# Account email actions must stay within their store database too.
$emailFiles=Get-ChildItem (Join-Path $runDir "store-data/$($stores.Alpha.id)/sent-emails") -Filter 'message-*.txt'
$verification=''
foreach($file in $emailFiles){$text=[IO.File]::ReadAllText($file.FullName);if($text.Contains('Subject: Verify your store email')){$verification=[regex]::Match($text,'#token=([A-Za-z0-9_-]{43})').Groups[1].Value;break}}
AssertValue ($verification.Length -eq 43) 'Tenant verification is delivered to private store message directory'
$null=StoreJson 'beta.localhost' POST '/api/account/verify-email' '' @{token=$verification} 400
$null=StoreJson 'alpha.localhost' POST '/api/account/verify-email' '' @{token=$verification}
$verified=StoreJson 'alpha.localhost' GET '/api/account/email-status' $tokens.AlphaCustomer
AssertValue $verified.verified 'Verification is stored in the correct tenant database'
$betaStatus=StoreJson 'beta.localhost' GET '/api/account/email-status' $tokens.BetaCustomer
AssertValue (!$betaStatus.verified) 'Verifying one store account does not verify a same-ID account elsewhere'
Write-Host 'Tenant email verification isolation passed.'

$alphaMessages=StoreJson 'alpha.localhost' GET '/api/merchant/notifications/messages' $tokens.Alpha
$betaMessages=StoreJson 'beta.localhost' GET '/api/merchant/notifications/messages' $tokens.Beta
$alphaMessage=@($alphaMessages)[0]
AssertValue (@($betaMessages|Where-Object id -EQ $alphaMessage.id).Count -eq 0) 'Email histories remain in each store database'
$null=StoreJson 'beta.localhost' POST "/api/merchant/notifications/messages/$($alphaMessage.id)/retry" $tokens.Beta @{} 409
$order=(StoreJson 'alpha.localhost' GET '/api/merchant/orders?pageSize=100' $tokens.Alpha).items|Where-Object orderNumber -EQ $placed.orderNumber
$null=StoreJson 'alpha.localhost' PUT "/api/merchant/orders/$($order.id)/status" $tokens.Alpha @{status='Delivered'}
$eligible=StoreJson 'alpha.localhost' GET "/api/returns/$($placed.orderNumber)" $tokens.AlphaCustomer
$return=StoreJson 'alpha.localhost' POST "/api/returns/$($placed.orderNumber)" $tokens.AlphaCustomer @{requestKey=[guid]::NewGuid().ToString();orderItemId=$eligible.items[0].id;quantity=1;reason='Tenant isolation return'}
$null=StoreJson 'beta.localhost' GET "/api/returns/$($placed.orderNumber)" $tokens.BetaCustomer $null 404
$otherReturns=StoreJson 'beta.localhost' GET '/api/merchant/returns' $tokens.Beta
AssertValue (@($otherReturns|Where-Object id -EQ $return.id).Count -eq 0) 'Return records stay within their store'
$null=StoreJson 'beta.localhost' POST "/api/merchant/returns/$($return.id)/actions" $tokens.Beta @{version=$return.version;action='Approve';notes='Cross-store attempt'} 409
$approved=StoreJson 'alpha.localhost' POST "/api/merchant/returns/$($return.id)/actions" $tokens.Alpha @{version=$return.version;action='Approve';notes='Approved in original store'}
$received=StoreJson 'alpha.localhost' POST "/api/merchant/returns/$($return.id)/actions" $tokens.Alpha @{version=$approved.version;action='Receive';notes='Inspected in original store';restock=$true}
AssertValue $received.restocked 'Tenant return restores its original stock row'
$alphaProduct=StoreJson 'alpha.localhost' GET "/api/catalog/$($stores.Alpha.ProductId)"
$betaProduct=StoreJson 'beta.localhost' GET "/api/catalog/$($stores.Beta.ProductId)"
AssertValue ($alphaProduct.stockQuantity -eq 37 -and $betaProduct.stockQuantity -eq 37) 'Return cannot change the other store stock'
Write-Host 'Tenant durable email history and return isolation passed.'

$alphaBackupResponse=Request POST ("/api/platform/operations/backups/"+$stores.Alpha.id) $platformToken
ExpectStatus $alphaBackupResponse 200 'Platform creates isolated tenant backup'
$alphaBackup=($alphaBackupResponse.Content|ConvertFrom-Json).data
$checkResponse=Request POST ("/api/platform/operations/backups/"+$alphaBackup.id+'/verify') $platformToken
ExpectStatus $checkResponse 200 'Tenant database restores in a separate validation database'
$check=($checkResponse.Content|ConvertFrom-Json).data
AssertValue ($check.storeId -eq $stores.Alpha.id -and $check.products -eq 1 -and $null -ne $check.lastVerifiedAtUtc) 'Correct store identity and restored rows verified'
$betaAfter=StoreJson 'beta.localhost' GET "/api/catalog/$($stores.Beta.ProductId)"
AssertValue ($betaAfter.name -eq 'Beta product' -and $betaAfter.stockQuantity -eq 37) 'Restore rehearsal never changes another store'
$alphaActivity=StoreJson 'alpha.localhost' GET '/api/merchant/activity' $tokens.Alpha
$betaActivity=StoreJson 'beta.localhost' GET '/api/merchant/activity' $tokens.Beta
AssertValue (@($alphaActivity).Count -gt 0) 'Merchant has a persisted activity history'
$alphaIds=@($alphaActivity|ForEach-Object id)
AssertValue (@($betaActivity|Where-Object {$_.id -in $alphaIds}).Count -eq 0) 'Merchant audit history cannot cross tenant boundaries'
Write-Host 'Tenant snapshot restore and activity isolation passed.'
$design=StoreJson 'alpha.localhost' GET '/api/merchant/storefront' $tokens.Alpha
$design.theme='kids';$design.brandName='Alpha kids';$design.newsletterEnabled=$true
$design.translations=@{'Alpha product'='منتج متجر ألفا';'Alpha kids'='أطفال ألفا'}
$design.sections=@(@{title='Alpha collection';eyebrow='New';subtitle='Tenant test';categoryId=$null;productIds=@($stores.Alpha.ProductId)})
$null=StoreJson 'alpha.localhost' PUT '/api/merchant/storefront' $tokens.Alpha $design
$alphaDesign=StoreJson 'alpha.localhost' GET '/api/storefront'
$betaDesign=StoreJson 'beta.localhost' GET '/api/storefront'
AssertValue ($alphaDesign.brandName -eq 'Alpha kids' -and $betaDesign.theme -eq 'classic') 'Storefront designs stay within their merchant database'
$alphaCatalog=StoreJson 'alpha.localhost' GET '/api/storefront/products'
$betaCatalog=StoreJson 'beta.localhost' GET '/api/storefront/products'
AssertValue ($alphaCatalog.items[0].name -eq 'Alpha product' -and $betaCatalog.items[0].name -eq 'Beta product') 'Themed product feeds remain tenant-bound'
$null=StoreJson 'alpha.localhost' POST '/api/storefront/subscribe' '' @{email='alpha-newsletter@example.invalid';consent=$true}
$betaSubscribers=StoreJson 'beta.localhost' GET '/api/merchant/storefront/subscribers' $tokens.Beta
AssertValue (@($betaSubscribers).Count -eq 0) 'Newsletter subscribers cannot cross stores'
AssertValue ($alphaDesign.translations.'Alpha product' -eq 'منتج متجر ألفا' -and $null -eq $betaDesign.translations.'Alpha product') 'Translations cannot cross store databases'
Write-Host 'Managed storefront themes, product feeds and newsletter isolation passed.'
$revoked=Request POST ("/api/platform/operations/stores/"+$stores.Alpha.id+'/revoke-sessions') $platformToken
ExpectStatus $revoked 200 'Platform can revoke store sessions before recovery'
$null=StoreJson 'alpha.localhost' GET '/api/merchant/products' $tokens.Alpha $null 401
$null=StoreJson 'alpha.localhost' GET '/api/account/profile' $tokens.AlphaCustomer $null 401
$null=StoreJson 'beta.localhost' GET '/api/merchant/products' $tokens.Beta
Write-Host 'Store recovery session revocation is isolated from other merchants.'
