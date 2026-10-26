param([string]$ApiBaseUrl='http://localhost:5000')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$private=Join-Path $repo 'src/Shop.Api/obj'
$platform=Get-Content (Join-Path $private 'platform-access.json') -Raw|ConvertFrom-Json
function Api($method,$path,$body=$null,$token='',$store=$false){$args=@{Uri=$ApiBaseUrl+'/api/'+$path;Method=$method;TimeoutSec=60};$headers=@{};if($token){$headers.Authorization='Bearer '+$token};if($store){$headers.Host='kids.localhost'};$args.Headers=$headers;if($null -ne $body){$args.ContentType='application/json';$args.Body=$body|ConvertTo-Json -Depth 24};$raw=Invoke-WebRequest @args;$response=$raw.Content|ConvertFrom-Json -AsHashtable;if(!$response.success){throw $response.message};return $response.data}
$platformToken=(Api POST 'platform/auth/login' @{username=$platform.username;password=$platform.password}).token
$accessFile=Join-Path $private 'kids-demo-access.json'
if(Test-Path $accessFile){$access=Get-Content $accessFile -Raw|ConvertFrom-Json}else{$access=[pscustomobject]@{username='kids-owner';password=('Kids-Demo-'+[guid]::NewGuid().ToString('N'));url='http://kids.localhost:4300/merchant/login'};$access|ConvertTo-Json|Set-Content $accessFile
    $acl=[Security.AccessControl.FileSecurity]::new();$acl.SetAccessRuleProtection($true,$false);foreach($identity in @([Security.Principal.WindowsIdentity]::GetCurrent().User,[Security.Principal.SecurityIdentifier]::new('S-1-5-18'))){$acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new($identity,'FullControl','Allow'))};Set-Acl -LiteralPath $accessFile -AclObject $acl
}
$store=@(Api GET 'platform/stores' $null $platformToken|Where-Object host -EQ 'kids.localhost')|Select-Object -First 1
if(!$store){$store=Api POST 'platform/stores' @{name='Little Days Kids';host='kids.localhost';connectionKey='KidsClothingDemo';ownerUsername=$access.username;ownerPassword=$access.password;ownerEmail='kids-owner@example.invalid'} $platformToken}
if($store.name -ne 'Little Days Kids'){throw 'The demo hostname belongs to another store; refusing to modify it.'}
$subscription=@(Api GET 'platform/subscriptions' $null $platformToken|Where-Object {$_.storeId -eq $store.id -or $_.id -eq $store.id})|Select-Object -First 1
if(!$subscription.subscription){
    $plan=@(Api GET 'platform/plans' $null $platformToken|Where-Object name -EQ 'Kids demo preview (not for billing)')|Select-Object -First 1
    if(!$plan){$plan=Api POST 'platform/plans' @{name='Kids demo preview (not for billing)';priceMinor=0;currency='EGP';periodMonths=1;trialDays=90;maxProducts=100;isActive=$true} $platformToken}
    $null=Api POST "platform/stores/$($store.id)/subscription" @{action='Trial';planId=$plan.id;version=0;reference='Local clothing UI demonstration'} $platformToken
}
$token=(Api POST 'merchant/auth/login' @{username=$access.username;password=$access.password} '' $true).token
function Merchant($method,$path,$body=$null){Api $method ('merchant/'+$path) $body $token $true}
$existing=Merchant GET 'products'
if(@($existing).Count -gt 0 -and (Merchant GET 'storefront').theme -eq 'kids'){Write-Output 'Demo already has products; preserving merchant edits. Access details remain in the private kids-demo-access.json file.';exit 0}
$null=Merchant PUT 'settings' @{currencyCode='EGP';currencySymbol='E£'}
$categories=@{};$current=Merchant GET 'categories';foreach($name in @('Baby Girls','Baby Boys','Girls','Boys','Pyjamas & Sleepwear','Bodysuits & Rompers','Everyday Sets','Warm Layers','Dresses','New Arrivals','Little Offers')){$row=@($current|Where-Object name -EQ $name)|Select-Object -First 1;if(!$row){$row=Merchant POST 'categories' @{name=$name}};$categories[$name]=$row.id}
$sizes=@{};$current=Merchant GET 'sizes';$index=0;foreach($name in @('0–3m','3–6m','6–9m','9–12m','12–18m','18–24m','2–3y','4–5y','6–7y','8–9y','10–11y','12–14y')){$row=@($current|Where-Object name -EQ $name)|Select-Object -First 1;if(!$row){$row=Merchant POST 'sizes' @{name=$name;displayOrder=$index;isActive=$true}};$index++;$sizes[$name]=$row.id}
$colors=@{};$current=Merchant GET 'colors';foreach($pair in @(@('Original','#eadfcb'),@('Soft white','#f7f5ef'))){$row=@($current|Where-Object name -EQ $pair[0])|Select-Object -First 1;if(!$row){$row=Merchant POST 'colors' @{name=$pair[0];hexCode=$pair[1];isActive=$true;displayOrder=0}};$colors[$pair[0]]=$row.id}
$downloads=Join-Path $private 'kids-demo-assets';New-Item -ItemType Directory -Force -Path $downloads|Out-Null
$credits=[Collections.Generic.List[object]]::new()
function UploadPhoto($url,$name){
    $extension=[IO.Path]::GetExtension(([uri]$url).AbsolutePath);$path=Join-Path $downloads ($name+$extension)
    if(!(Test-Path $path)){Invoke-WebRequest -Uri $url -OutFile $path -TimeoutSec 45}
    $bytes=[IO.File]::ReadAllBytes($path)
    if($bytes[0] -eq 255 -and $bytes[1] -eq 216){$extension='.jpg'}elseif($bytes[0] -eq 137 -and $bytes[1] -eq 80){$extension='.png'}elseif([Text.Encoding]::ASCII.GetString($bytes,0,4) -eq 'RIFF'){$extension='.webp'}else{throw 'Reference download did not contain a supported image.'}
    $client=[Net.Http.HttpClient]::new();$client.DefaultRequestHeaders.Authorization=[Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer',$token);$client.DefaultRequestHeaders.Host='kids.localhost'
    $form=[Net.Http.MultipartFormDataContent]::new();$image=[Net.Http.ByteArrayContent]::new($bytes);$image.Headers.ContentType=[Net.Http.Headers.MediaTypeHeaderValue]::new($(if($extension -eq '.png'){'image/png'}elseif($extension -in @('.jpg','.jpeg')){'image/jpeg'}else{'image/webp'}));$form.Add($image,'file',$name+$extension)
    try{$response=$client.PostAsync($ApiBaseUrl+'/api/merchant/uploads',$form).GetAwaiter().GetResult();$result=$response.Content.ReadAsStringAsync().GetAwaiter().GetResult()|ConvertFrom-Json;if(!$response.IsSuccessStatusCode){throw $result.message}}finally{$form.Dispose();$client.Dispose()}
    if(!$result.success){throw 'Demo image upload failed'}
    $credits.Add(@{source=$url;storedPath=$result.data});return $result.data
}
$assets=@{};foreach($name in @('ok-cat-clothing','ok-cat-baby','ok-cat-summer','ok-cat-sale','ok-cat-cl-bgirl','ok-cat-cl-bboy','ok-cat-cl-girls','ok-cat-cl-boys','ok-strip-clothing','ok-band-bg-sale')){$assets[$name]=UploadPhoto ('https://shop-ourkids.com/cdn/shop/t/54/assets/'+$name+'.webp') $name}
$source=Get-Content (Join-Path $PSScriptRoot 'demo-data/kids-products.json') -Raw|ConvertFrom-Json
$products=@();$merch=@();$i=0
foreach($item in $source){
    $photos=@();$photoIndex=0;foreach($url in $item.images){$photos+=UploadPhoto $url ('product-'+$i+'-'+$photoIndex++)}
    $baby=$item.name -match 'Baby';$girls=$item.name -match 'Girls|Dress|Nightgown';$category=if($baby){if($girls){'Baby Girls'}else{'Baby Boys'}}else{if($girls){'Girls'}else{'Boys'}}
    $sizeNames=if($baby){@('0–3m','3–6m','6–9m','9–12m','12–18m','18–24m')}else{@('2–3y','4–5y','6–7y','8–9y','10–11y','12–14y')}
    $variants=@();$v=0;foreach($size in $sizeNames){$variants+=@{sizeId=$sizes[$size];colorId=$colors.Original;price=$item.price;stockQuantity=if($v -eq 4){0}else{3+(($i+$v)%9)};lowStockThreshold=3;isActive=$true;imageUrl=$photos[0];imageUrls=$photos;sku=('LD-'+$i+'-'+$v++)}}
    $product=@($existing|Where-Object slug -EQ ('little-days-'+$i))|Select-Object -First 1
    if(!$product){$product=Merchant POST 'products' @{name=$item.name;slug=('little-days-'+$i);brand='Little Days';categoryId=$categories[$category];basePrice=$item.price;descriptionHtml='<p>Soft cotton, happy colours and room for every little adventure. A comfortable everyday favourite from the Little Days demo collection.</p><p>Choose an available size above. Sample product and stock for demonstration.</p>';hasVariants=$true;showStockQuantity=$true;isActive=$true;lowStockThreshold=3;imageUrl=$photos[0];imageUrls=$photos;variants=$variants}}
    $products+= $product
    $collections=@($categories[$category],$categories['New Arrivals'])
    if($item.name -match 'Pajama|Nightgown'){$collections+=$categories['Pyjamas & Sleepwear']}
    if($item.name -match 'Bodysuit|Romper'){$collections+=$categories['Bodysuits & Rompers']}
    if($item.name -match 'Set|Outfit'){$collections+=$categories['Everyday Sets']}
    if($item.name -match 'Thermal|Long.Sleeve'){$collections+=$categories['Warm Layers']}
    if($item.name -match 'Dress'){$collections+=$categories.Dresses}
    if($i%3 -ne 0){$collections+=$categories['Little Offers']}
    $merch+=@{productId=$product.id;compareAtPrice=if($i%3 -ne 0){[math]::Round($item.price/0.8,2)}else{$null};badge=if($i%3 -eq 0){'New season'}else{''};collectionIds=@($collections|Select-Object -Unique)};$i++
}
$content=Merchant GET 'storefront';$content.theme='kids';$content.brandName='little days';$content.brandSubtitle='KIDS & BABY';$content.announcement='Small outfits. Big adventures. Made for every little day.';$content.shippingText='Delivery to your door';$content.primaryColor='#426fb0';$content.introEyebrow='THE LITTLE THINGS';$content.introTitle='A little wardrobe. A world of possibilities.';$content.agesTitle='Find their perfect fit';$content.footerText='Demo collection · sample products & prices';$content.supportEmail='hello@example.invalid';$content.supportPhone='';$content.newsletterEnabled=$true
$content.promotions=@(@{label='New season favourites';subtitle='Made for little adventures';categoryId=$categories['New Arrivals']},@{label='Warm & wonderful';subtitle='Soft layers for cooler days';categoryId=$categories['Warm Layers']},@{label='Pyjamas & sleepwear';subtitle='Bedtime never felt so cosy';categoryId=$categories['Pyjamas & Sleepwear']},@{label='Little offers';subtitle='Selected styles, lovely savings';categoryId=$categories['Little Offers']})
$content.navigation=@('Baby Girls','Baby Boys','Girls','Boys','Pyjamas & Sleepwear','Warm Layers','New Arrivals','Little Offers')|ForEach-Object {@{label=$_;categoryId=$categories[$_]}}
$content.categories=@(@{label='Baby essentials';categoryId=$categories['Bodysuits & Rompers'];imageUrl=$assets['ok-cat-baby']},@{label='Girls';categoryId=$categories.Girls;imageUrl=$assets['ok-cat-cl-girls']},@{label='Boys';categoryId=$categories.Boys;imageUrl=$assets['ok-cat-cl-boys']},@{label='Everyday sets';categoryId=$categories['Everyday Sets'];imageUrl=$assets['ok-cat-clothing']},@{label='Cosy nights';categoryId=$categories['Pyjamas & Sleepwear'];imageUrl=(@($products|Where-Object name -Match 'Pajama'|Select-Object -First 1)[0].images[0].url)},@{label='New arrivals';categoryId=$categories['New Arrivals'];imageUrl=$assets['ok-cat-sale']})
$content.ages=@(@{label='Baby Girls · 0–24m';categoryId=$categories['Baby Girls'];imageUrl=$assets['ok-cat-cl-bgirl']},@{label='Baby Boys · 0–24m';categoryId=$categories['Baby Boys'];imageUrl=$assets['ok-cat-cl-bboy']},@{label='Girls · 2–14y';categoryId=$categories.Girls;imageUrl=$assets['ok-cat-cl-girls']},@{label='Boys · 2–14y';categoryId=$categories.Boys;imageUrl=$assets['ok-cat-cl-boys']})
$content.banners=@(@{eyebrow='MADE FOR LITTLE ADVENTURES';title='Everyday outfits they live in.';subtitle='Soft cotton. Happy colours. Newborn to 14 years.';buttonLabel='Explore the collection';categoryId=$null;imageUrl=$assets['ok-strip-clothing'];backgroundColor='#d6cec2';textColor='#51483b'},@{eyebrow='LITTLE PRICES, BIG SMILES';title='Their next favourite, for a little less.';subtitle='Discover selected styles with lovely savings.';buttonLabel='Shop little favourites';categoryId=$categories['Little Offers'];imageUrl=$assets['ok-band-bg-sale'];backgroundColor='#ae4c39';textColor='#ffffff'})
$content.sections=@(@{eyebrow='MOST LOVED';title='Small styles. Big favourites.';subtitle='Soft essentials for all their everyday adventures.';categoryId=$null;productIds=@($products|Select-Object -First 8|ForEach-Object id)},@{eyebrow='A LITTLE TREAT';title='Lovely finds, little prices';subtitle='Selected styles with a little something off.';categoryId=$categories['Little Offers'];productIds=@($merch|Where-Object compareAtPrice -GT 0|ForEach-Object productId)},@{eyebrow='HELLO, LITTLE ONE';title='Made for their first adventures';subtitle='Gentle cotton for the smallest members of the family.';categoryId=$categories['Baby Boys'];productIds=@($products|Where-Object name -Match 'Baby'|Select-Object -First 8|ForEach-Object id)})
$content.benefits=@(@{title='Soft, everyday comfort';subtitle='Cotton favourites for little wardrobes'},@{title='A size for every stage';subtitle='Newborn through growing-up years'},@{title='Delivered to your door';subtitle='Choose your area at checkout'},@{title='Here when you need us';subtitle='Manage orders & returns online'})
$content.translations=Get-Content (Join-Path $PSScriptRoot 'demo-data/kids-arabic.json') -Raw|ConvertFrom-Json -AsHashtable;$content.merchandising=$merch;$null=Merchant PUT 'storefront' $content
$gov=Merchant POST 'delivery/governorates' @{name='Cairo';isActive=$true};$null=Merchant POST 'delivery/areas' @{name='Cairo demo delivery';governorateId=$gov.id;currencyCode='EGP';shippingFee=65;isActive=$true}
$credits|ConvertTo-Json -Depth 5|Set-Content (Join-Path $private 'kids-demo-image-sources.json')
Write-Output ('Kids demo ready: '+$products.Count+' clothing products. http://kids.localhost:4300/ . Merchant credentials are saved privately in src/Shop.Api/obj/kids-demo-access.json.')
