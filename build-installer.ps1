#requires -Version 5.1
<#
    Publishes ShopDocsV2 as a self-contained win-x64 app and builds the
    per-user MSI installer around it.

    Output: installer\ShopDocsV2.Installer\bin\Release\ShopDocsV2Setup.msi
#>
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$repoRoot = $PSScriptRoot
$publishDir = Join-Path $repoRoot "publish\$Runtime"

Write-Host "Publishing ShopDocsV2.WinForms ($Runtime, self-contained)..." -ForegroundColor Cyan
if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

dotnet publish "$repoRoot\src\ShopDocsV2.WinForms\ShopDocsV2.WinForms.csproj" `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

Write-Host "Building MSI installer..." -ForegroundColor Cyan
dotnet build "$repoRoot\installer\ShopDocsV2.Installer\ShopDocsV2.Installer.wixproj" `
    -c $Configuration `
    -p:PublishDir=$publishDir
if ($LASTEXITCODE -ne 0) { throw "MSI build failed." }

$msi = Get-ChildItem "$repoRoot\installer\ShopDocsV2.Installer\bin\$Configuration" -Filter "*.msi" | Select-Object -First 1
Write-Host "`nInstaller ready: $($msi.FullName)" -ForegroundColor Green
