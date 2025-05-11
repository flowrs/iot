using Microsoft.Azure.Devices.Client;
using System.Text;
using System.Text.Json;
using DeviceSimulator.Models;

namespace DeviceSimulator.Services;

public class CloudMessageHandler
{
    private readonly DeviceClient _deviceClient;
    private readonly SecurityDevice _device;
    private readonly Action<string> _logAction;

    public CloudMessageHandler(DeviceClient deviceClient, SecurityDevice device, Action<string> logAction)
    {
        _deviceClient = deviceClient;
        _device = device;
        _logAction = logAction;
    }

    public async Task InitializeAsync()
    {
        await _deviceClient.SetReceiveMessageHandlerAsync(HandleCloudMessageAsync, null);
        _logAction("Cloud message handler initialized");
    }

    private async Task HandleCloudMessageAsync(Message message, object? userContext)
    {
        try
        {
            var messageBytes = message.GetBytes();
            var messageString = Encoding.UTF8.GetString(messageBytes);
            var cloudMessage = JsonSerializer.Deserialize<CloudMessage>(messageString);

            if (cloudMessage == null)
            {
                _logAction("Received invalid message format");
                return;
            }

            _logAction($"Received cloud message: {cloudMessage.Type}");

            switch (cloudMessage.Type.ToLower())
            {
                case "configuration":
                    await HandleConfigurationMessageAsync(cloudMessage);
                    break;
                case "command":
                    await HandleCommandMessageAsync(cloudMessage);
                    break;
                case "firmware":
                    await HandleFirmwareMessageAsync(cloudMessage);
                    break;
                default:
                    _logAction($"Unknown message type: {cloudMessage.Type}");
                    break;
            }

            // Acknowledge message receipt
            await _deviceClient.CompleteAsync(message);
        }
        catch (Exception ex)
        {
            _logAction($"Error processing cloud message: {ex.Message}");
            await _deviceClient.RejectAsync(message);
        }
    }

    private async Task HandleConfigurationMessageAsync(CloudMessage message)
    {
        if (message.Data == null) return;

        var config = JsonSerializer.Deserialize<DeviceConfiguration>(message.Data.ToString()!);
        if (config == null) return;

        // Update device configuration
        if (config.MotionSensitivity.HasValue)
        {
            _device.MotionSensitivity = config.MotionSensitivity.Value;
            _logAction($"Updated motion sensitivity to {config.MotionSensitivity}");
        }

        if (config.AlertThresholds != null)
        {
            _device.AlertThresholds = config.AlertThresholds;
            _logAction("Updated alert thresholds");
        }

        if (config.ReportingInterval.HasValue)
        {
            _device.ReportingInterval = config.ReportingInterval.Value;
            _logAction($"Updated reporting interval to {config.ReportingInterval} seconds");
        }

        // Acknowledge configuration update
        await SendConfigurationAckAsync(config);
    }

    private async Task HandleCommandMessageAsync(CloudMessage message)
    {
        if (message.Data == null) return;

        var command = JsonSerializer.Deserialize<DeviceCommand>(message.Data.ToString()!);
        if (command == null) return;

        switch (command.Action.ToLower())
        {
            case "reboot":
                _logAction("Rebooting device...");
                // Simulate reboot
                await Task.Delay(2000);
                _logAction("Device rebooted");
                break;

            case "reset":
                _logAction("Resetting device to factory settings...");
                // Simulate factory reset
                _device.ResetToDefaults();
                _logAction("Device reset complete");
                break;

            case "test":
                _logAction("Running self-test...");
                // Simulate self-test
                await RunSelfTestAsync();
                break;

            default:
                _logAction($"Unknown command: {command.Action}");
                break;
        }
    }

    private async Task HandleFirmwareMessageAsync(CloudMessage message)
    {
        if (message.Data == null) return;

        var firmware = JsonSerializer.Deserialize<FirmwareUpdate>(message.Data.ToString()!);
        if (firmware == null) return;

        _logAction($"Starting firmware update to version {firmware.Version}...");
        
        // Simulate firmware update
        _device.Status = DeviceStatus.Maintenance;
        await Task.Delay(5000); // Simulate update time
        
        _device.FirmwareVersion = firmware.Version;
        _device.Status = DeviceStatus.Online;
        
        _logAction("Firmware update completed successfully");
        
        // Send update status
        await SendFirmwareUpdateStatusAsync(firmware.Version, true);
    }

    private async Task RunSelfTestAsync()
    {
        var testResults = new
        {
            timestamp = DateTime.UtcNow,
            deviceId = _device.DeviceId,
            tests = new TestResult[]
            {
                new() { name = "Battery", status = "Pass", value = _device.BatteryLevel },
                new() { name = "Signal", status = "Pass", value = _device.SignalStrength },
                new() { name = "Sensors", status = "Pass", value = "OK" },
                new() { name = "Cellular", status = "Pass", value = "Connected" }
            }
        };

        var messageString = JsonSerializer.Serialize(testResults);
        var message = new Message(Encoding.UTF8.GetBytes(messageString))
        {
            ContentType = "application/json",
            ContentEncoding = "utf-8"
        };

        await _deviceClient.SendEventAsync(message);
    }

    private async Task SendConfigurationAckAsync(DeviceConfiguration config)
    {
        var ack = new
        {
            timestamp = DateTime.UtcNow,
            deviceId = _device.DeviceId,
            type = "configuration-ack",
            config = config
        };

        var messageString = JsonSerializer.Serialize(ack);
        var message = new Message(Encoding.UTF8.GetBytes(messageString))
        {
            ContentType = "application/json",
            ContentEncoding = "utf-8"
        };

        await _deviceClient.SendEventAsync(message);
    }

    private async Task SendFirmwareUpdateStatusAsync(string version, bool success)
    {
        var status = new
        {
            timestamp = DateTime.UtcNow,
            deviceId = _device.DeviceId,
            type = "firmware-status",
            version = version,
            success = success
        };

        var messageString = JsonSerializer.Serialize(status);
        var message = new Message(Encoding.UTF8.GetBytes(messageString))
        {
            ContentType = "application/json",
            ContentEncoding = "utf-8"
        };

        await _deviceClient.SendEventAsync(message);
    }
}

public class CloudMessage
{
    public string Type { get; set; } = string.Empty;
    public object? Data { get; set; }
}

public class DeviceConfiguration
{
    public int? MotionSensitivity { get; set; }
    public Dictionary<string, double>? AlertThresholds { get; set; }
    public int? ReportingInterval { get; set; }
}

public class DeviceCommand
{
    public string Action { get; set; } = string.Empty;
    public Dictionary<string, object>? Parameters { get; set; }
}

public class FirmwareUpdate
{
    public string Version { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DownloadUrl { get; set; }
}

public class TestResult
{
    public string name { get; set; } = string.Empty;
    public string status { get; set; } = string.Empty;
    public object value { get; set; } = string.Empty;
} 