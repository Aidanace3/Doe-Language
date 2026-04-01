param(
  [string]$Runtime = "win-x64",
  [string]$Configuration = "Release"
)

$project = Join-Path $PSScriptRoot 'Other_Bullshit\Doe-Language.csproj'
$output = Join-Path $PSScriptRoot ("published\" + $Runtime)

dotnet publish $project `
  -c $Configuration `
  -r $Runtime `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:PublishTrimmed=false `
  -o $output

exit $LASTEXITCODE
