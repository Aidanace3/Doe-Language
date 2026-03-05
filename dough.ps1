param(
  [Parameter(ValueFromRemainingArguments = $true)]
  [string[]]$Args
)

$project = Join-Path $PSScriptRoot 'Other_Bullshit\Doe-Language.csproj'
$exe = Join-Path $PSScriptRoot 'Other_Bullshit\bin\Debug\net10.0\Dough.exe'

if (-not (Test-Path $exe)) {
  dotnet build $project
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

& $exe @Args
exit $LASTEXITCODE
