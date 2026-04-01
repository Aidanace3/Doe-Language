@echo off
setlocal
set "PROJECT=%~dp0Other_Bullshit\Doe-Language.csproj"
set "STANDALONE=%~dp0published\win-x64\Dough.exe"
set "RELEASE_DLL=%~dp0Other_Bullshit\bin\Release\net10.0-windows\Dough.dll"
set "DEBUG_DLL=%~dp0Other_Bullshit\bin\Debug\net10.0-windows\Dough.dll"
set "LEGACY_RELEASE_DLL=%~dp0Other_Bullshit\bin\Release\net10.0\Dough.dll"
set "LEGACY_DEBUG_DLL=%~dp0Other_Bullshit\bin\Debug\net10.0\Dough.dll"
set "SELECTED_DLL="

if exist "%STANDALONE%" (
  "%STANDALONE%" %*
  exit /b %errorlevel%
)

if not exist "%RELEASE_DLL%" if not exist "%DEBUG_DLL%" if not exist "%LEGACY_RELEASE_DLL%" if not exist "%LEGACY_DEBUG_DLL%" (
  dotnet build "%PROJECT%" -c Release
  if errorlevel 1 exit /b %errorlevel%
)

if exist "%RELEASE_DLL%" if exist "%DEBUG_DLL%" (
  powershell -NoProfile -ExecutionPolicy Bypass -Command ^
    "$release = Get-Item '%RELEASE_DLL%'; $debug = Get-Item '%DEBUG_DLL%'; if ($debug.LastWriteTimeUtc -gt $release.LastWriteTimeUtc) { exit 10 } else { exit 11 }"
  if errorlevel 11 set "SELECTED_DLL=%RELEASE_DLL%"
  if errorlevel 10 set "SELECTED_DLL=%DEBUG_DLL%"
)

if not defined SELECTED_DLL if exist "%RELEASE_DLL%" set "SELECTED_DLL=%RELEASE_DLL%"
if not defined SELECTED_DLL if exist "%DEBUG_DLL%" set "SELECTED_DLL=%DEBUG_DLL%"
if not defined SELECTED_DLL if exist "%LEGACY_RELEASE_DLL%" set "SELECTED_DLL=%LEGACY_RELEASE_DLL%"
if not defined SELECTED_DLL if exist "%LEGACY_DEBUG_DLL%" set "SELECTED_DLL=%LEGACY_DEBUG_DLL%"

dotnet "%SELECTED_DLL%" %*
exit /b %errorlevel%
