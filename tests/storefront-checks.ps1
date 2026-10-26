. (Join-Path $PSScriptRoot 'product-order-checks.ps1')
$original=JsonRequest GET '/api/merchant/storefront'
ExpectEqual $original.theme 'classic' 'Existing stores retain classic appearance'
$cat=(JsonRequest GET '/api/merchant/categories')[0]
$product=JsonRequest POST '/api/merchant/products' @{name='Managed kids cotton set';categoryId=$cat.id;basePrice=375;stockQuantity=8;showStockQuantity=$true;hasVariants=$false;isActive=$true;imageUrl=$image}
$extra=JsonRequest POST '/api/merchant/categories' @{name='Additional storefront collection'}
$config=$original|ConvertTo-Json -Depth 12|ConvertFrom-Json
$config.theme='kids';$config.brandName='Fixture Kids';$config.newsletterEnabled=$true
$config.navigation=@(@{label='Children';categoryId=$cat.id});$config.categories=@(@{label='Cotton sets';categoryId=$cat.id;imageUrl=$image})
$config.sections=@(@{title='Merchant picks';eyebrow='Fresh';subtitle='Soft cotton';categoryId=$extra.id;productIds=@($product.id)})
$config.merchandising=@(@{productId=$product.id;compareAtPrice=450;badge='New';collectionIds=@($extra.id)})
$saved=JsonRequest PUT '/api/merchant/storefront' $config
ExpectEqual $saved.version 1 'Storefront version is persisted'
ExpectStatus (Request PUT '/api/merchant/storefront' $customerToken $config) 403 'Customer cannot manage theme'
ExpectStatus (Request PUT '/api/merchant/storefront' '' $config) 401 'Guest cannot manage theme'
$null=JsonRequest PUT '/api/merchant/storefront' $config 409
$public=JsonRequest GET '/api/storefront'
ExpectEqual $public.brandName 'Fixture Kids' 'Public storefront reflects merchant changes'
ExpectEqual $public.categories[0].imageUrl $image 'Uploaded card photo persists'
$bad=$saved|ConvertTo-Json -Depth 12|ConvertFrom-Json;$bad.categories[0].imageUrl='/uploads/stores/another-store/private.webp'
$null=JsonRequest PUT '/api/merchant/storefront' $bad 400
$bad=$saved|ConvertTo-Json -Depth 12|ConvertFrom-Json;$bad.sections[0].productIds=@(2147483000)
$null=JsonRequest PUT '/api/merchant/storefront' $bad 400
$listing=JsonRequest GET ('/api/storefront/products?categoryId='+$extra.id)
ExpectEqual $listing.totalCount 1 'Managed additional collection filters actual products'
ExpectEqual $listing.items[0].basePrice 375 'Cards use actual server product prices'
$searched=JsonRequest GET '/api/storefront/products?q=Managed%20kids'
ExpectEqual $searched.items[0].id $product.id 'Storefront search uses active products'
$null=JsonRequest GET '/api/storefront/products?ids=invalid' $null 400
$address='newsletter-'+[guid]::NewGuid().ToString('N')+'@example.invalid'
ExpectStatus (Request POST '/api/storefront/subscribe' '' @{email=$address;consent=$false}) 400 'Newsletter requires explicit consent'
ExpectStatus (Request POST '/api/storefront/subscribe' '' @{email=$address;consent=$true}) 200 'Guest newsletter signup saved'
ExpectStatus (Request POST '/api/storefront/subscribe' '' @{email=$address;consent=$true}) 200 'Repeated newsletter signup is idempotent'
$subscribers=JsonRequest GET '/api/merchant/storefront/subscribers'
ExpectEqual @($subscribers|Where-Object email -EQ $address).Count 1 'One newsletter record persists'
ExpectStatus (Request GET '/api/merchant/storefront/subscribers' $customerToken) 403 'Customer cannot read mailing list'
$productUpdate=@{name='Managed kids cotton set';categoryId=$cat.id;basePrice=399;stockQuantity=2;showStockQuantity=$true;hasVariants=$false;isActive=$true;imageUrl=$image}
$null=JsonRequest PUT "/api/merchant/products/$($product.id)" $productUpdate
$fresh=JsonRequest GET ("/api/storefront/products?ids="+$product.id)
ExpectEqual $fresh.items[0].basePrice 399 'Product edits immediately update card pricing'
ExpectEqual $fresh.items[0].stockQuantity 2 'Product edits immediately update card stock'
Write-Host 'Managed themes, image ownership, version checks, collections, real pricing/stock and private consent-based newsletter passed.'
