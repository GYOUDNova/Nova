@echo off
for /f "delims=" %%f in ('dir /b /a-d *.csproj') do (
    echo Verifying format for project: %%f
    dotnet format "%%f" --verify-no-changes --exclude "**/Mediapipe/**"
    echo Extra: Attempting to format the project
    dotnet format -v diag "%%f" --exclude "**/Mediapipe/**" 
)

echo Review the log messages above for any errors.
exit /b 0