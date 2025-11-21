@echo off
echo ================================================
echo Twitter Clone Backend - Quick Start
echo ================================================
echo.

REM Check if .NET is installed
where dotnet >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo X .NET SDK not found. Please install .NET 8 SDK first.
    echo Download: https://dotnet.microsoft.com/download/dotnet/8.0
    exit /b 1
)

echo [OK] .NET SDK found
dotnet --version
echo.

REM Check appsettings.json configuration
echo [*] Checking configuration...
findstr /C:"YOUR_GOOGLE_CLIENT_ID" appsettings.json >nul
if %ERRORLEVEL% EQU 0 (
    echo [!] WARNING: appsettings.json contains placeholder values!
    echo.
    echo Please update the following in appsettings.json:
    echo   1. Authentication:Google:ClientId
    echo   2. Authentication:Google:ClientSecret
    echo   3. Authentication:Jwt:SecretKey
    echo   4. ConnectionStrings:DefaultConnection ^(if needed^)
    echo.
    set /p continue="Do you want to continue anyway? (y/n): "
    if /i not "%continue%"=="y" exit /b 1
)

REM Run migrations
echo.
echo [*] Running database migrations...
dotnet ef database update
if %ERRORLEVEL% NEQ 0 (
    echo [X] Database migration failed
    echo Please ensure SQL Server is running and connection string is correct
    exit /b 1
)
echo [OK] Database migrations completed successfully

REM Create uploads directory
echo.
echo [*] Creating uploads directory...
if not exist "uploads\images" mkdir uploads\images
echo [OK] Uploads directory created

REM Build project
echo.
echo [*] Building project...
dotnet build
if %ERRORLEVEL% NEQ 0 (
    echo [X] Build failed
    exit /b 1
)
echo [OK] Build successful

REM Run project
echo.
echo ================================================
echo [*] Starting Twitter Clone Backend...
echo ================================================
echo.
echo API will be available at:
echo   - HTTPS: https://localhost:7xxx
echo   - HTTP:  http://localhost:5xxx
echo   - Swagger UI: https://localhost:7xxx/swagger
echo.
echo Press Ctrl+C to stop
echo.

dotnet run
