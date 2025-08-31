@echo off
setlocal enabledelayedexpansion

set BASE_DIR=%CD%

dotnet build src  1>nul

for /r tests %%i in (Test*.csproj) do (
    if EXIST "%%i" (

        set FULL_PATH=%%i
        set REL_PATH=!FULL_PATH:%BASE_DIR%\=!

        dotnet run --project "%%i"  1>nul

        if ERRORLEVEL 1 (
            echo.
            echo.
            echo ❌ FAILED: !REL_PATH!
            echo.
            echo dotnet run --project "!REL_PATH!"
            echo dotnet run --project "tests/Helper" "!REL_PATH!"
            echo.
            echo.
            goto :EOF
        ) else (
            echo ✓ !REL_PATH!
        )
    )
)
