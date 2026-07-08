$ErrorActionPreference = "Stop"
Write-Host "Installing Certificate..."
$pwd = ConvertTo-SecureString -String "teniko" -Force -AsPlainText
Import-PfxCertificate -FilePath "TenikoCert.pfx" -CertStoreLocation "Cert:\LocalMachine\TrustedPeople" -Password $pwd
Import-PfxCertificate -FilePath "TenikoCert.pfx" -CertStoreLocation "Cert:\LocalMachine\Root" -Password $pwd
Write-Host "Installing MSIX..."
Add-AppxPackage -Path "Teniko.msix"
Write-Host "Done!"
