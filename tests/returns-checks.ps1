. (Join-Path $PSScriptRoot 'email-outbox-checks.ps1')
$category=(JsonRequest GET '/api/merchant/categories')[0]
$returnProduct=JsonRequest POST '/api/merchant/products' @{name='Return inspection product';categoryId=$category.id;basePrice=50;stockQuantity=10;isActive=$true;hasVariants=$false;showStockQuantity=$true}
$orderBody=@{}+$guest;$orderBody.idempotencyKey=[guid]::NewGuid().ToString();$orderBody.lines=@(@{productId=$returnProduct.id;quantity=4})
$placed=Request POST '/api/checkout' $newToken $orderBody
ExpectStatus $placed 200 'Return test customer checkout'
$orderNumber=($placed.Content|ConvertFrom-Json).data.orderNumber
$returnOrder=(JsonRequest GET '/api/merchant/orders?pageSize=100').items|Where-Object orderNumber -EQ $orderNumber
$view=Request GET "/api/returns/$orderNumber" $newToken
ExpectStatus $view 200 'Owner reads return eligibility'
$item=(($view.Content|ConvertFrom-Json).data.items)[0]
$attempt=@{requestKey=[guid]::NewGuid().ToString();orderItemId=$item.id;quantity=2;reason='Incorrect size delivered'}
ExpectStatus (Request POST "/api/returns/$orderNumber" $newToken $attempt) 409 'Undelivered return denied'
ExpectStatus (Request GET "/api/returns/$orderNumber" $customerToken) 404 'Another customer cannot see returns'
ExpectStatus (Request GET "/api/returns/$orderNumber") 404 'Anonymous return access denied'
$null=JsonRequest PUT "/api/merchant/orders/$($returnOrder.id)/status" @{status='Delivered'}
ExpectEqual (GetProduct $returnProduct.id).stockQuantity 6 'Delivery deducts purchased stock'
$created=Request POST "/api/returns/$orderNumber" $newToken $attempt
ExpectStatus $created 200 'Owner requests partial return'
$return=($created.Content|ConvertFrom-Json).data
$replay=Request POST "/api/returns/$orderNumber" $newToken $attempt
ExpectStatus $replay 200 'Return attempt can be replayed'
ExpectEqual ($replay.Content|ConvertFrom-Json).data.id $return.id 'Replay returns same request'
$excess=@{}+$attempt;$excess.requestKey=[guid]::NewGuid().ToString();$excess.quantity=3
ExpectStatus (Request POST "/api/returns/$orderNumber" $newToken $excess) 409 'Cannot return beyond purchased quantity'
ExpectEqual (GetProduct $returnProduct.id).stockQuantity 6 'Request alone does not restore stock'
function ReturnAction($r,$action,$restock=$false,$amount=0,$reference=''){
    Request POST "/api/merchant/returns/$($r.id)/actions" $adminToken @{version=$r.version;action=$action;notes='Inspected by test merchant';restock=$restock;refundAmount=$amount;refundReference=$reference}
}
ExpectStatus (ReturnAction $return 'Receive' $true) 409 'Cannot inspect before approval'
$approved=ReturnAction $return 'Approve';ExpectStatus $approved 200 'Merchant approves return';$return=($approved.Content|ConvertFrom-Json).data
ExpectEqual (GetProduct $returnProduct.id).stockQuantity 6 'Approval does not restore stock'
$received=ReturnAction $return 'Receive' $true;ExpectStatus $received 200 'Merchant receives resellable return'
ExpectEqual (GetProduct $returnProduct.id).stockQuantity 8 'Only returned quantity restored'
ExpectStatus (ReturnAction $return 'Receive' $true) 409 'Duplicate inspection cannot restore stock twice'
ExpectEqual (GetProduct $returnProduct.id).stockQuantity 8 'Replay leaves stock unchanged'
$return=($received.Content|ConvertFrom-Json).data
ExpectStatus (ReturnAction $return 'RecordRefund' $false ($return.maxRefund+1) 'TEST') 409 'Refund cap enforced'
ExpectStatus (ReturnAction $return 'RecordRefund' $false 10 '') 409 'Positive manual refund requires reference'
$completed=ReturnAction $return 'RecordRefund' $false 10 'CASH-TEST-RECEIPT';ExpectStatus $completed 200 'Record external cash refund'
$return=($completed.Content|ConvertFrom-Json).data
ExpectEqual $return.state 'Completed' 'Return completed after recorded settlement'
ExpectEqual $return.refundAmount 10 'Recorded refund preserved'
ExpectStatus (ReturnAction $return 'RecordRefund' $false 10 'CASH-TEST-RECEIPT') 409 'Completed refund cannot be repeated'
$history=JsonRequest GET "/api/merchant/returns/$($return.id)/history"
ExpectEqual @($history).Count 4 'Request approval inspection and refund audited'
ExpectStatus (Request PUT "/api/merchant/orders/$($returnOrder.id)/status" $adminToken @{status='Confirmed'}) 409 'Delivered fulfillment cannot be reopened after return'
$damaged=@{}+$attempt;$damaged.requestKey=[guid]::NewGuid().ToString();$damaged.reason='Damaged item'
$damagedResult=Request POST "/api/returns/$orderNumber" $newToken $damaged
ExpectStatus $damagedResult 200 'Remaining quantity may be returned separately'
$damagedReturn=($damagedResult.Content|ConvertFrom-Json).data
$approved=ReturnAction $damagedReturn 'Approve';ExpectStatus $approved 200 'Approve damaged return';$damagedReturn=($approved.Content|ConvertFrom-Json).data
$received=ReturnAction $damagedReturn 'Receive' $false;ExpectStatus $received 200 'Damaged goods received without restocking'
ExpectEqual (GetProduct $returnProduct.id).stockQuantity 8 'Damaged return does not increase sellable stock'
$guestView=Request GET "/api/returns/$orderNumber" '' $null (($placed.Content|ConvertFrom-Json).data.trackingToken)
ExpectStatus $guestView 200 'Private order code grants return access'
ExpectStatus (Request GET '/api/merchant/returns' $customerToken) 403 'Customer cannot manage returns'
$variantProduct=JsonRequest POST '/api/merchant/products' @{name='Variant return concurrency';categoryId=$category.id;basePrice=50;isActive=$true;hasVariants=$true;showStockQuantity=$true;variants=@(@{sizeId=$sizes[0].id;colorId=$colors[0].id;price=50;stockQuantity=10;isActive=$true},@{sizeId=$sizes[1].id;colorId=$colors[1].id;price=50;stockQuantity=7;isActive=$true})}
$variantId=$variantProduct.variants[0].id;$untouchedVariantId=$variantProduct.variants[1].id
$variantOrder=@{}+$guest;$variantOrder.idempotencyKey=[guid]::NewGuid().ToString();$variantOrder.lines=@(@{productId=$variantProduct.id;variantId=$variantId;quantity=2})
$variantConfirmation=JsonRequest POST '/api/checkout' $variantOrder
$merchantOrder=(JsonRequest GET '/api/merchant/orders?pageSize=100').items|Where-Object orderNumber -EQ $variantConfirmation.orderNumber
Status $merchantOrder.id 'Delivered'
$variantView=Request GET "/api/returns/$($variantConfirmation.orderNumber)" '' $null $variantConfirmation.trackingToken
ExpectStatus $variantView 200 'Guest return eligibility uses private code'
$item=(($variantView.Content|ConvertFrom-Json).data.items)[0]
$variantReturn=Request POST "/api/returns/$($variantConfirmation.orderNumber)" '' @{requestKey=[guid]::NewGuid().ToString();orderItemId=$item.id;quantity=1;reason='Variant inspection'} $variantConfirmation.trackingToken
ExpectStatus $variantReturn 200 'Guest may request variant return'
$return=($variantReturn.Content|ConvertFrom-Json).data
$approved=ReturnAction $return 'Approve';ExpectStatus $approved 200 'Variant return approved';$return=($approved.Content|ConvertFrom-Json).data
$raceJson=@{version=$return.version;action='Receive';notes='Concurrent variant inspection';restock=$true}|ConvertTo-Json -Compress
$raceUrl=$base+"/api/merchant/returns/$($return.id)/actions"
$raceResults=1..2|ForEach-Object -Parallel {
    $response=Invoke-WebRequest -Uri $using:raceUrl -Headers @{Authorization=('Bearer '+$using:adminToken)} -Method POST -ContentType application/json -Body $using:raceJson -SkipHttpErrorCheck -TimeoutSec 30
    [int]$response.StatusCode
} -ThrottleLimit 2
ExpectEqual @($raceResults|Where-Object {$_ -eq 200}).Count 1 'One concurrent inspection succeeds'
ExpectEqual @($raceResults|Where-Object {$_ -eq 409}).Count 1 'Duplicate concurrent inspection conflicts'
ExpectEqual (Stock $variantProduct.id $variantId) 9 'Returned size/color stock restored exactly once'
ExpectEqual (Stock $variantProduct.id $untouchedVariantId) 7 'Other size/color stock remains unchanged'
Write-Host 'Returns ownership, partial quantities, concurrent variant inspection, refund cap, audit and damaged-stock handling passed.'
