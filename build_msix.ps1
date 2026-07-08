$ErrorActionPreference = "Stop"

# 1. Resize images
$baseIconPath = "img\app_icon.png"
$assetsDir = "msix_build\Assets"
New-Item -ItemType Directory -Force -Path $assetsDir

Add-Type -AssemblyName System.Drawing

function Resize-Image {
    param([string]$in, [string]$out, [int]$w, [int]$h)
    $img = [System.Drawing.Image]::FromFile($in)
    $bmp = New-Object System.Drawing.Bitmap($w, $h)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear([System.Drawing.Color]::Transparent)
    $g.DrawImage($img, 0, 0, $w, $h)
    $bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose()
    $bmp.Dispose()
    $img.Dispose()
}

Resize-Image $baseIconPath "$assetsDir\Square150x150Logo.png" 150 150
Resize-Image $baseIconPath "$assetsDir\Square44x44Logo.png" 44 44
Resize-Image $baseIconPath "$assetsDir\SplashScreen.png" 620 300
Resize-Image $baseIconPath "$assetsDir\StoreLogo.png" 50 50

# 1.5 Publish Application
Write-Host "Publishing the application..."
dotnet publish "Teniko.csproj" -c Release -r win-x64 --self-contained true -o "msix_build"

# 2. Write AppxManifest.xml
$manifestContent = @"
<?xml version="1.0" encoding="utf-8"?>
<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
         xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
         xmlns:uap5="http://schemas.microsoft.com/appx/manifest/uap/windows10/5"
         xmlns:desktop4="http://schemas.microsoft.com/appx/manifest/desktop/windows10/4"
         xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
         IgnorableNamespaces="uap uap5 desktop4 rescap">

  <Identity Name="ZenGoM.Teniko"
            Publisher="CN=E2338FF3-89FC-4D73-967C-BC126F7A8857"
            Version="1.0.4.0"
            ProcessorArchitecture="x64" />

  <Properties>
    <DisplayName>Teniko</DisplayName>
    <PublisherDisplayName>ZenGoM</PublisherDisplayName>
    <Logo>Assets\StoreLogo.png</Logo>
    <Description>TIFF to PDF Converter</Description>
  </Properties>

  <Resources>
    <Resource Language="ja-jp" />
  </Resources>

  <Dependencies>
    <TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.17763.0" MaxVersionTested="10.0.19041.0" />
  </Dependencies>

  <Capabilities>
    <rescap:Capability Name="runFullTrust" />
  </Capabilities>

  <Applications>
    <Application Id="TenikoApp"
                 Executable="teniko.exe"
                 EntryPoint="Windows.FullTrustApplication"
                 desktop4:Subsystem="console"
                 desktop4:SupportsMultipleInstances="true">
      <uap:VisualElements DisplayName="Teniko"
                          Description="TIFF to PDF Converter"
                          BackgroundColor="transparent"
                          Square150x150Logo="Assets\Square150x150Logo.png"
                          Square44x44Logo="Assets\Square44x44Logo.png">
        <uap:DefaultTile Wide310x150Logo="Assets\SplashScreen.png" />
        <uap:SplashScreen Image="Assets\SplashScreen.png" />
      </uap:VisualElements>
      <Extensions>
        <uap5:Extension Category="windows.appExecutionAlias">
          <uap5:AppExecutionAlias>
            <uap5:ExecutionAlias Alias="teniko.exe" />
          </uap5:AppExecutionAlias>
        </uap5:Extension>
      </Extensions>
    </Application>
  </Applications>
</Package>
"@

Set-Content -Path "msix_build\AppxManifest.xml" -Value $manifestContent -Encoding UTF8

# 3. Create Cert
$cert = New-SelfSignedCertificate -Type Custom -Subject "CN=E2338FF3-89FC-4D73-967C-BC126F7A8857" -KeyUsage DigitalSignature -FriendlyName "Teniko MSIX Cert" -CertStoreLocation "Cert:\CurrentUser\My" -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}") -NotAfter (Get-Date).AddYears(1)
$pwd = ConvertTo-SecureString -String "teniko" -Force -AsPlainText
Export-PfxCertificate -cert $cert -FilePath "TenikoCert.pfx" -Password $pwd

# 4. Pack MSIX
$makeappx = (Get-ChildItem "C:\Program Files (x86)\Windows Kits\10\bin" -Recurse -Filter "makeappx.exe" | Where-Object { $_.DirectoryName -match "x64" } | Select-Object -First 1).FullName
& $makeappx pack /d msix_build /p Teniko.msix /o

# 5. Sign MSIX
$signtool = (Get-ChildItem "C:\Program Files (x86)\Windows Kits\10\bin" -Recurse -Filter "signtool.exe" | Where-Object { $_.DirectoryName -match "x64" } | Select-Object -First 1).FullName
& $signtool sign /fd SHA256 /a /f TenikoCert.pfx /p teniko Teniko.msix

Write-Host "MSIX Packaging completed successfully."
