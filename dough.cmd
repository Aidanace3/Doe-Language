@echo off
setlocal
set "PROJECT=%~dp0Other_Bullshit\Doe-Language.csproj"
set "EXE=%~dp0Other_Bullshit\bin\Debug\net10.0\Dough.exe"

if not exist "%EXE%" (
  dotnet build "%PROJECT%"
  if errorlevel 1 exit /b %errorlevel%
)

"%EXE%" %*
exit /b %errorlevel%
