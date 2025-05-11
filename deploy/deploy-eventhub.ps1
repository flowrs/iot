# Read parameters from file
$parameters = Get-Content -Path "../infrastructure/eventhub.parameters.json" | ConvertFrom-Json

# Get values from parameters
$resourceGroup = $parameters.parameters.resourceGroupName.value
$location = $parameters.parameters.location.value
$eventHubNamespaceName = $parameters.parameters.eventHubNamespaceName.value
$eventHubName = $parameters.parameters.eventHubName.value
$iotHubName = $parameters.parameters.iotHubName.value

# Create resource group if it doesn't exist
Write-Host "`nCreating/verifying resource group..."
az group create --name $resourceGroup --location $location

# Deploy Bicep template
Write-Host "`nDeploying Event Hub..."
az deployment group create `
    --resource-group $resourceGroup `
    --template-file "../infrastructure/eventhub.bicep" `
    --parameters "../infrastructure/eventhub.parameters.json"

# Get the Event Hub connection string
Write-Host "`nGetting Event Hub connection string..."
$eventHubConnectionString = az eventhubs namespace authorization-rule keys list `
    --resource-group $resourceGroup `
    --namespace-name $eventHubNamespaceName `
    --name "RootManageSharedAccessKey" `
    --query "primaryConnectionString" `
    --output tsv

# Get the IoT Hub connection string
$iotHubConnectionString = az iot hub connection-string show `
    --resource-group $resourceGroup `
    --hub-name $iotHubName `
    --query "connectionString" `
    --output tsv

Write-Host "`nEvent Hub setup complete!"
Write-Host "Resource Group: $resourceGroup"
Write-Host "Event Hub Namespace: $eventHubNamespaceName"
Write-Host "Event Hub Name: $eventHubName"
Write-Host "IoT Hub Name: $iotHubName"
Write-Host "Event Hub Connection String: $eventHubConnectionString"
Write-Host "IoT Hub Connection String: $iotHubConnectionString"

# Store connection strings in user secrets
dotnet user-secrets init --project ../DeviceSimulator
dotnet user-secrets set "EventHub:ConnectionString" $eventHubConnectionString --project ../DeviceSimulator
dotnet user-secrets set "EventHub:Name" $eventHubName --project ../DeviceSimulator
dotnet user-secrets set "IoTHub:ConnectionString" $iotHubConnectionString --project ../DeviceSimulator 