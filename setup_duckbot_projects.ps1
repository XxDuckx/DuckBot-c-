# DuckBot project generator (C# + JS)
# Creates .NET 8 projects and solution

Write-Host "Setting up DuckBot .NET solution..." -ForegroundColor Cyan

# Ensure dotnet SDK exists
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "Error: .NET SDK not found. Please install .NET 8 SDK first." -ForegroundColor Red
    exit 1
}

# Create solution
dotnet new sln -n DuckBot | Out-Null

# --- Projects ---

# GUI (WPF)
Write-Host "Creating GUI (WPF) project..."
dotnet new wpf -n DuckBot.GUI -f net8.0-windows
Add-Content "DuckBot.GUI\DuckBot.GUI.csproj" '<Project Sdk="Microsoft.NET.Sdk.WindowsDesktop"><PropertyGroup><OutputType>WinExe</OutputType><TargetFramework>net8.0-windows</TargetFramework><UseWPF>true</UseWPF></PropertyGroup></Project>' -Encoding UTF8

# Core
Write-Host "Creating Core project..."
dotnet new classlib -n DuckBot.Core -f net8.0
dotnet add DuckBot.Core package OpenCvSharp4.Windows
dotnet add DuckBot.Core package Tesseract

# Scripting
Write-Host "Creating Scripting project..."
dotnet new classlib -n DuckBot.Scripting -f net8.0
dotnet add DuckBot.Scripting package Jint

# Data
Write-Host "Creating Data project..."
dotnet new classlib -n DuckBot.Data -f net8.0
dotnet add DuckBot.Data package Microsoft.Data.Sqlite

# API
Write-Host "Creating API project..."
dotnet new classlib -n DuckBot.API -f net8.0

# --- Add projects to solution ---
dotnet sln add DuckBot.GUI/DuckBot.GUI.csproj
dotnet sln add DuckBot.Core/DuckBot.Core.csproj
dotnet sln add DuckBot.Scripting/DuckBot.Scripting.csproj
dotnet sln add DuckBot.Data/DuckBot.Data.csproj
dotnet sln add DuckBot.API/DuckBot.API.csproj

# --- Add references ---
dotnet add DuckBot.GUI reference DuckBot.Core
dotnet add DuckBot.GUI reference DuckBot.Scripting
dotnet add DuckBot.GUI reference DuckBot.Data
dotnet add DuckBot.GUI reference DuckBot.API

dotnet add DuckBot.Core reference DuckBot.Scripting
dotnet add DuckBot.Core reference DuckBot.Data

# --- Initial Build ---
Write-Host "Restoring and building solution..."
dotnet restore
dotnet build --no-incremental

Write-Host "`n✅ DuckBot solution setup complete!" -ForegroundColor Green
Write-Host "Open DuckBot.sln in Visual Studio to begin coding."
