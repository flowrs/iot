# Read parameters from file
$parameters = Get-Content -Path "../infrastructure/eventhub.parameters.json" | ConvertFrom-Json

# Get values from parameters
$resourceGroup = $parameters.parameters.resourceGroupName.value
$eventHubNamespaceName = $parameters.parameters.eventHubNamespaceName.value
$eventHubName = $parameters.parameters.eventHubName.value

# Get the Event Hub connection string
$eventHubConnectionString = az eventhubs namespace authorization-rule keys list `
    --resource-group $resourceGroup `
    --namespace-name $eventHubNamespaceName `
    --name "RootManageSharedAccessKey" `
    --query "primaryConnectionString" `
    --output tsv

Write-Host "`nStarting Event Hub message monitoring..."
Write-Host "Press Ctrl+C to stop monitoring"
Write-Host "`nWaiting for messages..."

# Monitor Event Hub messages
az eventhubs eventhub consumer-group create `
    --resource-group $resourceGroup `
    --namespace-name $eventHubNamespaceName `
    --eventhub-name $eventHubName `
    --name "monitor-group"

az eventhubs eventhub consumer-group show `
    --resource-group $resourceGroup `
    --namespace-name $eventHubNamespaceName `
    --eventhub-name $eventHubName `
    --name "monitor-group"

# Start receiving messages
az eventhubs eventhub consumer-group receive `
    --resource-group $resourceGroup `
    --namespace-name $eventHubNamespaceName `
    --eventhub-name $eventHubName `
    --consumer-group "monitor-group" `
    --connection-string $eventHubConnectionString `
    --max-messages 100 `
    --timeout 60 