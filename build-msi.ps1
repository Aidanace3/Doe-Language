param(
  [string]$Runtime = "win-x64",
  [string]$Configuration = "Release",
  [string]$Version = "",
  [string]$InstallerConfiguration = "Release"
)

$ErrorActionPreference = "Stop"

$runtimeProject = Join-Path $PSScriptRoot "Other_Bullshit\Doe-Language.csproj"
$installerProject = Join-Path $PSScriptRoot "installer\Dough.Runtime.Installer.wixproj"
$runtimeOut = Join-Path $PSScriptRoot ("published\" + $Runtime)

if ([string]::IsNullOrWhiteSpace($Version)) {
  [xml]$projectXml = Get-Content -Path $runtimeProject
  $Version = $projectXml.Project.PropertyGroup.Version
}

& (Join-Path $PSScriptRoot "publish-runtime.ps1") -Runtime $Runtime -Configuration $Configuration
if ($LASTEXITCODE -ne 0) {
  exit $LASTEXITCODE
}

$installerPlatform = switch ($Runtime.ToLowerInvariant()) {
  "win-x64" { "x64" }
  "win-arm64" { "arm64" }
  "win-x86" { "x86" }
  default {
    throw "Unsupported MSI runtime '$Runtime'. Expected win-x64, win-arm64, or win-x86."
  }
}

dotnet build $installerProject `
  -c $InstallerConfiguration `
  -p:InstallerPlatform=$installerPlatform `
  -p:RuntimeVersion=$Version `
  -p:RuntimePayloadDir="$runtimeOut"

exit $LASTEXITCODE
