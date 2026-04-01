param(
  [Parameter(ValueFromRemainingArguments = $true)]
  [string[]]$Args
)

$project = Join-Path $PSScriptRoot 'Other_Bullshit\Doe-Language.csproj'
$standalone = Join-Path $PSScriptRoot 'published\win-x64\Dough.exe'
$releaseDll = Join-Path $PSScriptRoot 'Other_Bullshit\bin\Release\net10.0\Dough.dll'
$debugDll = Join-Path $PSScriptRoot 'Other_Bullshit\bin\Debug\net10.0\Dough.dll'

if (Test-Path $standalone) {
  & $standalone @Args
  exit $LASTEXITCODE
}

if (-not (Test-Path $releaseDll) -and -not (Test-Path $debugDll)) {
  dotnet build $project -c Release
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

if (Test-Path $releaseDll) {
  dotnet $releaseDll @Args
  exit $LASTEXITCODE
}

dotnet $debugDll @Args
exit $LASTEXITCODE
