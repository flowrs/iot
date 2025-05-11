# Check if .NET 8 SDK is installed
$dotnetVersion = dotnet --version
if (-not $dotnetVersion.StartsWith("8.")) {
    Write-Error ".NET 8 SDK is required. Please install it from https://dotnet.microsoft.com/download/dotnet/8.0"
    exit 1
}

# Create solution and project if they don't exist
if (-not (Test-Path "AzureIoTSample.sln")) {
    Write-Host "Creating solution..."
    dotnet new sln -n AzureIoTSample
}

if (-not (Test-Path "DeviceSimulator")) {
    Write-Host "Creating project..."
    dotnet new console -n DeviceSimulator
    dotnet sln add DeviceSimulator/DeviceSimulator.csproj
}

# Add required NuGet packages
Write-Host "Adding NuGet packages..."
dotnet add DeviceSimulator/DeviceSimulator.csproj package Microsoft.Azure.Devices.Client
dotnet add DeviceSimulator/DeviceSimulator.csproj package Microsoft.Extensions.Configuration.UserSecrets

# Initialize user secrets
Write-Host "Initializing user secrets..."
dotnet user-secrets init --project DeviceSimulator

# Prompt for IoT Hub connection string and device ID
$connectionString = Read-Host "Enter your IoT Hub device connection string"
$deviceId = Read-Host "Enter your device ID"

# Store secrets
dotnet user-secrets set "IotHub:ConnectionString" $connectionString --project DeviceSimulator
dotnet user-secrets set "IotHub:DeviceId" $deviceId --project DeviceSimulator

Write-Host "`nSetup complete! You can now run the device simulator using:"
Write-Host "dotnet run --project DeviceSimulator" 