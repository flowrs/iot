using Microsoft.Extensions.Logging;
using DeviceSimulator.Models;

namespace DeviceSimulator.Services;

public class LoggingService
{
    private readonly ILogger<LoggingService> _logger;
    private readonly Random _random = new();

    public LoggingService(ILogger<LoggingService> logger)
    {
        _logger = logger;
    }

    public void LogDeviceStartup(string deviceId)
    {
        _logger.LogInformation("Device {DeviceId} starting up", deviceId);
    }

    public void LogDeviceShutdown(string deviceId)
    {
        _logger.LogInformation("Device {DeviceId} shutting down", deviceId);
    }

    public void LogTelemetrySent(string deviceId, TelemetryData telemetry)
    {
        _logger.LogInformation(
            "Telemetry sent for device {DeviceId}: Temperature={Temperature}°C, Humidity={Humidity}%, Pressure={Pressure}hPa",
            deviceId,
            telemetry.Temperature,
            telemetry.Humidity,
            telemetry.Pressure);
    }

    public void LogDeviceStateChange(string deviceId, string state, string previousState)
    {
        _logger.LogInformation("Device state changed - Device: {DeviceId}, Previous: {PreviousState}, New: {NewState}", 
            deviceId, previousState, state);
    }

    public void SimulateErrors()
    {
        // Simulate various error scenarios
        if (_random.NextDouble() < 0.3)
        {
            _logger.LogError("Failed to connect to IoT Hub - Connection timeout");
        }

        if (_random.NextDouble() < 0.2)
        {
            _logger.LogError("Sensor reading failed - Invalid data format");
        }

        if (_random.NextDouble() < 0.15)
        {
            _logger.LogError("Failed to process cloud message - Invalid payload");
        }

        // New error scenarios
        if (_random.NextDouble() < 0.25)
        {
            _logger.LogError("Firmware update failed - {ErrorCode}: {ErrorMessage}", 
                _random.Next(1000, 9999), 
                "Update verification failed");
        }

        if (_random.NextDouble() < 0.2)
        {
            _logger.LogError("Device authentication failed - Invalid credentials");
        }

        if (_random.NextDouble() < 0.15)
        {
            _logger.LogError("Storage write error - Disk space critical");
        }

        if (_random.NextDouble() < 0.1)
        {
            _logger.LogError("Network configuration error - Invalid IP address format");
        }

        if (_random.NextDouble() < 0.05)
        {
            _logger.LogError("Critical system error - Memory allocation failed");
        }
    }

    public void SimulateWarnings()
    {
        // Simulate various warning scenarios
        if (_random.NextDouble() < 0.4)
        {
            _logger.LogWarning("Battery level low - {BatteryLevel}%", _random.Next(10, 20));
        }

        if (_random.NextDouble() < 0.3)
        {
            _logger.LogWarning("Signal strength degraded - {SignalStrength}/5", _random.Next(1, 3));
        }

        if (_random.NextDouble() < 0.25)
        {
            _logger.LogWarning("High temperature detected - {Temperature}°C", _random.Next(30, 35));
        }

        // New warning scenarios
        if (_random.NextDouble() < 0.35)
        {
            _logger.LogWarning("Storage space running low - {FreeSpace}MB remaining", _random.Next(50, 200));
        }

        if (_random.NextDouble() < 0.3)
        {
            _logger.LogWarning("Network latency high - {Latency}ms", _random.Next(100, 500));
        }

        if (_random.NextDouble() < 0.25)
        {
            _logger.LogWarning("Device memory usage high - {MemoryUsage}%", _random.Next(80, 95));
        }

        if (_random.NextDouble() < 0.2)
        {
            _logger.LogWarning("Sensor calibration needed - Last calibration: {DaysAgo} days ago", 
                _random.Next(30, 90));
        }

        if (_random.NextDouble() < 0.15)
        {
            _logger.LogWarning("Firmware update available - Current: {CurrentVersion}, Available: {NewVersion}", 
                "1.0.0", "1.1.0");
        }
    }

    public void SimulateSecurityWarnings()
    {
        if (_random.NextDouble() < 0.2)
        {
            _logger.LogWarning("Multiple failed login attempts detected - {Attempts} attempts", 
                _random.Next(3, 10));
        }

        if (_random.NextDouble() < 0.15)
        {
            _logger.LogWarning("Suspicious network activity detected - {IPAddress}", 
                $"192.168.1.{_random.Next(1, 255)}");
        }

        if (_random.NextDouble() < 0.1)
        {
            _logger.LogWarning("Unauthorized configuration change attempt - {Component}", 
                new[] { "Network", "Security", "System" }[_random.Next(3)]);
        }
    }

    public void SimulatePerformanceWarnings()
    {
        if (_random.NextDouble() < 0.3)
        {
            _logger.LogWarning("High CPU usage detected - {CpuUsage}%", _random.Next(80, 95));
        }

        if (_random.NextDouble() < 0.25)
        {
            _logger.LogWarning("Response time degraded - {ResponseTime}ms", _random.Next(200, 1000));
        }

        if (_random.NextDouble() < 0.2)
        {
            _logger.LogWarning("Queue processing delay - {QueueSize} items pending", _random.Next(100, 500));
        }
    }

    public void LogDeviceMetrics(string deviceId, double batteryLevel, int signalStrength, double temperature)
    {
        _logger.LogInformation(
            "Device metrics - Device: {DeviceId}, Battery: {BatteryLevel}%, Signal: {SignalStrength}/5, Temp: {Temperature}°C",
            deviceId, batteryLevel, signalStrength, temperature);
    }
} 