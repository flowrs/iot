# T-Mobile IoT Security Device Simulator

This sample application demonstrates Azure IoT capabilities using a simulated T-Mobile IoT security device. The device simulates a smart home security system with cellular connectivity, demonstrating real-world IoT scenarios.

## Features

- Cellular connectivity simulation (T-Mobile network)
- Real-time telemetry data:
  - Temperature and humidity monitoring
  - Motion detection
  - Door status
  - Battery level
  - Signal strength
  - Data usage tracking
- Device management:
  - Remote maintenance
  - Alert triggering
  - Telemetry interval control
  - Device twin properties
- Security alerts:
  - Motion detection
  - Door opening
  - Low battery warnings
  - Custom alerts

## Prerequisites

- .NET 8.0 SDK or higher
- Azure CLI
- Azure subscription
- PowerShell 5.1 or higher

## Setup

1. Install Azure CLI if you haven't already:
   - Windows: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-windows
   - macOS: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-macos
   - Linux: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-linux

2. Run the Azure setup script to create and configure IoT Hub:
```powershell
.\setup-azure.ps1
```
The script will:
- Check for Azure CLI installation
- Log in to Azure if needed
- Create a resource group
- Create an IoT Hub
- Create a device identity
- Store the connection string securely

3. Run the device simulator:
```powershell
.\run.ps1
```

4. Test device capabilities (in a separate terminal):
```powershell
# Monitor device events
.\test-device.ps1 -Action monitor

# Trigger a custom alert
.\test-device.ps1 -Action trigger-alert

# Perform maintenance
.\test-device.ps1 -Action maintenance

# Change telemetry interval
.\test-device.ps1 -Action set-interval
```

## Device Capabilities

### Telemetry Data
- Temperature and humidity readings
- Motion detection status
- Door open/closed status
- Battery level
- Cellular signal strength
- Data usage tracking

### Device Methods
- `SetTelemetryInterval`: Change the telemetry reporting frequency
- `TriggerAlert`: Send a custom alert message
- `PerformMaintenance`: Simulate device maintenance and battery replacement

### Device Twin Properties
- Device status (Online, Offline, Maintenance, Alert)
- Last maintenance date
- Battery level
- Custom properties

## Security Note

Never commit your user secrets or share your connection strings publicly. 