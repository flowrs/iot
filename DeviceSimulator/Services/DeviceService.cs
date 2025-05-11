using DeviceSimulator.Models;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using System.Text;
using System.Text.Json;

namespace DeviceSimulator.Services;

public class DeviceService
{
    private readonly DeviceClient _deviceClient;
    private readonly SecurityDevice _device;
    private readonly Action<string> _logAction;
    private readonly Random _random = new();
    private bool _isRunning;
    private int _telemetryInterval = 5000; // 5 seconds

    public DeviceService(DeviceClient deviceClient, SecurityDevice device, Action<string> logAction)
    {
        _deviceClient = deviceClient;
        _device = device;
        _logAction = logAction;
    }

    public async Task StartAsync()
    {
        _isRunning = true;
        _logAction("Device service started");

        // Set up handlers
        await _deviceClient.SetMethodHandlerAsync("SetTelemetryInterval", HandleSetTelemetryInterval, null);
        await _deviceClient.SetMethodHandlerAsync("TriggerAlert", HandleTriggerAlert, null);
        await _deviceClient.SetMethodHandlerAsync("PerformMaintenance", HandleMaintenance, null);
        await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(HandleDesiredPropertyUpdate, null);

        while (_isRunning)
        {
            try
            {
                await SendTelemetryAsync();
                await Task.Delay(TimeSpan.FromSeconds(_device.ReportingInterval));
            }
            catch (Exception ex)
            {
                _logAction($"Error sending telemetry: {ex.Message}");
                await Task.Delay(5000); // Wait before retrying
            }
        }
    }

    private async Task SendTelemetryAsync()
    {
        // Update device state
        UpdateDeviceState();

        // Create telemetry message
        var telemetry = new
        {
            timestamp = DateTime.UtcNow,
            deviceId = _device.DeviceId,
            status = _device.Status.ToString(),
            batteryLevel = _device.BatteryLevel,
            signalStrength = _device.SignalStrength,
            temperature = _device.Temperature,
            humidity = _device.Humidity,
            motionDetected = _device.MotionDetected,
            doorOpen = _device.DoorOpen,
            cellularData = _device.CellularData,
            firmwareVersion = _device.FirmwareVersion
        };

        var messageString = JsonSerializer.Serialize(telemetry);
        var messageBytes = Encoding.UTF8.GetBytes(messageString);

        // Send to IoT Hub (will be routed to Event Hub)
        var message = new Message(messageBytes)
        {
            ContentType = "application/json",
            ContentEncoding = "utf-8"
        };
        await _deviceClient.SendEventAsync(message);
        _logAction("Telemetry sent to IoT Hub (will be routed to Event Hub)");
    }

    private void UpdateDeviceState()
    {
        // Simulate battery drain
        _device.BatteryLevel = Math.Max(0, _device.BatteryLevel - 0.1);

        // Simulate signal strength changes
        _device.SignalStrength = _random.Next(1, 6);

        // Simulate temperature and humidity changes
        _device.Temperature = 20 + _random.NextDouble() * 10;
        _device.Humidity = 40 + _random.NextDouble() * 30;

        // Simulate motion detection
        if (_random.NextDouble() < 0.1)
        {
            _device.MotionDetected = true;
            _logAction("Motion detected!");
        }
        else
        {
            _device.MotionDetected = false;
        }

        // Simulate door state
        if (_random.NextDouble() < 0.05)
        {
            _device.DoorOpen = !_device.DoorOpen;
            _logAction($"Door {( _device.DoorOpen ? "opened" : "closed" )}");
        }

        // Update cellular data
        _device.CellularData.DataUsageMB += _random.NextDouble() * 0.1;
        _device.CellularData.LastSync = DateTime.UtcNow;

        // Check alert conditions
        CheckAlertConditions();
    }

    private void CheckAlertConditions()
    {
        var alerts = new List<string>();

        // Check temperature thresholds
        if (_device.Temperature > _device.AlertThresholds["temperatureHigh"])
        {
            alerts.Add($"High temperature alert: {_device.Temperature:F1}°C");
        }
        else if (_device.Temperature < _device.AlertThresholds["temperatureLow"])
        {
            alerts.Add($"Low temperature alert: {_device.Temperature:F1}°C");
        }

        // Check humidity thresholds
        if (_device.Humidity > _device.AlertThresholds["humidityHigh"])
        {
            alerts.Add($"High humidity alert: {_device.Humidity:F1}%");
        }
        else if (_device.Humidity < _device.AlertThresholds["humidityLow"])
        {
            alerts.Add($"Low humidity alert: {_device.Humidity:F1}%");
        }

        // Check battery level
        if (_device.BatteryLevel < _device.AlertThresholds["batteryLow"])
        {
            alerts.Add($"Low battery alert: {_device.BatteryLevel:F1}%");
        }

        // Send alerts if any
        if (alerts.Any())
        {
            _device.Status = DeviceStatus.Alert;
            SendAlertsAsync(alerts).Wait();
        }
        else
        {
            _device.Status = DeviceStatus.Online;
        }
    }

