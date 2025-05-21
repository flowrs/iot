using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace DeviceSimulator.Services;

public class MetricsService
{
    private readonly Meter _meter;
    private readonly Counter<int> _telemetryCounter;
    private readonly Histogram<double> _temperatureHistogram;
    private readonly Histogram<double> _humidityHistogram;
    private readonly Histogram<double> _pressureHistogram;
    private readonly Histogram<double> _telemetryDurationHistogram;
    private readonly Counter<int> _alertCounter;
    private readonly ILogger<MetricsService> _logger;

    public MetricsService(ILogger<MetricsService> logger)
    {
        _logger = logger;
        _meter = new Meter("DeviceSimulator.Metrics", "1.0.0");
        
        // Create counters and histograms with custom dimensions
        _telemetryCounter = _meter.CreateCounter<int>("device.telemetry.sent", "Number of telemetry messages sent");
        _temperatureHistogram = _meter.CreateHistogram<double>("device.temperature", "Celsius", "Temperature measurements");
        _humidityHistogram = _meter.CreateHistogram<double>("device.humidity", "Percent", "Humidity measurements");
        _pressureHistogram = _meter.CreateHistogram<double>("device.pressure", "hPa", "Pressure measurements");
        _telemetryDurationHistogram = _meter.CreateHistogram<double>("device.telemetry.duration", "Milliseconds", "Duration of telemetry send operations");
        _alertCounter = _meter.CreateCounter<int>("device.alerts", "Number of alerts generated");
    }

    public void RecordTelemetry(string deviceId, string location, double temperature, double humidity, double pressure)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("device.id", deviceId),
            new("device.location", location),
            new("environment", "production")
        };

        _telemetryCounter.Add(1, tags);
        _temperatureHistogram.Record(temperature, tags);
        _humidityHistogram.Record(humidity, tags);
        _pressureHistogram.Record(pressure, tags);

        _logger.LogDebug("Recorded telemetry metrics for device {DeviceId}", deviceId);
    }

    public void RecordTelemetryDuration(string deviceId, string location, double durationMs)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("device.id", deviceId),
            new("device.location", location),
            new("environment", "production")
        };

        _telemetryDurationHistogram.Record(durationMs, tags);
        _logger.LogDebug("Recorded telemetry duration for device {DeviceId}: {Duration}ms", deviceId, durationMs);
    }

    public void RecordAlert(string deviceId, string alertType, string alertMessage)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("device.id", deviceId),
            new("alert.type", alertType),
            new("alert.message", alertMessage),
            new("environment", "production")
        };

        _alertCounter.Add(1, tags);
        _logger.LogDebug("Recorded alert metric for device {DeviceId}", deviceId);
    }

    public void RecordDeviceState(string deviceId, string state, double batteryLevel, int signalStrength)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("device.id", deviceId),
            new("device.state", state),
            new("device.battery_level", batteryLevel),
            new("device.signal_strength", signalStrength),
            new("environment", "production")
        };

        _meter.CreateObservableGauge("device.battery", () => batteryLevel, "Percent", "Battery level");
        _meter.CreateObservableGauge("device.signal", () => signalStrength, "Bars", "Signal strength");

        _logger.LogDebug("Recorded device state metrics for device {DeviceId}", deviceId);
    }
} 