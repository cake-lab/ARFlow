:: Borrowed from https://github.com/open-telemetry/opentelemetry-dotnet
SETLOCAL
SETLOCAL ENABLEEXTENSIONS

rmdir /s /q api
cd ../../website/docs
rmdir /s /q client
cd ../../unity/Documentation

del index.md

cd ../
type "Packages/edu.wpi.cake.arflow/README.md" > "Documentation/index.md"
cd Documentation

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