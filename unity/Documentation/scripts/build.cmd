:: Borrowed from https://github.com/open-telemetry/opentelemetry-dotnet
SETLOCAL
SETLOCAL ENABLEEXTENSIONS

cd ../
:: make sure everything is able to compile when building
dotnet restore unity.sln
cd Documentation

rmdir /s /q api
rmdir /s /q clientHTMLOutput



docfx metadata
docfx build docfx.json > docfx.log
@IF NOT %ERRORLEVEL% == 0 (
  type docfx.log
  ECHO Error: docfx build failed. 1>&2
  @REM EXIT /B %ERRORLEVEL%
)
@type docfx.log
@type docfx.log | findstr /C:"Build succeeded."
@IF NOT %ERRORLEVEL% == 0 (
  ECHO There are build warnings. 1>&2
  @REM EXIT /B %ERRORLEVEL%
)