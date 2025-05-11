@description('The name of the resource group where resources will be deployed')
param resourceGroupName string

@description('The name of the Event Hub namespace (must be globally unique)')
param eventHubNamespaceName string

@description('The name of the Event Hub instance')
param eventHubName string

@description('The name of the new S1 IoT Hub')
param iotHubName string

@description('The number of partitions for the Event Hub (default: 2)')
param partitionCount int = 2

@description('The message retention period in days (default: 1)')
param messageRetentionInDays int = 1

@description('The Azure region where resources will be deployed (default: same as resource group)')
param location string = resourceGroup().location

@description('The SKU of the Event Hub namespace (default: Basic)')
@allowed([
  'Basic'
  'Standard'
  'Premium'
])
param skuName string = 'Basic'

@description('The capacity of the Event Hub namespace (default: 1)')
param skuCapacity int = 1

// Create Event Hub namespace first
resource eventHubNamespace 'Microsoft.EventHub/namespaces@2022-10-01-preview' = {
  name: eventHubNamespaceName
  location: location
  sku: {
    name: skuName
    tier: skuName
    capacity: skuCapacity
  }
  properties: {
    isAutoInflateEnabled: false
    maximumThroughputUnits: 0
  }
}

// Create Event Hub
resource eventHub 'Microsoft.EventHub/namespaces/eventhubs@2022-10-01-preview' = {
  parent: eventHubNamespace
  name: eventHubName
  properties: {
    partitionCount: partitionCount
    messageRetentionInDays: messageRetentionInDays
  }
}

// Create authorization rule for Event Hub
resource authRule 'Microsoft.EventHub/namespaces/authorizationRules@2022-10-01-preview' = {
  name: 'RootManageSharedAccessKey'
  parent: eventHubNamespace
  properties: {
    rights: [ 'Listen', 'Send', 'Manage' ]
  }
}

// Get the Event Hub connection string
var eventHubConnectionString = listKeys(authRule.id, authRule.apiVersion).primaryConnectionString

// Create IoT Hub with routing configuration
resource iotHub 'Microsoft.Devices/IotHubs@2021-07-02' = {
  name: iotHubName
  location: location
  sku: {
    name: 'S1'
    capacity: 1
  }
  properties: {
    routing: {
      endpoints: {
        eventHubs: [
          {
            name: 'eventhub'
            connectionString: '${eventHubConnectionString};EntityPath=${eventHubName}'
            subscriptionId: subscription().subscriptionId
            resourceGroup: resourceGroupName
          }
        ]
      }
      routes: [
        {
          name: 'eventhub-route'
          source: 'DeviceMessages'
          condition: 'true'
          endpointNames: [ 'eventhub' ]
          isEnabled: true
        }
      ]
    }
  }
  dependsOn: [
    eventHub
    authRule
  ]
}

output eventHubConnectionString string = eventHubConnectionString
output iotHubConnectionString string = iotHub.properties.hostName 
