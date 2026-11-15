# Only invoked within the disposable PostgreSQL cluster and isolated HTTP harness.
. (Join-Path $PSScriptRoot 'tenant-isolation-checks.ps1')
if(!$env:SHOP_TEST_POSTGRES){throw 'Temporary PostgreSQL test cluster required'}
& dotnet test (Join-Path $repoRoot 'tests/Shop.Infrastructure.Tests/Shop.Infrastructure.Tests.csproj') --no-build --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
if($LASTEXITCODE -ne 0){throw 'PostgreSQL transaction fault-injection tests failed'}
