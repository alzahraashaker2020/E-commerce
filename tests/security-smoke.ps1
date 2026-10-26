# Requires PowerShell 7 and a built Shop.Api Debug/net7.0 output.
# Uses a new temporary SQLite database, never the application's normal database.
param(
    [string]$ApiDll,
    [string]$AdditionalChecks,
    [string]$SeedDatabase,
    [switch]$PostgresTenants
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$runDir = Join-Path ([IO.Path]::GetTempPath()) ('shop-security-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $runDir | Out-Null
if ($SeedDatabase) { Copy-Item -LiteralPath $SeedDatabase -Destination (Join-Path $runDir 'test.db') }
Copy-Item (Join-Path $repoRoot 'src/Shop.Api/appsettings.json') (Join-Path $runDir 'appsettings.json')
$dll = if ($ApiDll) { (Resolve-Path -LiteralPath $ApiDll).Path } else { Join-Path $repoRoot 'src/Shop.Api/bin/Debug/net7.0/Shop.Api.dll' }
if (!(Test-Path $dll)) { throw 'Build Shop.sln before running this test.' }

# Ask the OS for an available loopback port; the API runs only on loopback.
$listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
$listener.Start()
$port = $listener.LocalEndpoint.Port
$listener.Stop()
$base = "http://127.0.0.1:$port"
$envValues = @{
    'Database__Provider' = 'Sqlite'
    'ConnectionStrings__SqliteConnection' = 'Data Source=' + (Join-Path $runDir 'test.db')
    'ASPNETCORE_URLS' = $base
    'ASPNETCORE_ENVIRONMENT' = 'Development'
    'Platform__DefaultStoreHost' = '127.0.0.1'
    'ConnectionStrings__PlatformConnection' = 'Data Source=' + (Join-Path $runDir 'platform.db')
    'Platform__BootstrapUsername' = 'platform-test'
    'Platform__BootstrapPassword' = 'Platform-test-' + [guid]::NewGuid().ToString('N')
    'StoreConnections__Alpha__Provider' = 'Sqlite'
    'StoreConnections__Alpha__ConnectionString' = 'Data Source=' + (Join-Path $runDir 'alpha.db')
    'StoreConnections__Beta__Provider' = 'Sqlite'
    'StoreConnections__Beta__ConnectionString' = 'Data Source=' + (Join-Path $runDir 'beta.db')
    'StoreConnections__Alias__Provider' = 'Sqlite'
    'StoreConnections__Alias__ConnectionString' = 'Data Source=' + (Join-Path $runDir 'alpha.db')
    'Email__Enabled' = 'false'
}
$previous = @{}
if ($PostgresTenants) {
    if (!$env:SHOP_TEST_POSTGRES) { throw 'An isolated PostgreSQL test cluster is required.' }
    foreach ($key in @('Alpha','Beta')) {
        $databaseName = 'shop_tenant_' + $key.ToLowerInvariant() + '_' + [guid]::NewGuid().ToString('N')
        $connection = [regex]::Replace($env:SHOP_TEST_POSTGRES, '(?i)(^|;)Database=[^;]*', ('${1}Database=' + $databaseName))
        $envValues["StoreConnections__${key}__Provider"] = 'Postgres'
        $envValues["StoreConnections__${key}__ConnectionString"] = $connection
    }
    $envValues.StoreConnections__Alias__Provider = 'Postgres'
    $envValues.StoreConnections__Alias__ConnectionString = $envValues.StoreConnections__Alpha__ConnectionString
}
$apiProcess = $null
$script:checks = 0

function Request($method, $path, $token = '', $body = $null, $trackingCode = '') {
    if ($path -eq '/api/checkout' -and $method -eq 'POST' -and $body -is [System.Collections.IDictionary] -and !$body.Contains('idempotencyKey')) {
        $body = @{} + $body
        $body.idempotencyKey = [guid]::NewGuid().ToString()
    }
    $headers = @{}
    if ($trackingCode) { $headers['X-Order-Tracking-Token']=$trackingCode }
    if ($token) { $headers.Authorization = 'Bearer ' + $token }
    $args = @{
        Uri = $base + $path; Method = $method; Headers = $headers
        SkipHttpErrorCheck = $true; TimeoutSec = 5
    }
    if ($null -ne $body) {
        $args.ContentType = 'application/json'
        $args.Body = $body | ConvertTo-Json -Depth 8 -Compress
    }
    elseif ($path -eq '/api/merchant/uploads' -and $method -eq 'POST') {
        $args.ContentType = 'multipart/form-data; boundary=security-test-boundary'
        $args.Body = "--security-test-boundary--`r`n"
    }
    elseif ($method -in @('POST','PUT','PATCH')) {
        $args.ContentType = 'application/json'
        $args.Body = '{}'
    }
    Invoke-WebRequest @args
}

function ExpectStatus($response, $expected, $name) {
    if ([int]$response.StatusCode -ne $expected) {
        throw "$name expected HTTP $expected, received $($response.StatusCode). Response: $($response.Content)"
    }
    $script:checks++
}

try {
    foreach ($key in $envValues.Keys) {
        $previous[$key] = [Environment]::GetEnvironmentVariable($key, 'Process')
        [Environment]::SetEnvironmentVariable($key, $envValues[$key], 'Process')
    }
    $apiProcess = Start-Process dotnet -ArgumentList @($dll, '--contentRoot', ('"' + $runDir + '"')) `
        -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $runDir 'api.log') `
        -RedirectStandardError (Join-Path $runDir 'api-error.log')
    $ready = $false
    for ($i = 0; $i -lt 60; $i++) {
        if ($apiProcess.HasExited) { throw 'Test API exited during startup; inspect test logs.' }
        try {
            $response = Request GET '/swagger/v1/swagger.json'
            if ([int]$response.StatusCode -eq 200) { $ready = $true; break }
        } catch { }
        Start-Sleep -Milliseconds 300
    }
    if (!$ready) { throw 'Test API was not reachable on loopback.' }
    $schema = $response.Content | ConvertFrom-Json
    $customerLogin = Request POST '/api/account/login' '' @{email='jordan@demo.local';password='Admin@12345'}
    ExpectStatus $customerLogin 200 'Customer login'
    $customerToken = ($customerLogin.Content | ConvertFrom-Json).data.token
    if (!$customerToken) { throw 'Customer login did not return a token.' }
    $adminLogin = Request POST '/api/merchant/auth/login' '' @{username='admin';password='Admin@12345'}
    ExpectStatus $adminLogin 200 'Admin login'
    $adminToken = ($adminLogin.Content | ConvertFrom-Json).data.token
    if (!$adminToken) { throw 'Admin login did not return a token.' }

    # Exercise every documented admin action, including writes/uploads.
    # An empty payload must not reach validation or persistence for a customer.
    $adminPaths = @($schema.paths.PSObject.Properties | Where-Object { $_.Name -like '/api/merchant/*' -and $_.Name -notlike '/api/merchant/auth/*' -and $_.Name -notlike '/api/merchant/operations/*' })
    if (!$adminPaths.Count) { throw 'No admin endpoints found in API schema.' }
    foreach ($entry in $adminPaths) {
        $path = $entry.Name -replace '\{[^}]+\}', '1'
        foreach ($operation in $entry.Value.PSObject.Properties) {
            $method = $operation.Name.ToUpperInvariant()
            if ($method -notin @('GET','POST','PUT','PATCH','DELETE')) { continue }
            ExpectStatus (Request $method $path) 401 "Anonymous $method $path"
            ExpectStatus (Request $method $path $customerToken) 403 "Customer $method $path"
        }
    }
    foreach ($path in @('products','categories','sizes','colors','orders','customers','settings','discounts','inventory/low-stock','sales/overview')) {
        ExpectStatus (Request GET "/api/merchant/$path" $adminToken) 200 "Admin GET $path"
    }
    ExpectStatus (Request POST '/api/merchant/uploads' $adminToken) 400 'Admin upload reaches file validation'
    ExpectStatus (Request GET '/api/account/profile' $customerToken) 200 'Customer can access own profile'
    ExpectStatus (Request GET '/api/account/profile' $adminToken) 403 'Admin cannot impersonate customer profile'
    ExpectStatus (Request GET '/api/catalog') 200 'Public catalog remains accessible'
    ExpectStatus (Request POST '/api/merchant/auth/login' '' @{username='admin';password='incorrect'}) 401 'Invalid admin password'
    foreach ($provider in @('Google','Facebook')) {
        $social = Request POST '/api/account/social' '' @{provider=$provider;credential='attacker:jordan@demo.local:Fake User'}
        ExpectStatus $social 503 "$provider fabricated credential rejected"
        if (($social.Content | ConvertFrom-Json).data.token) { throw 'Social endpoint issued a token.' }
    }
    # Local registration still works after removing the social bypass.
    $registration = Request POST '/api/account/register' '' @{
        fullName='Security Test';email=('security-'+[guid]::NewGuid().ToString('N')+'@example.invalid');password='Test@12345'
    }
    ExpectStatus $registration 200 'Customer registration'
    $newToken = ($registration.Content | ConvertFrom-Json).data.token
    if (!$newToken) { throw 'Registration did not return a token.' }
    ExpectStatus (Request GET '/api/merchant/products' $newToken) 403 'New customer cannot access admin'
    if ($AdditionalChecks) { . (Resolve-Path -LiteralPath $AdditionalChecks).Path }
    Write-Output "PASS: $script:checks HTTP checks. Isolated test data: $runDir"
} finally {
    if ($apiProcess -and !$apiProcess.HasExited) { Stop-Process -Id $apiProcess.Id }
    foreach ($key in $previous.Keys) {
        [Environment]::SetEnvironmentVariable($key, $previous[$key], 'Process')
    }
}
