param([Parameter(Mandatory=$true)][ValidatePattern('^[a-f0-9]{32}$')][string]$BackupId,[Parameter(Mandatory=$true)][ValidatePattern('^(default|[a-f0-9]{32})$')][string]$StoreId,[string]$ApiDll)
$ErrorActionPreference='Stop'
$apiRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../src/Shop.Api'))
if(!$ApiDll){$ApiDll=Join-Path $apiRoot 'bin/Debug/net7.0/Shop.Api.dll'}
$ApiDll=(Resolve-Path -LiteralPath $ApiDll).Path
Push-Location $apiRoot
try{& dotnet $ApiDll --contentRoot $apiRoot --restore-backup $BackupId --confirm-store $StoreId;if($LASTEXITCODE -ne 0){throw 'Recovery did not complete. Review the local recovery error; the API must be stopped first.'}}
finally{Pop-Location}
