param(
    [Parameter(Mandatory=$true)][string]$ApiDll,
    [Parameter(Mandatory=$true)][ValidatePattern('^[a-zA-Z0-9_-]+$')][string]$Label,
    [switch]$SqlServerTests,
    [string[]]$Suites=@('checkout-integrity-checks.ps1','deployment-profile-checks.ps1','returns-checks.ps1')
)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$dll=(Resolve-Path -LiteralPath $ApiDll).Path
$out=Join-Path $repo ('src/Shop.Api/obj/refactoring-evidence/'+$Label+'-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $out|Out-Null
$results=@()
$schema=@(Get-ChildItem (Join-Path $repo 'src/Shop.DAL/Migrations') -Filter '*.cs')+@(Get-ChildItem (Join-Path $repo 'src/Shop.DAL/Data') -Filter '*.cs')
$hashes=@{};foreach($file in $schema){$hashes[$file.FullName.Substring($repo.Length+1)]=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash}
foreach($suite in $Suites){
    $file=Join-Path $PSScriptRoot $suite
    if(!(Test-Path -LiteralPath $file) -or [IO.Path]::GetFileName($suite) -ne $suite){throw 'Use a known feature-check filename from tests.'}
    $start=[DateTime]::UtcNow
    $arguments=@('-NoProfile','-File',(Join-Path $PSScriptRoot 'security-smoke.ps1'),'-ApiDll',$dll,'-AdditionalChecks',$file)
    if($SqlServerTests){$arguments+='-SqlServerTests'}
    $output=& pwsh @arguments 2>&1
    $code=$LASTEXITCODE
    $output|Set-Content (Join-Path $out ($suite+'.log'))
    $match=[regex]::Match(($output -join "`n"),'PASS: (\d+) HTTP checks')
    $results+=@{suite=$suite;exitCode=$code;checks=if($match.Success){[int]$match.Groups[1].Value}else{$null};durationSeconds=([DateTime]::UtcNow-$start).TotalSeconds}
    Write-Output "$Label / $suite / exit $code"
}
@{label=$Label;head=(& git -C $repo rev-parse HEAD);sdk=(& dotnet --version);provider=$(if($SqlServerTests){'SQL Server store and registry'}else{'SQLite store and registry'});apiSha256=(Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash;schemaHashes=$hashes;results=$results}|ConvertTo-Json -Depth 8|Set-Content (Join-Path $out 'manifest.json')
Write-Output "Sanitized manifest: $out/manifest.json"
if(@($results|Where-Object exitCode -NE 0).Count){throw 'One or more baseline suites failed; inspect the isolated evidence.'}