    private async Task SendAlertsAsync(List<string> alerts)
    {
        var alertData = new
        {
            timestamp = DateTime.UtcNow,
            deviceId = _device.DeviceId,
            type = "alert",
            alerts = alerts
        };

        var messageString = JsonSerializer.Serialize(alertData);
        var message = new Message(Encoding.UTF8.GetBytes(messageString))
        {
            ContentType = "application/json",
            ContentEncoding = "utf-8"
        };

        await _deviceClient.SendEventAsync(message);
        _logAction($"Alerts sent: {string.Join(", ", alerts)}");
    }

    private async Task<MethodResponse> HandleSetTelemetryInterval(MethodRequest methodRequest, object? userContext)
    {
        Console.WriteLine($"Received method call: {methodRequest.Name}");
        var payload = JsonSerializer.Deserialize<Dictionary<string, int>>(methodRequest.DataAsJson);
        
        if (payload != null && payload.TryGetValue("interval", out int interval))
        {
            _telemetryInterval = interval * 1000; // Convert to milliseconds
            Console.WriteLine($"Setting telemetry interval to {interval} seconds");
            return new MethodResponse(200);
        }

        return new MethodResponse(400);
    }

    private async Task<MethodResponse> HandleTriggerAlert(MethodRequest methodRequest, object? userContext)
    {
        Console.WriteLine($"Received method call: {methodRequest.Name}");
        var payload = JsonSerializer.Deserialize<Dictionary<string, string>>(methodRequest.DataAsJson);
        
        if (payload != null && payload.TryGetValue("message", out string? message))
        {
            _device.Status = DeviceStatus.Alert;
            await SendAlertsAsync(new List<string> { message });
            return new MethodResponse(200);
        }

        return new MethodResponse(400);
    }

    private async Task<MethodResponse> HandleMaintenance(MethodRequest methodRequest, object? userContext)
    {
        Console.WriteLine($"Received method call: {methodRequest.Name}");
        _device.Status = DeviceStatus.Maintenance;
        _device.LastMaintenance = DateTime.UtcNow;
        _device.BatteryLevel = 100.0; // Simulate battery replacement

        var reportedProperties = new TwinCollection
        {
            ["status"] = _device.Status.ToString(),
            ["lastMaintenance"] = _device.LastMaintenance,
            ["batteryLevel"] = _device.BatteryLevel
        };

        await _deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
        return new MethodResponse(200);
    }

    private async Task HandleDesiredPropertyUpdate(TwinCollection desiredProperties, object userContext)
    {
        _logAction("Desired property update received");

        foreach (KeyValuePair<string, object> prop in desiredProperties)
        {
            var key = prop.Key;
            var value = prop.Value;

            switch (key)
            {
                case "reportingInterval":
                    if (value is int interval)
                    {
                        _device.ReportingInterval = interval;
                        _logAction($"Reporting interval updated to {interval} seconds");
                    }
                    break;

                case "motionSensitivity":
                    if (value is int sensitivity)
                    {
                        _device.MotionSensitivity = sensitivity;
                        _logAction($"Motion sensitivity updated to {sensitivity}");
                    }
                    break;

                case "alertThresholds":
                    if (value is JsonElement thresholds)
                    {
                        var newThresholds = new Dictionary<string, double>();
                        foreach (var threshold in thresholds.EnumerateObject())
                        {
                            if (threshold.Value.ValueKind == JsonValueKind.Number)
                            {
                                newThresholds[threshold.Name] = threshold.Value.GetDouble();
                            }
                        }
                        _device.AlertThresholds = newThresholds;
                        _logAction("Alert thresholds updated");
                    }
                    break;
            }
        }

        // Report back the current state
        var reportedProperties = new TwinCollection
        {
            ["reportingInterval"] = _device.ReportingInterval,
            ["motionSensitivity"] = _device.MotionSensitivity,
            ["alertThresholds"] = _device.AlertThresholds
        };

        await _deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
    }

    public void Stop()
    {
        _isRunning = false;
        _logAction("Device service stopped");
    }
} 