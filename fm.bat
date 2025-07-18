@echo off
for /f %%a in ('echo prompt $E ^| cmd') do set "ESC=%%a"

set "YELLOW=%ESC%[33m"
set "CYAN=%ESC%[36m"
set "GREEN=%ESC%[32m"
set "MAGENTA=%ESC%[35m"
set "RESET=%ESC%[0m"

for /f "delims=" %%f in ('dir /b /a-d *.csproj') do (
    echo.
    echo %YELLOW%VERIFY PROJECT %%f ======================================================================%RESET%
    dotnet format "%%f" --verify-no-changes --exclude "**/Mediapipe/**"

    echo.
    echo %CYAN%FORMAT PROJECT %%f ======================================================================%RESET%
    dotnet format -v diag "%%f" --exclude "**/Mediapipe/**"

    echo.
    echo %GREEN%DONE PROJECT %%f ======================================================================%RESET%
    echo.
)

echo.
echo %MAGENTA%REVIEW PROJECTS ======================================================================%RESET%
echo Please review the changes and commit them if they are acceptable.
exit /b 0