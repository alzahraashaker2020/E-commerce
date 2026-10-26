. (Join-Path $PSScriptRoot 'checkout-integrity-checks.ps1')

function UploadBytes($bytes,$name,$mime,$expected=400){
    $http=[Net.Http.HttpClient]::new()
    $http.DefaultRequestHeaders.Authorization=[Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer',$adminToken)
    $multipart=[Net.Http.MultipartFormDataContent]::new()
    $content=[Net.Http.ByteArrayContent]::new([byte[]]$bytes)
    $content.Headers.ContentType=[Net.Http.Headers.MediaTypeHeaderValue]::new($mime)
    $multipart.Add($content,'file',$name)
    try {
        $r=$http.PostAsync("$base/api/merchant/uploads",$multipart).GetAwaiter().GetResult()
        $response=@{StatusCode=[int]$r.StatusCode;Content=$r.Content.ReadAsStringAsync().GetAwaiter().GetResult()}
        ExpectStatus $response $expected "Upload $name as $mime"
        if($expected -eq 200){return ($response.Content|ConvertFrom-Json).data}
    }finally{$multipart.Dispose();$http.Dispose()}
}
$png=[IO.File]::ReadAllBytes((Join-Path $runDir 'sample.png'))
$null=UploadBytes ([Text.Encoding]::UTF8.GetBytes('<script>alert(1)</script>')) 'fake.png' 'image/png'
$null=UploadBytes ([Text.Encoding]::UTF8.GetBytes('<svg xmlns="http://www.w3.org/2000/svg" onload="alert(1)"/>')) 'image.svg' 'image/svg+xml'
$null=UploadBytes $png 'image.png.html' 'image/png'
$null=UploadBytes $png 'image.jpg' 'image/jpeg'
$null=UploadBytes $png 'image.png' 'text/plain'
$null=UploadBytes $png[0..50] 'cut.png' 'image/png'
$null=UploadBytes ([byte[]]::new(5*1024*1024+1)) 'large.png' 'image/png'
Add-Type -AssemblyName System.Drawing
$bitmap=[Drawing.Bitmap]::new(9000,1)
$buffer=[IO.MemoryStream]::new()
try{$bitmap.Save($buffer,[Drawing.Imaging.ImageFormat]::Png);$null=UploadBytes $buffer.ToArray() 'wide.png' 'image/png'}finally{$buffer.Dispose();$bitmap.Dispose()}
$payload=[Text.Encoding]::UTF8.GetBytes('UNTRUSTED-APPENDED-PAYLOAD-<script>alert(1)</script>')
$polyglot=[byte[]]($png+$payload)
$safePath=UploadBytes $polyglot 'image.png' 'image/png' 200
ExpectEqual ([IO.Path]::GetExtension($safePath)) '.webp' 'Server chooses safe output extension'
$served=Request GET $safePath
ExpectStatus $served 200 'Clean image available'
ExpectEqual ([string]$served.Headers['X-Content-Type-Options']) 'nosniff' 'Image response disables content sniffing'
$clean=[IO.File]::ReadAllBytes((Join-Path $runDir ('wwwroot'+$safePath.Replace('/','\'))))
ExpectEqual ([Text.Encoding]::ASCII.GetString($clean).Contains('UNTRUSTED-APPENDED-PAYLOAD')) $false 'Trailing payload is stripped by re-encoding'
$null=UploadBytes $clean 'clean.webp' 'image/webp' 200
$legacy=Join-Path $runDir 'wwwroot/uploads/legacy-active.svg'
[IO.File]::WriteAllText($legacy,'<svg onload="alert(1)"/>')
ExpectStatus (Request GET '/uploads/legacy-active.svg') 404 'Legacy active image formats are no longer served'

$number=$confirmation.orderNumber
$proof=$confirmation.trackingToken
ExpectEqual ($proof.Length -gt 30) $true 'Confirmation includes private tracking code'
ExpectStatus (Request GET "/api/ordertracking/$number") 404 'Order number alone reveals no details'
ExpectStatus (Request GET '/api/ordertracking/nonexistent-order') 404 'Unknown order returns the same status'
ExpectStatus (Request GET '/api/ordertracking/by-email?email=jordan%40demo.local') 404 'Email-only lookup disabled'
ExpectStatus (Request GET "/api/ordertracking/$number" $customerToken) 404 'Another customer cannot track the order'
ExpectStatus (Request GET "/api/ordertracking/$number" $newToken) 200 'Authenticated owner can track without a guest code'
ExpectStatus (Request GET "/api/ordertracking/$number" $adminToken) 404 'Merchant must use its own protected management endpoint'
ExpectStatus (Request GET "/api/ordertracking/$number" '' $null 'invalid-code') 404 'Wrong code reveals no details'
$tracked=Request GET "/api/ordertracking/$number" '' $null $proof
ExpectStatus $tracked 200 'Private proof enables guest tracking'
ExpectEqual ([string]$tracked.Headers['Cache-Control']) 'no-store' 'Private tracking is not cached'
ExpectStatus (Request GET "/api/ordertracking/$guestNumber" '' $null $proof) 404 'Code cannot track a different order'
ExpectStatus (Request GET "/api/ordertracking/$number`?trackingToken=$proof") 404 'Code in URL is not accepted'
$rows=JsonRequest GET '/api/merchant/orders?pageSize=100'
$row=$rows.items|Where-Object orderNumber -EQ $number
$ownerAccess=JsonRequest GET "/api/merchant/orders/$($row.id)/tracking-access"
ExpectEqual $ownerAccess.trackingToken $proof 'Merchant can recover the code for an existing order'
ExpectStatus (Request GET "/api/merchant/orders/$($row.id)/tracking-access" $newToken) 403 'Customer cannot mint tracking codes'
$limited=$false
for($i=0;$i -lt 35;$i++) { $r=Request GET '/api/ordertracking/unknown-probe'; if($r.StatusCode -eq 429){$limited=$true;break} }
ExpectEqual $limited $true 'Repeated tracking probes are rate limited'
Write-Host 'Image validation, metadata stripping, private tracking ownership and rate limiting passed.'
