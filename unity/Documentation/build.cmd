:: Borrowed from https://github.com/open-telemetry/opentelemetry-dotnet
SETLOCAL
SETLOCAL ENABLEEXTENSIONS

docfx metadata
docfx build docfx.json > docfx.log
@IF NOT %ERRORLEVEL% == 0 (
  type docfx.log
  ECHO Error: docfx build failed. 1>&2
  EXIT /B %ERRORLEVEL%
)
@type docfx.log
@type docfx.log | findstr /C:"Build succeeded."
@IF NOT %ERRORLEVEL% == 0 (
  ECHO Error: There are build warnings. 1>&2
  EXIT /B %ERRORLEVEL%
)