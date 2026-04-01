@echo off
setlocal
set "PROJECT=%~dp0Other_Bullshit\Doe-Language.csproj"
set "STANDALONE=%~dp0published\win-x64\Dough.exe"
set "RELEASE_DLL=%~dp0Other_Bullshit\bin\Release\net10.0\Dough.dll"
set "DEBUG_DLL=%~dp0Other_Bullshit\bin\Debug\net10.0\Dough.dll"

if exist "%STANDALONE%" (
  "%STANDALONE%" %*
  exit /b %errorlevel%
)

if not exist "%RELEASE_DLL%" if not exist "%DEBUG_DLL%" (
  dotnet build "%PROJECT%" -c Release
  if errorlevel 1 exit /b %errorlevel%
)

if exist "%RELEASE_DLL%" (
  dotnet "%RELEASE_DLL%" %*
  exit /b %errorlevel%
)

dotnet "%DEBUG_DLL%" %*
exit /b %errorlevel%
