# E-Helper Build and Sign Script
# Builds clean, standalone and framework-dependent binaries with full assembly metadata and Authenticode digital signing.

param(
    [switch]$SkipSign = $false,
    [string]$CertSubject = "CN=E-Helper Open Source Community"
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Resolve-Path "$PSScriptRoot\.."
$ProjectPath = "$ProjectRoot\src\EHelper\EHelper.csproj"
$StandaloneDir = "$ProjectRoot\publish-standalone"
$FrameworkDir = "$ProjectRoot\publish"
$CertsDir = "$ProjectRoot\certs"

if (-not (Test-Path $CertsDir)) {
    New-Item -ItemType Directory -Force -Path $CertsDir | Out-Null
}

Write-Host "==> [1/3] Building E-Helper Standalone Single-File..." -ForegroundColor Cyan
dotnet publish $ProjectPath -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $StandaloneDir

Write-Host "==> [2/3] Building E-Helper Framework-Dependent..." -ForegroundColor Cyan
dotnet publish $ProjectPath -c Release -r win-x64 --self-contained false -o $FrameworkDir

$StandaloneExe = "$StandaloneDir\EHelper.exe"
$FrameworkExe = "$FrameworkDir\EHelper.exe"

if (-not (Test-Path $StandaloneExe)) {
    Write-Error "Build failed: $StandaloneExe does not exist."
    exit 1
}

$StandaloneSize = (Get-Item $StandaloneExe).Length / 1MB
$FrameworkSize = (Get-Item $FrameworkExe).Length / 1MB
Write-Host "==> Standalone Size: $([Math]::Round($StandaloneSize, 2)) MB" -ForegroundColor Green
Write-Host "==> Framework-Dependent Size: $([Math]::Round($FrameworkSize, 2)) MB" -ForegroundColor Green

if (-not $SkipSign) {
    Write-Host "==> [3/3] Authenticode Digital Signing..." -ForegroundColor Cyan
    $cert = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert | Where-Object { $_.Subject -match "E-Helper" } | Select-Object -First 1

    if (-not $cert) {
        Write-Host "==> Generating local Code Signing Certificate..." -ForegroundColor Yellow
        $cert = New-SelfSignedCertificate `
            -Type CodeSigningCert `
            -Subject $CertSubject `
            -CertStoreLocation "Cert:\CurrentUser\My" `
            -HashAlgorithm "SHA256" `
            -NotAfter (Get-Date).AddYears(5)
    }

    # Export public certificate
    $publicCertPath = "$CertsDir\EHelper_CodeSigning.cer"
    [System.IO.File]::WriteAllBytes($publicCertPath, $cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert))

    # Add to TrustedPublisher & TrustedPeople (silent, no interactive popup)
    try {
        certutil -addstore -user TrustedPublisher $publicCertPath | Out-Null
        certutil -addstore -user TrustedPeople $publicCertPath | Out-Null
    } catch {
        # Non-critical if certutil fails
    }

    # Sign Standalone executable with DigiCert timestamp
    Write-Host "==> Signing Standalone binary with timestamp..." -ForegroundColor Cyan
    try {
        Set-AuthenticodeSignature -FilePath $StandaloneExe -Certificate $cert -HashAlgorithm SHA256 -TimestampServer "http://timestamp.digicert.com" | Out-Null
    } catch {
        Write-Host "Timestamp server unavailable, signing without timestamp..." -ForegroundColor Yellow
        Set-AuthenticodeSignature -FilePath $StandaloneExe -Certificate $cert -HashAlgorithm SHA256 | Out-Null
    }

    # Sign Framework-dependent executable with DigiCert timestamp
    Write-Host "==> Signing Framework-Dependent binary with timestamp..." -ForegroundColor Cyan
    try {
        Set-AuthenticodeSignature -FilePath $FrameworkExe -Certificate $cert -HashAlgorithm SHA256 -TimestampServer "http://timestamp.digicert.com" | Out-Null
    } catch {
        Set-AuthenticodeSignature -FilePath $FrameworkExe -Certificate $cert -HashAlgorithm SHA256 | Out-Null
    }

    $sigStandalone = Get-AuthenticodeSignature -FilePath $StandaloneExe
    $sigFramework = Get-AuthenticodeSignature -FilePath $FrameworkExe
    Write-Host "==> Standalone Signature: $($sigStandalone.SignerCertificate.Subject) [Status: $($sigStandalone.Status)]" -ForegroundColor Green
    Write-Host "==> Framework Signature:  $($sigFramework.SignerCertificate.Subject) [Status: $($sigFramework.Status)]" -ForegroundColor Green
}

Write-Host "==> Build & Digital Signing complete!" -ForegroundColor Green
Write-Host "    Standalone Output: $StandaloneExe"
Write-Host "    Framework Output:  $FrameworkExe"
