param([Parameter(Mandatory=$true)][string]$GmailAddress,[string]$StoreId='default')
$ErrorActionPreference='Stop'
if([Environment]::OSVersion.Platform -ne 'Win32NT'){throw 'Use protected environment variables for SMTP on this platform.'}
$mail=[Net.Mail.MailAddress]::new($GmailAddress)
if($mail.Address -ne $GmailAddress){throw 'Enter a plain sender email address.'}
if($StoreId -notmatch '^(default|[a-f0-9]{32})$'){throw 'Use the store ID returned by the platform.'}
$apiRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../src/Shop.Api'))
$path=Join-Path $apiRoot 'email-secrets.json'
$settings=if(Test-Path -LiteralPath $path){Get-Content -LiteralPath $path -Raw|ConvertFrom-Json -AsHashtable}else{@{}}
$secure=Read-Host 'Gmail App Password (not your normal Google password)' -AsSecureString
$pointer=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
try{
    $password=[Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer).Replace(' ','')
    if($password.Length -ne 16){throw 'Expected the 16-character Google App Password.'}
    $entry=@{Enabled=$true;SmtpHost='smtp.gmail.com';SmtpPort=587;SmtpUser=$GmailAddress;SmtpPassword=$password;From=$GmailAddress}
    if($StoreId -eq 'default'){$settings.Email=$entry}else{if(!$settings.StoreEmail){$settings.StoreEmail=@{}};$settings.StoreEmail[$StoreId]=$entry}
    if(!(Test-Path -LiteralPath $path)){New-Item -ItemType File -Path $path|Out-Null}
    $acl=[Security.AccessControl.FileSecurity]::new()
    $acl.SetAccessRuleProtection($true,$false)
    foreach($identity in @([Security.Principal.WindowsIdentity]::GetCurrent().User,[Security.Principal.SecurityIdentifier]::new('S-1-5-18'))){
        $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new($identity,'FullControl','Allow'))
    }
    Set-Acl -LiteralPath $path -AclObject $acl
    $settings|ConvertTo-Json -Depth 10|Set-Content -LiteralPath $path
    Write-Output 'Gmail SMTP configuration saved privately. Choose notification recipients in the merchant dashboard. No test email was sent.'
}finally{[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer);$password=$null;$entry=$null;$secure.Dispose()}
