param(
  [string]$Runtime = "win-x64",
  [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"

$artifactsRoot = Join-Path $PSScriptRoot ("artifacts\" + $Version)
$runtimeOut = Join-Path $PSScriptRoot ("published\" + $Runtime)
$extensionDir = Join-Path $PSScriptRoot "Dough"
$installerOut = Join-Path $PSScriptRoot "installer\bin\Release"

New-Item -ItemType Directory -Force -Path $artifactsRoot | Out-Null

& (Join-Path $PSScriptRoot "publish-runtime.ps1") -Runtime $Runtime -Configuration Release
if ($LASTEXITCODE -ne 0) {
  exit $LASTEXITCODE
}

$runtimeArtifactDir = Join-Path $artifactsRoot ("runtime-" + $Runtime)
New-Item -ItemType Directory -Force -Path $runtimeArtifactDir | Out-Null
Copy-Item (Join-Path $runtimeOut "*") $runtimeArtifactDir -Recurse -Force

& (Join-Path $PSScriptRoot "build-msi.ps1") -Runtime $Runtime -Version $Version -Configuration Release -InstallerConfiguration Release
if ($LASTEXITCODE -ne 0) {
  exit $LASTEXITCODE
}

$msi = Get-ChildItem -Path $installerOut -Filter "dough-runtime-*.msi" | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
if ($null -eq $msi) {
  throw "Expected an MSI artifact in $installerOut."
}

Copy-Item $msi.FullName $artifactsRoot -Force

Push-Location $extensionDir
try {
  npm ci
  if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
  }

  npx @vscode/vsce package --no-dependencies
  if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
  }

  $vsix = Get-ChildItem -Path $extensionDir -Filter "dough-language-$Version.vsix" | Select-Object -First 1
  if ($null -eq $vsix) {
    throw "Expected VSIX artifact dough-language-$Version.vsix was not created."
  }

  Copy-Item $vsix.FullName $artifactsRoot -Force
}
finally {
  Pop-Location
}

Write-Host ("Release artifacts created in " + $artifactsRoot)
