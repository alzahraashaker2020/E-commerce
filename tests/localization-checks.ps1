. (Join-Path $PSScriptRoot 'storefront-checks.ps1')
$sources=JsonRequest GET '/api/merchant/storefront/translation-sources'
ExpectEqual @($sources|Where-Object source -EQ 'Managed kids cotton set').Count 1 'Translation editor discovers store product text'
ExpectStatus (Request GET '/api/merchant/storefront/translation-sources' $customerToken) 403 'Customer cannot inspect merchant translation sources'
$draft=JsonRequest POST '/api/merchant/products' @{name='Private inactive translation fixture';categoryId=$cat.id;basePrice=100;hasVariants=$false;stockQuantity=2;isActive=$false}
$config=JsonRequest GET '/api/merchant/storefront'
$config.translations=@{'Fixture Kids'='متجر الصغار';'Managed kids cotton set'='طقم أطفال قطن ناعم';'Private inactive translation fixture'='منتج تجريبي خاص';'Unknown private text'='نص غير عام'}
$config=JsonRequest PUT '/api/merchant/storefront' $config
$public=JsonRequest GET '/api/storefront'
ExpectEqual $public.translations.'Fixture Kids' 'متجر الصغار' 'Store-owned Arabic brand persists'
ExpectEqual $public.translations.'Managed kids cotton set' 'طقم أطفال قطن ناعم' 'Store-owned Arabic product name persists'
ExpectEqual ($null -eq $public.translations.'Private inactive translation fixture') $true 'Inactive product translations are not exposed publicly'
ExpectEqual ($null -eq $public.translations.'Unknown private text') $true 'Unreferenced translation phrases are not exposed publicly'
$search=JsonRequest GET ('/api/storefront/products?q='+[uri]::EscapeDataString('أطفال'))
ExpectEqual $search.items[0].id $product.id 'Arabic search finds translated active product'
ExpectEqual $search.items[0].name 'Managed kids cotton set' 'Localization never changes canonical API product name'
$bad=$config|ConvertTo-Json -Depth 20|ConvertFrom-Json;$bad.translations=$null
$null=JsonRequest PUT '/api/merchant/storefront' $bad 400
# Account security mail is composed in the requested language and remains durably queued.
$arEmail='arabic-'+[guid]::NewGuid().ToString('N')+'@example.invalid'
$signup=Invoke-WebRequest ($base+'/api/account/register') -Method POST -Headers @{'Accept-Language'='ar-EG'} -ContentType application/json -Body (@{fullName='عميل عربي';email=$arEmail;password='Arabic-Test-12345'}|ConvertTo-Json) -SkipHttpErrorCheck -TimeoutSec 20
ExpectStatus $signup 200 'Arabic customer signup uses the same account protocol'
$history=JsonRequest GET '/api/merchant/notifications/messages'
ExpectEqual @($history|Where-Object {$_.recipient -eq $arEmail -and $_.subject -eq 'التحقق من بريد المتجر'}).Count 1 'Arabic verification subject is durably recorded'
Write-Output 'Arabic content persistence, source discovery, private-draft filtering, search and localized account email passed.'

$size=(JsonRequest GET '/api/merchant/sizes'|Where-Object isActive -EQ $true|Select-Object -First 1)
$color=(JsonRequest GET '/api/merchant/colors'|Where-Object isActive -EQ $true|Select-Object -First 1)
$localizedProduct=JsonRequest POST '/api/merchant/products' @{name='Localized variant set';categoryId=$cat.id;basePrice=120;hasVariants=$true;isActive=$true;variants=@(@{sizeId=$size.id;colorId=$color.id;price=120;stockQuantity=5;isActive=$true})}
$doc=JsonRequest GET '/api/merchant/storefront'
$doc.translations|Add-Member -NotePropertyName 'Localized variant set' -NotePropertyValue 'طقم تجريبي'
$doc.translations|Add-Member -NotePropertyName $size.name -NotePropertyValue 'مقاس عربي'
$doc.translations|Add-Member -NotePropertyName $color.name -NotePropertyValue 'لون عربي'
$null=JsonRequest PUT '/api/merchant/storefront' $doc
$mailSettings=JsonRequest GET '/api/merchant/settings'
$mailGovernorate=JsonRequest POST '/api/merchant/delivery/governorates' @{name='Localized email governorate';isActive=$true}
$mailArea=JsonRequest POST '/api/merchant/delivery/areas' @{name='Localized email area';governorateId=$mailGovernorate.id;currencyCode=$mailSettings.currencyCode;shippingFee=0;isActive=$true}
$body=@{idempotencyKey=[guid]::NewGuid().ToString();areaId=$mailArea.id;guestName='عميل تجريبي';guestEmail=$arEmail;addressLine1='عنوان تجريبي';city='Cairo';stateRegion='Cairo';postalCode='11511';country='Egypt';contactPhone='01000000000';paymentMethod='Cash';lines=@(@{productId=$localizedProduct.id;variantId=$localizedProduct.variants[0].id;quantity=1})}
$placed=Invoke-WebRequest ($base+'/api/checkout') -Method POST -Headers @{'Accept-Language'='ar'} -ContentType application/json -Body ($body|ConvertTo-Json -Depth 10) -SkipHttpErrorCheck -TimeoutSec 20
ExpectStatus $placed 200 'Arabic variant checkout preserves normal order creation'
$order=($placed.Content|ConvertFrom-Json).data
$mail=Join-Path $runDir ('store-data/default/sent-emails/'+$order.orderNumber+'.txt')
for($i=0;$i -lt 40 -and !(Test-Path $mail);$i++){Start-Sleep -Milliseconds 200}
if(!(Test-Path $mail)){throw 'Localized order confirmation was not written by development transport.'}
$text=Get-Content $mail -Raw
ExpectEqual ($text.Contains('طقم تجريبي (مقاس عربي، لون عربي)')) $true 'Arabic order message includes localized product and variant name'
ExpectEqual ($text.Contains('تأكيد الطلب')) $true 'Arabic order confirmation subject is composed before queuing'
Write-Output 'Arabic variant checkout and complete localized order message passed.'
