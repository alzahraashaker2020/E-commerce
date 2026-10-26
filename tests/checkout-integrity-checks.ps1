. (Join-Path $PSScriptRoot 'product-order-checks.ps1')
$currency=(JsonRequest GET '/api/settings').currencyCode
$gov=JsonRequest POST '/api/merchant/delivery/governorates' @{name='Checkout integrity';isActive=$true}
$area=JsonRequest POST '/api/merchant/delivery/areas' @{name='Integrity area';governorateId=$gov.id;currencyCode=$currency;shippingFee=10;isActive=$true}
$category=(JsonRequest GET '/api/merchant/categories')[0]
$productBody=@{name='Integrity product';categoryId=$category.id;basePrice=100;stockQuantity=30;isActive=$true;hasVariants=$false;showStockQuantity=$true}
$product=JsonRequest POST '/api/merchant/products' $productBody
$body=@{idempotencyKey=[guid]::NewGuid().ToString();areaId=$area.id;guestName='Untrusted name';guestEmail='jordan@demo.local';addressLine1='Integrity street';city='Cairo';stateRegion='Cairo';postalCode='11511';country='Egypt';contactPhone='01000000000';paymentMethod='Cash';lines=@(@{productId=$product.id;quantity=1})}
$before=(JsonRequest GET '/api/merchant/orders').totalCount
$card=@{}+$body; $card.paymentMethod='Card'
ExpectStatus (Request POST '/api/checkout' '' $card) 400 'Unconfigured card rejected'
ExpectEqual (JsonRequest GET '/api/merchant/orders').totalCount $before 'Card rejection saves no order'
$invalid=@{}+$body; $invalid.idempotencyKey=[guid]::Empty.ToString()
ExpectStatus (Request POST '/api/checkout' '' $invalid) 400 'Empty idempotency key rejected'
ExpectStatus (Request POST '/api/checkout' 'invalid-token' $body) 401 'Invalid session cannot become a guest order'
ExpectStatus (Request POST '/api/checkout' $adminToken $body) 403 'Merchant session cannot impersonate customer checkout'
$placed=Request POST '/api/checkout' $newToken $body
ExpectStatus $placed 200 'Customer checkout'
$confirmation=($placed.Content|ConvertFrom-Json).data
ExpectEqual $confirmation.paymentStatus 'CashOnDelivery' 'Cash is not marked paid'
$repeat=Request POST '/api/checkout' $newToken $body
ExpectStatus $repeat 200 'Identical retry succeeds'
ExpectEqual ($repeat.Content|ConvertFrom-Json).data.orderNumber $confirmation.orderNumber 'Retry returns the original order'
ExpectEqual (JsonRequest GET '/api/merchant/orders').totalCount ($before+1) 'Exactly one order is saved'
$ownOrders=Request GET '/api/account/orders' $newToken
ExpectStatus $ownOrders 200 'Customer order history'
ExpectEqual @((($ownOrders.Content|ConvertFrom-Json).data)|Where-Object orderNumber -EQ $confirmation.orderNumber).Count 1 'Order belongs to authenticated customer'
$otherOrders=Request GET '/api/account/orders' $customerToken
ExpectEqual @((($otherOrders.Content|ConvertFrom-Json).data)|Where-Object orderNumber -EQ $confirmation.orderNumber).Count 0 'Order is not linked by typed email'
ExpectStatus (Request POST '/api/checkout' $customerToken $body) 409 'Other customer cannot replay an attempt'
ExpectStatus (Request POST '/api/checkout' '' $body) 409 'Guest cannot replay customer receipt'
$changed=@{}+$body; $changed.contactPhone='01000000001'
ExpectStatus (Request POST '/api/checkout' $newToken $changed) 409 'Attempt cannot be reused with altered details'
$productBody.basePrice=250
$null=JsonRequest PUT "/api/merchant/products/$($product.id)" $productBody
$repriced=Request POST '/api/checkout' $newToken $body
ExpectStatus $repriced 200 'Retry after price change'
ExpectEqual ($repriced.Content|ConvertFrom-Json).data.total $confirmation.total 'Replay preserves original total'
$mail=Join-Path $runDir "store-data/default/sent-emails/$($confirmation.orderNumber).txt"
for($wait=0;$wait -lt 100 -and !(Test-Path $mail);$wait++){Start-Sleep -Milliseconds 100}
ExpectEqual @([regex]::Matches([IO.File]::ReadAllText($mail),'Subject:')).Count 1 'Replay does not send another confirmation'
$guest=@{}+$body; $guest.idempotencyKey=[guid]::NewGuid().ToString()
$guestResult=Request POST '/api/checkout' '' $guest
ExpectStatus $guestResult 200 'Guest checkout retained'
$guestNumber=($guestResult.Content|ConvertFrom-Json).data.orderNumber
$otherOrders=Request GET '/api/account/orders' $customerToken
ExpectEqual @((($otherOrders.Content|ConvertFrom-Json).data)|Where-Object orderNumber -EQ $guestNumber).Count 0 'Guest email does not claim an existing customer account'

$race=@{}+$body; $race.idempotencyKey=[guid]::NewGuid().ToString()
$raceJson=$race|ConvertTo-Json -Depth 10 -Compress
$countBeforeRace=(JsonRequest GET '/api/merchant/orders').totalCount
$raceResults=1..4 | ForEach-Object -Parallel {
    $r=Invoke-WebRequest -Uri ($using:base+'/api/checkout') -Method POST -ContentType application/json -Body $using:raceJson -SkipHttpErrorCheck -TimeoutSec 30
    [pscustomobject]@{StatusCode=[int]$r.StatusCode;Number=($r.Content|ConvertFrom-Json).data.orderNumber}
} -ThrottleLimit 4
foreach($r in $raceResults){ExpectStatus $r 200 'Concurrent duplicate checkout'}
ExpectEqual @($raceResults.Number|Select-Object -Unique).Count 1 'Concurrent requests receive one order number'
ExpectEqual (JsonRequest GET '/api/merchant/orders').totalCount ($countBeforeRace+1) 'Concurrent requests save one order'
Stop-Process -Id $apiProcess.Id; $apiProcess.WaitForExit()
$apiProcess=Start-Process dotnet -ArgumentList @($dll,'--contentRoot',('"'+$runDir+'"')) -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $runDir 'restart-checkout.log') -RedirectStandardError (Join-Path $runDir 'restart-checkout-error.log')
for($i=0;$i -lt 60;$i++){try{if((Request GET '/api/settings').StatusCode -eq 200){break}}catch{};Start-Sleep -Milliseconds 200}
$afterRestart=Request POST '/api/checkout' '' $race
ExpectStatus $afterRestart 200 'Receipt survives API restart'
ExpectEqual ($afterRestart.Content|ConvertFrom-Json).data.orderNumber $raceResults[0].Number 'Restart does not create another order'
Write-Output 'Checkout identity, cash payment, durable retries and concurrent submission checks passed.'
