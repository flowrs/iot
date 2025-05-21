using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DeviceSimulator.Models;
using DeviceSimulator;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using System.Text;
using System.Text.Json;
using System.Diagnostics;

namespace DeviceSimulator.Services;

public class DeviceService : BackgroundService
{
    private readonly ILogger<DeviceService> _logger;
    private readonly LoggingService _loggingService;
    private readonly MetricsService _metricsService;
    private readonly Random _random = new();
    private readonly Device _device;
    private readonly DeviceClient _deviceClient;
    private readonly SecurityDevice _securityDevice;
    private readonly Action<string> _logAction;
    private bool _isRunning;
    private int _telemetryInterval = 5000; // 5 seconds

    public DeviceService(
        ILogger<DeviceService> logger, 
        LoggingService loggingService,
        MetricsService metricsService,
        DeviceClient deviceClient, 
        SecurityDevice securityDevice)
    {
        _logger = logger;
        _loggingService = loggingService;
        _metricsService = metricsService;
        _deviceClient = deviceClient;
        _securityDevice = securityDevice;
        _logAction = message => logger.LogInformation("{Message}", message);
        _device = new Device
        {
            DeviceId = securityDevice.DeviceId,
            Location = "Building A",
            Status = "Online"
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _isRunning = true;
        _logAction("Device service started");

        try
        {
            // Set up handlers
            await _deviceClient.SetMethodHandlerAsync("SetTelemetryInterval", HandleSetTelemetryInterval, null);
            await _deviceClient.SetMethodHandlerAsync("TriggerAlert", HandleTriggerAlert, null);
            await _deviceClient.SetMethodHandlerAsync("PerformMaintenance", HandleMaintenance, null);
            await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(HandleDesiredPropertyUpdate, null);

            while (_isRunning && !stoppingToken.IsCancellationRequested)
            {
                using var activity = Telemetry.Source.StartActivity("GenerateTelemetry");
                activity?.SetTag("device.id", _device.DeviceId);
                activity?.SetTag("device.location", _device.Location);

                try
                {
                    await SendTelemetryAsync();
                    await Task.Delay(TimeSpan.FromSeconds(_device.ReportingInterval), stoppingToken);
                }
                catch (Exception ex)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    _logger.LogError(ex, "Error occurred while sending telemetry for device {DeviceId}", _device.DeviceId);
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in device service");
            throw;
        }
        finally
        {
            _isRunning = false;
            _logAction("Device service stopped");
        }
    }

    private async Task SendTelemetryAsync()
    {
        // Update device state
        UpdateDeviceState();

        // Create telemetry message
        var telemetry = new TelemetryData
        {
            Timestamp = DateTime.UtcNow,
            Temperature = Math.Round(_securityDevice.Temperature, 2),
            Humidity = Math.Round(_securityDevice.Humidity, 2),
            Pressure = Math.Round(1000 + _random.NextDouble() * 50, 2)
        };

        // Record metrics
        _metricsService.RecordTelemetry(
            _device.DeviceId,
            _device.Location,
            telemetry.Temperature,
            telemetry.Humidity,
            telemetry.Pressure
        );

        // Record device state
        _metricsService.RecordDeviceState(
            _device.DeviceId,
            _securityDevice.Status.ToString(),
            _securityDevice.BatteryLevel,
            _securityDevice.SignalStrength
        );

        _loggingService.LogTelemetrySent(_device.DeviceId, telemetry);
        _logger.LogInformation("Telemetry sent for device {DeviceId}: {Telemetry}", _device.DeviceId, telemetry);

        // Send to IoT Hub and measure duration
        var messageString = JsonSerializer.Serialize(telemetry);
        var messageBytes = Encoding.UTF8.GetBytes(messageString);
        var message = new Message(messageBytes)
        {
            ContentType = "application/json",
            ContentEncoding = "utf-8"
        };

        var startTime = DateTime.UtcNow;
        try
        {
            await _deviceClient.SendEventAsync(message);
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _metricsService.RecordTelemetryDuration(_device.DeviceId, _device.Location, duration);
            _logAction($"Telemetry sent to IoT Hub in {duration:F2}ms");
        }
        catch (Exception ex)
        {
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _metricsService.RecordTelemetryDuration(_device.DeviceId, _device.Location, duration);
            _logger.LogError(ex, "Failed to send telemetry after {Duration}ms", duration);
            throw;
        }
    }

    private void UpdateDeviceState()
    {
        // Simulate battery drain
        _securityDevice.BatteryLevel = Math.Max(0, _securityDevice.BatteryLevel - 0.1);

        // Simulate signal strength changes
        _securityDevice.SignalStrength = _random.Next(1, 6);

        // Simulate temperature and humidity changes
        _securityDevice.Temperature = 20 + _random.NextDouble() * 10;
        _securityDevice.Humidity = 40 + _random.NextDouble() * 30;

        // Simulate motion detection
        if (_random.NextDouble() < 0.1)
        {
            _securityDevice.MotionDetected = true;
            _logAction("Motion detected!");
        }
        else
        {
            _securityDevice.MotionDetected = false;
        }

        // Simulate door state
        if (_random.NextDouble() < 0.05)
        {
            _securityDevice.DoorOpen = !_securityDevice.DoorOpen;
            _logAction($"Door {( _securityDevice.DoorOpen ? "opened" : "closed" )}");
        }

        // Update cellular data
        _securityDevice.CellularData.DataUsageMB += _random.NextDouble() * 0.1;
        _securityDevice.CellularData.LastSync = DateTime.UtcNow;

        // Check alert conditions
        CheckAlertConditions();
    }

    private void CheckAlertConditions()
    {
        var alerts = new List<string>();

        // Check temperature thresholds
        if (_securityDevice.Temperature > _securityDevice.AlertThresholds["temperatureHigh"])
        {
            var alert = $"High temperature alert: {_securityDevice.Temperature:F1}°C";
            alerts.Add(alert);
            _metricsService.RecordAlert(_device.DeviceId, "temperature", alert);
        }
        else if (_securityDevice.Temperature < _securityDevice.AlertThresholds["temperatureLow"])
        {
            var alert = $"Low temperature alert: {_securityDevice.Temperature:F1}°C";
            alerts.Add(alert);
            _metricsService.RecordAlert(_device.DeviceId, "temperature", alert);
        }

        // Check humidity thresholds
        if (_securityDevice.Humidity > _securityDevice.AlertThresholds["humidityHigh"])
        {
            var alert = $"High humidity alert: {_securityDevice.Humidity:F1}%";
            alerts.Add(alert);
            _metricsService.RecordAlert(_device.DeviceId, "humidity", alert);
        }
        else if (_securityDevice.Humidity < _securityDevice.AlertThresholds["humidityLow"])
        {
            var alert = $"Low humidity alert: {_securityDevice.Humidity:F1}%";
            alerts.Add(alert);
            _metricsService.RecordAlert(_device.DeviceId, "humidity", alert);
        }

        // Check battery level
        if (_securityDevice.BatteryLevel < _securityDevice.AlertThresholds["batteryLow"])
        {
            var alert = $"Low battery alert: {_securityDevice.BatteryLevel:F1}%";
            alerts.Add(alert);
            _metricsService.RecordAlert(_device.DeviceId, "battery", alert);
        }

        // Send alerts if any
        if (alerts.Any())
        {
            _securityDevice.Status = DeviceStatus.Alert;
            SendAlertsAsync(alerts).Wait();
        }
        else
        {
            _securityDevice.Status = DeviceStatus.Online;
        }
    }

    private async Task SendAlertsAsync(List<string> alerts)
    {
        var alertData = new
        {
            timestamp = DateTime.UtcNow,
            deviceId = _securityDevice.DeviceId,
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
            _device.ReportingInterval = interval;
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
            _securityDevice.Status = DeviceStatus.Alert;
            await SendAlertsAsync(new List<string> { message });
            return new MethodResponse(200);
        }

        return new MethodResponse(400);
    }

    private async Task<MethodResponse> HandleMaintenance(MethodRequest methodRequest, object? userContext)
    {
        Console.WriteLine($"Received method call: {methodRequest.Name}");
        _securityDevice.Status = DeviceStatus.Maintenance;
        _securityDevice.LastMaintenance = DateTime.UtcNow;
        _securityDevice.BatteryLevel = 100.0; // Simulate battery replacement

        var reportedProperties = new TwinCollection
        {
            ["status"] = _securityDevice.Status.ToString(),
            ["lastMaintenance"] = _securityDevice.LastMaintenance,
            ["batteryLevel"] = _securityDevice.BatteryLevel
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
                        _securityDevice.MotionSensitivity = sensitivity;
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
                        _securityDevice.AlertThresholds = newThresholds;
                        _logAction("Alert thresholds updated");
                    }
                    break;
            }
        }

        // Report back the current state
        var reportedProperties = new TwinCollection
        {
            ["reportingInterval"] = _device.ReportingInterval,
            ["motionSensitivity"] = _securityDevice.MotionSensitivity,
            ["alertThresholds"] = _securityDevice.AlertThresholds
        };

        await _deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
    }
} 