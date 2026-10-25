# Run through security-smoke.ps1 -AdditionalChecks tests/product-order-checks.ps1.
function ExpectEqual($actual, $expected, $name) {
    if ($actual -ne $expected) { throw "$name expected '$expected', received '$actual'" }
    $script:checks++
}
function JsonRequest($method, $path, $body = $null, $expected = 200) {
    $response = Request $method $path $adminToken $body
    ExpectStatus $response $expected "$method $path"
    if ($expected -eq 200) { return ($response.Content | ConvertFrom-Json).data }
}
function GetProduct($id) { JsonRequest GET "/api/catalog/$id" }
function Status($id, $value, $expected = 200) { $null = JsonRequest PUT "/api/admin/orders/$id/status" @{status=$value} $expected }
function NewOrder($lines) {
    $currentSettings = JsonRequest GET '/api/settings'
    if ($checkoutArea.currencyCode -ne $currentSettings.currencyCode) {
        $script:checkoutArea = JsonRequest PUT "/api/admin/delivery/areas/$($checkoutArea.id)" @{
            name='Test delivery';governorateId=$checkoutGovernorate.id;shippingFee=0;currencyCode=$currentSettings.currencyCode;isActive=$true
        }
    }
    $result = JsonRequest POST '/api/checkout' @{
        areaId=$checkoutArea.id
        guestName='Stock Test';guestEmail='stock@example.invalid';addressLine1='Test address';city='Cairo'
        stateRegion='Cairo';postalCode='11511';country='Egypt';contactPhone='01000000000';paymentMethod='Cash';lines=$lines
    }
    $orders = JsonRequest GET '/api/admin/orders?pageSize=100'
    return ($orders.items | Where-Object orderNumber -EQ $result.orderNumber).id
}
function Stock($id, $variantId) {
    $p = GetProduct $id
    if ($variantId) { return ($p.variants | Where-Object id -EQ $variantId).stockQuantity }
    return $p.stockQuantity
}

$checkoutGovernorate = JsonRequest POST '/api/admin/delivery/governorates' @{name='Test governorate';isActive=$true}
$script:checkoutArea = JsonRequest POST '/api/admin/delivery/areas' @{name='Test delivery';governorateId=$checkoutGovernorate.id;shippingFee=0;currencyCode='EGP';isActive=$true}

