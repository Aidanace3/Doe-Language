param(
  [Parameter(ValueFromRemainingArguments = $true)]
  [string[]]$Args
)

$project = Join-Path $PSScriptRoot 'Other_Bullshit\Doe-Language.csproj'
$standalone = Join-Path $PSScriptRoot 'published\win-x64\Dough.exe'
$releaseDll = Join-Path $PSScriptRoot 'Other_Bullshit\bin\Release\net10.0-windows\Dough.dll'
$debugDll = Join-Path $PSScriptRoot 'Other_Bullshit\bin\Debug\net10.0-windows\Dough.dll'
$legacyReleaseDll = Join-Path $PSScriptRoot 'Other_Bullshit\bin\Release\net10.0\Dough.dll'
$legacyDebugDll = Join-Path $PSScriptRoot 'Other_Bullshit\bin\Debug\net10.0\Dough.dll'
$selectedDll = $null

if (Test-Path $standalone) {
  & $standalone @Args
  exit $LASTEXITCODE
}

if (-not (Test-Path $releaseDll) -and -not (Test-Path $debugDll) -and -not (Test-Path $legacyReleaseDll) -and -not (Test-Path $legacyDebugDll)) {
  dotnet build $project -c Release
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

if ((Test-Path $releaseDll) -and (Test-Path $debugDll)) {
  $releaseInfo = Get-Item $releaseDll
  $debugInfo = Get-Item $debugDll
  $selectedDll = if ($debugInfo.LastWriteTimeUtc -gt $releaseInfo.LastWriteTimeUtc) { $debugDll } else { $releaseDll }
} elseif (Test-Path $releaseDll) {
  $selectedDll = $releaseDll
} elseif (Test-Path $debugDll) {
  $selectedDll = $debugDll
} elseif (Test-Path $legacyReleaseDll) {
  $selectedDll = $legacyReleaseDll
} elseif (Test-Path $legacyDebugDll) {
  $selectedDll = $legacyDebugDll
}

dotnet $selectedDll @Args
exit $LASTEXITCODE
