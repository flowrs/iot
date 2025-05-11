param(
    [switch]$Help,
    [string]$EventType = "all"  # Options: all, telemetry, operations, connections
)

function Show-Help {
    Write-Host "Azure IoT Hub Monitor Script"
    Write-Host "Usage: .\monitor.ps1 [options]"
    Write-Host ""
    Write-Host "Options:"
    Write-Host "  -EventType <type>    Type of events to monitor (all, telemetry, operations, connections)"
    Write-Host "  -Help                Show this help message"
}

if ($Help) {
    Show-Help
    exit 0
}

# Check if Azure CLI is installed
$azVersion = az version --query '"azure-cli"' -o tsv
if (-not $azVersion) {
    Write-Error "Azure CLI is not installed. Please install it from https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
    exit 1
}

# Get IoT Hub name from user secrets
$iotHubName = dotnet user-secrets list --project DeviceSimulator | 
    Where-Object { $_ -match "IotHub:ConnectionString" } | 
    ForEach-Object { 
        if ($_ -match "HostName=([^;]+)") { 
            $matches[1] 
        }
    }

if (-not $iotHubName) {
    Write-Error "IoT Hub connection string not found in user secrets. Please run setup-azure.ps1 first."
    exit 1
}

Write-Host "Monitoring IoT Hub: $iotHubName"
Write-Host "Event Type: $EventType"
Write-Host "Press Ctrl+C to stop monitoring"
Write-Host ""

try {
    switch ($EventType.ToLower()) {
        "telemetry" {
            az iot hub monitor-events --hub-name $iotHubName --device-id (dotnet user-secrets list --project DeviceSimulator | Where-Object { $_ -match "IotHub:DeviceId" } | ForEach-Object { $_.Split("=")[1] })
        }
        "operations" {
            az iot hub monitor-events --hub-name $iotHubName --device-id (dotnet user-secrets list --project DeviceSimulator | Where-Object { $_ -match "IotHub:DeviceId" } | ForEach-Object { $_.Split("=")[1] }) --properties sys
        }
        "connections" {
            az iot hub monitor-events --hub-name $iotHubName --device-id (dotnet user-secrets list --project DeviceSimulator | Where-Object { $_ -match "IotHub:DeviceId" } | ForEach-Object { $_.Split("=")[1] }) --properties sys --query "properties.connectionDeviceId"
        }
        default {
            az iot hub monitor-events --hub-name $iotHubName --device-id (dotnet user-secrets list --project DeviceSimulator | Where-Object { $_ -match "IotHub:DeviceId" } | ForEach-Object { $_.Split("=")[1] }) --properties all
        }
    }
}
catch {
    Write-Host "`nMonitoring stopped."
} 