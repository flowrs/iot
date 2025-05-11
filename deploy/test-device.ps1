param(
    [switch]$Help,
    [string]$Action = "monitor"  # Options: monitor, trigger-alert, maintenance, set-interval, configure, command, firmware
)

function Show-Help {
    Write-Host "T-Mobile IoT Security Device Test Script"
    Write-Host "Usage: .\test-device.ps1 [options]"
    Write-Host ""
    Write-Host "Options:"
    Write-Host "  -Action <action>    Action to perform:"
    Write-Host "    monitor           Monitor device events"
    Write-Host "    trigger-alert     Trigger a custom alert"
    Write-Host "    maintenance       Perform device maintenance"
    Write-Host "    set-interval      Change telemetry interval"
    Write-Host "    configure         Update device configuration"
    Write-Host "    command           Send device command"
    Write-Host "    firmware          Send firmware update"
    Write-Host "  -Help               Show this help message"
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

# Get IoT Hub name and device ID from user secrets
$iotHubName = dotnet user-secrets list --project DeviceSimulator | 
    Where-Object { $_ -match "IotHub:ConnectionString" } | 
    ForEach-Object { 
        if ($_ -match "HostName=([^;]+)") { 
            $matches[1] 
        }
    }

$deviceId = dotnet user-secrets list --project DeviceSimulator | 
    Where-Object { $_ -match "IotHub:DeviceId" } | 
    ForEach-Object { $_.Split("=")[1] }

if (-not $iotHubName -or -not $deviceId) {
    Write-Error "IoT Hub connection string or device ID not found in user secrets. Please run setup-azure.ps1 first."
    exit 1
}

Write-Host "IoT Hub: $iotHubName"
Write-Host "Device ID: $deviceId"
Write-Host "Action: $Action"
Write-Host ""

try {
    switch ($Action.ToLower()) {
        "monitor" {
            Write-Host "Monitoring device events..."
            az iot hub monitor-events --hub-name $iotHubName --device-id $deviceId --properties all
        }
        "trigger-alert" {
            $message = Read-Host "Enter alert message"
            Write-Host "Triggering alert..."
            az iot hub invoke-device-method --hub-name $iotHubName --device-id $deviceId --method-name "TriggerAlert" --method-payload "{\"message\":\"$message\"}"
        }
        "maintenance" {
            Write-Host "Performing maintenance..."
            az iot hub invoke-device-method --hub-name $iotHubName --device-id $deviceId --method-name "PerformMaintenance"
        }
        "set-interval" {
            $interval = Read-Host "Enter new telemetry interval in seconds"
            Write-Host "Setting telemetry interval..."
            az iot hub invoke-device-method --hub-name $iotHubName --device-id $deviceId --method-name "SetTelemetryInterval" --method-payload "{\"interval\":$interval}"
        }
        "configure" {
            Write-Host "`nDevice Configuration Options:"
            Write-Host "1. Update motion sensitivity"
            Write-Host "2. Update alert thresholds"
            Write-Host "3. Update reporting interval"
            $option = Read-Host "Select option (1-3)"

            $config = @{}
            switch ($option) {
                "1" {
                    $sensitivity = Read-Host "Enter motion sensitivity (1-10)"
                    $config = @{
                        type = "configuration"
                        data = @{
                            motionSensitivity = [int]$sensitivity
                        }
                    }
                }
                "2" {
                    Write-Host "`nAlert Thresholds:"
                    $tempHigh = Read-Host "Enter high temperature threshold"
                    $tempLow = Read-Host "Enter low temperature threshold"
                    $humidityHigh = Read-Host "Enter high humidity threshold"
                    $humidityLow = Read-Host "Enter low humidity threshold"
                    $batteryLow = Read-Host "Enter low battery threshold"

                    $config = @{
                        type = "configuration"
                        data = @{
                            alertThresholds = @{
                                temperatureHigh = [double]$tempHigh
                                temperatureLow = [double]$tempLow
                                humidityHigh = [double]$humidityHigh
                                humidityLow = [double]$humidityLow
                                batteryLow = [double]$batteryLow
                            }
                        }
                    }
                }
                "3" {
                    $interval = Read-Host "Enter reporting interval in seconds"
                    $config = @{
                        type = "configuration"
                        data = @{
                            reportingInterval = [int]$interval
                        }
                    }
                }
                default {
                    Write-Error "Invalid option"
                    exit 1
                }
            }

            $payload = $config | ConvertTo-Json -Compress
            Write-Host "`nSending configuration update..."
            az iot device c2d-message send --hub-name $iotHubName --device-id $deviceId --data $payload
        }
        "command" {
            Write-Host "`nAvailable Commands:"
            Write-Host "1. Reboot device"
            Write-Host "2. Reset to factory settings"
            Write-Host "3. Run self-test"
            $option = Read-Host "Select command (1-3)"

            $command = switch ($option) {
                "1" { "reboot" }
                "2" { "reset" }
                "3" { "test" }
                default {
                    Write-Error "Invalid command"
                    exit 1
                }
            }

            $payload = @{
                type = "command"
                data = @{
                    action = $command
                }
            } | ConvertTo-Json -Compress

            Write-Host "`nSending command..."
            az iot device c2d-message send --hub-name $iotHubName --device-id $deviceId --data $payload
        }
        "firmware" {
            $version = Read-Host "Enter firmware version"
            $description = Read-Host "Enter firmware description"
            $url = Read-Host "Enter firmware download URL"

            $payload = @{
                type = "firmware"
                data = @{
                    version = $version
                    description = $description
                    downloadUrl = $url
                }
            } | ConvertTo-Json -Compress

            Write-Host "`nSending firmware update..."
            az iot device c2d-message send --hub-name $iotHubName --device-id $deviceId --data $payload
        }
        default {
            Write-Error "Invalid action. Use -Help to see available options."
            exit 1
        }
    }
}
catch {
    Write-Host "`nOperation failed: $_"
} 