# Upload a real tiny PNG and verify the stored bytes can be served.
$pngPath = Join-Path $runDir 'sample.png'
[IO.File]::WriteAllBytes($pngPath, [Convert]::FromBase64String('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aP1sAAAAASUVORK5CYII='))
$client = [Net.Http.HttpClient]::new()
$client.DefaultRequestHeaders.Authorization = [Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer', $adminToken)
$multipart = [Net.Http.MultipartFormDataContent]::new()
$fileContent = [Net.Http.ByteArrayContent]::new([IO.File]::ReadAllBytes($pngPath))
$fileContent.Headers.ContentType = [Net.Http.Headers.MediaTypeHeaderValue]::new('image/png')
$multipart.Add($fileContent, 'file', 'sample.png')
try {
    $uploadResponse = $client.PostAsync("$base/api/admin/uploads", $multipart).GetAwaiter().GetResult()
    $upload = @{StatusCode=[int]$uploadResponse.StatusCode;Content=$uploadResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult()}
} finally { $multipart.Dispose(); $client.Dispose() }
ExpectStatus $upload 200 'Upload PNG'
$image = ($upload.Content | ConvertFrom-Json).data
ExpectStatus (Request GET $image) 200 'Uploaded image is served by API'
$categories = JsonRequest GET '/api/admin/categories'
$sizes = JsonRequest GET '/api/admin/sizes'
$colors = JsonRequest GET '/api/admin/colors'
$body = @{
    name='Image Stock Test';slug='image-stock-test';categoryId=$categories[0].id;basePrice=100;isActive=$true
    showStockQuantity=$true;hasVariants=$true;imageUrl=$image;variants=@(
        @{sizeId=$sizes[0].id;colorId=$colors[0].id;price=100;stockQuantity=10;isActive=$true;imageUrl=$image},
        @{sizeId=$sizes[1].id;colorId=$colors[1].id;price=120;stockQuantity=7;isActive=$true}
    )
}
$product = JsonRequest POST '/api/admin/products' $body
$testProductId = $product.id
$vid = $product.variants[0].id
$otherVid = $product.variants[1].id
ExpectEqual (GetProduct $testProductId).images[0].url $image 'Product image persisted'
ExpectEqual ((GetProduct $testProductId).variants | Where-Object id -EQ $vid).imageUrl $image 'Variant image persisted'
$orderId = NewOrder @(@{productId=$testProductId;variantId=$vid;quantity=2})
ExpectEqual (Stock $testProductId $vid) 10 'Pending order does not deduct stock'
Status $orderId 'Confirmed'
ExpectEqual (Stock $testProductId $vid) 8 'Confirmed deducts selected variant'
ExpectEqual (Stock $testProductId $otherVid) 7 'Other size/color stock unchanged'
Status $orderId 'Confirmed'
Status $orderId 'Delivered'
ExpectEqual (Stock $testProductId $vid) 8 'Repeated confirmation and delivery do not deduct again'

# Updating a sold product must preserve the order's variant reference.
$body.variants[0].id = $vid
$body.variants[0].stockQuantity = 8
$body.variants[1].id = $otherVid
$updated = JsonRequest PUT "/api/admin/products/$testProductId" $body
ExpectEqual $updated.variants[0].id $vid 'Editing sold product preserves variant ID'
$body.imageUrl = ''
$null = JsonRequest PUT "/api/admin/products/$testProductId" $body
ExpectEqual @((GetProduct $testProductId).images).Count 0 'Remove primary image persists'
$listing = JsonRequest GET '/api/catalog?pageSize=100'
ExpectEqual ($listing.items | Where-Object id -EQ $testProductId).primaryImageUrl $image 'Variant image is catalog fallback'
$body.imageUrl = $image
$null = JsonRequest PUT "/api/admin/products/$testProductId" $body
ExpectEqual (GetProduct $testProductId).images[0].url $image 'Replace primary image persists'

$cancelId = NewOrder @(@{productId=$testProductId;variantId=$vid;quantity=3})
Status $cancelId 'Confirmed'
ExpectEqual (Stock $testProductId $vid) 5 'Second order deducted'
Status $cancelId 'Cancelled'
Status $cancelId 'Cancelled'
ExpectEqual (Stock $testProductId $vid) 8 'Cancellation restores once'
Status $cancelId 'Confirmed' 409

# Pending orders compete for stock only at confirmation.
$large1 = NewOrder @(@{productId=$testProductId;variantId=$vid;quantity=6})
$large2 = NewOrder @(@{productId=$testProductId;variantId=$vid;quantity=6})
Status $large1 'Confirmed'
Status $large2 'Confirmed' 409
ExpectEqual (Stock $testProductId $vid) 2 'Insufficient confirmation does not deduct'

# Multi-line confirmation rolls back its first deduction if another line fails.
$multi = NewOrder @(@{productId=$testProductId;variantId=$vid;quantity=1},@{productId=$testProductId;variantId=$otherVid;quantity=7})
$consumeOther = NewOrder @(@{productId=$testProductId;variantId=$otherVid;quantity=2})
Status $consumeOther 'Confirmed'
Status $multi 'Confirmed' 409
ExpectEqual (Stock $testProductId $vid) 2 'Failed multi-line order rolled back all deductions'
ExpectEqual (Stock $testProductId $otherVid) 5 'Failed multi-line order leaves other stock unchanged'

$simple = JsonRequest POST '/api/admin/products' @{
    name='Simple Stock Test';slug='simple-stock-test';categoryId=$categories[0].id;basePrice=50;isActive=$true
    hasVariants=$false;stockQuantity=4;imageUrl=$image
}
$simpleOrder = NewOrder @(@{productId=$simple.id;quantity=2})
Status $simpleOrder 'Confirmed'
Status $simpleOrder 'Shipped'
Status $simpleOrder 'Delivered'
ExpectEqual (Stock $simple.id $null) 2 'Product without variants deducts only once'
Status $simpleOrder 'Cancelled' 409

# Duplicate lines cannot bypass the checkout stock check.
$null = JsonRequest POST '/api/checkout' @{
    areaId=$checkoutArea.id
    guestName='Stock Test';guestEmail='stock@example.invalid';addressLine1='Test';city='Cairo';stateRegion='Cairo'
    postalCode='11511';country='Egypt';contactPhone='01000000000';paymentMethod='Cash'
    lines=@(@{productId=$testProductId;variantId=$vid;quantity=2},@{productId=$testProductId;variantId=$vid;quantity=1})
} 409

# Restart against the same test DB: the marker survives and cannot double-debit.
Stop-Process -Id $apiProcess.Id
$apiProcess.WaitForExit()
$apiProcess = Start-Process dotnet -ArgumentList @($dll,'--contentRoot',('"'+$runDir+'"')) -WindowStyle Hidden -PassThru `
    -RedirectStandardOutput (Join-Path $runDir 'restart.log') -RedirectStandardError (Join-Path $runDir 'restart-error.log')
$ready = $false
for ($i=0;$i -lt 60;$i++) {
    try { if ((Request GET '/api/catalog').StatusCode -eq 200) { $ready=$true;break } } catch { }
    Start-Sleep -Milliseconds 300
}
if (!$ready) { throw 'API restart failed.' }
Status $large1 'Delivered'
ExpectEqual (Stock $testProductId $vid) 2 'Restart preserves deducted marker'

function ConcurrentConfirm($ids) {
    $http = [Net.Http.HttpClient]::new()
    $http.DefaultRequestHeaders.Authorization = [Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer', $adminToken)
    $tasks = @()
    $contents = @()
    try {
        foreach ($orderKey in $ids) {
            $content = [Net.Http.StringContent]::new('{"status":"Confirmed"}', [Text.Encoding]::UTF8, 'application/json')
            $contents += $content
            $tasks += $http.PutAsync("$base/api/admin/orders/$orderKey/status", $content)
        }
        return @($tasks | ForEach-Object { [int]$_.GetAwaiter().GetResult().StatusCode })
    } finally { foreach ($content in $contents) { $content.Dispose() }; $http.Dispose() }
}
$sameOrder = NewOrder @(@{productId=$simple.id;quantity=1})
$results = ConcurrentConfirm @($sameOrder,$sameOrder)
if (@($results | Where-Object { $_ -notin @(200,409) }).Count -or 200 -notin $results) { throw "Concurrent same-order statuses: $results" }
ExpectEqual (Stock $simple.id $null) 1 'Concurrent confirmation deducts same order once'
$race1 = NewOrder @(@{productId=$simple.id;quantity=1})
$race2 = NewOrder @(@{productId=$simple.id;quantity=1})
$results = ConcurrentConfirm @($race1,$race2)
ExpectEqual @($results | Where-Object { $_ -eq 200 }).Count 1 'Only one competing order is confirmed'
ExpectEqual @($results | Where-Object { $_ -eq 409 }).Count 1 'Other competing order receives stock conflict'
ExpectEqual (Stock $simple.id $null) 0 'Concurrent orders do not oversell'
# Gallery contracts: creation, ordering, independent variants, removal and legacy reads.
$galleryBody = @{
    name='Gallery Test';slug='gallery-test';categoryId=$categories[0].id;basePrice=50;isActive=$true
    hasVariants=$true;imageUrls=@($image,'/uploads/product-second.png');variants=@(
        @{sizeId=$sizes[0].id;colorId=$colors[0].id;price=50;stockQuantity=5;isActive=$true;imageUrls=@($image,'/uploads/red-second.png')},
        @{sizeId=$sizes[1].id;colorId=$colors[1].id;price=60;stockQuantity=8;isActive=$true;imageUrls=@('/uploads/blue-first.png','/uploads/blue-second.png')}
    )
}
$galleryProduct = JsonRequest POST '/api/admin/products' $galleryBody
$galleryRead = GetProduct $galleryProduct.id
ExpectEqual $galleryRead.images.Count 2 'Product accepts multiple images'
ExpectEqual $galleryRead.images[0].url $image 'Product primary image order'
ExpectEqual $galleryRead.variants[0].imageUrls.Count 2 'Variant accepts multiple images'
ExpectEqual $galleryRead.variants[1].imageUrls[0] '/uploads/blue-first.png' 'Other variant gallery independent'
ExpectEqual $galleryRead.variants[0].imageUrl $image 'Legacy primary image kept compatible'
$galleryBody.imageUrls = @('/uploads/product-second.png',$image)
$galleryBody.variants[0].id = $galleryRead.variants[0].id
$galleryBody.variants[1].id = $galleryRead.variants[1].id
$galleryBody.variants[0].imageUrls = @('/uploads/red-second.png')
$galleryBody.variants[1].imageUrls = @()
$null = JsonRequest PUT "/api/admin/products/$($galleryProduct.id)" $galleryBody
$galleryRead = GetProduct $galleryProduct.id
ExpectEqual $galleryRead.images[0].url '/uploads/product-second.png' 'Product primary can be changed'
ExpectEqual $galleryRead.variants[0].imageUrls.Count 1 'Variant image removal persists'
ExpectEqual $galleryRead.variants[0].imageUrl '/uploads/red-second.png' 'Variant primary follows first image'
ExpectEqual $galleryRead.variants[1].imageUrls.Count 0 'Clear variant gallery persists'
ExpectEqual $galleryRead.variants[1].imageUrl $null 'Cleared gallery does not retain legacy image'
$galleryBody.imageUrls = @()
$null = JsonRequest PUT "/api/admin/products/$($galleryProduct.id)" $galleryBody
ExpectEqual @((GetProduct $galleryProduct.id).images).Count 0 'Clear product gallery persists'
$galleryBody.imageUrls = @(1..21 | ForEach-Object { "/uploads/$_.png" })
$null = JsonRequest PUT "/api/admin/products/$($galleryProduct.id)" $galleryBody 400
# Admin-managed colors, currencies and order status definitions.
$newColor = JsonRequest POST '/api/admin/colors' @{name='Ocean Test';hexCode='#123abc';displayOrder=20;isActive=$true}
ExpectEqual $newColor.name 'Ocean Test' 'Admin creates color'
$null = JsonRequest DELETE "/api/admin/colors/$($newColor.id)"
$currency = JsonRequest POST '/api/admin/currencies' @{code='eur';name='Euro';symbol='€';isActive=$true}
ExpectEqual $currency.code 'EUR' 'Currency codes normalize'
$null = JsonRequest POST '/api/admin/currencies' @{code='EUR';name='Duplicate';symbol='€';isActive=$true} 409
$null = JsonRequest PUT '/api/admin/settings' @{currencyCode='EUR';currencySymbol='wrong'}
$publicSettings = JsonRequest GET '/api/settings'
ExpectEqual $publicSettings.currencyCode 'EUR' 'Currency selected for storefront'
ExpectEqual $publicSettings.currencySymbol '€' 'Symbol resolves from saved currency'
$null = JsonRequest PUT "/api/admin/currencies/$($currency.id)" @{code='EUR';name='Euro';symbol='€';isActive=$false} 409
$null = JsonRequest DELETE "/api/admin/currencies/$($currency.id)" $null 409
$null = JsonRequest PUT '/api/admin/settings' @{currencyCode='ZZZ';currencySymbol='?'} 400
$definition = JsonRequest POST '/api/admin/order-statuses' @{name='Ready for customer';color='#123abc';behavior='Confirmed';displayOrder=11;isActive=$true}
$null = JsonRequest POST '/api/admin/order-statuses' @{name='Ready for customer';color='#123abc';behavior='Pending';isActive=$true} 409
$statusOrder = NewOrder @(@{productId=$galleryProduct.id;variantId=$galleryRead.variants[0].id;quantity=1})
$null = JsonRequest PUT "/api/admin/orders/$statusOrder/status" @{statusId=$definition.id}
$orderList = JsonRequest GET '/api/admin/orders?pageSize=100'
$statusOrderRow = $orderList.items | Where-Object id -EQ $statusOrder
ExpectEqual $statusOrderRow.status 'Ready for customer' 'Custom status appears on order'
ExpectEqual $statusOrderRow.currencyCode 'EUR' 'New order snapshots selected currency'
ExpectEqual (Stock $galleryProduct.id $galleryRead.variants[0].id) 4 'Custom confirmation deducts once'
$tracked = JsonRequest GET "/api/ordertracking/$($statusOrderRow.orderNumber)"
ExpectEqual $tracked.status 'Ready for customer' 'Tracking shows custom status name'
$null = JsonRequest PUT "/api/admin/order-statuses/$($definition.id)" @{name='Ready to collect';color='#654321';behavior='Confirmed';displayOrder=12;isActive=$true}
$tracked = JsonRequest GET "/api/ordertracking/$($statusOrderRow.orderNumber)"
ExpectEqual $tracked.status 'Ready to collect' 'Renaming status propagates'
$null = JsonRequest PUT "/api/admin/order-statuses/$($definition.id)" @{name='Ready to collect';color='#654321';behavior='Cancelled';isActive=$true} 409
$null = JsonRequest DELETE "/api/admin/order-statuses/$($definition.id)" $null 409
$null = JsonRequest PUT "/api/admin/order-statuses/$($definition.id)" @{name='Ready to collect';color='#654321';behavior='Confirmed';isActive=$false}
$null = JsonRequest PUT "/api/admin/orders/$statusOrder/status" @{statusId=$definition.id} 400
$null = JsonRequest PUT '/api/admin/order-statuses/1' @{name='New order';color='#123456';behavior='Pending';isActive=$false} 409
$unused = JsonRequest POST '/api/admin/order-statuses' @{name='Unused step';color='#123456';behavior='Processing';isActive=$true}
$null = JsonRequest DELETE "/api/admin/order-statuses/$($unused.id)"
$null = JsonRequest PUT '/api/admin/settings' @{currencyCode='EGP';currencySymbol='E£'}
$null = JsonRequest DELETE "/api/admin/currencies/$($currency.id)" $null 409
$oldOrders = JsonRequest GET '/api/admin/orders?pageSize=100'
ExpectEqual ($oldOrders.items | Where-Object id -EQ $orderId).currencyCode 'EGP' 'Old order keeps original currency'
$overview = JsonRequest GET '/api/admin/sales/overview'
ExpectEqual $overview.currencyCode 'EGP' 'Sales overview identifies its currency'
ExpectEqual $overview.totalRevenue (($oldOrders.items | Where-Object currencyCode -EQ 'EGP' | Measure-Object total -Sum).Sum) 'Sales totals do not mix currencies'
# Shipping configuration, authoritative quoting and immutable order snapshots.
$shippingGovernorate = JsonRequest POST '/api/admin/delivery/governorates' @{name='Cairo shipping test';isActive=$true}
$shippingBody = @{name='Maadi shipping test';governorateId=$shippingGovernorate.id;shippingFee=60;currencyCode='EGP';isActive=$true}
$shippingArea = JsonRequest POST '/api/admin/delivery/areas' $shippingBody
$null = JsonRequest POST '/api/admin/discounts' @{code='SHIP10';type='Percentage';value=10;isActive=$true}
$quoteBody = @{areaId=$shippingArea.id;discountCode='SHIP10';lines=@(@{productId=$galleryProduct.id;variantId=$galleryRead.variants[0].id;quantity=2})}
$ordersBeforeQuote = (JsonRequest GET '/api/admin/orders').totalCount
$quote = JsonRequest POST '/api/checkout/quote' $quoteBody
ExpectEqual $quote.subtotal 100 'Quote uses current product price'
ExpectEqual $quote.discountTotal 10 'Quote computes discount before shipping'
ExpectEqual $quote.shippingTotal 60 'Quote uses configured shipping rate'
ExpectEqual $quote.total 150 'Quote total includes shipping after discount'
ExpectEqual (JsonRequest GET '/api/admin/orders').totalCount $ordersBeforeQuote 'Quoting does not create an order'
$shippingOrderBody = @{
    areaId=$shippingArea.id;discountCode='SHIP10';lines=$quoteBody.lines;expectedTotal=150;expectedCurrencyCode='EGP'
    guestName='Shipping test';guestEmail='shipping@example.invalid';addressLine1='Test street';city='untrusted area';stateRegion='untrusted governorate'
    postalCode='11511';country='Egypt';contactPhone='01000000000';paymentMethod='Cash';shippingTotal=0;total=1
}
$shippingOrder = JsonRequest POST '/api/checkout' $shippingOrderBody
ExpectEqual $shippingOrder.shippingTotal 60 'Client cannot override shipping'
ExpectEqual $shippingOrder.total 150 'Client cannot override final amount'
ExpectEqual $shippingOrder.city 'Maadi shipping test' 'Area name comes from selected area'
ExpectEqual $shippingOrder.stateRegion 'Cairo shipping test' 'Governorate name comes from selected area'
$shippingBody.shippingFee = 80
$null = JsonRequest PUT "/api/admin/delivery/areas/$($shippingArea.id)" $shippingBody
$null = JsonRequest POST '/api/checkout' $shippingOrderBody 409
$quote2 = JsonRequest POST '/api/checkout/quote' $quoteBody
ExpectEqual $quote2.total 170 'New quote reflects rate change'
$oldShippingOrder = JsonRequest GET "/api/ordertracking/$($shippingOrder.orderNumber)"
ExpectEqual $oldShippingOrder.shippingTotal 60 'Existing order retains shipping snapshot'
ExpectEqual $oldShippingOrder.total 150 'Existing order total unchanged by rate edit'
$shippingOrderBody.expectedTotal = 170
$null = JsonRequest POST '/api/checkout' $shippingOrderBody
$null = JsonRequest DELETE "/api/admin/delivery/areas/$($shippingArea.id)" $null 409
$null = JsonRequest DELETE "/api/admin/delivery/governorates/$($shippingGovernorate.id)" $null 409
$shippingBody.isActive = $false
$null = JsonRequest PUT "/api/admin/delivery/areas/$($shippingArea.id)" $shippingBody
ExpectEqual @((JsonRequest GET "/api/delivery/areas?governorateId=$($shippingGovernorate.id)")).Count 0 'Inactive areas hidden from checkout'
$null = JsonRequest POST '/api/checkout/quote' $quoteBody 400
$null = JsonRequest POST '/api/checkout' $shippingOrderBody 400
$shippingBody.isActive = $true
$shippingBody.currencyCode = 'USD'
$null = JsonRequest PUT "/api/admin/delivery/areas/$($shippingArea.id)" $shippingBody
ExpectEqual @((JsonRequest GET "/api/delivery/areas?governorateId=$($shippingGovernorate.id)")).Count 0 'Rates in another currency are unavailable'
$null = JsonRequest POST '/api/checkout' $shippingOrderBody 400
$shippingBody.currencyCode = 'EGP'; $shippingBody.shippingFee = 0
$null = JsonRequest PUT "/api/admin/delivery/areas/$($shippingArea.id)" $shippingBody
$freeQuote = JsonRequest POST '/api/checkout/quote' $quoteBody
ExpectEqual $freeQuote.shippingTotal 0 'Explicit zero rate supports free shipping'
ExpectEqual $freeQuote.total 90 'Free shipping quote still applies discount'
$null = JsonRequest PUT "/api/admin/delivery/governorates/$($shippingGovernorate.id)" @{name='Cairo shipping test';isActive=$false}
$publicGovernorates = JsonRequest GET '/api/delivery/governorates'
ExpectEqual @($publicGovernorates | Where-Object id -EQ $shippingGovernorate.id).Count 0 'Inactive governorate hidden'
$null = JsonRequest POST '/api/checkout/quote' $quoteBody 400
$null = JsonRequest POST '/api/checkout' $shippingOrderBody 400
$badBody = @{} + $shippingBody; $badBody.shippingFee = -1
$null = JsonRequest POST '/api/admin/delivery/areas' $badBody 400
$badBody.shippingFee = 1.123
$null = JsonRequest POST '/api/admin/delivery/areas' $badBody 400
$badBody.shippingFee = 10; $badBody.governorateId = 999999
$null = JsonRequest POST '/api/admin/delivery/areas' $badBody 400
$missingAreaBody = @{} + $shippingOrderBody; $missingAreaBody.Remove('areaId')
$null = JsonRequest POST '/api/checkout' $missingAreaBody 400
$emptyGovernorate = JsonRequest POST '/api/admin/delivery/governorates' @{name='Empty test';isActive=$true}
$unusedArea = JsonRequest POST '/api/admin/delivery/areas' @{name='Unused';governorateId=$emptyGovernorate.id;shippingFee=12;currencyCode='EGP';isActive=$true}
$null = JsonRequest DELETE "/api/admin/delivery/areas/$($unusedArea.id)"
$null = JsonRequest DELETE "/api/admin/delivery/governorates/$($emptyGovernorate.id)"
$shippingReport = JsonRequest GET '/api/admin/sales/overview'
$reportOrders = JsonRequest GET '/api/admin/orders?pageSize=100'
ExpectEqual $shippingReport.shippingRevenue (($reportOrders.items | Where-Object currencyCode -EQ 'EGP' | Measure-Object shippingTotal -Sum).Sum) 'Shipping revenue reported separately'
ExpectEqual ($shippingReport.productRevenue + $shippingReport.shippingRevenue) $shippingReport.totalRevenue 'Product and shipping totals reconcile'
Write-Output 'Product galleries, stock, configuration and shipping scenarios passed.'
