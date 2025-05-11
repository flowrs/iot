# Check if Azure CLI is installed
try {
    $azVersion = az --version
    if (-not $azVersion) {
        throw "Azure CLI not found"
    }
} catch {
    Write-Error "Azure CLI is not installed. Please install it from https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
    exit 1
}

# Check if logged in to Azure
$account = az account show 2>$null
if (-not $account) {
    Write-Host "Please log in to Azure..."
    az login
}

# Get the script directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Join-Path -Path $scriptPath -ChildPath ".."
$parametersPath = Join-Path -Path $projectDir -ChildPath "infrastructure\eventhub.parameters.json"

# Read parameters from file
$parameters = Get-Content -Path $parametersPath | ConvertFrom-Json

# Get values from parameters
$resourceGroup = $parameters.parameters.resourceGroupName.value
$location = $parameters.parameters.location.value
$iotHubName = $parameters.parameters.iotHubName.value
$deviceId = "simulated-device-1"

# Create resource group if it doesn't exist
Write-Host "`nCreating/verifying resource group..."
az group create --name $resourceGroup --location $location

# Create IoT Hub if it doesn't exist
Write-Host "`nCreating/verifying IoT Hub..."
$existingHub = az iot hub show --name $iotHubName --resource-group $resourceGroup 2>$null
if ($existingHub) {
    Write-Host "IoT Hub already exists."
} else {
    Write-Host "Creating new IoT Hub with F1 (Free) tier..."
    az iot hub create --name $iotHubName --resource-group $resourceGroup --sku F1 --partition-count 2
}

# Create device in IoT Hub
Write-Host "`nCreating device in IoT Hub..."
az iot hub device-identity create `
    --hub-name $iotHubName `
    --device-id $deviceId `
    --resource-group $resourceGroup

# Get device connection string
Write-Host "`nGetting device connection string..."
$deviceConnectionString = az iot hub device-identity connection-string show `
    --hub-name $iotHubName `
    --device-id $deviceId `
    --resource-group $resourceGroup `
    --query "connectionString" `
    --output tsv

# Store connection strings in user secrets
Write-Host "`nStoring connection strings in user secrets..."
$projectPath = Join-Path -Path $projectDir -ChildPath "DeviceSimulator\DeviceSimulator.csproj"
dotnet user-secrets init --project $projectPath
dotnet user-secrets set "DeviceId" $deviceId --project $projectPath
dotnet user-secrets set "IoTHub:ConnectionString" $deviceConnectionString --project $projectPath

Write-Host "`nSetup complete!"
Write-Host "Device ID: $deviceId"
Write-Host "Connection string has been stored in user secrets"
Write-Host "Resource Group: $resourceGroup"
Write-Host "IoT Hub: $iotHubName"
Write-Host "`nYou can now run the device simulator using:"
Write-Host ".\run.ps1" 