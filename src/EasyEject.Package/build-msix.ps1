param(
    [string]$Root = 'D:\Github Projects\#1 Multi Usb Eject',
    [ValidateSet('x64', 'arm64')]
    [string]$Arch = 'x64',
    [string]$CertSubject = 'CN=EasyEject',
    [string]$CertPassword = 'EasyEject-Dev-Pass-2026',
    [switch]$FreshCert
)

$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$objDir = Join-Path $here 'obj'
$layout = Join-Path $objDir "layout\$Arch"
$outDir = Join-Path $here 'bin'
$msixPath = Join-Path $outDir "EasyEject-$Arch.msix"
$pfxPath = Join-Path $objDir "EasyEject-$Arch.pfx"

if (Test-Path $layout) { Remove-Item $layout -Recurse -Force }
New-Item -ItemType Directory -Force -Path $layout, $outDir, $objDir | Out-Null

$archTag = if ($Arch -eq 'x64') { 'x64' } else { 'arm64' }
$makeappx = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\makeappx.exe"
$signtool = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe"
if (-not (Test-Path $makeappx)) { throw "makeappx.exe not found: $makeappx" }
if (-not (Test-Path $signtool)) { throw "signtool.exe not found: $signtool" }

# ---------------------------------------------------------------------------
# 1. Publish the app: .NET self-contained, WinAppSDK framework-dependent,
#    not single-file so the payload DLLs are present in the package.
# ---------------------------------------------------------------------------
Write-Host "[1/3] Publishing EasyEject.UI ($Arch)..." -ForegroundColor Cyan
Push-Location (Join-Path $Root 'src\EasyEject.UI')
try
{
    dotnet publish ".\EasyEject.UI.csproj" -c Release -r "win-$Arch" `
        -p:PublishSingleFile=false -p:PublishTrimmed=false `
        -p:SelfContained=true -p:WindowsAppSDKSelfContained=true `
        -p:WindowsAppSdkBootstrapInitialize=false `
        -o $layout
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed (exit $LASTEXITCODE)." }

    # This is a packaged app: the WinAppSDK bootstrapper is dead weight
    # (self-contained runtime is bundled, no dynamic dependencies needed).
    # Drop it so it can never be mis-activated at startup.
    Remove-Item (Join-Path $layout 'Microsoft.WindowsAppRuntime.Bootstrap*.dll') -Force -ErrorAction SilentlyContinue

    # WinUI compiles each .xaml to a .xbf at build time and copies it into the
    # normal bin output, but not into the -o publish layout. Packaged apps
    # resolve ms-appx:///...xaml to those loose .xbf files, so without them
    # XAML parsing fails (XamlParseException 0x802B000A). Copy them over.
    $uiBin = Join-Path $Root "src\EasyEject.UI\bin\Release\net9.0-windows10.0.19041.0\win-$Arch"
    if (-not (Test-Path $uiBin)) { throw "Expected build output not found: $uiBin" }
    $copied = 0
    foreach ($xf in (Get-ChildItem $uiBin -Recurse -Filter '*.xbf'))
    {
        $rel = $xf.FullName.Substring($uiBin.Length).TrimStart('\')
        $dest = Join-Path $layout $rel
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $dest) | Out-Null
        Copy-Item -Path $xf.FullName -Destination $dest -Force
        $copied++
    }
    if ($copied -eq 0) { throw "No .xbf files found in $uiBin (XAML build output missing)." }

    # Packaged apps resolve their resource map from resources.pri (MSIX
    # convention); WinUI/MSBuild renames the app pri for MSIX packaging.
    # The raw publish leaves it as EasyEject.pri, so packaged MRT would
    # find nothing. Ship it under the name the package manager expects.
    Copy-Item (Join-Path $layout 'EasyEject.pri') (Join-Path $layout 'resources.pri') -Force
}
finally { Pop-Location }

# ---------------------------------------------------------------------------
# 2. Assemble the layout and pack with makeappx.
# ---------------------------------------------------------------------------
Write-Host "[2/3] Assembling layout and packing..." -ForegroundColor Cyan
Copy-Item (Join-Path $here 'AppxManifest.xml') $layout -Force
Copy-Item (Join-Path $Root 'assets\images') (Join-Path $layout 'Assets') -Recurse -Force

& $makeappx pack /d $layout /p $msixPath /o
if ($LASTEXITCODE -ne 0) { throw "makeappx pack failed (exit $LASTEXITCODE)." }

# ---------------------------------------------------------------------------
# 3. Create/refresh a signing certificate and sign the package.
# ---------------------------------------------------------------------------
Write-Host "[3/3] Signing..." -ForegroundColor Cyan
$cert = Get-ChildItem 'Cert:\CurrentUser\My' -CodeSigningCert -ErrorAction SilentlyContinue |
    Where-Object { $_.Subject -like "*$CertSubject*" } | Select-Object -First 1

if (-not $cert -or $SkipCert)
{
    Write-Host "Creating a new self-signed code-signing certificate ($CertSubject)." -ForegroundColor Yellow
    $cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject $CertSubject `
        -CertStoreLocation 'Cert:\CurrentUser\My' -NotAfter (Get-Date).AddYears(2)
}

[System.IO.File]::WriteAllBytes(
    $pfxPath,
    $cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Pfx, $CertPassword))

# Make the developer cert trusted for CurrentUser sideloading.
if (-not (Get-ChildItem 'Cert:\CurrentUser\Root' -ErrorAction SilentlyContinue |
    Where-Object { $_.Thumbprint -eq $cert.Thumbprint }))
{
    $cerPath = Join-Path $objDir "EasyEject-$Arch.cer"
    [System.IO.File]::WriteAllBytes(
        $cerPath,
        $cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert))
    Import-Certificate -FilePath $cerPath -CertStoreLocation 'Cert:\CurrentUser\Root' | Out-Null
}

& $signtool sign /f $pfxPath /p $CertPassword /fd SHA256 $msixPath
if ($LASTEXITCODE -ne 0) { throw "signtool failed (exit $LASTEXITCODE)." }

Write-Host ''
Write-Host "OK: $msixPath" -ForegroundColor Green
Get-Item $msixPath | Select-Object FullName, Length | Format-List