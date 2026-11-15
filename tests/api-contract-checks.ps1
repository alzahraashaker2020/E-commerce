function Shape($value) {
    if($null -eq $value){return 'null'}
    if($value -is [System.Collections.IDictionary]){$o=[ordered]@{};foreach($k in ($value.Keys|Sort-Object)){$o[$k]=Shape $value[$k]};return $o}
    if($value -is [array]){return ,@($value|ForEach-Object {Shape $_})}
    if($value -is [bool]){return 'boolean'}
    if($value -is [string]){return 'string'}
    return 'number'
}
$observed=[ordered]@{}
$cases=@(
    @{name='merchant_unauthorized';method='GET';path='/api/merchant/products';token=''},
    @{name='customer_forbidden';method='GET';path='/api/merchant/products';token=$customerToken},
    @{name='missing_currency';method='PUT';path='/api/merchant/currencies/2147483647';token=$adminToken;body=@{code='USD';name='Dollar';symbol='$';isActive=$true}},
    @{name='missing_status';method='PUT';path='/api/merchant/order-statuses/2147483647';token=$adminToken;body=@{name='Test';color='#123456';behavior='Pending';isActive=$true;displayOrder=0}},
    @{name='model_validation';method='POST';path='/api/merchant/products';token=$adminToken;body=@{}},
    @{name='cash_only';method='POST';path='/api/checkout';token='';body=@{paymentMethod='Card';idempotencyKey=[guid]::NewGuid();guestName='Test';guestEmail='test@example.test';addressLine1='Test';city='Test';stateRegion='Test';postalCode='1';country='Test';contactPhone='12345678';areaId=1;lines=@(@{productId=1;quantity=1})}},
    @{name='settings';method='GET';path='/api/settings';token=''},
    @{name='currencies';method='GET';path='/api/merchant/currencies';token=$adminToken},
    @{name='governorates';method='GET';path='/api/merchant/delivery/governorates';token=$adminToken},
    @{name='areas';method='GET';path='/api/merchant/delivery/areas';token=$adminToken},
    @{name='statuses';method='GET';path='/api/merchant/order-statuses';token=$adminToken}
)
foreach($case in $cases){
    $r=Request $case.method $case.path $case.token $case.body
    $content=if($r.Content -is [byte[]]){[Text.Encoding]::UTF8.GetString($r.Content)}else{[string]$r.Content}
    $body=if($content){$content|ConvertFrom-Json -AsHashtable}else{$null}
    $observed[$case.name]=[ordered]@{status=[int]$r.StatusCode;shape=Shape $body;message=if($body -is [System.Collections.IDictionary]){$body.message}else{$null};checkoutOutcome=[string]$r.Headers['X-Checkout-Outcome'];contentType=[string]$r.Headers['Content-Type']}
}
$path=Join-Path $PSScriptRoot 'fixtures/refactoring/contracts.json'
$json=$observed|ConvertTo-Json -Depth 30
if($env:SHOP_CAPTURE_CONTRACTS -eq '1') {New-Item -ItemType Directory -Path (Split-Path $path) -Force|Out-Null;$json|Set-Content $path;Write-Host 'Captured baseline HTTP contracts'}
else {if($json -ne (Get-Content $path -Raw).TrimEnd()){throw 'HTTP contract snapshot changed; review status/body/casing/headers before continuing.'};Write-Host 'HTTP contract snapshots unchanged'}
