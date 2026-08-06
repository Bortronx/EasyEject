param(
    [string]$Root = 'D:\Github Projects\#1 Multi Usb Eject',
    [ValidateSet('x64', 'arm64')]
    [string]$Arch = 'x64',
    [string]$MsixPath = ''
)

# ---------------------------------------------------------------------------
# Installs the EasyEject MSIX for the current user, together with its
# Windows App SDK 1.8 runtime prerequisites (framework, main, singleton,
# DDLM) and the VC++ runtime framework package.
# ---------------------------------------------------------------------------

$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$transcript = Join-Path $here 'obj\install-msix.log'
$transcriptDir = Split-Path -Parent $transcript
New-Item -ItemType Directory -Force -Path $transcriptDir | Out-Null
Start-Transcript -Path $transcript -Force | Out-Null

# ---------------------------------------------------------------------------
# Self-elevate: AppX deployment of self-signed packages needs the signing
# certificate to be trusted at machine level (LocalMachine\Root).
# ---------------------------------------------------------------------------
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin)
{
    Write-Host 'Re-launching with administrator privileges...' -ForegroundColor Yellow
    Start-Process powershell.exe -Verb RunAs -Wait -ArgumentList (
        "-NoProfile -ExecutionPolicy Bypass -File `"$($MyInvocation.MyCommand.Path)`" -Root `"$Root`" -Arch $Arch" +
        $(if ($MsixPath) { " -MsixPath `"$MsixPath`"" } else { '' }))
    exit
}

# --- Trust the self-signed EasyEject developer certificate -----------------
if (-not $MsixPath) { $MsixPath = Join-Path $here "bin\EasyEject-$Arch.msix" }
if (-not (Test-Path $MsixPath)) { throw "MSIX not found: $MsixPath (run build-msix.ps1 first)." }

$signer = (Get-AuthenticodeSignature $MsixPath).SignerCertificate
if ($signer)
{
    Write-Host "Trusting signing certificate $($signer.Subject)..." -ForegroundColor Cyan
    if (-not (Get-ChildItem 'Cert:\LocalMachine\Root' -ErrorAction SilentlyContinue | Where-Object { $_.Thumbprint -eq $signer.Thumbprint }))
    {
        $cerPath = Join-Path $here "obj\EasyEject-$Arch.cer"
        [System.IO.File]::WriteAllBytes($cerPath, $signer.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert))
        Import-Certificate -FilePath $cerPath -CertStoreLocation 'Cert:\LocalMachine\Root' | Out-Null
    }
}

# --- VC++ runtime framework package (required by the Windows App SDK) ------
$vclibs = Get-AppxPackage -Name 'Microsoft.VCLibs.140.00.UWPDesktop' -ErrorAction SilentlyContinue
if (-not $vclibs)
{
    Write-Host 'Installing Microsoft.VCLibs (x64) framework package...' -ForegroundColor Cyan
    $vclibsUrl = 'https://aka.ms/Microsoft.VCLibs.x64.14.00.Desktop.appx'
    $vclibsPath = Join-Path $env:TEMP 'Microsoft.VCLibs.x64.14.00.Desktop.appx'
    Invoke-WebRequest -Uri $vclibsUrl -OutFile $vclibsPath -UseBasicParsing
    Add-AppxPackage -Path $vclibsPath
}
else
{
    Write-Host 'Microsoft.VCLibs already installed.' -ForegroundColor DarkGray
}

# --- Windows App SDK 1.8 runtime packages ----------------------------------
$nugetRuntime = Join-Path $env:USERPROFILE '.nuget\packages\microsoft.windowsappsdk.runtime\1.8.260710003\tools\MSIX'
if (-not (Test-Path $nugetRuntime))
{
    throw "Windows App SDK runtime MSIX files not found under $nugetRuntime"
}

$osArch = if ($Arch -eq 'x64') { 'win10-x64' } else { 'win10-arm64' }
$runtimeDir = Join-Path $nugetRuntime $osArch
$installed = Get-AppxPackage -ErrorAction SilentlyContinue

foreach ($file in Get-ChildItem $runtimeDir -Filter '*.msix')
{
    $existing = $installed | Where-Object { $_.PackageFullName -like "$($file.BaseName)*" }
    if ($existing)
    {
        Write-Host "Runtime package already installed: $($file.Name)" -ForegroundColor DarkGray
        continue
    }
    Write-Host "Installing runtime package: $($file.Name)..." -ForegroundColor Cyan
    Add-AppxPackage -Path $file.FullName
}

# --- The app itself ---------------------------------------------------------
Write-Host "Installing EasyEject..." -ForegroundColor Cyan
Add-AppxPackage -Path $MsixPath
Write-Host 'Done. EasyEject is installed under the name "EasyEject".' -ForegroundColor Green