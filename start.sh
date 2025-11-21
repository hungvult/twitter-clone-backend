#!/bin/bash

echo "================================================"
echo "Twitter Clone Backend - Quick Start"
echo "================================================"
echo ""

# Check if .NET is installed
if ! command -v dotnet &> /dev/null
then
    echo "❌ .NET SDK not found. Please install .NET 8 SDK first."
    echo "Download: https://dotnet.microsoft.com/download/dotnet/8.0"
    exit 1
fi

echo "✅ .NET SDK found: $(dotnet --version)"
echo ""

# Check appsettings.json configuration
echo "📋 Checking configuration..."
if grep -q "YOUR_GOOGLE_CLIENT_ID" appsettings.json; then
    echo "⚠️  WARNING: appsettings.json contains placeholder values!"
    echo ""
    echo "Please update the following in appsettings.json:"
    echo "  1. Authentication:Google:ClientId"
    echo "  2. Authentication:Google:ClientSecret"
    echo "  3. Authentication:Jwt:SecretKey"
    echo "  4. ConnectionStrings:DefaultConnection (if needed)"
    echo ""
    read -p "Do you want to continue anyway? (y/n) " -n 1 -r
    echo ""
    if [[ ! $REPLY =~ ^[Yy]$ ]]
    then
        exit 1
    fi
fi

# Run migrations
echo ""
echo "🗄️  Running database migrations..."
if dotnet ef database update; then
    echo "✅ Database migrations completed successfully"
else
    echo "❌ Database migration failed"
    echo "Please ensure SQL Server is running and connection string is correct"
    exit 1
fi

# Create uploads directory
echo ""
echo "📁 Creating uploads directory..."
mkdir -p uploads/images
echo "✅ Uploads directory created"

# Build project
echo ""
echo "🔨 Building project..."
if dotnet build; then
    echo "✅ Build successful"
else
    echo "❌ Build failed"
    exit 1
fi

# Run project
echo ""
echo "================================================"
echo "🚀 Starting Twitter Clone Backend..."
echo "================================================"
echo ""
echo "API will be available at:"
echo "  - HTTPS: https://localhost:7xxx"
echo "  - HTTP:  http://localhost:5xxx"
echo "  - Swagger UI: https://localhost:7xxx/swagger"
echo ""
echo "Press Ctrl+C to stop"
echo ""

dotnet run
